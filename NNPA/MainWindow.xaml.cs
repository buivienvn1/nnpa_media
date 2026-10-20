using Newtonsoft.Json;
using NNPA.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace NNPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Connection con = new Connection();
        public static string indexUrl = "http://103.124.93.98/nnpa/22test/";
        string apiUrl = indexUrl+"login.php";
        int code = 2;

        //private Uri uri;
        //private HttpClient client;


        //string username, password;
        public MainWindow()
        {
            InitializeComponent();
        }
        
        #region UI Login
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CbMatkhau_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)cbMatkhau.IsChecked)
            {
                ShowPasswordFunction();
            }
            else
            {
                HidePasswordFunction();
            }
        }

        private void ShowPasswordFunction()
        {
            PasswordUnmask.Visibility = Visibility.Visible;
            txtMatkhau.Visibility = Visibility.Hidden;
            PasswordUnmask.Text = txtMatkhau.Password;
        }

        private void HidePasswordFunction()
        {
            PasswordUnmask.Visibility = Visibility.Hidden;
            txtMatkhau.Visibility = Visibility.Visible;
            txtMatkhau.Password = PasswordUnmask.Text;
        }

        #endregion

        async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtTaikhoan.Text != "" && txtMatkhau.Password != "")
                {
                    string username = txtTaikhoan.Text;
                    string password = txtMatkhau.Password;
                    await Task.Run(() => LoginAsync(username, password));
                }
                else
                {
                    MessageBox.Show("Tài khoản hoặc mật khẩu không được để trống", "Thông báo");
                }
            }
            catch
            {
                new MessageBoxCustom("Kết nối bị lỗi", MessageType.Error,MessageButtons.Ok).ShowDialog();
            }

            if (code == 1 || code == 0)
            {
                //this.Dispatcher.Invoke(() =>
                //{
                    LoginLoad loginLoad = new LoginLoad();
                    loginLoad.Show();
                    this.Close();
                //});
                
            }
        }

        //public async Task<List<Items>> ReadData()
        //{
        //    client = new HttpClient();
        //    uri = new Uri(apiUrl);

        //    var response = await client.GetAsync(uri);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        item = JsonConvert.DeserializeObject<List<Items>>(content);
        //    }
        //    return item;
        //}

        public async Task LoginAsync(string username, string password)
        {
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //setup login data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                });
                //send request
                HttpResponseMessage responseMessage = await client.PostAsync("", formContent);
                //get access token from response body
                var response = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(response);
                // decode json string to dto object
                API_Response r = JsonConvert.DeserializeObject<API_Response>(response);

                //show message
                MessageBox.Show(r.message);

                // check response
                if (!r.IsError && r.success == 1)
                {
                    code = r.code;

                    //this.Dispatcher.Invoke(() =>
                    //{
                    //    LoginLoad loginLoad = new LoginLoad(txtTaikhoan.Text, code);
                    //    loginLoad.Show();
                    //    this.Close();
                    //});
                }

                Console.WriteLine("Press any key to exit...");
                //Console.ReadKey();
            }
        }
    }
    public class API_Response
    {
        public bool IsError { get; set; }
        public int success { get; set; }
        public string message { get; set; }
        public int code { get; set; }
    }
}
