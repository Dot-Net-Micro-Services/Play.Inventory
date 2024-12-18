using System;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using MassTransit;
using Microsoft.Extensions.Logging;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers
{
    public class SubtractItemsConsumer : IConsumer<SubtractItems>
    {
        private readonly IRepository<InventoryItem> itemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;
        private readonly ILogger<SubtractItemsConsumer> logger;

        public SubtractItemsConsumer(
                    IRepository<InventoryItem> itemsRepository,
                    IRepository<CatalogItem> catalogItemsRepository,
                    ILogger<SubtractItemsConsumer> logger)
        {
            this.catalogItemsRepository = catalogItemsRepository;
            this.itemsRepository = itemsRepository;
            this.logger = logger;
        }
        public async Task Consume(ConsumeContext<SubtractItems> context)
        {
            var message = context.Message;
            logger.LogInformation(
                "Request Received to Revoke Item {CatalogItemId} of Quantity {Quantity} for user {userId} for the purchase with CorrelationId {CorrelationId}",
                message.CatalogItemId,
                message.Quantity,
                message.UserId,
                message.CorrelationId
            );
            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);
            
            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }
            
            var inventoryItem = await itemsRepository.GetAsync(item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);
            
            if (inventoryItem is not null)
            {
                if(inventoryItem.MessageIds.Contains(context.MessageId.Value)){
                    await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                    return;
                }
                
                inventoryItem.Quantity -= message.Quantity;
                await itemsRepository.UpdateAsync(inventoryItem);

                await context.Publish(new InventoryItemUpdated(
                    inventoryItem.UserId,
                    inventoryItem.CatalogItemId,
                    inventoryItem.Quantity
                ));

            }

            await context.Publish(new InventoryItemsGranted(message.CorrelationId));
        }
    }
}