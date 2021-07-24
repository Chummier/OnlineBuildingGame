using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login()
        { 
            if (ModelState.IsValid)
            {
                var res = await _signInManager.PasswordSignInAsync(Name, Password, isPersistent: false, lockoutOnFailure: false);
                if (res.Succeeded)
                {
                    return RedirectToRoute("Game/Main");
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Register()
        {
            if (ModelState.IsValid)
            {
                IdentityUser newUser = new IdentityUser { UserName = Name };
                var res = await _userManager.CreateAsync(newUser, Password);

                if (res.Succeeded)
                {
                    await _signInManager.SignInAsync(newUser, isPersistent: false);
                    return RedirectToRoute("Game/Main");
                }
            }
            return RedirectToAction("Index");
        }
    }
}
