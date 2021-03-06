using System.Collections.Generic;
using System.Threading.Tasks;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public interface IPointcheckRepo
    {
        void AddGamesPlayed();

        MatchedGamesResult GetMatchedGames(string playerOne, string playerTwo);
        
        Task<string> GetEmblem(string haloGame, string playerName);

        int CorruptedCount();
        Game DatabaseTest();

        bool IsInDbH3();

        bool IsInDbH2();

        bool IsInDbHR();

        Task<List<Game>> ScrapeH3(bool getCustoms, string playerName);

        Task<List<Game>> ScrapeH2(bool getCustoms, string playerName);

        Task<List<Game>> ScrapeHR(bool getCustoms, string playerName);
    }
}