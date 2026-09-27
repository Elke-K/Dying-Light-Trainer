using dying_light_trainer;
using Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static dying_light_trainer.InventoryManager;

namespace DyingLightTrainer
{
    public partial class DyingLight1 : Window
    {
        public static Mem m;

        public static long moduleBase;

        private DispatcherTimer hookTimer;

        public static List<(IntPtr, string)> ItemDescCraftplanList = new List<(IntPtr, string)>();
        public static List<(IntPtr, string)> ItemDescCraftUpgradesList = new List<(IntPtr, string)>();
        public static List<(IntPtr, string)> ItemDescMeleeList = new List<(IntPtr, string)>();

        public DyingLight1()
        {
            InitializeComponent();

            Debug.WriteLine("Hello");

            if (Process.GetProcessesByName("DyingLightGame").Length <= 0)
            {
                MessageBox.Show("Dying Light is not running!");
                Environment.Exit(0);
            }
            else
            {
                m = new Mem();
                m.OpenProcess("DyingLightGame");
                moduleBase = m.GetModuleAddressByName("gamedll_x64_rwdi.dll");

                _ = InitializeAsync();

                lblStatus.Text = "Dying Light currently running and injected!";
            }

            SetupTimers();

        }

        private async Task InitializeAsync()
        {
            await LoadVTableInformation();
        }

