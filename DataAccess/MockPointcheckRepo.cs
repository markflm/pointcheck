using System.Collections.Generic;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public class MockPointcheckRepo : IPointcheckRepo
    {
        public void AddGamesPlayed()
        {
            throw new System.NotImplementedException();
        }

        public MatchedGamesResult GetMatchedGames(string playerOne, string playerTwo)
        {
            throw new System.NotImplementedException();
        }
    }
}