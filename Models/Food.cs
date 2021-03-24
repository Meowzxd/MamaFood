using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MamaFood.Models
{
    public class Food
    {
        public int ID { get; set; }
        
        [Required]
        [Display(Name = "Food Image")]
        [NotMapped]
        public IFormFile FoodImage { get; set; }

        [StringLength(60, ErrorMessage = "The food name should be between 3 to 60 characters!", MinimumLength = 3)]
        [Required]
        [Display(Name = "Food Name")]
        public string FoodName { get; set; }

        [RegularExpression(@"^[A-Z]+[a-zA-Z""'\s-]*$", ErrorMessage = "Food type should start with capital letter")]
        [Required]
        [Display(Name = "Food Type")]
        public string FoodType { get; set; }

        [Range(1, 100)]
        [Column(TypeName = "decimal(18,3)")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }
    }
}
