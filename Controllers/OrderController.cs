using MamaFood.Data;
using MamaFood.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamaFood.Controllers
{
    public class OrderController : Controller
    {
        private readonly FoodContext _context;
        private static readonly string QueueName = "MamaFoodQueue";
        private static IConfigurationRoot configure;

        public OrderController(FoodContext context)
        {
            _context = context;
            configure = GetAppConfiguration();
        }

        private CloudTable GetTableStorageInformation(string tableName)
        {
            //step 1: read json to get key access
            //once link, time to read the content to get the connectionstring
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(configure["ConnectionStrings:StorageConnection"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //step 2: create a new table in the storage account
            CloudTable table = tableClient.GetTableReference(tableName);
            
            return table;
        }

        private static IConfigurationRoot GetAppConfiguration()
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();

            return configure;
        }

        [Authorize(Roles = "Admin")]
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

            results = results.OrderByDescending(i => i.Timestamp).ToList();

            return View(results);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cart(int? foodID, double price, int? qty)
        {
            var managementClient = new ManagementClient(configure["ConnectionStrings:ServiceBusConnection"]);
            var queue = await managementClient.GetQueueRuntimeInfoAsync(QueueName);
            ViewBag.MessageCount = queue.MessageCount;

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

            ViewBag.orderId = order.PartitionKey; // Store order ID for assignment of params in View

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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult> CheckOut(string orderId)
        {
            Order order = new Order
            {
                PartitionKey = orderId,
                RowKey = User.Identity.Name
            };

            QueueClient queue = new QueueClient(GetAppConfiguration()["ConnectionStrings:ServiceBusConnection"], QueueName);
            if (ModelState.IsValid)
            {
                var orderJSON = JsonConvert.SerializeObject(order);
                var message = new Message(Encoding.UTF8.GetBytes(orderJSON))
                {
                    MessageId = Guid.NewGuid().ToString(),
                    ContentType = "application/json"
                };
                await queue.SendAsync(message);

                // Update order status in Table Storage
                CloudTable orderTable = GetTableStorageInformation("Order");
                order.ETag = "*";
                order.OrderStatus = "Confirmed";
                TableOperation update = TableOperation.Replace(order);
                await orderTable.ExecuteAsync(update);
            }

            return RedirectToAction("Cart");
        }

        private static async Task CreateQueueFunctionAsync()
        {
            var managementClient = new ManagementClient(GetAppConfiguration()["ConnectionStrings:ServiceBusConnection"]);
            bool queueExists = await managementClient.QueueExistsAsync(QueueName);
            if (!queueExists)
            {
                QueueDescription qd = new QueueDescription(QueueName)
                {
                    MaxSizeInMB = 1024,
                    MaxDeliveryCount = 3
                };
                await managementClient.CreateQueueAsync(qd);
            }
        }

        public static void Initialize()
        {
            CreateQueueFunctionAsync().GetAwaiter().GetResult();
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> OrderApproval()
        {
            // Connect to the same queue
            var managementClient = new ManagementClient(configure["ConnectionStrings:ServiceBusConnection"]);
            var queue = await managementClient.GetQueueRuntimeInfoAsync(QueueName);

            List<Order> messages = new List<Order>();
            List<long> sequence = new List<long>();

            // Collect the sequence number and each message content from the queue
            MessageReceiver messageReceiver = new MessageReceiver(configure["ConnectionStrings:ServiceBusConnection"], QueueName);
            for (int i = 0; i < queue.MessageCount; i++)
            {
                Message message = await messageReceiver.PeekAsync();
                Order confirmedOrder = JsonConvert.DeserializeObject<Order>(Encoding.UTF8.GetString(message.Body));
                sequence.Add(message.SystemProperties.SequenceNumber);
                messages.Add(confirmedOrder);
            }

            // Bring all the collected information to the frontend page
            ViewBag.sequence = sequence;
            ViewBag.messages = messages;
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Approve(long sequence, string status)
        {
            // Connect to the same queue
            var managementClient = new ManagementClient(configure["ConnectionStrings:ServiceBusConnection"]);
            var queue = await managementClient.GetQueueRuntimeInfoAsync(QueueName);

            // Receive the selected message
            MessageReceiver messageReceiver = new MessageReceiver(configure["ConnectionStrings:ServiceBusConnection"], QueueName);
            for (int i = 0; i < queue.MessageCount; i++)
            {
                Message message = await messageReceiver.ReceiveAsync();
                string token = message.SystemProperties.LockToken;

                // To find the selected message - read and remove from the queue
                if (message.SystemProperties.SequenceNumber == sequence)
                {
                    Order confirmedOrder = JsonConvert.DeserializeObject<Order>(Encoding.UTF8.GetString(message.Body));
                    
                    // Complete the message
                    await messageReceiver.CompleteAsync(token);

                    // Update order status in Table Storage
                    CloudTable orderTable = GetTableStorageInformation("Order");
                    confirmedOrder.ETag = "*";
                    confirmedOrder.OrderStatus = status;
                    TableOperation update = TableOperation.Replace(confirmedOrder);
                    await orderTable.ExecuteAsync(update);
                    break;
                }
            }
            return RedirectToAction("OrderApproval");
        }
    }
}
