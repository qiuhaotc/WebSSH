using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using WebSSH.Shared;

namespace WebSSH.Test
{
    public class ClientStoredSessionModelTest
    {
        [Test]
        public void TestDefaultAuthenticationType()
        {
            var sessionModel = new ClientStoredSessionModel();
            ClassicAssert.AreEqual(AuthenticationType.Password, sessionModel.AuthenticationType);
        }

        [Test]
        public void TestPasswordAuthentication()
        {
            var sessionModel = new ClientStoredSessionModel
            {
                AuthenticationType = AuthenticationType.Password,
                PasswordDecryped = "testpassword"
            };

            ClassicAssert.AreEqual(AuthenticationType.Password, sessionModel.AuthenticationType);
            ClassicAssert.AreEqual("testpassword", sessionModel.PasswordDecryped);
            ClassicAssert.IsFalse(string.IsNullOrEmpty(sessionModel.Password));
        }

        [Test]
        public void TestPrivateKeyAuthentication()
        {
            var testKey = "-----BEGIN RSA PRIVATE KEY-----\ntest key content\n-----END RSA PRIVATE KEY-----";
            var sessionModel = new ClientStoredSessionModel
            {
                AuthenticationType = AuthenticationType.PrivateKey,
                PrivateKeyDecrypted = testKey
            };

            ClassicAssert.AreEqual(AuthenticationType.PrivateKey, sessionModel.AuthenticationType);
            ClassicAssert.AreEqual(testKey, sessionModel.PrivateKeyDecrypted);
            ClassicAssert.IsFalse(string.IsNullOrEmpty(sessionModel.PrivateKey));
        }

        [Test]
        public void TestPrivateKeyPassphrase()
        {
            var testPassphrase = "mysecretpassphrase";
            var sessionModel = new ClientStoredSessionModel
            {
                PrivateKeyPassphraseDecrypted = testPassphrase
            };

            ClassicAssert.AreEqual(testPassphrase, sessionModel.PrivateKeyPassphraseDecrypted);
            ClassicAssert.IsFalse(string.IsNullOrEmpty(sessionModel.PrivateKeyPassphrase));
        }

        [Test]
        public void TestCloneWithPrivateKey()
        {
            var testKey = "-----BEGIN RSA PRIVATE KEY-----\ntest\n-----END RSA PRIVATE KEY-----";
            var original = new ClientStoredSessionModel
            {
                DisplayName = "Test Session",
                Host = "example.com",
                Port = 22,
                UserName = "testuser",
                AuthenticationType = AuthenticationType.PrivateKey,
                PrivateKeyDecrypted = testKey,
                PrivateKeyPassphraseDecrypted = "testpass"
            };

            var cloned = original.Clone(true);

            ClassicAssert.AreEqual(original.UniqueKey, cloned.UniqueKey);
            ClassicAssert.AreEqual(original.DisplayName, cloned.DisplayName);
            ClassicAssert.AreEqual(original.Host, cloned.Host);
            ClassicAssert.AreEqual(original.Port, cloned.Port);
            ClassicAssert.AreEqual(original.UserName, cloned.UserName);
            ClassicAssert.AreEqual(original.AuthenticationType, cloned.AuthenticationType);
            ClassicAssert.AreEqual(original.PrivateKey, cloned.PrivateKey);
            ClassicAssert.AreEqual(original.PrivateKeyPassphrase, cloned.PrivateKeyPassphrase);
        }

        [Test]
        public void TestCloneWithDifferentKey()
        {
            var original = new ClientStoredSessionModel
            {
                DisplayName = "Test Session",
                AuthenticationType = AuthenticationType.PrivateKey
            };

            var cloned = original.Clone(false);

            ClassicAssert.AreNotEqual(original.UniqueKey, cloned.UniqueKey);
            ClassicAssert.AreEqual(original.AuthenticationType, cloned.AuthenticationType);
        }

        [Test]
        public void TestEmptyPrivateKeyDecrypted()
        {
            var sessionModel = new ClientStoredSessionModel();
            
            ClassicAssert.AreEqual(string.Empty, sessionModel.PrivateKeyDecrypted);
            
            sessionModel.PrivateKeyDecrypted = null;
            ClassicAssert.AreEqual(string.Empty, sessionModel.PrivateKey);
        }
    }
}
