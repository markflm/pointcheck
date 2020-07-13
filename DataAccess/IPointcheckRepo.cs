using System.Collections.Generic;
using System.Threading.Tasks;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public interface IPointcheckRepo
    {
        void AddGamesPlayed(string player, string gameName, List<Game> gamesList);

        MatchedGamesResult GetMatchedGames(string playerOne, string playerTwo);
        
        Task<string> GetEmblem(string haloGame, string playerName);

        int CorruptedCount();
        Game DatabaseTest();

        PlayerStoredResult IsInDbH3(string player, string game);

        PlayerStoredResult IsInDbH2(string player, string game);

        PlayerStoredResult IsInDbHR(string player, string game);

        Task<List<Game>> ScrapeH3(bool getCustoms, string playerName);

        Task<List<Game>> ScrapeH2(bool getCustoms, string playerName);

        Task<List<Game>> ScrapeHR(bool getCustoms, string playerName);
    }
}