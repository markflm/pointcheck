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

    }
}