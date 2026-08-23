using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.user
{
    [DataContract, Serializable]
    class UserModel
    {

        [DataMember]
        private string userName;

        [DataMember]
        private string password;

        [DataMember]
        private string phone;

        [DataMember]
        private string mail;

        [DataMember]
        private string refresh;

        [DataMember]
        private string registerDate;

        [DataMember]
        private string lastLoginDate;

        [DataMember]
        private string status;
        




        [DataMember]
        private string refreshFile;

        [DataMember]
        private string filePath;

        [DataMember]
        private string fileName;

        [DataMember]
        private int currentPage;

        [DataMember]
        private int pageSize;

        public int getCurrentPage()
        {
            return this.currentPage;
        }

        public void setCurrentPage(int currentPage)
        {
            this.currentPage = currentPage;
        }

        public int getPageSize()
        {
            return this.pageSize;
        }

        public void setPageSize(int pageSize)
        {
            this.pageSize = pageSize;
        }

        public string getFilePath()
        {
            return this.filePath;
        }

        public void setFilePath(string filePath)
        {
            this.filePath = filePath;
        }

        public string getFileName()
        {
            return this.fileName;
        }

        public void setFileName(string fileName)
        {
            this.fileName = fileName;
        }

        public string getRefreshFile()
        {
            return this.refreshFile;
        }

        public void setRefreshFile(string refreshFile)
        {
            this.refreshFile = refreshFile;
        }

        public string getStatus()
        {
            return this.status;
        }

        public void setStatus(string status)
        {
            this.status = status;
        }

        public string getLastLoginDate()
        {
            return this.lastLoginDate;
        }

        public void setlastLoginDate(string lastLoginDate)
        {
            this.lastLoginDate = lastLoginDate;
        }

        public string getRegisterDate()
        {
            return this.registerDate;
        }

        public void setRegisterDate(string registerDate)
        {
            this.registerDate = registerDate;
        }

        public string getRefresh()
        {
            return this.refresh;
        }

        public void setRefresh(string refresh)
        {
            this.refresh = refresh;
        }

        public string getUserName()
        {
            return this.userName;
        }

        public void setUserName(string userName)
        {
            this.userName = userName;
        }

        public string getPassword()
        {
            return this.password;
        }

        public void setPassword(string password)
        {
            this.password = password;
        }

        public string getPhone()
        {
            return this.phone;
        }

        public void setPhone(string phone)
        {
            this.phone = phone;
        }

        public string getMail()
        {
            return this.mail;
        }

        public void setMail(string mail)
        {
            this.mail = mail;
        }
    }
}
