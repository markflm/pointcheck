using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using pointcheck_api.DataAccess;
using pointcheck_api.Models;

namespace pointcheck_api.Controllers
{

    [Route("api/pointcheck")]
    [ApiController] //provides some 'default behaviors' out of the box
    public class PointcheckController : ControllerBase
    {

        private readonly IPointcheckRepo _repository;
        private readonly GameIDComparer _comparer;

        public PointcheckController(IPointcheckRepo repository)
        {
            _repository = repository;
            _comparer = new GameIDComparer();
        }

        [HttpGet] //GET api/pointcheck; default route - to be deleted
        public ActionResult<IEnumerable<MatchedGamesResult>> GetResults()
        {
           
            return NotFound();

        }

        [HttpGet("db")] //GET api/pointcheck
        public ActionResult<IEnumerable<Game>> GetResultsFromDB()
        {

            var result = _repository.DatabaseTest();
            System.Diagnostics.Debug.WriteLine(result);
            return Ok(result);

        }

        [HttpGet("scrape/H2/{names}")] //GET api/pointcheck
        public async Task<ActionResult<List<Game>>> ScrapeH2(string names)
        {

            string[] players = names.Split("&");
            List<Game> playerOneGames = new List<Game>();
            List<Game> playerTwoGames = new List<Game>();

            string playerOne = players[0]; //gamertag before the & in http req - spaces seem to work by default?
            string playerTwo = players[1];

            MatchedGamesResult resultObj = new MatchedGamesResult(); //object to be returned from the endpoint

            resultObj.playerOneEmblem = await _repository.GetEmblem("Halo 2", playerOne);  //link to the service record emblem
            resultObj.playerTwoEmblem = await _repository.GetEmblem("Halo 2", playerTwo);

            if (resultObj.playerOneEmblem == null)
            {
                resultObj.note = playerOne + " has no Bungie.net games for Halo 2";
                return NotFound(resultObj); //if either playerOne's name isn't a legit GT for that game
            }
            else if (resultObj.playerTwoEmblem == null)
            {
                resultObj.note = playerTwo + " has no Bungie.net games for Halo 2";
                return NotFound(resultObj); //if either playerTwo's name isn't a legit GT for that game
            }


            System.Diagnostics.Debug.WriteLine("Players received: " + playerOne + " " + playerTwo + " " + System.DateTime.Now);

            //var reader = new StreamReader(Request.Body); //read request's json body

            //string reqBody= await reader.ReadToEndAsync(); - don't need to read body; no customs for H2


            System.Diagnostics.Debug.WriteLine("Getting " + playerOne + "'s H2 games " + System.DateTime.Now);

            playerOneGames = await _repository.ScrapeH2(getCustoms: false, playerName: playerOne); //get mm games

            if (_repository.CorruptedCount() > 50)
                resultObj.note += (playerOne + "has " + _repository.CorruptedCount() + " corrupted games. consider re-running ");


            System.Diagnostics.Debug.WriteLine("Getting " + playerTwo + "'s H2 games " + System.DateTime.Now);

            playerTwoGames = await _repository.ScrapeH2(getCustoms: false, playerName: playerTwo); //get mm games

            if (_repository.CorruptedCount() > 50)
                resultObj.note += (playerTwo + "has " + _repository.CorruptedCount() + " corrupted games. consider re-running ");

            System.Diagnostics.Debug.WriteLine("Filtering game lists to find common gameIDs " + System.DateTime.Now);
            resultObj.MatchedGames = playerOneGames.Intersect(playerTwoGames, _comparer).ToList();

            string gamesMatchBaseUrl = "https://halo.bungie.net/Stats/GameStatsHalo2.aspx?gameid="; //different for each game
            var final = from game in resultObj.MatchedGames
                        join p2game in playerTwoGames on game.gameID equals p2game.gameID
                        select new Game
                        {
                            gameUrl = gamesMatchBaseUrl + game.gameID,
                            gameID = game.gameID,
                            map = game.map,
                            playlist = game.playlist,
                            gametype = game.gametype,
                            gamedate = game.gamedate,
                            playerOnePlacing = game.playerOnePlacing,
                            playerTwoPlacing = p2game.playerOnePlacing
                        };

            resultObj.MatchedGames = final.ToList();

            resultObj.playerOneName = playerOne; resultObj.playerTwoName = playerTwo;

            resultObj.MatchedGames.Sort((x, y) => DateTime.Compare(y.gamedate, x.gamedate));
            System.Diagnostics.Debug.WriteLine("sending resultObj" + System.DateTime.Now);

            return Ok(resultObj);

        }

