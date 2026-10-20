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
    /// Interaction logic for Doimatkhau.xaml
    /// </summary>
    public partial class Doimatkhau : Window
    {
        string tUser;
        string apiUrlChangePass = MainWindow.indexUrl + "change_password.php";
        public Doimatkhau(string tUser)
        {
            InitializeComponent();
            this.tUser = tUser;
        }
        public async Task UpdatePass(string username, string mkCu, string mkMoi)
        {
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri(apiUrlChangePass);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //setup login data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username",     username),
                    new KeyValuePair<string, string>("oldpassword",   mkCu),
                    new KeyValuePair<string, string>("newpassword",   mkMoi),

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


        async void BtnXacnhan_Click(object sender, RoutedEventArgs e)
        {
            if(txtMkCu.Password!="" && txtMkMoi.Password != "" && txtMkMoiLap.Password != "")
            {
                if (txtMkMoi.Password == txtMkMoiLap.Password)
                {
                    string mkCu = txtMkCu.Password.ToString();
                    string mkMoi = txtMkMoi.Password.ToString();
                    await UpdatePass(tUser,mkCu,mkMoi);
                }
                else
                {
                    MessageBox.Show("Mật khẩu mới không trùng khớp", "Thông báo");
                }
            }
            else
            {
                MessageBox.Show("Mật khẩu không được để trống", "Thông báo");
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
