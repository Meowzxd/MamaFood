using MamaFood.Areas.Identity.Data;
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
        public IActionResult Index()
        {
            return View(UserManager.Users);
        }
    }
}
