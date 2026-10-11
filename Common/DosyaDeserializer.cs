using System.IO;
using System.Xml.Serialization;

namespace Common
{
    public class DosyaDeserializer
    {
        public T XmlDeserializer<T>(string xmlDosyaYolu)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var fileStream = new FileStream(xmlDosyaYolu, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return (T) serializer.Deserialize(fileStream);
            }
        }
    }
}