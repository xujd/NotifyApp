using SerialCommunication.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Linq;
using System.Collections.ObjectModel;

namespace SerialCommunication
{
    /// <summary>
    /// Interaction logic for TrainTypeSetting.xaml
    /// </summary>
    public partial class TrainTypeSetting : Window
    {
        MainWindow owner = null;
        ObservableCollection<TrainTypeConfig> configlist = null;
        public TrainTypeSetting(MainWindow owner, ObservableCollection<TrainTypeConfig> configlist)
        {
            InitializeComponent();

            this.owner = owner;
            this.configlist = configlist;
            
            dataGrid.ItemsSource = configlist;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var type = tbTrainType.Text;
            if (string.IsNullOrEmpty(type))
            {
                MessageBox.Show("车型不能为空！");
                return;
            }
            if(configlist.Where(i=>i.TrainType == type).Count() > 0)
            {
                MessageBox.Show("该车型已存在！");
                return;
            }
            int num = 0;
            if(!int.TryParse(tbTypeNo.Text,out num))
            {
                MessageBox.Show("车型码必须为数字！");
                return;
            }
            if (configlist.Where(i => i.AddressNum == num).Count() > 0)
            {
                MessageBox.Show("该车型码已被占用！");
                return;
            }

            if (string.IsNullOrEmpty(tbAddress.Text))
            {
                MessageBox.Show("上沙车地址是数字（如2），且不能为空（多个时逗号分隔）！");
                return;
            }

            try
            {
                var ports = tbAddress.Text.Trim().Split(new char[] { ',', '，' }).Select(i => int.Parse(i)).ToArray();

                configlist.Add(new TrainTypeConfig() { TrainType = type, AddressNum = num, Port = ports });

                owner.SaveTrainConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show("上沙车地址格式错误！" + ex.Message);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if(dataGrid.SelectedItems.Count == 0)
            {
                return;
            }

            var selectedList = new List<TrainTypeConfig>();
            foreach(var item in dataGrid.SelectedItems)
            {
                selectedList.Add(item as TrainTypeConfig);
            }

            foreach(var item in selectedList)
            {
                this.configlist.Remove(item);
            }

            owner.SaveTrainConfig();
        }
    }
}
