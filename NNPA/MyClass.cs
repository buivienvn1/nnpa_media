using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NNPA.LoginLoad;

namespace NNPA
{
    class MyClass
    {
        public delegate void OnWorkerMethodCompleteDelegate(bool st1);
        public event OnWorkerMethodCompleteDelegate OnWorkerComplete;


        public void WorkerMethod()
        {
            if (PingHost(SvIP))
            {
                OnWorkerComplete(true);
                //lblStatus1.Text = "IP1: " + Properties.Settings.Default.IPAddress + " - Đã kết nối";
                //lblStatus2.Text = "IP2: " + Properties.Settings.Default.IPAddress2 + " - Chưa kết nối";
                //return true;
            }
            else
            {
                OnWorkerComplete(false);
            }
        }
    }
}
