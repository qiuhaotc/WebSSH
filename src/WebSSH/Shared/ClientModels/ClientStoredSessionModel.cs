using System;
using System.Text;
using System.Text.Json.Serialization;

namespace WebSSH.Shared
{
    public class ClientStoredSessionModel
    {
        public Guid UniqueKey { get; set; } = Guid.NewGuid();
        public string DisplayName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string UserName { get; set; }
        public string Password { get; set; }
        public byte[] FingerPrint { get; set; }
        public byte[] LoginKey { get; set; }

        [JsonIgnore]
        public string PasswordDecryped
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(Password))
                    {
                        return Encoding.UTF8.GetString(Convert.FromBase64String(Password));
                    }
                }
                catch
                {
                }

                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                }
                else
                {
                    Password = string.Empty;
                }
            }
        }

        public ClientStoredSessionModel Clone(bool sameKey = true)
        {
            return new()
            {
                UniqueKey = sameKey ? UniqueKey : Guid.NewGuid(),
                DisplayName = DisplayName,
                Host = Host,
                Port = Port,
                UserName = UserName,
                Password = Password,
                FingerPrint = FingerPrint,
                LoginKey = LoginKey,
            };
        }
    }
}
