using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Themtaikhoan.xaml
    /// </summary>
    public partial class Themtaikhoan : Window
    {
        string apiUrlRegister = MainWindow.indexUrl + "register.php";
        private Uri uri;
        private HttpClient client;

        public Themtaikhoan()
        {
            InitializeComponent();
        }

        public async Task Register(string username, string password, string email)
        {
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(apiUrlRegister);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //setup login data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username",    username),
                    new KeyValuePair<string, string>("password",    password),
                    new KeyValuePair<string, string>("email",       email),

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
                    this.Close();
                }

                Console.WriteLine("Press any key to exit...");
            }
        }

        async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            if (tbUsername.Text != "" && tbPassword.Text != "")
            {
                await Register(tbUsername.Text, tbPassword.Text, tbEmail.Text);
            }
            else
            {
                MessageBox.Show("Tên tài khoản và Mật khẩu không được để trống", "Thông báo");
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
