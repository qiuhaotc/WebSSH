using System;
using NUnit.Framework;
using WebSSH.Shared;

namespace WebSSH.Test
{
    public class ClientStoredSessionsModelTest
    {
        [Test]
        public void TestAddOrUpdateStoredSessions()
        {
            var storedSessionsModel = new ClientStoredSessionsModel();
            Assert.AreEqual(0, storedSessionsModel.Sessions.Count);

            storedSessionsModel.AddOrUpdateStoredSessions(new ClientStoredSessionModel());
            Assert.AreEqual(1, storedSessionsModel.Sessions.Count);
        }

        [Test]
        public void TestRemoveStoredSessions()
        {
            var sessionModel = new ClientStoredSessionModel();
            var storedSessionsModel = new ClientStoredSessionsModel();
            storedSessionsModel.AddOrUpdateStoredSessions(sessionModel);
            Assert.AreEqual(1, storedSessionsModel.Sessions.Count);

            storedSessionsModel.RemoveStoredSession(Guid.NewGuid());
            Assert.AreEqual(1, storedSessionsModel.Sessions.Count);

            storedSessionsModel.RemoveStoredSession(sessionModel.UniqueKey);
            Assert.AreEqual(0, storedSessionsModel.Sessions.Count);
        }
    }
}
