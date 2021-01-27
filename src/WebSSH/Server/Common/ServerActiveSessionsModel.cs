using System;
using System.Collections.Concurrent;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using WebSSH.Shared;

namespace WebSSH.Server
{
    public class ServerActiveSessionsModel
    {
        public ServerActiveSessionModel Connected(ActiveSessionModel activeSessionModel, ShellConfiguration shellConfiguration)
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
                    OutputQueue = outputQueue
                };

                string result = null;
                while ((result = sessionModel.ShellStream.ReadLine(TimeSpan.FromSeconds(0.3))) != null)
                {
                    outputQueue.Enqueue(result + Constants.NewLineForShell);
                }

                outputQueue.Enqueue(sessionModel.ShellStream.Read());

                shellStream.DataReceived += (obj, e) =>
                {
                    try
                    {
                        outputQueue.Enqueue(Encoding.UTF8.GetString(e.Data));

                        if(outputQueue.Count > Constants.MaxinumQueueCount)
                        {
                            outputQueue.TryDequeue(out _);
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
