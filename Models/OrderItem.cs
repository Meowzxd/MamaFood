using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Models
{
    public class OrderItem : TableEntity
    {
        public OrderItem() { }
        public OrderItem(string foodId, string orderId, double price, int qty = 1)
        {
            this.PartitionKey = foodId;
            this.RowKey = orderId;
            this.UnitPrice = price;
            this.Quantity = qty;
        }
        public double UnitPrice { get; set; }
        public int Quantity { get; set; }

    }
}
