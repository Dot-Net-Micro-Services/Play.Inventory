using System;

namespace Play.Inventory.Service.Exceptions;

[Serializable]
internal class UnknownItemException : Exception
{
    private Guid ItemId;

    public UnknownItemException(Guid ItemId) : base($"Unknow Item '{ItemId}'")
    {
        this.ItemId = ItemId;
    }
}