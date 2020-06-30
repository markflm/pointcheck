using System.Collections.Generic;
using System.Threading.Tasks;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public interface IPointcheckRepo
    {
        void AddGamesPlayed();

        MatchedGamesResult GetMatchedGames(string playerOne, string playerTwo);
        
        Game DatabaseTest();

        Task<List<Game>> scrapeH3(bool getCustoms, string playerName);

        Task<List<Game>> scrapeH2(bool getCustoms, string playerName);

        Task<List<Game>> scrapeHR(bool getCustoms, string playerName);
    }
}