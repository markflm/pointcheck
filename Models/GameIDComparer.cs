
using System.Collections.Generic;
using pointcheck_api.Models;

public class GameIDComparer: IEqualityComparer<Game>
{
    public bool Equals (Game x, Game y)
    {
        return x.gameID == y.gameID;
    }

    public int GetHashCode(Game obj)
    {
        return obj.gameID.GetHashCode();
    }

}