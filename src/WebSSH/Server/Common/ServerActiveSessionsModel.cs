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

                string result = null;
                while ((result = sessionModel.ShellStream.ReadLine(TimeSpan.FromSeconds(0.3))) != null)
                {
                    outputQueue.Enqueue(result + Constants.NewLineForShell);
                }

                outputQueue.Enqueue(sessionModel.ShellStream.Read());

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
