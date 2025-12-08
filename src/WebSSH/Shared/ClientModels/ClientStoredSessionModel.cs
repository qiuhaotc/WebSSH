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
        public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Password;
        public string PrivateKey { get; set; }
        public string PrivateKeyPassphrase { get; set; }

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

        [JsonIgnore]
        public string PrivateKeyDecrypted
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(PrivateKey))
                    {
                        return Encoding.UTF8.GetString(Convert.FromBase64String(PrivateKey));
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
                    PrivateKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                }
                else
                {
                    PrivateKey = string.Empty;
                }
            }
        }

        [JsonIgnore]
        public string PrivateKeyPassphraseDecrypted
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(PrivateKeyPassphrase))
                    {
                        return Encoding.UTF8.GetString(Convert.FromBase64String(PrivateKeyPassphrase));
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
                    PrivateKeyPassphrase = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                }
                else
                {
                    PrivateKeyPassphrase = string.Empty;
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
                AuthenticationType = AuthenticationType,
                PrivateKey = PrivateKey,
                PrivateKeyPassphrase = PrivateKeyPassphrase,
            };
        }
    }
}
