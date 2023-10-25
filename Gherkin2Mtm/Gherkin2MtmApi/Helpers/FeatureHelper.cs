using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gherkin;
using Gherkin.Ast;
using Gherkin2MtmApi.Models;
using Gherkin2MtmApi.Utils;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Field = Microsoft.TeamFoundation.WorkItemTracking.Client.Field;
using ITestBase = Microsoft.TeamFoundation.TestManagement.Client.ITestBase;

namespace Gherkin2MtmApi.Helpers
{
    public static class FeatureHelper
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(FeatureHelper));
        private static AdoHelper _adoHelper;

        public static void WorkoutFeatures(CommandLineOptions commandLineOptions, ITestManagementTeamProject teamProject, string featurePath,
            string area, IList<TestCaseField> fieldsCollection)
        {
            _adoHelper = new AdoHelper(commandLineOptions);
            var featureFiles = GetFeatureFiles(featurePath);
            foreach (var featureFile in featureFiles)
            {
                WorkoutFeature(teamProject, featureFile, area, fieldsCollection);
            }
        }

        public static string GetWorkItemField(string fieldName, string workItemId)
        {
            return _adoHelper.GetWorkItemFieldValue(fieldName, workItemId).Result;
        }

        private static IEnumerable<string> GetFeatureFiles(string featuresPath)
        {
            string[] featureFiles;
            var featureFilePattern = $"{SyncUtil.FeatureFilter.Substring(1)}";
            try
            {
                if (featuresPath.Contains(";"))
                {
                    featureFiles = featuresPath.Split(';');
                    featureFiles = featureFiles.Select(
                        featureFile =>
                        {
                            if (!featuresPath.EndsWith(featureFilePattern, StringComparison.InvariantCulture))
                            {
                                throw new DirectoryNotFoundException($"Not a feature file, \"{featureFile}\"");
                            }

                            return featureFile;
                        }).ToArray();
                }
                else if (featuresPath.EndsWith(featureFilePattern, StringComparison.InvariantCulture))
                {
                    if (File.Exists(featuresPath))
                    {
                        return new[] { featuresPath };
                    }

                    Logger.Error($"Feature file at path, \"{featuresPath}\" is not found");
                    return new string[] { };
                }
                else
                {
                    featureFiles = Directory.GetFiles(
                        featuresPath, SyncUtil.FeatureFilter, SearchOption.AllDirectories);
                }
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
                Logger.Error(directoryNotFoundException.Message);
                return new string[] { };
            }

            return featureFiles;
        }

        private static void WorkoutFeature(ITestManagementTeamProject teamProject, string featureFile,
            string area, IList<TestCaseField> fieldsCollection)
        {
            var parser = new Parser();
            var tags = new List<Tag>();
            Feature feature;
            var message = $"There is an error parsing the feature file, \"{featureFile}\". See below for more details";
            try
            {
                feature = parser.Parse(featureFile).Feature;
            }
            catch (ParserException parserException)
            {
                Logger.Error(message);
                Logger.Error(parserException.Message);
                return;
            }

            var firstScenario =
                feature.Children.FirstOrDefault(scenarioDefinition => !(scenarioDefinition is Background));
            if (firstScenario == null)
            {
                return;
            }

            var arrLine = File.ReadAllLines(featureFile);
            Logger.Info(ResourceStrings.DECORATION, $"Syncing feature, {featureFile}");

            ChurnScenarios(arrLine, feature, teamProject, tags, area, fieldsCollection);
            Logger.Info(ResourceStrings.DECORATION, "feature sync completed");

            if (tags.Count <= 0) return;

            foreach (var tag in tags)
            {
                var lineLocation = tag.Location.Line - 1;
                var tagLocationLineText = arrLine[lineLocation];
                var isSuccessfulMatch = Regex.Match(tagLocationLineText, SyncUtil.MtmTcLinkTagPattern).Success;
                if (!isSuccessfulMatch)
                {
                    arrLine[lineLocation] = tagLocationLineText + " " + tag.Name;
                    continue;
                }

                arrLine[lineLocation] = Regex.Replace(tagLocationLineText, SyncUtil.MtmTcLinkTagPattern, tag.Name);
            }

            File.WriteAllLines(featureFile, arrLine);
        }

        private static bool ValidateTags(IEnumerable<Tag> scenarioTags, IEnumerable<TestCaseField> fieldsCollection)
        {
            scenarioTags = scenarioTags.ToList();
            foreach (var testCaseField in fieldsCollection)
            {
                if (testCaseField.Required && IsRequiredFieldMissing(testCaseField, scenarioTags))
                {
                    Logger.Error(
                        $"{testCaseField.Tag} is a required tag but is not found as one of the tags of the scenario");
                    return false;
                }

                var tag = GetTag(scenarioTags, testCaseField.Tag);
                var tagValue = TestCaseHelper.GetTagValue(scenarioTags, testCaseField.Tag, testCaseField.Prefix);
                if (tag != null && tagValue.Length <= 0)
                {
                    Logger.Error(
                        $"{testCaseField.Tag} does not have a value associated with it");
                    return false;
                }

                if (testCaseField.RequirementField == null ||
                    testCaseField.RequirementField.Trim().Length <= 0) continue;

                tagValue = TestCaseHelper.GetTagValue(scenarioTags, SyncUtil.REQUIREMENT_TAG_NAME, "");
                if (tagValue.Trim().Length > 0 && !IsValidRequirement(testCaseField, tagValue))
                {
                    return false;
                }
            }

            return true;
        }

        private static Tag GetTag(IEnumerable<Tag> tags, string tagName)
        {
            return tags.ToList().Find(tag =>
            {
                var match = Regex.Match(tag.Name, $"{SyncUtil.TagNameIdPattern}");
                return string.Equals(match.Groups[1].Value, tagName, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        private static bool IsValidRequirement(TestCaseField testCaseField, string requirementIds)
        {
            foreach (var requirementId in requirementIds.Split(','))
            {
                try
                {
                    var workItemField = GetWorkItemField(testCaseField.RequirementField, requirementId);
                    Logger.Info($"{testCaseField.RequirementField}: {workItemField} for RequirementId: {requirementId}");
                }
                catch (Exception)
                {
                    Logger.Error($"Check that the requirement, {requirementId} exists");
                    return false;
                }
            }

            return true;
        }

        private static bool IsRequiredFieldMissing(TestCaseField field, IEnumerable<Tag> scenarioTags)
        {
            foreach (var scenarioTag in scenarioTags)
            {
                var regMatchGroups = Regex.Match(scenarioTag.Name, $"{SyncUtil.TagNameIdPattern}").Groups;
                if (String.Equals(regMatchGroups[1].Value, field.Tag, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsManualScenario(IEnumerable<Tag> scenarioTags)
        {
            return scenarioTags.Any(
                tag => tag.Name.Equals(SyncUtil.ManualTag, StringComparison.InvariantCultureIgnoreCase));
        }

        private static void ChurnScenarios(string[] arrLine, Feature feature, ITestManagementTeamProject teamProject,
            ICollection<Tag> tags, string area, IList<TestCaseField> fieldsCollection)
         {
            var backgroundSteps = GetBackgroundSteps(arrLine, feature);
            var iterationStart = backgroundSteps.Any() ? 1 : 0;
            ScenarioDefinition background = null;
            if (backgroundSteps.Any())
            {
                background = GetBackground(feature);
            }

            var scenarioDefinitions = feature.Children.ToArray();
            for (; iterationStart < scenarioDefinitions.Length; iterationStart++)
            {
                var scenarioDefinition = scenarioDefinitions[iterationStart];
                var scenarioTags = GherkinUtils.GetTags(scenarioDefinition);
                if (!CanChurnScenario(scenarioDefinition, fieldsCollection))
                {
                    continue;
                }

                var hash = GetHash(arrLine, scenarioDefinitions, iterationStart, scenarioDefinition, backgroundSteps);
                var mtmIdTag = scenarioTags.FirstOrDefault(
                    scenarioTag => scenarioTag.Name.Trim().ToUpperInvariant().StartsWith(
                        SyncUtil.MtmTcLink, StringComparison.InvariantCulture));
                Logger.Info(ResourceStrings.DECORATION, $"Syncing scenario, {scenarioDefinition.Name}");
                if (mtmIdTag == null)
                {
                    SaveChanges(teamProject, background, null, scenarioDefinition, hash, area, tags, fieldsCollection);
                    continue;
                }

                var testCaseId = Regex.Match(mtmIdTag.Name.ToUpperInvariant(), SyncUtil.MtmTcIdPattern).Groups[1].Value;
                try
                {
                    var testCase = teamProject.TestCases.Find(Int32.Parse(testCaseId, CultureInfo.InvariantCulture));
                    if (testCase != null)
                    {
                        if (UpdateTestCase(testCase, scenarioDefinition, hash, fieldsCollection)) {
                            testCase.Actions.Clear();
                            SaveChanges(teamProject, background, testCase, scenarioDefinition, hash, area, tags,
                                fieldsCollection);
                        }

                        continue;
                    }
                }
                catch (DeniedOrNotExistException)
                {
                    // This could happen when a test case is deleted from the MTM but exists in the corresponding feature file
                    Logger.Info(ResourceStrings.DECORATION, $"Linked test case, {mtmIdTag.Name}, is not found");
                }
                // Need to create a test case when the link is failed
                SaveChanges(teamProject, background, null, scenarioDefinition, hash, area, tags, fieldsCollection);
            }
        }

        private static IList<string> GetBackgroundSteps(string[] arrLine, Feature feature)
        {
            var background = GetBackground(feature);
            if (background == null)
            {
                return new List<string>();
            }

            var firstScenario =
                feature.Children.FirstOrDefault(scenarioDefinition => !(scenarioDefinition is Background));
            var backgroundFirstStepLocation = background.Steps.First()?.Location;
            return VersionUtils.GetLinesList(
                arrLine, backgroundFirstStepLocation, GherkinUtils.GetFirstTagLocation(firstScenario));

        }

        private static ScenarioDefinition GetBackground(Feature feature)
        {
            return feature.Children.FirstOrDefault(scenarioDefinition => scenarioDefinition is Background);
        }

        private static ITestCase CreateTestCase(ITestManagementTeamProject teamProject, ScenarioDefinition scenarioDefinition,
            IEnumerable<TestCaseField> fieldsCollection)
        {
            var scenarioTags = GherkinUtils.GetTags(scenarioDefinition);
            var testCase = teamProject.TestCases.Create();
            TestCaseHelper.UpdateTestCaseDetails(scenarioDefinition, testCase);
            TestCaseHelper.UpdateTestcaseFields(scenarioTags, testCase, fieldsCollection);
            var stories = scenarioTags.Where(z => z.Name.ToUpperInvariant().StartsWith("@STORY:")).ToList();
            var bugs = scenarioTags.Where(z => z.Name.ToUpperInvariant().StartsWith("@BUG:")).ToList();

            var links = new List<int>();

            foreach (var link in stories)
            {
                var linkNumber = Regex.Match(link.Name.ToUpperInvariant(), $"@STORY:([0-9]+)").Groups[1].Value;
                links.Add(int.Parse(linkNumber));
            }

            foreach (var bug in bugs)
            {
                var linkNumber = Regex.Match(bug.Name.ToUpperInvariant(), $"@BUG:([0-9]+)").Groups[1].Value;
                links.Add(int.Parse(linkNumber));
            }

            var x = TestCaseHelper.UpdateLinks(links, testCase);
            testCase.WorkItem.Fields["Reason"].Value = SyncUtil.REASON;
            testCase.State = SyncUtil.TestCaseStateReady;
            return testCase;
        }

        private static void UpdateTestCaseArea(ITestBase testBase, string area)
        {
            if (area != null &&
                !testBase.Area.Equals(area.ToUpperInvariant(), StringComparison.InvariantCultureIgnoreCase))
            {
                testBase.Area = area;
            }
        }

        private static bool UpdateTestCase(ITestBase testCase, ScenarioDefinition scenarioDefinition,
            string hash, IEnumerable<TestCaseField> fieldsCollection)
        {
            testCase.Refresh();
            var currentState = testCase.State;
            if (currentState.Equals(SyncUtil.RemovedState, StringComparison.InvariantCulture) ||
                currentState.Equals(SyncUtil.ClosedState, StringComparison.InvariantCulture))
            {
                Logger.Info(ResourceStrings.DECORATION, $"{testCase.Id} is {currentState}");
                return false;
            }

            var scenarioTags = GherkinUtils.GetTags(scenarioDefinition);
            var removedTag = scenarioTags.FirstOrDefault(
                scenarioTag => scenarioTag.Name.Trim().ToUpperInvariant().StartsWith(
                    SyncUtil.RemovedTag, StringComparison.InvariantCulture));
            if (removedTag != null)
            {
                testCase.State = SyncUtil.RemovedState;
                SaveTestCase(testCase);
                Logger.Info(ResourceStrings.DECORATION, $"{testCase.Id} is marked removed");
                return false;
            }

            var currentVersion = testCase.WorkItem.Fields[SyncUtil.VersionField].Value
                .ToString();
            var isTestCaseDetailsChanged = TestCaseHelper.UpdateTestCaseDetails(
                scenarioDefinition,
                testCase);
            var isTestCaseFieldsChanged = TestCaseHelper.UpdateTestcaseFields(
                scenarioTags,
                testCase,
                fieldsCollection);

            var stories = scenarioTags.Where(z => z.Name.ToUpperInvariant().StartsWith("@STORY:")).ToList();
            var bugs = scenarioTags.Where(z => z.Name.ToUpperInvariant().StartsWith("@BUG:")).ToList();

            var links = new List<int>();

            foreach (var link in stories)
            {
                var linkNumber = Regex.Match(link.Name.ToUpperInvariant(), $"@STORY:([0-9]+)").Groups[1].Value;
                links.Add(int.Parse(linkNumber));
            }

            foreach (var bug in bugs)
            {
                var linkNumber = Regex.Match(bug.Name.ToUpperInvariant(), $"@BUG:([0-9]+)").Groups[1].Value;
                links.Add(int.Parse(linkNumber));
            }

            var x = TestCaseHelper.UpdateLinks(links, testCase);

            var scenarioStepsUptoDate = VersionUtils.IsUptoDate(hash, currentVersion);
            if (isTestCaseDetailsChanged || isTestCaseFieldsChanged || !scenarioStepsUptoDate)
            {
                return true;
            }

            Logger.Info(ResourceStrings.DECORATION, $"{scenarioDefinition.Name}<{testCase.Id}> is uptodate");
            return false;
        }

        private static bool CanChurnScenario(ScenarioDefinition scenarioDefinition, IEnumerable<TestCaseField> fieldsCollection)
        {
            var scenarioTags = GherkinUtils.GetTags(scenarioDefinition);
            if (!IsManualScenario(scenarioTags))
            {
                Logger.Info(ResourceStrings.DECORATION, $"{scenarioDefinition.Name} ignored: not a manual scenario");
                return false;
            }

            if (ValidateTags(scenarioTags, fieldsCollection))
            {
                return true;
            }

            Logger.Error(ResourceStrings.DECORATION, $"{scenarioDefinition.Name} aborted: Required tags are missing");
            return false;
        }

        private static bool IsTestCaseValid(ITestBase testCase)
        {
            var validationList = testCase.WorkItem.Validate();

            if (validationList.Count <= 0)
            {
                return true;
            }

            Logger.Error(ResourceStrings.DECORATION, "Invalid fields");
            foreach (var validation in validationList)
            {
                var field = (Field)validation;
                Logger.Info($"{field.Name}:{field.Value}");
            }

            return validationList.Count <= 0;
        }

        private static void SaveChanges(ITestManagementTeamProject teamProject, IHasSteps background, ITestCase testCase,
            ScenarioDefinition scenarioDefinition, string hash, string area, ICollection<Tag> tags,
            IEnumerable<TestCaseField> fieldsCollection)
        {
            var isUpdate = true;
            if (testCase == null)
            {
                try
                {
                    testCase = CreateTestCase(teamProject, scenarioDefinition, fieldsCollection);
                }
                catch (InvalidFieldValueException exception)
                {
                    Logger.Error(
                        ResourceStrings.DECORATION,
                        string.Format(CultureInfo.InvariantCulture, SyncUtil.ABORTED_MESSAGE, exception.Message));
                    return;
                }

                isUpdate = false;
            }

            UpdateTestCaseArea(testCase, area);
            TestCaseHelper.UpdateTestcase(background, scenarioDefinition, testCase);
            if (!IsTestCaseValid(testCase))
            {
                Logger.Error(
                    ResourceStrings.DECORATION,
                    string.Format(
                        CultureInfo.InvariantCulture, SyncUtil.ABORTED_MESSAGE,
                        "Something is wrong, check the field values"));
                return;
            }

            SaveTestCase(testCase);
            if (!isUpdate)
            {
                var scenarioTags = GherkinUtils.GetTags(scenarioDefinition);
                var mtmIdTag = scenarioTags.Last();
                tags.Add(
                    new Tag(
                        new Location(mtmIdTag.Location.Line, mtmIdTag.Location.Column + 1),
                        $"{SyncUtil.MtmTcLink}{testCase.Id}"));
                Logger.Info(ResourceStrings.DECORATION, $"{testCase.Id} is created");
                return;
            }

            Logger.Info(ResourceStrings.DECORATION, $"{testCase.Id} is updated");
        }

        private static void SaveTestCase(ITestBase testCase)
        {
            try
            {
                testCase.Flush();
                testCase.Save();
            }
            catch (TestManagementValidationException exception)
            {
                Logger.Info(exception.StackTrace);
                Logger.Error(
                    ResourceStrings.DECORATION,
                    String.Format(
                        CultureInfo.InvariantCulture, SyncUtil.ABORTED_MESSAGE,
                        "Something is wrong, check the field values"));
            }
        }

        private static string GetHash(string[] arrLine, IReadOnlyList<ScenarioDefinition> scenarioDefinitions,
            int startLocation, ScenarioDefinition scenarioDefinition, IEnumerable<string> backgroundSteps)
        {
            var currentScenarioStartLocation = GherkinUtils.GetFirstStepLocation(scenarioDefinition);
            Location nextScenarioStartLocation = null;
            if (startLocation + 1 < scenarioDefinitions.Count)
            {
                nextScenarioStartLocation =
                    GherkinUtils.GetFirstTagLocation(scenarioDefinitions[startLocation + 1]);
            }

            return VersionUtils.GetVersion(
                arrLine, currentScenarioStartLocation, nextScenarioStartLocation, backgroundSteps);
        }
    }
}
