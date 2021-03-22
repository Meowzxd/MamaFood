using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Models
{
    public class OrderItem
    {
        public OrderItem(string id, int qty = 1)
        {
            this.FoodID = id;
            this.Quantity = qty;
        }
        public string FoodID { get; set; }
        public int Quantity { get; set; }
    }
}
