using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Xml;

namespace chat_service.util
{
    class XmlConfigUtils
    {
        private static XmlDocument doc = new XmlDocument();
        private static string xmlFilePath = "";

        // 初始化Doc
        public static void InitDoc()
        {
            xmlFilePath = AppDomain.CurrentDomain.BaseDirectory.ToString() + "client.xml";
            doc.Load(xmlFilePath);
        }

        // 根据key读取配置文件中的value值
        public static string GetValue(string key)
        {
            string value = "";
            XmlNodeList nodes = doc.GetElementsByTagName("add");
            for (int i = 0; i < nodes.Count; i++)
            {
                //获得将当前元素的key属性
                XmlAttribute att = nodes[i].Attributes["key"];
                //根据元素的第一个属性来判断当前的元素是不是目标元素
                if (att.Value == key)
                {
                    //对目标元素中的第二个属性赋值
                    att = nodes[i].Attributes["value"];
                    return value = att.Value;
                }
            }

            return value;
        }

        // 根据key修改配置文件中的value值
        public static void UpdateConfig(string key, string value)
        {
            //找出名称为“add”的所有元素
            XmlNodeList nodes = doc.GetElementsByTagName("add");
            for (int i = 0; i < nodes.Count; i++)
            {
                //获得将当前元素的key属性
                XmlAttribute att = nodes[i].Attributes["key"];
                //根据元素的第一个属性来判断当前的元素是不是目标元素
                if (att.Value == key)
                {
                    //对目标元素中的第二个属性赋值
                    att = nodes[i].Attributes["value"];
                    att.Value = value;
                    break;
                }
            }

            //保存上面的修改
            doc.Save(xmlFilePath);
        }
    }
}
