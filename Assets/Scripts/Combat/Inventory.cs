using System.Collections.Generic;

namespace TPRPG
{
    public class Inventory
    {
        public class Entry
        {
            public ItemData item;
            public int amount;
        }

        public readonly List<Entry> entries = new List<Entry>();

        public Inventory(InventoryData data)
        {
            if (data == null) return;
            foreach (var s in data.items)
            {
                if (s == null || s.item == null) continue;
                var e = Find(s.item);
                if (e != null) e.amount += s.amount;
                else entries.Add(new Entry { item = s.item, amount = s.amount });
            }
        }

        Entry Find(ItemData item)
        {
            foreach (var e in entries) if (e.item == item) return e;
            return null;
        }

        public int Amount(ItemData item)
        {
            var e = Find(item);
            return e != null ? e.amount : 0;
        }

        public bool HasAny()
        {
            foreach (var e in entries) if (e.amount > 0) return true;
            return false;
        }

        public bool Consume(ItemData item)
        {
            var e = Find(item);
            if (e == null || e.amount <= 0) return false;
            e.amount--;
            return true;
        }

        public void Refund(ItemData item)
        {
            var e = Find(item);
            if (e != null) e.amount++;
            else entries.Add(new Entry { item = item, amount = 1 });
        }
    }
}
