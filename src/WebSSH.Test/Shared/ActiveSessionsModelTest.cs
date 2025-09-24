﻿using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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
            ClassicAssert.AreEqual(1, model.Sessions.Count);
        }

        [Test]
        public void TestRemoveActiveSession()
        {
            var sessionModel = new ActiveSessionModel();
            var model = new ActiveSessionsModel();
            model.AddActiveSession(sessionModel);
            ClassicAssert.AreEqual(1, model.Sessions.Count);

            model.RemoveActiveSession(Guid.NewGuid());
            ClassicAssert.AreEqual(1, model.Sessions.Count);

            model.RemoveActiveSession(sessionModel.UniqueKey);
            ClassicAssert.AreEqual(0, model.Sessions.Count);
        }
    }
}
