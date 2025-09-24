using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using WebSSH.Shared;

namespace WebSSH.Test
{
    public class ClientStoredSessionsModelTest
    {
        [Test]
        public void TestAddOrUpdateStoredSessions()
        {
            var storedSessionsModel = new ClientStoredSessionsModel();
            ClassicAssert.AreEqual(0, storedSessionsModel.Sessions.Count);

            storedSessionsModel.AddOrUpdateStoredSessions(new ClientStoredSessionModel());
            ClassicAssert.AreEqual(1, storedSessionsModel.Sessions.Count);
        }

        [Test]
        public void TestRemoveStoredSessions()
        {
            var sessionModel = new ClientStoredSessionModel();
            var storedSessionsModel = new ClientStoredSessionsModel();
            storedSessionsModel.AddOrUpdateStoredSessions(sessionModel);
            ClassicAssert.AreEqual(1, storedSessionsModel.Sessions.Count);

            storedSessionsModel.RemoveStoredSession(Guid.NewGuid());
            ClassicAssert.AreEqual(1, storedSessionsModel.Sessions.Count);

            storedSessionsModel.RemoveStoredSession(sessionModel.UniqueKey);
            ClassicAssert.AreEqual(0, storedSessionsModel.Sessions.Count);
        }
    }
}
