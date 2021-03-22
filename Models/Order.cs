using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Models
{
    public class Order: TableEntity
    {
        public List<OrderItem> Items { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
