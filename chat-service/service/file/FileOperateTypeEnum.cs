using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.service.file
{
    public class FileOperateTypeEnum
    {
        private TypeEnum typeEnum;

        public enum TypeEnum
        {
            TRANSPORT = 00000000, STORE = 00000001
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
