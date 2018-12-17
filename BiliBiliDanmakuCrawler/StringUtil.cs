using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

// ReSharper disable IdentifierTypo
namespace BiliBiliDanmakuCrawler
{
    public static class StringUtil
    {
        private static readonly StringBuilder StringBuilder = new StringBuilder();
        public static string PatternFirst(this string source, string pattern, int group)
        {
            var match = Regex.Match(source, pattern);
            return match.Groups[group].ToString();
        }

        public static List<string> PatternEach(this string source, string pattern, int group)
        {
            var returnList = new List<string>();

            var matches = Regex.Matches(source, pattern);
            foreach (Match match in matches) returnList.Add(match.Groups[group].ToString());

            return returnList;
        }

        public static bool IsNumber(this string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            const string pattern = "^[0-9]*$";
            var rx = new Regex(pattern);
            return rx.IsMatch(s);
        }

        public static string GetString(this byte[] source, Encoding encoding)
        {
            return encoding.GetString(source);
        }

        public static string TrimToChain(this List<Cookie> list)
        {
            foreach (var cookie in list)
            {
                StringBuilder.Append(cookie.Name).Append('=').Append(cookie.Value).Append(";");
            }

            return StringBuilder.ToString();
        }

        public static string EncodeTo(this string str, Encoding charset)
        {
            return HttpUtility.UrlEncode(str, charset);
        }

        public static string DecodeAs(this string str, Encoding charset)
        {
            return HttpUtility.UrlDecode(str, charset);
        }
    }
}