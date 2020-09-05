using System;

namespace WebSSH.Shared
{
    public class ClientLoginModel
    {
        public Guid UniqueKey { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool Persist { get; set; }
        public string Captcha { get; set; }
        public LoginStatus Status { get; set; }
        public string Message { get; set; }
    }
}
