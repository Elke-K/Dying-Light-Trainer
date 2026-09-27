using DyingLightTrainer;
using Memory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dying_light_trainer
{
    public partial class InventoryManager : Window
    {
        public static long moduleBaseEngine;

        public bool scanbuttonPressed = false;

        public int oldQuantity = 0;
        public int oldRepairs = 0;
        public float oldDurability = 0;

        public byte oldRarity = 13;

        public int selectedWeaponIndex = 0;

        public InventoryManager()
        {
            InitializeComponent();

            moduleBaseEngine = DyingLight1.m.GetModuleAddressByName("engine_x64_rwdi.dll");

            LoadInventoryWeaponList();
            LoadInventoryComboBox();
            LoadUpgradeBoxes();
        }

        public void LoadInventoryComboBox()
        {
            Mem m = DyingLight1.m;

            string chain = $"{moduleBaseEngine:X}+A3F5F8,208,A0,90,0,8E0,60,40";
            long inventoryItemsPointerVal = m.ReadLong(chain, "");
            IntPtr inventoryItemsPointer = (IntPtr)inventoryItemsPointerVal;

            Debug.WriteLine(inventoryItemsPointer.ToString("X"));

            IntPtr[] inventoryItems = new IntPtr[24];
            IntPtr[] inventoryItemsDesc = new IntPtr[24];
            IntPtr[] inventoryItemsStrings = new IntPtr[24];
            IntPtr[] inventoryItemsStringPointers = new IntPtr[24];
            int[] inventoryItemsLength = new int[24];
            IntPtr[] inventoryItemsLengthPointers = new IntPtr[24];
            IntPtr[] inventoryItemsSlotFieldAddress = new IntPtr[24];

            // Get inventory item pointers
            for (int i = 0; i < 24; i++)
            {
                IntPtr slotAddress = inventoryItemsPointer + (i * 0x8);
                long itemPointerVal = m.ReadLong(slotAddress.ToInt64().ToString("X"), "");
                inventoryItems[i] = (IntPtr)itemPointerVal;
            }

            // Get item descriptions
            for (int i = 0; i < inventoryItems.Length; i++)
            {
                if (inventoryItems[i] == IntPtr.Zero)
                    continue;

                IntPtr item = inventoryItems[i] + 0x60;
                long itemPointerVal = m.ReadLong(item.ToInt64().ToString("X"), "");

                inventoryItemsDesc[i] = (IntPtr)itemPointerVal;
                inventoryItemsSlotFieldAddress[i] = item;
            }

            // Get string pointer + address of the pointer field
            for (int i = 0; i < inventoryItemsDesc.Length; i++)
            {
                if (inventoryItemsDesc[i] == IntPtr.Zero)
                    continue;

                IntPtr stringPointerAddress = inventoryItemsDesc[i] + 0x18;
                long stringPointerVal = m.ReadLong(stringPointerAddress.ToInt64().ToString("X"), "");

                inventoryItemsStringPointers[i] = stringPointerAddress;
                inventoryItemsStrings[i] = (IntPtr)stringPointerVal;
            }

            // Get string length + address of length field
            for (int i = 0; i < inventoryItemsDesc.Length; i++)
            {
                if (inventoryItemsDesc[i] == IntPtr.Zero)
                    continue;

                IntPtr lengthAddress = inventoryItemsDesc[i] + 0x20;
                int length = m.ReadInt(lengthAddress.ToInt64().ToString("X"), "");

                inventoryItemsLengthPointers[i] = lengthAddress;
                inventoryItemsLength[i] = length;
            }

            Item.WeaponsInventorydl1.Clear();

            for (int i = 0; i < inventoryItemsStrings.Length; i++)
            {
                IntPtr stringPointer = inventoryItemsStrings[i];
                IntPtr stringPointerAddress = inventoryItemsStringPointers[i];

                int length = inventoryItemsLength[i];
                IntPtr lengthAddress = inventoryItemsLengthPointers[i];

                if (stringPointer == IntPtr.Zero ||
                    stringPointerAddress == IntPtr.Zero ||
                    lengthAddress == IntPtr.Zero ||
                    length <= 0 ||
                    length >= 64)
                {
                    continue;
                }

                string weaponId = m.ReadString(stringPointer.ToInt64().ToString("X"), "", length, true, null);

                string weaponName = "Unknown";

                foreach (Item weapon in Item.WeaponsListdl1)
                {
                    if (weapon.ItemId == weaponId)
                    {
                        weaponName = weapon.ItemName;
                        break;
                    }
                }

                Item newWeaponInv = new Item(
                    inventoryItems[i],
                    inventoryItemsDesc[i],
                    inventoryItemsSlotFieldAddress[i],
                    weaponName,
                    weaponId,
                    "weapon",
                    true,
                    "dl1"
                );

                Debug.WriteLine(newWeaponInv.BasePointer.ToString("X"));
            }

            weapons_inventory.ItemsSource = null;
            weapons_inventory.ItemsSource = Item.WeaponsInventorydl1;
            weapons_inventory.SelectedIndex = selectedWeaponIndex;
            weapons_inventory2.ItemsSource = null;
            weapons_inventory2.ItemsSource = Item.WeaponsInventorydl1;
            weapons_inventory2.SelectedIndex = selectedWeaponIndex;
        }

        public void LoadInventoryWeaponList()
        {
            string weaponsPath = Path.Combine(AppContext.BaseDirectory, "Common.txt");

            if (!File.Exists(weaponsPath))
            {
                MessageBox.Show("Common.txt not found. Make sure to include it next to the exe!");
                return;
            }

            string[] weaponsFile = File.ReadAllLines(weaponsPath);
            List<Item> weapons = new List<Item>();

            Item.WeaponsListdl1.Clear();

            foreach (string line in weaponsFile)
            {
                string[] weapon = line.Split("=");
                string weaponName = weapon[1];
                string weaponId = weapon[0];

                Item newWeapon = new Item(weaponName, weaponId, "weapon_list", false, "dl1");
                weapons.Add(newWeapon);
            }

            foreach ((IntPtr weaponPointer, string weaponId) in DyingLight1.ItemDescMeleeList)
            {
                Item sameWeapon = weapons.Find(w => w.ItemId == weaponId + "_N");

                string weaponName = "Unknown";

                if (sameWeapon != null)
                {
                    weaponName = sameWeapon.ItemName;
                }

                if (!Item.WeaponsListdl1.Any(w => w.ItemId == weaponId))
                {
                    Item newWeapon = new Item(weaponPointer, weaponPointer, weaponName, weaponId, "weapon_list", true, "dl1");
                }

                Debug.WriteLine(weaponPointer);
            }

            weapons_list.ItemsSource = null;
            weapons_list.ItemsSource = Item.WeaponsListdl1;
            weapons_list.SelectedIndex = 0;
        }

        private void ChangeWeapon(object sender, RoutedEventArgs e)
        {
            Mem m = DyingLight1.m;

            if (scanbuttonPressed)
            {
                lbltext.Text = "The inventory manager is still loading!";
                return;
            }

            Item weaponFromList = ((Item)weapons_list.SelectedItem);
            Item weaponFromInventory = ((Item)weapons_inventory.SelectedItem);

            if (weapons_inventory.SelectedItem == null || weapons_list.SelectedItem == null)
            {
                MessageBox.Show("Unable to change weapon. Either none is selected or the game is not running.");
                return;
            }

            if (!Item.WeaponsInventorydl1.Contains(weaponFromInventory))
            {
                MessageBox.Show("Unable to find this weapon inside inventory.");
                return;
            }

            byte[] valueToWrite = BitConverter.GetBytes(weaponFromList.ItemPointer.ToInt64());

            long before = m.ReadLong(weaponFromInventory.SlotFieldAddress.ToInt64().ToString("X"), "");
            m.WriteBytes(weaponFromInventory.SlotFieldAddress.ToInt64().ToString("X"), valueToWrite);
            long after = m.ReadLong(weaponFromInventory.SlotFieldAddress.ToInt64().ToString("X"), "");

            Debug.WriteLine($"target: {weaponFromList.ItemPointer.ToInt64()}, before: {before}, after: {after}");

            LoadInventoryWeaponList();
            LoadInventoryComboBox();
        }

        private void CloseInventoryManager(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RefreshInventory(object sender = null, RoutedEventArgs e = null)
        {
            LoadInventoryWeaponList();
            LoadInventoryComboBox();
            SetWeaponAttributesUI();
        }

        private async void OnScanButtonClick(object sender, RoutedEventArgs e)
        {
            scanbuttonPressed = true;
            if (Process.GetProcessesByName("DyingLightGame").Length <= 0)
            {
                MessageBox.Show("Dying Light is not running!");
                scanbuttonPressed = false;
                return;
            }

            DyingLight1.ItemDescCraftplanList.Clear();
            DyingLight1.ItemDescMeleeList.Clear();

            lbltext.Text = "Scanning...";

            await DyingLight1.LoadVTableInformation();

            LoadInventoryWeaponList();
            LoadInventoryComboBox();

            lbltext.Text = "Scan successful!";
            scanbuttonPressed = false;
        }

        private void LoadUpgradeBoxes()
        {
            string itemsPath = Path.Combine(AppContext.BaseDirectory, "Common.txt");

            if (!File.Exists(itemsPath))
            {
                MessageBox.Show("Common.txt not found. Make sure to include it next to the exe!");
                return;
            }

            string[] itemsFile = File.ReadAllLines(itemsPath);
            List<Item> items = new List<Item>();

            Item.CraftplanListdl1.Clear();

            Item noItem = new Item("None", "", "craftplan", false, "dl1") { ItemPointer = IntPtr.Zero };
            Item.CraftplanListdl1.Add(noItem);

            foreach (string line in itemsFile)
            {
                string[] item = line.Split("=");
                string itemName = item[1];
                string itemId = item[0];

                Item newItem = new Item(itemName, itemId, "craftplan", false, "dl1");
                items.Add(newItem);
            }

            foreach ((IntPtr itemPointer, string itemId) in DyingLight1.ItemDescCraftplanList)
            {
                Item sameItem = items.Find(w => w.ItemId == itemId + "_N");
                string itemName = sameItem != null ? sameItem.ItemName : "Unknown";

                if (!Item.CraftplanListdl1.Any(w => w.ItemId == itemId))
                {
                    Item newItem = new Item(itemPointer, itemPointer, itemName, itemId, "craftplan", true, "dl1");
                }
            }

            foreach ((IntPtr itemPointer, string itemId) in DyingLight1.ItemDescCraftUpgradesList)
            {
                Item sameItem = items.Find(w => w.ItemId == itemId + "_N");
                string itemName = sameItem != null ? sameItem.ItemName : "Unknown";

                if (!Item.CraftupgradesListdl1.Any(w => w.ItemId == itemId))
                {
                    Item newItem = new Item(itemPointer, itemPointer, itemName, itemId, "upgrade", true, "dl1");
                }
            }

            modification_box.ItemsSource = null;
            modification_box.ItemsSource = Item.CraftplanListdl1;
            modification_box.SelectedIndex = 0;

            upgrade_box1.ItemsSource = null;
            upgrade_box1.ItemsSource = Item.CraftplanListdl1;
            upgrade_box1.SelectedIndex = 0;

            upgrade_box2.ItemsSource = null;
            upgrade_box2.ItemsSource = Item.CraftplanListdl1;
            upgrade_box2.SelectedIndex = 0;

            upgrade_box3.ItemsSource = null;
            upgrade_box3.ItemsSource = Item.CraftplanListdl1;
            upgrade_box3.SelectedIndex = 0;

            upgrade_box4.ItemsSource = null;
            upgrade_box4.ItemsSource = Item.CraftplanListdl1;
            upgrade_box4.SelectedIndex = 0;
        }

        private void UpdateWeaponAttributesUI(object sender, SelectionChangedEventArgs e)
        {
            SetWeaponAttributesUI();
        }



        private void SetWeaponAttributesUI()
        {
            Mem m = DyingLight1.m;
            Item currentlySelectedWeapon = (Item)weapons_inventory2.SelectedItem;

            if (currentlySelectedWeapon == null)
            {
                return;
            }

            long basePtr = currentlySelectedWeapon.BasePointer.ToInt64();

            float durability = m.ReadFloat((basePtr + 0x44).ToString("X"), "", false);
            int quantity = m.ReadInt((basePtr + 0x40).ToString("X"), "");
            int repairsUsed = m.ReadInt((basePtr + 0x48).ToString("X"), "");
            byte quality = (byte)(m.ReadInt((basePtr + 0x68).ToString("X"), "") & 0xFF);

            oldDurability = durability;
            oldRepairs = repairsUsed;
            oldQuantity = quantity;
            oldRarity = quality;

            long modificationPointer = m.ReadLong((basePtr + 0x78).ToString("X"), "");
            int length = m.ReadInt((modificationPointer + 0x20).ToString("X"), "");
            long modStringPtr = m.ReadLong((modificationPointer + 0x18).ToString("X"), "");
            string selectedModification = m.ReadString(modStringPtr.ToString("X"), "", length, true, null);

            long itemdescCraftplanPointer = m.ReadLong((basePtr + 0x88).ToString("X"), "");
            long craftplan1Pointer = m.ReadLong((itemdescCraftplanPointer + 0x0).ToString("X"), "");
            long craftplan2Pointer = m.ReadLong((itemdescCraftplanPointer + 0x8).ToString("X"), "");
            long craftplan3Pointer = m.ReadLong((itemdescCraftplanPointer + 0x10).ToString("X"), "");
            long craftplan4Pointer = m.ReadLong((itemdescCraftplanPointer + 0x18).ToString("X"), "");

            string item1Id = "";
            string item2Id = "";
            string item3Id = "";
            string item4Id = "";

            Item noItem = Item.CraftplanListdl1.FirstOrDefault(i => i.ItemId == "")
              ?? new Item("None", "", "craftplan", false, "dl1");

            if (craftplan1Pointer == 0) upgrade_box1.SelectedItem = noItem;
            if (craftplan2Pointer == 0) upgrade_box2.SelectedItem = noItem;
            if (craftplan3Pointer == 0) upgrade_box3.SelectedItem = noItem;
            if (craftplan4Pointer == 0) upgrade_box4.SelectedItem = noItem;

            if (craftplan1Pointer != 0)
            {
                int box1Length = m.ReadInt((craftplan1Pointer + 0x20).ToString("X"), "");
                long strPtr = m.ReadLong((craftplan1Pointer + 0x18).ToString("X"), "");
                item1Id = m.ReadString(strPtr.ToString("X"), "", box1Length, true, null);
            }
            if (craftplan2Pointer != 0)
            {
                int box2Length = m.ReadInt((craftplan2Pointer + 0x20).ToString("X"), "");
                long strPtr = m.ReadLong((craftplan2Pointer + 0x18).ToString("X"), "");
                item2Id = m.ReadString(strPtr.ToString("X"), "", box2Length, true, null);
            }
            if (craftplan3Pointer != 0)
            {
                int box3Length = m.ReadInt((craftplan3Pointer + 0x20).ToString("X"), "");
                long strPtr = m.ReadLong((craftplan3Pointer + 0x18).ToString("X"), "");
                item3Id = m.ReadString(strPtr.ToString("X"), "", box3Length, true, null);
            }
            if (craftplan4Pointer != 0)
            {
                int box4Length = m.ReadInt((craftplan4Pointer + 0x20).ToString("X"), "");
                long strPtr = m.ReadLong((craftplan4Pointer + 0x18).ToString("X"), "");
                item4Id = m.ReadString(strPtr.ToString("X"), "", box4Length, true, null);
            }

            quantity_box.Text = quantity.ToString();
            durabiility_box.Text = durability.ToString();
            repairs_box.Text = repairsUsed.ToString();

            foreach (ComboBoxItem item in rarity_box.Items)
            {
                if (item.Tag.ToString() == quality.ToString())
                {
                    rarity_box.SelectedItem = item;
                    break;
                }
            }

            foreach (Item item in modification_box.Items)
            {
                if (item.ItemId == selectedModification)
                    modification_box.SelectedItem = item;
            }

            foreach (Item item in upgrade_box1.Items)
                if (item.ItemId == item1Id) upgrade_box1.SelectedItem = item;

            foreach (Item item in upgrade_box2.Items)
                if (item.ItemId == item2Id) upgrade_box2.SelectedItem = item;

            foreach (Item item in upgrade_box3.Items)
                if (item.ItemId == item3Id) upgrade_box3.SelectedItem = item;

            foreach (Item item in upgrade_box4.Items)
                if (item.ItemId == item4Id) upgrade_box4.SelectedItem = item;
        }

        private void UpdateWeapon(object sender, RoutedEventArgs e)
        {
            Mem m = DyingLight1.m;
            Item currentlySelectedWeapon = (Item)weapons_inventory2.SelectedItem;
            if (currentlySelectedWeapon == null) return;

            long basePtr = currentlySelectedWeapon.BasePointer.ToInt64();

            ComboBoxItem rarityItem = rarity_box.SelectedItem as ComboBoxItem;
            Item modification = modification_box.SelectedItem as Item;
            Item craftslot1 = upgrade_box1.SelectedItem as Item;
            Item craftslot2 = upgrade_box2.SelectedItem as Item;
            Item craftslot3 = upgrade_box3.SelectedItem as Item;
            Item craftslot4 = upgrade_box4.SelectedItem as Item;

            int quantity = TestInt(quantity_box.Text) ? Convert.ToInt32(quantity_box.Text) : oldQuantity;
            int repairs = TestInt(repairs_box.Text) ? Convert.ToInt32(repairs_box.Text) : oldRepairs;
            float durability = TestFloat(durabiility_box.Text) ? Convert.ToSingle(durabiility_box.Text) : oldDurability;

            byte rarity = oldRarity;
            if (rarityItem != null && TestByte(rarityItem.Tag?.ToString()))
            {
                byte parsedRarity = Convert.ToByte(rarityItem.Tag);
                if (parsedRarity >= 8 && parsedRarity <= 13)
                    rarity = parsedRarity;
            }

            // 1. Write Weapon Stats
            m.WriteMemory((basePtr + 0x44).ToString("X"), "float", durability.ToString(), "");
            m.WriteMemory((basePtr + 0x40).ToString("X"), "int", quantity.ToString(), "");
            m.WriteMemory((basePtr + 0x48).ToString("X"), "int", repairs.ToString(), "");
            m.WriteBytes((basePtr + 0x68).ToString("X"), new byte[] { rarity });

            // 2. Write Modification Pointer
            if (modification != null)
            {
                long targetModification = basePtr + 0x78;
                byte[] modBytes = BitConverter.GetBytes(modification.ItemPointer.ToInt64());
                m.WriteBytes(targetModification.ToString("X"), modBytes);
            }

            // 3. Write Craftplan Upgrades
            long craftplanBase = m.ReadLong((basePtr + 0x88).ToString("X"), "");

            if (craftplanBase != 0)
            {
                if (craftslot1 != null)
                    m.WriteBytes((craftplanBase + 0x00).ToString("X"), BitConverter.GetBytes(craftslot1.ItemPointer.ToInt64()));

                if (craftslot2 != null)
                    m.WriteBytes((craftplanBase + 0x08).ToString("X"), BitConverter.GetBytes(craftslot2.ItemPointer.ToInt64()));

                if (craftslot3 != null)
                    m.WriteBytes((craftplanBase + 0x10).ToString("X"), BitConverter.GetBytes(craftslot3.ItemPointer.ToInt64()));

                if (craftslot4 != null)
                    m.WriteBytes((craftplanBase + 0x18).ToString("X"), BitConverter.GetBytes(craftslot4.ItemPointer.ToInt64()));

                if (craftslot1.ItemName == "None")
                    m.WriteBytes((craftplanBase + 0x00).ToString("X"), BitConverter.GetBytes(0));

                if (craftslot2.ItemName == "None")
                    m.WriteBytes((craftplanBase + 0x08).ToString("X"), BitConverter.GetBytes(0));

                if (craftslot3.ItemName == "None")
                    m.WriteBytes((craftplanBase + 0x10).ToString("X"), BitConverter.GetBytes(0));

                if (craftslot4.ItemName == "None")
                    m.WriteBytes((craftplanBase + 0x18).ToString("X"), BitConverter.GetBytes(0));
            }

            selectedWeaponIndex = weapons_inventory2.SelectedIndex;

            RefreshInventory();
        }

        public bool TestInt(string value) => int.TryParse(value, out _);
        public bool TestFloat(string value) => float.TryParse(value, out _);
        public bool TestByte(string value) => byte.TryParse(value, out _);
    }
}