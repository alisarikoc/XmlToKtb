using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Common
{
    public class DosyaDeserializer
    {
        public T XmlDeserializer<T>(string xmlDosyaYolu)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var fileStream = new FileStream(xmlDosyaYolu, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new LandXmlSurumBagimsizReader(fileStream))
            {
                return (T) serializer.Deserialize(reader);
            }
        }

        public T XmlDeserializer<T>(Stream xmlAkisi)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new LandXmlSurumBagimsizReader(xmlAkisi))
            {
                return (T) serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// LandXML sınıfı yalnızca LandXML-1.2 ad alanını tanır; LandXML-1.0/1.1 ad alanıyla kaydedilmiş dosyalar
        /// (ör. Leica Geo Office çıktıları) "&lt;LandXML xmlns=...&gt; beklenmiyordu" hatası verir. Bu okuyucu, diğer
        /// LandXML sürümlerinin ad alanını 1.2 olarak bildirerek aynı sınıfla okunmalarını sağlar.
        /// </summary>
        private class LandXmlSurumBagimsizReader : XmlTextReader
        {
            private const string LandXmlOnEk = "http://www.landxml.org/schema/LandXML-1.";
            private const string LandXml12 = "http://www.landxml.org/schema/LandXML-1.2";

            public LandXmlSurumBagimsizReader(Stream stream) : base(stream)
            {
            }

            public override string NamespaceURI
            {
                get
                {
                    var adAlani = base.NamespaceURI;
                    // XmlSerializer ad alanlarını referans eşitliğiyle karşılaştırdığından değer NameTable üzerinden verilir.
                    if (adAlani.StartsWith(LandXmlOnEk, StringComparison.Ordinal) && adAlani != LandXml12)
                        return NameTable.Add(LandXml12);
                    return adAlani;
                }
            }
        }
    }
}
