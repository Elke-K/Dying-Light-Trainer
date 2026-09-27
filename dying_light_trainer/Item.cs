using System;
using System.Collections.Generic;
using System.Text;

namespace dying_light_trainer
{
    public class Item
    {
        public IntPtr BasePointer { get; set; }
        public IntPtr ItemPointer { get; set; }
        public IntPtr SlotFieldAddress { get; set; } = IntPtr.Zero;  // NEW
        public string ItemName { get; set; }
        public string ItemId { get; set; }

        public string DisplayName => $"{ItemName} - {ItemId}";

        public static List<Item> WeaponsListdl1 = new List<Item>();
        public static List<Item> WeaponsInventorydl1 = new List<Item>();
        public static List<Item> WeaponsListdl2 = new List<Item>();
        public static List<Item> WeaponsInventorydl2 = new List<Item>();
        public static List<Item> CraftplanListdl1 = new List<Item>();
        public static List<Item> CraftplanListdl2 = new List<Item>();

        public static List<Item> CraftupgradesListdl1 = new List<Item>();
        public static List<Item> CraftupgradesListdl2 = new List<Item>();

        public Item(string temName, string itemId, string itemType, bool addToList, string game)
        {
            ItemName = temName;
            ItemId = itemId;

            if (addToList)
            {

                if (itemType == "weapon_list" && game == "dl1")
                {

                    WeaponsListdl1.Add(this);
                }
                else if (itemType == "weapon" && game == "dl1")
                {
                    WeaponsInventorydl1.Add(this);
                }
                else if (itemType == "craftplan" && game == "dl1" || itemType == "upgrade" && game == "dl1")
                {
                    CraftplanListdl1.Add(this);
                }

                if (itemType == "weapon_list" && game == "dl2")
                {

                    WeaponsListdl2.Add(this);
                }
                else if (itemType == "weapon" && game == "dl2")
                {
                    WeaponsInventorydl2.Add(this);
                }
                else if (itemType == "craftplan" && game == "dl2" || itemType == "upgrade" && game == "dl2")
                {
                    CraftplanListdl2.Add(this);
                }
            }

        }
        public Item(IntPtr basePointer, IntPtr itemPointer, string itemName, string itemId, string itemType, bool addToList, string game)
        {
            BasePointer = basePointer;
            ItemPointer = itemPointer;
            ItemName = itemName;
            ItemId = itemId;

            if (addToList)
            {
                if (itemType == "weapon_list" && game == "dl1")
                {

                    WeaponsListdl1.Add(this);
                }
                else if (itemType == "weapon" && game == "dl1")
                {
                    WeaponsInventorydl1.Add(this);
                }
                else if (itemType == "craftplan" && game == "dl1" || itemType == "upgrade" && game == "dl1")
                {
                    CraftplanListdl1.Add(this);
                }

                if (itemType == "weapon_list" && game == "dl2")
                {

                    WeaponsListdl2.Add(this);
                }
                else if (itemType == "weapon" && game == "dl2")
                {
                    WeaponsInventorydl2.Add(this);
                }
                else if (itemType == "craftplan" && game == "dl2" || itemType == "upgrade" && game == "dl2")
                {
                    CraftplanListdl2.Add(this);
                }
            }

        }

        public Item(IntPtr basePointer, IntPtr itemPointer, IntPtr slotFieldAddress, string itemName, string itemId, string itemType, bool addToList, string game)
        {
            BasePointer = basePointer;
            ItemPointer = itemPointer;
            SlotFieldAddress = slotFieldAddress;
            ItemName = itemName;
            ItemId = itemId;

            if (addToList)
            {
                if (itemType == "weapon_list" && game == "dl1")
                {

                    WeaponsListdl1.Add(this);
                }
                else if (itemType == "weapon" && game == "dl1")
                {
                    WeaponsInventorydl1.Add(this);
                }
                else if (itemType == "craftplan" && game == "dl1" || itemType == "upgrade" && game == "dl1")
                {
                    CraftplanListdl1.Add(this);
                }
                if (itemType == "weapon_list" && game == "dl2")
                {

                    WeaponsListdl2.Add(this);
                }
                else if (itemType == "weapon" && game == "dl2")
                {
                    WeaponsInventorydl2.Add(this);
                }
                else if (itemType == "craftplan" && game == "dl2" || itemType == "upgrade" && game == "dl2")
                {
                    CraftplanListdl2.Add(this);
                }

                
            }
        }
    }
}