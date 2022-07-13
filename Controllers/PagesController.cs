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

        public async Task<bool> LoginOrRegister(string name, string password)
        {
            var resSM = await _signInManager.PasswordSignInAsync(name, password, isPersistent: false, lockoutOnFailure: false);
            if (resSM.Succeeded)
            {
                return true;
            } else
            {
                IdentityUser newUser = new IdentityUser { UserName = name };
                var resUM = await _userManager.CreateAsync(newUser, password);
                if (resUM.Succeeded)
                {
                    await _signInManager.SignInAsync(newUser, isPersistent: false);
                    return true;
                } else
                {
                    return false;
                }
            }
        }

        public async Task<IActionResult> Host()
        {
            var res = await LoginOrRegister(Name, Password);
            if (res)
            {
                return RedirectToAction("Host", "Game");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Join()
        {
            var res = await LoginOrRegister(Name, Password);
            if (res)
            {
                return RedirectToAction("Join", "Game");
            } else
            {
                return RedirectToAction("Index");
            }
        }
    }
}
