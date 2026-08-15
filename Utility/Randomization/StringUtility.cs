using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

using static Schala.Solver;

namespace Schala;

//A set of functions 
public static class StringUtility
{

    /// <summary>
    /// Returns the start and length of the first match in the string.
    /// </summary>
    /// <param name="Pattern"></param>
    /// <returns></returns>
    public static Tuple<int,int> RegexFind(this string Subject, string Pattern)
    {
        Match m = Regex.Match(Subject, Pattern);
        return m.Success ? new Tuple<int, int>(m.Index, m.Length) : new Tuple<int, int>(-1, -1);
    }

    public static (int, int) RegexFind(this string Subject, Regex Pattern)
    {
        Match m = Pattern.Match(Subject);
        if (m.Success)
        {
            return (m.Index, m.Length);
        }

        return (-1, -1);
    }

    /// <summary>
    /// Replaces all instances of Pattern with Replacement in Subject.
    /// </summary>
    /// <param name="Subject"></param>
    /// <param name="Pattern"></param>
    /// <param name="Replacement"></param>
    /// <returns></returns>
    public static Tuple<bool, string> RegexGsub(this string Subject, string Pattern, string Replacement)
    {
        string output = Regex.Replace(Subject, Pattern, Replacement);
        return new Tuple<bool, string>(Subject == output, output);
    }

    public static Tuple<bool, string> RegexGsub(this string Subject, Regex Pattern, string Replacement)
    {
        string output = Pattern.Replace(Subject, Replacement);
        return new Tuple<bool, string>(Subject == output, output);
    }

    public static Tuple<bool, string> RegexGsub(this string Subject, string Pattern, MatchEvaluator Delegate)
    {
        string output = Regex.Replace(Subject, Pattern, Delegate);
        return new Tuple<bool, string>(Subject == output, output);
    }

    public static Tuple<bool, string> RegexGsub(this string Subject, Regex Regex, RegexEvaluator Delegate, ParserState State)
    {
        return Regex.Replace(Delegate, Subject, State);
    }

    internal static Tuple<bool,string> Replace(this Regex regex, RegexEvaluator evaluator, string input, ParserState State)
    {
        Match match;

        ArgumentNullException.ThrowIfNull(evaluator);

        match = regex.Match(input, 0);

        if (!match.Success)
        {
            return new Tuple<bool, string>(false, input);
        }
        else
        {
            StringBuilder sb = new();
            int prevat = 0;

            do
            {
                if (match.Index != prevat)
                    sb.Append(input, prevat, match.Index - prevat);

                prevat = match.Index + match.Length;

                sb.Append(evaluator(match, State));

                match = match.NextMatch();
            } while (match.Success);

            if (prevat < input.Length)
                sb.Append(input, prevat, input.Length - prevat);

            return new Tuple<bool, string>(true, sb.ToString());
        }
    }
}
