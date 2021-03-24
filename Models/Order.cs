using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Models
{
    public class Order : TableEntity
    {
        public Order() { }
        public Order(string custName)
        {
            this.PartitionKey = Guid.NewGuid().ToString();
            this.RowKey = custName;
            this.OrderStatus = "Pending";
        }
        public string OrderStatus { get; set; }
    }
}
