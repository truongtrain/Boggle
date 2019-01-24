using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Boggle.DataModels;
using CustomNetworking;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Runtime.Serialization.Json;
using static Boggle.DataModels.Player;

namespace Boggle
{
    class Program
    {
        static void Main()
        {
            //HttpStatusCode status;
            //UserInfo a = new UserInfo();
            //a.Nickname = "hansol";
            //BoggleService service = new BoggleService();
            //a = service.CreateUser(a, out status);
            //Console.WriteLine(a.UserToken);
            //Console.WriteLine(status.ToString());

            // This is our way of preventing the main thread from
            // exiting while the server is in use
            new BoggleServer(60000);
            Console.ReadLine();
        }

        public class BoggleServer
        {
            // listens for incoming connection requests
            private SSListener server;

            public BoggleServer(int port)
            {
                Encoding encoding = new UTF8Encoding();
                // a StringStocketListener that listens for incoming connection requests
                server = new SSListener(port, encoding);

                // start the StringSocketListener
                server.Start();

                // Ask the server to call ConnectionRequested when a connection request arrives
                server.BeginAcceptSS(ConnectionRequested, null);

            }


            /// <summary>
            /// This is the callback method that is passed to BeginAcceptStringSocket. It is called
            /// when a connection request has arrived at the server.
            /// </summary>
            /// <param name="result"></param>
            private void ConnectionRequested(SS ss, object payload)
            {
                server.BeginAcceptSS(ConnectionRequested, null);
                new RequestHandler(ss);
            }

            public class RequestHandler
            {
                // The socket making the request
                private SS ss;

                // The first line from the socket or null if not read yet
                private string firstLine;

                // The value of the Content-Length header or zero if no header seen yet
                private int contentLength;

                private static readonly Regex createUserPattern = new Regex(@"^POST /BoggleService.svc/users HTTP");

                private static readonly Regex joinGamePattern = new Regex(@"^POST /BoggleService.svc/games HTTP");

                private static readonly Regex playWordPattern = new Regex(@"^PUT /BoggleService.svc/games\/(\d+) HTTP");

                private static readonly Regex cancelJoinPattern = new Regex(@"^PUT /BoggleService.svc/games HTTP");

                private static readonly Regex gameStatusPattern = new Regex(@"^GET /BoggleService.svc/games\/(\d+) HTTP");

                private static readonly Regex contentLengthPattern = new Regex(@"^content-length: (\d+)", RegexOptions.IgnoreCase);
                public RequestHandler(SS ss)
                {
                    this.ss = ss;
                    contentLength = 0;
                    ss.BeginReceive(ReadLines, null);
                }

                private void ReadLines(String line, object p)
                {
                    if (line.Trim().Length == 0 && contentLength > 0) // has contentLength header
                    {
                        ss.BeginReceive(ProcessRequest, null, contentLength);
                    }
                    else if (line.Trim().Length == 0) // No JSON object in request
                    {
                        ProcessRequest(null);
                    }
                    else if (firstLine != null) // if this is not the first line
                    {
                        Match m = contentLengthPattern.Match(line);
                        if (m.Success)
                        {
                            contentLength = int.Parse(m.Groups[1].ToString());
                        }
                        ss.BeginReceive(ReadLines, null);
                    }
                    else // this is the first line
                    {
                        firstLine = line;
                        ss.BeginReceive(ReadLines, null);
                    }
                }

