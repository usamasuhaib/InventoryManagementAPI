using InventoryManagementAPI.Data;
using InventoryManagementAPI.DTOs;
using InventoryManagementAPI.Models;
using InventoryManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IWarehouseService _warehouseService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(IInventoryService inventoryService, IWarehouseService warehouseService, IHttpContextAccessor httpContextAccessor, AppDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _inventoryService = inventoryService;
            _warehouseService = warehouseService;
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _userManager = userManager;
        }


        [HttpGet("GetInventoryItems")]
        public async Task<IActionResult> GetInventoryItems()
        {
            try
            {
                var tenantId = Request.Headers["TenantId"].ToString();
                //var tenantId = "tenant2";

                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest("Tenant ID is missing");
                }

                var items = await _dbContext.InventoryItems
                    .FromSqlRaw(@"SELECT Id, Name, CAST(Price AS decimal(18,2)) AS Price, Quantity, Description, Category, TenantId 
                  FROM InventoryItems 
                  WHERE TenantId = {0}", tenantId)
                    .ToListAsync();
                
                return Ok(items);
            }
            catch (Exception ex)
            {
                // Optionally log the exception
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryItem>> GetInventoryItem(int id)
        {
            var item = await _inventoryService.GetInventoryItemByIdAsync(id);
            if (item == null)
                return NotFound();
            return Ok(item);
        }

        [HttpPost("CreateInventoryItem")]
        public async Task<ActionResult<InventoryItem>> CreateInventoryItem([FromBody] InventoryItemDto inventoryItemDto)
        {
            if (ModelState.IsValid)
            {
                var tenantId = Request.Headers["TenantId"].ToString();

                // Map DTO to entity
                var inventoryItem = new InventoryItem
                {
                    Name = inventoryItemDto.Name,
                    Description = inventoryItemDto.Description,
                    Price = inventoryItemDto.Price,
                    Quantity = inventoryItemDto.Quantity,
                    Category = inventoryItemDto.Category,
                    TenantId = tenantId
                };

                // Add association with warehouses
                foreach (var warehouseId in inventoryItemDto.WarehouseIds)
                {
                    var warehouse = await _warehouseService.GetWarehouseByIdAsync(warehouseId);
                    if (warehouse != null)
                    {
                        inventoryItem.Warehouses.Add(warehouse);
                    }
                    else
                    {
                        return NotFound($"Warehouse with ID {warehouseId} not found.");
                    }
                }

                var createdItem = await _inventoryService.CreateInventoryItemAsync(inventoryItem);

                return CreatedAtAction(nameof(GetInventoryItem), new { id = createdItem.Id }, createdItem);
            }

            return BadRequest(ModelState);
        }

        private string GetCurrentTenantId()
        {
            return _httpContextAccessor.HttpContext?.Items["TenantId"] as string ?? "default_tenant";
        }


        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateInventoryItem(int id, InventoryItem item)
        {
            if (id != item.Id)
                return BadRequest();

            var updatedItem = await _inventoryService.UpdateInventoryItemAsync(item);
            if (updatedItem == null)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteInventoryItem(int id)
        {
            var success = await _inventoryService.DeleteInventoryItemAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetInventoryItemsByCategory(string category)
        {
            var items = await _inventoryService.GetInventoryItemsByCategoryAsync(category);
            return Ok(items);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> SearchInventoryItemsByName(string name)
        {
            var items = await _inventoryService.SearchInventoryItemsByNameAsync(name);
            return Ok(items);
        }
    }
}
