using System.Collections.Generic;

namespace WebSSH.Shared
{
    public class FileUploadResult
    {
        public int TotalFiles { get; set; }
        public int SuccessfulUploads { get; set; }
        public List<UploadedFileInfo> UploadedFiles { get; set; } = new List<UploadedFileInfo>();
    }

    public class UploadedFileInfo
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public string RemotePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}