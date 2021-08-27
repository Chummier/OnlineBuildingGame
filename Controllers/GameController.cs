using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using OnlineBuildingGame.Game;
using OnlineBuildingGame.Hubs;
using OnlineBuildingGame.Data;
using OnlineBuildingGame.Models;

namespace OnlineBuildingGame.Controllers
{
    public class GameController : Controller
    {
        private GameWorld _world;
        private GameLogic _worldLogic;
        private readonly SignInManager<IdentityUser> _signInManager;

        public GameController(GameWorld world, GameLogic worldLogic, SignInManager<IdentityUser> signInManager)
        {
            _world = world;
            _worldLogic = worldLogic;
            _signInManager = signInManager;
        }
        
        public async Task<IActionResult> Main()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Pages");
            } else
            {
                _world.LoginPlayer(User.Identity.Name);

                ViewData["TileImageList"] = _world.GetTileImages();
                ViewData["ItemImageList"] = _world.GetItemImages();
                ViewData["Username"] = User.Identity.Name;
                ViewData["InventoryRows"] = _world.InventoryRows;
                ViewData["InventoryCols"] = _world.InventoryCols;
                return View();
            }
        }
    }
}
