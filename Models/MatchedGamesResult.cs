using System.Collections.Generic;

namespace pointcheck_api.Models
{
    public class MatchedGamesResult
    {
        public string playerOneName { get; set; }
        public int playerOneKd { get; set; }
        public double playerOneWinPercent { get; set; }

        public string playerTwoName { get; set; }
        public int playerTwoKd { get; set; }
        public double playerTwoWinPercent { get; set; }
        public List<Game> MatchedGames {get; set;}


    }
}