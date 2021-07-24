using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineBuildingGame.Controllers
{
    public class GameController : Controller
    {
        private readonly GameWorld _world;

        public GameController(GameWorld world)
        {
            _world = world;
        }

        public IActionResult Main()
        {
            ViewData["World"] = _world.getWorld();
            return View();
        }
    }
}
