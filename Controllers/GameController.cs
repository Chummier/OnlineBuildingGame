using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using OnlineBuildingGame.Game;

namespace OnlineBuildingGame.Controllers
{
    public class GameController : Controller
    {
        private GameWorld _world;
        private GameLogic _worldLogic;
        private readonly SignInManager<IdentityUser> _signInManager;
        private const int numOfWorlds = 5;

        [BindProperty]
        [Required]
        public int selectedWorld { get; set; }

        [BindProperty]
        public string SelectedLevel { get; set; }

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
                _world.AddPlayerToWorld(User.Identity.Name, "Main");
                 
                ViewData["TileImageList"] = _world.GetTileImages();
                ViewData["ItemImageList"] = _world.GetItemImages();
                ViewData["Username"] = User.Identity.Name;
                ViewData["InventoryRows"] = _world.InventoryRows;
                ViewData["InventoryCols"] = _world.InventoryCols;
                return View();
            }
        }

        public async Task<IActionResult> Host()
        {
            string[] resNames = new string[numOfWorlds]; 
            string[] worldNames = _world.GetPlayerWorldNames(User.Identity.Name);

            for (int i = 0; i < numOfWorlds; i++)
            {
                resNames[i] = "World " + (i+1);
            }

            for (int i = 0; i < worldNames.Length && i < numOfWorlds; i++)
            {
                resNames[i] = worldNames[i];
            }

            ViewData["PlayersWorlds"] = resNames;
            ViewData["NumberOfWorlds"] = resNames.Length;
            return View();
        }

        public async Task<IActionResult> Join()
        {
            string[] serverNames = { "Alpha", "Beta", "Charlie", "Delta", "Epsilon", "Foxtrot", "Gamma", "Houston" };
            ViewData["ServerNames"] = serverNames;
            ViewData["NumberOfServers"] = serverNames.Length;
            return View();
        }

        public async Task<IActionResult> CreateWorld()
        {
            return View();
        }

        public async Task<IActionResult> LoadWorld()
        {
            if (int.TryParse(SelectedLevel, out int selectedLevel))
            {

            }
            else
            {

            }
            return RedirectToAction("Host");
        }
    }
}
