using Microsoft.Win32;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Forms;
using static NNPA.LoginLoad;

namespace NNPA
{
    /// <summary>
    /// Interaction logic for Caidat.xaml
    /// </summary>
    public partial class Caidat : Window
    {
        public Caidat()
        {
            InitializeComponent();
            IP1.Text = Properties.Settings.Default.IPAddress;
            Port1.Text = Properties.Settings.Default.PortNumber.ToString();
            FolderPath.Text = selectedPath;

            int frameSize = Properties.Settings.Default.PlaybackFrameSize;
            FrameSize2048.IsChecked = frameSize == LoginLoad.ModernFrameSize;
            FrameSize1024.IsChecked = frameSize != LoginLoad.ModernFrameSize;
        }

        private void BtnPing1_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IP1.Text;
            if (PingHost(ipAddress))
            {
                // pingcheck = true;
                //MessageBox.Show("Có thể kết nối đến IP: " + ipAddress, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                new MessageBoxCustom("Có thể kết nối đến IP: " + ipAddress, MessageType.Info, MessageButtons.Ok).ShowDialog();
            }
            else
            {
                //pingcheck = false;
                new MessageBoxCustom("Không thể kết nối đến IP: " + ipAddress, MessageType.Error, MessageButtons.Ok).ShowDialog();

                //MessageBox.Show("Không thể kết nối đến IP: " + ipAddress, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }



        private void BtnChonpath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Lựa chọn thư mục chứa File chạy và mp3";
                dialog.SelectedPath = FolderPath.Text;
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    FolderPath.Text = selectedPath;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int portNumber;
            if (!int.TryParse(Port1.Text, out portNumber) || portNumber < 1 || portNumber > 65535)
            {
                new MessageBoxCustom("Port phải là số từ 1 đến 65535.", MessageType.Warning, MessageButtons.Ok).ShowDialog();
                return;
            }

            // Kiểm tra xem đường dẫn có tồn tại hay không
            if (!Directory.Exists(FolderPath.Text))
            {
                new MessageBoxCustom($"Đường dẫn chứa File Chạy không tồn tại.", MessageType.Warning, MessageButtons.Ok).ShowDialog();
                return;
            }

            SvIP = Properties.Settings.Default.IPAddress = IP1.Text;
            PortNum = Properties.Settings.Default.PortNumber = portNumber;
            selectedPath = Properties.Settings.Default.FolderPath = FolderPath.Text;
            Properties.Settings.Default.PlaybackFrameSize = FrameSize2048.IsChecked == true
                ? LoginLoad.ModernFrameSize
                : LoginLoad.LegacyFrameSize;
            Properties.Settings.Default.BuocTik = LoginLoad.FrameIntervalMilliseconds;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
