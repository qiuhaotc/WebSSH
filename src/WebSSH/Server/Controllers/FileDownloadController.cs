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
using WebSSH.Server.Extensions;
using WebSSH.Server.Hubs;
using WebSSH.Shared;

namespace WebSSH.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class FileDownloadController : ControllerBase
    {
        public ShellPool ShellPool { get; }
        public IHubContext<ShellHub> HubContext { get; }
        public ShellConfiguration ShellConfiguration { get; }
        public IMemoryCache MemoryCache { get; }

        public FileDownloadController(ShellPool shellPool, IHubContext<ShellHub> hubContext,
            ShellConfiguration shellConfiguration, IMemoryCache memoryCache)
        {
            ShellPool = shellPool;
            HubContext = hubContext;
            ShellConfiguration = shellConfiguration;
            MemoryCache = memoryCache;
        }

        [HttpPost]
        public async Task<IActionResult> DownloadFiles(Guid uniqueId, [FromBody] FileDownloadRequest request)
        {
            var response = new ServerResponse<FileDownloadResult> { StausResult = StausResult.Successful };
            var groupName = "";

            try
            {
                var sessionId = HttpContext.Session.GetString(Constants.ClientSessionIdName);
                if (string.IsNullOrEmpty(sessionId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No active session";
                    return BadRequest(response);
                }

                groupName = ShellHub.BuildGroup(sessionId, uniqueId);

                if (!ShellPool.IsConnected(sessionId, uniqueId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "Shell not connected";
                    await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", "Error: Shell not connected");
                    return BadRequest(response);
                }

                if (request?.FilePaths == null || !request.FilePaths.Any())
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No files specified for download";
                    await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", "Error: No files specified for download");
                    return BadRequest(response);
                }

                // Validate file count
                if (request.FilePaths.Count > ShellConfiguration.MaxFilesPerDownload)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = $"Too many files. Maximum {ShellConfiguration.MaxFilesPerDownload} files allowed per download";
                    await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus",
                        $"Error: Too many files. Maximum {ShellConfiguration.MaxFilesPerDownload} files allowed per download");
                    return BadRequest(response);
                }

                // Check IP-based rate limiting with sliding expiration
                var clientIp = HttpContext.GetRealIpAddress();
                var cacheKey = $"download_count_{clientIp}";

                var currentDownloads = MemoryCache.GetOrCreate(cacheKey, entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromHours(1);
                    entry.Size = 1; // Required when SizeLimit is set
                    return 0;
                });

                if (currentDownloads + request.FilePaths.Count > ShellConfiguration.MaxDownloadsPerHour)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = $"Download limit exceeded. Maximum {ShellConfiguration.MaxDownloadsPerHour} files per hour allowed. Current: {currentDownloads}";
                    await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus",
                        $"Error: Download limit exceeded. Maximum {ShellConfiguration.MaxDownloadsPerHour} files per hour allowed. You have downloaded {currentDownloads} files this hour");
                    return BadRequest(response);
                }

                // Get the SSH client for the session
                var sshClient = ShellPool.GetSshClient(sessionId, uniqueId);
                if (sshClient == null)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "SSH client not available";
                    await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", "Error: SSH client not available");
                    return BadRequest(response);
                }

                var result = new FileDownloadResult();
                var zipFileName = $"download_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
                var tempZipPath = Path.Combine(Path.GetTempPath(), zipFileName);
                var downloadStreams = new List<(string fileName, byte[] content)>();

                using (var sftpClient = new SftpClient(sshClient.ConnectionInfo))
                {
                    await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", "Connecting to SFTP...");
                    sftpClient.Connect();

                    long totalSize = 0;
                    var maxSizeBytes = ShellConfiguration.MaxDownloadSizeMB * 1024 * 1024;

                    foreach (var filePath in request.FilePaths)
                    {
                        try
                        {
                            await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"Downloading {Path.GetFileName(filePath)}...");

                            if (!sftpClient.Exists(filePath))
                            {
                                result.DownloadedFiles.Add(new DownloadedFileInfo
                                {
                                    FileName = Path.GetFileName(filePath),
                                    Size = 0,
                                    RemotePath = filePath,
                                    Success = false,
                                    ErrorMessage = "File not found"
                                });

                                await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"✗ File not found: {Path.GetFileName(filePath)}");
                                continue;
                            }

                            var fileInfo = sftpClient.GetAttributes(filePath);
                            if (totalSize + fileInfo.Size > maxSizeBytes)
                            {
                                result.DownloadedFiles.Add(new DownloadedFileInfo
                                {
                                    FileName = Path.GetFileName(filePath),
                                    Size = fileInfo.Size,
                                    RemotePath = filePath,
                                    Success = false,
                                    ErrorMessage = $"Combined download size would exceed {ShellConfiguration.MaxDownloadSizeMB}MB limit"
                                });

                                await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"✗ {Path.GetFileName(filePath)} skipped: Size limit exceeded");
                                continue;
                            }

                            using (var memoryStream = new MemoryStream())
                            {
                                sftpClient.DownloadFile(filePath, memoryStream);
                                var fileContent = memoryStream.ToArray();
                                downloadStreams.Add((Path.GetFileName(filePath), fileContent));
                                totalSize += fileContent.Length;
                            }

                            result.DownloadedFiles.Add(new DownloadedFileInfo
                            {
                                FileName = Path.GetFileName(filePath),
                                Size = fileInfo.Size,
                                RemotePath = filePath,
                                Success = true
                            });

                            await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"✓ {Path.GetFileName(filePath)} downloaded successfully");
                        }
                        catch (Exception ex)
                        {
                            result.DownloadedFiles.Add(new DownloadedFileInfo
                            {
                                FileName = Path.GetFileName(filePath),
                                Size = 0,
                                RemotePath = filePath,
                                Success = false,
                                ErrorMessage = ex.Message
                            });

                            await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"✗ Failed to download {Path.GetFileName(filePath)}: {ex.Message}");
                        }
                    }
                }

                // Update the download count in cache only if downloads were successful
                var successfulDownloads = result.DownloadedFiles.Count(f => f.Success);
                if (successfulDownloads > 0)
                {
                    MemoryCache.Set(cacheKey, currentDownloads + successfulDownloads, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromHours(1),
                        Size = 1 // Required when SizeLimit is set
                    });

                    // Create ZIP file if multiple files or return single file
                    if (downloadStreams.Count == 1)
                    {
                        var singleFile = downloadStreams.First();
                        await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"Download ready: {singleFile.fileName}");

                        return File(singleFile.content, "application/octet-stream", singleFile.fileName);
                    }
                    else if (downloadStreams.Count > 1)
                    {
                        // Create ZIP archive
                        using (var zipStream = new MemoryStream())
                        {
                            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
                            {
                                foreach (var (fileName, content) in downloadStreams)
                                {
                                    var entry = archive.CreateEntry(fileName);
                                    using (var entryStream = entry.Open())
                                    {
                                        await entryStream.WriteAsync(content, 0, content.Length);
                                    }
                                }
                            }

                            await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"Download ready: {downloadStreams.Count} files in ZIP archive");

                            return File(zipStream.ToArray(), "application/zip", zipFileName);
                        }
                    }
                }

                result.TotalFiles = request.FilePaths.Count;
                result.SuccessfulDownloads = successfulDownloads;

                await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"Download completed: {result.SuccessfulDownloads}/{result.TotalFiles} files downloaded successfully");

                if (successfulDownloads == 0)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No files were successfully downloaded";
                    response.Response = result;
                    return BadRequest(response);
                }

                response.Response = result;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;

                if (!string.IsNullOrEmpty(groupName))
                {
                    await HubContext.Clients.Group(groupName).SendAsync("FileDownloadStatus", $"Download failed: {ex.Message}");
                }

                return StatusCode(500, response);
            }
        }

        [HttpGet]
        public IActionResult ListFiles(Guid uniqueId, string path = "/")
        {
            var response = new ServerResponse<List<RemoteFileInfo>> { StausResult = StausResult.Successful };

            try
            {
                var sessionId = HttpContext.Session.GetString(Constants.ClientSessionIdName);
                if (string.IsNullOrEmpty(sessionId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "No active session";
                    return BadRequest(response);
                }

                if (!ShellPool.IsConnected(sessionId, uniqueId))
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "Shell not connected";
                    return BadRequest(response);
                }

                var sshClient = ShellPool.GetSshClient(sessionId, uniqueId);
                if (sshClient == null)
                {
                    response.StausResult = StausResult.Failed;
                    response.ExtraMessage = "SSH client not available";
                    return BadRequest(response);
                }

                var files = new List<RemoteFileInfo>();

                using (var sftpClient = new SftpClient(sshClient.ConnectionInfo))
                {
                    sftpClient.Connect();

                    var sftpFiles = sftpClient.ListDirectory(path);

                    // Add parent directory entry if not at root
                    if (path != "/" && !string.IsNullOrEmpty(path.Trim('/')))
                    {
                        files.Add(new RemoteFileInfo
                        {
                            Name = "..",
                            FullPath = Path.GetDirectoryName(path.TrimEnd('/'))?.Replace('\\', '/') ?? "/",
                            IsDirectory = true,
                            Size = 0,
                            LastModified = DateTime.Now
                        });
                    }

                    foreach (var file in sftpFiles.Where(f => f.Name != "." && f.Name != ".." && !f.Name.StartsWith('.')))
                    {
                        files.Add(new RemoteFileInfo
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            IsDirectory = file.IsDirectory,
                            Size = file.IsDirectory ? 0 : file.Length,
                            LastModified = file.LastWriteTime
                        });
                    }
                }

                response.Response = files.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.Name).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.StausResult = StausResult.Exception;
                response.ExtraMessage = ex.Message;
                return StatusCode(500, response);
            }
        }
    }
}