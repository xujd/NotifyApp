using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfInfoPusher
{
    /// <summary>
    /// Interaction logic for TaskCreate.xaml
    /// </summary>
    public partial class TaskCreate : Window
    {
        public TaskCreate()
        {
            InitializeComponent();
        }

        MainWindow owner = null;
        public TaskCreate(MainWindow owner)
        {
            InitializeComponent();

            this.owner = owner;
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog path = new FolderBrowserDialog();
            path.ShowDialog();
            tbFilePath.Text = path.SelectedPath;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (owner != null)
            {

                var type = rbLed.IsChecked.HasValue && rbLed.IsChecked.Value ? 0 : 1;
                if(type == 0)
                {
                    if (string.IsNullOrEmpty(tbFilePath.Text) || string.IsNullOrEmpty(tbHost.Text) ||
                        string.IsNullOrEmpty(tbUser.Text) || string.IsNullOrEmpty(tbPassword.Password) ||
                        string.IsNullOrEmpty(tbTargetPath.Text) || string.IsNullOrEmpty(tbFtpFile.Text))
                    {
                        System.Windows.MessageBox.Show("存在为空的项，请检查！");
                        return;
                    }
                    owner.AddNewFtpTask(type, tbFilePath.Text, tbHost.Text, tbUser.Text, tbPassword.Password, tbTargetPath.Text, tbFtpFile.Text);
                }
                else
                {
                    if (string.IsNullOrEmpty(tbFilePath.Text) || string.IsNullOrEmpty(tbHostUdp.Text) ||
                        string.IsNullOrEmpty(tbPortUdp.Text))
                    {
                        System.Windows.MessageBox.Show("存在为空的项，请检查！");
                        return;
                    }
                    int port;
                    if (!int.TryParse(tbPortUdp.Text,out port))
                    {
                        System.Windows.MessageBox.Show("UDP端口格式不正确！");
                        return;
                    }
                    owner.AddNewUdpTask(type, tbFilePath.Text, tbHostUdp.Text, tbPortUdp.Text);
                }
            }

            this.Close();
        }
    }
}
