using System.Collections.Generic;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public interface IPointcheckRepo
    {
        void AddGamesPlayed();

        MatchedGamesResult GetMatchedGames(string playerOne, string playerTwo);
        
    }
}