        [HttpGet("scrape/H3/{names}")] //GET api/pointcheck/scrape/H3/[name1&name2]
        public async Task<ActionResult<List<Game>>> ScrapeH3(string names)
        {
            //need to refactor H3 scrape in repository to handle customs in one call like HR does
            bool getCustoms;
            string[] players = names.Split("&");

            List<Game> playerOneGames = new List<Game>();
            List<Game> playerTwoGames = new List<Game>();

            string playerOne = players[0]; //gamertag before the & in http req
            string playerTwo = players[1];

            MatchedGamesResult resultObj = new MatchedGamesResult(); //object to be returned from the endpoint

            resultObj.playerOneEmblem = await _repository.GetEmblem("Halo 3", playerOne);  //link to the service record emblem
            resultObj.playerTwoEmblem = await _repository.GetEmblem("Halo 3", playerTwo);

            if (resultObj.playerOneEmblem == null)
            {
                resultObj.note = playerOne + " has no Bungie.net games for Halo 3";
                return NotFound(resultObj); //if either playerOne's name isn't a legit GT for that game
            }
            else if (resultObj.playerTwoEmblem == null)
            {
                resultObj.note = playerTwo + " has no Bungie.net games for Halo 3";
                return NotFound(resultObj); //if either playerOne's name isn't a legit GT for that game
            }

            System.Diagnostics.Debug.WriteLine("Players received: " + playerOne + " " + playerTwo + " " + System.DateTime.Now);

            var reader = new StreamReader(Request.Body); //read request's json body

            string reqBody = await reader.ReadToEndAsync();
            getCustoms = reqBody.Contains("\"getCustoms\":true"); //if json request body includes getCustoms:true
                                                                  //getCustoms = false; //take out

            System.Diagnostics.Debug.WriteLine("Getting " + playerOne + "'s H3 MM games " + System.DateTime.Now);

            playerOneGames = await _repository.ScrapeH3(getCustoms: false, playerName: playerOne); //get mm games

            if (_repository.CorruptedCount() > 50)
                resultObj.note += (playerOne + " has " + _repository.CorruptedCount() + " corrupted games. consider re-running ");

            if (getCustoms)
            {
                System.Diagnostics.Debug.WriteLine("Getting " + playerOne + "'s H3 Custom games " + System.DateTime.Now);

                var result = await _repository.ScrapeH3(getCustoms: true, playerName: playerOne); //get custom games if requested
                playerOneGames.AddRange(result); //append customs to list

            }

            System.Diagnostics.Debug.WriteLine("Getting " + playerTwo + "'s H3 MM games " + System.DateTime.Now);

            playerTwoGames = await _repository.ScrapeH3(getCustoms: false, playerName: playerTwo); //get mm games

            if (_repository.CorruptedCount() > 50)
                resultObj.note += (playerTwo + " has " + _repository.CorruptedCount() + " corrupted games. consider re-running ");

            if (getCustoms)
            {
                System.Diagnostics.Debug.WriteLine("Getting " + playerTwo + "'s H3 Custom games " + System.DateTime.Now);

                var result = await _repository.ScrapeH3(getCustoms: true, playerName: playerTwo); //get custom games if requested
                playerTwoGames.AddRange(result); //append customs to list

            }

            System.Diagnostics.Debug.WriteLine("Filtering game lists to find common gameIDs " + System.DateTime.Now);
            resultObj.MatchedGames = playerOneGames.Intersect(playerTwoGames, _comparer).ToList();

            string gamesMatchBaseUrl = "https://halo.bungie.net/Stats/GameStatsHalo3.aspx?gameid="; //different for each game
            var final = from game in resultObj.MatchedGames
                        join p2game in playerTwoGames on game.gameID equals p2game.gameID
                        select new Game //get games both players share
                        {
                            gameUrl = gamesMatchBaseUrl + game.gameID,
                            gameID = game.gameID,
                            map = game.map,
                            playlist = game.playlist,
                            gametype = game.gametype,
                            gamedate = game.gamedate,
                            playerOnePlacing = game.playerOnePlacing,
                            playerTwoPlacing = p2game.playerOnePlacing
                        };

            resultObj.MatchedGames = final.ToList();

            resultObj.playerOneName = playerOne; resultObj.playerTwoName = playerTwo;

            resultObj.MatchedGames.Sort((x, y) => DateTime.Compare(y.gamedate, x.gamedate));
            System.Diagnostics.Debug.WriteLine("sending resultObj" + System.DateTime.Now);

            return Ok(resultObj);

        }

