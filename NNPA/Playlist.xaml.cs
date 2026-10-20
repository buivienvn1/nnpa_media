using Newtonsoft.Json;
using NNPA.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
using static NNPA.LoginLoad;

namespace NNPA
{
    /// <summary>
    /// Interaction logic for Playlist.xaml
    /// </summary>
    public partial class Playlist : Window
    {
        string apiUrlUpdateMusic = MainWindow.indexUrl + "update_music.php";
        string apiUrlGetDataMusic = MainWindow.indexUrl + "getdata_music.php";

        private Uri uri;
        private HttpClient client;
        private ObservableCollection<DataMusicItems> musicItems;
        private ObservableCollection<string> checkedfile;
        public ObservableCollection<DataMusicItems> dataMusics
        {
            get { return (ObservableCollection<DataMusicItems>)GetValue(TopicListProperty); }
            set { SetValue(TopicListProperty, value); }
        }
        public static readonly DependencyProperty TopicListProperty =
            DependencyProperty.Register("dataMusics", typeof(ObservableCollection<DataMusicItems>), typeof(Playlist), new UIPropertyMetadata(null));

        int chontatca = 0;


        public Playlist()
        {
            InitializeComponent();
            this.checkedfile = checkedFiles;
            checkedFiles = new ObservableCollection<string>();

            LoadData();
        }

        async void LoadData()
        {
            //dataMusics = await LoadMusic();
            if (selectedPath != "")
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(selectedPath);
                FileInfo[] files = directoryInfo.GetFiles();

                foreach (FileInfo file in files)
                {
                    if (file.Extension == ".txt")
                    {
                        string content = File.ReadAllText(file.FullName);
                        CheckBox checkBox = new CheckBox();
                        checkBox.Content = file.Name.Replace(".txt","");
                        checkBox.Checked += CheckBox_Checked;
                        checkBox.Unchecked += CheckBox_Unchecked;
                        lbPlaylist.Items.Add(checkBox);
                        if (checkedFiles == null) continue;
                        foreach(string o in checkedfile)
                        {
                            if (o == checkBox.Content + ".txt")
                            {
                                checkBox.IsChecked = true;
                                break;
                            }
                        }
                    }
                }
            }
            
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            string fileName = checkBox.Content + ".txt";
            checkedFiles.Add(fileName);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            string fileName = checkBox.Content + ".txt";
            bool removed = LoginLoad.checkedFiles.Remove(fileName);
            Console.WriteLine($"File {fileName} removed: {removed}");
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox checkBox in lbPlaylist.Items)
            {
                checkBox.IsChecked = true;
            }
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox checkBox in lbPlaylist.Items)
            {
                checkBox.IsChecked = false;
            }
        }
        public async Task<ObservableCollection<DataMusicItems>> LoadMusic()
        {
            client = new HttpClient();
            uri = new Uri(apiUrlGetDataMusic);

            var response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                musicItems = JsonConvert.DeserializeObject<ObservableCollection<DataMusicItems>>(content);

            }
            return musicItems;
        }

        public async Task UpdateMusic(int id, int luachon)
        {
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(apiUrlUpdateMusic);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //setup login data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("idNN",     id.ToString()),
                    new KeyValuePair<string, string>("luachonNN",   luachon.ToString()),
                    
                });
                //send request
                HttpResponseMessage responseMessage = await client.PostAsync("", formContent);
                //get access token from response body
                var response = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(response);

                // check response
                //if (response == "success")
                //{
                //    BtnReload_Click(null, null);
                //}

                Console.WriteLine("Press any key to exit...");
            }
        }

        private void BtnChontatca_Click(object sender, RoutedEventArgs e)
        {
            if (chontatca == 1)
            {
                chontatca = 0;
                btnChontatca.Content = "Chọn tất cả";
                DeselectAllButton_Click(null, null);
            }
            else
            {
                chontatca = 1;
                btnChontatca.Content = "Bỏ chọn tất cả";
                SelectAllButton_Click(null, null);

            }

            //foreach (DataMusicItems o in lbPlaylist.Items)
            //    o.LuaChon = chontatca == 1 ? 0 : 1;

            //lbPlaylist.ItemsSource = null;
            //lbPlaylist.ItemsSource = dataMusics;
        }

        async void BtnXacnhan_Click(object sender, RoutedEventArgs e)
        {
            //foreach (DataMusicItems o in lbPlaylist.Items)
            //    await UpdateMusic(o.ID, o.LuaChon);

            if (selectedPath != "" && checkedFiles.Count>0)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(selectedPath);
                FileInfo[] files = directoryInfo.GetFiles();
                fileList = new ObservableCollection<FileData>();
                foreach (FileInfo file in files)
                {
                    string fileName = file.Name;
                    foreach(string o in checkedFiles)
                    {
                        if (o != fileName) continue;

                        List<byte[]> frames;
                        string error;
                        if (!LoginLoad.TryReadPlaybackFile(file.FullName, out frames, out error))
                        {
                            new MessageBoxCustom(error, MessageType.Warning, MessageButtons.Ok).ShowDialog();
                            break;
                        }

                        FileData fileData = new FileData(
                            System.IO.Path.GetFileNameWithoutExtension(fileName),
                            frames);
                        fileList.Add(fileData);
                        break;
                    }
                    
                }
                // Do something with the files array, e.g. display in a ListBox
            }

            this.Close();
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
