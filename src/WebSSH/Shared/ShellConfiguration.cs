namespace WebSSH.Shared
{
    public class ShellConfiguration
    {
        public ServerUser[] Users { get; set; }
        public int MaxIdleMinutes { get; set; }
        public bool NeedAuthorization { get; set; }
        
        // File upload limitations
        public int MaxFilesPerUpload { get; set; } = 3;
        public int MaxFileSizeMB { get; set; } = 10;
        public int MaxFilesPerHour { get; set; } = 20;
    }
}
