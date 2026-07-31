using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Schala.Utility
{
    public static class StringHelper
    {
        public static int GetValueFromRegex(this string Text, Regex Expression)
        {
            Match profMatch = Expression.Match(Text);
            if (profMatch.Success)
            {
                if (string.IsNullOrEmpty(profMatch.Groups[1].Value))
                    return int.Parse(profMatch.Groups[2].Value);
                else
                    return int.Parse(profMatch.Groups[1].Value);
            }

            return 0;
        }

        public static string SanitizeSqlParameter(this string input)
        {
            return input.Replace("'", "''");
        }
    }
}