        private void chkInfiniteHook_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckIfRunning())
            {
                hookTimer.Start();
                lblStatus.Text = "Successfully injected unlimited grappling hook cheat!";
            }
        }

        private void chkInfiniteHook_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckIfRunning())
            {
                hookTimer.Stop();
                lblStatus.Text = "Successfully stopped unlimited grappling hook cheat!";
            }
        }

        public void UnlimitedGrapplingHook(object sender, EventArgs e)
        {
            string hookAddress = $"{moduleBase:X}+1C3AA30,18,18,10,1D8,40";
            m.WriteMemory(hookAddress, "float", "2.5", "");
        }

        public void Cloning(object sender, EventArgs e)
        {
            long assemblyCloningAddress = moduleBase + 0xCD484E;
            m.WriteMemory(assemblyCloningAddress.ToString("X"), "bytes", "89 56 30");
        }

        private void chkGodMode_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckIfRunning())
            {
                long godModeAddress = moduleBase + 0xBA6C13;
                m.WriteMemory(godModeAddress.ToString("X"), "bytes", "90 90 90");
                lblStatus.Text = "Successfully injected god mode cheat!";
            }
        }

        private void chkGodMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckIfRunning())
            {
                long godModeAddress = moduleBase + 0xBA6C13;
                m.WriteMemory(godModeAddress.ToString("X"), "bytes", "FF 53 20");
                lblStatus.Text = "Successfully stopped god mode cheat!";
            }
        }

        public bool CheckIfRunning()
        {
            if (Process.GetProcessesByName("DyingLightGame").Length <= 0)
            {
                lblStatus.Text = "Dying Light not running! Unable to inject cheat.";
                return false;
            }
            return true;
        }

        public void SetupTimers()
        {
            hookTimer = new DispatcherTimer();
            hookTimer.Interval = TimeSpan.FromMilliseconds(50);
            hookTimer.Tick += UnlimitedGrapplingHook;
        }

        private async void chkCloning_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckIfRunning())
            {
                string addr1 = "7E 49 48 8B 46 40";
                string addr2 = "7E 36 FF CA";

                List<IntPtr> addr1Results = await ItemDescScanner.MemoryScannerString(m, addr1);
                List<IntPtr> addr2Results = await ItemDescScanner.MemoryScannerString(m, addr2);

                if (addr1Results.Count > 0 && addr2Results.Count > 0)
                {
                    long foundaddr1 = addr1Results[0].ToInt64();
                    long foundaddr2 = addr2Results[0].ToInt64(); 

                    m.WriteMemory(foundaddr1.ToString("X"), "bytes", "90 90");
                    m.WriteMemory(foundaddr2.ToString("X"), "bytes", "90 90");
                    lblStatus.Text = "Cloning enabled!";
                }
                else
                {
                    lblStatus.Text = "Cloning failed.";
                }
            }
        }

        private async void chkCloning_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckIfRunning())
            {
                string addr1 = "90 90 48 8B 46 40";
                string addr2 = "90 90 FF CA";

                List<IntPtr> addr1Results = await ItemDescScanner.MemoryScannerString(m, addr1);
                List<IntPtr> addr2Results = await ItemDescScanner.MemoryScannerString(m, addr2);

                if (addr1Results.Count > 0 && addr2Results.Count > 0)
                {
                    long foundaddr1 = addr1Results[0].ToInt64();
                    long foundaddr2 = addr2Results[0].ToInt64();

                    m.WriteMemory(foundaddr1.ToString("X"), "bytes", "7E 49");
                    m.WriteMemory(foundaddr2.ToString("X"), "bytes", "7E 36");
                    lblStatus.Text = "Cloning disabled!";
                }
                else
                {
                    lblStatus.Text = "Cloning failed.";
                }
            }
        }

        private void OpenInventoryManager(object sender, RoutedEventArgs e)
        {
            dying_light_trainer.InventoryManager inventoryManager = new dying_light_trainer.InventoryManager();
            inventoryManager.Show();
        }

        public static async Task LoadVTableInformation()
        {
            long vtableCraftplan = moduleBase + 0x15538D0;
            List<IntPtr> craftplan = await ItemDescScanner.MemoryScanner(m, vtableCraftplan);

            long vtableMelee = moduleBase + 0x1559BE0;
            List<IntPtr> melee = await ItemDescScanner.MemoryScanner(m, vtableMelee);

            long vtableCraftUpgrades = moduleBase + 0x1551AE0;
            List<IntPtr> craftupgrades = await ItemDescScanner.MemoryScanner(m, vtableCraftUpgrades);

            foreach (IntPtr item in craftplan)
            {
                long itemStringPointer = m.ReadLong((item.ToInt64() + 0x18).ToString("X"), "");
                int itemStringLength = m.ReadInt((item.ToInt64() + 0x20).ToString("X"), "");
                string itemString = m.ReadString(itemStringPointer.ToString("X"), "", itemStringLength, true, null);
                ItemDescCraftplanList.Add((item, itemString));
            }

            foreach ((IntPtr basePointer, string itemstring) in ItemDescCraftplanList)
            {
                long baseVal = m.ReadLong(basePointer.ToInt64().ToString("X"), "");
                Debug.WriteLine($"Base value: {baseVal:X}, string: {itemstring}");
            }

            foreach (IntPtr item in melee)
            {
                long itemStringPointer = m.ReadLong((item.ToInt64() + 0x18).ToString("X"), "");
                int itemStringLength = m.ReadInt((item.ToInt64() + 0x20).ToString("X"), "");

                if (itemStringPointer != 0 && itemStringLength > 0 && itemStringLength < 100)
                {
                    string itemString = m.ReadString(itemStringPointer.ToString("X"), "", itemStringLength, true, null);
                    ItemDescMeleeList.Add((item, itemString));
                }
            }

            foreach ((IntPtr basePointer, string itemstring) in ItemDescMeleeList)
            {
                long baseVal = m.ReadLong(basePointer.ToInt64().ToString("X"), "");
                Debug.WriteLine($"Base value: {baseVal:X}, string: {itemstring}");
            }

            foreach (IntPtr item in craftupgrades)
            {
                long itemStringPointer = m.ReadLong((item.ToInt64() + 0x18).ToString("X"), "");
                int itemStringLength = m.ReadInt((item.ToInt64() + 0x20).ToString("X"), "");

                if (itemStringPointer != 0 && itemStringLength > 0 && itemStringLength < 100)
                {
                    string itemString = m.ReadString(itemStringPointer.ToString("X"), "", itemStringLength, true, null);
                    ItemDescCraftUpgradesList.Add((item, itemString));
                }
            }

            foreach ((IntPtr basePointer, string itemstring) in ItemDescCraftUpgradesList)
            {
                long baseVal = m.ReadLong(basePointer.ToInt64().ToString("X"), "");
                Debug.WriteLine($"Base value: {baseVal:X}, string: {itemstring}");
            }
        }

        

    }
}