        [HttpGet("scrape/HR/{names}")] //GET api/pointcheck/scrape/HR/[name1&name2]
        public async Task<ActionResult<List<Game>>> ScrapeHR(string names)
        {
            bool getCustoms;
            string[] players = names.Split("&");

            List<Game> playerOneGames = new List<Game>();
            List<Game> playerTwoGames = new List<Game>();

            string playerOne = players[0]; //gamertag before the & in http req
            string playerTwo = players[1];

            MatchedGamesResult resultObj = new MatchedGamesResult(); //object to be returned from the endpoint
            //emblem check doubles as "does this guy exist?" check
            resultObj.playerOneEmblem = await _repository.GetEmblem("Halo Reach", playerOne);  //link to the service record emblem
            resultObj.playerTwoEmblem = await _repository.GetEmblem("Halo Reach", playerTwo);

            if (resultObj.playerOneEmblem == null)
            {
                resultObj.note = playerOne + " has no Bungie.net games for Halo Reach";
                return NotFound(resultObj); //if either playerOne's name isn't a legit GT for that game
            }
            else if (resultObj.playerTwoEmblem == null)
            {
                resultObj.note = playerTwo + " has no Bungie.net games for Halo Reach";
                return NotFound(resultObj); //if either playerOne's name isn't a legit GT for that game
            }
            System.Diagnostics.Debug.WriteLine("Players received: " + playerOne + " " + playerTwo + " " + System.DateTime.Now);

            var reader = new StreamReader(Request.Body); //read request's json body

            string reqBody = await reader.ReadToEndAsync();
            getCustoms = reqBody.Contains("\"getCustoms\":true"); //if json request body includes getCustoms:true
            //getCustoms = false; //take out

            System.Diagnostics.Debug.WriteLine("Getting " + playerOne + "'s HR games " + System.DateTime.Now);

            playerOneGames = await _repository.ScrapeHR(getCustoms, playerOne);
            if (_repository.CorruptedCount() > 50)
                resultObj.note += (playerOne + "has " + _repository.CorruptedCount() + " corrupted games. consider re-running ");



            System.Diagnostics.Debug.WriteLine("Getting " + playerTwo + "'s HR games " + System.DateTime.Now);

            playerTwoGames = await _repository.ScrapeHR(getCustoms, playerTwo);

            if (_repository.CorruptedCount() > 50)
                resultObj.note += (playerTwo + "has " + _repository.CorruptedCount() + " corrupted games. consider re-running");


            System.Diagnostics.Debug.WriteLine("Filtering game lists to find common gameIDs " + System.DateTime.Now);
            resultObj.MatchedGames = playerOneGames.Intersect(playerTwoGames, _comparer).ToList();

            string gamesMatchBaseUrl = "https://halo.bungie.net/Stats/Reach/GameStats.aspx?gameid="; //different for each game
            var final = from game in resultObj.MatchedGames
                        join p2game in playerTwoGames on game.gameID equals p2game.gameID
                        select new Game
                        {
                            gameUrl = gamesMatchBaseUrl + game.gameID,
                            gameID = game.gameID,
                            map = game.map,
                            playlist = game.playlist,
                            gametype = game.gametype,
                            gamedate = game.gamedate,
                            playerOnePlacing = game.playerOnePlacing,
                            playerTwoPlacing = p2game.playerOnePlacing,
                            playerOneKD = game.playerOneKD,
                            playerTwoKD = p2game.playerOneKD
                        };

            resultObj.MatchedGames = final.ToList();
            resultObj.MatchedGames.Sort((x, y) => DateTime.Compare(y.gamedate, x.gamedate)); //order games by date desc
            resultObj.playerOneName = playerOne; resultObj.playerTwoName = playerTwo;

            System.Diagnostics.Debug.WriteLine("sending resultObj" + System.DateTime.Now);

            return Ok(resultObj);
        }
    }





}