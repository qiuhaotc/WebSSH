using System;
using System.Collections.Generic;
using System.Linq;

namespace WebSSH.Shared
{
    public class ClientStoredSessionsModel
    {
        public void AddOrUpdateStoredSessions(ClientStoredSessionModel session)
        {
            var exist = Sessions.FirstOrDefault(u => u.UniqueKey == session.UniqueKey);
            if (exist != null)
            {
                if (!session.Equals(exist))
                {
                    Sessions.Remove(exist);
                    Sessions.Add(session);
                }
            }
            else
            {
                Sessions.Add(session);
            }
        }

        public void RemoveStoredSession(Guid sessionKey)
        {
            var session = Sessions.FirstOrDefault(u => u.UniqueKey == sessionKey);
            if (session != null)
            {
                Sessions.Remove(session);
            }
        }

        public List<ClientStoredSessionModel> Sessions { get; set; } = new();
    }
}
