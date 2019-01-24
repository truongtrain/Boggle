using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.HttpStatusCode;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Dynamic;
using static Boggle.DataModels;

namespace Boggle
{
    /// <summary>
    /// Provides a way to start and stop the IIS web server from within the test
    /// cases.  If something prevents the test cases from stopping the web server,
    /// subsequent tests may not work properly until the stray process is killed
    /// manually.
    /// </summary>
    public static class IISAgent
    {
        // Reference to the running process
        private static Process process = null;

        /// <summary>
        /// Starts IIS
        /// </summary>
        public static void Start(string arguments)
        {
            if (process == null)
            {
                ProcessStartInfo info = new ProcessStartInfo(Properties.Resources.IIS_EXECUTABLE, arguments);
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = false;
                process = Process.Start(info);
            }
        }

        /// <summary>
        ///  Stops IIS
        /// </summary>
        public static void Stop()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }
    [TestClass]
    public class BoggleTests
    {
        /// <summary>
        /// This is automatically run prior to all the tests to start the server
        /// </summary>
        [ClassInitialize()]
        public static void StartIIS(TestContext testContext)
        {
            //IISAgent.Start(@"/site:""BoggleService"" /apppool:""Clr4IntegratedAppPool"" /config:""..\..\..\.vs\config\applicationhost.config""");
        }

        /// <summary>
        /// This is automatically run when all tests have completed to stop the server
        /// </summary>
        [ClassCleanup()]
        public static void StopIIS()
        {
            //IISAgent.Stop();
        }

        private RestTestClient client = new RestTestClient("http://localhost:60000/BoggleService.svc/");

        [TestMethod]
        public void TestCreateUser()
        {
            // created

            UserInfo user = new UserInfo();
            user.Nickname = "Alan";
            Response r = client.DoPostAsync("users", user).Result;
            Assert.AreEqual(Created, r.Status);
            
         
            


        }

        [TestMethod]
        public void TestCreateUser2()
        {
            //"" forbidden
            UserInfo user = new UserInfo();
            user.Nickname = "";
            Response r = client.DoPostAsync("users", user).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestCreateUser3()
        {
            //null forbidden
            UserInfo user = new UserInfo();
            user.Nickname = null;
            Response r = client.DoPostAsync("users", user).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestCreateUser4()
        {
           
          
            //>50 char forbidden
            UserInfo user = new UserInfo();
            user.Nickname = "11111111111111111111111111111111111111111111111111111111111111111111111111111";
            Response r = client.DoPostAsync("users", user).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }




        

        [TestMethod]
        public void TestJoinGame2()
        {
            // <5 forbidden
            UserInfo user = new UserInfo();
            user.Nickname = "Alan";
            Response r = client.DoPostAsync("users", user).Result;
            string userToken = r.Data.UserToken.ToString();
            user.UserToken = userToken;
            user.TimeLimit = 3;
            r = client.DoPostAsync("games", user).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestJoinGame3()
        {
            // >120 forbidden
            UserInfo user = new UserInfo();
            user.Nickname = "Alan";
            Response r = client.DoPostAsync("users", user).Result;
            string userToken = r.Data.UserToken.ToString();
            user.UserToken = userToken;
            user.TimeLimit = 121;
            r = client.DoPostAsync("games", user).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestJoinGame4()
        {
            // Invalid user token forbidden
            UserInfo user = new UserInfo();
            user.Nickname = "Alan";
            Response r = client.DoPostAsync("users", user).Result;
            string userToken = r.Data.UserToken.ToString();
            user.UserToken = "";
            user.TimeLimit = 60;
            r = client.DoPostAsync("games", user).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestJoinGame5()
        {
            // Invalid user token forbidden
            UserInfo user = new UserInfo();
            user.Nickname = "Alan";
            Response r = client.DoPostAsync("users", user).Result;
            string userToken = r.Data.UserToken.ToString();
            user.UserToken = null;
            user.TimeLimit = 60;
            r = client.DoPostAsync("games", user).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        



    }
}
