using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public class MockPointcheckRepo : IPointcheckRepo
    {
        public void AddGamesPlayed()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetEmblem(string haloGame, string playerName)
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

          public Game DatabaseTest()
        {
   
      
          throw new System.NotImplementedException();

          
        }

        public Task<List<Game>> scrapeH3(bool getCustoms, string playerName)
                {
   
      
          throw new System.NotImplementedException();

          
        }

        public Task<List<Game>> scrapeH2(bool getCustoms, string playerName)
                {
   
      
          throw new System.NotImplementedException();

          
        }
        public Task<List<Game>> scrapeHR(bool getCustoms, string playerName)
                {
   
      
          throw new System.NotImplementedException();

          
        }
    }
}