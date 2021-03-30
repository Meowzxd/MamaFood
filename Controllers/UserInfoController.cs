using MamaFood.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Controllers
{
    public class UserInfoController : Controller
    {
        private UserManager<MamaFoodUser> UserManager;

        public UserInfoController(UserManager<MamaFoodUser>usrMgr)
        {
            UserManager = usrMgr;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index(string Search)
        {
            var user = from u in UserManager.Users // Full list
                       select u;
            if (!String.IsNullOrEmpty(Search)) // If got any search keyword
            {
                user = user.Where(s => s.UserName.Contains(Search)); // Filter
            }
            return View(user.ToList());
        }
    }
}
