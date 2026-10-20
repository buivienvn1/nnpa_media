using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NNPA
{
    /// <summary>
    /// Interaction logic for Thongtin.xaml
    /// </summary>
    public partial class Thongtin : Window
    {
        public Thongtin()
        {
            InitializeComponent();
        }

        private void BtnTruycap_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://nhacnuocphatan.com/");
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
