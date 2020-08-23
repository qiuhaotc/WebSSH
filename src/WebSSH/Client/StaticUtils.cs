using System;
using System.Collections.Generic;
using WebSSH.Shared;

namespace WebSSH.Client
{
    public static class StaticUtils
    {
        public static ActiveSessionsModel ActiveSessionsModel { get; set; } = new ActiveSessionsModel();

        public static ClientStoredSessionModel ClientStoredSessionModel { get; set; }

        public static void AddOutputString(Guid uniqueKey, string message)
        {
            if (!OutputStrings.TryGetValue(uniqueKey, out var outputString))
            {
                OutputStrings.Add(uniqueKey, message);
            }
            else
            {
                outputString += Environment.NewLine + message;
                OutputStrings[uniqueKey] = outputString;
            }
        }

        public static string GetOutputString(Guid uniqueKey)
        {
            return OutputStrings.TryGetValue(uniqueKey, out var outputString) ? outputString : string.Empty;
        }

        public static void ClearOutputString(Guid uniqueKey)
        {
            if (OutputStrings.ContainsKey(uniqueKey))
            {
                OutputStrings.Remove(uniqueKey);
            }
        }

        static Dictionary<Guid, string> OutputStrings { get; set; } = new Dictionary<Guid, string>();
    }
}
