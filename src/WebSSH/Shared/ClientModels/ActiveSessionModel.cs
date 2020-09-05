using System;
using System.Text.Json.Serialization;

namespace WebSSH.Shared
{
    public class ActiveSessionModel
    {
        public ClientStoredSessionModel StoredSessionModel { get; set; }

        public Guid UniqueKey { get; set; } = Guid.NewGuid();

        public string Status { get; set; }

        [JsonIgnore]
        public string OutputStr { get; set; }

        public DateTime StartSessionDate { get; set; }
    }
}
