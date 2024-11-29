using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers
{
    public class GrantItemsConsumer : IConsumer<GrantItems>
    {
        private readonly IRepository<InventoryItem> itemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;

        public GrantItemsConsumer(
                    IRepository<InventoryItem> itemsRepository,
                    IRepository<CatalogItem> catalogItemsRepository
        )
        {
            this.catalogItemsRepository = catalogItemsRepository;
            this.itemsRepository = itemsRepository;
        }

        public async Task Consume(ConsumeContext<GrantItems> context)
        {
            var message = context.Message;
            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);
            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }
            var inventoryItem = await itemsRepository.GetAsync(item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);
            if (inventoryItem is null)
            {
                inventoryItem = new InventoryItem
                {
                    UserId = message.UserId,
                    CatalogItemId = message.CatalogItemId,
                    Quantity = message.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                inventoryItem.MessageIds.Add(context.MessageId.Value);

                await itemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                if(inventoryItem.MessageIds.Contains(context.MessageId.Value)){
                    await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                    return;
                }
                inventoryItem.Quantity += message.Quantity;
                inventoryItem.MessageIds.Add(context.MessageId.Value);
                await itemsRepository.UpdateAsync(inventoryItem);
            }

            var itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
            var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            ));

            await Task.WhenAll(itemsGrantedTask, inventoryUpdatedTask);
        }
    }
}