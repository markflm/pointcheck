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

        public PointcheckController(IPointcheckRepo repository){
            _repository = repository;
            _comparer = new GameIDComparer();
        }

        [HttpGet] //GET api/pointcheck
        public ActionResult <IEnumerable<MatchedGamesResult>> GetResults()
        {
                var result = _repository.GetMatchedGames("Kifflom","Infury");

                return Ok(result);

        }

        [HttpGet("db")] //GET api/pointcheck
        public ActionResult <IEnumerable<Game>> GetResultsFromDB()
        {
            
                var result = _repository.DatabaseTest();
                System.Diagnostics.Debug.WriteLine(result);
                return Ok(result);

        }

        [HttpGet("scrape/{names}")] //GET api/pointcheck
        public async Task<ActionResult<List<Game>>> scrape(string names)
        {
            bool getCustoms;
            string[] players = names.Split("&");
            List<Game> playerOneGames = new List<Game>();
            List<Game> playerTwoGames = new List<Game>();
           
            string playerOne = players[0]; //gamertag before the & in http req
            string playerTwo = players[1];
            MatchedGamesResult resultObj = new MatchedGamesResult(); //object to be returned from the endpoint

            System.Diagnostics.Debug.WriteLine("Players received: " + playerOne +" " + playerTwo + " " + System.DateTime.Now);
            
                var reader = new StreamReader(Request.Body); //read request's json body

                string reqBody= await reader.ReadToEndAsync();
                getCustoms = reqBody.Contains("\"getCustoms\":true"); //if json request body includes getCustoms:true
                getCustoms = false;

                System.Diagnostics.Debug.WriteLine("Getting " + playerOne + "'s games "  + System.DateTime.Now);

                playerOneGames = await _repository.scrapeH2(getCustoms: false, playerName: playerOne); //get mm games

            if (getCustoms)
            {
                var result = await _repository.scrapeH2(getCustoms:true, playerName: playerOne); //get custom games if requested
                playerOneGames.AddRange(result); //append customs to list

            }

            System.Diagnostics.Debug.WriteLine("Getting " + playerTwo + "'s games "  + System.DateTime.Now); 

                playerTwoGames = await _repository.scrapeH2(getCustoms: false, playerName: playerTwo).ConfigureAwait(false); //get mm games

            System.Diagnostics.Debug.WriteLine("Filtering game lists to find common gameIDs " + System.DateTime.Now); 
                resultObj.MatchedGames = playerOneGames.Intersect(playerTwoGames, _comparer).ToList();

                System.Diagnostics.Debug.WriteLine(System.DateTime.Now);

            var final = from game in resultObj.MatchedGames
                        join p2game in playerTwoGames on game.gameID  equals p2game.gameID
                        select new Game { gameID = game.gameID, map = game.map, playlist = game.playlist, gametype = game.gametype,
                                     gamedate = game.gamedate, playerOnePlacing = game.playerOnePlacing, playerTwoPlacing = p2game.playerOnePlacing};

        resultObj.MatchedGames = final.ToList();                    

        System.Diagnostics.Debug.WriteLine(final); 

  
/*                  for (int i = 0, x = 0; i < resultObj.MatchedGames.Count; i++)
                {
                    if (resultObj.MatchedGames[i].gameID == playerTwoGames[x].gameID)
                        resultObj.MatchedGames[i].playerTwoPlacing = playerTwoGames[x].playerOnePlacing;
                }  */
        System.Diagnostics.Debug.WriteLine(System.DateTime.Now);
                return Ok(resultObj);

        }

    }
}