using System;

namespace Play.Inventory.Service.Dtos
{
    public record GrantItemsDto(Guid UserId, Guid CatalogItemId, int Quantity);
    public record InventoryItemDto(Guid CatalogItemId, string name, string description, int Quantity, DateTimeOffset AcquiredDate);
    public record CatalogItemDto(Guid Id, string name, string description);
}