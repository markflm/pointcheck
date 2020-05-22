namespace pointcheck_api.Models
{
    public class GamePlayed
    {
        public string playerName { get; set; }
        public int gameID { get; set; }
        public bool isCustom { get; set; }
        public bool IsWin { get; set; }

        public int KillDeath { get; set; }

    }
}