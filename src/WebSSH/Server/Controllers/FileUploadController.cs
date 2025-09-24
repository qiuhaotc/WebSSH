using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Renci.SshNet;
using WebSSH.Shared;
using WebSSH.Server.Hubs;

namespace WebSSH.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class FileUploadController : ControllerBase
    {
        public ShellPool ShellPool { get; }
        public IHubContext<ShellHub> HubContext { get; }

        public FileUploadController(ShellPool shellPool, IHubContext<ShellHub> hubContext)
        {
            ShellPool = shellPool;
            HubContext = hubContext;
        }

        [HttpPost]
        public async Task<ServerResponse<FileUploadResult>> UploadFiles(Guid uniqueId, List<IFormFile> files)
        {
            var response = new ServerResponse<FileUploadResult> { StausResult = StausResult.Successful };
            
            try
            {
                var sessionId = HttpContext.Session.GetString(Constants.ClientSessionIdName);
                if (string.IsNullOrEmpty(sessionId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No active session";
                    return response;
                }

                if (!ShellPool.IsConnected(sessionId, uniqueId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "Shell not connected";
                    return response;
                }

                if (files == null || !files.Any())
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No files provided";
                    return response;
                }

                // Get the remote path from form data, default to root if not provided
                var remotePath = HttpContext.Request.Form["remotePath"].FirstOrDefault() ?? "/";

                // Get the SSH client for the session
                var sshClient = ShellPool.GetSshClient(sessionId, uniqueId);
                if (sshClient == null)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "SSH client not available";
                    return response;
                }

                var result = new FileUploadResult();
                var groupName = ShellHub.BuildGroup(sessionId, uniqueId);

                using (var sftpClient = new SftpClient(sshClient.ConnectionInfo))
                {
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", "Connecting to SFTP...");
                    sftpClient.Connect();

                    foreach (var file in files)
                    {
                        try
                        {
                            await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", $"Uploading {file.FileName}...");

                            var remoteFileName = Path.Combine(remotePath, file.FileName).Replace("\\", "/");
                            
                            using (var fileStream = file.OpenReadStream())
                            {
                                sftpClient.UploadFile(fileStream, remoteFileName, true);
                            }

                            result.UploadedFiles.Add(new UploadedFileInfo
                            {
                                FileName = file.FileName,
                                Size = file.Length,
                                RemotePath = remoteFileName,
                                Success = true
                            });

                            await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", $"✓ {file.FileName} uploaded successfully");
                        }
                        catch (Exception ex)
                        {
                            result.UploadedFiles.Add(new UploadedFileInfo
                            {
                                FileName = file.FileName,
                                Size = file.Length,
                                RemotePath = Path.Combine(remotePath, file.FileName).Replace("\\", "/"),
                                Success = false,
                                ErrorMessage = ex.Message
                            });

                            await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", $"✗ Failed to upload {file.FileName}: {ex.Message}");
                        }
                    }
                }

                result.TotalFiles = files.Count;
                result.SuccessfulUploads = result.UploadedFiles.Count(f => f.Success);
                response.Response = result;

                await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", $"Upload completed: {result.SuccessfulUploads}/{result.TotalFiles} files uploaded successfully");
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;
                
                var sessionId = HttpContext.Session.GetString(Constants.ClientSessionIdName);
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var groupName = ShellHub.BuildGroup(sessionId, uniqueId);
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", $"Upload failed: {ex.Message}");
                }
            }

            return response;
        }
    }
}