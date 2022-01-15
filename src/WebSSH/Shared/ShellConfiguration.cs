namespace WebSSH.Shared
{
    public class ShellConfiguration
    {
        public ServerUser[] Users { get; set; }
        public int MaxIdleMinutes { get; set; }
        public bool NeedAuthorization { get; set; }
    }
}
