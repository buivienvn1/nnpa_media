using NNPA.Update;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NNPA
{
    public partial class Thongtin : Window
    {
        private readonly AppUpdateManager updateManager = new AppUpdateManager();
        private CancellationTokenSource cancellationSource;
        private AppUpdateInfo availableUpdate;
        private bool isBusy;

        public Thongtin()
        {
            InitializeComponent();
            txtCurrentVersion.Text = "Phiên bản " + AppUpdateManager.GetCurrentVersionText();
            chkAutoCheck.IsChecked = Properties.Settings.Default.AutoCheckUpdate;
        }

        public async Task CheckForUpdatesAsync(bool showUpToDateMessage)
        {
            if (isBusy)
                return;

            DisposeCancellation();
            cancellationSource = new CancellationTokenSource();
            SetBusy(true, "Đang kiểm tra phiên bản mới...");
            availableUpdate = null;
            btnUpdate.IsEnabled = false;
            lstChangelog.Items.Clear();
            txtReleaseInfo.Text = String.Empty;

            try
            {
                UpdateCheckResult result = await updateManager.CheckForUpdateAsync(
                    UpdateEndpointProvider.GetManifestEndpoint(),
                    cancellationSource.Token);

                if (result.IsUpdateAvailable)
                {
                    ShowAvailableUpdate(result.UpdateInfo);
                }
                else
                {
                    txtUpdateStatus.Text = "Bạn đang sử dụng phiên bản mới nhất.";
                    if (showUpToDateMessage)
                        MessageBox.Show(this, "Phần mềm đã ở phiên bản mới nhất.", "Cập nhật", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                txtUpdateStatus.Text = "Đã hủy thao tác cập nhật.";
            }
            catch (Exception exception)
            {
                txtUpdateStatus.Text = "Không thể kiểm tra cập nhật: " + GetFriendlyError(exception);
                if (showUpToDateMessage)
                    MessageBox.Show(this, txtUpdateStatus.Text, "Cập nhật", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false, null);
                DisposeCancellation();
            }
        }

        private async void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync(true);
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (availableUpdate == null || isBusy)
                return;

            DisposeCancellation();
            cancellationSource = new CancellationTokenSource();
            SetBusy(true, "Đang tải bộ cài cập nhật...");
            prgDownload.Visibility = Visibility.Visible;
            prgDownload.Value = 0;

            try
            {
                Progress<UpdateDownloadProgress> progress = new Progress<UpdateDownloadProgress>(UpdateDownloadProgressUi);
                string installerPath = await updateManager.DownloadUpdateAsync(
                    availableUpdate,
                    progress,
                    cancellationSource.Token);

                txtUpdateStatus.Text = "Đã tải và xác minh bộ cài. Đang khởi chạy cập nhật...";
                AppUpdateManager.LaunchInstaller(installerPath);
                Application.Current.Shutdown();
            }
            catch (OperationCanceledException)
            {
                txtUpdateStatus.Text = "Đã hủy tải bản cập nhật.";
            }
            catch (Exception exception)
            {
                txtUpdateStatus.Text = "Không thể tải bản cập nhật: " + GetFriendlyError(exception);
                MessageBox.Show(this, txtUpdateStatus.Text, "Cập nhật", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false, null);
                DisposeCancellation();
            }
        }

        private void UpdateDownloadProgressUi(UpdateDownloadProgress progress)
        {
            prgDownload.IsIndeterminate = !progress.TotalBytes.HasValue;
            if (progress.TotalBytes.HasValue)
                prgDownload.Value = progress.Percentage;

            txtDownloadProgress.Text = progress.TotalBytes.HasValue
                ? String.Format("{0}% ({1} / {2})", progress.Percentage, FormatBytes(progress.BytesReceived), FormatBytes(progress.TotalBytes.Value))
                : FormatBytes(progress.BytesReceived);
        }

        public void ShowAvailableUpdate(AppUpdateInfo updateInfo)
        {
            availableUpdate = updateInfo;
            txtUpdateStatus.Text = String.IsNullOrWhiteSpace(updateInfo.Title)
                ? "Có phiên bản mới " + updateInfo.VersionText + "."
                : updateInfo.Title;
            txtReleaseInfo.Text = "Phiên bản " + updateInfo.VersionText
                + (updateInfo.ReleaseDate.HasValue ? " — phát hành " + updateInfo.ReleaseDate.Value.ToString("dd/MM/yyyy") : String.Empty)
                + (updateInfo.IsMandatory ? " — cập nhật bắt buộc" : String.Empty);

            lstChangelog.Items.Clear();
            foreach (string item in updateInfo.Changelog)
                lstChangelog.Items.Add("• " + item);

            btnUpdate.IsEnabled = true;
        }

        private void SetBusy(bool busy, string status)
        {
            isBusy = busy;
            btnCheck.IsEnabled = !busy;
            btnUpdate.IsEnabled = !busy && availableUpdate != null;
            btnDong.Content = busy ? "Hủy" : "Đóng";
            if (!String.IsNullOrWhiteSpace(status))
                txtUpdateStatus.Text = status;
        }

        private void AutoCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            Properties.Settings.Default.AutoCheckUpdate = chkAutoCheck.IsChecked == true;
            Properties.Settings.Default.Save();
        }

        private void BtnTruycap_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://nhacnuocphatan.com/",
                UseShellExecute = true
            });
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy)
                cancellationSource?.Cancel();
            else
                Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (isBusy)
            {
                e.Cancel = true;
                cancellationSource?.Cancel();
                return;
            }
            DisposeCancellation();
            updateManager.Dispose();
            base.OnClosing(e);
        }

        private void DisposeCancellation()
        {
            if (cancellationSource == null)
                return;
            cancellationSource.Dispose();
            cancellationSource = null;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L)
                return (bytes / (1024d * 1024d)).ToString("0.0") + " MB";
            if (bytes >= 1024L)
                return (bytes / 1024d).ToString("0.0") + " KB";
            return bytes + " B";
        }

        private static string GetFriendlyError(Exception exception)
        {
            if (exception is HttpRequestException)
                return "Không kết nối được máy chủ cập nhật. Vui lòng kiểm tra Internet.";
            return exception.Message;
        }
    }
}
