using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.service.file
{
    public class FileFrameTypeEnum
    {
        private TypeEnum typeEnum;

        public enum TypeEnum
        {
            UPLOAD = 00000000, DOWNLOAD = 00000001, ONLINE_TRANSPORT = 00000010,
            DELETE = 00000011, DATA_TRANSPORT = 00000100, DATA_TRANSPORT_END= 00000101
        }

        public TypeEnum getTypeEnum()
        {
            return this.typeEnum;
        }

        public void setTypeEnum(TypeEnum typeEnum)
        {
            this.typeEnum = typeEnum;
        }

        public static string GetEnumName<TypeEnum>(int value)
        {
            string name = "";
            name = Enum.Parse(typeof(TypeEnum), Enum.GetName(typeof(TypeEnum), value)).ToString();
            return name;
        }

        public static int GetEnumValue<TypeEnum>(string value)
        {
            Type type = typeof(TypeEnum);
            var schoolId = Enum.Format(type, Enum.Parse(type, value.ToUpper()), "d");
            return Convert.ToInt32(schoolId);
        }
    }
}
