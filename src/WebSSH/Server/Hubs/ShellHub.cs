using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using WebSSH.Shared;

namespace WebSSH.Server.Hubs
{
    [Authorize]
    public class ShellHub : Hub
    {
        public ShellPool ShellPool { get; }

        public ShellHub(ShellPool shellPool)
        {
            ShellPool = shellPool;
        }

        public static string BuildGroup(string sessionId, Guid uniqueId) => $"{sessionId}:{uniqueId}";

        public async Task JoinShell(Guid uniqueId)
        {
            var httpContext = Context.GetHttpContext();
            var sessionId = httpContext?.Session.GetString(Constants.ClientSessionIdName);
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new HubException("No active session");
            }

            bool connected;
            try
            {
                connected = ShellPool.IsConnected(sessionId, uniqueId);
            }
            catch (Exception ex)
            {
                throw new HubException(ex.Message);
            }

            if (!connected)
            {
                throw new HubException("Shell not connected");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroup(sessionId, uniqueId));

            // Flush existing queued output to caller so terminal has initial backlog.
            try
            {
                var existing = ShellPool.GetShellOutput(sessionId, uniqueId);
                if (!string.IsNullOrEmpty(existing.Output))
                {
                    await Clients.Caller.SendAsync("ShellOutput", existing.Output);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ShellOutput", $"Error reading initial output: {ex.Message}");
            }

            // Mark session as active - stop queuing, send data directly via SignalR
            ShellPool.SetActiveClient(sessionId, uniqueId, true);
        }

        public async Task LeaveShell(Guid uniqueId)
        {
            var httpContext = Context.GetHttpContext();
            var sessionId = httpContext?.Session.GetString(Constants.ClientSessionIdName);
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            // Mark session as inactive - resume queuing data
            ShellPool.SetActiveClient(sessionId, uniqueId, false);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildGroup(sessionId, uniqueId));
        }

        public async Task SendInput(Guid uniqueId, string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var httpContext = Context.GetHttpContext();
            var sessionId = httpContext?.Session.GetString(Constants.ClientSessionIdName);
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new HubException("No active session");
            }
            try
            {
                ShellPool.SendInputRaw(sessionId, uniqueId, data);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ShellOutput", $"Error input: {ex.Message}");
            }
        }

        public async Task NotifyFileUploadStart(Guid uniqueId, int fileCount)
        {
            var httpContext = Context.GetHttpContext();
            var sessionId = httpContext?.Session.GetString(Constants.ClientSessionIdName);
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new HubException("No active session");
            }

            await Clients.Group(BuildGroup(sessionId, uniqueId)).SendAsync("FileUploadStatus", $"Starting upload of {fileCount} file(s)...");
        }

        public async Task NotifyFileDownloadStart(Guid uniqueId, int fileCount)
        {
            var httpContext = Context.GetHttpContext();
            var sessionId = httpContext?.Session.GetString(Constants.ClientSessionIdName);
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new HubException("No active session");
            }

            await Clients.Group(BuildGroup(sessionId, uniqueId)).SendAsync("FileDownloadStatus", $"Starting download of {fileCount} file(s)...");
        }
    }
}
