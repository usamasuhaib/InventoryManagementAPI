using InventoryManagementAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementAPI.DTOs
{
    public class InventoryItemDto
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        public string Category { get; set; }

        [Required]
        public List<int> WarehouseIds { get; set; }

    }
}
