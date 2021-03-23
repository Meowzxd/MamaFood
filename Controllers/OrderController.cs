using MamaFood.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Controllers
{
    public class OrderController : Controller
    {
        private CloudTable GetTableStorageInformation()
        {
            //step 1: read json
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();

            //to get key access
            //once link, time to read the content to get the connectionstring
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(configure["ConnectionStrings:StorageConnection"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //step 2: create a new table in the storage account
            CloudTable table = tableClient.GetTableReference("Order");
            
            return table;
        }
        public async Task<IActionResult> Index()
        {
            CloudTable table = GetTableStorageInformation();

            TableQuery<Order> query = new TableQuery<Order>();

            List<Order> results = new List<Order>();
            TableContinuationToken continuationToken = null;
            do
            {
                TableQuerySegment<Order> queryResults = await table.ExecuteQuerySegmentedAsync(query, continuationToken);

                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);

            } while (continuationToken != null);

            return View(results);

            //CloudTable table = GetTableStorageInformation();

            //try
            //{

            //TableOperation retrieve = TableOperation.Retrieve<List<Order>>;
            //TableResult result = table.ExecuteAsync(retrieve).Result;
            /*if (result.Etag != null)
            {
                return View(result);
            }
            else
            {
                ViewBag.msg = "Data does not exist.";
            }*/
            //}
            //catch (Exception ex)
            //{
            //    ViewBag.msg = ex.ToString();
            //}

            //var results = GetOrders();
            //return View();
        }
    }
}
