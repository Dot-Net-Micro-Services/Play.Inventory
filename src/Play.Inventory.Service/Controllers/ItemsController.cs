using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private const string AdminRole = "Admin";
        private readonly IRepository<InventoryItem> itemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;
        private readonly IPublishEndpoint publishEndpoint;

        public ItemsController(
            IRepository<InventoryItem> itemsRepository, 
            IRepository<CatalogItem> cataLogItemsRepository, 
            IPublishEndpoint publishEndpoint
        )
        {
            this.itemsRepository = itemsRepository;
            this.catalogItemsRepository = cataLogItemsRepository;
            this.publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if(Guid.Parse(currentUserId) != userId || User.IsInRole(AdminRole))
            {
                return Forbid();
            }

            var inventoryItemEntities = await itemsRepository.GetAllAsync(item => item.UserId == userId);
            var itemIds = inventoryItemEntities.Select(item  => item.CatalogItemId);
            var catalogItemEntities = await catalogItemsRepository.GetAllAsync(item => itemIds.Contains(item.Id));
            
            var inventoryItems = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItems);
        }

        [HttpPost]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItems)
        {
            var inventoryItem = await itemsRepository.GetAsync(item => item.UserId == grantItems.UserId && item.CatalogItemId == grantItems.CatalogItemId);
            if (inventoryItem is null)
            {
                inventoryItem = new InventoryItem
                {
                    UserId = grantItems.UserId,
                    CatalogItemId = grantItems.CatalogItemId,
                    Quantity = grantItems.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await itemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItems.Quantity;
                await itemsRepository.UpdateAsync(inventoryItem);
            }

            await publishEndpoint.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            ));


            return Ok();
        }
    }
}