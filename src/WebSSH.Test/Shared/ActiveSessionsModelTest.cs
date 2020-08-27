using System;
using NUnit.Framework;
using WebSSH.Shared;

namespace WebSSH.Test
{
    public class ActiveSessionsModelTest
    {
        [Test]
        public void TestAddActiveSession()
        {
            var model = new ActiveSessionsModel();
            model.AddActiveSession(new ActiveSessionModel());
            Assert.AreEqual(1, model.Sessions.Count);
        }

        [Test]
        public void TestRemoveActiveSession()
        {
            var sessionModel = new ActiveSessionModel();
            var model = new ActiveSessionsModel();
            model.AddActiveSession(sessionModel);
            Assert.AreEqual(1, model.Sessions.Count);

            model.RemoveActiveSession(Guid.NewGuid());
            Assert.AreEqual(1, model.Sessions.Count);

            model.RemoveActiveSession(sessionModel.UniqueKey);
            Assert.AreEqual(0, model.Sessions.Count);
        }
    }
}
