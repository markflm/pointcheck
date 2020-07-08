using System;

namespace pointcheck_api.Models
{
    public class Game
    {
        public int gameID { get; set; }
        public string map { get; set; }
        public string playlist { get; set; }
        public string gametype { get; set; }
        public DateTime gamedate { get; set; }

        public string playerOnePlacing {get; set;}

        public string playerTwoPlacing {get; set;}
        
        public string playerOneKD {get; set;}

        public string playerTwoKD {get; set;}

    }
}