using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gherkin.Ast;
using Gherkin2MtmApi.Models;
using Gherkin2MtmApi.Utils;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Gherkin2MtmApi.Helpers
{
    public static class TestCaseHelper
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(TestCaseHelper));

        internal static void UpdateTestcase(IHasSteps background, IHasSteps scenarioDefinition, ITestCase testCase)
        {
            var isScenarioOutline = scenarioDefinition is ScenarioOutline;
            AddBackground(background, testCase);
            StepHelper.AddSteps(testCase, scenarioDefinition?.Steps, "", isScenarioOutline);
            if (!isScenarioOutline) {
                testCase.DefaultTable.Reset();
                return;
            }

            var scenarioOutline = (ScenarioOutline)scenarioDefinition;
            AddParameters(scenarioOutline, testCase);
        }

        public static bool UpdateTestcaseFields(IList<Tag> scenarioTags, ITestBase testCase,
            IEnumerable<TestCaseField> testCaseFields)
        {
            if (testCase == null || testCaseFields == null)
            {
                return false;
            }

            var fieldMapper = UpdateMappedFields(scenarioTags, testCase, testCaseFields);
            if (fieldMapper.Count <= 0)
            {
                return false;
            }

            var tags = GetUnmappedTags(scenarioTags, fieldMapper["matchedFields"]);
            if (tags.EndsWith(",", StringComparison.InvariantCulture))
            {
                tags = tags.TrimEnd(',');
            }

            var tagsFieldValue = testCase.WorkItem.Fields[SyncUtil.TagsField].Value.ToString();
            if (tags.Equals(tagsFieldValue, StringComparison.InvariantCulture))
            {
                return fieldMapper["modifiedFields"].Count > 0;
            }

            testCase.WorkItem.Fields[SyncUtil.TagsField].Value = tags;
            return true;
        }

        public static bool UpdateLinks(IList<int> newLinks, ITestBase testCase)
        {
            if (testCase == null)
            {
                return false;
            }

            if (newLinks.Count == 0)
            {
                return false;
            }

            foreach (var link in newLinks)
            {
                if (!LinkAlreadyExists(link, testCase.WorkItem.Links))
                {
                    var externalLink = new RelatedLink(link);
                    testCase.WorkItem.Links.Add(externalLink);
                }
            }

            return true;
        }

        private static bool LinkAlreadyExists(int workItemId, LinkCollection links)
        {
            foreach (RelatedLink link in links)
            {
                if (workItemId == link.RelatedWorkItemId)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool UpdateTestCaseDetails(ScenarioDefinition scenarioDefinition, ITestBase testCase)
        {
            if (testCase == null)
            {
                return false;
            }

            var scenarioDescription = scenarioDefinition?.Description?.Trim() ?? string.Empty;
            scenarioDescription = scenarioDescription
                .Replace("\n", "<br>")
                .Replace("\r", "");
            var title = scenarioDefinition?.Name?.Trim() ?? string.Empty;
            var updated = false;
            if (testCase.WorkItem.Description.Trim() != scenarioDescription)
            {
                testCase.WorkItem.Description = scenarioDescription;
                updated = true;
            }

            if (testCase.Title == title) return updated;

            testCase.Title = title;
            return true;
        }

        private static IDictionary<string, IList<TestCaseField>> UpdateMappedFields(IEnumerable<Tag> scenarioTags, ITestBase testCase,
            IEnumerable<TestCaseField> testCaseFields)
        {
            var matchedFields = new List<TestCaseField>();
            var modifiedFields = new List<TestCaseField>();
            foreach (var testCaseField in testCaseFields)
            {
                var tagValue = GetTagValue(scenarioTags, testCaseField.Tag, testCaseField.Prefix);
                if (!testCaseField.Required && tagValue.Trim().Length <= 0
                    && testCaseField.RequirementField != null
                    && testCaseField.RequirementField.Trim().Length <= 0)
                {
                    continue;
                }

                if (testCaseField.RequirementField != null && testCaseField.RequirementField.Trim().Length > 0)
                {
                    tagValue = GetTagValue(scenarioTags, SyncUtil.REQUIREMENT_TAG_NAME, testCaseField.Prefix);
                    var tagValues = tagValue.Split(',').Select(requirementId =>
                        FeatureHelper.GetWorkItemField(testCaseField.RequirementField, requirementId));
                    tagValue = tagValues.Aggregate((i, j) => $"{i},{j}");
                }

                if (tagValue.Length > 0)
                {
                    matchedFields.Add(testCaseField);
                }

                if (!testCaseField.AllowMultiple && tagValue.Contains(","))
                {
                    Logger.Error($"{testCaseField.Name} field cannot have multiple values: {tagValue}");
                    continue;
                }

                try
                {
                    var fieldValue = testCase.WorkItem.Fields[testCaseField.Name].Value.ToString();
                    if (tagValue == fieldValue)
                    {
                        continue;
                    }

                    Logger.Info($"{testCaseField.Name}:{tagValue}");
                    testCase.WorkItem.Fields[testCaseField.Name].Value = tagValue;
                    modifiedFields.Add(testCaseField);
                }
                catch (Exception exception)
                {
                    Logger.Error(exception.Message);
                }
            }

            var fieldResultMapper = new Dictionary<string, IList<TestCaseField>>
            {
                {"matchedFields", matchedFields},
                {"modifiedFields", modifiedFields}
            };
            return fieldResultMapper;
        }

        internal static string GetTagValue(IEnumerable<Tag> scenarioTags, string tagName, string prefix)
        {
            string tagValue;
            var sb = new StringBuilder();
            foreach (var scenarioTag in scenarioTags)
            {
                var match = Regex.Match(scenarioTag.Name, $"{SyncUtil.TagNameIdPattern}");
                if (!string.Equals(match.Groups[1].Value, tagName, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                tagValue = $"{prefix}{match.Groups[2].Value},";
                sb.Append(tagValue);
            }
            
            tagValue = sb.ToString();
            return tagValue.Trim().Length <= 0
                ? tagValue
                : tagValue.Substring(0, tagValue.LastIndexOf(",", StringComparison.InvariantCulture));
        }

        private static string GetUnmappedTags(IEnumerable<Tag> scenarioTags, IEnumerable<TestCaseField> matchedFields)
        {
            return string.Join(
                SyncUtil.TagsToken, scenarioTags.Select(tag =>
                {
                    var tagName = tag.Name;

                    var match = Regex.Match(tagName, $"{SyncUtil.TagNameIdPattern}");
                    if (!match.Success &&
                        !string.Equals(tagName, SyncUtil.ManualTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return tagName.Trim().Length != 0 ? tagName.Substring(1) : null;
                    }

                    tagName = match.Groups[1].Value;
                    if (tagName.IsNullOrEmpty() || IsMappedField(tagName, matchedFields))
                    {
                        return null;
                    }

                    return string.Equals(tagName, SyncUtil.MtmTcTagName, StringComparison.InvariantCultureIgnoreCase) ?
                        null :
                        match.Groups[2].Value;
                }).SkipWhile(string.IsNullOrEmpty));
        }

        private static bool IsMappedField(string tagName, IEnumerable<TestCaseField> matchedFields)
        {
            return matchedFields.Any(
                testCaseField => testCaseField.Tag.ToUpperInvariant().Equals(
                    tagName.ToUpperInvariant(), StringComparison.InvariantCulture));
        }

        private static void AddParameters(ScenarioOutline scenarioOutline, ITestCase testCase)
        {
            foreach (var example in scenarioOutline.Examples)
            {
                foreach (var tableHeaderCell in example.TableHeader.Cells)
                {
                    var columnName = RegexUtil.ReplaceSpaces(tableHeaderCell.Value, SyncUtil.ParameterToken);
                    if (testCase.DefaultTableReadOnly.Columns.Contains(columnName))
                    {
                        continue;
                    }

                    testCase.DefaultTableReadOnly.Columns.Add(columnName, typeof(string));
                }

                testCase.DefaultTableReadOnly.Rows.Clear();
                foreach (var tableRow in example.TableBody)
                {
                    var cells = tableRow.Cells.Select(tableCell => tableCell.Value);
                    testCase.DefaultTableReadOnly.Rows.Add(cells.ToArray<object>());
                }
            }
        }

        private static void AddBackground(IHasSteps background, ITestBase testCase)
        {
            if (background == null)
            {
                return;
            }

            StepHelper.AddSteps(testCase, background.Steps, SyncUtil.BackgroundPrefix, false);
        }
    }
}
