using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.file
{
    [DataContract, Serializable]
    class FileRequest
    {
        [DataMember]
        private string fileName;

        public string getFileName()
        {
            return this.fileName;
        }

        public void setFileName(string fileName)
        {
            this.fileName = fileName;
        }
    }
}
