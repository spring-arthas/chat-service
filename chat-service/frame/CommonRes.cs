using chat_service.file;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.frame
{
    [DataContract, Serializable]
    public class CommonRes
    {
        [DataMember]
        private string code;

        [DataMember]
        private string userName;  // 发起消息的用户，可能是发起用户也有可能是接收用户

        [DataMember]
        private string messageType; // 消息类型

        [DataMember]
        private string message;  // 消息文本内容

        [DataMember]
        private string loginTime;

        [DataMember]
        private string time; // 消息时间

        // ****************************************** 文件操作 ********************************************

        [DataMember]
        private string launchUserName; // 发送端用户名

        [DataMember]
        private string receiveUserName; // 接收端用户名

        [DataMember]
        private string fileName; // 文件名称

        [DataMember]
        private long fileSize; // 文件大小

        [DataMember]
        private string operate; // 消息中指定需要执行的操作

        [DataMember]
        private string status; // 当前操作的文件的任务状态 1、未开始 2、处理中 3、处理完成

        [DataMember]
        private byte[] streamData; // 当前操作的文件的任务状态 1、未开始 2、处理中 3、处理完成

        [DataMember]
        private FileDto data;  // 数据

        [DataMember]
        private int downloadLoopCount;  // 当前文件下载时对用进度条需要循环总次数

        public void setDownloadLoopCount(int downloadLoopCount)
        {
            this.downloadLoopCount = downloadLoopCount;
        }

        public int getDownloadLoopCount()
        {
            return this.downloadLoopCount;
        }

        public FileDto getData()
        {
            return this.data;
        }

        public void setData(FileDto data)
        {
            this.data = data;
        }

        public string getMessageType()
        {
            return this.messageType;
        }

        public void setMessageType(string messageType)
        {
            this.messageType = messageType;
        }

        public string getCode()
        {
            return this.code;
        }

        public void setCode(string code)
        {
            this.code = code;
        }

        public string getUserName()
        {
            return this.userName;
        }

        public void setUserName(string userName)
        {
            this.userName = userName;
        }

        public string getStatus()
        {
            return this.status;
        }

        public void setStatus(string status)
        {
            this.status = status;
        }

        public byte[] getStreamData()
        {
            return this.streamData;
        }

        public void setStreamData(byte[] streamData)
        {
            this.streamData = streamData;
        }

        public string getMessage()
        {
            return this.message;
        }

        public void setMessage(string message)
        {
            this.message = message;
        }

        public string getLoginTime()
        {
            return this.loginTime;
        }

        public void setLoginTime(string loginTime)
        {
            this.loginTime = loginTime;
        }

        public string getTime()
        {
            return this.time;
        }

        public void setTime(string time)
        {
            this.time = time;
        }

        public string getLaunchUserName()
        {
            return this.launchUserName;
        }

        public void setLaunchUserName(string launchUserName)
        {
            this.launchUserName = launchUserName;
        }

        public string getReceiveUserName()
        {
            return this.receiveUserName;
        }

        public void setReceiveUserName(string receiveUserName)
        {
            this.receiveUserName = receiveUserName;
        }

        public string getFileName()
        {
            return this.fileName;
        }

        public void setFileName(string fileName)
        {
            this.fileName = fileName;
        }

        public long getFileSize()
        {
            return this.fileSize;
        }

        public void setFileSize(long fileSize)
        {
            this.fileSize = fileSize;
        }

        public string getOperate()
        {
            return this.operate;
        }

        public void setOperate(string operate)
        {
            this.operate = operate;
        }
    }
}
