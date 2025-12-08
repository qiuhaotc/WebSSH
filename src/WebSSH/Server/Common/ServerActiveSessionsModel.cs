using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Renci.SshNet;
using Renci.SshNet.Common;
using WebSSH.Shared;

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
                var clientStoredSessionModel = activeSessionModel.StoredSessionModel;
                
                // Create SSH client based on authentication type
                if (clientStoredSessionModel.AuthenticationType == AuthenticationType.PrivateKey)
                {
                    // Private key authentication
                    var privateKeyContent = clientStoredSessionModel.PrivateKeyDecrypted;
                    if (string.IsNullOrEmpty(privateKeyContent))
                    {
                        throw new Exception("Private key is empty");
                    }

                    // Create PrivateKeyFile from the key content
                    // Note: PrivateKeyFile reads the key data during construction, so we can dispose the stream afterwards
                    PrivateKeyFile privateKeyFile;
                    var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(privateKeyContent));
                    try
                    {
                        if (!string.IsNullOrEmpty(clientStoredSessionModel.PrivateKeyPassphraseDecrypted))
                        {
                            // Private key with passphrase
                            privateKeyFile = new PrivateKeyFile(stream, clientStoredSessionModel.PrivateKeyPassphraseDecrypted);
                        }
                        else
                        {
                            // Private key without passphrase
                            privateKeyFile = new PrivateKeyFile(stream);
                        }
                    }
                    finally
                    {
                        stream.Dispose();
                    }

                    var keyAuth = new PrivateKeyAuthenticationMethod(clientStoredSessionModel.UserName, privateKeyFile);
                    var connectionInfo = new ConnectionInfo(clientStoredSessionModel.Host, clientStoredSessionModel.Port, clientStoredSessionModel.UserName, keyAuth);
                    sshClient = new SshClient(connectionInfo);
                }
                else
                {
                    // Password authentication (default)
                    sshClient = new SshClient(clientStoredSessionModel.Host, clientStoredSessionModel.Port, clientStoredSessionModel.UserName, clientStoredSessionModel.PasswordDecryped);
                }
                
                sshClient.Connect();
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

                // Ensure TERM is set (some shells may rely on it); use \n instead of WriteLine to avoid extra CR in some shells
                shellStream.Write("export TERM=xterm-256color\n");

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
                        
                        // If session has active client, send data directly via SignalR
                        if (sessionModel.HasActiveClient)
                        {
                            try
                            {
                                await hubContext.Clients.Group(Hubs.ShellHub.BuildGroup(sessionId, sessionModel.UniqueKey)).SendAsync("ShellOutput", msg);
                            }
                            catch
                            {
                                // Broadcast failed, client may have disconnected
                                // Queue the data for later retrieval
                                outputQueue.Enqueue(msg);
                                if (outputQueue.Count > Constants.MaxinumQueueCount)
                                {
                                    outputQueue.TryDequeue(out _);
                                }
                            }
                        }
                        else
                        {
                            // No active client, save to queue
                            outputQueue.Enqueue(msg);
                            if (outputQueue.Count > Constants.MaxinumQueueCount)
                            {
                                outputQueue.TryDequeue(out _);
                            }
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
