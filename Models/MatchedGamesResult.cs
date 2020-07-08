using System.Collections.Generic;

namespace pointcheck_api.Models
{
    public class MatchedGamesResult
    {
        // k/d is total kills - total deaths in ranked games
        // w/l is wins/losses in ranked games
        // the head to head versions of those metrics are calculated from games where player one and player two were on different teams
        public string playerOneName { get; set; }
        
        public string playerTwoName { get; set; }
       
        public List<Game> MatchedGames {get; set;}
        


    }
}