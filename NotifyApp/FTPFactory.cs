using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotifyApp
{
    class FTPFactory
    {
        public static FTPHelper CreateSrcFtp()
        {
            return new FTPHelper(Param.SRC_HOST, Param.SRC_PATH, Param.SRC_USR, Param.SRC_PWD);
        }

        public static FTPHelper CreateSrcFtp(CHostConfig host)
        {
            return new FTPHelper(host.Host, host.Path, host.User,host.Password);
        }

        public static FTPHelper CreateDestFtp(string fileName, string inout)
        {
            int dest = Param.CheckFile(fileName, inout);

            return CreateDestFtp(dest);
        }

        public static FTPHelper CreateDestFtp(int dest)
        {
            if (dest >= 0 && dest < Param.HostList.Count)
            {
                return new FTPHelper(Param.HostList[dest].Host, Param.HostList[dest].Path, Param.HostList[dest].User, Param.HostList[dest].Password);
            }

            return null;
        }

        public static List<FTPHelper> CreateMsgFtpList()
        {
            var ftpList = new List<FTPHelper>();

            Param.MSGFILE_FTPList.ForEach(item =>
            {
                ftpList.Add(new FTPHelper(item.Host, item.Path, item.User, item.Password));
            });

            return ftpList;
        }
    }
}
