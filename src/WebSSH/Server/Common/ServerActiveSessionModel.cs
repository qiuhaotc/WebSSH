using System;
using System.Collections.Concurrent;
using Renci.SshNet;
using WebSSH.Shared;

namespace WebSSH.Server
{
    public class ServerActiveSessionModel : IDisposable
    {
        public ClientStoredSessionModel StoredSessionModel { get; set; }
        public Guid UniqueKey { get; set; } = Guid.NewGuid();
        public string Status { get; set; }
        public DateTime StartSessionDate { get; set; }
        public DateTime LastAccessSessionDate { get; set; }
        public SshClient Client { get; set; }
        public ShellStream ShellStream { get; set; }
        public ConcurrentQueue<string> OutputQueue { get; set; }

        public void Dispose()
        {
            try
            {
                ShellStream?.Dispose();
                ShellStream = null;
                Client?.Dispose();
                Client = null;
            }
            catch
            {
            }
        }
    }
}
