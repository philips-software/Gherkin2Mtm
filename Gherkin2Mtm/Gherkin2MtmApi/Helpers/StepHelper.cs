using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gherkin.Ast;
using Gherkin2MtmApi.Utils;
using Microsoft.TeamFoundation.TestManagement.Client;

namespace Gherkin2MtmApi.Helpers
{
    using System.Web;

    internal static class StepHelper
    {
        private const string PARAMETER_PATTERN = @"<(.*?)>";
        private const string THEN = "THEN";
        private const string WHEN = "WHEN";

        public static void AddSteps(ITestBase testCase, IEnumerable<Step> steps, string prefix, bool isScenarioOutline)
        {
            if (steps == null || testCase == null)
            {
                return;
            }

            var isThen = false;
            foreach (var stepDef in steps)
            {
                var step = testCase.CreateTestStep();
                var text = stepDef.Text;
                if (isScenarioOutline)
                {
                    text = TransformParameters(text);
                }

                text = HttpUtility.HtmlEncode(text);
                var dataTable = CreateDataTable(stepDef.Argument, isScenarioOutline);

                /*
                 * This was to handle the situation when, the "Then" step followed by "And" steps in which case,
                 * they should also go to Expected Result column instead of a normal step. It should resume
                 * populating normal steps as soon as it hits a WHEN, assuming that there won't be Givens
                 * followed by Then
                 */
                if (stepDef.Keyword.Trim().Equals(WHEN, StringComparison.InvariantCultureIgnoreCase))
                {
                    isThen = false;
                }

                if (stepDef.Keyword.Trim().Equals(THEN, StringComparison.InvariantCultureIgnoreCase) || isThen)
                {
                    isThen = true;
                    step.ExpectedResult = text;
                    step.ExpectedResult += dataTable;
                    step.Title = stepDef.Keyword.Trim();
                }
                else
                {
                    step.Title = $"{prefix} {stepDef.Keyword} {text}";

                    step.Title += dataTable;
                }

                testCase.Actions.Add(step);
            }
        }

        private static string CreateDataTable(StepArgument stepArgument, bool isScenarioOutline)
        {
            if (!(stepArgument is DataTable))
            {
                return "";
            }

            var dataTable = (DataTable)stepArgument;
            var htmlBulder = new StringBuilder();
            const string TABLE_START_ELEMENT = @"<Table style='border:1px;'>";
            const string TABLE_END_ELEMENT = "</Table>";
            const string TABLE_ROW_START_ELEMENT = "<Tr>";
            const string TABLE_ROW_END_ELEMENT = "</Tr>";
            const string TABLE_HEADER_START_ELEMENT = "<Th>";
            const string TABLE_HEADER_END_ELEMENT = "</Th>";
            const string TABLE_COLUMN_START_ELEMENT = "<Td>";
            const string TABLE_COLUMN_END_ELEMENT = "</Td>";
            const string TAG_BOLD_START = "<b>";
            const string TAG_BOLD_END = "</b>";

            htmlBulder.Append(TABLE_START_ELEMENT);
            var rowList = dataTable.Rows.ToList();
            htmlBulder.Append(TABLE_ROW_START_ELEMENT);
            foreach (var tableCell in rowList[0].Cells)
            {
                htmlBulder.Append($"{TAG_BOLD_START}{TABLE_HEADER_START_ELEMENT}{tableCell.Value}{TABLE_HEADER_END_ELEMENT}{TAG_BOLD_END}");
            }

            htmlBulder.Append(TABLE_ROW_END_ELEMENT);

            for (var rowIndex = 1; rowIndex < rowList.Count; rowIndex++)
            {
                var dataTableRow = rowList[rowIndex];
                htmlBulder.Append(TABLE_ROW_START_ELEMENT);
                htmlBulder = dataTableRow.Cells.Aggregate(
                    htmlBulder,
                    (current, tableCell) =>
                        {
                            var cellValue = tableCell.Value;
                            if (isScenarioOutline)
                            {
                                cellValue = TransformParameters(cellValue);
                            }

                            cellValue = HttpUtility.HtmlEncode(cellValue);
                            return htmlBulder.Append($"{TABLE_COLUMN_START_ELEMENT}{cellValue}{TABLE_COLUMN_END_ELEMENT}");
                    });
                htmlBulder.Append(TABLE_ROW_END_ELEMENT);
            }

            htmlBulder.Append(TABLE_END_ELEMENT);
            return htmlBulder.ToString();
        }

        private static string TransformParameters(string text)
        {
            var allMatches = Regex.Matches(text, PARAMETER_PATTERN);
            foreach (Match match in allMatches)
            {
                var value = match.Groups[1].Value;
                var parameter = $"<{value}>";
                text = Regex.Replace(
                    text,
                    parameter,
                    $"@{RegexUtil.ReplaceSpaces(value, SyncUtil.ParameterToken)}");
            }

            return text;
        }
    }
}
