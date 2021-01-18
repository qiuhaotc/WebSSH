using System;
using System.Collections.Concurrent;

namespace WebSSH.Shared
{
    public class ActiveSessionsModel
    {
        public void RemoveActiveSession(Guid sessionKey)
        {
            Sessions.TryRemove(sessionKey, out _);
        }

        public void RemoveActiveSessions()
        {
            Sessions.Clear();
        }

        public void AddActiveSession(ActiveSessionModel session)
        {
            Sessions.TryAdd(session.UniqueKey, session);
        }

        public ConcurrentDictionary<Guid, ActiveSessionModel> Sessions { get; set; } = new();
    }
}
