using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfInfoPusher.Helpers;
using WpfInfoPusher.Models;

namespace WpfInfoPusher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //任务列表
        private ObservableCollection<FtpTask> taskList = new ObservableCollection<FtpTask>();
        //扫描定时器
        private DispatcherTimer timer = new DispatcherTimer();
        //消息分析发送udp客户端
        //private TcpClient tcpClient;

        public MainWindow()
        {
            InitializeComponent();
            
            //tcpClient = new TcpClient();

            Param.Init();

            ReadConfig();
            ReadSerialNo();

            this.dataGrid.ItemsSource = taskList;
            this.lbLog.ItemsSource = Log.LogList;

            this.Closing += MainWindow_Closing;

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var item in taskList)
            {
                Run(item);
            }
        }

        Random rand = new Random();
        char[] splitStr = new char[] {',','，' }; 
        private void Run(FtpTask ftpTask)
        {
            if (string.IsNullOrEmpty(ftpTask.FilePath) || string.IsNullOrEmpty(ftpTask.Host))
            {
                return;
            }

            try
            {
                var folder = new DirectoryInfo(ftpTask.FilePath);
                var files = (folder.GetFiles().OrderByDescending(i => i.LastWriteTime)).ToList();
                //发现新文件
                if (files.Count > 0 && files.First().LastWriteTime > ftpTask.LastestTime)
                {
                    var file = files.First();
                    ftpTask.LastestTime = files.First().LastWriteTime;
                    Log.WriteLog(string.Format("任务编号{0}：发现新文件{1}({2})。", ftpTask.TaskId, file.Name, file.LastWriteTime.ToString()));
                    var fileStream = file.OpenRead();
                    var streamReader = new StreamReader(fileStream, Encoding.Default);
                    var content = streamReader.ReadToEnd().Trim();
                    fileStream.Close();
                    fileStream.Dispose();
                    streamReader.Close();
                    streamReader.Dispose();

                    if (string.IsNullOrEmpty(content))
                    {
                        Log.WriteLog(string.Format("任务编号{0}：{1}的内容为空，跳过此文件。", ftpTask.TaskId, file.Name));
                    }
                    else
                    {
                        Log.WriteLog(string.Format("任务编号{0}：{1}的内容为【{2}】。", ftpTask.TaskId, file.Name, content));
                        var tempContents = content.Split(splitStr);
                        if (tempContents.Length < 3 || tempContents[0].Length < 5)
                        {
                            Log.WriteLog(string.Format("任务编号{0}：{1}的内容格式不正确。正确格式如：HXD3C0014,2017/07/21 15:35:15,入库。"));
                            return;
                        }
                        var trainType = tempContents[0].Substring(0, tempContents[0].Length - 4);
                        var trainNo = tempContents[0].Substring(tempContents[0].Length - 4);
                        var times = tempContents[1].Split(' ');
                        var t = DateTime.Now;
                        var trainTimeShort = times.Length > 1 ? tempContents[1].Split(' ')[1] : file.LastWriteTime.ToString("HH:mm:ss");
                        var trainTimeLong = DateTime.TryParse(tempContents[1], out t) ? t.ToString("yyyyMMddHHmmss") : file.LastWriteTime.ToString("yyyyMMddHHmmss");
                        var trainDest = tempContents[2] == "入库" ? 1 : (tempContents[2] == "出库" ? 0 : 2);//入库-下行，出库-上行
                        //Log.WriteLog(string.Format("任务编号{0}：{1}的内容为{2}。", ftpTask.TaskId, file.Name, content), @"\temps\src\");
                        var data = "";
                        if (ftpTask.TaskType == 0)//led显示
                        {
                            data = string.Format(Param.FORMAT, trainType + " " + trainNo, trainTimeShort);

                            Log.WriteLog(string.Format("任务编号{0}：采集到信息-\r\n{1}", ftpTask.TaskId, data));
                            //Log.WriteLog(string.Format("任务编号{0}：采集到信息-\r\n{1}", ftpTask.TaskId, data), @"\temps\dest\");

                            var tempPath = string.Format(@"{0}\datas\{1}\", Param.APPFILEPATH, ftpTask.TaskId);
                            if (!Directory.Exists(tempPath))
                            {
                                DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
                                directoryInfo.Create();
                            }
                            var tempFile = new FileStream(tempPath + ftpTask.FtpFile, FileMode.Create);
                            byte[] buf = Encoding.Default.GetBytes(data);
                            tempFile.Write(buf, 0, buf.Length);
                            tempFile.Flush();
                            tempFile.Close();
                            tempFile.Dispose();

                            var ftp = new FTPHelper(ftpTask.Host, ftpTask.TargetPath, ftpTask.FtpUser, ftpTask.FtpPassword);
                            if (ftp.Upload(tempPath + ftpTask.FtpFile))
                            {
                                Log.WriteLog(string.Format("任务编号{0}：ftp上传成功。", ftpTask.TaskId));
                            }
                            else//失败
                            {
                                Log.WriteLog(string.Format("任务编号{0}：ftp上传失败！5秒后重试...", ftpTask.TaskId));
                                DispatcherTimer tempTimer = new DispatcherTimer();
                                EventHandler handler = null;
                                tempTimer.Tick += handler = (s, a) =>
                                {
                                    tempTimer.Tick -= handler;
                                    tempTimer.Stop();
                                    Log.WriteLog(string.Format("任务编号{0}：开始重新上传...", ftpTask.TaskId));
                                    var tempftp = (tempTimer.Tag as object[])[0] as FTPHelper;
                                    var tempfile = (tempTimer.Tag as object[])[1].ToString();
                                    if (tempftp.Upload(tempfile))
                                    {
                                        Log.WriteLog(string.Format("任务编号{0}：ftp重新上传成功。", ftpTask.TaskId));
                                    }
                                    else
                                    {
                                        Log.WriteLog(string.Format("任务编号{0}：ftp重新上传失败，放弃该条记录。", ftpTask.TaskId));
                                    }
                                };
                                tempTimer.Tag = new object[] { ftp, tempPath + ftpTask.FtpFile };
                                tempTimer.Start();
                            }
                        }
                        else//报文解析
                        {
                            var spliters = new char[] { ' ', ' ' };
                            var temps = content.Split(spliters);
                            data = "{" + 
                                string.Format(Param.MSGFORMAT,      //格式：：报文类型@设备编号@车来报文识别号@到达时间@列车去向@辆数_车型_车号_配属段
                                serialNo,
                                trainTimeLong,    //到达时间
                                trainDest, //(ftpTask.FilePath.EndsWith("\\0") ? 0 : (ftpTask.FilePath.EndsWith("\\1") ? 1 : 2)),  //去向：：
                                trainType,  //车型
                                trainNo)  //车号
                                + "}";

                            UpdateSerialNo();
                            serialNo++;
                            //随机文件名
                            //ftpTask.FtpFile = string.Format("{0}_{1}_{2}_{3}.txt", ftpTask.TaskId, DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("HHmmss"), rand.Next(1, 1000));

                            Log.WriteLog(string.Format("任务编号{0}：采集到信息-\r\n{1}", ftpTask.TaskId, data));
                            //Log.WriteLog(string.Format("任务编号{0}：采集到信息-\r\n{1}", ftpTask.TaskId, data), @"\temps\dest\");

                            //将该文本转化为字节数组
                            byte[] b = System.Text.Encoding.UTF8.GetBytes(data);
                            //向配置的服务器端口发送数据
                            try
                            {
                                var tcpClient = new TcpClient();
                                tcpClient.Connect(new IPEndPoint(IPAddress.Parse(ftpTask.Host), int.Parse(ftpTask.Port)));

                                NetworkStream clientStream = tcpClient.GetStream();

                                clientStream.Write(b, 0, b.Length);
                                clientStream.Flush();

                                byte[] obuffer = new byte[1024];
                                var len = clientStream.Read(obuffer, 0, 1024);

                                var result = System.Text.Encoding.UTF8.GetString(obuffer, 0, len);
                                Log.WriteLog(string.Format("任务编号{0}：收到{1}:{2}的返回【{3}】。", ftpTask.TaskId, ftpTask.Host, ftpTask.Port, result));
                                if (result == "success")
                                {
                                    Log.WriteLog(string.Format("任务编号{0}：向{1}:{2}发送消息成功。", ftpTask.TaskId, ftpTask.Host, ftpTask.Port));
                                }
                                else
                                {
                                    Log.WriteLog(string.Format("任务编号{0}：向{1}:{2}发送消息失败！", ftpTask.TaskId, ftpTask.Host, ftpTask.Port));
                                }
                                tcpClient.Close();
                            }
                            catch(Exception e)
                            {
                                Log.WriteLog(string.Format("任务编号{0}：向{1}:{2}发送消息失败！异常信息：{3}。", ftpTask.TaskId, ftpTask.Host, ftpTask.Port, e.Message));
                            }
                        }
                    }
                }
                else
                {
                    //Log.WriteLog(string.Format("任务编号{0}：无新文件。", ftpTask.TaskId));
                }
            }
            catch (Exception e)
            {
                Log.WriteLog("ERROR：" + e.Message);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (taskList.Count > 0 && taskList.Where(i => i.TaskState == "正在运行").Count() > 0)
            {
                MessageBoxResult result = MessageBox.Show("任务正在运行，确定退出吗？退出后，任务会终止。", "确认？", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                //关闭窗口
                if (result == MessageBoxResult.OK)
                {
                    UpdateSerialNo();
                    timer.Stop();
                    e.Cancel = false;
                }
                //不关闭窗口
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as MenuItem).Tag.ToString();
            switch (tag)
            {
                case "About":
                    (new About()).ShowDialog();
                    break;
                case "Create":
                    (new TaskCreate(this)).ShowDialog();
                    break;
                case "Start":
                    menuStart.Visibility = Visibility.Collapsed;
                    menuStop.Visibility = Visibility.Visible;
                    StartTask();
                    break;
                case "Stop":
                    menuStart.Visibility = Visibility.Visible;
                    menuStop.Visibility = Visibility.Collapsed;
                    StopTask();
                    break;
                case "Main":
                    dataGrid.Visibility = Visibility.Visible;
                    lbLog.Visibility = Visibility.Collapsed;
                    tbTitle.Text = "当前任务列表";
                    break;
                case "Log":
                    dataGrid.Visibility = Visibility.Collapsed;
                    lbLog.Visibility = Visibility.Visible;
                    tbTitle.Text = "最新日志";
                    break;
            }
        }

        private void StartTask()
        {
            foreach (var item in taskList)
            {
                item.TaskState = "正在运行";
            }
            Log.WriteLog("INFO：任务启动，开始扫描文件...");
            timer.Start();
        }

        private void StopTask()
        {
            foreach (var item in taskList)
            {
                item.TaskState = "未开始";
            }
            Log.WriteLog("INFO：任务停止。");
            timer.Stop();
        }

        internal void AddNewFtpTask(int type, string filePath, string host, string user, string password, string targetPath, string ftpFile)
        {
            var id = taskList.Count > 0 ? taskList.Select(i => i.TaskId).Max() + 1 : 1;
            var task = new FtpTask()
            {
                TaskType = type,
                TaskId = id,
                FilePath = filePath,
                Host = host,
                FtpUser = user,
                FtpPassword = password,
                TargetPath = targetPath,
                FtpFile = ftpFile
            };

            taskList.Add(task);

            UpdateConfig();
        }

        internal void AddNewUdpTask(int type, string filePath, string host, string port)
        {
            var id = taskList.Count > 0 ? taskList.Select(i => i.TaskId).Max() + 1 : 1;
            var task = new FtpTask()
            {
                TaskType = type,
                TaskId = id,
                FilePath = filePath,
                Host = host,
                Port = port
            };

            taskList.Add(task);

            UpdateConfig();
        }


        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("确定删除该任务吗？", "确认？", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                taskList.Remove((sender as Image).Tag as FtpTask);
            }

            UpdateConfig();
        }

        private void UpdateConfig()
        {
            var str = SerializeHelper.ScriptSerializeToXML<ObservableCollection<FtpTask>>(taskList);
            File.WriteAllText(Param.APPFILEPATH + "/config.xml", str, Encoding.UTF8);
        }

        private void ReadConfig()
        {
            try
            {
                var str = File.ReadAllText(Param.APPFILEPATH + "/config.xml");
                var temps = SerializeHelper.JSONXMLToObject<List<FtpTask>>(str);
                if (temps != null)
                {
                    foreach (var item in temps)
                    {
                        taskList.Add(item);
                    }
                }
                Log.WriteLog("INFO：读取任务配置完毕。");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private long serialNo = 1;
        private void UpdateSerialNo()
        {
            File.WriteAllText(Param.APPFILEPATH + "/serialno.txt", serialNo.ToString(), Encoding.UTF8);
        }

        private void ReadSerialNo()
        {
            try
            {
                var str = File.ReadAllText(Param.APPFILEPATH + "/serialno.txt");
                if(!long.TryParse(str.Trim(), out serialNo))
                {
                    serialNo = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
