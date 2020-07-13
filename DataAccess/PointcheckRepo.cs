using System.Collections.Generic;
using pointcheck_api.Models;
using Dapper;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace pointcheck_api.DataAccess
{

    public class PointcheckRepo : IPointcheckRepo
    {
        private IDbConnection db;

        private readonly IConfiguration _config;

        public PointcheckRepo(IConfiguration config)
        {
            _config = config;

            this.db = new SqlConnection(_config.GetConnectionString("cloudserver"));

        }


        private static HttpClient _httptest = new HttpClient(); //only http client. used to scrape bungie.net

        public int _corruptedCount = 0; //# of corrupted bungie pages hit by scrape method. resets each run. 
        public async Task<string> httpReq(Uri url)
        {
            // HttpClient _httptest = new HttpClient();

            HttpResponseMessage response = await _httptest.GetAsync(url);

            string result = null;

            try
            {
                response.EnsureSuccessStatusCode();

                if (response.Content is object)
                {
                    result = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

                response.Dispose();
                // _httptest.Dispose();

            }

            return result;
        }

        //return number of corrupt pages; currently only used for HR as that seems to corrupt the most
        public int CorruptedCount()
        {
            return _corruptedCount;
        }

        public async Task<string> GetEmblem(string haloGame, string playerName)
        {
            HttpClient bungie = new HttpClient();

            string profileLink = null, emblemUrlLeadUp = null, baseUrl = "http://halo.bungie.net/", errorString = null, errorString2;
            if (haloGame == "Halo 3")
            {
                profileLink = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player=";
                emblemUrlLeadUp = "identityStrip_EmblemCtrl_imgEmblem\" src=\"/";
                errorString = "We were not able to find any record of Halo 3 activity";
                errorString2 = "No games found for this player"; //sometimes this generic error page comes up
            }
            else if (haloGame == "Halo 2")
            {
                profileLink = "http://halo.bungie.net/stats/playerstatshalo2.aspx?player=";
                emblemUrlLeadUp = "identityStrip_EmblemCtrl2_imgEmblem\" src=\"/";
                errorString = "We were not able to find any record of Halo 2 activity";
                errorString2 = "No games found for this player"; //sometimes this generic error page comes up
            }
            else //must be reach
            {
                profileLink = "https://halo.bungie.net/Stats/Reach/default.aspx?player=";
                emblemUrlLeadUp = "img id=\"ctl00_mainContent_identityBar_emblemImg\" src=\"/";
                errorString = "We were not able to find any record of Halo: Reach activity";
                errorString2 = "No games found for this player"; //sometimes this generic error page comes up
            }


            string emblemFullUrl = null, fullhtml = await bungie.GetStringAsync(profileLink + playerName);
            //gamertag exists check
            if (fullhtml.IndexOf(errorString) == -1 && fullhtml.IndexOf(errorString2) == -1)
            {
                emblemFullUrl = fullhtml.Substring(fullhtml.IndexOf(emblemUrlLeadUp) + emblemUrlLeadUp.Length, //start substring at shortest unique lead of characters before image + length of lead
                (fullhtml.IndexOf(" ", fullhtml.IndexOf(emblemUrlLeadUp) + emblemUrlLeadUp.Length)) - (fullhtml.IndexOf(emblemUrlLeadUp) + emblemUrlLeadUp.Length) - 1);
                //^ dirty string work. this should probably be a regex

                emblemFullUrl = emblemFullUrl.Replace("&amp;", "&");
                return baseUrl + emblemFullUrl;
            }
            else
                return null; //return null if player doesn't exist
        }

        public PlayerStoredResult IsInDbH3(string player, string game)
        {
            PlayerStoredResult status;
            //throw new System.NotImplementedException();
            var p = new DynamicParameters();
            p.Add("@Player", player);
            p.Add("@Game", game);

           status = this.db.Query<PlayerStoredResult>("GetPlayerStatus", p, commandType: CommandType.StoredProcedure).SingleOrDefault();


           return status;
        }
        public PlayerStoredResult IsInDbH2(string player, string game)
        {
            throw new System.NotImplementedException();
        }
        public PlayerStoredResult IsInDbHR(string player, string game)
        {
            throw new System.NotImplementedException();
        }
        public async void AddGamesPlayed(string player, string gameName, List<Game> gamesList)
        {
            DataTable insertedGames = new DataTable();

            insertedGames.Columns.Add("Player", typeof(string));
            insertedGames.Columns.Add("Game", typeof(string));
            insertedGames.Columns.Add("GameID", typeof(int));
            insertedGames.Columns.Add("Map", typeof(string));
            insertedGames.Columns.Add("Playlist", typeof(string));
            insertedGames.Columns.Add("Gametype", typeof(string));
            insertedGames.Columns.Add("PlayerKD", typeof(string));
            insertedGames.Columns.Add("PlayerPlacing", typeof(string));
            insertedGames.Columns.Add("GameDate", typeof(DateTime));

            foreach(Game i in gamesList)
            {
                //object[] o = {player, gameName, i.gameID, i.map, i.playlist, i.gametype, i.playerOneKD, i.playerOnePlacing, i.gamedate};
                insertedGames.Rows.Add(new {player, gameName, i.gameID, i.map, i.playlist, i.gametype, i.playerOneKD, i.playerOnePlacing, i.gamedate});
            }

            IDbTransaction trans = db.BeginTransaction();

            db.ExecuteAsync(@"
            INSERT INTO CachedGames(Player, Game, GameID, Map, Playlist, Gametype, PlayerKD, PlayerPlacing, GameDate, DateStored)
            VALUES(@Player, @Game, @GameID, @Map, @Playlist, @Gametype, @PlayerKD, @PlayerPlacing, @GameDate)", insertedGames, transaction: trans);


            //throw new System.NotImplementedException();
        }

        public MatchedGamesResult GetMatchedGames(string playerOne, string playerTwo)
        {
            var returnedResult = new MatchedGamesResult
            {
                playerOneName = "Kifflom",
                playerTwoName = "Infury",
                MatchedGames = new List<Game>{
                 new Game{gameID=100, map="The Pit", gametype="CTF 3Flag", gamedate = new DateTime(2010, 2, 2)},
                 new Game{gameID=200, map="Narrows", gametype="CTF 3Flag", gamedate = new DateTime(2010, 4, 2)},
                 new Game{gameID=3400, map="Heretic", gametype="Slayer", gamedate = new DateTime(2011, 4, 2)}
             }
            };

            return returnedResult;
        }


        //need the .Single() or this.db.QuerySingle(etc.) in place of this.db.Query
        public Game DatabaseTest()
        {

            var game = this.db.Query<Game>("Select top 1 Gameid, map, playlist, gametype from GameDetails").Single();

            return game;

        }

        public async Task<List<Game>> ScrapeH3(bool getCustoms, string playerName)
        {
            WebClient bungie = new WebClient(); //used to get count of pages of games played
            //HttpClient IDDownloader = new HttpClient();
            List<Game> gameList = new List<Game>();

            //reformat to put multiple variables on one line
            string GT = playerName;
            string matchHistoryP2;
            string matchHistoryP1;
            string fullhtml;
            int sigStartGameCount;
            int sigEndGameCount;
            int numofGames;
            int approxNumOfPages;
            int sigStartGameID;
            int sigEndGameID;
            int sigMidGameID;
            int sigStartGameType;
            int sigEndGameType;
            int sigStartDate;
            int sigEndDate;
            int sigStartMap;
            int sigEndMap;
            int sigStartPlaylist;
            int sigEndPlaylist;
            int sigStartPlacing;
            int sigEndPlacing;
            string taskResult;
            int gameID = 0;
            int corruptPages = 0;
            List<string> corruptedPages = new List<string>();
            string gametype, map, playlist, date;
            DateTime dateConvert = new DateTime();

            if (getCustoms)
            {
                matchHistoryP2 = "&cus=1&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //the URL for customs
            }
            else
            {
                matchHistoryP2 = "&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //URL for MM games
            }

            matchHistoryP1 = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player="; //first part of match history page string
                                                                                           //2nd part of match history page string. concatted to current page


            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + 1); //first page of GT1s game history
            sigStartGameCount = fullhtml.IndexOf("&nbsp;<strong>"); //index of first char in HTML line that gives you total MM games

            sigEndGameCount = fullhtml.IndexOf("</strong>", sigStartGameCount); //index of next char after final digit of total MM games
            //fist char + length of that substring as start index, length of characters in number of MM games as endingChar - startingChar - length of "Intro" substring = number of MM games as string
            numofGames = int.Parse(fullhtml.Substring(sigStartGameCount + "&nbsp;<strong>".Length, (sigEndGameCount - sigStartGameCount - "&nbsp;<strong>".Length)));
            approxNumOfPages = (numofGames / 25) + 1; //25 games a page, +1 to make sure a page isn't missed due to integer division
            bungie.Dispose();

            List<Task<string>> newTasks = new List<Task<string>>();


            List<string> taskIDandSiteLink = new List<string>();
            for (int i = 1; i <= approxNumOfPages; i++)
            {
                Uri siteLink = new Uri(matchHistoryP1 + GT + matchHistoryP2 + i); //GT = name of player, passed to method.
                                                                                  //creates url like http://halo.bungie.net/stats/playerstatshalo3.aspx?player=infury&ctl00_mainContent_bnetpgl_recentgamesChangePage=1

                //taskIDandSiteLink.Add(tasks.Last().Id + " " + siteLink.ToString()); //list of taskIDs and what page they should download
                newTasks.Add(httpReq(siteLink));
            }

            while (newTasks.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("new iteration of while loop");

                var taskComplete = await Task.WhenAny(newTasks);

                newTasks.Remove(taskComplete); //remove finished task from list
                System.Diagnostics.Debug.WriteLine("Task " + taskComplete.Id + " finshed at " + System.DateTime.Now + " - " + newTasks.Count + " tasks remain");

                try
                {

                    taskResult = taskComplete.Result;
                    //taskComplete.Dispose();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine(taskComplete.Id.ToString());
                    //Debug.Print(taskComplete.Id.ToString());
                    // taskComplete.Dispose();
                    taskResult = "";
                    continue;
                }


                sigMidGameID = 0;
                sigStartGameID = 0;
                sigEndGameID = 0;

                if (taskResult.IndexOf("No games found for this player.") != -1 ||
                    taskResult.IndexOf("It seems that you have encountered a problem with our site.") != -1)
                {
                    corruptPages++;
                    corruptedPages.Add(taskResult);

                    continue; //if index of above IS NOT negative one, then it's a corrupted page or a customs page that doesn't exist.
                              //skip this task and await the next one
                }


                for (int x = 0; x < 25; x++) //25 GameIDs per page
                {
                    sigStartGameID = taskResult.IndexOf("GameStatsHalo3", sigMidGameID); //find gameID
                    sigEndGameID = taskResult.IndexOf("&amp;player", sigMidGameID);

                    try
                    {
                        GamePlayed foundGame = new GamePlayed();
                        Game gameDetailed = new Game();
                        int.TryParse(taskResult.Substring(sigStartGameID + "GameStatsHalo3.aspx?gameid=".Length, sigEndGameID - "GameStatsHalo3.aspx?gameid=".Length - sigStartGameID), out gameID);
                        foundGame.gameID = gameID;
                        gameDetailed.gameID = gameID;

                        //get gametype for this row --working
                        sigStartGameType = taskResult.IndexOf("\">", sigEndGameID);
                        sigEndGameType = taskResult.IndexOf("</a", sigEndGameID);
                        gameDetailed.gametype = taskResult.Substring(sigStartGameType + "\">".Length, sigEndGameType - "\">".Length - sigStartGameType);

                        //get date for this row -- working
                        sigStartDate = taskResult.IndexOf("</td><td>\r\n                                ", sigEndGameType) + "</td><td>\r\n                                ".Length;
                        sigEndDate = taskResult.IndexOf("M", sigStartDate) + 1;
                        date = taskResult.Substring(sigStartDate, sigEndDate - sigStartDate);

                        //get map for this row -- working
                        sigStartMap = taskResult.IndexOf("</td><td>\r\n                                ", sigEndDate) + "</td><td>\r\n                                ".Length;
                        sigEndMap = taskResult.IndexOf("\r\n", sigStartMap);
                        gameDetailed.map = taskResult.Substring(sigStartMap, sigEndMap - sigStartMap);

                        //get playlist for this row
                        sigStartPlaylist = taskResult.IndexOf("</td><td>\r\n                                ", sigEndMap) + "</td><td>\r\n                                ".Length;
                        sigEndPlaylist = taskResult.IndexOf("\r\n", sigStartPlaylist);
                        gameDetailed.playlist = taskResult.Substring(sigStartPlaylist, sigEndPlaylist - sigStartPlaylist);

                        //get placing for this row

                        sigStartPlacing = taskResult.IndexOf("</td><td>\r\n                                ", sigEndPlaylist) + "</td><td>\r\n                                ".Length;
                        sigEndPlacing = taskResult.IndexOf("\r\n", sigStartPlacing);
                        gameDetailed.playerOnePlacing = taskResult.Substring(sigStartPlacing, sigEndPlacing - sigStartPlacing);
                        try
                        {
                            gameDetailed.gamedate = DateTime.Parse(date); //try to parse what we think is the date, if parse fails it's not a valid gameID

                            //detailTable.Rows.Add(gameID, map, playlist, gametype, dateConvert, GT);
                            gameList.Add(gameDetailed);

                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine(gameID + " couldn't be parsed");
                            //int ix = 0;
                            //couldn't parse this date
                        }


                        //foundGame.IsWin = [..]
                        //dataTable.Rows.Add(x, GT, gameID, customsFlag);


                    }
                    catch
                    {
                        x = 0;
                        break; //if parse fails before x = 25, taskResult page didn't have a full 25 games iterate to next Task
                    }


                    sigMidGameID = sigEndGameID + 1; //increment index by 1 to find next instance of a GameID in the html

                }

            }


            /*             if (GameDetailsTable.Rows.Count == 0) //if there aren't already games in this table from a previous instance of this method
                            GameDetailsTable = detailTable; //assign details table to player property since idk if you can return more than one thing per method
                        else
                            GameDetailsTable.Merge(detailTable); //if rowcount != 0, merge existing GameDetailsTable with one from this instance of the method */

            return gameList;

        }


        public async Task<List<Game>> ScrapeH2(bool getCustoms, string playerName)
        {

            //ServicePointManager.DefaultConnectionLimit = 90;
            WebClient bungie = new WebClient(); //used to get count of pages of games played
            //HttpClient IDDownloader = new HttpClient();
            List<Game> gameList = new List<Game>();


            //IDDownloader.DefaultRequestHeaders.ConnectionClose = true;

            string GT = playerName;
            string matchHistoryP2;
            string matchHistoryP1;
            string fullhtml;
            int sigStartGameCount;
            int sigEndGameCount;
            int numofGames;
            int approxNumOfPages;
            int sigStartGameID;
            int sigEndGameID;
            int sigMidGameID;
            int sigStartGameType;
            int sigEndGameType;
            int sigStartDate;
            int sigEndDate;
            int sigStartMap;
            int sigEndMap;
            int sigStartPlaylist;
            int sigEndPlaylist;
            string taskResult;
            int gameID = 0;
            int corruptPages = 0;
            List<string> corruptedPages = new List<string>();
            string date;

            //if (getCustoms) - don't need get customs logic, customs included in regular gameID feed for H2

            matchHistoryP2 = "&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //URL for MM games

            matchHistoryP1 = "http://halo.bungie.net/stats/playerstatshalo2.aspx?player="; //first part of match history page string
                                                                                           //2nd part of match history page string. concatted to current page


            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + 1); //first page of GT1s game history
            sigStartGameCount = fullhtml.IndexOf("&nbsp;<strong>"); //index of first char in HTML line that gives you total MM games

            sigEndGameCount = fullhtml.IndexOf("</strong>", sigStartGameCount); //index of next char after final digit of total MM games
            //fist char + length of that substring as start index, length of characters in number of MM games as endingChar - startingChar - length of "Intro" substring = number of MM games as string
            numofGames = int.Parse(fullhtml.Substring(sigStartGameCount + "&nbsp;<strong>".Length, (sigEndGameCount - sigStartGameCount - "&nbsp;<strong>".Length)));
            approxNumOfPages = (numofGames / 25) + 1; //25 games a page, +1 to make sure a page isn't missed due to integer division
            bungie.Dispose();

            List<Task<string>> newTasks = new List<Task<string>>();

            List<string> taskIDandSiteLink = new List<string>();
            for (int i = 1; i <= approxNumOfPages; i++)
            {
                Uri siteLink = new Uri(matchHistoryP1 + GT + matchHistoryP2 + i); //GT = name of player, passed to method.
                                                                                  //creates url like http://halo.bungie.net/stats/playerstatshalo3.aspx?player=infury&ctl00_mainContent_bnetpgl_recentgamesChangePage=1

                //taskIDandSiteLink.Add(tasks.Last().Id + " " + siteLink.ToString()); //list of taskIDs and what page they should download
                newTasks.Add(httpReq(siteLink));
            }


            while (newTasks.Count > 0)
            {
                //if (tasks.Count < 2) //debugging; why the fuck will this not complete for all tasks
                //System.Diagnostics.Debug.WriteLine("last 15 tasks");

                System.Diagnostics.Debug.WriteLine("new iteration of while loop");

                var taskComplete = await Task.WhenAny(newTasks);

                newTasks.Remove(taskComplete); //remove finished task from list
                System.Diagnostics.Debug.WriteLine("Task " + taskComplete.Id + " finshed at " + System.DateTime.Now + " - " + newTasks.Count + " tasks remain");

                try
                {

                    taskResult = taskComplete.Result;
                    //taskComplete.Dispose();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine(taskComplete.Id.ToString());
                    //Debug.Print(taskComplete.Id.ToString());
                    // taskComplete.Dispose();
                    taskResult = "";
                    continue;
                }


                sigMidGameID = 0;
                sigStartGameID = 0;
                sigEndGameID = 0;

                if (taskResult.IndexOf("No games found for this player.") != -1 ||
                    taskResult.IndexOf("It seems that you have encountered a problem with our site.") != -1)
                {
                    corruptPages++;
                    corruptedPages.Add(taskResult);

                    continue; //if index of above IS NOT negative one, then it's a corrupted page or a customs page that doesn't exist.
                              //skip this task and await the next one
                }


                for (int x = 0; x < 25; x++) //25 GameIDs per page
                {
                    sigStartGameID = taskResult.IndexOf("GameStatsHalo2", sigMidGameID); //find gameID
                    sigEndGameID = taskResult.IndexOf("&amp;player", sigMidGameID);

                    try
                    {
                        GamePlayed foundGame = new GamePlayed();
                        Game gameDetailed = new Game();
                        int.TryParse(taskResult.Substring(sigStartGameID + "GameStatsHalo2.aspx?gameid=".Length, sigEndGameID - "GameStatsHalo2.aspx?gameid=".Length - sigStartGameID), out gameID);
                        foundGame.gameID = gameID;
                        gameDetailed.gameID = gameID;

                        //get gametype for this row --working
                        sigStartGameType = taskResult.IndexOf("\">", sigEndGameID);
                        sigEndGameType = taskResult.IndexOf("</a", sigEndGameID);
                        gameDetailed.gametype = taskResult.Substring(sigStartGameType + "\">".Length, sigEndGameType - "\">".Length - sigStartGameType);

                        //get date for this row -- working
                        sigStartDate = taskResult.IndexOf("</td><td>\r\n                                ", sigEndGameType) + "</td><td>\r\n                                ".Length;
                        sigEndDate = taskResult.IndexOf("M", sigStartDate) + 1;
                        date = taskResult.Substring(sigStartDate, sigEndDate - sigStartDate);

                        //get map for this row -- working
                        sigStartMap = taskResult.IndexOf("</td><td>\r\n                                ", sigEndDate) + "</td><td>\r\n                                ".Length;
                        sigEndMap = taskResult.IndexOf("\r\n", sigStartMap);
                        gameDetailed.map = taskResult.Substring(sigStartMap, sigEndMap - sigStartMap);

                        //get playlist for this row
                        sigStartPlaylist = taskResult.IndexOf("</td><td>\r\n                                ", sigEndMap) + "</td><td>\r\n                                ".Length;
                        sigEndPlaylist = taskResult.IndexOf("\r\n", sigStartPlaylist);
                        gameDetailed.playlist = taskResult.Substring(sigStartPlaylist, sigEndPlaylist - sigStartPlaylist);


                        try
                        {
                            gameDetailed.gamedate = DateTime.Parse(date); //try to parse what we think is the date, if parse fails it's not a valid gameID

                            //detailTable.Rows.Add(gameID, map, playlist, gametype, dateConvert, GT);
                            gameList.Add(gameDetailed);

                        }
                        catch
                        {

                            System.Diagnostics.Debug.WriteLine(gameID + " couldn't be parsed");
                            //int ix = 0;

                            break;
                        }


                    }
                    catch
                    {
                        x = 0;
                        break; //if parse fails before x = 25, taskResult page didn't have a full 25 games iterate to next Task
                    }


                    sigMidGameID = sigEndGameID + 1; //increment index by 1 to find next instance of a GameID in the html

                }


                System.Diagnostics.Debug.WriteLine("Task " + taskComplete.Id + " completed");

                //taskComplete.Dispose(); 






            }


            /*             if (GameDetailsTable.Rows.Count == 0) //if there aren't already games in this table from a previous instance of this method
                            GameDetailsTable = detailTable; //assign details table to player property since idk if you can return more than one thing per method
                        else
                            GameDetailsTable.Merge(detailTable); //if rowcount != 0, merge existing GameDetailsTable with one from this instance of the method */



            return gameList;


        }

        public async Task<List<Game>> ScrapeHR(bool getCustoms, string playerName)
        {
            _corruptedCount = 0; //set corrupted count back to 0 for new run

            //ServicePointManager.DefaultConnectionLimit = 90;
            WebClient bungie = new WebClient(); //used to get count of pages of games played
            List<Game> gameList = new List<Game>();


            //IDDownloader.DefaultRequestHeaders.ConnectionClose = true;

            string GT = playerName;
            string matchHistoryP1, matchHistoryP2, playerProfile;
            string fullhtml;
            int sigStartCompGameCount, sigEndCompGameCount, sigStartCustomGameCount, sigEndCustomGameCount;
            int numOfCompGames, numOfCustomGames;
            int approxNumOfCompPages;
            int sigStartGameType, sigEndGameType;
            int sigStartGameID, sigEndGameID;
            int sigStartPlacing, sigEndPlacing;
            int sigStartDate, sigEndDate;
            int sigStartKd, sigEndKd;
            int sigStartMap, sigEndMap;
            int sigStartPlaylist, sigEndPlaylist;
            string taskResult;
            int gameID = 0;
            List<string> corruptedPages = new List<string>();
            string date;

            //if (getCustoms) - don't need get customs logic, customs included in regular gameID feed for H2

            matchHistoryP2 = "vc=3&player="; //URL for MM games

            matchHistoryP1 = "https://halo.bungie.net/Stats/Reach/PlayerGameHistory.aspx?"; //first part of match history page string
                                                                                            //2nd part of match history page string. concatted to current page
            playerProfile = "https://halo.bungie.net/Stats/Reach/default.aspx?player=";
            int substringLen;

            fullhtml = bungie.DownloadString(playerProfile + GT); //first page of GT1s game history
            int tagGameCount = fullhtml.IndexOf("<h4>Competitive</h4>"); //where to look in html to pull # of comp games
            sigStartCompGameCount = fullhtml.IndexOf("\">", tagGameCount); //pull # of Competitive games from player profile
            sigEndCompGameCount = fullhtml.IndexOf("</span>", sigStartCompGameCount);
            //fist char + length of that substring as start index, length of characters in number of MM games as endingChar - startingChar - length of "Intro" substring = number of MM games as string
            numOfCompGames = int.Parse((fullhtml.Substring(sigStartCompGameCount + "\">".Length, (sigEndCompGameCount - sigStartCompGameCount - "\">".Length))).Replace(",", ""));
            approxNumOfCompPages = (numOfCompGames / 25) + 1; //25 games a page, +1 to make sure a page isn't missed due to integer division



            bungie.Dispose();
            List<Task<string>> newTasks = new List<Task<string>>();

            List<string> taskIDandSiteLink = new List<string>();
            for (int i = 0; i <= approxNumOfCompPages; i++)
            {
                Uri siteLink = new Uri(matchHistoryP1 + matchHistoryP2 + GT + "&page=" + i);
                //creats url like https://halo.bungie.net/stats/reach/playergamehistory.aspx?vc=6&player=Kifflom&page=0


                newTasks.Add(httpReq(siteLink));
                taskIDandSiteLink.Add(newTasks.Last().Id + " " + siteLink.ToString()); //list of taskIDs and what page they should download
            }
            if (getCustoms)
            {
                /*                 int approxNumOfCustomPages;
                                tagGameCount = "<span id=\"ctl00_bottomContent_pieChartPopoutRepeater_ctl03_gameCountLabel\">2,431</span>";
                                sigStartCustomGameCount = fullhtml.IndexOf(tagGameCount);
                                substringLen = tagGameCount.Length;
                                sigEndCustomGameCount = fullhtml.IndexOf("</span>");

                                numOfCustomGames = int.Parse(fullhtml.Substring(sigStartCustomGameCount + substringLen, (sigEndCompGameCount - sigStartCompGameCount - substringLen)).Replace(",", "")); //remove comma in thousands
                                approxNumOfCustomPages = (numOfCustomGames / 25) + 1;
                                matchHistoryP2 = "vc=6&player="; //vc6 == customs

                                for (int i = 1; i <= approxNumOfCustomPages; i++)
                                {
                                    Uri siteLink = new Uri(matchHistoryP1 + matchHistoryP2 + GT + "&page=" + i);

                                    newTasks.Add(httpReq(siteLink));
                                } */
            }


            while (newTasks.Count > 0)
            {

                System.Diagnostics.Debug.WriteLine("new iteration of while loop");

                var taskComplete = await Task.WhenAny(newTasks);

                newTasks.Remove(taskComplete); //remove finished task from list
                System.Diagnostics.Debug.WriteLine("Task " + taskComplete.Id + " finshed at " + System.DateTime.Now + " - " + newTasks.Count + " tasks remain");

                try
                {

                    taskResult = taskComplete.Result;
                    //taskComplete.Dispose();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine(taskComplete.Id.ToString());
                    taskResult = "";
                    continue;
                }




                if (taskResult.IndexOf("No games found for this player.") != -1 ||
                    taskResult.IndexOf("It seems that you have encountered a problem with our site.") != -1)
                {
                    _corruptedCount++;
                    corruptedPages.Add(taskResult);

                    continue; //if index of above IS NOT negative one, then it's a corrupted page or a customs page that doesn't exist.
                              //skip this task and await the next one
                }
                sigEndPlaylist = 0;
                int ghStart;

                for (int x = 0; x < 25; x++) //25 GameIDs per page
                {
                    try
                    {
                        ghStart = taskResult.IndexOf("pTagOutcome", sigEndPlaylist); //start of reach gamehistory table; start at end of last column each loop
                                                                                     //reach game history starts with gameType, pull that first
                        sigStartGameType = taskResult.IndexOf("style=\"text-transform: capitalize;\">", ghStart);
                        substringLen = "style=\"text-transform: capitalize;\">".Length;
                        sigEndGameType = taskResult.IndexOf("</a", sigStartGameType);
                    }
                    catch (Exception e)
                    {
                        // taskIDandSiteLink.IndexOf(taskComplete.ID)
                        //throw e;
                        break; //pulled a page, like the final page, of games that has no games on it, causing ghStart to OutofIndex. skip to next task.

                    }

                    try
                    {

                        Game gameDetailed = new Game();
                        //store gametype for this row -- working
                        gameDetailed.gametype = taskResult.Substring(sigStartGameType + substringLen, sigEndGameType - substringLen - sigStartGameType);

                        //get gameID for this row -- working
                        sigStartGameID = taskResult.IndexOf("gameid=", sigEndGameType) + "gameid=".Length; //start of game ID string
                        sigEndGameID = taskResult.IndexOf("&amp;player", sigStartGameID);

                        int.TryParse(taskResult.Substring(sigStartGameID, sigEndGameID - sigStartGameID), out gameID);
                        gameDetailed.gameID = gameID;

                        //get placing for this row -- working
                        sigStartPlacing = taskResult.IndexOf("class=\"place\">", sigEndGameID) + "class=\"place\">".Length;
                        sigEndPlacing = taskResult.IndexOf("</p>", sigStartPlacing);

                        gameDetailed.playerOnePlacing = taskResult.Substring(sigStartPlacing, sigEndPlacing - sigStartPlacing);


                        //get date for this row -- working
                        sigStartDate = taskResult.IndexOf("class=\"date\">", sigEndGameType) + "class=\"date\">".Length;
                        sigEndDate = taskResult.IndexOf("</p>", sigStartDate);
                        date = taskResult.Substring(sigStartDate, sigEndDate - sigStartDate);


                        sigStartKd = taskResult.IndexOf("class=\"spread\">", sigEndDate) + "class=\"spread\">".Length;
                        sigEndKd = taskResult.IndexOf("</p>", sigStartKd);
                        gameDetailed.playerOneKD = taskResult.Substring(sigStartKd, sigEndKd - sigStartKd);

                        //get map for this row -- working
                        sigStartMap = taskResult.IndexOf("class=\"map\">", sigEndKd) + "class=\"map\">".Length;
                        sigEndMap = taskResult.IndexOf("</p>", sigStartMap);
                        gameDetailed.map = taskResult.Substring(sigStartMap, sigEndMap - sigStartMap);

                        //get playlist for this row
                        sigStartPlaylist = taskResult.IndexOf("class=\"playlist\">", sigEndMap) + "class=\"playlist\">".Length;
                        sigEndPlaylist = taskResult.IndexOf("</p>", sigStartPlaylist);
                        gameDetailed.playlist = taskResult.Substring(sigStartPlaylist, sigEndPlaylist - sigStartPlaylist);


                        try
                        {
                            gameDetailed.gamedate = DateTime.Parse(date); //try to parse what we think is the date, if parse fails it's not a valid gameID

                            //detailTable.Rows.Add(gameID, map, playlist, gametype, dateConvert, GT);
                            gameList.Add(gameDetailed);

                        }
                        catch
                        {

                            System.Diagnostics.Debug.WriteLine(gameID + "'s date couldn't be parsed");


                            break;
                        }


                    }
                    catch
                    {
                        x = 0;
                        break; //if parse fails before x = 25, taskResult page didn't have a full 25 games; iterate to next Task
                    }


                }


                System.Diagnostics.Debug.WriteLine("Task " + taskComplete.Id + " completed");

            }


            return gameList;

        }

    }
}