                private void ProcessRequest(string line, object p = null)
                {
                    // Determine which service method to invoke from

                    // Handle "create user" requests
                    if (createUserPattern.IsMatch(firstLine))
                    {
                        UserInfo user = JsonConvert.DeserializeObject<UserInfo>(line);
                        user = new BoggleService().CreateUser(user, out HttpStatusCode status);
                        String result = "HTTP/1.1 " + (int)status + " " + status + "\r\n";
                        if ((int)status / 100 == 2) // status is OK
                        {
                            string res = JsonConvert.SerializeObject(user);
                            result += "Content-Length: " + Encoding.UTF8.GetByteCount(res) + "\r\n";
                            result += res;
                        }
                        result += "\r\n";
                        ss.BeginSend(result, (x, y) =>
                        {
                            ss.Dispose();
                            //ss.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        }, null);

                    }
                    // Handle "Join game" request
                    else if (joinGamePattern.IsMatch(firstLine))
                    {
                        UserInfo user = JsonConvert.DeserializeObject<UserInfo>(line);
                        GameInfo game = new BoggleService().JoinGame(user, out HttpStatusCode status);
                        String result = "HTTP/1.1 " + (int)status + " " + status + "\r\n";
                        if ((int)status / 100 == 2) // status is OK
                        {
                            string res = JsonConvert.SerializeObject(game);
                            result += "Content-Length: " + Encoding.UTF8.GetByteCount(res) + "\r\n";
                            result += res;
                        }
                        result += "\r\n";

                        ss.BeginSend(result, (x, y) =>
                        {
                            ss.Dispose();
                            //ss.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        }, null);
                    }

                    // Handle Play word request
                    else if (playWordPattern.IsMatch(firstLine))
                    {
                        UserInfo user = JsonConvert.DeserializeObject<UserInfo>(line);
                        string gameid = "";
                        foreach (char a in firstLine)
                        {
                            if (Int32.TryParse(a.ToString(), out int IdontCare))
                            {
                                gameid += a.ToString();
                            }
                            if (a == 'H')
                                break;
                        }
                        WordPlayed wp = new BoggleService().PlayWord(user, gameid, out HttpStatusCode status);
                        String result = "HTTP/1.1 " + (int)status + " " + status + "\r\n";
                        if ((int)status / 100 == 2) // status is OK
                        {
                            string res = JsonConvert.SerializeObject(wp);
                            result += "Content-Length: " + Encoding.UTF8.GetByteCount(res) + "\r\n";
                            result += res;
                        }
                        result += "\r\n";
                        ss.BeginSend(result, (x, y) =>
                        {
                            ss.Dispose();
                            //ss.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        }, null);
                    }
                    // Handle cancel join request
                    else if (cancelJoinPattern.IsMatch(firstLine))
                    {
                        UserInfo user = JsonConvert.DeserializeObject<UserInfo>(line);
                        new BoggleService().CancelJoinRequest(user, out HttpStatusCode status);
                        String result = "HTTP/1.1 " + (int)status + " " + status + "\r\n";
                        result += "\r\n";
                        ss.BeginSend(result, (x, y) =>
                        {
                            ss.Dispose();
                            //ss.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        }, null);
                    }
                    // Handle game status request
                    else if (gameStatusPattern.IsMatch(firstLine))
                    {
                        string Brief = "hgf";
                        int briefIndex = firstLine.IndexOf("Brief");
                        if (briefIndex != -1)
                        {
                            if (firstLine.Substring(briefIndex + 6, 12) == "yes HTTP/1.1")
                            {
                                Brief = "yes";
                            }
                        }
                        string gameid = "";
                        foreach (char a in firstLine)
                        {
                            if (Int32.TryParse(a.ToString(), out int IdontCare))
                            {
                                gameid += a.ToString();
                            }
                            if (a == 'H')
                                break;
                        }
                        GameInfo gi = new BoggleService().GameStatus(Brief, gameid, out HttpStatusCode status);
                        String result = "HTTP/1.1 " + (int)status + " " + status + "\r\n";
                        if ((int)status / 100 == 2) // status is OK
                        {
                            string res = JsonConvert.SerializeObject(gi);
                            result += "Content-Length: " + Encoding.UTF8.GetByteCount(res) + "\r\n";
                            result += res;
                        }
                        result += "\r\n";
                        ss.BeginSend(result, (x, y) =>
                        {
                            ss.Dispose();
                            //ss.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        }, null);

                    }
                    else // handle invalid requests
                    {
                        String result = "HTTP/1.1 400 INVALID REQUEST\r\n\r\n";
                        ss.BeginSend(result, (x, y) =>
                        {
                            ss.Dispose();
                            //ss.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                        }, null);
                    }



                }
            }
        }
    }
}
