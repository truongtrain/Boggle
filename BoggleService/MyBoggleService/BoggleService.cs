//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MyBoggleService
//{
//    class BoggleService
//    {
//    }
//}


using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Net;
using static Boggle.DataModels;
using static Boggle.DataModels.Player;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService 
    {



        private static readonly object sync = new object();
        private static string BoggleDB;
        static BoggleService()
        {
            string dbFolder = System.IO.Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            string connectionString = String.Format(@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = {0}\BoggleDB.mdf; Integrated Security = True", dbFolder);
            
            BoggleDB = connectionString;
        }



        /// <summary>
        /// The most recent call to SetStatus determines the response code used when
        /// an http response is sent.
        /// </summary>
        /// <param name="status"></param>
        //private static void SetStatus(HttpStatusCode status)
        //{
        //    WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        //}

        /// <summary>
        /// Returns a Stream version of index.html.
        /// </summary>
        /// <returns></returns>
        //public Stream API()
        //{
        //    //SetStatus(OK);
        //    //WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
        //    return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        //}

        public void CancelJoinRequest(UserInfo user,out HttpStatusCode status)
        {
            lock (sync)
            {
                if (user == null || user.UserToken == null || user.UserToken.Length == 0)//||!(users.ContainsKey(user.UserToken)))
                {
                    status = Forbidden; // do this for step 7
                    return;
                }

                using (SqlConnection conn = new SqlConnection(BoggleDB))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        // if the user token doesn't exist in the data, set status forbiden and return.
                        using (SqlCommand cmd = new SqlCommand("select UserID from Users where UserID = @UserID", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UserID", user.UserToken);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    reader.Close();
                                    status = Forbidden;
                                    trans.Commit();
                                    return;
                                }
                                else
                                {
                                    reader.Close();
                                    // get player1 userID from pending game
                                    using (SqlCommand cmd2 = new SqlCommand("select Player1 from Games where Player1 = @UserID and Player2 is NULL", conn, trans))
                                    {
                                        cmd2.Parameters.AddWithValue("@UserID", user.UserToken);
                                        using (SqlDataReader reader2 = cmd2.ExecuteReader())
                                        {
                                            // if no pending game with user
                                            if (!reader2.HasRows)
                                            {
                                                reader2.Close();
                                                status = Forbidden;
                                                trans.Commit();
                                                return;
                                            }
                                            else
                                            {
                                                reader2.Close();
                                                //remove player1 from pending game
                                                using (SqlCommand cmd3 = new SqlCommand("update Games set Games.Player1 = NULL where Player1 = @Player1", conn, trans))
                                                {
                                                    cmd3.Parameters.AddWithValue("@Player1", user.UserToken);
                                                    cmd3.ExecuteNonQuery();
                                                }
                                                status = OK;
                                                trans.Commit();
                                                return;
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }

        public UserInfo CreateUser(UserInfo user, out HttpStatusCode status)
        {
			lock (sync)
			{
				if (user == null || user.Nickname == null || user.Nickname.Length == 0 || user.Nickname.Length > 50)
				{
					status = Forbidden;
					return null;
				}
				string UserToken = Guid.NewGuid().ToString();
				string trimmedNickname = user.Nickname.Trim();

				user.UserToken = UserToken;
				user.Nickname = trimmedNickname;

				//users.Add(user.UserToken,user);

				using (SqlConnection conn = new SqlConnection(BoggleDB))
				{
					conn.Open();
					using (SqlTransaction trans = conn.BeginTransaction())
					{
						//insert new user into Users table
						using (SqlCommand command = new SqlCommand("insert into Users (UserID, Nickname)" +
							"values (@UserToken, @Nickname)", conn, trans))
						{
							command.Parameters.AddWithValue("@UserToken", user.UserToken);
							command.Parameters.AddWithValue("@Nickname", trimmedNickname);

							command.ExecuteNonQuery();
							status = Created;
							trans.Commit();

							UserInfo temp = new UserInfo();
							temp.UserToken = UserToken;
							return temp;
						}
					}
				}
			}
        }



        public GameInfo JoinGame(UserInfo user, out HttpStatusCode status)
        {

            status = OK;
			lock (sync)
			{
				if (user.UserToken == null || user.UserToken.Length == 0 || user.TimeLimit < 5 || user.TimeLimit > 120)
            {
                status = Forbidden;
                return null;
            }
            string gameID = "";
            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
					using (SqlTransaction trans = conn.BeginTransaction())
					{
						//check if the user is in the user table.
						//if not set the status to forbidden and return null.
						using (SqlCommand command = new SqlCommand("select UserID from Users where UserID = @UserID", conn, trans))
						{
							command.Parameters.AddWithValue("@UserID", user.UserToken);

							using (SqlDataReader reader = command.ExecuteReader())
							{
								//if the user is not registered, forbidden
								if (!reader.HasRows)
								{
									reader.Close();
									status = Forbidden;
									trans.Commit();
									return null;
								}
							}
						}

						// create a pending game with no player if needed
						using (SqlCommand command = new SqlCommand("select GameID from Games", conn, trans))
						{

							using (SqlDataReader reader = command.ExecuteReader())
							{
								//if there is no games, add a new pending game.
								if (!reader.HasRows)
								{
									reader.Close();
									using (SqlCommand command4 = new SqlCommand("insert into Games (GameState) values (@GameState)", conn, trans))
									{
										command4.Parameters.AddWithValue("@GameState", "pending");
										command4.ExecuteNonQuery();
									}
								}
							}
						}

						// check if user is already a player in a pending game
						using (SqlCommand command = new SqlCommand("select Player1, Player2 "

					+ "from Games where Player1 = @Player1 and Player2 is NULL", conn, trans))
						{
							command.Parameters.AddWithValue("@Player1", user.UserToken);
							//command.Parameters.AddWithValue("@Player2", null);

							using (SqlDataReader reader = command.ExecuteReader())
							{
								//if user is already Player1 in pending game
								if (reader.HasRows)
								{
									reader.Close();
									status = Conflict;
									trans.Commit();
									return null;
								}
							}
						}
						// get pending gameID
						using (SqlCommand command = new SqlCommand("select GameID from Games where "

							+ "Player2 is NULL", conn, trans))
						{
							//command.Parameters.AddWithValue("@Player2", null);

							using (SqlDataReader reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									gameID = reader["GameID"].ToString();
								}
							}
						}

						// determine if Player1 of the pending game is null
						using (SqlCommand command = new SqlCommand("select Player1 from Games where "

							+ "GameID = @GameID", conn, trans))
						{
							command.Parameters.AddWithValue("@GameID", gameID);
							using (SqlDataReader reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									if (reader.IsDBNull(0))//)//||
									{
										reader.Close();
										//add user as player1 to pending game
										using (SqlCommand command2 = new SqlCommand("update Games set Player1 = @Player1, TimeLimit = @TimeLimit, GameState = @GameState where GameID = @GameID", conn, trans))
										{
											command2.Parameters.AddWithValue("@GameID", gameID);
											command2.Parameters.AddWithValue("@Player1", user.UserToken);
											command2.Parameters.AddWithValue("@TimeLimit", user.TimeLimit);
											command2.Parameters.AddWithValue("@GameState", "pending");

											command2.ExecuteNonQuery();
											status = Accepted;
											GameInfo temp = new GameInfo();
											temp.GameID = gameID;
											trans.Commit();
											return temp;
										}
									}
									else
									{
										reader.Close();
										//otherwise, add user as player2 to pending game and make gamestate active
										using (SqlCommand command3 = new SqlCommand("update Games set Player2 = @Player2, "

											+ "Board = @Board, TimeLimit = (TimeLimit + @TimeLimit) / 2, "

											+ "StartTime = @StartTime, GameState = @GameState where GameID = @GameID", conn, trans))
										{
											command3.Parameters.AddWithValue("@GameID", gameID);
											command3.Parameters.AddWithValue("@Player2", user.UserToken);
											command3.Parameters.AddWithValue("@TimeLimit", user.TimeLimit);
											command3.Parameters.AddWithValue("@GameState", "active");

											BoggleBoard board = new BoggleBoard();
											command3.Parameters.AddWithValue("@Board", board.ToString());
											command3.Parameters.AddWithValue("@StartTime", DateTime.UtcNow);


											command3.ExecuteNonQuery();
											status = Created;
										}
										//adding a new pending game without any players
										using (SqlCommand command4 = new SqlCommand("insert into Games (GameState) values (@GameState)", conn, trans))
										{
											command4.Parameters.AddWithValue("@GameState", "pending");
											command4.ExecuteNonQuery();
											GameInfo temp = new GameInfo();
											temp.GameID = gameID;
											trans.Commit();
											return temp;
										}
									}
								}
								return null;
							}
						}

					}

                }
            }
        }

        public WordPlayed PlayWord(UserInfo user, string GameID, out HttpStatusCode status)
        {

            lock (sync)
            {
                string trimmedWord = user.Word.Trim().ToUpper();
                int scoreOfWord = 0;
                string boardString = "";

                if (!(Int32.TryParse(GameID, out int result)))
                {
                    status = Forbidden;
                    return null;
                }


                if (user.UserToken == null || user.UserToken.Length == 0 || user.Word == null || user.Word.Length == 0 || user.Word.Trim().Length > 30)
                {
                    status = Forbidden;
                    return null;
                }
                using (SqlConnection conn = new SqlConnection(BoggleDB))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        // determine whether GameID is valid (exists in Games table)
                        using (SqlCommand command = new SqlCommand("select GameID, Player1, Player2, Board from Games where Games.GameID = @GameID", conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", GameID);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {   // if GameID does not exist in games
                                if (!reader.HasRows)
                                {
                                    status = Forbidden;
                                    reader.Close();
                                    trans.Commit();
                                    return null;
                                }
                                // determine whether user has valid userToken (Player1 or Player2 has user.UserToken)
                                while (reader.Read())
                                {
                                    string player1Token = reader["Player1"].ToString();
                                    string player2Token = reader["Player2"].ToString();
                                    boardString = reader["Board"].ToString();
                                    // if neither player1 nor player2 has user.UserToken
                                    if ((player1Token != user.UserToken) && (player2Token != user.UserToken))
                                    {
                                        status = Forbidden;
                                        reader.Close();

                                        trans.Commit();
                                        return null;
                                    }
                                }
                                reader.Close();
                                // determine whether game state is not active
                                string gameState = "";
                                HttpStatusCode a = OK;
                                GameStatus("asd", GameID, out a);
                                using (SqlCommand cmd = new SqlCommand("select GameState from Games where Games.GameID = @GameID2", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@GameID2", GameID);
                                    using (SqlDataReader reader2 = cmd.ExecuteReader())
                                    {
                                        while (reader2.Read())
                                        {
                                            gameState = reader2["GameState"].ToString();
                                        }
                                        reader2.Close();
                                    }

                                }



                                if (gameState != "active")
                                {
                                    status = Conflict;
                                    trans.Commit();
                                    return null;
                                }


                                if (trimmedWord.Length < 3)
                                    scoreOfWord = 0;
                                else
                                {
                                    BoggleBoard board = new BoggleBoard(boardString);
                                    if (board.CanBeFormed(trimmedWord) && IsInDictionary(trimmedWord))
                                    {
                                        if (trimmedWord.Length < 5)
                                            scoreOfWord = 1;
                                        else if (trimmedWord.Length == 5)
                                            scoreOfWord = 2;
                                        else if (trimmedWord.Length == 6)
                                            scoreOfWord = 3;
                                        else if (trimmedWord.Length == 7)
                                            scoreOfWord = 5;
                                        else
                                            scoreOfWord = 11;

                                    }
                                    else
                                    {
                                        scoreOfWord = -1;
                                    }
                                }

                            }
                        }

                        // check if this word has already been played by this player in this game
                        using (SqlCommand command = new SqlCommand("select Word from Words where Words.GameID = @GameID and Words.Word = @Word and Words.Player = @Player", conn, trans))
                        {
                            command.Parameters.AddWithValue("@Word", trimmedWord);
                            command.Parameters.AddWithValue("@GameID", GameID);
                            command.Parameters.AddWithValue("@Player", user.UserToken);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {   // if this word has already been played by this player in this game
                                if (reader.HasRows)
                                {
                                    scoreOfWord = 0;
                                }
                                reader.Close();
                            }
                        }


                        //update Word table
                        using (SqlCommand command = new SqlCommand("insert into Words(Word, GameID, Player, Score) values(@Word, @GameID, @Player, @Score)", conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", GameID);
                            command.Parameters.AddWithValue("@Word", trimmedWord);
                            command.Parameters.AddWithValue("@Player", user.UserToken);
                            command.Parameters.AddWithValue("@Score", scoreOfWord);
                            command.ExecuteNonQuery();
                            status = OK;
                            trans.Commit();
                            WordPlayed word = new WordPlayed();
                            word.Score = scoreOfWord;
                            return word;
                        }


                    }
                }
            }
        }




        public Boolean IsInDictionary(string word)
        {
            string line;

            using (StreamReader file = new System.IO.StreamReader(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt"))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line == word)
                    {
                        return true;
                    }
                }
                return false;
            }

        }






        public GameInfo GameStatus(string Brief, string GameID, out HttpStatusCode statusCode)
        {
			lock (sync)
			{
				statusCode = OK;
            if (GameID == null || !(Int32.TryParse(GameID, out int result)))//|| !games.ContainsKey(GameID)|| result>=games.Count
            {
                statusCode = Forbidden;
                return null;
            }
            else
            {
                GameInfo status;
                using (SqlConnection conn = new SqlConnection(BoggleDB))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
							// if the user token doesn't exist in the data, set status forbiden and return.
							using (SqlCommand cmd = new SqlCommand("select * from Games where GameID = @GameID", conn, trans))
							{
								Int32.TryParse(GameID, out int intGameID);
								cmd.Parameters.AddWithValue("@GameID", intGameID);
								using (SqlDataReader reader = cmd.ExecuteReader())
								{
									while (reader.Read())
									{
										if (!reader.HasRows)
										{
											statusCode = Forbidden;
											trans.Commit();
											reader.Close();
											return null;
										}
										else
										{
											statusCode = OK;
											status = new GameInfo
											{
												GameState = reader["GameState"].ToString()
											};

											if (status.GameState == "pending")
											{
												reader.Close();
												trans.Commit();
												return status;
											}
											else
											//active or completed
											{

												if (Brief != null && Brief == "yes")
												{
													//gamestate,timeleft,players' score
													Player player1 = new Player();
													Player player2 = new Player();
													player1.Score = 0;
													player2.Score = 0;
													player1.WordsPlayed = new List<WordPlayed>();
													player2.WordsPlayed = new List<WordPlayed>();
													string playerID1 = reader["Player1"].ToString();
													string playerID2 = reader["Player2"].ToString();
													status.Player1 = player1;
													status.Player2 = player2;
                                                    status.Player1.WordsPlayed = null;
                                                    status.Player2.WordsPlayed = null;

													Int32.TryParse(reader["TimeLimit"].ToString(), out int result1);
													DateTime startTime = (DateTime)reader["StartTime"];
													status.TimeLeft = ((result1) - (int)(DateTime.UtcNow - startTime).TotalSeconds);

													reader.Close();
													//get scores for players
													using (SqlCommand cmd2 = new SqlCommand("select Score from Words where Player = @Player and GameID = @GameID", conn, trans))
													{
														cmd2.Parameters.AddWithValue("@Player", playerID1);
														Int32.TryParse(GameID, out int intGameID2);
														cmd2.Parameters.AddWithValue("@GameID", intGameID2);

                                                        using (SqlDataReader reader2 = cmd2.ExecuteReader())
                                                        {
                                                            

                                                            while (reader2.Read())
                                                            {
                                                                if (reader2.IsDBNull(0))//)//||
                                                                {
                                                                    status.Player1.Score = 0;
                                                                }
                                                                status.Player1.Score += Convert.ToInt32(reader2["Score"]);//have to chekc if this works
                                                            }
                                                            reader2.Close();
                                                        }
													}
													using (SqlCommand cmd2 = new SqlCommand("select Score from Words where Player = @Player and GameID = @GameID", conn, trans))
													{
														cmd2.Parameters.AddWithValue("@Player", playerID2);
														cmd2.Parameters.AddWithValue("@GameID", GameID);
                                                        using (SqlDataReader reader2 = cmd2.ExecuteReader())
                                                        {
                                                            while (reader2.Read())
                                                            {
                                                                if (reader2.IsDBNull(0))//)//||
                                                                {
                                                                    status.Player2.Score = 0;
                                                                }
                                                                status.Player2.Score += Convert.ToInt32(reader2["Score"]);
                                                            }
                                                            reader2.Close();
                                                        }
													}


													// if the game is over, update the gamestate to completed
													if (((result1) - (int)(DateTime.UtcNow - startTime).TotalSeconds) <= 0)
													{
														//maybe wrong syntax. game id == the current game id
														using (SqlCommand cmd2 = new SqlCommand("update Games set GameState = 'completed' where GameID = @GameID", conn, trans))
														{
															cmd2.Parameters.AddWithValue("@GameID", GameID);
															status.TimeLeft = 0;
															cmd2.ExecuteNonQuery();
															return status;
														}
													}
													status.TimeLeft = ((result1) - (int)(DateTime.UtcNow - startTime).TotalSeconds);
													trans.Commit();
													return status;
												}
												//no brief.
												else
												{

													status.Board = reader["Board"].ToString();
													Int32.TryParse(reader["TimeLimit"].ToString(), out int result1);
													//Int32.TryParse(reader["StartTime"].ToString(), out int result2);
													status.TimeLimit = result1;
													status.TimeLeft = ((result1) - (int)(DateTime.UtcNow - (DateTime)reader["StartTime"]).TotalSeconds);


													Player player1 = new Player();
													Player player2 = new Player();
													player1.Score = 0;
													player2.Score = 0;
													player1.WordsPlayed = new List<WordPlayed>();
													player2.WordsPlayed = new List<WordPlayed>();
													string playerID1 = reader["Player1"].ToString();
													string playerID2 = reader["Player2"].ToString();
													status.Player1 = player1;
													status.Player2 = player2;
													DateTime startTime = (DateTime)reader["StartTime"];
													reader.Close();
													//get nicknames
													using (SqlCommand cmd2 = new SqlCommand("select Nickname from Users where UserID = @UserID", conn, trans))
													{
														cmd2.Parameters.AddWithValue("@UserID", playerID1);
														SqlDataReader reader2 = cmd2.ExecuteReader();
														while (reader2.Read())
														{
															status.Player1.Nickname = (reader2.GetString(0));
														}
														reader2.Close();
													}
													using (SqlCommand cmd2 = new SqlCommand("select Nickname from Users where UserID = @UserID", conn, trans))
													{
														cmd2.Parameters.AddWithValue("@UserID", playerID2);
														SqlDataReader reader2 = cmd2.ExecuteReader();
														while (reader2.Read())
														{
															status.Player2.Nickname = (reader2.GetString(0));
														}
														reader2.Close();
													}


													//modify the score of each player
													using (SqlCommand cmd2 = new SqlCommand("select Score from Words where Player = @Player and GameID = @GameID and not Score is NULL", conn, trans))
													{
														cmd2.Parameters.AddWithValue("@Player", playerID1);
														cmd2.Parameters.AddWithValue("@GameID", GameID);
														using (SqlDataReader reader2 = cmd2.ExecuteReader())
														{
															//determine if the words table contains the player
															if (!reader2.HasRows)//)//||
															{
																status.Player1.Score = 0;
															}
															while (reader2.Read())
															{
																status.Player1.Score += Convert.ToInt32(reader2["Score"]);
															}
															reader2.Close();
														}
													}
													using (SqlCommand cmd3 = new SqlCommand("select Score from Words where Player = @Player and GameID = @GameID and not Score is NULL", conn, trans))
													{
														cmd3.Parameters.AddWithValue("@Player", playerID2);
														cmd3.Parameters.AddWithValue("@GameID", GameID);
														using (SqlDataReader reader2 = cmd3.ExecuteReader())
														{
															//determine if the words table contains the player
															if (!reader2.HasRows)//)//||
															{
																status.Player2.Score = 0;
															}
															while (reader2.Read())
															{
																status.Player2.Score += Convert.ToInt32(reader2["Score"]);
															}
															reader2.Close();
														}
													}
													//at the moment when game status is called.
													if (status.GameState == "active")
													{
														//if it is no longer an active game, set state to completed and return.
														if (status.TimeLeft <= 0)
														{
															//maybe wrong syntax. game id == the current game id
															using (SqlCommand cmd2 = new SqlCommand("update Games set GameState = 'completed' where GameID = @GameID", conn, trans))
															{
																cmd2.Parameters.AddWithValue("@GameID", GameID);
																//Int32.TryParse(GameID, out int result3);
																//cmd.Parameters.Add("@GameId", SqlDbType.Int).Value = result3;
																status.TimeLeft = 0;
																cmd2.ExecuteNonQuery();
																status.GameState = "completed";
																List<WordPlayed> wordsplayed1 = new List<WordPlayed>();
																List<WordPlayed> wordsplayed2 = new List<WordPlayed>();
																using (SqlCommand cmd3 = new SqlCommand("select * from Words where Player = @Player and GameID = @GameID", conn, trans))
																{
																	cmd3.Parameters.AddWithValue("@Player", playerID1);
																	cmd3.Parameters.AddWithValue("@GameID", GameID);
																	SqlDataReader reader2 = cmd3.ExecuteReader();
																	while (reader2.Read())
																	{
																		string word = reader2["Word"].ToString();
																		int score = reader2.GetInt32(4);//check later the parameter is the column number
																		WordPlayed temp = new WordPlayed();
																		temp.Word = word;
																		temp.Score = score;
																		wordsplayed1.Add(temp);
																	}
																	reader2.Close();
																	status.Player1.WordsPlayed = wordsplayed1;
																}
																using (SqlCommand cmd3 = new SqlCommand("select * from Words where Player = @Player and GameID = @GameID", conn, trans))
																{
																	cmd3.Parameters.AddWithValue("@Player", playerID2);
																	cmd3.Parameters.AddWithValue("@GameID", GameID);
																	SqlDataReader reader2 = cmd3.ExecuteReader();
																	while (reader2.Read())
																	{
																		string word = reader2["Word"].ToString();
																		int score = reader2.GetInt32(4);
																		WordPlayed temp = new WordPlayed();
																		temp.Word = word;
																		temp.Score = score;
																		wordsplayed2.Add(temp);
																	}
																	reader2.Close();
																	status.Player2.WordsPlayed = wordsplayed2;
																}
																trans.Commit();
																return status;
															}
														}
														//else, just set the timeleft and return.
														status.TimeLeft = ((result1) - (int)(DateTime.UtcNow - startTime).TotalSeconds);
														trans.Commit();
														return status;
													}
													//completed
													else
													{
														List<WordPlayed> wordsplayed1 = new List<WordPlayed>();
														List<WordPlayed> wordsplayed2 = new List<WordPlayed>();
														using (SqlCommand cmd2 = new SqlCommand("select * from Words where Player = @Player and GameID = @GameID", conn, trans))
														{
															cmd2.Parameters.AddWithValue("@Player", playerID1);
															cmd2.Parameters.AddWithValue("@GameID", GameID);
															SqlDataReader reader2 = cmd2.ExecuteReader();
															while (reader2.Read())
															{
																string word = reader2["Word"].ToString();
																int score = reader2.GetInt32(4);//check later the parameter is the column number
																WordPlayed temp = new WordPlayed();
																temp.Word = word;
																temp.Score = score;
																wordsplayed1.Add(temp);
															}
															reader2.Close();
															status.Player1.WordsPlayed = wordsplayed1;
														}
														using (SqlCommand cmd2 = new SqlCommand("select * from Words where Player = @Player and GameID = @GameID", conn, trans))
														{
															cmd2.Parameters.AddWithValue("@Player", playerID2);
															cmd2.Parameters.AddWithValue("@GameID", GameID);
															SqlDataReader reader2 = cmd2.ExecuteReader();
															while (reader2.Read())
															{
																string word = reader2["Word"].ToString();
																int score = reader2.GetInt32(4);
																WordPlayed temp = new WordPlayed();
																temp.Word = word;
																temp.Score = score;
																wordsplayed2.Add(temp);
															}
															reader2.Close();
															status.Player2.WordsPlayed = wordsplayed2;
														}
														status.Player1.WordsPlayed = wordsplayed1;
														status.Player2.WordsPlayed = wordsplayed2;
														status.TimeLeft = 0;
														trans.Commit();
														return status;
													}
												}
											}
										}
									}
									return null;
								}
							}
                        }
                    }
                }
            }
        }
    }
}
