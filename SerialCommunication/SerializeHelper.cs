using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SerialCommunication
{
    class SerializeHelper
    {
        public static string ScriptSerializeToXML<T>(T t)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            MemoryStream mem = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mem, new UTF8Encoding(false));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            serializer.Serialize(writer, t, ns);
            writer.Close();
            return Encoding.UTF8.GetString(mem.ToArray());
        }

        public static T JSONXMLToObject<T>(string str)
        {
            XmlDocument xdoc = new XmlDocument();
            try
            {
                xdoc.LoadXml(str);
                XmlNodeReader reader = new XmlNodeReader(xdoc.DocumentElement);
                XmlSerializer ser = new XmlSerializer(typeof(T));
                object obj = ser.Deserialize(reader);
                return (T)obj;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return default(T);
            }
        }
    }
}
