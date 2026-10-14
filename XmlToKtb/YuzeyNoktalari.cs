using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>Yüzey (üçgen model) noktası. Y = sağa (easting), X = yukarı (northing), Z = kot.</summary>
    public class YuzeyNoktasi
    {
        public string No;
        public double Y;
        public double X;
        public double Z;
    }

    /// <summary>LandXML dosyasındaki bir yüzey (Surface) ve noktaları.</summary>
    public class Yuzey
    {
        public string Ad;
        public readonly List<YuzeyNoktasi> Noktalar = new List<YuzeyNoktasi>();
        /// <summary>Üçgenler: her üçgen için art arda üç nokta indeksi (Noktalar listesine göre).</summary>
        public readonly List<int> Ucgenler = new List<int>();

        public override string ToString()
        {
            return Ad + "  (" + Noktalar.Count + " nokta, " + Ucgenler.Count / 3 + " üçgen)";
        }
    }

    /// <summary>LandXML yüzey dosyalarından noktaları okur ve NCN / Excel olarak yazar.</summary>
    public static class YuzeyNoktalari
    {
        /// <summary>
        /// LandXML dosyasındaki yüzeyleri (Surfaces/Surface/Definition/Pnts/P) okur. Büyük dosyalar için akış halinde
        /// okur; LandXML sürümünden (ad alanından) bağımsızdır. P değeri "X(yukarı) Y(sağa) Z" sırasındadır.
        /// </summary>
        public static List<Yuzey> Oku(string dosyaYolu)
        {
            var yuzeyler = new List<Yuzey>();
            Yuzey guncelYuzey = null;
            var noktalarIcinde = false;
            var ucgenlerIcinde = false;
            // Üçgenler noktaları numarasıyla (P id) gösterir; numaradan Noktalar listesindeki indekse geçiş için.
            var noktaIndeksi = new Dictionary<string, int>();
            var ayiricilar = new[] { ' ', '\t', '\r', '\n' };

            using (var reader = XmlReader.Create(dosyaYolu, new XmlReaderSettings { IgnoreComments = true, DtdProcessing = DtdProcessing.Ignore }))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.LocalName == "Surface")
                        {
                            var ad = reader.GetAttribute("name");
                            guncelYuzey = new Yuzey { Ad = string.IsNullOrWhiteSpace(ad) ? "Yüzey " + (yuzeyler.Count + 1) : ad };
                            yuzeyler.Add(guncelYuzey);
                            noktaIndeksi.Clear();
                        }
                        else if (reader.LocalName == "Faces")
                        {
                            ucgenlerIcinde = !reader.IsEmptyElement;
                        }
                        else if (reader.LocalName == "F" && ucgenlerIcinde && guncelYuzey != null)
                        {
                            // i="1" görünmez (yüzeye dahil olmayan) üçgendir.
                            var gorunmez = reader.GetAttribute("i") == "1";
                            var kose = reader.ReadElementContentAsString().Split(ayiricilar, StringSplitOptions.RemoveEmptyEntries);
                            if (!gorunmez && kose.Length >= 3
                                && noktaIndeksi.TryGetValue(kose[0], out var i0) && noktaIndeksi.TryGetValue(kose[1], out var i1) && noktaIndeksi.TryGetValue(kose[2], out var i2))
                            {
                                guncelYuzey.Ucgenler.Add(i0);
                                guncelYuzey.Ucgenler.Add(i1);
                                guncelYuzey.Ucgenler.Add(i2);
                            }
                            continue;
                        }
                        else if (reader.LocalName == "Pnts")
                        {
                            noktalarIcinde = !reader.IsEmptyElement;
                        }
                        else if (reader.LocalName == "P" && noktalarIcinde && guncelYuzey != null)
                        {
                            var no = reader.GetAttribute("id") ?? (guncelYuzey.Noktalar.Count + 1).ToString();
                            // ReadElementContentAsString okuyucuyu bir sonraki düğüme taşır; bu yüzden aşağıdaki Read atlanır.
                            var parcalar = reader.ReadElementContentAsString().Split(ayiricilar, StringSplitOptions.RemoveEmptyEntries);
                            if (parcalar.Length >= 2 && SayiOku(parcalar[0], out var x) && SayiOku(parcalar[1], out var y))
                            {
                                var z = 0.0;
                                if (parcalar.Length >= 3)
                                    SayiOku(parcalar[2], out z);
                                noktaIndeksi[no] = guncelYuzey.Noktalar.Count;
                                guncelYuzey.Noktalar.Add(new YuzeyNoktasi { No = no, X = x, Y = y, Z = z });
                            }
                            continue;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "Pnts")
                    {
                        noktalarIcinde = false;
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "Faces")
                    {
                        ucgenlerIcinde = false;
                    }

                    reader.Read();
                }
            }

            return yuzeyler;
        }

        /// <summary>
        /// DXF dosyasındaki üçgen modeli (3DFACE, üç/dört köşeli kapalı 3B polyline, polyface mesh) tek bir yüzey olarak
        /// okur. Aynı konumdaki köşeler tek noktada birleştirilir. Dosya önce netDxf ile, o okuyamazsa (ör. eski
        /// R12 biçimi) programın kendi DXFLib okuyucusuyla okunur.
        /// </summary>
        public static Yuzey DxftenOku(string dosyaYolu)
        {
            var yuzey = new Yuzey { Ad = Path.GetFileNameWithoutExtension(dosyaYolu) };
            var noktaIndeksi = new Dictionary<string, int>();

            // DXF'te X sağa, Y yukarıdır; NCN düzeninde bunlar sırasıyla Y ve X'tir.
            int NoktaEkle(double dxfX, double dxfY, double dxfZ)
            {
                var anahtar = Math.Round(dxfX, 4).ToString("R", CultureInfo.InvariantCulture) + "|" +
                              Math.Round(dxfY, 4).ToString("R", CultureInfo.InvariantCulture) + "|" +
                              Math.Round(dxfZ, 4).ToString("R", CultureInfo.InvariantCulture);
                if (noktaIndeksi.TryGetValue(anahtar, out var indeks))
                    return indeks;

                indeks = yuzey.Noktalar.Count;
                noktaIndeksi[anahtar] = indeks;
                yuzey.Noktalar.Add(new YuzeyNoktasi { No = (indeks + 1).ToString(), Y = dxfX, X = dxfY, Z = dxfZ });
                return indeks;
            }

            // Köşeleri verilen çokgeni (üçgen ya da dörtgen) üçgen(ler) olarak ekler; çakışan köşeler elenir.
            void CokgenEkle(IList<int> koseler)
            {
                var tekil = new List<int>();
                foreach (var k in koseler)
                {
                    if (!tekil.Contains(k))
                        tekil.Add(k);
                }

                if (tekil.Count < 3)
                    return;
                yuzey.Ucgenler.AddRange(new[] { tekil[0], tekil[1], tekil[2] });
                if (tekil.Count >= 4)
                    yuzey.Ucgenler.AddRange(new[] { tekil[0], tekil[2], tekil[3] });
            }

            netDxf.DxfDocument belge = null;
            try
            {
                belge = netDxf.DxfDocument.Load(dosyaYolu);
            }
            catch
            {
                // netDxf okuyamadıysa aşağıda DXFLib denenir.
            }

            // AutoCAD'den çıkan üçgen modellerde her üçgen hem 3DFACE (gerçek kotlarıyla) hem de çevresini çizen, kotu
            // 0 olan bir polyline olarak bulunabilir. 3DFACE varsa polyline'lar üçgen sayılmaz.
            if (belge != null)
            {
                var ucgenYuzuVar = belge.Entities.All.Any(v => v is netDxf.Entities.Face3D);
                foreach (var varlik in belge.Entities.All)
                {
                    switch (varlik)
                    {
                        case netDxf.Entities.Face3D yuz:
                            CokgenEkle(new[]
                            {
                                NoktaEkle(yuz.FirstVertex.X, yuz.FirstVertex.Y, yuz.FirstVertex.Z),
                                NoktaEkle(yuz.SecondVertex.X, yuz.SecondVertex.Y, yuz.SecondVertex.Z),
                                NoktaEkle(yuz.ThirdVertex.X, yuz.ThirdVertex.Y, yuz.ThirdVertex.Z),
                                NoktaEkle(yuz.FourthVertex.X, yuz.FourthVertex.Y, yuz.FourthVertex.Z)
                            });
                            break;

                        case netDxf.Entities.Polyline3D polyline:
                        {
                            // NetCAD üçgenleri kapalı (ya da ilk noktası sonda tekrarlanan) 3B polyline olarak yazar.
                            if (ucgenYuzuVar)
                                break;
                            var koseler = polyline.Vertexes.Select(v => NoktaEkle(v.X, v.Y, v.Z)).ToList();
                            if (koseler.Count >= 3 && koseler.Distinct().Count() <= 4)
                                CokgenEkle(koseler);
                            break;
                        }

                        case netDxf.Entities.PolyfaceMesh ag:
                        {
                            var agNoktalari = ag.Vertexes.Select(v => NoktaEkle(v.X, v.Y, v.Z)).ToList();
                            foreach (var agYuzu in ag.Faces)
                            {
                                // Köşe numaraları 1'den başlar; eksi değer o kenarın görünmez olduğunu belirtir.
                                var koseler = agYuzu.VertexIndexes
                                    .Select(i => Math.Abs((int)i))
                                    .Where(i => i >= 1 && i <= agNoktalari.Count)
                                    .Select(i => agNoktalari[i - 1]).ToList();
                                CokgenEkle(koseler);
                            }
                            break;
                        }
                    }
                }
                return yuzey;
            }

            // DXFLib sayıları bilgisayarın diliyle okur; ondalık nokta doğru okunsun diye geçici olarak değişmez kültüre geçilir.
            var oncekiKultur = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                var dxfLibBelge = new DXFLib.DXFDocument();
                dxfLibBelge.Load(dosyaYolu);

                var ucgenYuzuVar = dxfLibBelge.Entities.Any(v => v is DXFLib.DXF3DFace);
                foreach (DXFLib.DXFEntity varlik in dxfLibBelge.Entities)
                {
                    if (varlik is DXFLib.DXF3DFace yuz)
                    {
                        CokgenEkle(yuz.Corners
                            .Where(k => k != null && k.X.HasValue && k.Y.HasValue)
                            .Select(k => NoktaEkle(k.X.Value, k.Y.Value, k.Z ?? 0)).ToList());
                    }
                    else if (varlik is DXFLib.DXFPolyLine polyline && !ucgenYuzuVar)
                    {
                        var koseler = polyline.Children.OfType<DXFLib.DXFVertex>()
                            .Where(v => v.Location.X.HasValue && v.Location.Y.HasValue)
                            .Select(v => NoktaEkle(v.Location.X.Value, v.Location.Y.Value, v.Location.Z ?? 0)).ToList();
                        if (koseler.Count >= 3 && koseler.Distinct().Count() <= 4)
                            CokgenEkle(koseler);
                    }
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oncekiKultur;
            }

            return yuzey;
        }

        /// <summary>
        /// Nokta dosyasını (NCN ya da "nokta no, Y, X, Z" sütunlu düz metin) okur ve noktaları en yakın komşularına
        /// bağlayan üçgenlerle (Delaunay) yüzey oluşturur.
        /// </summary>
        public static Yuzey NoktaDosyasindanOlustur(string dosyaYolu)
        {
            var yuzey = new Yuzey { Ad = Path.GetFileNameWithoutExtension(dosyaYolu) };
            var ayiricilar = new[] { ' ', '\t', ';' };

            foreach (var satir in File.ReadLines(dosyaYolu, Encoding.Default))
            {
                var parcalar = satir.Split(ayiricilar, StringSplitOptions.RemoveEmptyEntries);
                if (parcalar.Length < 3 || !SayiOku(parcalar[1], out var y) || !SayiOku(parcalar[2], out var x))
                    continue;

                var z = 0.0;
                if (parcalar.Length >= 4)
                    SayiOku(parcalar[3], out z);
                yuzey.Noktalar.Add(new YuzeyNoktasi { No = parcalar[0], Y = y, X = x, Z = z });
            }

            // Üçgenleme yatay düzlemde yapılır: çizimde sağa giden eksen Y, yukarı giden eksen X'tir.
            yuzey.Ucgenler.AddRange(Delaunay.Ucgenle(
                yuzey.Noktalar.Select(n => n.Y).ToArray(),
                yuzey.Noktalar.Select(n => n.X).ToArray()));
            return yuzey;
        }

        /// <summary>Yüzeyi çizim ekranı objesine çevirir (çizimde X sağa = Y, Y yukarı = X).</summary>
        public static YuzeyNesnesi NesneyeCevir(Yuzey yuzey)
        {
            return new YuzeyNesnesi(
                yuzey.Ad,
                yuzey.Noktalar.Select(n => n.No).ToList(),
                yuzey.Noktalar.Select(n => n.Y).ToList(),
                yuzey.Noktalar.Select(n => n.X).ToList(),
                yuzey.Noktalar.Select(n => n.Z).ToList(),
                yuzey.Ucgenler.ToList());
        }

        /// <summary>Çizim ekranındaki yüzey objesinin güncel noktalarını (kaydırma vb. sonrası) yüzey modeline çevirir.</summary>
        public static Yuzey NesnedenAl(YuzeyNesnesi nesne)
        {
            // Silinmiş nokta ve üçgenler ayıklanmış güncel veri alınır.
            var nolar = new List<string>();
            var x = new List<double>();
            var y = new List<double>();
            var z = new List<double>();
            var yuzey = new Yuzey { Ad = nesne.Ad };
            nesne.CanliVeriAl(nolar, x, y, z, yuzey.Ucgenler);
            for (var i = 0; i < nolar.Count; i++)
                yuzey.Noktalar.Add(new YuzeyNoktasi { No = nolar[i], Y = x[i], X = y[i], Z = z[i] });
            return yuzey;
        }

        // Bu programın yazdığı yüzey dosyalarında ondalık ayırıcı bilgisayarın diline göre virgül olabilir.
        private static bool SayiOku(string metin, out double sayi)
        {
            return double.TryParse(metin.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out sayi);
        }

        private static string Sayi(double deger)
        {
            return deger.ToString("0.000", CultureInfo.InvariantCulture);
        }

        /// <summary>Noktaları NetCAD NCN biçiminde (nokta no, Y, X, Z) yazar.</summary>
        public static void NcnKaydet(Yuzey yuzey, string dosyaYolu)
        {
            using (var yazici = new StreamWriter(dosyaYolu, false, Encoding.Default))
            {
                foreach (var nokta in yuzey.Noktalar)
                {
                    yazici.WriteLine(
                        nokta.No.PadLeft(10) + Sayi(nokta.Y).PadLeft(13) + Sayi(nokta.X).PadLeft(14) + Sayi(nokta.Z).PadLeft(13) +
                        " 0 \"          \" \"  \" \"      \"");
                }
            }
        }

        /// <summary>Noktaları Excel (.xlsx) dosyasına "Nokta No, Y, X, Z" sütunlarıyla yazar.</summary>
        public static void ExcelKaydet(Yuzey yuzey, string dosyaYolu)
        {
            const string anaAdAlani = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            const string iliskiAdAlani = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            const string paketIliskiAdAlani = "http://schemas.openxmlformats.org/package/2006/relationships";

            using (var dosyaAkisi = new FileStream(dosyaYolu, FileMode.Create))
            using (var arsiv = new ZipArchive(dosyaAkisi, ZipArchiveMode.Create))
            {
                MetinGirdisiYaz(arsiv, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "</Types>");

                MetinGirdisiYaz(arsiv, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"" + paketIliskiAdAlani + "\">" +
                    "<Relationship Id=\"rId1\" Type=\"" + iliskiAdAlani + "/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");

                MetinGirdisiYaz(arsiv, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"" + anaAdAlani + "\" xmlns:r=\"" + iliskiAdAlani + "\">" +
                    "<sheets><sheet name=\"Noktalar\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                    "</workbook>");

                MetinGirdisiYaz(arsiv, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"" + paketIliskiAdAlani + "\">" +
                    "<Relationship Id=\"rId1\" Type=\"" + iliskiAdAlani + "/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "</Relationships>");

                var sayfa = arsiv.CreateEntry("xl/worksheets/sheet1.xml");
                using (var yazici = new StreamWriter(sayfa.Open(), new UTF8Encoding(false)))
                {
                    yazici.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    // Sütunlar geniş tutulur ki koordinatlar dar sütunda yuvarlanmış görünmesin.
                    yazici.Write("<worksheet xmlns=\"" + anaAdAlani + "\"><cols><col min=\"1\" max=\"4\" width=\"16\" customWidth=\"1\"/></cols><sheetData>");
                    yazici.Write("<row r=\"1\">" + MetinHucresi("A1", "Nokta No") + MetinHucresi("B1", "Y") + MetinHucresi("C1", "X") + MetinHucresi("D1", "Z") + "</row>");

                    var satirNo = 1;
                    foreach (var nokta in yuzey.Noktalar)
                    {
                        satirNo++;
                        // Sayısal nokta numaraları Excel'de sayı olarak sıralanabilsin diye sayı hücresi yazılır.
                        var noHucresi = long.TryParse(nokta.No, NumberStyles.None, CultureInfo.InvariantCulture, out var noSayi)
                            ? SayiHucresi("A" + satirNo, noSayi.ToString(CultureInfo.InvariantCulture))
                            : MetinHucresi("A" + satirNo, nokta.No);

                        yazici.Write("<row r=\"" + satirNo + "\">" + noHucresi +
                                     SayiHucresi("B" + satirNo, Sayi(nokta.Y)) +
                                     SayiHucresi("C" + satirNo, Sayi(nokta.X)) +
                                     SayiHucresi("D" + satirNo, Sayi(nokta.Z)) + "</row>");
                    }

                    yazici.Write("</sheetData></worksheet>");
                }
            }
        }

        private static void MetinGirdisiYaz(ZipArchive arsiv, string ad, string icerik)
        {
            using (var yazici = new StreamWriter(arsiv.CreateEntry(ad).Open(), new UTF8Encoding(false)))
                yazici.Write(icerik);
        }

        private static string MetinHucresi(string adres, string metin)
        {
            return "<c r=\"" + adres + "\" t=\"inlineStr\"><is><t>" + SecurityElement.Escape(metin) + "</t></is></c>";
        }

        private static string SayiHucresi(string adres, string sayi)
        {
            return "<c r=\"" + adres + "\"><v>" + sayi + "</v></c>";
        }
    }
}
