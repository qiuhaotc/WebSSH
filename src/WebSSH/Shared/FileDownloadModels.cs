using System;
using System.Collections.Generic;

namespace WebSSH.Shared
{
    public class FileDownloadResult
    {
        public int TotalFiles { get; set; }
        public int SuccessfulDownloads { get; set; }
        public List<DownloadedFileInfo> DownloadedFiles { get; set; } = new List<DownloadedFileInfo>();
    }

    public class DownloadedFileInfo
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public string RemotePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class FileDownloadRequest
    {
        public List<string> FilePaths { get; set; } = new List<string>();
    }

    public class RemoteFileInfo
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}