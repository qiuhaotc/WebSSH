using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSSH.Shared;

namespace WebSSH.Server
{
    public class ShellPool
    {
        public ShellPool(ShellConfiguration shellConfiguration)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    // Do check per 2 minutes
                    Thread.Sleep(120 * 1000);

                    foreach (var shell in ShellPoolDictionary.ToArray())
                    {
                        // Haven't access more than Max Idle Minutes or Connected Is False
                        var expiredShells = shell.Value.Sessions.Where(u => u.Value.LastAccessSessionDate < DateTime.Now.AddMinutes(-shellConfiguration.MaxIdleMinutes) || !u.Value.Client.IsConnected).ToArray();

                        foreach (var expiredShell in expiredShells)
                        {
                            shell.Value.RemoveActiveSession(expiredShell.Key);
                        }

                        if (shell.Value.Sessions.Count == 0)
                        {
                            // All sessions are expired, Remove session key
                            ShellPoolDictionary.TryRemove(shell.Key, out _);
                        }
                    }
                }
            });

            ShellConfiguration = shellConfiguration;
        }

        static ConcurrentDictionary<string, ServerActiveSessionsModel> ShellPoolDictionary { get; set; } = new();
        public ShellConfiguration ShellConfiguration { get; }

        public void AddShellToPool(string sessionId, ActiveSessionModel activeSessionModel)
        {
            if (!ShellPoolDictionary.TryGetValue(sessionId, out var sessionsModel))
            {
                sessionsModel = new ServerActiveSessionsModel();
                ShellPoolDictionary.TryAdd(sessionId, sessionsModel);
            }

            sessionsModel.Connected(activeSessionModel, ShellConfiguration);
        }

        const string ControlCommand = "ctrl + ";

        public void RunShellCommand(string sessionId, Guid uniqueId, string command)
        {
            if (ShellPoolDictionary.TryGetValue(sessionId, out var serverActiveSessionsModel) && serverActiveSessionsModel.Sessions.TryGetValue(uniqueId, out var serverActiveSessionModel))
            {
                if (command?.StartsWith(ControlCommand) ?? false)
                {
                    var lastWord = command[ControlCommand.Length..].ToLower();
                    if (lastWord.Length == 1 && lastWord[0] >= 'a' && lastWord[0] <= 'z')
                    {
                        command = ((char)(lastWord[0] - 96)).ToString();
                    }
                }

                serverActiveSessionModel.LastAccessSessionDate = DateTime.Now;
                serverActiveSessionModel.ShellStream.WriteLine(command);
            }
            else
            {
                throw new Exception("No available shell connected");
            }
        }

        public ServerOutput GetShellOutput(string sessionId, Guid uniqueId)
        {
            if (ShellPoolDictionary.TryGetValue(sessionId, out var serverActiveSessionsModel) && serverActiveSessionsModel.Sessions.TryGetValue(uniqueId, out var serverActiveSessionModel))
            {
                var totalLines = 0;
                StringBuilder outputStringBuilder = null;
                while (totalLines < Constants.MaxinumLines && serverActiveSessionModel.OutputQueue.TryDequeue(out var output))
                {
                    totalLines++;
                    outputStringBuilder = outputStringBuilder ?? new StringBuilder();
                    outputStringBuilder.Append(output);
                }

                return new ServerOutput { Output = outputStringBuilder?.ToString() ?? string.Empty, Lines = totalLines };
            }
            else
            {
                throw new Exception("No available shell connected");
            }
        }

        public bool IsConnected(string sessionId, Guid uniqueId)
        {
            if (ShellPoolDictionary.TryGetValue(sessionId, out var serverActiveSessionsModel) && serverActiveSessionsModel.Sessions.TryGetValue(uniqueId, out var serverActiveSessionModel))
            {
                return serverActiveSessionModel.Client.IsConnected;
            }
            else
            {
                throw new Exception("No available shell connected");
            }
        }

        public bool Disconnected(string sessionId, Guid uniqueId)
        {
            if (ShellPoolDictionary.TryGetValue(sessionId, out var serverActiveSessionsModel))
            {
                serverActiveSessionsModel.RemoveActiveSession(uniqueId);

                return true;
            }
            else
            {
                throw new Exception("No available shell connected");
            }
        }

        public List<ActiveSessionModel> GetConnectedSessionsAndClearNotConnected(string sessionId)
        {
            if (ShellPoolDictionary.TryGetValue(sessionId, out var serverActiveSessionsModel))
            {
                var result = new List<ActiveSessionModel>();

                var sessions = serverActiveSessionsModel.Sessions.ToArray();
                foreach (var session in sessions)
                {
                    if (!session.Value.Client.IsConnected)
                    {
                        serverActiveSessionsModel.RemoveActiveSession(session.Key);
                    }
                    else
                    {
                        result.Add(new ActiveSessionModel
                        {
                            StartSessionDate = session.Value.StartSessionDate,
                            Status = session.Value.Status,
                            StoredSessionModel = session.Value.StoredSessionModel,
                            UniqueKey = session.Key
                        });
                    }
                }

                return result;
            }
            else
            {
                return new List<ActiveSessionModel>();
            }
        }
    }
}
