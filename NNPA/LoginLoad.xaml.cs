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
        

        public const int FrameIntervalMilliseconds = 50;
        public const int PacketPayloadSize = 1024;
        public const int LegacyFrameSize = 1024;
        public const int ModernFrameSize = 2048;
        public const int PacketOverheadSize = 3;

        DispatcherTimer timer;
        private Stopwatch stopwatch;
        BrushConverter bc= new BrushConverter();
        private string tUser;
        int repeat;
        private int totalSeconds;
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

            mediaPlayer = new MediaPlayer
            {
                IsMuted = Settings.Default.IsAudioMuted
            };
            UpdateAudioMuteUi();

            if (fileList.Count <= 0) return;
            mp3File = selectedPath + "/" + fileList[0].FileName + ".mp3";
            mediaPlayer.Open(new Uri(mp3File));

            CheckFileMp3();
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

            if (isPlaying && st1)
            {
                SendCurrentPackets();
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
            timer.Interval = TimeSpan.FromMilliseconds(FrameIntervalMilliseconds);

            stopwatch.Start();
            timer.Start();
        }

        private static byte[] CreatePacket(byte[] frame, int payloadOffset, byte packetNumber)
        {
            byte[] packet = new byte[PacketPayloadSize + PacketOverheadSize];
            packet[0] = (byte)'*';
            packet[1] = packetNumber;
            Buffer.BlockCopy(frame, payloadOffset, packet, 2, PacketPayloadSize);
            packet[packet.Length - 1] = (byte)'#';
            return packet;
        }

        private void getData()
        {
            if (byteArrayList == null || byteArrayList.Count == 0)
                return;

            int index = (int)(sliProgress.Value * 1000 / FrameIntervalMilliseconds);
            if (index < 0 || index >= byteArrayList.Count)
                return;

            byte[] frame = byteArrayList[index];
            if (frame == null || (frame.Length != LegacyFrameSize && frame.Length != ModernFrameSize))
                return;

            _lstDatasend[0] = CreatePacket(frame, 0, 1);
            _lstDatasend[1] = frame.Length == ModernFrameSize
                ? CreatePacket(frame, PacketPayloadSize, 2)
                : null;
        }

        private void SendCurrentPackets()
        {
            if (_lstDatasend[0] == null)
                return;

            using (UdpClient client = new UdpClient())
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(SvIP), PortNum);
                for (int packetIndex = 0; packetIndex < _lstDatasend.Length; packetIndex++)
                {
                    byte[] packet = _lstDatasend[packetIndex] as byte[];
                    if (packet != null)
                        client.Send(packet, packet.Length, endpoint);
                }
            }
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
                // Gửi frame zero đúng theo định dạng controller đang cấu hình.
                byte[] zeroFrame = new byte[GetConfiguredFrameSize()];
                _lstDatasend[0] = CreatePacket(zeroFrame, 0, 1);
                _lstDatasend[1] = zeroFrame.Length == ModernFrameSize
                    ? CreatePacket(zeroFrame, PacketPayloadSize, 2)
                    : null;
                if (isPlaying)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        SendCurrentPackets();
                        Thread.Sleep(100);
                    }
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
            if (mediaPlayer == null)
                return;

            mediaPlayer.IsMuted = !mediaPlayer.IsMuted;
            Settings.Default.IsAudioMuted = mediaPlayer.IsMuted;
            Settings.Default.Save();
            UpdateAudioMuteUi();
        }

        private void UpdateAudioMuteUi()
        {
            bool isMuted = mediaPlayer != null && mediaPlayer.IsMuted;
            mIconVlH.Visibility = isMuted ? Visibility.Hidden : Visibility.Visible;
            mIconVlO.Visibility = isMuted ? Visibility.Visible : Visibility.Hidden;
            btnVolume.ToolTip = isMuted
                ? "Bật âm thanh ứng dụng"
                : "Tắt âm thanh ứng dụng (chỉ gửi dữ liệu)";
            btnVolume.Background = isMuted
                ? (Brush)bc.ConvertFrom("#7B5CD6")
                : Brushes.Transparent;
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

        public static int GetConfiguredFrameSize()
        {
            return Settings.Default.PlaybackFrameSize == ModernFrameSize
                ? ModernFrameSize
                : LegacyFrameSize;
        }

        public static bool TryReadPlaybackFile(string filePath, out List<byte[]> frames, out string error)
        {
            frames = new List<byte[]>();
            error = null;
            int frameSize = GetConfiguredFrameSize();
            FileInfo file = new FileInfo(filePath);

            if (file.Length == 0 || file.Length % frameSize != 0)
            {
                error = string.Format(
                    "File '{0}' có {1} byte, không chia hết cho frame {2} byte.",
                    file.Name,
                    file.Length,
                    frameSize);
                return false;
            }

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                while (stream.Position < stream.Length)
                {
                    byte[] frame = new byte[frameSize];
                    int offset = 0;
                    while (offset < frame.Length)
                    {
                        int read = stream.Read(frame, offset, frame.Length - offset);
                        if (read == 0)
                        {
                            error = "File kết thúc giữa một frame dữ liệu.";
                            frames.Clear();
                            return false;
                        }
                        offset += read;
                    }
                    frames.Add(frame);
                }
            }
            return true;
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
                    if (!file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) continue;

                    List<byte[]> frames;
                    string error;
                    if (!TryReadPlaybackFile(file.FullName, out frames, out error))
                        continue;

                    checkedFiles.Add(file.Name);
                    fileList.Add(new FileData(
                        System.IO.Path.GetFileNameWithoutExtension(file.Name),
                        frames));
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

        public FileData(string fileName, List<byte[]> byteArrayList)
        {
            FileName = fileName;
            FileTotalSecond = (int)Math.Ceiling(
                Math.Max(0, byteArrayList.Count - 1) *
                LoginLoad.FrameIntervalMilliseconds / 1000.0);
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
