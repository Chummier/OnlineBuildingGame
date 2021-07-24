using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineBuildingGame.Game;

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
            ViewData["WorldDim"] = _world.getSize();
            return View();
        }
    }
}
