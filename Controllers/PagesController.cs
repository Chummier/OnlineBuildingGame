using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace OnlineBuildingGame.Controllers
{
    public class PagesController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        [Required]
        public string Name { get; set; }
        [BindProperty]
        [Required]
        public string Password { get; set; }

        public PagesController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Main", "Game");
            } else
            {
                var users = from u in _userManager.Users.ToList()
                            select u.UserName;
                ViewData["Users"] = users.ToList();
                return View();
            }
            
        }

        [HttpPost]
        public async Task<IActionResult> Login()
        { 
            var res = await _signInManager.PasswordSignInAsync(Name, Password, isPersistent: false, lockoutOnFailure: false);
            if (res.Succeeded)
            {
                return RedirectToAction("Main", "Game");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Register()
        {
            IdentityUser newUser = new IdentityUser { UserName = Name };
            var res = await _userManager.CreateAsync(newUser, Password);
            
            if (res.Succeeded)
            {
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                return RedirectToAction("Main", "Game");
            }
            return RedirectToAction("Index");
        }
    }
}
