public static class SlotTransfer
{
    // Both slots are validated before either is changed.
    public static bool TryTransfer(InventorySlot source, InventorySlot target,
        ItemData expectedItem, int expectedAmount, ItemData targetItem, int targetAmount, int amount)
    {
        if (source == null || target == null || ReferenceEquals(source, target) ||
            source.IsEmpty || source.item != expectedItem || source.amount != expectedAmount ||
            target.item != targetItem || target.amount != targetAmount || amount < 1 || amount > source.amount)
            return false;

        if (!target.IsEmpty)
        {
            if (amount != source.amount)
                return false;
            ItemData item = source.item;
            int count = source.amount;
            source.item = target.item;
            source.amount = target.amount;
            target.item = item;
            target.amount = count;
            return true;
        }

        if (amount > expectedItem.MaxStack)
            return false;
        target.item = expectedItem;
        target.amount = amount;
        source.amount -= amount;
        if (source.amount == 0)
            source.Clear();
        return true;
    }
}
