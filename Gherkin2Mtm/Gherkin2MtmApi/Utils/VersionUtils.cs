using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Gherkin.Ast;

namespace Gherkin2MtmApi.Utils
{
    internal static class VersionUtils
    {
        public static IList<string> GetLinesList(string[] arrLine, Location startLocation, Location endLocation)
        {
            if (startLocation == null || arrLine == null)
            {
                return new List<string>();
            }

            var startingLine = startLocation.Line;
            var endingLine = arrLine.Length;
            if (endLocation != null)
            {
                endingLine = endLocation.Line - 1;
            }

            var linesList = new List<string>();
            for (var i = startingLine - 1; i < endingLine; i++)
            {
                var line = Regex.Replace(arrLine[i], @"[\t\r\n]", "");
                if (line.Trim().Length > 0)
                {
                    linesList.Add(line);
                }
            }

            return linesList;
        }

        public static string GetVersion(string[] arrLine, Location startLocation,
            Location endLocation, IEnumerable<string> backgroundLines)
        {
            var lines = new List<string>();
            if (backgroundLines != null)
            {
                lines.AddRange(backgroundLines);
            }

            lines.AddRange(GetLinesList(arrLine, startLocation, endLocation));
            var source = string.Join("", lines);
            source = Regex.Replace(source, $@"\s+{SyncUtil.MtmTcLinkTagPattern}", string.Empty);
            using (var md5Hash = new SHA256Managed())
            {
                return GetMd5Hash(md5Hash, source);
            }
        }

        private static string GetMd5Hash(HashAlgorithm md5Hash, string input)
        {
            var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            foreach (var item in data)
            {
                sBuilder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
            }

            return sBuilder.ToString();
        }

        public static bool IsUptoDate(string currentVersion, string previousVersion)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(currentVersion, previousVersion) == 0;
        }
    }
}
