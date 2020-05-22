using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using pointcheck_api.DataAccess;
using pointcheck_api.Models;

namespace pointcheck_api.Controllers
{
    [Route("api/commands")]
    [ApiController] //provides some 'default behaviors' out of the box
    public class CommandsController : ControllerBase //inherit from controllerbase instead of controller bc base doesn't implement view support
    {
        private readonly ICommanderRepo _repository;

        public CommandsController(ICommanderRepo repository) //this repository is being Injected from the service container, that's how I can use an interface as an argument 
        {
                _repository = repository;
        }

        [HttpGet] //GET api/commands
        public ActionResult <IEnumerable<Command>> GetAllCommands()
        {
                var commandItems = _repository.GetAllCommands();

                return Ok(commandItems);

        }

        [HttpGet("{id}")] //get by id: api/commands/{id}
        public ActionResult <Command> GetCommandById(int id)
        {
            var commandItem = _repository.GetCommandById(id);

            return Ok(commandItem);
        }
    }
}