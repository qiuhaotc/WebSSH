using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Renci.SshNet;
using WebSSH.Shared;
using WebSSH.Server.Hubs;
using WebSSH.Server.Extensions;

namespace WebSSH.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class FileUploadController : ControllerBase
    {
        public ShellPool ShellPool { get; }
        public IHubContext<ShellHub> HubContext { get; }
        public ShellConfiguration ShellConfiguration { get; }
        public IMemoryCache MemoryCache { get; }

        public FileUploadController(ShellPool shellPool, IHubContext<ShellHub> hubContext, 
            ShellConfiguration shellConfiguration, IMemoryCache memoryCache)
        {
            ShellPool = shellPool;
            HubContext = hubContext;
            ShellConfiguration = shellConfiguration;
            MemoryCache = memoryCache;
        }

        [HttpPost]
        public async Task<ServerResponse<FileUploadResult>> UploadFiles(Guid uniqueId, List<IFormFile> files)
        {
            var response = new ServerResponse<FileUploadResult> { StausResult = StausResult.Successful };
            var groupName = "";
            
            try
            {
                var sessionId = HttpContext.Session.GetString(Constants.ClientSessionIdName);
                if (string.IsNullOrEmpty(sessionId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No active session";
                    return response;
                }

                groupName = ShellHub.BuildGroup(sessionId, uniqueId);

                if (!ShellPool.IsConnected(sessionId, uniqueId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "Shell not connected";
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", "Error: Shell not connected");
                    return response;
                }

                if (files == null || !files.Any())
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No files provided";
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", "Error: No files provided");
                    return response;
                }

                // Validate file count
                if (files.Count > ShellConfiguration.MaxFilesPerUpload)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = $"Too many files. Maximum {ShellConfiguration.MaxFilesPerUpload} files allowed per upload";
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", 
                        $"Error: Too many files. Maximum {ShellConfiguration.MaxFilesPerUpload} files allowed per upload");
                    return response;
                }

                // Validate file sizes
                var maxSizeBytes = ShellConfiguration.MaxFileSizeMB * 1024 * 1024;
                var oversizedFiles = files.Where(f => f.Length > maxSizeBytes).ToList();
                if (oversizedFiles.Any())
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = $"File(s) too large: {string.Join(", ", oversizedFiles.Select(f => f.FileName))}. Maximum size: {ShellConfiguration.MaxFileSizeMB}MB";
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", 
                        $"Error: File(s) too large: {string.Join(", ", oversizedFiles.Select(f => f.FileName))}. Maximum size: {ShellConfiguration.MaxFileSizeMB}MB");
                    return response;
                }

                // Check IP-based rate limiting with sliding expiration
                var clientIp = HttpContext.GetRealIpAddress();
                var cacheKey = $"upload_count_{clientIp}";

                var currentUploads = MemoryCache.GetOrCreate(cacheKey, entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromHours(1);
                    entry.Size = 1; // Required when SizeLimit is set
                    return 0;
                });

                if (currentUploads + files.Count > ShellConfiguration.MaxFilesPerHour)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = $"Upload limit exceeded. Maximum {ShellConfiguration.MaxFilesPerHour} files per hour allowed. Current: {currentUploads}";
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", 
                        $"Error: Upload limit exceeded. Maximum {ShellConfiguration.MaxFilesPerHour} files per hour allowed. You have uploaded {currentUploads} files this hour");
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
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", "Error: SSH client not available");
                    return response;
                }

                var result = new FileUploadResult();

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

                // Update the upload count in cache only if upload was successful
                var successfulUploads = result.UploadedFiles.Count(f => f.Success);
                if (successfulUploads > 0)
                {
                    MemoryCache.Set(cacheKey, currentUploads + successfulUploads, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromHours(1),
                        Size = 1 // Required when SizeLimit is set
                    });
                }

                result.TotalFiles = files.Count;
                result.SuccessfulUploads = successfulUploads;
                response.Response = result;

                await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", $"Upload completed: {result.SuccessfulUploads}/{result.TotalFiles} files uploaded successfully");
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;
                
                if (!string.IsNullOrEmpty(groupName))
                {
                    await HubContext.Clients.Group(groupName).SendAsync("FileUploadStatus", $"Upload failed: {ex.Message}");
                }
            }

            return response;
        }
    }
}