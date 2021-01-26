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
            var messages = message.SplitByLines();

            if (!OutputStrings.TryGetValue(uniqueKey, out var outputStrings))
            {
                outputStrings = new List<string>();
                OutputStrings.Add(uniqueKey, outputStrings);
            }

            outputStrings.AddRange(messages);

            if (outputStrings.Count > Constants.MaxinumCachedLines)
            {
                outputStrings.RemoveRange(0, outputStrings.Count - Constants.MaxinumCachedLines);
            }
        }

        public static string[] GetOutputString(Guid uniqueKey, ref int currentLine)
        {
            var outputs = OutputStrings.TryGetValue(uniqueKey, out var outputStrings) ? outputStrings : new List<string>();

            var lines = outputs.Skip(currentLine).ToArray();
            currentLine += lines.Length;

            return lines;
        }

        public static (string CommandStr, bool Successful) GetPreCommand(Guid uniqueKey, ref int currentCommandLine)
        {
            if (Commands.TryGetValue(uniqueKey, out var commands) && commands.Count > 0 && currentCommandLine > 1)
            {
                currentCommandLine--;
                return (commands.Skip(currentCommandLine - 1).FirstOrDefault(), true);
            }

            return (null, false);
        }

        public static (string CommandStr, bool Successful) GetNextCommand(Guid uniqueKey, ref int currentCommandLine)
        {
            if (Commands.TryGetValue(uniqueKey, out var commands) && commands.Count > currentCommandLine && currentCommandLine > 0)
            {
                currentCommandLine++;
                return (commands.Skip(currentCommandLine - 1).FirstOrDefault(), true);
            }

            return (null, false);
        }

        public static void RecordCommands(Guid uniqueKey, string commandStr, ref int currentCommandLine)
        {
            if (!Commands.TryGetValue(uniqueKey, out var commands))
            {
                commands = new List<string>();
                Commands.Add(uniqueKey, commands);
            }

            var commandStrArray = commandStr.SplitByLines();

            commands.AddRange(commandStrArray);

            if (commands.Count > Constants.MaxinumLines)
            {
                commands.RemoveRange(0, commands.Count - Constants.MaxinumLines);
            }

            currentCommandLine = commands.Count + 1;
        }

        public static int InitCurrentCommandLine(Guid uniqueKey)
        {
            if (!Commands.TryGetValue(uniqueKey, out var commands))
            {
                return 1;
            }

            return commands.Count + 1;
        }

        public static void Clear(Guid uniqueKey)
        {
            if (OutputStrings.ContainsKey(uniqueKey))
            {
                OutputStrings.Remove(uniqueKey);
            }

            if (Commands.ContainsKey(uniqueKey))
            {
                Commands.Remove(uniqueKey);
            }
        }

        static Dictionary<Guid, List<string>> OutputStrings { get; set; } = new();
        static Dictionary<Guid, List<string>> Commands { get; set; } = new();

        static string[] SplitByLines(this string str)
        {
            return str?.Replace("\r\n", "\n").Split("\n") ?? Array.Empty<string>();
        }
    }
}
