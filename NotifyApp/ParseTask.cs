using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace NotifyApp
{
    class ParseTask
    {
        string[] reps = new string[] { "{", "｛", "}", "｝" };
        //char spliter = '@';

        private DateTime lastTime = DateTime.Now.AddMinutes(-5);//.AddSeconds(-20);
        private Dictionary<CHostConfig, DateTime> cLastTime = new Dictionary<CHostConfig, DateTime>();
        private Dictionary<CHostConfig, string> cLastData = new Dictionary<CHostConfig, string>();

        private List<string> lastFileList = new List<string>();

        private string curTrain = "";

        FTPHelper msgFileFtp = null;

        public void Do()
        {
            this.msgFileFtp = FTPFactory.CreateMsgFtp();
            //var test = "D0201010000H0000001H    43132*****201511130934420020151113093442002015111309350000001100J16155450224AH    43132";
            //var time = test.Substring(66, 14);
            //CreateMessageFile("1", "1234", "222", "20181213194533");
            //CreateMessageFile("0", "1234", "222", "20180128194533");
            // 传感器
            Thread thread = new Thread(RunThread);
            thread.Start();
            // 摄像头
            if (Param.CHostList.Count > 0)
            {
                for (var i = 0; i < Param.CHostList.Count; i++)
                {
                    var host = Param.CHostList[i];
                    if (host.Host == null || host.Host == "")
                    {
                        continue;
                    }
                    cLastTime.Add(host, DateTime.Now.AddMinutes(-1));
                    Thread threadc = new Thread(new ParameterizedThreadStart(RunThreadC));
                    threadc.Start(host);
                }
            }
        }

        private void RunThread()
        {
            FTPHelper helper = new FTPHelper(Param.SRC_HOST, Param.SRC_PATH, Param.SRC_USR, Param.SRC_PWD);

            Thread.Sleep(500);

            while (true)
            {
                //lock (Param.LOCK)
                //{

                if (Param.IsClosing)
                    break;

                // 传感器文件
                RunSensorFile(helper);

                Console.WriteLine("传感器");

                while (lastFileList.Count > 100)//只保留最新的100个文件
                    lastFileList.RemoveAt(0);
                // }
                Thread.Sleep(TimeSpan.FromSeconds(Param.INTERVAL));

            }
        }

        private void RunThreadC(object obj)
        {
            Thread.Sleep(500);
            CHostConfig host = obj as CHostConfig;
            FTPHelper helper_c = new FTPHelper(host.Host, host.Path, host.User, host.Password);

            while (true)
            {
                //lock (Param.LOCK)
                //{

                if (Param.IsClosing)
                    break;

                // 摄像头文件
                RunCameraFile(helper_c as FTPHelper, host);

                Console.WriteLine(host.Key);
                // }
                Thread.Sleep(TimeSpan.FromSeconds(Param.INTERVAL));

            }
        }
        private void RunCameraFile(FTPHelper helper, CHostConfig host)
        {
            Log.WriteLine("开始执行任务(摄像头-" + host.Key + ")...");
            var tempArray = helper.GetFilesDetailList();
            if (tempArray == null) return;

            var fileList = tempArray.ToList();

            DateTime maxtime = DateTime.Now.AddMinutes(-5);//.AddSeconds(-20);
            bool flag = false;
            var filesToLoadList = new List<FileToLoad>();
            foreach (var item in fileList)
            {
                var temps = item.Substring(0, item.IndexOf("M") + 1).Split(' ');
                var dates = temps[0].Split('-');
                var fileTime = DateTime.Parse(string.Format("{0}-{1}-{2} {3}", dates[2], dates[0], dates[1], temps[2]));
                if (fileTime.AddSeconds(-30) <= cLastTime[host])  // 30秒之内的都判断为一个记录
                {
                    Log.WriteLine(string.Format("摄像头-重复车辆，暂时跳过"));
                    continue;
                }

                if (maxtime < fileTime) maxtime = fileTime;

                var fileName = Regex.Match(item, @"[\S]+ [\S]+", RegexOptions.IgnoreCase).Value.Split(' ')[1];

                filesToLoadList.Add(new FileToLoad() { FileName = fileName, DateTime = fileTime });

                flag = true;
            }

            var orderByTime = filesToLoadList.OrderBy(i => i.DateTime);

            foreach (var f in orderByTime)
            {
                var fileName = f.FileName;
                Log.WriteLine(string.Format("摄像头-" + host.Key + "-下载文件【{0}】", fileName));
                var downFlag = false;
                try
                {
                    //下载
                    downFlag = Download(fileName, host.Key + @"\", host);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(string.Format("摄像头-" + host.Key + "-下载文件出错【{0}】，错误：{1}", fileName, ex.Message));
                }
                Log.WriteLine(string.Format("摄像头-" + host.Key + "-解析文件【{0}】", fileName));
                //解析
                string[] inout = null;
                try
                {
                    inout = ParseCameraFile(@".\" + host.Key + @"\" + fileName, host);

                }
                catch (Exception ex)
                {
                    Log.WriteLine(string.Format("摄像头-" + host.Key + "-解析文件【{0}】，错误：{1}", fileName, ex.Message));
                }
                //上传
                if (!downFlag || inout == null)
                {
                    Log.WriteLine(string.Format("下载或解析错误，等待重新执行。"));
                    flag = false;
                }
                else if (inout[0] == "-1")
                {
                    Log.WriteLine(string.Format("【{0}】找不到匹配的推送规则。", fileName));
                }
                else if (inout[0] == "-2")
                {
                    Log.WriteLine(string.Format("【{0}】格式错误，无法解析。", fileName));
                }
                else
                {
                    lock (Param.LOCK)
                    {
                        if (curTrain != inout[1])  // 传感器未上传时
                        {
                            curTrain = inout[1];
                            Log.WriteLine(string.Format("摄像头-" + host.Key + "-开始上传文件【{0}】", fileName));
                            try
                            {
                                UploadCameraFile(fileName, inout[0], host);

                                // 创建消息报文文件
                                CreateMessageFile(inout[0], inout[1], inout[2], inout[3], inout[4]);
                            }
                            catch (Exception e)
                            {
                                Log.WriteLine(string.Format("摄像头-" + host.Key + "-上传文件，出现错误：{0}", e.Message));
                            }
                        }
                        else  // 文件已上传
                        {
                            Log.WriteLine(string.Format("文件已上传。"));
                            var srcFile = Application.StartupPath + @"\datas\" + host.Key + @"\" + fileName;
                            if (File.Exists(srcFile))
                            {
                                File.Delete(srcFile);
                            }
                            curTrain = "";
                        }
                    }
                }

                Thread.Sleep(300);
            }

            if (flag)
            {
                cLastTime[host] = maxtime;
            }
            else
            {
                Log.WriteLine("没有新文件...");
            }

            // CheckOutofDate();
        }

        private void RunSensorFile(FTPHelper helper)
        {
            //Log.Clean();

            Log.WriteLine("开始执行任务(传感器)...");
            var tempArray = helper.GetFilesDetailList();
            if (tempArray == null) return;

            var fileList = tempArray.ToList();

            DateTime maxtime = DateTime.Now.AddMinutes(-5);//.AddSeconds(-20);
            bool flag = false;
            var filesToLoadList = new List<FileToLoad>();
            foreach (var item in fileList)
            {
                var temps = item.Substring(0, item.IndexOf("M") + 1).Split(' ');
                var dates = temps[0].Split('-');
                var fileTime = DateTime.Parse(string.Format("{0}-{1}-{2} {3}", dates[2], dates[0], dates[1], temps[2]));
                if (fileTime <= lastTime)
                {
                    continue;
                }

                if (maxtime < fileTime) maxtime = fileTime;

                var fileName = Regex.Match(item, @"[\S]+ [\S]+", RegexOptions.IgnoreCase).Value.Split(' ')[1];

                if (lastFileList.Contains(item))
                {
                    continue;
                }

                filesToLoadList.Add(new FileToLoad() { FileName = fileName, DateTime = fileTime });

                lastFileList.Add(item);

                flag = true;
            }

            var orderByTime = filesToLoadList.OrderBy(i => i.DateTime);

            foreach (var f in orderByTime)
            {
                var fileName = f.FileName;
                Log.WriteLine(string.Format("传感器-下载文件【{0}】", fileName));
                //下载
                Download(fileName, "", null);
                Log.WriteLine(string.Format("传感器-解析文件【{0}】", fileName));
                //解析
                var inout = Parse(fileName);
                //上传
                if (inout[0] == "-1")
                {
                    Log.WriteLine(string.Format("【{0}】找不到匹配的推送规则。", fileName));
                }
                else if (inout[0] == "-2")
                {
                    Log.WriteLine(string.Format("【{0}】格式错误，无法解析。", fileName));
                }
                else
                {
                    lock (Param.LOCK)
                    {
                        if (curTrain != inout[1])  // 摄像头未上传时
                        {
                            curTrain = inout[1];
                            Log.WriteLine(string.Format("传感器-开始上传文件【{0}】", fileName));
                            try
                            {
                                Upload(fileName, inout[0]);

                                // 创建消息报文文件
                                CreateMessageFile(inout[0], inout[1], inout[2], inout[3], inout[4]);
                            }
                            catch (Exception e)
                            {
                                Log.WriteLine(string.Format("传感器-上传文件，出现错误：{0}", e.Message));
                            }
                        }
                        else  // 文件已上传
                        {
                            Log.WriteLine(string.Format("文件已上传。"));
                            var path = Application.StartupPath + @"\datas\";
                            if (File.Exists(path + fileName))
                            {
                                File.Delete(path + fileName);
                            }
                            curTrain = "";
                        }
                    }
                }

                Thread.Sleep(300);
            }

            if (flag)
            {
                lastTime = maxtime;
            }
            else
            {
                Log.WriteLine("没有新文件...");
            }

            CheckOutofDate();

        }

        int serialNo = 1;
        DateTime curDate = DateTime.Now.Date;
        private void CreateMessageFile(string inout, string trainNo, string trainType, string time, string devId)
        {
            // 第二日时重置serialNo
            if(curDate != DateTime.Now.Date)
            {
                curDate = DateTime.Now.Date;
                serialNo = 1;
            }

            // 验证目录
            var fileDir = Application.StartupPath + ".\\";
            if(Param.MSGFILE_DIR != "")
            {
                fileDir = Param.MSGFILE_DIR;
            }

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }
            // 文件名称
            var month = int.Parse(time.Substring(4, 2));
            var monthStr = month.ToString();
            if (month == 10)
            {
                monthStr = "A";
            }
            if (month == 11)
            {
                monthStr = "B";
            }
            if (month == 12)
            {
                monthStr = "C";
            }
            var dayStr = time.Substring(6, 2);
            var fileName = Param.MSGFILE_FORMAT.Replace("mmm", serialNo.ToString().PadLeft(3, '0')).Replace("MDD", monthStr + dayStr).Replace("A", devId);

            // 创建文件
            File.WriteAllText(fileDir + fileName, string.Format(Param.MESSAGE_FORMAT, trainType, trainNo, time, inout), Encoding.Default);

            // 上传到ftp
            if(this.msgFileFtp != null)
            {
                this.msgFileFtp.Upload(fileDir + fileName);
            }
        }

        private void CheckOutofDate()
        {
            for (int i = 0; i < Param.LastFileTime.Count; i++)
            {
                var dt = Param.LastFileTime[i];

                if (dt.AddMinutes(Param.IDLE_INTERVAL) < DateTime.Now)
                {
                    UploadDefault(i);
                    Param.LastFileTime[i] = DateTime.Now;
                    Log.WriteLine(string.Format("已5分钟没有新消息，更新{0}号牌为默认值。", i + 1));
                }
            }
        }

        private void UploadDefault(int fileNo)
        {
            var helper = FTPFactory.CreateDestFtp(fileNo);

            if (helper == null)
            {
                return;
            }
            File.WriteAllText("notify.txt", Param.DEFAULT, Encoding.Default);
            bool flag = helper.Upload(@".\notify.txt");

            if (!flag)
            {
                Thread.Sleep(500);
                helper.Upload(@".\notify.txt");

            }
            //上传完毕，删除本地
            File.Delete("notify.txt");
        }

        private void UpdateTime(int fileNo)
        {
            if (fileNo >= 0 && fileNo < Param.LastFileTime.Count)
            {
                Param.LastFileTime[fileNo] = DateTime.Now;
            }
        }

        private string[] Parse(string fileName)
        {
            string[] retFlag = new string[5];

            string data = "";
            using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
            {
                String line;
                while ((line = sr.ReadLine()) != null && !string.IsNullOrEmpty(line))
                {
                    data = line;
                    break;
                }

                sr.Close();
            }

            //解析完毕，删除
            File.Delete(fileName);

            //D0201010000H0000001H    43132*****201511130934420020151113093442002015111309350000001100J16155450224AH    43132

            if (data.Length < 105)
            {
                retFlag[0] = "-2";
                return retFlag;
            }

            //0-上行/出库，1-下行/入库
            var inout = data.Substring(18, 1);

            //时间
            var time = string.Format("{0}:{1}:{2}", data.Substring(74, 2), data.Substring(76, 2), data.Substring(78, 2));
            // 20151113093500
            var fullTime = data.Substring(66, 14);
            //车型
            var trainType = "";
            var trainNo = "";
            if (data[88] == 'J')//机车
            {
                trainType = data.Substring(89, 3).Trim();
                trainNo = data.Substring(92, 4).Trim();
            }
            else //T或Q
            {
                trainType = data.Substring(90, 5).Trim();
                trainNo = data.Substring(95, 7).Trim();
            }
            //文件分类
            var type = GetFileType(fileName, inout);

            if (type < 0)
            {
                retFlag[0] = "-1";
                return retFlag;
            }
            string result = string.Format(Param.FORMAT, GetLineType(type), GetInOrOut(inout), /*trainType,*/ trainNo, time);
            //var textdata = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.ASCII, System.Text.Encoding.UTF8.GetBytes(result.ToCharArray())));

            // File.WriteAllText(fileName, result, Encoding.Default);
            string path = Application.StartupPath + @"\datas\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(path + fileName, result, Encoding.Default);


            Log.WriteLine(string.Format("文件【{0}】,解析结果：{1}", fileName, result));

            //更新最新时间
            UpdateTime(type);

            retFlag[0] = inout;
            retFlag[1] = trainNo;
            retFlag[2] = trainType;
            retFlag[3] = fullTime;
            retFlag[4] = data[4].ToString();
            return retFlag;
        }

        private string[] ParseCameraFile(string fileName, CHostConfig host)
        {
            string[] retFlag = new string[5];
            string data = "";
            using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
            {
                String line;
                while ((line = sr.ReadLine()) != null && !string.IsNullOrEmpty(line))
                {
                    data = line;
                    break;
                }

                sr.Close();
            }

            //解析完毕，删除
            File.Delete(fileName);

            //HXD3,0798,2018/12/02 19:00:37,出库,1
            char[] spliter1 = new char[] { ',', '，' };
            var datasegs = data.Split(spliter1);
            if (datasegs.Length < 5)
            {
                retFlag[0] = "-2";
                return retFlag;
            }

            //0-上行/出库，1-下行/入库
            var inout = datasegs[3] == "出库" ? "0" : "1";

            //时间
            var time = datasegs[2].Split(' ')[1];
            //车型
            var trainType = datasegs[0];
            var trainNo = datasegs[1];

            //文件分类
            var type = -1;
            for (var i = 0; i < Param.HostList.Count; i++)
            {
                if (Param.HostList[i].Key == host.Dest)
                {
                    type = i;
                    break;
                }
            }

            if (type < 0)
            {
                retFlag[0] = "-1";
                return retFlag;
            }

            string result = string.Format(Param.FORMAT, GetLineType(type), GetInOrOut(inout), /*trainType,*/ trainNo, time);
            //var textdata = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.ASCII, System.Text.Encoding.UTF8.GetBytes(result.ToCharArray())));

            // File.WriteAllText(fileName, result, Encoding.Default);
            string path = Application.StartupPath + @"\datas\";
            if (!Directory.Exists(path + host.Key + @"\"))
            {
                Directory.CreateDirectory(path + host.Key + @"\");
            }
            File.WriteAllText(path + fileName, result, Encoding.Default);


            Log.WriteLine(string.Format("文件【{0}】,解析结果：{1}", fileName, result));

            //更新最新时间
            UpdateTime(type);

            retFlag[0] = inout;
            retFlag[1] = trainNo;
            retFlag[2] = Param.GetTrainTypeNo(trainType);
            retFlag[3] = datasegs[2].Replace("/", "").Replace(" ", "").Replace(":", "");
            retFlag[4] = datasegs[4];
            return retFlag;
        }

        private int GetFileType(string fileName, string inout)
        {
            return Param.CheckFile(fileName, inout);
        }

        private string GetLineType(int fileNo)
        {
            if (fileNo >= 0 && fileNo < Param.HostList.Count)
            {
                return Param.HostList[fileNo].LineType;
            }

            return "未知";
        }

        private string GetInOrOut(string inout)
        {
            if (Param.INOUT_MAP.ContainsKey(inout))
                return Param.INOUT_MAP[inout];

            return inout == "0" ? "出库" : (inout == "1" ? "入库" : "未知");
            //if (fileNo >= 0 && fileNo < Param.HostList.Count)
            //{
            //    return Param.HostList[fileNo].InOut;
            //}

            //return "未知";
        }

        private bool Download(string fileName, string subpath, CHostConfig host)
        {
            FTPHelper helper = host == null ? FTPFactory.CreateSrcFtp() : FTPFactory.CreateSrcFtp(host);

            if (helper == null) return false;

            helper.Download(@".\" + subpath, fileName);

            return true;
        }

        private bool Upload(string fileName, string inout)
        {
            var helper = FTPFactory.CreateDestFtp(fileName, inout);
            var path = Application.StartupPath + @"\datas\";
            if (helper == null)
            {
                File.Delete(fileName);
                Log.WriteLine(string.Format("找不到相关ftp配置，文件名：{0}，出入库类型：{1}。", fileName, inout));
                return false;
            }
            Log.WriteLine(string.Format("文件名称：{0}，出入库类型：{1}，目标ftp服务器：{2}。", fileName, inout, helper.FtpServerIP));
            if (File.Exists(path + "notify.txt"))
            {
                File.Delete(path + "notify.txt");
            }
            File.Copy(path + fileName, path + "notify.txt");
            bool flag = helper.Upload(path + "notify.txt");

            if (!flag)
            {
                Thread.Sleep(500);
                flag = helper.Upload(path + "notify.txt");

            }
            if (flag)
            {
                Log.WriteLine(string.Format("文件名称：{0}，上传完毕。", fileName));
            }
            else
            {
                Log.WriteLine(string.Format("文件名称：{0}，上传失败！", fileName));
            }
            //上传完毕，删除本地
            File.Delete(path + "notify.txt");
            File.Delete(path + fileName);  // 先不进行删除
            // Move(fileName);  // 保存本地文件到具体目录

            return true;
        }

        private bool UploadCameraFile(string fileName, string inout, CHostConfig host)
        {
            fileName = Application.StartupPath + @"\datas\" + host.Key + @"\" + fileName;
            int destIndex = -1;
            for (var i = 0; i < Param.HostList.Count; i++)
            {
                if (Param.HostList[i].Key == host.Dest)
                {
                    destIndex = i;
                    break;
                }
            }
            if (destIndex < 0)
            {
                File.Delete(fileName);
                Log.WriteLine(string.Format("找不到目标主机地址"));
                return false;
            }
            var helper = FTPFactory.CreateDestFtp(destIndex);

            if (helper == null)
            {
                File.Delete(fileName);
                Log.WriteLine(string.Format("找不到相关ftp配置，文件名：{0}，出入库类型：{1}。", fileName, inout));
                return false;
            }
            Log.WriteLine(string.Format("文件名称：{0}，出入库类型：{1}，目标ftp服务器：{2}。", fileName, inout, helper.FtpServerIP));
            var toFile = Application.StartupPath + @"\datas\" + host.Key + @"\notify.txt";
            if (File.Exists(toFile))
            {
                File.Delete(toFile);
            }

            File.Copy(fileName, toFile);
            bool flag = helper.Upload(toFile);

            if (!flag)
            {
                Thread.Sleep(500);
                flag = helper.Upload(toFile);

            }
            if (flag)
            {
                Log.WriteLine(string.Format("文件名称：{0}，上传完毕。", fileName));
            }
            else
            {
                Log.WriteLine(string.Format("文件名称：{0}，上传失败！", fileName));
            }
            //上传完毕，删除本地
            File.Delete(toFile);
            File.Delete(fileName);  // 先不进行删除
            // Move(fileName);  // 保存本地文件到具体目录

            return true;
        }

        private void Move(string fileName)
        {
            string path = Application.StartupPath + @"\datas\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            File.Move(fileName, path + fileName);
        }
    }
}
