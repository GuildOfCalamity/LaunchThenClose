using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LaunchThenClose
{
    internal static class CommandLineHelper
    {
        // We'll use this prefix for all our switches.
        // A single tick/dash is reserved for passing negative or hyphenated values.
        public const string PREAMBLE = "--";

        #region [CommandLine Helpers]
        /// <summary>
        /// Convenience method to return only the first "--{tagName}" value or empty.
        /// </summary>
        public static string GetFirstThisValue(string tagName, string[] args)
        {
            foreach (var value in GetThisValues(tagName, args))
                return value;

            return string.Empty;
        }
        public static List<string> GetThisValues(string tagName, string[] args)
        {
            if (args == null)
                return new List<string>();

            var matches = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == $"{PREAMBLE}{tagName}" && i + 1 < args.Length)
                {
                    matches.Add(args[i + 1]);
                    i++; // don't re-parse it
                }
            }
            return matches;
        }

        /// <summary>
        /// Convenience method to return only the first "--task" value or empty.
        /// </summary>
        public static string GetFirstTaskValue(string[] args)
        {
            foreach (var value in GetTaskValues(args))
                return value;

            return string.Empty;
        }
        public static List<string> GetTaskValues(string[] args)
        {
            if (args == null)
                return new List<string>();

            var matches = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == $"{PREAMBLE}task" || args[i] == $"{PREAMBLE}Task") && i + 1 < args.Length)
                {
                    matches.Add(args[i + 1]);
                    i++; // don't re-parse it
                }
            }
            return matches;
        }

        /// <summary>
        /// Scans the args for every occurrence of "--help" and returns
        /// the argument immediately following it as a search string.
        /// </summary>
        /// <param name="args">The array of command-line arguments.</param>
        /// <returns>True if help is found, False otherwise.</returns>
        public static bool GetHelpFlag(string[] args)
        {
            if (args == null)
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == $"{PREAMBLE}help" || args[i] == $"{PREAMBLE}?") && i < args.Length)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Scans the args for every occurrence of "--debug" and returns
        /// the argument immediately following it as a search string.
        /// </summary>
        /// <param name="args">The array of command-line arguments.</param>
        /// <returns>True if debug is found, False otherwise.</returns>
        public static bool GetDebugFlag(string[] args)
        {
            if (args == null)
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == $"{PREAMBLE}debug" && i < args.Length)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Splits the raw command line into tokens.<br/>
        /// A token is either:<br/>
        ///   • a quoted string including its quotes (e.g. "hello there")<br/>
        ///   • or a run of non-space characters.<br/>
        /// </summary>
        public static IReadOnlyList<string> TokenizeAndPreserveQuotes(string rawCommandLine)
        {
            if (string.IsNullOrEmpty(rawCommandLine))
                return Array.Empty<string>();

            // Regex: match either "…?" or any sequence of non-space chars
            var matches = Regex.Matches(rawCommandLine, @"(""[^""]*""|\S+)")
                                .Cast<Match>()
                                .Select(m => m.Value)
                                .ToList();
            return matches;
        }

        /// <summary>
        /// Finds every argument after "--match", preserving its surrounding quotes.
        /// </summary>
        public static List<string> GetMatchValuesWithQuotes()
        {
            // get the raw CL (including program path and all switches)
            string raw = Environment.CommandLine;

            var tokens = TokenizeAndPreserveQuotes(raw);
            var results = new List<string>();

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Equals($"{PREAMBLE}match", StringComparison.Ordinal))
                {
                    // next token still has its quotes if user typed "…"
                    results.Add(tokens[i + 1]);
                    i++;  // skip next so we don’t double‐count
                }
            }

            return results;
        }
        #endregion
    }
}
