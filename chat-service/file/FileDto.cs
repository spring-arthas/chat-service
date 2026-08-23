using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.file
{
    [DataContract, Serializable]
    public class FileDto
    {
        [DataMember]
        private long id;

        [DataMember]
        private long pId;

        [DataMember]
        private string fileName;

        [DataMember]
        private string filePath;

        [DataMember]
        private string fileType;

        [DataMember]
        private string fileSize;

        [DataMember]
        private string isFile;

        [DataMember]
        private string isExist;

        [DataMember]
        private string hasChild;

        [DataMember]
        private string del;

        [DataMember]
        private string gmtCreate;

        [DataMember]
        private string gmtModified;

        [DataMember]
        private string delTime;

        [DataMember]
        private List<FileDto> childFileList;

        [DataMember]
        private string repeatCreate;

        [DataMember]
        private long fileCount;

        public long getFileCount()
        {
            return this.fileCount;
        }

        public void setFileCount(long fileCount)
        {
            this.fileCount = fileCount;
        }

        public string getFileSize()
        {
            return this.fileSize;
        }

        public void setFileSize(string fileSize)
        {
            this.fileSize = fileSize;
        }

        public string getRepeatCreate()
        {
            return this.repeatCreate;
        }

        public void setRepeatCreate(string repeatCreate)
        {
            this.repeatCreate = repeatCreate;
        }

        public long getId()
        {
            return this.id;
        }

        public void setId(long id)
        {
            this.id = id;
        }

        public long getPid()
        {
            return this.pId;
        }

        public void setPid(long pId)
        {
            this.pId = pId;
        }

        public string getFileName()
        {
            return this.fileName;
        }

        public void setFileName(string fileName)
        {
            this.fileName = fileName;
        }

        public string getFilePath()
        {
            return this.filePath;
        }

        public void setFilePath(string filePath)
        {
            this.filePath = filePath;
        }

        public string getFileType()
        {
            return this.fileType;
        }

        public void setFileType(string fileType)
        {
            this.fileType = fileType;
        }

        public string getIsFile()
        {
            return this.isFile;
        }

        public void setIsFile(string isFile)
        {
            this.isFile = isFile;
        }

        public string getIsExist()
        {
            return this.isExist;
        }

        public void setIsExist(string isExist)
        {
            this.isExist = isExist;
        }

        public string getHasChild()
        {
            return this.hasChild;
        }

        public void setHasChild(string hasChild)
        {
            this.hasChild = hasChild;
        }

        public string getGmtCreate()
        {
            return this.gmtCreate;
        }

        public void setGmtCreate(string gmtCreate)
        {
            this.gmtCreate = gmtCreate;
        }

        public string getGmtModified()
        {
            return this.gmtModified;
        }

        public void setGmtModified(string gmtModified)
        {
            this.gmtModified = gmtModified;
        }

        public List<FileDto> getChildFileList()
        {
            return this.childFileList;
        }

        public void setChildFileList(List<FileDto> childFileList)
        {
            this.childFileList = childFileList;
        }
    }
}
