using Memory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dying_light_trainer
{
    /// <summary>
    /// Interaction logic for InventoryManager2.xaml
    /// </summary>
    /// 

    public partial class InventoryManager2 : Window
    {
        public static List<Item> itemDescMeleesDl2 = new List<Item>();

        public static Mem m = DyingLight2.m;

        public static long moduleBase;
        public InventoryManager2()
        {
            InitializeComponent();

            Loaded += InventoryManager2_Loaded;
        }

        private async void InventoryManager2_Loaded(object sender, RoutedEventArgs e)
        {
            m.OpenProcess("DyingLightGame_x64_rwdi.exe");
            moduleBase = m.GetModuleAddressByName("gamedll_ph_x64_rwdi.dll");

            // 1. Wait for weapons list to finish scanning & populating itemDescMeleesDl2
            await LoadWeaponsList();

            // 2. Now load inventory weapons using the fully populated list
            await LoadInventoryWeapons();
        }


        public async Task LoadWeaponsList()
        {
            string common2Path = Path.Combine(AppContext.BaseDirectory, "Common2.txt");
            if (!File.Exists(common2Path))
            {
                MessageBox.Show("Common2.txt not found!");
                return;
            }

            var common2Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadLines(common2Path))
            {
                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length < 2)
                    continue;

                string id = parts[0].Trim().Trim('"');
                if (!common2Names.ContainsKey(id))
                    common2Names[id] = parts[1].Trim().Trim('"');
            }

            // ---- 2. guide.txt ("Name = ID[, ID...]") ----
            var guideBases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var guideFamilies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lineRx = new Regex(@"^\s*""?(?<name>.+?)""?\s=\s(?<ids>.+?)\s*$");
            var idRx = new Regex(@"^\w+$");

            string guidePath = Path.Combine(AppContext.BaseDirectory, "guide.txt");
            if (File.Exists(guidePath))
            {
                foreach (string line in File.ReadLines(guidePath))
                {
                    Match match = lineRx.Match(line);
                    if (!match.Success)
                        continue;

                    string name = match.Groups["name"].Value.Trim();
                    string ids = Regex.Replace(match.Groups["ids"].Value, @"\s+--.*$", "");
                    if (ids.Contains("{"))
                        continue;

                    foreach (string rawId in ids.Split(','))
                    {
                        string id = rawId.Trim();
                        if (!idRx.IsMatch(id))
                            continue;

                        const string baseTail = "_fpp_r0";
                        if (id.StartsWith("wpn_", StringComparison.OrdinalIgnoreCase) &&
                            id.EndsWith(baseTail, StringComparison.OrdinalIgnoreCase))
                        {
                            string b = id.Substring(0, id.Length - baseTail.Length);
                            if (!guideBases.ContainsKey(b))
                                guideBases[b] = name;
                        }

                        string fam = StripTail(id);
                        if (!guideFamilies.ContainsKey(fam))
                            guideFamilies[fam] = name;
                    }
                }
            }
            else
            {
                Debug.WriteLine("guide.txt not found, using Common2.txt only");
            }

            // ---- 3. Resolve raw in-memory name -> Item (always returns a named Item) ----
            var howNamed = new Dictionary<string, string>();   // ItemId -> "prefix" / "auto", for review

            Item ResolveWeapon(string rawName, IntPtr basepointer)
            {
                Debug.WriteLine(rawName);
                string[] parts = rawName.Split('_');

                // a) base weapon: longest prefix (3-5 segments) that is a guide base
                for (int take = Math.Min(5, parts.Length); take >= 3; take--)
                {
                    string key = string.Join("_", parts.Take(take));
                    if (guideBases.TryGetValue(key, out string baseName))
                        return new Item(basepointer, basepointer, baseName, key + "_N", "weapon_list", true, "dl2");
                }

                // b) exact match on the family (rarity / level / tier / fpp stripped)
                string family = StripTail(rawName);
                string famId = family + "_N";
                if (guideFamilies.TryGetValue(family, out string famName))
                    return new Item(basepointer, basepointer, famName, famId, "weapon_list", true, "dl2");

                // c) Common2.txt
                for (int take = Math.Min(4, parts.Length); take >= 2; take--)
                {
                    string id = string.Join("_", parts.Take(take)) + "_N";
                    if (common2Names.TryGetValue(id, out string c2Name))
                        return new Item(basepointer, basepointer, c2Name, id, "weapon_list", true, "dl2");
                }

                // d) approximate: longest leading part of the family that is in the guide
                string[] fp = family.Split('_');
                for (int take = fp.Length - 1; take >= 3; take--)
                {
                    if (guideFamilies.TryGetValue(string.Join("_", fp.Take(take)), out string prefixName))
                    {
                        howNamed[famId] = "prefix";
                        return new Item(basepointer, basepointer, prefixName, famId, "weapon_list", true, "dl2");
                    }
                }

                // e) last resort: name generated from the ID itself
                howNamed[famId] = "auto";
                return new Item(basepointer, basepointer, AutoName(family), famId, "weapon_list", true, "dl2");
            }

            // ---- 4. Scan memory ----
            long itemdescmelees = moduleBase + 0x26C24A0;
            List<IntPtr> itemDescMeleeResults = await ItemDescScanner.MemoryScanner(m, itemdescmelees);
            long itemdescfirearms = moduleBase + 0x26BB658;
            List<IntPtr> itemDescFirearmsResults = await ItemDescScanner.MemoryScanner(m, itemdescfirearms);
            itemDescMeleeResults.AddRange(itemDescFirearmsResults);

            foreach (IntPtr basePointer in itemDescMeleeResults)
            {
                string raw = GetItemName(basePointer.ToInt64());
                Item item = ResolveWeapon(raw, basePointer);

                string rank = GetRankTag(raw);
                if (rank != "")
                {
                    // wpn_2hs_f_N -> wpn_2hs_f_r12_N
                    string idNoN = item.ItemId.Substring(0, item.ItemId.Length - 2);
                    item = new Item(basePointer, basePointer, item.ItemName, idNoN + "_" + rank + "_N", "weapon_list", true, "dl2");

                }

                itemDescMeleesDl2.Add(item);
                Debug.WriteLine("Basepointer: " + item.BasePointer.ToString("X"));
                Debug.WriteLine("ItemPointer: " + item.ItemPointer.ToString("X"));
                Debug.WriteLine($"{item.ItemId} - {item.ItemName}");   // every result, one line each
            }

            Item.WeaponsListdl2 = Item.WeaponsListdl2.OrderBy(w => w.ItemName).ToList();

            weapons_list.ItemsSource = null;
            weapons_list.ItemsSource = Item.WeaponsListdl2;
            weapons_list.SelectedIndex = 0;
        }


        private void OnWeaponSearchTextChanged(object sender, TextChangedEventArgs e)
            {
                if (weapons_list.ItemsSource == null)
                    return;

                string filterText = txtWeaponSearch.Text.Trim();
                ICollectionView view = CollectionViewSource.GetDefaultView(weapons_list.ItemsSource);

                if (string.IsNullOrWhiteSpace(filterText))
                {
                    view.Filter = null;
                }
                else
                {
                    view.Filter = obj =>
                    {
                        if (obj is Item item)
                        {
                            bool nameMatch = item.ItemName != null &&
                                             item.ItemName.Contains(filterText, StringComparison.OrdinalIgnoreCase);

                            bool idMatch = item.ItemId != null &&
                                           item.ItemId.Contains(filterText, StringComparison.OrdinalIgnoreCase);

                            return nameMatch || idMatch;
                        }
                        return false;
                    };
                }
            }


    private void UpdateWeapon(object sender, RoutedEventArgs e)
        {

        }

        private void ChangeWeapon(object sender, RoutedEventArgs e)
        {
            Item selectedInventoryWeapon = (Item)weapons_inventory.SelectedItem;
            Item selectedWeaponList = (Item)weapons_list.SelectedItem;

            // 1. Target Address: The +0x48 slot of the inventory item
            long writeTargetAddress = selectedInventoryWeapon.BasePointer.ToInt64() + 0x48;

            // 2. New Value: The address of the weapon template from your list
            long newWeaponPointer = selectedWeaponList.ItemPointer.ToInt64();

            Debug.WriteLine($"Writing new pointer 0x{newWeaponPointer:X} into offset address 0x{writeTargetAddress:X}");

            // Destination Address = HEX ("X") | New Value = DECIMAL
            m.WriteMemory(writeTargetAddress.ToString("X"), "long", newWeaponPointer.ToString());
        }

        private void OnScanButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateWeaponAttributesUI(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CloseInventoryManager(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RefreshInventory(object sender, RoutedEventArgs e)
        {

        }

        public async Task LoadInventoryWeapons()
        {
            long rcxAddress = (long)m.GetCode(
                "gamedll_ph_x64_rwdi.dll+0x03442298,0x230,0x228,0x40,0x2C0,0x58,0x50,0x0"
            );

            string hexValue1 = m.ReadInt((rcxAddress + 0x38).ToString("X")).ToString("X8");
            string hexValue2 = m.ReadInt((rcxAddress + 0x3C).ToString("X")).ToString("X8");

            string fullHex = hexValue2 + hexValue1;
            string inventoryAddress = fullHex.Remove(0, 5);

            long inventoryBase = Convert.ToInt64(inventoryAddress, 16);

            Debug.WriteLine(inventoryBase.ToString("X") + " inv");

            List<Item> inventoryItems = new List<Item>();

            for (int i = 0x00; i <= 0x70; i += 0x08)
            {
                long address = inventoryBase + i;
                long itemPtr = m.ReadLong(address.ToString("X"));
                string inventoryStartString = inventoryBase.ToString("X").Substring(0, 2);

                if (itemPtr.ToString("X").StartsWith(inventoryStartString))
                {
                    string id = GetItemName(m.ReadLong((itemPtr + 0x48).ToString("X")));
                    if (id != null)
                    {
                        string cleanId = StripTail(id);
                        string rank = GetRankTag(id);

                        string inventoryItemId = !string.IsNullOrEmpty(rank)
                            ? $"{cleanId}_{rank}_N"
                            : $"{cleanId}_N"; int r = GetRarity(itemPtr);

                        // 1. Instantiate and add the item to inventoryItems
                        Item invItem = new Item(new IntPtr(itemPtr), new IntPtr(m.ReadLong((itemPtr + 0x48).ToString("X"))), "Unknown Item", inventoryItemId, "weapon", true, "dl2");
                        inventoryItems.Add(invItem);
                        Debug.WriteLine("Basepointer: " + invItem.BasePointer);
                        Debug.WriteLine("ItemPointer: " + invItem.ItemPointer);
                        SetRarity(itemPtr, 6);
                    }
                }
            }

            // 2. Build a lookup dictionary from itemDescMeleesDl2 for O(1) matching
            var weaponLookup = itemDescMeleesDl2
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.First().ItemName);

            // 3. Resolve names using the lookup
            foreach (var invItem in inventoryItems)
            {
                if (weaponLookup.TryGetValue(invItem.ItemId, out string matchedName))
                {
                    invItem.ItemName = matchedName;
                }
            }

            weapons_inventory.ItemsSource = null;
            weapons_inventory.ItemsSource = Item.WeaponsInventorydl2;
            weapons_inventory.SelectedIndex = 0;
        }
        static readonly string[] RarityNames =
            { "common", "uncommon", "rare", "epic", "legendary", "platinum", "exotic" };

        public static int GetRarity(long item)
        {
            uint raw = (uint)m.ReadInt((item + 0x50).ToString("X"));
            return (int)((raw >> 10) & 7);
        }

        public static void SetRarity(long item, int tier)   // 0..6
        {
            uint raw = (uint)m.ReadInt((item + 0x50).ToString("X"));
            uint updated = (raw & ~0x1C00u) | ((uint)tier << 10);
            m.WriteMemory((item + 0x50).ToString("X"), "int", ((int)updated).ToString());
        }

        private static readonly Regex TailRx = new Regex(
            @"_(?:Common|Uncommon|Ucommon|Rare|Unique|Artifact|Legendary|Exotic|fpp|r\d+|t\d+)$",
            RegexOptions.IgnoreCase);

        private static string StripTail(string id)
        {
            string prev;
            do { prev = id; id = TailRx.Replace(id, ""); } while (id != prev);
            return id;
        }

        private static readonly Regex RankTokenRx = new Regex(
    @"^(?:r\d+|t\d+|Common|Uncommon|Ucommon|Rare|Unique|Artifact|Legendary|Exotic)$",
    RegexOptions.IgnoreCase);

        // returns e.g. "r12", "Legendary", "t5", or "" if the raw name has none
        private static string GetRankTag(string rawName)
        {
            string[] parts = rawName.Split('_');
            for (int i = parts.Length - 1; i >= 1; i--)
                if (RankTokenRx.IsMatch(parts[i]))
                    return parts[i];
            return "";
        }

        // "wpn_1hs_machete_TR_fire_LowDurability" -> "Machete TR Fire Low Durability"
        private static string AutoName(string family)
        {
            var words = new List<string>();
            foreach (string tok in family.Split('_'))
            {
                if (tok.Equals("wpn", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(tok, @"^\d+h[sb]$", RegexOptions.IgnoreCase))
                    continue;

                foreach (string w in Regex.Split(tok, @"(?<=[a-z])(?=[A-Z])"))
                    if (w.Length > 0)
                        words.Add(char.ToUpper(w[0]) + w.Substring(1));
            }
            return string.Join(" ", words);
        }

        public static string GetItemName(long instanceAddr)
        {
            Debug.WriteLine(instanceAddr);
            long raw = m.ReadLong((instanceAddr + 0x08).ToString("X"));

            if (raw == 0) return null;

            // Mask off the tag bits
            long strPtr = raw & 0x1FFFFFFFFFFFFFFF;
            if (strPtr == 0) return null;

            // Read the null-terminated string at that address
            string name = m.ReadString(strPtr.ToString("X"), "", 64);
            return name;
        }
    }
}
