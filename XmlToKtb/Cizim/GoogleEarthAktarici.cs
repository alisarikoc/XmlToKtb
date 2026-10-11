using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using XmlToKtb.Projeksiyon;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Çizim ekranındaki objeleri, tanımlı koordinat sistemi üzerinden WGS84 enlem/boylama çevirip
    /// Google Earth'te açılabilecek KML içeriği üretir (KMZ kaydı, bu KML metnini zip'leyerek yapılır).
    /// </summary>
    public static class GoogleEarthAktarici
    {
        private const int DaireOrnekSayisi = 72;
        private const int YayOrnekSayisi = 36;

        private static readonly XNamespace Ns = "http://www.opengis.net/kml/2.2";

        /// <summary>KML üretir; koordinat sistemi tanımsızsa veya hiçbir obje dönüştürülemezse null döner.</summary>
        public static string KmlOlustur(IEnumerable<CizimNesnesi> nesneler, ProjeksiyonSistemi kaynakSistem, DilimBilgisi? kaynakDilim)
        {
            if (kaynakSistem == ProjeksiyonSistemi.Tanimsiz)
                return null;

            var dokuman = new XElement(Ns + "Document",
                new XElement(Ns + "name", "XmlToKtb Çizim"),
                new XElement(Ns + "Style",
                    new XAttribute("id", "cizgiStil"),
                    new XElement(Ns + "LineStyle",
                        new XElement(Ns + "color", "ff0000ff"),
                        new XElement(Ns + "width", 2))));

            // Obje tipine göre gruplanmış klasörler (Folder), eklendikleri ilk sıraya göre; her placemark'a
            // kendi tipinden ve sıra numarasından oluşan bir isim veriliyor (Yazı objeleri kendi metnini korur).
            var klasorSirasi = new List<string>();
            var klasorIcerikleri = new Dictionary<string, List<XElement>>();
            var tipSayaci = new Dictionary<string, int>();

            foreach (var nesne in nesneler)
            {
                foreach (var placemark in NesneyiPlacemarkYap(nesne, kaynakSistem, kaynakDilim))
                {
                    var tip = nesne.Tip;
                    if (!klasorIcerikleri.TryGetValue(tip, out var liste))
                    {
                        liste = new List<XElement>();
                        klasorIcerikleri[tip] = liste;
                        klasorSirasi.Add(tip);
                        tipSayaci[tip] = 0;
                    }

                    tipSayaci[tip]++;
                    if (placemark.Element(Ns + "name") == null)
                        placemark.AddFirst(new XElement(Ns + "name", tip + " " + tipSayaci[tip]));

                    liste.Add(placemark);
                }
            }

            if (klasorSirasi.Count == 0)
                return null;

            foreach (var tip in klasorSirasi)
            {
                var klasor = new XElement(Ns + "Folder", new XElement(Ns + "name", tip));
                foreach (var placemark in klasorIcerikleri[tip])
                    klasor.Add(placemark);
                dokuman.Add(klasor);
            }

            var kml = new XElement(Ns + "kml", dokuman);
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + kml;
        }

        private static IEnumerable<XElement> NesneyiPlacemarkYap(CizimNesnesi nesne, ProjeksiyonSistemi sistem, DilimBilgisi? dilim)
        {
            switch (nesne)
            {
                case CizgiNesnesi c:
                {
                    var cizgi = CizgiPlacemarkOlustur(new[] { (c.BaslangicX, c.BaslangicY, c.BaslangicZ), (c.BitisX, c.BitisY, c.BitisZ) }, sistem, dilim);
                    if (cizgi != null) yield return cizgi;
                    break;
                }
                case NoktaNesnesi n:
                {
                    var nokta = NoktaPlacemarkOlustur(n.X, n.Y, n.Z, null, sistem, dilim);
                    if (nokta != null) yield return nokta;
                    break;
                }
                case YaziNesnesi y when !string.IsNullOrEmpty(y.Metin):
                {
                    var nokta = NoktaPlacemarkOlustur(y.KonumX, y.KonumY, y.KonumZ, y.Metin, sistem, dilim);
                    if (nokta != null) yield return nokta;
                    break;
                }
                case DaireNesnesi d:
                {
                    var noktalar = new List<(double X, double Y, double Z)>();
                    for (var i = 0; i <= DaireOrnekSayisi; i++)
                    {
                        var aci = 2 * Math.PI * i / DaireOrnekSayisi;
                        noktalar.Add((d.MerkezX + d.Yaricap * Math.Cos(aci), d.MerkezY + d.Yaricap * Math.Sin(aci), d.MerkezZ));
                    }
                    var cizgi = CizgiPlacemarkOlustur(noktalar, sistem, dilim);
                    if (cizgi != null) yield return cizgi;
                    break;
                }
                case YayNesnesi a:
                {
                    var noktalar = new List<(double X, double Y, double Z)>();
                    var supurme = NormalizeSupurme(a.BitisAcisi - a.BaslangicAcisi);
                    for (var i = 0; i <= YayOrnekSayisi; i++)
                    {
                        var aciDerece = a.BaslangicAcisi + supurme * i / YayOrnekSayisi;
                        var rad = aciDerece * Math.PI / 180.0;
                        noktalar.Add((a.MerkezX + a.Yaricap * Math.Cos(rad), a.MerkezY + a.Yaricap * Math.Sin(rad), a.MerkezZ));
                    }
                    var cizgi = CizgiPlacemarkOlustur(noktalar, sistem, dilim);
                    if (cizgi != null) yield return cizgi;
                    break;
                }
                case SpiralNesnesi s:
                {
                    var noktalar = s.OrnekNoktalariVeZGetir().Select(p => ((double)p.Nokta.X, (double)p.Nokta.Y, p.Z));
                    var cizgi = CizgiPlacemarkOlustur(noktalar, sistem, dilim);
                    if (cizgi != null) yield return cizgi;
                    break;
                }
            }
        }

        private static XElement CizgiPlacemarkOlustur(IEnumerable<(double X, double Y, double Z)> dunyaNoktalari, ProjeksiyonSistemi sistem, DilimBilgisi? dilim)
        {
            var koordinatMetni = new List<string>();
            foreach (var (x, y, z) in dunyaNoktalari)
            {
                if (!CografiyeCevir(x, y, sistem, dilim, out var boylam, out var enlem))
                    return null;
                koordinatMetni.Add(FormatKoordinat(boylam, enlem, z));
            }

            if (koordinatMetni.Count < 2)
                return null;

            return new XElement(Ns + "Placemark",
                new XElement(Ns + "styleUrl", "#cizgiStil"),
                new XElement(Ns + "LineString",
                    new XElement(Ns + "tessellate", 1),
                    new XElement(Ns + "altitudeMode", "absolute"),
                    new XElement(Ns + "coordinates", string.Join(" ", koordinatMetni))));
        }

        private static XElement NoktaPlacemarkOlustur(double x, double y, double z, string ad, ProjeksiyonSistemi sistem, DilimBilgisi? dilim)
        {
            if (!CografiyeCevir(x, y, sistem, dilim, out var boylam, out var enlem))
                return null;

            var placemark = new XElement(Ns + "Placemark",
                new XElement(Ns + "Point",
                    new XElement(Ns + "altitudeMode", "absolute"),
                    new XElement(Ns + "coordinates", FormatKoordinat(boylam, enlem, z))));

            if (!string.IsNullOrEmpty(ad))
                placemark.AddFirst(new XElement(Ns + "name", ad));

            return placemark;
        }

        private static bool CografiyeCevir(double x, double y, ProjeksiyonSistemi sistem, DilimBilgisi? dilim, out double boylam, out double enlem)
        {
            return KoordinatDonusturucu.Donustur(sistem, dilim, x, y, ProjeksiyonSistemi.Cografi, null, out boylam, out enlem);
        }

        private static string FormatKoordinat(double boylam, double enlem, double z)
        {
            return boylam.ToString("F8", CultureInfo.InvariantCulture) + "," + enlem.ToString("F8", CultureInfo.InvariantCulture) + "," + z.ToString("F3", CultureInfo.InvariantCulture);
        }

        private static double NormalizeSupurme(double supurmeDerece)
        {
            var sonuc = supurmeDerece % 360.0;
            if (sonuc < 0) sonuc += 360.0;
            return sonuc <= 0 ? sonuc + 360.0 : sonuc;
        }
    }
}
