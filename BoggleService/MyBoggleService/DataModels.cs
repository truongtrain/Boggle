using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Boggle
{
    /// <summary>
    /// Data Models class represents the data types in the boggle service.
    /// </summary>
    public class DataModels
    {
        /// <summary>
        /// User Info class has two property, Nick name and User Token.
        /// </summary>
        [DataContract]
        public class UserInfo
        {

            /// <summary>
            /// Nick name of the user. 
            /// It doens't have to be unique.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string Nickname { get; set; }

            /// <summary>
            /// User Token.
            /// This is unique.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string UserToken { get; set; }

            /// <summary>
            /// a user's desired time limit
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public int TimeLimit { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public string Word { get; set; }
        }


        /// <summary>
        /// Game info class is the information of the game.
        /// </summary>
        [DataContract()]
        public class GameInfo
        {
            public GameInfo ()
            {

            }
            public GameInfo (int Gameid)
            {
                GameState = "pending";
                GameID = Gameid.ToString();
                //Player1.Score = 0;
               // Player2.Score = 0;
                GameBoard = new BoggleBoard();
                Board = GameBoard.ToString();
            }
            /// <summary>
            /// Game Id is a unique if for each game.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string GameID { get; set; }

            /// <summary>
            /// Game state can be either be active, pending and completed.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string GameState { get; set; }


            /// <summary>
            /// Board is the charaters that will be displayed in the game client.
            /// It is randomly generated from the predefined sequence of characters.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public BoggleBoard GameBoard { get; set; }


            /// <summary>
            /// Board is the charaters that will be displayed in the game client.
            /// It is randomly generated from the predefined sequence of characters.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string Board { get; set; }



            /// <summary>
            /// The remaining time in the game
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public int? TimeLeft { get; set; }
            /// <summary>
            /// Time limit should be between 5 and 120 seconds.
            /// It is the average of two players' desired time limits.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public int TimeLimit { get; set; }

            /// <summary>
            /// Time left in the game.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public int TimeStarted { get; set; }

            /// <summary>
            /// player 1's information.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public Player Player1 { get; set; }

            /// <summary>
            /// player 2's information.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public Player Player2 { get; set; }

            /// <summary>
            /// This is the optional argument that determines what information is going to be displayed.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string Brief { get; set; }
        }

        /// <summary>
        /// The player object contains information of each player.
        /// </summary>
        [DataContract()]
        [Serializable]
        public class Player
        {
            public Player()
            { }
            public Player(Player a)
            {
                UserToken = a.UserToken;
                Nickname = a.Nickname;
                Score = a.Score;
                this.WordsPlayed = a.WordsPlayed;
            }
            /// <summary>
            /// User's unique token
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string UserToken { get; set; }

            /// <summary>
            /// User's nick name.
            /// Doesn't have to be unique.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public string Nickname { get; set; }

            /// <summary>
            /// The current score of a player.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public int? Score { get; set; }

            /// <summary>
            /// List of the words played.
            /// The object word played contains each word and its score.
            /// </summary>
            [DataMember(EmitDefaultValue = false)]
            public List<WordPlayed> WordsPlayed { get; set; }

            /// <summary>
            /// This object contains words and their scores.
            /// </summary>
            [DataContract]
            public class WordPlayed
            {
                /// <summary>
                /// word typed.
                /// </summary>
                [DataMember(EmitDefaultValue = false)]
                public string Word { get; set; }

                /// <summary>
                /// If a string has fewer than three characters, it scores zero points.
                /// Otherwise, if a string has a duplicate that occurs earlier in the list, it scores zero points.
                /// Otherwise, if a string is legal(it appears in the dictionary and occurs on the board), 
                /// it receives a score that depends on its length.
                /// 
                /// Three- and four-letter words are worth one point, 
                /// five-letter words are worth two points, six-letter words are worth three points, 
                /// seven-letter words are worth five points, and longer words are worth 11 points.
                /// Otherwise, the string scores negative one point.
                /// </summary>
                [DataMember(EmitDefaultValue = false)]
                public int? Score { get; set; }
            }
        }
        
    }
}