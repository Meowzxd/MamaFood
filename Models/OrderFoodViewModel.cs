using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Models
{
    public class OrderFoodViewModel
    {
        public Food FoodModel { get; set; }
        public OrderItem OrderItemModel { get; set; }
    }
}
