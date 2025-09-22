using System;
using System.Collections.Concurrent;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using WebSSH.Shared;
using Microsoft.AspNetCore.SignalR;

namespace WebSSH.Server
{
    public class ServerActiveSessionsModel
    {
        public ServerActiveSessionModel Connected(string sessionId, ActiveSessionModel activeSessionModel, ShellConfiguration shellConfiguration, Microsoft.AspNetCore.SignalR.IHubContext<WebSSH.Server.Hubs.ShellHub> hubContext)
        {
            SshClient sshClient = null;
            ShellStream shellStream = null;
            try
            {
                var timeOutMinutes = shellConfiguration.MaxIdleMinutes < 1 ? 1 : shellConfiguration.MaxIdleMinutes > 20 ? 20 : shellConfiguration.MaxIdleMinutes;
                var clientStoredSessionModel = activeSessionModel.StoredSessionModel;
                sshClient = new SshClient(clientStoredSessionModel.Host, clientStoredSessionModel.Port, clientStoredSessionModel.UserName, clientStoredSessionModel.PasswordDecryped);
                sshClient.ConnectionInfo.Timeout = TimeSpan.FromMinutes(timeOutMinutes);
                sshClient.Connect();
                shellStream = sshClient.CreateShellStream("Terminal", 80, 30, 800, 400, 1000);
                var outputQueue = new ConcurrentQueue<string>();

                var termModes = new System.Collections.Generic.Dictionary<Renci.SshNet.Common.TerminalModes, uint>
                {
                    { Renci.SshNet.Common.TerminalModes.ECHO, 1 },
                    { Renci.SshNet.Common.TerminalModes.ICANON, 1 },
                    { Renci.SshNet.Common.TerminalModes.ISIG, 1 }
                };

                // Use xterm-256color for better TUI compatibility (htop, vim, etc.)
                shellStream = sshClient.CreateShellStream(
                    "xterm-256color", // terminal type
                    120,               // columns (initial)
                    40,                // rows (initial)
                    800,               // width pixels (approx)
                    600,               // height pixels (approx)
                    4096,              // buffer size
                    termModes);

                // Ensure TERM is set (some shells may rely on it)
                // shellStream.WriteLine("export TERM=xterm-256color");

                var sessionModel = new ServerActiveSessionModel
                {
                    Status = "Connected",
                    UniqueKey = activeSessionModel.UniqueKey,
                    ShellStream = shellStream,
                    Client = sshClient,
                    StartSessionDate = DateTime.Now,
                    LastAccessSessionDate = DateTime.Now,
                    StoredSessionModel = clientStoredSessionModel,
                    OutputQueue = outputQueue,
                    SessionId = sessionId
                };

                // Non-blocking initial buffer read if any data already available
                if (shellStream.DataAvailable)
                {
                    try
                    {
                        var initial = shellStream.Read();
                        if (!string.IsNullOrEmpty(initial))
                        {
                            outputQueue.Enqueue(initial);
                        }
                    }
                    catch { }
                }

                shellStream.DataReceived += async (obj, e) =>
                {
                    try
                    {
                        var msg = Encoding.UTF8.GetString(e.Data);
                        outputQueue.Enqueue(msg);

                        if(outputQueue.Count > Constants.MaxinumQueueCount)
                        {
                            outputQueue.TryDequeue(out _);
                        }

                        // Broadcast real-time output to group listeners
                        try
                        {
                            await hubContext.Clients.Group(Hubs.ShellHub.BuildGroup(sessionId, sessionModel.UniqueKey)).SendAsync("ShellOutput", msg);
                        }
                        catch
                        {
                            // Ignore broadcast errors; queue still holds data.
                        }
                    }
                    catch (Exception ex)
                    {
                        outputQueue.Enqueue(ex.Message);
                    }
                };

                AddActiveSession(sessionModel);

                sshClient.ErrorOccurred += SshClient_ErrorOccurred;

                return sessionModel;
            }
            catch
            {
                shellStream?.Dispose();
                sshClient?.Dispose();

                throw;
            }
        }

        void SshClient_ErrorOccurred(object sender, ExceptionEventArgs e)
        {
            RemoveActiveSession((sender as ServerActiveSessionModel)?.UniqueKey ?? Guid.Empty);
        }

        public void RemoveActiveSession(Guid uniqueKey)
        {
            if (Sessions.TryRemove(uniqueKey, out var activeSession))
            {
                activeSession.Dispose();
            }
        }

        public ConcurrentDictionary<Guid, ServerActiveSessionModel> Sessions { get; set; } = new();

        void AddActiveSession(ServerActiveSessionModel session)
        {
            Sessions.TryAdd(session.UniqueKey, session);
        }
    }
}
