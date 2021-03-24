using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MamaFood.Data;
using MamaFood.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace MamaFood.Views.Foods
{
    public class FoodsController : Controller
    {
        private readonly FoodContext _context;

        public FoodsController(FoodContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Food.ToListAsync());
        }

        public async Task<IActionResult> Menu()
        {
            return View(await _context.Food.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var food = await _context.Food
                .FirstOrDefaultAsync(m => m.ID == id);
            if (food == null)
            {
                return NotFound();
            }

            return View(food);
        }

        public IActionResult Create()
        {
            return View();
        }

        private CloudBlobContainer getBlobStorageInformation()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();

            CloudStorageAccount objectaccount = CloudStorageAccount.Parse(configure["ConnectionStrings:StorageConnection"]);
            CloudBlobClient blobclient = objectaccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobclient.GetContainerReference("mamafood-blobcontainer");
            return container;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,ImageFile,FoodName,FoodType,Price")] Food food)
        {
            if (ModelState.IsValid)
            {
                CloudBlobContainer container = getBlobStorageInformation();
                CloudBlockBlob blob = container.GetBlockBlobReference(food.ImageFile.FileName);
                using (var fileStream = food.ImageFile.OpenReadStream())
                {
                    blob.UploadFromStreamAsync(fileStream).Wait();
                }

                food.FoodImage = blob.Uri.ToString();

                _context.Add(food);
                await _context.SaveChangesAsync();

                return RedirectToAction("Menu", "Blobs");
            }
            return View(food);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var food = await _context.Food.FindAsync(id);
            if (food == null)
            {
                return NotFound();
            }
            return View(food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,ImageFile,FoodImage,FoodName,FoodType,Price")] Food food)
        {
            if (id != food.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    CloudBlobContainer container = getBlobStorageInformation();
                    CloudBlockBlob blob = container.GetBlockBlobReference(food.ImageFile.FileName);
                    using (var fileStream = food.ImageFile.OpenReadStream())
                    {
                        blob.UploadFromStreamAsync(fileStream).Wait();
                    }

                    food.FoodImage = blob.Uri.ToString();

                    _context.Update(food);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FoodExists(food.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(food);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var food = await _context.Food
                .FirstOrDefaultAsync(m => m.ID == id);
            if (food == null)
            {
                return NotFound();
            }

            return View(food);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var food = await _context.Food.FindAsync(id);

            CloudBlobContainer container = getBlobStorageInformation();
            CloudBlockBlob deletedblob = container.GetBlockBlobReference(food.FoodImage.Substring(food.FoodImage.LastIndexOf("/") + 1));

            deletedblob.DeleteIfExistsAsync().Wait();

            _context.Food.Remove(food);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FoodExists(int id)
        {
            return _context.Food.Any(e => e.ID == id);
        }
    }
}
