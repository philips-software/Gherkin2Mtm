using System.Text.RegularExpressions;

namespace Gherkin2MtmApi.Utils
{
    internal static class RegexUtil
    {
        private const string SPACE_PATTERN = @"\s+";

        public static string ReplaceSpaces(string text, string replaceText)
        {
            return Regex.Replace(text, SPACE_PATTERN, replaceText);
        }
    }
}
