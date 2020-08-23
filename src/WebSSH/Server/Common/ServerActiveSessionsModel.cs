using System;
using System.Collections.Concurrent;
using Renci.SshNet;
using WebSSH.Shared;

namespace WebSSH.Server
{
    public class ServerActiveSessionsModel
    {
        public ServerActiveSessionModel Connected(ActiveSessionModel activeSessionModel)
        {
            SshClient sshClient = null;
            ShellStream shellStream = null;
            try
            {
                var clientStoredSessionModel = activeSessionModel.StoredSessionModel;
                sshClient = new SshClient(clientStoredSessionModel.Host, clientStoredSessionModel.Port, clientStoredSessionModel.UserName, clientStoredSessionModel.PasswordDecryped);
                sshClient.Connect();
                shellStream = sshClient.CreateShellStream("Terminal", 80, 30, 800, 400, 1000);

                var sessionModel = new ServerActiveSessionModel
                {
                    Status = "Connected",
                    UniqueKey = activeSessionModel.UniqueKey,
                    ShellStream = shellStream,
                    Client = sshClient,
                    StartSessionDate = DateTime.Now,
                    LastAccessSessionDate = DateTime.Now,
                    StoredSessionModel = clientStoredSessionModel
                };

                AddActiveSession(sessionModel);

                sshClient.ErrorOccurred += SshClient_ErrorOccurred;

                return sessionModel;
            }
            catch (Exception ex)
            {
                shellStream?.Dispose();
                sshClient?.Dispose();

                throw ex;
            }
        }

        void SshClient_ErrorOccurred(object sender, Renci.SshNet.Common.ExceptionEventArgs e)
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

        public ConcurrentDictionary<Guid, ServerActiveSessionModel> Sessions { get; set; } = new ConcurrentDictionary<Guid, ServerActiveSessionModel>();

        void AddActiveSession(ServerActiveSessionModel session)
        {
            Sessions.TryAdd(session.UniqueKey, session);
        }
    }
}
