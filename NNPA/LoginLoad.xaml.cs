using Microsoft.Win32;
using Newtonsoft.Json;
using NNPA.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NNPA
{
    /// <summary>
    /// Interaction logic for LoginLoad.xaml
    /// </summary>
    public partial class LoginLoad : Window
    {
        

        DispatcherTimer timer;
        private Stopwatch stopwatch;
        BrushConverter bc= new BrushConverter();
        private string tUser;
        int repeat;
        private int totalSeconds;
        private double buocTik = Settings.Default.BuocTik;
        private bool isPlaying;
        private bool st1;
        public static ProgressWindows pbw = null;
        object[] _lstDatasend = new object[2];
        public static string SvIP = "192.168.1.88";
        public static int PortNum =80;
        public static ObservableCollection<FileData> fileList { get; set; }
        public static ObservableCollection<string> checkedFiles { get; set; } = new ObservableCollection<string>();

        public static string selectedPath;
        List<byte[]> byteArrayList;
        public static byte[] listDataSen;
        byte[] arrKytu = Encoding.ASCII.GetBytes("*1234#");
        public static UdpClient ClientSocket;
        int id;
        MediaPlayer mediaPlayer;
        string mp3File = "";
        DirectoryInfo di;
        FileInfo[] files;

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);


        public LoginLoad()
        {
            InitializeComponent();
            this.tUser = "Admin";
            DateTime d = DateTime.Now;
            //string ap = d.ToString("tt");

            txtXinchao.Text = (d.Hour < 12) ? ("Chào buổi sáng, " + tUser) : ("Chào buổi chiều, " + tUser);

            selectedPath = Properties.Settings.Default.FolderPath == "" 
                ? AppDomain.CurrentDomain.BaseDirectory + "File Chay" 
                : Properties.Settings.Default.FolderPath;


            SvIP = Properties.Settings.Default.IPAddress;
            PortNum = Properties.Settings.Default.PortNumber;

            // Kiểm tra xem đường dẫn có tồn tại hay không
            if (!Directory.Exists(selectedPath))
            {
                new MessageBoxCustom($"Đường dẫn chứa File Chạy không tồn tại.", MessageType.Warning, MessageButtons.Ok).ShowDialog();
                return;
            }

            Load_Playlist();
            lbMusic.ItemsSource = fileList;

            if (fileList.Count <= 0) return;
            mp3File = selectedPath + "/"+fileList[0].FileName + ".mp3";
            mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri(mp3File));
            
            CheckFileMp3();

            buocTik = Settings.Default.BuocTik;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Đăng ký sự kiện PowerModeChanged
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            // Ngăn chặn máy tính tắt màn hình và chuyển sang chế độ sleep
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED);

            // ... (Các công việc khác)
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
             if (e.Mode == PowerModes.Suspend)
            {
                // Xử lý khi máy tính chuyển sang chế độ sleep
                // Có thể đóng kết nối mạng, dừng phát nhạc, hoặc thực hiện các công việc cần thiết khi máy tính vào chế độ sleep

                BtnStart_Click(null, null);
            }
        }

        private void CheckFileMp3()
        {
            // Kiểm tra xem đường dẫn có tồn tại hay không
            if (!Directory.Exists(selectedPath))
            {
                new MessageBoxCustom($"Đường dẫn chứa File Chạy không tồn tại.", MessageType.Warning, MessageButtons.Ok).ShowDialog();
                return;
            }

            di = new DirectoryInfo(selectedPath);
            files = di.GetFiles("*.mp3");

            foreach (var file in fileList)
            {
                if (files.Any(f => f.Name == file.FileName + ".mp3"))
                {
                    file.GhiChu = null;
                }
                else
                    file.GhiChu = ($"File '{file.FileName}.mp3' không tồn tại trong thư mục.");
            }
            //lbMusic.ItemsSource = fileList;

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //textBlockTimer.Text = stopwatch.Elapsed.ToString(@"mm\:ss\:fff");

            if (totalSeconds < 3600)
            {
                tbProgressStatus.Text = TimeSpan.FromSeconds((int)sliProgress.Value).ToString(@"mm\:ss");
                //tbProgressStatus.Text = stopwatch.Elapsed.ToString(@"mm\:ss");
            }
            else
            {
                tbProgressStatus.Text = TimeSpan.FromSeconds((int)sliProgress.Value).ToString(@"hh\:mm\:ss");
            }
            sliProgress.Value = stopwatch.Elapsed.TotalSeconds;

            getData();

            if (sliProgress.Value >= sliProgress.Maximum)
            {
                BtnStop_Click(null, null);
                if (lbMusic.SelectedIndex < lbMusic.Items.Count - 1)
                {
                    BtnNext_Click(null, null);
                }
                else if (repeat==1 )
                {
                    BtnNext_Click(null, null);
                }
            }

            if (isPlaying)
            {
                //if (!SystemSleepDetector.IsSystemAwake()) return;
                if (_lstDatasend[0] != null && st1 == true)
                {
                    UdpClient client = new UdpClient();
                    IPEndPoint ip = new IPEndPoint(IPAddress.Parse(SvIP), Convert.ToInt16(PortNum));

                    client.Send((byte[])_lstDatasend[0], ((byte[])_lstDatasend[0]).Length, ip);
                    client.Close();
                }
            }
        }
        private bool isConnected()
        {
            //Load IP Address and Port number
            OnWorkerMethodStart();
            return st1 ? true : false;
        }

        private void OnWorkerMethodStart()
        {

            pbw = new ProgressWindows();
            pbw.Owner = this;

            MyClass myC = new MyClass();
            myC.OnWorkerComplete += new MyClass.OnWorkerMethodCompleteDelegate(OnWorkerMethodComplete);
            ThreadStart tStart = new ThreadStart(myC.WorkerMethod);
            Thread t = new Thread(tStart);
            t.Start();

            pbw.ShowDialog();
        }

        private void OnWorkerMethodComplete(bool st1)
        {
            this.st1 = st1;
            pbw.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
            new Action(
            delegate ()
            {
                pbw.Close();
            }
            ));

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

        public void setupTimer()
        {
            stopwatch = new Stopwatch();
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(Timer_Tick); // Có += thì phải có -=
            timer.Interval = TimeSpan.FromMilliseconds(buocTik); //BƯỚC 20MS

            stopwatch.Start();
            timer.Start();
        }

        private void getData()
        {
            int _index = (int)(sliProgress.Value * 1000 / buocTik);
            if (_index > byteArrayList.Count - 1) return;

            byte[] originalArray = byteArrayList[_index];

            #region startData array
            listDataSen = new byte[1024 + 3];
            #endregion

            // Add element at beginning
            Array.Copy(originalArray, 0, listDataSen, 2, 1024);
            listDataSen[0] = arrKytu[0]; //*
            listDataSen[1] = 1;//1


            #region endData array
            listDataSen[listDataSen.Length - 1] = arrKytu[5]; //#
            _lstDatasend[0] = listDataSen;
            listDataSen = null;
            #endregion
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                stopwatch.Stop();
                timer.Stop();
                mediaPlayer = null;
                timer.Tick -= Timer_Tick;
            }
            this.Close();
        }

        #region Task Data
        async void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            CheckFileMp3();
        }
        #endregion

        private void BtnPlayList_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying) return;

            Playlist playlist = new Playlist();
            playlist.ShowDialog();
            lbMusic.ItemsSource = fileList;

            CheckFileMp3();
        }
        
        #region Button Media
        private void LbMusic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbMusic.SelectedItem != null)
            {
                id = lbMusic.SelectedIndex;
                txtTenBai.Text = fileList[id].FileName;
                mp3File = selectedPath + "/" + txtTenBai.Text + ".mp3";
                mediaPlayer.Open(new Uri(mp3File));
                totalSeconds = fileList[id].FileTotalSecond;
                sliProgress.Maximum = totalSeconds + 1;
                if (totalSeconds < 3600)
                {
                    tbTotalSecond.Text = TimeSpan.FromSeconds((int)totalSeconds).ToString(@"mm\:ss");
                }
                else
                {
                    tbTotalSecond.Text = TimeSpan.FromSeconds((int)totalSeconds).ToString(@"hh\:mm\:ss");
                }

                byteArrayList = fileList[id].ByteArrayList;

                BtnStop_Click(null,null);
                BtnStart_Click(null, null);
            }
                
        }
        
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (totalSeconds > 0)
            {
                //nếu không chạy - isplaying bằng false
                if (!isPlaying)
                {
                    if (isConnected())
                    {
                        //btnPlay.Content = "PAUSE";
                        isPlaying = true;

                        if (timer == null) setupTimer();
                        else
                        {
                            stopwatch.Start();
                            timer.Start();
                        }
                        //Media play
                        if (mediaPlayer.Source != null) mediaPlayer.Play();

                        if(lbMusic.SelectedIndex<0) lbMusic.SelectedIndex = 0;


                        imgLogoxoay.Visibility = Visibility.Hidden;
                        btnStart.Content = new MaterialDesignThemes.Wpf.PackIcon
                        { Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause };

                    }
                    else
                    {
                        new MessageBoxCustom("Không thể kết nối đến Bộ trung tâm", MessageType.Warning, MessageButtons.Ok).ShowDialog();
                        //MessageBox.Show("Không thể kết nối đến Bộ trung tâm", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else //nhấn pause
                {
                    if (sliProgress.Value > 0)
                    {
                        stopwatch.Stop();
                        timer.Stop();
                    }
                    //btnPlay.Content = "PLAY";
                    if (mediaPlayer.Source != null)
                    {
                        mediaPlayer.Pause();
                    }

                    btnStart.Content = new MaterialDesignThemes.Wpf.PackIcon
                    { Kind = MaterialDesignThemes.Wpf.PackIconKind.Play };
                    isPlaying = false;
                    imgLogoxoay.Visibility = Visibility.Visible;
                }
                tbNoteListMusic.Visibility = Visibility.Visible;
            }
            else if (fileList!=null && byteArrayList == null)
            {
                id = 0;
                txtTenBai.Text = fileList[id].FileName;
                totalSeconds = fileList[id].FileTotalSecond;
                sliProgress.Maximum = totalSeconds + 1;
                if (totalSeconds < 3600)
                {
                    tbTotalSecond.Text = TimeSpan.FromSeconds((int)totalSeconds).ToString(@"mm\:ss");
                }
                else
                {
                    tbTotalSecond.Text = TimeSpan.FromSeconds((int)totalSeconds).ToString(@"hh\:mm\:ss");
                }

                byteArrayList = fileList[id].ByteArrayList;

                BtnStop_Click(null, null);
                BtnStart_Click(null, null);
            }
            

        }

        private void OpenPlay_Music(string targetFileName)
        {
            string[] mp3Files = Directory.GetFiles(selectedPath, "*.mp3");

            foreach (string mp3File in mp3Files)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(mp3File);

                if (fileName == targetFileName)
                {
                    MediaPlayer mediaPlayer = new MediaPlayer();
                    mediaPlayer.Open(new Uri(mp3File));
                    mediaPlayer.Play();

                    break;
                }
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            //media stop
            if (totalSeconds > 0)
            {
                //gửi data end
                _lstDatasend[0] = new byte[1024 + 3];
                ((byte[])_lstDatasend[0])[0] = arrKytu[0]; //*
                ((byte[])_lstDatasend[0])[1] = 1;//1
                ((byte[])_lstDatasend[0])[1027 - 1] = arrKytu[5]; //#
                if (isPlaying)
                {
                    //if (SystemSleepDetector.IsSystemAwake()) return;
                    UdpClient client = new UdpClient();
                    IPEndPoint ip = new IPEndPoint(IPAddress.Parse(SvIP), Convert.ToInt16(PortNum));
                    for (int i = 0; i < 5; i++)
                    {
                        client.Send((byte[])_lstDatasend[0], ((byte[])_lstDatasend[0]).Length, ip);

                        Thread.Sleep(100);
                    }
                    client.Close();
                }
                if (ClientSocket != null) ClientSocket.Close();

                if (timer != null)
                {
                    stopwatch.Stop();
                    timer.Stop();
                    timer.Tick -= Timer_Tick;
                    timer = null;
                }
            }
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Stop();
                tbProgressStatus.Text = mediaPlayer.Position.ToString(@"mm\:ss");
            }
            else
            {
                tbProgressStatus.Text = "00:00";
            }
            sliProgress.Value = 0;
            //tbProgressStatus.Text = "00:00";
            isPlaying = false;
            imgLogoxoay.Visibility = Visibility.Visible;
            tbNoteListMusic.Visibility = Visibility.Collapsed;
            btnStart.Content = new MaterialDesignThemes.Wpf.PackIcon
            { Kind = MaterialDesignThemes.Wpf.PackIconKind.Play };
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            int prevIndex = lbMusic.Items.Count - 1;
            if ((lbMusic.SelectedIndex > 0) && (lbMusic.SelectedIndex < lbMusic.Items.Count))
                prevIndex = lbMusic.SelectedIndex - 1;
            lbMusic.SelectedIndex = prevIndex;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            int nextIndex = 0;
            if ((lbMusic.SelectedIndex >= 0) && (lbMusic.SelectedIndex < (lbMusic.Items.Count - 1)))
                nextIndex = lbMusic.SelectedIndex + 1;
            lbMusic.SelectedIndex = nextIndex;
        }

        private void BtnRepeat_Click(object sender, RoutedEventArgs e)
        {
            repeat = repeat == 1 ? 0 : 1;
            btnRepeat.Background = repeat == 1 ? (Brush)bc.ConvertFrom("#7B5CD6") : (Brush)bc.ConvertFrom("#00000000");
        }

        private void BtnVolume_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null) return;
            mediaPlayer.Volume = mediaPlayer.Volume == 0 ? 100 : 0;
            mIconVlO.Visibility = mediaPlayer.Volume == 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tbProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"mm\:ss");
        }
        #endregion

        private void BtnDoimatkhau_Click(object sender, RoutedEventArgs e)
        {
            Doimatkhau doimatkhau = new Doimatkhau(tUser);
            doimatkhau.ShowDialog();
        }

        private void BtnCaidat_Click(object sender, RoutedEventArgs e)
        {
            Caidat caidat = new Caidat();
            caidat.ShowDialog();

            // Kiểm tra xem đường dẫn có tồn tại hay không
            if (!Directory.Exists(selectedPath))
            {
                new MessageBoxCustom($"Đường dẫn chứa File Chạy không tồn tại.", MessageType.Warning, MessageButtons.Ok).ShowDialog();
                return;
            }
            Load_Playlist();
            lbMusic.ItemsSource = fileList;
            CheckFileMp3();
        }

        private void BtnQuanlytaikhoan_Click(object sender, RoutedEventArgs e)
        {
            Quanlytaikhoan quanlytaikhoan = new Quanlytaikhoan();
            quanlytaikhoan.ShowDialog();
        }

        private void BtnThongtin_Click(object sender, RoutedEventArgs e)
        {
            Thongtin thongtin = new Thongtin();
            thongtin.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
                this.WindowState = WindowState.Normal;
            else this.WindowState = WindowState.Minimized;
        }

        void Load_Playlist()
        {
            if (selectedPath != "")
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(selectedPath);
                FileInfo[] files = directoryInfo.GetFiles();
                fileList = new ObservableCollection<FileData>();
                checkedFiles = new ObservableCollection<string>();
                foreach (FileInfo file in files)
                {
                    if (file.Extension != ".txt") continue;
                    string fileName = file.Name;
                    checkedFiles.Add(fileName);

                    List<byte[]> byteArrayList = new List<byte[]>();
                    using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
                    {
                        int bytesRead = 0;
                        byte[] buffer = new byte[1024];
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            byte[] byteArray = new byte[bytesRead];
                            Buffer.BlockCopy(buffer, 0, byteArray, 0, bytesRead);
                            byteArrayList.Add(byteArray);
                        }
                    }
                    FileData fileData = new FileData(fileName.Replace(".txt", ""), byteArrayList, buocTik);
                    fileList.Add(fileData);
                }
            }
        }
        private void BtnHuongdan_Click(object sender, RoutedEventArgs e)
        {
            Huongdan huongdan = new Huongdan();
            huongdan.ShowDialog();
        }
    }

    public class FileData : INotifyPropertyChanged
    {
        public string FileName { get; set; }

        private string _ghiChu;
        public string GhiChu
        {
            get { return _ghiChu; }
            set
            {
                if (_ghiChu != value)
                {
                    _ghiChu = value;
                    OnPropertyChanged(nameof(GhiChu));
                }
            }
        }

        public int FileTotalSecond { get; set; }
        public List<byte[]> ByteArrayList { get; set; }

        public FileData(string fileName, List<byte[]> byteArrayList, double buocTik)
        {
            FileName = fileName;
            FileTotalSecond = (int)((byteArrayList.Count - 1) * buocTik / 1000);
            ByteArrayList = byteArrayList;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public static class SystemSleepDetector
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        public struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte Reserved1;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        public static bool IsSystemAwake()
        {
            SYSTEM_POWER_STATUS status;
            if (GetSystemPowerStatus(out status))
            {
                // ACLineStatus = 0 khi máy tính chuyển sang chế độ sleep
                return (status.ACLineStatus != 0);
            }
            return false;
        }
    }
}
