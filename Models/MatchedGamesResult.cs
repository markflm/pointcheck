using System.Collections.Generic;

namespace pointcheck_api.Models
{
    public class MatchedGamesResult
    {

        // the head to head versions of those metrics are calculated from games where player one and player two were on different teams
        public string playerOneName { get; set; }
        public string playerOneEmblem {get; set;}
        public string playerTwoName { get; set; }
        public string playerTwoEmblem {get; set;}
        public List<Game> MatchedGames {get; set;}
        
        public string note {get; set;}


    }
}