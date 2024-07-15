namespace InventoryManagementAPI.Models
{
    public class Warehouse : EntityBase
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public ICollection<InventoryItem> InventoryItems { get; set; }
    }

}
