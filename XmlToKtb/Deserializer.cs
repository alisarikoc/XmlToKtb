using System.IO;
using System.Xml.Serialization;

namespace XmlToKtb
{
    public class Deserializer
    {
        public T XmlDeserializer<T>(string xmlFilePath)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var fileStream = new FileStream(xmlFilePath, FileMode.Open))
            {
                return (T)serializer.Deserialize(fileStream);
            }
        }
    }
}
