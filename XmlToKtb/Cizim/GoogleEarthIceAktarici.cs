using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using XmlToKtb.Projeksiyon;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// KML/KMZ dosyasındaki Placemark'ları (Point/LineString/Polygon), tanımlı koordinat sistemi üzerinden
    /// dünya (çizim ekranı) koordinatına çevirip çizim ekranı objelerine dönüştürür (KmlOlustur'un tersi).
    /// </summary>
    public static class GoogleEarthIceAktarici
    {
        public static List<CizimNesnesi> DosyadanOku(string dosyaYolu, ProjeksiyonSistemi hedefSistem, DilimBilgisi? hedefDilim)
        {
            var kmlMetni = Path.GetExtension(dosyaYolu).Equals(".kmz", StringComparison.OrdinalIgnoreCase)
                ? KmzIcindenKmlOku(dosyaYolu)
                : File.ReadAllText(dosyaYolu);

            return KmlAyikla(kmlMetni, hedefSistem, hedefDilim);
        }

        private static string KmzIcindenKmlOku(string dosyaYolu)
        {
            using (var arsiv = ZipFile.OpenRead(dosyaYolu))
            {
                var girdi = arsiv.Entries.FirstOrDefault(e => e.FullName.Equals("doc.kml", StringComparison.OrdinalIgnoreCase))
                            ?? arsiv.Entries.FirstOrDefault(e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));
                if (girdi == null)
                    throw new InvalidDataException("KMZ içinde .kml dosyası bulunamadı.");

                using (var akis = girdi.Open())
                using (var okuyucu = new StreamReader(akis, Encoding.UTF8))
                    return okuyucu.ReadToEnd();
            }
        }

        public static List<CizimNesnesi> KmlAyikla(string kmlMetni, ProjeksiyonSistemi hedefSistem, DilimBilgisi? hedefDilim)
        {
            var sonuc = new List<CizimNesnesi>();
            var belge = XDocument.Parse(kmlMetni);

            foreach (var placemark in belge.Descendants().Where(e => e.Name.LocalName == "Placemark"))
            {
                var adElemani = placemark.Elements().FirstOrDefault(e => e.Name.LocalName == "name");
                var ad = adElemani?.Value;

                var noktaElemani = placemark.Elements().FirstOrDefault(e => e.Name.LocalName == "Point");
                if (noktaElemani != null)
                {
                    var noktalar = KoordinatlariOku(noktaElemani, hedefSistem, hedefDilim);
                    if (noktalar.Count > 0)
                    {
                        var (x, y, z) = noktalar[0];
                        sonuc.Add(new NoktaNesnesi { X = x, Y = y, Z = z });

                        // Bizim KmlOlustur'umuzun kendi ürettiği "Nokta 3" gibi otomatik isimler için ayrı bir
                        // etiket eklemiyoruz (round-trip'te gereksiz Yazı objesi çoğalmasın diye); ama gerçek
                        // bir isim varsa (kullanıcının Google Earth'te verdiği ad ya da bizim Yazı objemizin
                        // orijinal metni) onu Yazı etiketi olarak koruyoruz.
                        if (!string.IsNullOrWhiteSpace(ad) && !OtomatikUretilenIsimMi(placemark, ad))
                            sonuc.Add(new YaziNesnesi { KonumX = x, KonumY = y, KonumZ = z, Metin = ad, Yukseklik = 1.5 });
                    }
                    continue;
                }

                var cizgiElemani = placemark.Elements().FirstOrDefault(e => e.Name.LocalName == "LineString");
                if (cizgiElemani != null)
                {
                    var noktalar = KoordinatlariOku(cizgiElemani, hedefSistem, hedefDilim);
                    NoktalariCizgiSegmentleriYap(noktalar, sonuc);
                    continue;
                }

                var poligonElemani = placemark.Elements().FirstOrDefault(e => e.Name.LocalName == "Polygon");
                if (poligonElemani != null)
                {
                    var disHalka = poligonElemani.Descendants().FirstOrDefault(e => e.Name.LocalName == "LinearRing");
                    if (disHalka != null)
                    {
                        var noktalar = KoordinatlariOku(disHalka, hedefSistem, hedefDilim);
                        NoktalariCizgiSegmentleriYap(noktalar, sonuc);
                    }
                }
            }

            return sonuc;
        }

        /// <summary>
        /// Placemark'ın adı, kendisini içeren Folder'ın adına dayalı "{KlasörAdı} {sayı}" kalıbıyla birebir
        /// eşleşiyor mu (KmlOlustur'un Nokta/Çizgi/Daire/Yay/Spiral için ürettiği otomatik isim kalıbı budur).
        /// </summary>
        private static bool OtomatikUretilenIsimMi(XElement placemark, string ad)
        {
            var klasorAdi = placemark.Ancestors().FirstOrDefault(e => e.Name.LocalName == "Folder")
                ?.Elements().FirstOrDefault(e => e.Name.LocalName == "name")?.Value;

            if (string.IsNullOrEmpty(klasorAdi))
                return false;

            return Regex.IsMatch(ad, "^" + Regex.Escape(klasorAdi) + @"\s+\d+$");
        }

        private static void NoktalariCizgiSegmentleriYap(List<(double X, double Y, double Z)> noktalar, List<CizimNesnesi> sonuc)
        {
            for (var i = 0; i < noktalar.Count - 1; i++)
            {
                var (bx, by, bz) = noktalar[i];
                var (ex, ey, ez) = noktalar[i + 1];
                sonuc.Add(new CizgiNesnesi { BaslangicX = bx, BaslangicY = by, BaslangicZ = bz, BitisX = ex, BitisY = ey, BitisZ = ez });
            }
        }

        private static List<(double X, double Y, double Z)> KoordinatlariOku(XElement icerenEleman, ProjeksiyonSistemi hedefSistem, DilimBilgisi? hedefDilim)
        {
            var sonuc = new List<(double X, double Y, double Z)>();
            var koordinatElemani = icerenEleman.Elements().FirstOrDefault(e => e.Name.LocalName == "coordinates");
            if (koordinatElemani == null)
                return sonuc;

            var parcalar = koordinatElemani.Value.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var parca in parcalar)
            {
                var bilesenler = parca.Split(',');
                if (bilesenler.Length < 2)
                    continue;

                if (!double.TryParse(bilesenler[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var boylam) ||
                    !double.TryParse(bilesenler[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var enlem))
                    continue;

                var z = 0.0;
                if (bilesenler.Length >= 3)
                    double.TryParse(bilesenler[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z);

                if (KoordinatDonusturucu.Donustur(ProjeksiyonSistemi.Cografi, null, boylam, enlem, hedefSistem, hedefDilim, out var x, out var y))
                    sonuc.Add((x, y, z));
            }

            return sonuc;
        }
    }
}
