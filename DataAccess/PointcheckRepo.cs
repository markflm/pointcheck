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

        public async Task<string> GetEmblem(string haloGame, string playerName)
        {
            HttpClient bungie = new HttpClient();

            string profileLink = null, emblemUrlLeadUp = null, baseUrl = "http://halo.bungie.net/";
            if (haloGame == "Halo 3") 
            {
                profileLink = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player=";
                emblemUrlLeadUp = "identityStrip_EmblemCtrl_imgEmblem\" src=\"/";
            }
            else if (haloGame == "Halo 2")
            {
                profileLink = "http://halo.bungie.net/stats/playerstatshalo2.aspx?player=";
                emblemUrlLeadUp = "identityStrip_EmblemCtrl2_imgEmblem\" src=\"/";
            }
            else //must be reach
            {
                //reach logic
            }
            string emblemFullUrl = null, fullhtml = await bungie.GetStringAsync(profileLink + playerName);
            emblemFullUrl = fullhtml.Substring(fullhtml.IndexOf(emblemUrlLeadUp) + emblemUrlLeadUp.Length, //start substring at shortest unique lead of characters before image + length of lead
            (fullhtml.IndexOf(" ", fullhtml.IndexOf(emblemUrlLeadUp) + emblemUrlLeadUp.Length)) - (fullhtml.IndexOf(emblemUrlLeadUp) + emblemUrlLeadUp.Length) - 1);
            //^ dirty string work. this should probably be a regex

            emblemFullUrl = emblemFullUrl.Replace("&amp;", "&");
            return baseUrl + emblemFullUrl;
        } 
        public void AddGamesPlayed()
        {
            throw new System.NotImplementedException();
        }

        public MatchedGamesResult GetMatchedGames(string playerOne, string playerTwo)
        {
            var returnedResult = new MatchedGamesResult{playerOneName = "Kifflom",
            playerTwoName = "Infury", 
             MatchedGames = new List<Game>{
                 new Game{gameID=100, map="The Pit", gametype="CTF 3Flag", gamedate = new DateTime(2010, 2, 2)},
                 new Game{gameID=200, map="Narrows", gametype="CTF 3Flag", gamedate = new DateTime(2010, 4, 2)},
                 new Game{gameID=3400, map="Heretic", gametype="Slayer", gamedate = new DateTime(2011, 4, 2)}
             }};

             return returnedResult;
        }

        
        //need the .Single() or this.db.QuerySingle(etc.) in place of this.db.Query
        public Game DatabaseTest()
        {

            var game = this.db.Query<Game>("Select top 1 Gameid, map, playlist, gametype from GameDetails").Single();

            return game;
          
        }

       public async Task<List<Game>> scrapeH3(bool getCustoms, string playerName)
       {
            WebClient bungie = new WebClient();
            HttpClient IDDownloader = new HttpClient();
            List<Game> gameList = new List<Game>();
            List<GamePlayed> playedList = new List<GamePlayed>();

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
            
            List<Task<string>> tasks = new List<Task<string>>();
            

            List<string> taskIDandSiteLink = new List<string>();
            for (int i = 1; i <= approxNumOfPages; i++)
            {
                Uri siteLink = new Uri(matchHistoryP1 + GT + matchHistoryP2 + i); //GT = name of player, passed to method.
                                                                              //creates url like
                                                                              //http://halo.bungie.net/stats/playerstatshalo3.aspx?player=infury&ctl00_mainContent_bnetpgl_recentgamesChangePage=1

                tasks.Add(IDDownloader.GetStringAsync(siteLink));

                taskIDandSiteLink.Add(tasks.Last().Id + " " + siteLink.ToString()); //list of taskIDs and what page they should download
            }

            while (tasks.Count > 0)
            {

                var taskComplete = await Task.WhenAny(tasks);

                
                tasks.Remove(taskComplete); //remove finished task from list

                try
                {
                    taskResult = taskComplete.Result;
                }
                catch
                {
                     System.Diagnostics.Debug.WriteLine(taskComplete.Id.ToString());
                    //Debug.Print(taskComplete.Id.ToString());
                    taskComplete.Dispose();
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
                            int ix = 0;
                            //couldn't parse this date
                        }

                        foundGame.playerName = GT; foundGame.isCustom = getCustoms;
                        //foundGame.IsWin = [..]
                        //dataTable.Rows.Add(x, GT, gameID, customsFlag);
                        playedList.Add(foundGame);

                    }
                    catch
                    {
                        x = 0;
                        break; //if parse fails before x = 25, taskResult page didn't have a full 25 games iterate to next Task
                    }


                    sigMidGameID = sigEndGameID + 1; //increment index by 1 to find next instance of a GameID in the html
                    
                }
                
            }

            IDDownloader.Dispose();
