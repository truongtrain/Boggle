using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using static Boggle.DataModels;
using static Boggle.DataModels.Player;

namespace Boggle
{
    [ServiceContract]
    public interface IBoggleService
    {
        /// <summary>
        /// Sends back index.html as the response body.
        /// </summary>
        [WebGet(UriTemplate = "/api")]
        Stream API();

       


        /// <summary>
        /// Registers a new user.
        /// Nick name doesn't have to be unique, but should be less than 50 characters.
        /// </summary>
        [WebInvoke(Method = "POST", UriTemplate = "/users")]
        UserInfo CreateUser(UserInfo user);


        [WebInvoke(Method = "POST", UriTemplate = "/games")]
        GameInfo JoinGame(UserInfo user);


        [WebInvoke(Method = "PUT", UriTemplate = "/games")]
        void CancelJoinRequest(UserInfo user);

        //Ask about the slash after games.(From API)
        [WebInvoke(Method = "PUT", UriTemplate = "/games/{GameID}")]
        WordPlayed PlayWord(UserInfo user,string GameID);


        [WebInvoke(Method = "GET", UriTemplate = "/games/{GameID}?Brief={Brief}")]
        GameInfo GameStatus(string Brief,string GameID);
    }
}
