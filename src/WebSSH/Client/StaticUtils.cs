using System;
using System.Collections.Generic;
using System.Linq;
using WebSSH.Shared;

namespace WebSSH.Client
{
    public static class StaticUtils
    {
        public static ActiveSessionsModel ActiveSessionsModel { get; set; } = new();

        public static ClientStoredSessionModel ClientStoredSessionModel { get; set; }

        public static void AddOutputString(Guid uniqueKey, string message)
        {
            var messages = message?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>();

            if (!OutputStrings.TryGetValue(uniqueKey, out var outputStrings))
            {
                outputStrings = new List<string>();
                OutputStrings.Add(uniqueKey, outputStrings);
            }

            outputStrings.AddRange(messages);
        }

        public static string[] GetOutputString(Guid uniqueKey, ref int currentLine)
        {
            var outputs = OutputStrings.TryGetValue(uniqueKey, out var outputStrings) ? outputStrings : new List<string>();

            var lines = outputs.Skip(currentLine).ToArray();
            currentLine += lines.Length;

            return lines;
        }

        public static void ClearOutputString(Guid uniqueKey)
        {
            if (OutputStrings.ContainsKey(uniqueKey))
            {
                OutputStrings.Remove(uniqueKey);
            }
        }

        static Dictionary<Guid, List<string>> OutputStrings { get; set; } = new();
    }
}
