using System;
using System.Threading.Tasks;
using MassTransit;
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

        public SubtractItemsConsumer(
                    IRepository<InventoryItem> itemsRepository,
                    IRepository<CatalogItem> catalogItemsRepository
        )
        {
            this.catalogItemsRepository = catalogItemsRepository;
            this.itemsRepository = itemsRepository;
        }
        public async Task Consume(ConsumeContext<SubtractItems> context)
        {
            var message = context.Message;
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