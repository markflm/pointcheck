using System.Collections.Generic;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public interface ICommanderRepo
    {
        IEnumerable<Command> GetAllCommands();

        Command GetCommandById(int id);

    }
}