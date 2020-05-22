using System.Collections.Generic;
using pointcheck_api.Models;

namespace pointcheck_api.DataAccess
{
    public class MockCommanderRepo : ICommanderRepo
    {
        public IEnumerable<Command> GetAllCommands()
        {
            var commands = new List<Command>
            {
                new Command{id=0, howTo="i dunno", line="idk",platform="windows"},
                new Command{id=1, howTo="i do know", line="id2k",platform="linux"},
                new Command{id=2, howTo="turn it off", line="begin",platform="xbox"}

            };
            
        return commands;
        }

        public Command GetCommandById(int id)
        {
            return new Command{id=0, howTo="i dunno", line="idk",platform="windows"};
        }
    }
}