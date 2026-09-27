using Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace dying_light_trainer
{
    /// <summary>
    /// Interaction logic for DyingLight2.xaml
    /// </summary>
    public partial class DyingLight2 : Window
    {
        public static Mem m;

        public static long moduleBase;
        public DyingLight2()
        {
            InitializeComponent();

            m = new Mem();
            m.OpenProcess("DyingLightGame_x64_rwdi.exe");
            moduleBase = m.GetModuleAddressByName("gamedll_ph_x64_rwdi.dll");

        }

        private void OpenInventoryManager(object sender, RoutedEventArgs e)
        {
            dying_light_trainer.InventoryManager2 inventoryManager2 = new dying_light_trainer.InventoryManager2();
            inventoryManager2.Show();
        }

        
    }
}
