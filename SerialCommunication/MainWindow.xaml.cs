using SerialCommunication.Helpers;
using SerialCommunication.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SerialCommunication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ComConfig config = new ComConfig();
        List<string> bitRateList = new List<string>() { "300", "600", "1200", "2400", "4800", "9600", "19200", "38400", "115200" };
        List<string> dataBitList = new List<string>() { "5", "6", "7", "8" };
        List<string> stopBitList = new List<string>() { "1", "1.5", "2" };
        List<string> parityList = new List<string>() { "无", "奇校验", "偶校验" };

        List<SerialPort> serialPorts = new List<SerialPort>();
        //扫描定时器
        private DispatcherTimer timer = new DispatcherTimer();

        private DateTime lastestTime = DateTime.Now;

        private DataTable dataTable = null;
        private string dtColumns = "日期,时间,上沙车地址,机车车型,机车车号,砂箱1加沙量,砂箱2加沙量,砂箱3加沙量,砂箱4加沙量,砂箱5加沙量,砂箱6加沙量,砂箱7加沙量,砂箱8加沙量,加沙总量";

        //数据解析线程
        Thread parseThread;
        object objlck = new object();
        CancellationTokenSource cts = new CancellationTokenSource();

        // 沙箱配置
        private List<TrainBox> boxConfigList = new List<TrainBox>();

        // 版本号
        private string VERSION = "2.1";  // 1.0

        private string dataFile
        {
            get { return Param.APPFILEPATH + @"\data\" + DateTime.Now.ToString("yyyyMMdd") + ".csv"; }
        }

        public MainWindow()
        {
            InitializeComponent();

            //this.GetTrainMessage(new TrainTypeConfig() { AddressNum = 1, TrainType = "DF2", Port = new int[2] { 1, 2 } }, "0x01", "1234");

            //this.UpdateBoxInfo("DF2", "1982", new List<string>()
            //{
            //    "198","82","28","132","245","238","123","98",
            //    "1198","182","128","1132","1245","1238","1123","198",
            //    "2198","282","228","2132","2245","2238","2123","298",
            //});

            this.Closing += MainWindow_Closing;

            Log.dispatcher = this.Dispatcher;

            parseThread = new Thread(new ThreadStart(parseMessage));
            parseThread.Start();

            Param.Init();

            this.lbLog.ItemsSource = Log.LogList;

            dataTable = CSVFileHelper.OpenCSV(dataFile);
            if (dataTable == null)
            {
                dataTable = new DataTable();
                var columns = dtColumns.Split(',');
                foreach (var col in columns)
                {
                    DataColumn dc = new DataColumn(col);
                    dataTable.Columns.Add(dc);
                }
                CSVFileHelper.SaveCSV(dataTable, dataFile);
            }

            dataGrid.ItemsSource = dataTable.DefaultView;

            CheckSerialPort();

            this.cbBitRate.ItemsSource = bitRateList;
            this.cbBitRate.SelectedIndex = 5;//默认9600
            this.cbDataBit.ItemsSource = dataBitList;
            this.cbDataBit.SelectedIndex = 3;//默认8
            this.cbStopBit.ItemsSource = stopBitList;
            this.cbStopBit.SelectedIndex = 0;//默认1
            this.cbParity.ItemsSource = parityList;
            this.cbParity.SelectedIndex = 0;//默认无

            ReadConfig();

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void MainWindow_Closing1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            throw new NotImplementedException();
        }

        private TrainTypeConfig curTrainType = null;
        private string curTrainNo = null;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (serialPorts.Count == 0 || !serialPorts[0].IsOpen || string.IsNullOrEmpty(config.FilePath)) //如果没打开
            {
                return;
            }

            //发现新文件
            var files = GetFiles(config.FilePath + @"\车型\");
            if (files == null)
            {
                Log.WriteLog(string.Format("ERROR：车型文件夹不存在！路径：" + config.FilePath + @"\车型\"));
                return;
            }
            if (files.Count > 0)
            {
                //5分钟以内正在处理的车型，防止重复发送
                if (curTrainType != null && ReadFile(files.First()).Contains(curTrainType.TrainType) && (files.First().LastWriteTime - lastestTime).TotalMinutes < 5)
                {
                    lastestTime = files.First().LastWriteTime;
                    return;
                }
                //更新时间
                lastestTime = files.First().LastWriteTime;
                // 向前5秒，作为检查车号的起始时间
                var tempTime = lastestTime.AddSeconds(-5);
                //读取车型
                string trainType = "";
                string trainNo = "";
                curTrainNo = "";
                curTrainType = null;
                var content = ReadFile(files.First()).Trim();
                if (string.IsNullOrEmpty(content))
                {
                    Log.WriteLog(string.Format("ERROR：车辆识别信息错误，车型为空！跳过..."));
                }
                else
                {
                    trainType = content;
                    Log.WriteLog(string.Format("INFO：读取车型信息，车型为-{0}", trainType));
                    //车型
                    curTrainType = trainTypeConfigList.Where(i => i.TrainType == trainType).FirstOrDefault();

                    if (curTrainType == null && curTrainType.Port == null)
                    {
                        Log.WriteLog(string.Format("ERROR：车型未包含在上沙车地址列表中，请先进行车型配置！"));
                        return;
                    }
                    //读取车号
                    Log.WriteLog(string.Format("INFO：开始读取车号信息..."));
                    DispatcherTimer timerCheck = new DispatcherTimer();
                    timerCheck.Interval = TimeSpan.FromSeconds(2);//每2秒执行一次
                    EventHandler handler = null;
                    int index = 0;
                    timerCheck.Tick += handler = (s, a) =>
                    {
                        var trainNoFiles = GetFiles(config.FilePath + @"\车号\", tempTime);
                        if (trainNoFiles == null)
                        {
                            Log.WriteLog(string.Format("ERROR：车号文件夹不存在！路径：" + config.FilePath + @"\车号\"));
                            timerCheck.Tick -= handler;
                            timerCheck.Stop();
                            return;
                        }
                        if (trainNoFiles.Count > 0 && trainNoFiles.First().LastWriteTime >= tempTime)//找到车号文件
                        {
                            try
                            {
                                trainNo = ReadFile(trainNoFiles.First()).Trim();
                                if (string.IsNullOrEmpty(trainNo))
                                {
                                    trainNo = "0000";
                                    Log.WriteLog(string.Format("INFO：车号为空，使用默认车号-0000"));
                                }
                                else
                                {
                                    Log.WriteLog(string.Format("INFO：读取车号信息，车号为-{0}", trainNo));
                                }
                                //找到最佳车号
                                if (!string.IsNullOrEmpty(trainNo) && trainNo.Length == 4 && Regex.IsMatch(trainNo, @"^\d{4}$"))
                                {
                                    timerCheck.Tick -= handler;
                                    timerCheck.Stop();
                                    trainNo = DecimalStrToHex(trainNo);
                                    curTrainNo = trainNo;
                                    //第一次发送第一个地址
                                    // v1.0
                                    var message = "";
                                    if (VERSION == "1.0")
                                    {
                                        message = string.Format("0x{0},0xa0,0x{1},0x{2},0x{3},0x01,0x08,0x0d", curTrainType.Port[0].ToString("x2"), curTrainType.AddressNum.ToString("x2"), trainNo.Substring(0, 2), trainNo.Substring(2, 2));
                                    }
                                    else if (VERSION == "2.1") // v2.1 2018-8-5 增加沙箱信息
                                    {
                                        message = this.GetTrainMessage(curTrainType, curTrainType.Port[0].ToString("x2"), trainNo);
                                        // string.Format("0x{0},0xa0,0x{1},0x{2},0x{3},0x07,0x0d", curTrainType.Port[0].ToString("x2"), curTrainType.AddressNum.ToString("x2"), trainNo.Substring(0, 2), trainNo.Substring(2, 2));
                                    }
                                    Log.WriteLog(string.Format("INFO：开始发送识别车辆信息：{0}", message));
                                    this.SendMessage(message);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLog("ERROR：读取车号错误，信息：" + ex.Message);
                            }
                        }
                        index++;
                        if (index == 10 && string.IsNullOrEmpty(trainNo))//20秒，还未更新
                        {
                            timerCheck.Tick -= handler;
                            timerCheck.Stop();

                            if (VERSION == "1.0")
                            {
                                trainNo = "0000";
                                curTrainNo = trainNo;

                                Log.WriteLog(string.Format("INFO：20秒内车号未识别，使用默认车号-0000"));

                                //第一次发送第一个地址
                                // v1.0
                                var message = string.Format("0x{0},0xa0,0x{1},0x{2},0x{3},0x01,0x08,0x0d", curTrainType.Port[0].ToString("x2"), curTrainType.AddressNum.ToString("x2"), trainNo.Substring(0, 2), trainNo.Substring(2, 2));

                                Log.WriteLog(string.Format("INFO：开始发送第一次识别信息！信息：{0}", message));
                                this.SendMessage(message);
                            }
                        }
                    };
                    //启动车号检测
                    timerCheck.Start();
                }
                //START 车型车号在一个文件
                //timer.Stop();
                ////查找最佳文件
                //ParseFileAndSendMessage();
                //END
            }
        }

        private string GetTrainMessage(TrainTypeConfig trainType, string port, string trainNo)
        {
            List<BoxInfo> list = new List<BoxInfo>();
            // 根据车型和车号取配置
            var config = this.boxConfigList.Where(i => i.TrainType == trainType.TrainType && i.TrainNo == trainNo).FirstOrDefault();
            if (config == null)  // 没有时，根据车型取
            {
                config = this.boxConfigList.Where(i => i.TrainType == trainType.TrainType).FirstOrDefault();
            }
            if (config != null) // 取到配置
            {
                list = config.BoxList;
            }
            else  // 否则
            {
                // Random rand = new Random();
                for (int i = 1; i < 9; i++)
                {
                    list.Add(new BoxInfo() { No = i/*, FrontDist = rand.Next(10, 500), Height = rand.Next(10, 100), Position = rand.Next(500, 2000)*/ });
                }
            }

            // 生成发送信息
            string message = string.Format("0x{0},0xa0,0x{1},0x{2},0x{3}", port, trainType.AddressNum.ToString("x2"), trainNo.Substring(0, 2), trainNo.Substring(2, 2));
            var msgFragments = new List<string>() { message };
            var positionList = new List<string>();
            var heightList = new List<string>();
            var frontDistList = new List<string>();
            var endMessage = new List<string>() { "0x37,0x0d" };
            list.ForEach(item =>
            {
                // 位置
                var hexStr = this.DecimalStrToHex(item.Position.ToString());
                positionList.Add(string.Format("0x{0},0x{1}", hexStr.Substring(0, 2), hexStr.Substring(2, 2)));
                // 高度
                hexStr = this.DecimalStrToHex(item.Height.ToString());
                heightList.Add(string.Format("0x{0},0x{1}", hexStr.Substring(0, 2), hexStr.Substring(2, 2)));
                // 前伸
                hexStr = this.DecimalStrToHex(item.FrontDist.ToString());
                frontDistList.Add(string.Format("0x{0},0x{1}", hexStr.Substring(0, 2), hexStr.Substring(2, 2)));
            });
            var total = msgFragments.Concat(positionList).Concat(heightList).Concat(frontDistList).Concat(endMessage);
            string result = string.Join(",", total);

            return result;
        }

        private List<FileInfo> GetFiles(string filePath, DateTime? time = null)
        {
            if (!time.HasValue)
            {
                time = lastestTime;
            }
            var folder = new DirectoryInfo(filePath);
            if (!folder.Exists)
            {
                return null;
            }
            //新的文件
            var files = (folder.GetFiles().Where(i => i.LastWriteTime > time.Value).OrderByDescending(i => i.LastWriteTime)).ToList();
            return files;
        }

        private string ReadFile(FileInfo file)
        {
            var content = "";
            using (var fileStream = file.OpenRead())
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.Default))
                {
                    content = streamReader.ReadToEnd().Trim();
                }
                //fileStream.Close();
                //fileStream.Dispose();
                //streamReader.Close();
                //streamReader.Dispose();
            }
            return content;
        }

        // 未引用
        private void ParseFileAndSendMessage()
        {
            DispatcherTimer timerCheck = new DispatcherTimer();
            timerCheck.Interval = TimeSpan.FromSeconds(3);//查看3秒内是否还有新增文件
            EventHandler handler = null;
            int fileNum = GetFiles(config.FilePath).Count;
            timerCheck.Tick += handler = (s, a) =>
            {
                //检测停止
                if (fileNum == GetFiles(config.FilePath).Count)
                {
                    timerCheck.Tick -= handler;
                    timerCheck.Stop();

                    var tempfiles = GetFiles(config.FilePath);
                    lastestTime = tempfiles.First().LastWriteTime;
                    string trainType = "";
                    string trainNo = "";
                    curTrainNo = "";
                    curTrainType = null;
                    foreach (var file in tempfiles)
                    {
                        Log.WriteLog(string.Format("INFO：发现新车号文件{0}({1})。", file.Name, file.LastWriteTime.ToString()));
                        var content = ReadFile(file);
                        Log.WriteLog(string.Format("INFO：读取车号信息--{0}。", content));
                        if (content.Contains(','))
                        {
                            var temps = content.Split(',');
                            if (temps.Length < 2)
                            {
                                Log.WriteLog(string.Format("ERROR：车辆识别信息格式错误！读取下一个文件..."));
                                continue;
                            }
                            trainType = temps[0];
                            //找到最佳车号
                            if (!string.IsNullOrEmpty(trainNo) && trainNo.Length == 4 && Regex.IsMatch(trainNo, @"^\d{4}$"))
                            {
                                trainNo = DecimalStrToHex(temps[1]);
                                break;
                            }
                        }
                    }
                    //确实未找到时，填充0000
                    if (string.IsNullOrEmpty(trainNo))
                    {
                        trainNo = "0000";
                    }
                    curTrainNo = trainNo;
                    if (!string.IsNullOrEmpty(curTrainNo) && !string.IsNullOrEmpty(trainType))
                    {
                        curTrainType = trainTypeConfigList.Where(i => i.TrainType == trainType).FirstOrDefault();
                        if (curTrainType == null && curTrainType.Port == null)
                        {
                            Log.WriteLog(string.Format("ERROR：车型未包含在上沙车地址列表中，请先进行车型配置！"));
                            return;
                        }
                        //第一次发送第一个地址
                        var message = string.Format("0x{0},0xa0,0x{1},0x{2},0x{3},0x01,0x08,0x0d", curTrainType.Port[0].ToString("x2"), curTrainType.AddressNum.ToString("x2"), trainNo.Substring(0, 2), trainNo.Substring(2, 2));
                        this.SendMessage(message);
                    }
                    else
                    {
                        Log.WriteLog(string.Format("ERROR：车辆信息格式错误！"));
                    }
                    //重新开始计时器
                    timer.Start();
                }
            };
            timerCheck.Start();
        }

        private string DecimalStrToHex(string trainNo)
        {
            int a = 0;
            var str = "";
            if (int.TryParse(trainNo, out a))
            {
                str = a.ToString("x4");
            }
            return str;
        }

        private string HexToDecimal(string num)
        {
            return Int32.Parse(num, System.Globalization.NumberStyles.HexNumber).ToString();
        }

        private void CheckSerialPort()
        {
            //检查是否含有串口
            string[] str = SerialPort.GetPortNames();
            if (str == null)
            {
                System.Windows.MessageBox.Show("本机没有串口！", "Error");
                return;
            }

            //添加串口项目
            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                cbSerialPort.Items.Add(s);
            }

            //for(var i = 0; i < 10; i++)
            //{
            //    cbSerialPort.Items.Add(i.ToString());
            //}
            if (cbSerialPort.Items.Count > 0)//默认选中第一个
            {
                cbSerialPort.SelectedItem = cbSerialPort.Items[0];
            }
        }

        private void UpdateConfig()
        {
            var str = SerializeHelper.ScriptSerializeToXML<ComConfig>(config);
            File.WriteAllText(Param.APPFILEPATH + "/config.xml", str, Encoding.UTF8);
        }

        private void UpdateBoxInfo(string trainType, string trainNo, List<string> boxInfos)
        {
            var config = this.boxConfigList.Where(i => i.TrainType == trainType && i.TrainNo == trainNo).FirstOrDefault();
            if (config == null)
            {
                config = new TrainBox() { TrainType = trainType, TrainNo = trainNo };
                this.boxConfigList.Add(config);
            }

            config.BoxList.Clear();

            for (int i = 0; i < boxInfos.Count / 3; i++)
            {
                config.BoxList.Add(new BoxInfo()
                {
                    No = i + 1,
                    Position = int.Parse(boxInfos[i]),
                    Height = int.Parse(boxInfos[i + 8]),
                    FrontDist = int.Parse(boxInfos[i + 16])
                });
            }

            var str = SerializeHelper.ScriptSerializeToXML<List<TrainBox>>(this.boxConfigList);
            File.WriteAllText(Param.APPFILEPATH + "/box_config.xml", str, Encoding.UTF8);
        }

        internal void SaveTrainConfig()
        {
            var str = SerializeHelper.ScriptSerializeToXML<ObservableCollection<TrainTypeConfig>>(trainTypeConfigList);
            File.WriteAllText(Param.APPFILEPATH + "/train_config.xml", str, Encoding.UTF8);
        }

        private void ReadConfig()
        {
            try
            {
                var str = File.ReadAllText(Param.APPFILEPATH + "/config.xml");
                var temp = SerializeHelper.JSONXMLToObject<ComConfig>(str);
                if (temp != null)
                {
                    this.config = temp;
                    SetConfigToView();

                    Log.WriteLog("INFO：读取任务配置完毕。");
                }

                str = File.ReadAllText(Param.APPFILEPATH + "/train_config.xml");
                var temp2 = SerializeHelper.JSONXMLToObject<ObservableCollection<TrainTypeConfig>>(str);
                if (temp2 != null)
                {
                    foreach (var item in temp2)
                    {
                        trainTypeConfigList.Add(item);
                    }

                    Log.WriteLog("INFO：读取车型配置完毕。");
                }

                if (File.Exists(Param.APPFILEPATH + "/box_config.xml"))
                {
                    Log.WriteLog("INFO：读取沙箱配置完毕。");
                    str = File.ReadAllText(Param.APPFILEPATH + "/box_config.xml");
                    var temp3 = SerializeHelper.JSONXMLToObject<List<TrainBox>>(str);
                    if (temp3 != null)
                    {
                        this.boxConfigList = temp3;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Log.WriteLog("ERROR：读取任务配置出错！");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentMessage))
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("任务正在运行，确定退出吗？退出后，任务会终止。", "确认？", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                //关闭窗口
                if (result == MessageBoxResult.OK)
                {
                    CSVFileHelper.SaveCSV(dataTable, dataFile);
                    timer.Stop();
                    cts.Cancel();
                    e.Cancel = false;
                }
                //不关闭窗口
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
            else
            {
                cts.Cancel();
            }
        }

        private ObservableCollection<TrainTypeConfig> trainTypeConfigList = new ObservableCollection<TrainTypeConfig>();
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as System.Windows.Controls.MenuItem).Tag.ToString();
            switch (tag)
            {
                case "About":
                    (new About()).ShowDialog();
                    break;
                case "Main":
                    dataGrid.Visibility = tbOpenDataFolder.Visibility = Visibility.Collapsed;
                    dashboard.Visibility = Visibility.Visible;
                    lbLog.Visibility = Visibility.Collapsed;
                    tbTitle.Text = "控制面板";
                    break;
                case "Log":
                    dataGrid.Visibility = tbOpenDataFolder.Visibility = Visibility.Collapsed;
                    dashboard.Visibility = Visibility.Collapsed;
                    lbLog.Visibility = Visibility.Visible;
                    tbTitle.Text = "最新日志";
                    break;
                case "Data":
                    dataGrid.Visibility = tbOpenDataFolder.Visibility = Visibility.Visible;
                    dashboard.Visibility = Visibility.Collapsed;
                    lbLog.Visibility = Visibility.Collapsed;
                    tbTitle.Text = "加沙数据";
                    break;
                case "TypeSetting":
                    (new TrainTypeSetting(this, trainTypeConfigList)).ShowDialog();
                    break;
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog path = new FolderBrowserDialog();
            path.ShowDialog();
            tbFilePath.Text = path.SelectedPath;
        }

        private bool Check()
        {
            string error = "";
            if (string.IsNullOrEmpty(tbFilePath.Text))
            {
                error += "车号文件路径不能为空！\r\n";
            }
            if (cbSerialPort.SelectedItems.Count == 0)
            {
                error += "未选择串口！\r\n";
            }
            if (cbBitRate.SelectedItem == null)
            {
                error += "未选择波特率！\r\n";
            }
            if (cbDataBit.SelectedItem == null)
            {
                error += "未选择数据位！\r\n";
            }
            if (cbStopBit.SelectedItem == null)
            {
                error += "未选择停止位！\r\n";
            }
            if (cbParity.SelectedItem == null)
            {
                error += "未选择校验位！\r\n";
            }
            if (!string.IsNullOrEmpty(error))
            {
                System.Windows.MessageBox.Show(error);
            }

            return string.IsNullOrEmpty(error);
        }

        private void SetConfigToView()
        {
            tbFilePath.Text = config.FilePath;

            var serialPorts = config.SerialPort.Split(',');
            cbSerialPort.SelectedItems.Clear();
            for (var i = 0; i < cbSerialPort.Items.Count; i++)
            {
                if (serialPorts.Contains((cbSerialPort.Items[i]).ToString()))
                {
                    cbSerialPort.SelectedItems.Add((cbSerialPort.Items[i]).ToString());
                }
            }

            for (var i = 0; i < bitRateList.Count; i++)
            {
                if (bitRateList[i] == config.BitRate)
                {
                    cbBitRate.SelectedIndex = i;
                }
            }

            for (var i = 0; i < dataBitList.Count; i++)
            {
                if (dataBitList[i] == config.DataBit)
                {
                    cbDataBit.SelectedIndex = i;
                }
            }

            for (var i = 0; i < stopBitList.Count; i++)
            {
                if (stopBitList[i] == config.StopBit)
                {
                    cbStopBit.SelectedIndex = i;
                }
            }

            for (var i = 0; i < parityList.Count; i++)
            {
                if (parityList[i] == config.Parity)
                {
                    cbParity.SelectedIndex = i;
                }
            }
        }

        private bool GetConfigFromView()
        {
            if (!Check())
            {
                return false;
            }
            config.FilePath = tbFilePath.Text;
            List<string> seletedPorts = new List<string>();
            foreach (var item in cbSerialPort.SelectedItems)
            {
                seletedPorts.Add(item.ToString());
            }
            config.SerialPort = string.Join(",", seletedPorts);
            config.BitRate = cbBitRate.SelectedItem.ToString();
            config.DataBit = cbDataBit.SelectedItem.ToString();
            config.StopBit = cbStopBit.SelectedItem.ToString();
            config.Parity = cbParity.SelectedItem.ToString();

            return true;
        }

        private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (GetConfigFromView())
            {
                UpdateConfig();

                ShowMessage("INFO：保存设置成功！");
            }
            else
            {
                ShowMessage("ERROR：配置校验失败！");
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (btnStart.Content.ToString() == "打开串口")
            {
                if (!GetConfigFromView())
                {
                    return;
                }

                UpdateConfig();

                if (!OpenSerialPort())
                {
                    return;
                }

                btnStart.Content = "关闭串口";

                btnOpen.IsEnabled = cbSerialPort.IsEnabled = cbBitRate.IsEnabled = cbDataBit.IsEnabled = cbStopBit.IsEnabled = cbParity.IsEnabled = false;
                btnSaveConfig.IsEnabled = false;
                timer.Start();

                ShowMessage("INFO：连接成功！");
            }
            else
            {
                if (serialPorts.Count > 0)//先关闭
                {
                    foreach (var item in serialPorts)
                    {
                        item.DataReceived -= Sp_DataReceived;
                        if (item.IsOpen)
                        {
                            item.Close();
                        }
                    }
                }
                serialPorts.Clear();

                btnStart.Content = "打开串口";
                btnOpen.IsEnabled = cbSerialPort.IsEnabled = cbBitRate.IsEnabled = cbDataBit.IsEnabled = cbStopBit.IsEnabled = cbParity.IsEnabled = true;
                btnSaveConfig.IsEnabled = true;
                timer.Stop();
                ShowMessage("INFO：连接关闭！");
            }
        }

        private bool OpenSerialPort()
        {
            if (serialPorts.Count > 0)//先关闭
            {
                foreach (var item in serialPorts)
                {
                    item.DataReceived -= Sp_DataReceived;
                    if (item.IsOpen)
                    {
                        item.Close();
                    }
                }
            }
            serialPorts.Clear();
            var serialNames = config.SerialPort.Split(',');
            if (serialNames.Count() == 0)
            {
                return false;
            }

            var flags = new List<bool>();
            foreach (var item in serialNames)
            {
                flags.Add(CreateSerialPort(item));
            }

            //全部创建成功
            return flags.All(i => i);
        }

        private bool CreateSerialPort(string serialName)
        {
            //创建新串口
            try
            {
                var sp = new SerialPort();

                sp.DtrEnable = true;
                sp.RtsEnable = true;
                //设置数据读取超时为1秒
                sp.ReadTimeout = 1000;
                sp.DataReceived += Sp_DataReceived;

                //设置串口号
                sp.PortName = serialName;
                //sp.DiscardInBuffer();
                //设置各“串口设置”
                string strBaudRate = config.BitRate;
                string strDateBits = config.DataBit;
                string strStopBits = config.StopBit;
                Int32 iBaudRate = Convert.ToInt32(strBaudRate);
                Int32 iDateBits = Convert.ToInt32(strDateBits);

                sp.BaudRate = iBaudRate;       //波特率
                sp.DataBits = iDateBits;       //数据位
                switch (strStopBits)            //停止位
                {
                    case "1":
                        sp.StopBits = StopBits.One;
                        break;
                    case "1.5":
                        sp.StopBits = StopBits.OnePointFive;
                        break;
                    case "2":
                        sp.StopBits = StopBits.Two;
                        break;
                    default:
                        break;
                }
                switch (config.Parity)             //校验位
                {
                    case "无":
                        sp.Parity = Parity.None;
                        break;
                    case "奇校验":
                        sp.Parity = Parity.Odd;
                        break;
                    case "偶校验":
                        sp.Parity = Parity.Even;
                        break;
                    default:
                        break;
                }

                if (sp.IsOpen)//如果打开状态，则先关闭一下
                {
                    sp.Close();
                }

                sp.Open();     //打开串口

                this.serialPorts.Add(sp);

                ShowMessage("INFO：打开串口成功！");

                return true;
            }
            catch (System.Exception ex)
            {
                ShowMessage("ERROR：" + ex.Message);
                return false;
            }
        }

        private void parseMessage()
        {
            while (true)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine("线程被终止！");
                    break;
                }
                // 数据少于5个时，sleep3秒
                if (receivedBytes.All(i => i.Value.Count < 5))
                {
                    // Log.WriteLog("INFO：没有接收到数据，解析线程休眠3秒。");
                    Thread.Sleep(3000);
                }
                else
                {
                    try
                    {
                        parse();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLog(string.Format("ERROR：线程处理错误：{0},{1},{2}", ex.Message, ex.Source, ex.StackTrace));
                    }
                }
            }
        }

        private void parse()
        {
            // 应答数据格式--0x02 0xa0 0x01 0x05 0x0d
            // 沙箱信息请求--0x02 0xa0 0x00 0x1a 0x85 0x07 0x0d
            // 加沙数据格式--0x02 0xa1 0x1a 0x85 0x01 0x02 0x03 0x04 0x05 0x06 0x07 0x08 0x0e 0x0d
            bool flag = false;
            Log.WriteLog("INFO：数据满足5条，开始解析...");
            lock (objlck)
            {
                var spList = receivedBytes.Keys.ToList();
                foreach (var key in spList)
                {
                    Log.WriteLog(string.Format("INFO：{0}的数据内容：{1}", key.PortName, string.Join(" ", receivedBytes[key].Select(d => d.ToString("x2")))));
                    // 从第二个位置开始
                    for (var i = 1; i < receivedBytes[key].Count; i++)
                    {
                        if (receivedBytes[key][i].ToString("x2").ToLower() == "a0") //疑似应答数据
                        {
                            // 继续判断第i+2和i+3位
                            if (i + 3 < receivedBytes[key].Count &&
                                receivedBytes[key][i + 2].ToString("x2").ToLower() == "05" &&
                                receivedBytes[key][i + 3].ToString("x2").ToLower() == "0d")
                            {
                                // 是返回的应答数据
                                Log.WriteLog(string.Format("INFO：分析到应答数据！数据内容：{0}", string.Join(" ", receivedBytes[key].Skip(i - 1).Take(5).Select(d => d.ToString("x2")))));
                                // 取第三个字节确认是否成功
                                if (receivedBytes[key][i + 1].ToString("x2") == "01")//成功
                                {
                                    Log.WriteLog(string.Format("INFO：发送识别车号完毕！信息：{0}", currentMessage));
                                }
                                else
                                {
                                    Log.WriteLog(string.Format("ERROR：发送识别车号失败！信息：{0}", currentMessage));
                                }
                                if (curTrainType != null && curTrainNo.Length == 4 && receivedBytes[key][i - 1] == curTrainType.Port[0] && curTrainType.Port.Length > 1)//第二次发送，发送第二个地址
                                {
                                    var message = "";
                                    if (VERSION == "1.0")
                                    {
                                        message = string.Format("0x{0},0xa0,0x{1},0x{2},0x{3},0x01,0x08,0x0d", curTrainType.Port[1].ToString("x2"), curTrainType.AddressNum.ToString("x2"), curTrainNo.Substring(0, 2), curTrainNo.Substring(2, 2));
                                    }
                                    else if (VERSION == "2.1")
                                    {
                                        message = this.GetTrainMessage(this.curTrainType, curTrainType.Port[1].ToString("x2"), curTrainNo);
                                    }
                                    Log.WriteLog(string.Format("INFO：开始发送第二次识别信息！信息：{0}", message));
                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        this.SendMessage(message);
                                    }));
                                }
                                else
                                {
                                    currentMessage = "";
                                }
                                // 跳过已解析到的内容
                                receivedBytes[key] = receivedBytes[key].Skip(i + 3 + 1).ToList();
                                flag = true;
                                break;
                            }
                            // 判断是否请求沙箱数据，版本2.1
                            if (VERSION == "2.1" && i + 5 < receivedBytes[key].Count &&
                                receivedBytes[key][i + 4].ToString("x2").ToLower() == "07" &&
                                receivedBytes[key][i + 5].ToString("x2").ToLower() == "0d")
                            {
                                // 是请求沙箱数据
                                Log.WriteLog(string.Format("INFO：分析到请求沙箱配置数据！数据内容：{0}", string.Join(" ", receivedBytes[key].Skip(i - 1).Take(7).Select(d => d.ToString("x2")))));
                                // 上沙车地址
                                var port = receivedBytes[key][i - 1].ToString("x2").ToLower();
                                // 车号
                                var trainNo = receivedBytes[key][i + 2].ToString("x2") + receivedBytes[key][i + 3].ToString("x2");
                                // 信息
                                var message = this.GetTrainMessage(this.curTrainType, port, trainNo);
                                Log.WriteLog(string.Format("INFO：开始发送沙箱配置数据！信息：{0}", message));
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    this.SendMessage(message);
                                }));
                            }
                        }
                        if (receivedBytes[key][i].ToString("x2").ToLower() == "a1") //疑似加沙数据
                        {
                            if (VERSION == "1.0")
                            {
                                // 继续判断第i+11和i+12位
                                if (i + 12 < receivedBytes[key].Count &&
                                    receivedBytes[key][i + 11].ToString("x2").ToLower() == "0e" &&
                                    receivedBytes[key][i + 12].ToString("x2").ToLower() == "0d")
                                {
                                    // 是返回的加沙数据
                                    Log.WriteLog(string.Format("INFO：分析到加沙数据！数据内容：{0}", string.Join(" ", receivedBytes[key].Skip(i - 1).Take(14).Select(d => d.ToString("x2")))));

                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        FileInfo fi = new FileInfo(dataFile);//新的一天
                                        if (!fi.Exists)
                                        {
                                            if (dataTable == null)
                                            {
                                                Log.WriteLog("ERROR：DataTable为空！");
                                            }
                                            dataTable.Rows.Clear();
                                        }

                                        DataRow dr = dataTable.NewRow();
                                        if (dr == null)
                                        {
                                            Log.WriteLog("ERROR：DataRow为空！");
                                        }
                                        List<string> datas = new List<string>();
                                        datas.Add(DateTime.Now.ToString("yyyy-MM-dd"));//日期
                                        datas.Add(DateTime.Now.ToString("HH:mm:ss"));//时间
                                        var addressNum = HexToDecimal(receivedBytes[key][i - 1].ToString("x2"));
                                        datas.Add(addressNum);//上沙车地址
                                        datas.Add(curTrainType != null ? curTrainType.TrainType : "未取到");
                                        datas.Add(HexToDecimal(receivedBytes[key][i + 1].ToString("x2") + receivedBytes[key][i + 2].ToString("x2")).PadLeft(4, '0'));//机车车号

                                        int total = 0;
                                        for (int j = i + 3; j < i + 11; j++)
                                        {
                                            var str = HexToDecimal(receivedBytes[key][j].ToString("x2"));
                                            datas.Add(str);//砂箱1-8加沙量
                                            total += int.Parse(str);
                                        }
                                        datas.Add(total.ToString());//加沙总量
                                                                    //添加到datatable
                                        for (int j = 0; j < datas.Count; j++)
                                        {
                                            dr[j] = datas[j];
                                        }
                                        dataTable.Rows.Add(dr);

                                        //保存
                                        CSVFileHelper.SaveCSV(dataTable, dataFile);
                                        Log.WriteLog(string.Format("INFO：接收到从站PLC加沙数据！信息：上沙车地址-{0}|机车车型-{1}|机车车号-{2}|加沙总量-{3}", datas[2], datas[3], datas[4], total));

                                        //发送应答信息
                                        var ackMessage = string.Format("0x{0},0xa1,0x01,0x05,0x0d", receivedBytes[key][i - 1].ToString("x2"));

                                        this.SendMessage(ackMessage, false);
                                    }));
                                    // 跳过已解析到的内容
                                    receivedBytes[key] = receivedBytes[key].Skip(i + 12 + 1).ToList();
                                    flag = true;
                                    break;
                                }
                            }
                            else if (VERSION == "2.1")
                            {
                                // 继续判断第i+60和i+61位
                                if (i + 61 < receivedBytes[key].Count &&
                                    receivedBytes[key][i + 60].ToString("x2").ToLower() == "3f" &&
                                    receivedBytes[key][i + 61].ToString("x2").ToLower() == "0d")
                                {
                                    // 是返回的加沙数据
                                    Log.WriteLog(string.Format("INFO：分析到加沙数据！数据内容：{0}", string.Join(" ", receivedBytes[key].Skip(i - 1).Take(63).Select(d => d.ToString("x2")))));

                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        FileInfo fi = new FileInfo(dataFile);//新的一天
                                        if (!fi.Exists)
                                        {
                                            if (dataTable == null)
                                            {
                                                Log.WriteLog("ERROR：DataTable为空！");
                                            }
                                            dataTable.Rows.Clear();
                                        }

                                        DataRow dr = dataTable.NewRow();
                                        if (dr == null)
                                        {
                                            Log.WriteLog("ERROR：DataRow为空！");
                                        }
                                        List<string> datas = new List<string>();
                                        datas.Add(DateTime.Now.ToString("yyyy-MM-dd"));//日期
                                        datas.Add(DateTime.Now.ToString("HH:mm:ss"));//时间
                                        var addressNum = HexToDecimal(receivedBytes[key][i - 1].ToString("x2"));
                                        datas.Add(addressNum);//上沙车地址
                                        // 车型
                                        var tempNum = int.Parse(HexToDecimal(receivedBytes[key][i + 1].ToString("x2")));

                                        var type = trainTypeConfigList.Where(item => item.Port.Contains(tempNum)).FirstOrDefault();
                                        var trainType = type != null ? type.TrainType : "未匹配车型";
                                        datas.Add(trainType);
                                        var trainNo = HexToDecimal(receivedBytes[key][i + 2].ToString("x2") + receivedBytes[key][i + 3].ToString("x2")).PadLeft(4, '0');
                                        datas.Add(trainNo);//机车车号

                                        int total = 0;
                                        for (int j = i + 52; j < i + 60; j++)
                                        {
                                            var str = HexToDecimal(receivedBytes[key][j].ToString("x2"));
                                            datas.Add(str);//砂箱1-8加沙量
                                            total += int.Parse(str);
                                        }
                                        datas.Add(total.ToString());//加沙总量
                                                                    //添加到datatable
                                        for (int j = 0; j < datas.Count; j++)
                                        {
                                            dr[j] = datas[j];
                                        }
                                        dataTable.Rows.Add(dr);

                                        //保存
                                        CSVFileHelper.SaveCSV(dataTable, dataFile);
                                        Log.WriteLog(string.Format("INFO：接收到从站PLC加沙数据！信息：上沙车地址-{0}|机车车型-{1}|机车车号-{2}|加沙总量-{3}", datas[2], datas[3], datas[4], total));

                                        // 更新沙箱位置信息
                                        List<string> boxInfos = new List<string>();
                                        for (int j = i + 4; j < i + 52; j+=2)
                                        {
                                            var str = HexToDecimal(receivedBytes[key][j].ToString("x2")) + HexToDecimal(receivedBytes[key][j+1].ToString("x2"));
                                            boxInfos.Add(str);
                                        }
                                        this.UpdateBoxInfo(trainType, trainNo, boxInfos);

                                        //发送应答信息
                                        var ackMessage = string.Format("0x{0},0xa1,0x01,0x05,0x0d", receivedBytes[key][i - 1].ToString("x2"));

                                        this.SendMessage(ackMessage, false);
                                    }));
                                    // 跳过已解析到的内容
                                    receivedBytes[key] = receivedBytes[key].Skip(i + 12 + 1).ToList();
                                    flag = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            // 没有合适的数据
            if (!flag)
            {
                Log.WriteLog("INFO：没有合适的数据，解析线程休眠3秒。");
                Thread.Sleep(3000);
            }

        }

        private Dictionary<SerialPort, List<Byte>> receivedBytes = new Dictionary<SerialPort, List<Byte>>();
        private int dataLen = 0;
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = sender as SerialPort;
            if (sp != null && sp.IsOpen)
            {
                lock (objlck)
                {
                    if (!receivedBytes.ContainsKey(sp))
                    {
                        receivedBytes[sp] = new List<byte>();
                    }
                    Byte[] receivedData = new Byte[sp.BytesToRead];        //创建接收字节数组
                    sp.Read(receivedData, 0, receivedData.Length);         //读取数据

                    string strRcv = "";
                    for (int i = 0; i < receivedData.Length; i++)
                    {
                        if (receivedData[i] == Byte.MaxValue) // 去掉255字符
                        {
                            continue;
                        }
                        receivedBytes[sp].Add(receivedData[i]);
                        strRcv += receivedData[i].ToString() + " ";
                    }
                    Log.WriteLog(string.Format("INFO：收到信息！信息：{0}", strRcv));
                }
                return;
                // 以下内容暂时去掉
                //try
                //{
                //    Byte[] receivedData = new Byte[sp.BytesToRead];        //创建接收字节数组
                //    sp.Read(receivedData, 0, receivedData.Length);         //读取数据

                //    string strRcv = "";
                //    for (int i = 0; i < receivedData.Length; i++)
                //    {
                //        if (receivedData[i] == Byte.MaxValue) // 去掉255字符
                //        {
                //            continue;
                //        }
                //        receivedBytes[sp].Add(receivedData[i]);
                //        strRcv += receivedData[i].ToString() + " ";
                //    }
                //    Log.WriteLog(string.Format("INFO：收到信息！信息：{0}", strRcv));

                //    if (dataLen == 0 && receivedBytes[sp].Count > 1)//取第二位判断
                //    {
                //        dataLen = receivedBytes[sp][1].ToString("x2").ToLower() == "a0" ? 5 : 14;
                //    }

                //    if (receivedBytes[sp].Count >= dataLen)//接收完毕处理
                //    {
                //        Log.WriteLog(string.Format("INFO：开始解析信息！信息内容：{0}", string.Join(" ", receivedBytes[sp])));
                //        if (dataLen == 5)
                //        { //从站PLC车号识别应答

                //            //取第三个字节确认是否成功
                //            if (receivedBytes[sp][2].ToString("x2") == "01")//成功
                //            {
                //                Log.WriteLog(string.Format("INFO：发送识别车号完毕！信息：{0}", currentMessage));
                //            }
                //            if (receivedBytes[sp][0] == curTrainType.Port[0] && curTrainType.Port.Length > 1)//第二次发送，发送第二个地址
                //            {
                //                var message = string.Format("0x{0},0xa0,0x{1},0x{2},0x{3},0x01,0x08,0x0d", curTrainType.Port[1].ToString("x2"), curTrainType.AddressNum.ToString("x2"), curTrainNo.Substring(0, 2), curTrainNo.Substring(2, 2));

                //                this.SendMessage(message);
                //            }
                //            else
                //            {
                //                currentMessage = "";
                //            }
                //        }
                //        else if (dataLen == 14)//收到从站PLC加沙数据
                //        {
                //            FileInfo fi = new FileInfo(dataFile);//新的一天
                //            if (!fi.Exists)
                //            {
                //                dataTable.Rows.Clear();
                //            }

                //            DataRow dr = dataTable.NewRow();
                //            List<string> datas = new List<string>();
                //            datas.Add(DateTime.Now.ToString("yyyy-MM-dd"));//日期
                //            datas.Add(DateTime.Now.ToString("HH:mm:ss"));//时间
                //            var addressNum = HexToDecimal(receivedBytes[sp][0].ToString("x2"));
                //            datas.Add(addressNum);//上沙车地址
                //            datas.Add(curTrainType.TrainType);
                //            datas.Add(HexToDecimal(receivedBytes[sp][2].ToString("x2")) + HexToDecimal(receivedBytes[sp][3].ToString("x2")));//机车车号

                //            int total = 0;
                //            for (int i = 4; i < 12; i++)
                //            {
                //                var str = HexToDecimal(receivedBytes[sp][i].ToString("x2"));
                //                datas.Add(str);//砂箱1-8加沙量
                //                total += int.Parse(str);
                //            }
                //            datas.Add(total.ToString());//加沙总量
                //                                        //添加到datatable
                //            for (int j = 0; j < datas.Count; j++)
                //            {
                //                dr[j] = datas[j];
                //            }
                //            dataTable.Rows.Add(dr);

                //            //保存
                //            CSVFileHelper.SaveCSV(dataTable, dataFile);
                //            Log.WriteLog(string.Format("INFO：接收到从站PLC加沙数据！信息：上沙车地址-{0}|机车车号-{1}|加沙总量-{2}", datas[2], datas[3], total));

                //            //发送应答信息
                //            var ackMessage = string.Format("0x{0},0xa1,0x01,0x05,0x0d", receivedBytes[sp][0].ToString("x2"));
                //            if (!SendMessage(ackMessage, false))//发送不成功时，加入重发队列
                //            {

                //            }
                //        }
                //        // 跳过读取到内容
                //        receivedBytes[sp] = receivedBytes[sp].Skip(dataLen).ToList();
                //        dataLen = 0;
                //    }
                //}
                //catch (System.Exception ex)
                //{
                //    Log.WriteLog("ERROR：" + ex.Message);
                //    Log.WriteLog("ERROE：跳过读取内容-" + string.Join(" ", receivedBytes[sp].Take(dataLen)));
                //    // 跳过读取到内容
                //    receivedBytes[sp] = receivedBytes[sp].Skip(dataLen).ToList();
                //    dataLen = 0;
                //}
                //finally
                //{

                //}
            }
        }

        private string currentMessage = "";
        private bool SendMessage(string message, bool isNeedAck = true)
        {
            Log.WriteLog("INFO：判断串口是否打开！");
            if (serialPorts.Count == 0 || !serialPorts[0].IsOpen) //如果没打开
            {
                Log.WriteLog("ERROR：串口未打开！");
                return false;
            }
            Log.WriteLog("INFO：开始格式化发送内容...");
            string strSend = message.Trim().Replace(',', ' ').Replace("0x", "");
            currentMessage = message;
            //启动检查
            //处理数字转换
            string[] strArray = strSend.Split(' ');

            int byteBufferLength = strArray.Length;
            for (int i = 0; i < strArray.Length; i++)
            {
                if (strArray[i] == "")
                {
                    byteBufferLength--;
                }
            }
            // int temp = 0;
            byte[] byteBuffer = new byte[byteBufferLength];
            int ii = 0;
            for (int i = 0; i < strArray.Length; i++)//对获取的字符做相加运算
            {

                Byte[] bytesOfStr = Encoding.Default.GetBytes(strArray[i]);

                int decNum = 0;
                if (strArray[i] == "")
                {
                    continue;
                }
                else
                {
                    decNum = Convert.ToInt32(strArray[i], 16); //atrArray[i] == 12时，temp == 18 
                }

                try    //防止输错，使其只能输入一个字节的字符
                {
                    byteBuffer[ii] = Convert.ToByte(decNum);
                }
                catch (System.Exception ex)
                {
                    Log.WriteLog("ERROR：发送失败，错误信息：" + ex.Message);
                    return false;
                }

                ii++;
            }

            Log.WriteLog("INFO：格式化发送内容完毕，开始写入串口！");
            try
            {
                foreach (var sp in serialPorts)
                {
                    sp.Write(byteBuffer, 0, byteBuffer.Length);
                }
            }
            catch (System.Exception ex)
            {
                Log.WriteLog("ERROR：发送失败，错误信息：" + ex.Message);
                return false;
            }
            Log.WriteLog("INFO：写入内容完毕！");
            return true;
        }


        private void ShowMessage(string str)
        {
            tbinfo.Text = str;
            Log.WriteLog(str);
        }

        private void tbOpenDataFolder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(Param.APPFILEPATH + @"\data\");
        }
    }
}
