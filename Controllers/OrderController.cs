﻿using MamaFood.Data;
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
        private readonly FoodContext _context;

        public OrderController(FoodContext context)
        {
            _context = context;
        }

        private CloudTable GetTableStorageInformation(string tableName)
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
            CloudTable table = tableClient.GetTableReference(tableName);
            
            return table;
        }
        public async Task<IActionResult> Index()
        {
            CloudTable table = GetTableStorageInformation("Order");

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

        public async Task<IActionResult> Cart(int? foodID, double price, int? qty)
        {
            CloudTable orderTable = GetTableStorageInformation("Order");
            CloudTable detailTable = GetTableStorageInformation("OrderDetails");
            TableContinuationToken continuationToken;
            Order order;

            // Query Pending Order from Logged-in User
            TableQuery<Order> query = new TableQuery<Order>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, User.Identity.Name),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("OrderStatus", QueryComparisons.Equal, "Pending"))
                );

            List<Order> results = new List<Order>();
            continuationToken = null;
            do
            {
                TableQuerySegment<Order> queryResults = await orderTable.ExecuteQuerySegmentedAsync(query, continuationToken);

                continuationToken = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);

            } while (continuationToken != null);

            if (results.Count == 0) // If there is no pending order
            {
                order = new Order(User.Identity.Name);
                TableOperation insertOrder = TableOperation.Insert(order); // Add new order
                orderTable.ExecuteAsync(insertOrder).Wait();
            }
            else
            {
                order = results[0]; // Link to existing order
            }

            if (foodID != null && qty != null) // New food added
            {
                try
                {
                    // Add food into OrderDetail Table with quantity
                    OrderItem item = new OrderItem(foodID.ToString(), order.PartitionKey, price, (int)qty);
                    TableOperation insertFood = TableOperation.Insert(item);
                    detailTable.ExecuteAsync(insertFood).Wait();
                }
                catch (Exception) { }
            }
            else if (foodID != null)
            {
                try
                {
                    // Add food into OrderDetail Table without quantity (default to 1)
                    OrderItem item = new OrderItem(foodID.ToString(), order.PartitionKey, price);
                    TableOperation insertFood = TableOperation.Insert(item);
                    detailTable.ExecuteAsync(insertFood).Wait();
                }
                catch (Exception) { }
            }

            // Query All Order Details (Cart Items) from Pending Order
            TableQuery<OrderItem> detailQuery = new TableQuery<OrderItem>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, order.PartitionKey));

            List<OrderItem> orderDetails = new List<OrderItem>();
            continuationToken = null;
            do
            {
                TableQuerySegment<OrderItem> queryResults = await detailTable.ExecuteQuerySegmentedAsync(detailQuery, continuationToken);

                continuationToken = queryResults.ContinuationToken;
                orderDetails.AddRange(queryResults.Results);

            } while (continuationToken != null);

            List<OrderFoodViewModel> foods = new List<OrderFoodViewModel>();
            foreach (var d in orderDetails)
            {
                var f = _context.Food.Find(int.Parse(d.PartitionKey));
                foods.Add(new OrderFoodViewModel
                {
                    FoodModel = f,
                    OrderItemModel = d
                });
            }

            return View(foods);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string foodId, string orderId, int qty, double price)
        {
            CloudTable detailTable = GetTableStorageInformation("OrderDetails");
            TableOperation update = TableOperation.Replace(
                new OrderItem
                {
                    PartitionKey = foodId,
                    RowKey = orderId,
                    Quantity = qty,
                    UnitPrice = price,
                    ETag = "*"
                });
            await detailTable.ExecuteAsync(update);

            return RedirectToAction("Cart");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string foodId, string orderId)
        {
            CloudTable detailTable = GetTableStorageInformation("OrderDetails");
            TableOperation delete = TableOperation.Delete(
                new OrderItem
                {
                    PartitionKey = foodId,
                    RowKey = orderId,
                    ETag = "*"
                });
            await detailTable.ExecuteAsync(delete);

            return RedirectToAction("Cart");
        }
    }
}
