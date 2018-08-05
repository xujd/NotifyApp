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

        private List<string> lastFileList = new List<string>();

        public void Do()
        {
            Thread thread = new Thread(RunThread);
            thread.Start();
        }

        private void RunThread()
        {
            FTPHelper helper = new FTPHelper(Param.SRC_HOST, Param.SRC_PATH, Param.SRC_USR, Param.SRC_PWD);

            Thread.Sleep(500);

            while (true)
            {
                lock (Param.LOCK)
                {

                    if (Param.IsClosing)
                        break;

                    Run(helper);

                    while (lastFileList.Count > 100)//只保留最新的100个文件
                        lastFileList.RemoveAt(0);
                }
                Thread.Sleep(TimeSpan.FromSeconds(Param.INTERVAL));

            }
        }

        private void Run(FTPHelper helper)
        {
            //Log.Clean();

            Log.WriteLine("开始执行任务...");
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
                if (fileTime < lastTime)
                    continue;

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
                Log.WriteLine(string.Format("下载文件【{0}】", fileName));
                Log.WriteLog(string.Format("下载文件【{0}】", fileName));
                //下载
                Download(fileName);
                Log.WriteLine(string.Format("解析文件【{0}】", fileName));
                Log.WriteLog(string.Format("解析文件【{0}】", fileName));
                //解析
                var inout = Parse(fileName);
                //上传
                if (inout == "-1")
                {
                    Log.WriteLine(string.Format("【{0}】找不到匹配的推送规则。", fileName));
                    Log.WriteLog(string.Format("【{0}】找不到匹配的推送规则。", fileName));
                }
                else if (inout == "-2")
                {
                    Log.WriteLine(string.Format("【{0}】格式错误，无法解析。", fileName));
                    Log.WriteLog(string.Format("【{0}】格式错误，无法解析。", fileName));
                }
                else
                {
                    Log.WriteLine(string.Format("上传文件【{0}】", fileName));
                    Log.WriteLog(string.Format("上传文件【{0}】", fileName));
                    Upload(fileName, inout);
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

            Log.WriteLine("执行完毕。");
        }

        private void CheckOutofDate()
        {
            for (int i = 0; i < Param.LastFileTime.Count; i++)
            {
                var dt = Param.LastFileTime[i];

                if (dt.AddMinutes(5) < DateTime.Now)
                {
                    UploadDefault(i);
                    Param.LastFileTime[i] = DateTime.Now;
                    Log.WriteLog(string.Format("已5分钟没有新消息，更新{0}号牌为默认值。", i + 1));
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

        private string Parse(string fileName)
        {
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
                return "-2";
            }

            //0-上行/出库，1-下行/入库
            var inout = data.Substring(18, 1);

            //时间
            var time = string.Format("{0}:{1}:{2}", data.Substring(74, 2), data.Substring(76, 2), data.Substring(78, 2));
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

            if (type < 0) return "-1";

            string result = string.Format(Param.FORMAT, GetLineType(type), GetInOrOut(inout), /*trainType,*/ trainNo, time);
            //var textdata = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.ASCII, System.Text.Encoding.UTF8.GetBytes(result.ToCharArray())));

            File.WriteAllText(fileName, result, Encoding.Default);
            string path = Application.StartupPath + @"\datas\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(path + fileName, result, Encoding.Default);


            Log.WriteLine(string.Format("文件【{0}】,解析结果：{1}", fileName, result));
            Log.WriteLog(string.Format("文件【{0}】,解析结果：{1}", fileName, result));

            //更新最新时间
            UpdateTime(type);

            return inout;
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

        private bool Download(string fileName)
        {
            FTPHelper helper = FTPFactory.CreateSrcFtp();

            if (helper == null) return false;

            helper.Download(@".\", fileName);

            return true;
        }

        private bool Upload(string fileName, string inout)
        {
            var helper = FTPFactory.CreateDestFtp(fileName, inout);

            if (helper == null)
            {
                File.Delete(fileName);
                Log.WriteLine(string.Format("找不到相关ftp配置，文件名：{0}，出入库类型：{1}。", fileName, inout));
                Log.WriteLog(string.Format("找不到相关ftp配置，文件名：{0}，出入库类型：{1}。", fileName, inout));
                return false;
            }
            Log.WriteLine(string.Format("文件名称：{0}，出入库类型：{1}，目标ftp服务器：{2}。", fileName, inout, helper.FtpServerIP));
            Log.WriteLog(string.Format("文件名称：{0}，出入库类型：{1}，目标ftp服务器：{2}。", fileName, inout, helper.FtpServerIP));

            File.Copy(fileName, "notify.txt");
            bool flag = helper.Upload(@".\notify.txt");

            if (!flag)
            {
                Thread.Sleep(500);
                helper.Upload(@".\notify.txt");

            }
            //上传完毕，删除本地
            File.Delete("notify.txt");
            File.Delete(fileName);
            //Move(fileName);

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