/*             if (GameDetailsTable.Rows.Count == 0) //if there aren't already games in this table from a previous instance of this method
                GameDetailsTable = detailTable; //assign details table to player property since idk if you can return more than one thing per method
            else
                GameDetailsTable.Merge(detailTable); //if rowcount != 0, merge existing GameDetailsTable with one from this instance of the method */
           
            return  gameList; 
          
        }
        
        private static HttpClient _httptest = new HttpClient(); 
        public async Task<string> fuck (Uri url)
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
        public async Task<List<Game>> scrapeH2(bool getCustoms, string playerName)
       {
      
           //ServicePointManager.DefaultConnectionLimit = 90;
            WebClient bungie = new WebClient();
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

            if (getCustoms)
            {
                matchHistoryP2 = "&cus=1&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //the URL for customs
            }
            else
            {
                matchHistoryP2 = "&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //URL for MM games
            }

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

                //tasks.Add(IDDownloader.GetStringAsync(siteLink));

                //taskIDandSiteLink.Add(tasks.Last().Id + " " + siteLink.ToString()); //list of taskIDs and what page they should download
                newTasks.Add(fuck(siteLink));
            }

          
            while (newTasks.Count > 0)
            {
                //if (tasks.Count < 2) //debugging; why the fuck will this not complete for all tasks
                    //System.Diagnostics.Debug.WriteLine("last 15 tasks");

                System.Diagnostics.Debug.WriteLine("new iteration of while loop");
                
                var taskComplete = await Task.WhenAny(newTasks);

                newTasks.Remove(taskComplete); //remove finished task from list
                System.Diagnostics.Debug.WriteLine("Task " +taskComplete.Id + " finshed at " + System.DateTime.Now +" - " + newTasks.Count + " tasks remain");

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
        

        
        return  gameList; 

          
        }

        public async Task<List<Game>> scrapeHR(bool getCustoms, string playerName)
       {
            WebClient bungie = new WebClient();
            HttpClient IDDownloader = new HttpClient();
            List<Game> gameList = new List<Game>();
            List<GamePlayed> playedList = new List<GamePlayed>();


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

            matchHistoryP1 = "http://halo.bungie.net/stats/playerstatshalo2.aspx?player="; //first part of match history page string
                                                                                           //2nd part of match history page string. concatted to current page


            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + 1); //first page of GT1s game history
            sigStartGameCount = fullhtml.IndexOf("&nbsp;<strong>"); //index of first char in HTML line that gives you total MM games

            sigEndGameCount = fullhtml.IndexOf("</strong>", sigStartGameCount); //index of next char after final digit of total MM games
            //fist char + length of that substring as start index, length of characters in number of MM games as endingChar - startingChar - length of "Intro" substring = number of MM games as string
            numofGames = int.Parse(fullhtml.Substring(sigStartGameCount + "&nbsp;<strong>".Length, (sigEndGameCount - sigStartGameCount - "&nbsp;<strong>".Length)));
            approxNumOfPages = (numofGames / 25) + 1; //25 games a page, +1 to make sure a page isn't missed due to integer division
            bungie.Dispose();
            
            List<Task<string>> tasks = new List<Task<string>>();
            

            List<string> taskIDandSiteLink = new List<string>();
            for (int i = 1; i <= approxNumOfPages; i++)
            {
                Uri siteLink = new Uri(matchHistoryP1 + GT + matchHistoryP2 + i); //GT = name of player, passed to method.
                                                                              //creates url like
                                                                              //http://halo.bungie.net/stats/playerstatshalo3.aspx?player=infury&ctl00_mainContent_bnetpgl_recentgamesChangePage=1

                tasks.Add(IDDownloader.GetStringAsync(siteLink));

                taskIDandSiteLink.Add(tasks.Last().Id + " " + siteLink.ToString()); //list of taskIDs and what page they should download
            }

            while (tasks.Count > 0)
            {

                var taskComplete = await Task.WhenAny(tasks);

                
                tasks.Remove(taskComplete); //remove finished task from list

                try
                {
                    taskResult = taskComplete.Result;
                }
                catch
                {
                     System.Diagnostics.Debug.WriteLine(taskComplete.Id.ToString());
                    //Debug.Print(taskComplete.Id.ToString());
                    taskComplete.Dispose();
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
                            int ix = 0;
                            //couldn't parse this date
                        }

                        foundGame.playerName = GT; foundGame.isCustom = getCustoms;
                        //foundGame.IsWin = [..]
                        //dataTable.Rows.Add(x, GT, gameID, customsFlag);
                        playedList.Add(foundGame);

                    }
                    catch
                    {
                        x = 0;
                        break; //if parse fails before x = 25, taskResult page didn't have a full 25 games iterate to next Task
                    }


                    sigMidGameID = sigEndGameID + 1; //increment index by 1 to find next instance of a GameID in the html
                    
                }
                
               


                





            }

            IDDownloader.Dispose();
/*             if (GameDetailsTable.Rows.Count == 0) //if there aren't already games in this table from a previous instance of this method
                GameDetailsTable = detailTable; //assign details table to player property since idk if you can return more than one thing per method
            else
                GameDetailsTable.Merge(detailTable); //if rowcount != 0, merge existing GameDetailsTable with one from this instance of the method */
           
            return  gameList; 
          
        }
        
    }
}