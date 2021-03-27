using System;
using MamaFood.Areas.Identity.Data;
using MamaFood.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(MamaFood.Areas.Identity.IdentityHostingStartup))]
namespace MamaFood.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<MamaFoodContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("MamaFoodContextConnection")));

                services.AddDefaultIdentity<MamaFoodUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<MamaFoodContext>();
            });
        }
    }
}