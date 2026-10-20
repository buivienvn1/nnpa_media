using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace NNPA
{
    /// <summary>
    /// Interaction logic for Quanlytaikhoan.xaml
    /// </summary>
    public partial class Quanlytaikhoan : Window
    {
        string apiUrlResetAcc = MainWindow.indexUrl + "reset_acc.php";
        string apiUrlGetDataAcc = MainWindow.indexUrl + "getdata_acc.php";
        string apiUrlDelAcc = MainWindow.indexUrl + "delete_acc.php";
        

        private Uri uri;
        private HttpClient client;

        ObservableCollection<Member> members;
        public ObservableCollection<Member> dataMembers
        {
            get { return (ObservableCollection<Member>)GetValue(TopicListProperty); }
            set { SetValue(TopicListProperty, value); }
        }
        public static readonly DependencyProperty TopicListProperty =
            DependencyProperty.Register("dataMembers", typeof(ObservableCollection<Member>), typeof(Quanlytaikhoan), new UIPropertyMetadata(null));

        public Quanlytaikhoan()
        {
            InitializeComponent();

            LoadData();
        }
        async void LoadData()
        {
            dataMembers = await LoadAcc();
            membersDataGrid.ItemsSource = null;
            membersDataGrid.ItemsSource = dataMembers;
        }


        public async Task<ObservableCollection<Member>> LoadAcc()
        {
            client = new HttpClient();
            uri = new Uri(apiUrlGetDataAcc);

            var response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                members = JsonConvert.DeserializeObject<ObservableCollection<Member>>(content);

            }
            return members;
        }

        public async Task ResetAcc(int id)
        {
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(apiUrlResetAcc);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //setup login data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("idCuaAcc",     id.ToString()),

                });
                //send request
                HttpResponseMessage responseMessage = await client.PostAsync("", formContent);
                //get access token from response body
                var response = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(response);

                //check response
                if (response == "success")
                {
                    MessageBox.Show("Reset tài khoản thành công. Mật khẩu mặc định là: 1111", "Thông báo");
                }
                else
                {
                    MessageBox.Show("Lỗi reset tài khoản", "Thông báo");
                }

                Console.WriteLine("Press any key to exit...");
            }
        }

        public async Task DelAcc(int id)
        {
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(apiUrlDelAcc);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //setup login data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("idCuaAcc",     id.ToString()),

                });
                //send request
                HttpResponseMessage responseMessage = await client.PostAsync("", formContent);
                //get access token from response body
                var response = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(response);

                //check response
                if (response == "success")
                {
                    MessageBox.Show("Xoá tài khoản thành công", "Thông báo");
                }
                else
                {
                    MessageBox.Show("Lỗi xoá tài khoản", "Thông báo");
                }

                Console.WriteLine("Press any key to exit...");
            }
        }

        

        async void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            Member member = (sender as Button).DataContext as Member;

            if (member.code != 1)
            {
                MessageBoxResult result = MessageBox.Show("Bạn có muốn RESET tài khoản " + member.username + " không?", "Cảnh báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    await ResetAcc(member.id);
                }
            }
            else
            {
                MessageBox.Show("Không thể Reset tài khoản Admin");
            }
        }

        async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            Member member = (sender as Button).DataContext as Member;

            if (member.code != 1)
            {
                MessageBoxResult result = MessageBox.Show("Bạn có muốn XOÁ tài khoản " + member.username + " không?", "Cảnh báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    await DelAcc(member.id);
                    for(int i=0; i < dataMembers.Count; i++)
                    {
                        if (dataMembers[i].id == member.id)
                        {
                            dataMembers.RemoveAt(i);
                            break;
                        }
                    }
                    membersDataGrid.ItemsSource = null;
                    membersDataGrid.ItemsSource = dataMembers;
                    
                }
            }
            else
            {
                MessageBox.Show("Không thể Xoá tài khoản Admin");
            }
        }

        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            Themtaikhoan themtaikhoan = new Themtaikhoan();
            themtaikhoan.ShowDialog();
            LoadData();
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class Member
    {
        public int id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public int code { get; set; }
    }
}
