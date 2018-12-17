using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.DataLake.Store.AclTools;
using System.Text;
using System.IO;
using Microsoft.Azure.DataLake.Store.AclTools.Jobs;
using Microsoft.Azure.DataLake.Store.MockAdlsFileSystem;
using System;
using System.Threading;

namespace Microsoft.Azure.DataLake.Store.UnitTest
{
    /// <summary>
    /// Tests retry mechanisms, token cancellation, Account validation and Acl serializing deserializing
    /// </summary>
    [TestClass]
    public class MockAdlsApiTest{
        private static AdlsClient _adlsClient = MockAdlsFileSystem.MockAdlsClient.GetMockClient();
        private static string rootPath = "/a";
        private static string TestString = "Hello";

        [ClassInitialize]
        public static void SetupClient(TestContext context)
        {
            _adlsClient.CreateDirectory(rootPath);
            _adlsClient.CreateDirectory(rootPath+"/b0");
            var testByte= Encoding.UTF8.GetBytes(TestString);
            using(var stream = _adlsClient.CreateFile(rootPath+"/bFile01", IfExists.Overwrite)){
                stream.Write(testByte,0, testByte.Length);
            }
            _adlsClient.CreateDirectory(rootPath+"/b0/c0");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMockClientAccountValidation()
        {
            MockAdlsClient.GetMockClient("rdutta.azuredatalakestore.net/");
        }

        [TestMethod]
        public void TestModifyAndRemoveAclRecursively()
        {
            var acls=FilePropertiesUnitTest.GetAclEntryForModifyAndRemove();
            var stats = _adlsClient.ChangeAcl(rootPath, acls, RequestedAclType.ModifyAcl, 1, null, default(CancellationToken));
            Assert.IsTrue(stats.DirectoryProcessed == 3);
            Assert.IsTrue(stats.FilesProcessed == 1);
            Assert.IsTrue(VerifyChangeAclJob.CheckAclListContains(_adlsClient.GetAclStatus(rootPath).Entries, acls));
            Assert.IsTrue(VerifyChangeAclJob.CheckAclListContains(_adlsClient.GetAclStatus(rootPath + "/b0/c0").Entries, acls));
            Assert.IsTrue(VerifyChangeAclJob.CheckAclListContains(_adlsClient.GetAclStatus(rootPath + "/bFile01").Entries, acls));
            stats = _adlsClient.ChangeAcl(rootPath, acls, RequestedAclType.RemoveAcl, 1, null, default(CancellationToken));
            Assert.IsTrue(stats.DirectoryProcessed == 3);
            Assert.IsTrue(stats.FilesProcessed == 1);
            Assert.IsTrue(VerifyChangeAclJob.CheckAclListContains(_adlsClient.GetAclStatus(rootPath).Entries, acls, true));
            Assert.IsTrue(VerifyChangeAclJob.CheckAclListContains(_adlsClient.GetAclStatus(rootPath + "/b0/c0").Entries, acls, true));
            Assert.IsTrue(VerifyChangeAclJob.CheckAclListContains(_adlsClient.GetAclStatus(rootPath + "/bFile01").Entries, acls, true));
        }

        [TestMethod]
        public void TestGetContentSummary()
        {
            var summary= _adlsClient.GetContentSummary(rootPath);
            Assert.IsTrue(summary.DirectoryCount == 2);
            Assert.IsTrue(summary.FileCount == 1);
            Assert.IsTrue(summary.Length == TestString.Length);
        }

        [TestMethod]
        public void TestExportFileProperties()
        {
            var acls = FilePropertiesUnitTest.GetAclEntryForSet();
            var stats = _adlsClient.ChangeAcl(rootPath, acls, RequestedAclType.SetAcl);
            _adlsClient.GetFileProperties(rootPath, true, @"C:\Data\logFile");
            Assert.IsTrue(File.Exists(@"C:\Data\logFile"));
            _adlsClient.GetFileProperties(rootPath, true, "/Data/logFile", true, false);
            Assert.IsTrue(_adlsClient.GetDirectoryEntry("/Data/logFile") != null);
        }

        [TestMethod]
        public void TestCreateWithOverwrite()
        {
            var pathname = "/filename";
            _adlsClient.CreateFile(pathname, IfExists.Fail);
            Assert.IsTrue(_adlsClient.GetDirectoryEntry(pathname) != null);
            _adlsClient.CreateFile(pathname, IfExists.Overwrite);
            Assert.IsTrue(_adlsClient.GetDirectoryEntry(pathname) != null);
        }
    }
}