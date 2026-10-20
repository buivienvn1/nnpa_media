using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NNPA.Update
{
    [DataContract]
    public sealed class AppUpdateInfo
    {
        public AppUpdateInfo()
        {
            Changelog = new List<string>();
        }

        [DataMember(Name = "version")]
        public string VersionText { get; set; }

        [DataMember(Name = "releaseDate")]
        public string ReleaseDateText { get; set; }

        [DataMember(Name = "downloadUrl")]
        public string DownloadUrl { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "isMandatory")]
        public bool IsMandatory { get; set; }

        [DataMember(Name = "changelog")]
        public List<string> Changelog { get; set; }

        [DataMember(Name = "sha256", EmitDefaultValue = false)]
        public string Sha256 { get; set; }

        public Version ParsedVersion
        {
            get
            {
                Version result;
                return System.Version.TryParse(VersionText, out result)
                    ? result
                    : null;
            }
        }

        public DateTime? ReleaseDate
        {
            get
            {
                DateTime result;
                return DateTime.TryParse(
                    ReleaseDateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out result)
                    ? result
                    : (DateTime?)null;
            }
        }
    }

    public sealed class UpdateCheckResult
    {
        public Version CurrentVersion { get; set; }
        public AppUpdateInfo UpdateInfo { get; set; }
        public bool IsUpdateAvailable { get; set; }
    }

    public sealed class UpdateDownloadProgress
    {
        public long BytesReceived { get; set; }
        public long? TotalBytes { get; set; }

        public int Percentage
        {
            get
            {
                if (!TotalBytes.HasValue || TotalBytes.Value <= 0)
                    return 0;

                return (int)Math.Min(
                    100,
                    BytesReceived * 100L / TotalBytes.Value);
            }
        }
    }

    public sealed class AppUpdateManager : IDisposable
    {
        private const int BufferSize = 81920;
        private readonly HttpClient httpClient;
        private bool isDisposed;

        public AppUpdateManager()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "NNPA-Media/" + GetCurrentVersionText());
        }

        public static Version CurrentVersion
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                return assembly.GetName().Version ?? new Version(0, 0, 0, 0);
            }
        }

        public static string GetCurrentVersionText()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyInformationalVersionAttribute informational = assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();

            if (informational != null &&
                !String.IsNullOrWhiteSpace(informational.InformationalVersion))
            {
                return informational.InformationalVersion.Trim();
            }

            Version version = CurrentVersion;
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}.{2}",
                version.Major,
                version.Minor,
                Math.Max(0, version.Build));
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(
            string manifestUrl,
            CancellationToken cancellationToken)
        {
            Uri manifestUri;
            if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out manifestUri) ||
                (manifestUri.Scheme != Uri.UriSchemeHttps &&
                 manifestUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException(
                    "Địa chỉ kiểm tra cập nhật không hợp lệ.");
            }

            string json = await httpClient.GetStringAsync(manifestUri)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            AppUpdateInfo updateInfo = DeserializeManifest(json);
            ValidateManifest(updateInfo);

            return new UpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                UpdateInfo = updateInfo,
                IsUpdateAvailable = updateInfo.ParsedVersion.CompareTo(CurrentVersion) > 0
            };
        }

        public async Task<string> DownloadUpdateAsync(
            AppUpdateInfo updateInfo,
            IProgress<UpdateDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            ValidateManifest(updateInfo);

            Uri downloadUri = new Uri(updateInfo.DownloadUrl, UriKind.Absolute);
            string safeVersion = updateInfo.VersionText.Replace('.', '-');
            string destinationPath = Path.Combine(
                Path.GetTempPath(),
                "NNPA-Media-Setup-" + safeVersion + "-update.exe");
            string temporaryPath = destinationPath + ".download";

            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            try
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(
                    downloadUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (Stream source = await response.Content.ReadAsStreamAsync()
                        .ConfigureAwait(false))
                    using (FileStream destination = new FileStream(
                        temporaryPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        BufferSize,
                        true))
                    {
                        byte[] buffer = new byte[BufferSize];
                        long received = 0;
                        int read;
                        while ((read = await source.ReadAsync(
                            buffer,
                            0,
                            buffer.Length,
                            cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            await destination.WriteAsync(
                                buffer,
                                0,
                                read,
                                cancellationToken).ConfigureAwait(false);
                            received += read;
                            if (progress != null)
                            {
                                progress.Report(new UpdateDownloadProgress
                                {
                                    BytesReceived = received,
                                    TotalBytes = totalBytes
                                });
                            }
                        }
                    }
                }

                if (!String.IsNullOrWhiteSpace(updateInfo.Sha256))
                    VerifySha256(temporaryPath, updateInfo.Sha256);

                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                File.Move(temporaryPath, destinationPath);
                return destinationPath;
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                throw;
            }
        }

        public static void LaunchInstaller(string installerPath)
        {
            if (String.IsNullOrWhiteSpace(installerPath) ||
                !File.Exists(installerPath))
            {
                throw new FileNotFoundException(
                    "Không tìm thấy bộ cài cập nhật.",
                    installerPath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(installerPath)
            });
        }

        private static AppUpdateInfo DeserializeManifest(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("Máy chủ trả về dữ liệu cập nhật rỗng.");

            try
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(AppUpdateInfo));
                using (MemoryStream stream = new MemoryStream(
                    Encoding.UTF8.GetBytes(json)))
                {
                    return (AppUpdateInfo)serializer.ReadObject(stream);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidDataException(
                    "Tệp version.json không đúng định dạng.",
                    exception);
            }
        }

        private static void ValidateManifest(AppUpdateInfo updateInfo)
        {
            if (updateInfo == null || updateInfo.ParsedVersion == null)
                throw new InvalidDataException("Phiên bản trên máy chủ không hợp lệ.");

            Uri downloadUri;
            if (!Uri.TryCreate(updateInfo.DownloadUrl, UriKind.Absolute, out downloadUri) ||
                (downloadUri.Scheme != Uri.UriSchemeHttps &&
                 downloadUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidDataException(
                    "Địa chỉ tải bộ cài trên máy chủ không hợp lệ.");
            }

            if (updateInfo.Changelog == null)
                updateInfo.Changelog = new List<string>();
        }

        private static void VerifySha256(string filePath, string expectedHash)
        {
            string normalizedExpected = expectedHash
                .Replace("-", String.Empty)
                .Replace(" ", String.Empty)
                .Trim();

            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                string actualHash = BitConverter.ToString(hash)
                    .Replace("-", String.Empty);
                if (!String.Equals(
                    actualHash,
                    normalizedExpected,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "Bộ cài tải xuống không khớp mã SHA-256. " +
                        "Tệp có thể bị lỗi hoặc đã bị thay đổi.");
                }
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            httpClient.Dispose();
            isDisposed = true;
        }
    }
}
