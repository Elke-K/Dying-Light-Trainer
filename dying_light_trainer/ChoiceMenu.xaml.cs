using DyingLightTrainer;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for ChoiceMenu.xaml
    /// </summary>
    public partial class ChoiceMenu : Window
    {
        public ChoiceMenu()
        {
            InitializeComponent();
        }

        private void OpenDyingLight1(object sender, RoutedEventArgs e)
        {
            DyingLight1 window = new DyingLight1();
            window.Show();
            this.Close();
        }

        private void OpenDyingLight2(object sender, RoutedEventArgs e)
        {
            DyingLight2 window = new DyingLight2();
            window.Show();
            this.Close();
        }

        private void OpenDyingLightTheBeast(object sender, RoutedEventArgs e)
        {
            DyingLight3 window = new DyingLight3();
            window.Show();
            this.Close();
        }
    }
}
