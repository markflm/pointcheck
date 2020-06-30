namespace pointcheck_api.Models
{
    public class GamePlayed
    {
        public string playerName { get; set; }
        public int gameID { get; set; }
        public bool isCustom { get; set; }
        public int placing { get; set; }

        public int KillDeath { get; set; }

    }
}