using System;
using System.Collections.Generic;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// LandXML dosyasındaki güzergah eksenini (Alignment/CoordGeom: Line/Curve/Spiral) çizim ekranı
    /// objelerine çevirir. Ham LandXML nokta metinleri "Kuzey Doğu" (Northing Easting) sırasındadır;
    /// bu sınıf projedeki diğer yerlerle (KmdenKoorHesapla.cs vb.) aynı ayrıştırma kuralını kullanır,
    /// ama çizim ekranının Sağa(X)/Yukarı(Y) kuralına uyması için sonuçta X=Doğu, Y=Kuzey olarak eşler.
    /// </summary>
    public static class GuzergahCizici
    {
        public static List<CizimNesnesi> Olustur(LandXML landXml)
        {
            var sonuc = new List<CizimNesnesi>();
            var items = landXml?.Alignments?.Alignment?.CoordGeom?.Items;
            if (items == null)
                return sonuc;

            foreach (var item in items)
            {
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine line:
                        sonuc.Add(CizgiOlustur(line));
                        break;

                    case LandXMLAlignmentsAlignmentCoordGeomCurve curve:
                        var yay = YayOlustur(curve);
                        if (yay != null)
                            sonuc.Add(yay);
                        break;

                    case LandXMLAlignmentsAlignmentCoordGeomSpiral spiral:
                        var spiralNesne = SpiralOlustur(spiral);
                        if (spiralNesne != null)
                            sonuc.Add(spiralNesne);
                        break;
                }
            }

            return sonuc;
        }

        /// <summary>
        /// LandXML güzergah eksenini tek bir güzergah objesine çevirir. Güzergah, ilk elemanın başlangıç noktası ve
        /// doğrultusundan başlayıp elemanların uzunluk, yarıçap ve dönüş yönleriyle sırayla kurulur. Dosyada eksen
        /// yoksa null döner.
        /// </summary>
        public static GuzergahNesnesi GuzergahOlustur(LandXML landXml)
        {
            var alignment = landXml?.Alignments?.Alignment;
            var items = alignment?.CoordGeom?.Items;
            if (items == null)
                return null;

            var guzergah = new GuzergahNesnesi { Ad = alignment.name ?? "", BaslangicKm = Convert.ToDouble(alignment.staStart) };
            var ilkParca = true;

            // İlk parçanın başlangıç noktası ve doğrultusu güzergahın başlangıcıdır. Her parça ayrıca dosyadaki kendi
            // başlangıç noktasına ve doğrultusuna sabitlenir (bkz. GuzergahParcasi.Sabit).
            void ParcaEkle(GuzergahParcasi parca, double x, double y, double aci)
            {
                if (ilkParca)
                {
                    guzergah.BaslangicX = x;
                    guzergah.BaslangicY = y;
                    guzergah.BaslangicAcisi = aci;
                    ilkParca = false;
                }

                parca.Sabit = true;
                parca.SabitX = x;
                parca.SabitY = y;
                parca.SabitAci = aci;
                guzergah.Parcalar.Add(parca);
            }

            foreach (var item in items)
            {
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine line:
                    {
                        var (bx, by, _) = NoktaAyikla(line.Start);
                        var (ex, ey, _) = NoktaAyikla(line.End);
                        var uzunluk = Convert.ToDouble(line.length);
                        if (uzunluk <= 0)
                            uzunluk = Math.Sqrt((ex - bx) * (ex - bx) + (ey - by) * (ey - by));
                        if (uzunluk <= 0)
                            break;

                        ParcaEkle(new GuzergahParcasi { Tip = GuzergahParcaTipi.Dogru, Uzunluk = uzunluk }, bx, by, Math.Atan2(ey - by, ex - bx));
                        break;
                    }

                    case LandXMLAlignmentsAlignmentCoordGeomCurve curve:
                    {
                        if (string.IsNullOrWhiteSpace(curve.Center))
                            break;

                        var (sx, sy, _) = NoktaAyikla(curve.Start);
                        var (ex, ey, _) = NoktaAyikla(curve.End);
                        var (mx, my, _) = NoktaAyikla(curve.Center);
                        var yaricap = Convert.ToDouble(curve.radius);
                        var sola = curve.rot == "ccw";
                        if (yaricap <= 0)
                            break;

                        var uzunluk = Convert.ToDouble(curve.length);
                        if (uzunluk <= 0)
                        {
                            // Uzunluk verilmemişse merkezden görülen açıdan hesaplanır.
                            var supurme = Math.Atan2(ey - my, ex - mx) - Math.Atan2(sy - my, sx - mx);
                            if (!sola)
                                supurme = -supurme;
                            supurme %= 2 * Math.PI;
                            if (supurme <= 0)
                                supurme += 2 * Math.PI;
                            uzunluk = yaricap * supurme;
                        }

                        // Kurbun başındaki gidiş doğrultusu yarıçapa diktir: sola dönüşte yarıçap +90°, sağa dönüşte -90° çevrilir.
                        var yaricapAcisi = Math.Atan2(sy - my, sx - mx);
                        ParcaEkle(new GuzergahParcasi
                        {
                            Tip = GuzergahParcaTipi.Kurp,
                            Uzunluk = uzunluk,
                            YaricapBaslangic = yaricap,
                            YaricapBitis = yaricap,
                            Yon = sola ? DonusYonu.Sol : DonusYonu.Sag
                        }, sx, sy, yaricapAcisi + (sola ? Math.PI / 2 : -Math.PI / 2));
                        break;
                    }

                    case LandXMLAlignmentsAlignmentCoordGeomSpiral spiral:
                    {
                        var uzunluk = Convert.ToDouble(spiral.length);
                        if (uzunluk < 1e-6)
                            break;

                        var (bx, by, _) = NoktaAyikla(spiral.Start);
                        var (px, py, _) = NoktaAyikla(spiral.PI);

                        // Spiralin başındaki gidiş doğrultusu, başlangıçtan PI (teğetlerin kesişimi) noktasına doğrudur.
                        ParcaEkle(new GuzergahParcasi
                        {
                            Tip = GuzergahParcaTipi.Spiral,
                            Uzunluk = uzunluk,
                            YaricapBaslangic = spiral.radiusStart.ToUpper() == "INF" ? double.PositiveInfinity : Convert.ToDouble(spiral.radiusStart),
                            YaricapBitis = spiral.radiusEnd.ToUpper() == "INF" ? double.PositiveInfinity : Convert.ToDouble(spiral.radiusEnd),
                            Yon = spiral.rot == "ccw" ? DonusYonu.Sol : DonusYonu.Sag
                        }, bx, by, Math.Atan2(py - by, px - bx));
                        break;
                    }
                }
            }

            DuseyiOku(alignment, guzergah);
            guzergah.Dever = DeverTablosu.GuzergahtanOku(alignment);

            guzergah.Hesapla();
            return guzergah;
        }

        // LandXML düşey geometrisini (Profile/ProfAlign: PVI, ParaCurve, CircCurve) güzergahın some listesine okur.
        private static void DuseyiOku(LandXMLAlignmentsAlignment alignment, GuzergahNesnesi guzergah)
        {
            var items = alignment.Profile?.ProfAlign?.Items;
            if (items == null)
                return;

            // Elemanın metni "kilometre kot" biçimindedir.
            bool KmKotAyikla(string metin, out double km, out double kot)
            {
                km = kot = 0;
                var parcalar = (metin ?? "").Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return parcalar.Length >= 2
                       && double.TryParse(parcalar[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out km)
                       && double.TryParse(parcalar[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out kot);
            }

            foreach (var item in items)
            {
                DuseyNokta nokta = null;
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentProfileProfAlignParaCurve parabol:
                        if (KmKotAyikla(parabol.Value, out var pKm, out var pKot))
                            nokta = new DuseyNokta { Km = pKm, Kot = pKot, Tip = DuseyKurpTipi.Parabol, Uzunluk = Convert.ToDouble(parabol.length) };
                        break;
                    case LandXMLAlignmentsAlignmentProfileProfAlignCircCurve daire:
                        if (KmKotAyikla(daire.Value, out var dKm, out var dKot))
                            nokta = new DuseyNokta { Km = dKm, Kot = dKot, Tip = DuseyKurpTipi.Daire, Uzunluk = Convert.ToDouble(daire.length), Yaricap = Convert.ToDouble(daire.radius) };
                        break;
                    case LandXMLAlignmentsAlignmentProfileProfAlignPVI pvi:
                        if (KmKotAyikla(pvi.Value, out var vKm, out var vKot))
                            nokta = new DuseyNokta { Km = vKm, Kot = vKot };
                        break;
                    case string metin:
                        if (KmKotAyikla(metin, out var sKm, out var sKot))
                            nokta = new DuseyNokta { Km = sKm, Kot = sKot };
                        break;
                }

                if (nokta == null)
                    continue;
                // Boyu sıfır olan kurp, kurpsuz some demektir.
                if (nokta.Uzunluk <= 0)
                    nokta.Tip = DuseyKurpTipi.Yok;
                guzergah.Dusey.Add(nokta);
            }
        }

        // Güzergahın some listesini LandXML düşey geometrisine çevirir; some yoksa null döner.
        private static LandXMLAlignmentsAlignmentProfile DuseyiYaz(GuzergahNesnesi guzergah)
        {
            if (guzergah.Dusey.Count == 0)
                return null;

            string Sayi(double deger)
            {
                return deger.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture);
            }

            var elemanlar = new List<object>();
            for (var i = 0; i < guzergah.Dusey.Count; i++)
            {
                var nokta = guzergah.Dusey[i];
                var metin = Sayi(nokta.Km) + " " + Sayi(nokta.Kot);
                var kurpVar = i > 0 && i < guzergah.Dusey.Count - 1 && nokta.Uzunluk > 0;

                if (kurpVar && nokta.Tip == DuseyKurpTipi.Daire)
                    elemanlar.Add(new LandXMLAlignmentsAlignmentProfileProfAlignCircCurve { length = (decimal)nokta.Uzunluk, radius = (decimal)nokta.Yaricap, Value = metin });
                else if (kurpVar && nokta.Tip == DuseyKurpTipi.Parabol)
                    elemanlar.Add(new LandXMLAlignmentsAlignmentProfileProfAlignParaCurve { length = (decimal)nokta.Uzunluk, Value = metin });
                else
                    elemanlar.Add(new LandXMLAlignmentsAlignmentProfileProfAlignPVI { Value = metin });
            }

            return new LandXMLAlignmentsAlignmentProfile
            {
                name = guzergah.Ad ?? string.Empty,
                ProfAlign = new LandXMLAlignmentsAlignmentProfileProfAlign { name = guzergah.Ad ?? string.Empty, Items = elemanlar.ToArray() }
            };
        }

        // Güzergahın dever tablosunu LandXML dever (Cant) bölümüne çevirir; dever yoksa null döner.
        private static LandXMLAlignmentsAlignmentCant DeveriYaz(GuzergahNesnesi guzergah)
        {
            if (guzergah.Dever.Satirlar.Count == 0)
                return null;

            var istasyonlar = new List<LandXMLAlignmentsAlignmentCantCantStation>();
            foreach (var satir in guzergah.Dever.Satirlar)
            {
                istasyonlar.Add(new LandXMLAlignmentsAlignmentCantCantStation
                {
                    station = (decimal)satir.Km,
                    appliedCant = (decimal)satir.Dever,
                    curvature = satir.Yon == "ccw" ? "ccw" : "cw"
                });
            }

            return new LandXMLAlignmentsAlignmentCant
            {
                name = guzergah.Ad ?? string.Empty,
                gauge = (decimal)guzergah.Dever.Ekartman,
                rotationPoint = "insideRail",
                CantStation = istasyonlar.ToArray()
            };
        }

        /// <summary>
        /// Güzergah objesini LandXML'e çevirir: yatay geometri (doğru, kurp, spiral), varsa düşey geometri ve dever.
        /// Nokta metinleri LandXML sırasıyla "Kuzey Doğu" olarak yazılır.
        /// </summary>
        public static LandXML LandXmlOlustur(GuzergahNesnesi guzergah)
        {
            guzergah.Hesapla();

            string Nokta(double x, double y)
            {
                return y.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture) + " " +
                       x.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture);
            }

            // LandXML'de doğrultular +X'ten (doğu) saat yönünün tersine, derece olarak yazılır.
            decimal Derece(double radyan)
            {
                var derece = radyan * 180.0 / Math.PI % 360.0;
                return (decimal)(derece < 0 ? derece + 360.0 : derece);
            }

            // Parçanın başındaki ve sonundaki teğetlerin kesiştiği nokta (PI); teğetler paralelse parçanın ortası.
            double[] TegetKesisimi(GuzergahParcasi p)
            {
                double d0x = Math.Cos(p.BaslangicAcisi), d0y = Math.Sin(p.BaslangicAcisi), d1x = Math.Cos(p.BitisAcisi), d1y = Math.Sin(p.BitisAcisi);
                var payda = d0x * d1y - d0y * d1x;
                if (Math.Abs(payda) < 1e-12)
                    return new[] { (p.BaslangicX + p.BitisX) / 2, (p.BaslangicY + p.BitisY) / 2 };
                var t = ((p.BitisX - p.BaslangicX) * d1y - (p.BitisY - p.BaslangicY) * d1x) / payda;
                return new[] { p.BaslangicX + t * d0x, p.BaslangicY + t * d0y };
            }

            double Uzaklik(double x1, double y1, double x2, double y2)
            {
                return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            }

            var elemanlar = new List<object>();
            foreach (var p in guzergah.Parcalar)
            {
                var rot = p.Yon == DonusYonu.Sol ? "ccw" : "cw";
                if (p.Tip == GuzergahParcaTipi.Dogru)
                {
                    elemanlar.Add(new LandXMLAlignmentsAlignmentCoordGeomLine
                    {
                        Start = Nokta(p.BaslangicX, p.BaslangicY),
                        End = Nokta(p.BitisX, p.BitisY),
                        dir = Derece(p.BaslangicAcisi),
                        length = (decimal)p.Uzunluk
                    });
                }
                else if (p.Tip == GuzergahParcaTipi.Kurp)
                {
                    var yaricap = p.YaricapBaslangic;
                    var delta = p.Uzunluk / yaricap; // merkez açısı (radyan)
                    // Merkez, gidiş doğrultusunun dönüş tarafındaki dikmesi üzerinde yarıçap kadar ötededir.
                    var dikme = p.BaslangicAcisi + (p.Yon == DonusYonu.Sol ? Math.PI / 2 : -Math.PI / 2);
                    var pi = TegetKesisimi(p);
                    var yariAci = delta / 2;
                    elemanlar.Add(new LandXMLAlignmentsAlignmentCoordGeomCurve
                    {
                        rot = rot,
                        crvType = "arc",
                        radius = (decimal)yaricap,
                        length = (decimal)p.Uzunluk,
                        delta = (decimal)(delta * 180.0 / Math.PI),
                        chord = (decimal)(2 * yaricap * Math.Sin(yariAci)),
                        tangent = (decimal)(yaricap * Math.Tan(Math.Min(yariAci, Math.PI / 2 - 1e-6))),
                        external = (decimal)(yaricap * (1 / Math.Cos(Math.Min(yariAci, Math.PI / 2 - 1e-6)) - 1)),
                        midOrd = (decimal)(yaricap * (1 - Math.Cos(yariAci))),
                        dirStart = Derece(p.BaslangicAcisi),
                        dirEnd = Derece(p.BitisAcisi),
                        Start = Nokta(p.BaslangicX, p.BaslangicY),
                        Center = Nokta(p.BaslangicX + yaricap * Math.Cos(dikme), p.BaslangicY + yaricap * Math.Sin(dikme)),
                        End = Nokta(p.BitisX, p.BitisY),
                        PI = Nokta(pi[0], pi[1])
                    });
                }
                else
                {
                    var pi = TegetKesisimi(p);
                    // Bitiş noktasının, başlangıç teğetine göre boyuna (totalX) ve enine (totalY) uzaklığı.
                    double dx = p.BitisX - p.BaslangicX, dy = p.BitisY - p.BaslangicY;
                    var boyuna = dx * Math.Cos(p.BaslangicAcisi) + dy * Math.Sin(p.BaslangicAcisi);
                    var enine = Math.Abs(-dx * Math.Sin(p.BaslangicAcisi) + dy * Math.Cos(p.BaslangicAcisi));
                    var tegetBas = Uzaklik(p.BaslangicX, p.BaslangicY, pi[0], pi[1]);
                    var tegetBit = Uzaklik(pi[0], pi[1], p.BitisX, p.BitisY);
                    elemanlar.Add(new LandXMLAlignmentsAlignmentCoordGeomSpiral
                    {
                        length = (decimal)p.Uzunluk,
                        radiusStart = double.IsInfinity(p.YaricapBaslangic) ? "INF" : p.YaricapBaslangic.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture),
                        radiusEnd = double.IsInfinity(p.YaricapBitis) ? "INF" : p.YaricapBitis.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture),
                        rot = rot,
                        spiType = "clothoid",
                        theta = (decimal)(Math.Abs(p.BitisAcisi - p.BaslangicAcisi) * 180.0 / Math.PI),
                        totalX = (decimal)boyuna,
                        totalY = (decimal)enine,
                        tanLong = (decimal)Math.Max(tegetBas, tegetBit),
                        tanShort = (decimal)Math.Min(tegetBas, tegetBit),
                        Start = Nokta(p.BaslangicX, p.BaslangicY),
                        PI = Nokta(pi[0], pi[1]),
                        End = Nokta(p.BitisX, p.BitisY)
                    });
                }
            }

            return new LandXML
            {
                Units = new LandXMLUnits
                {
                    Metric = new LandXMLUnitsMetric
                    {
                        areaUnit = "squareMeter",
                        linearUnit = "meter",
                        volumeUnit = "cubicMeter",
                        temperatureUnit = "celsius",
                        pressureUnit = "milliBars",
                        diameterUnit = "millimeter",
                        angularUnit = "decimal degrees",
                        directionUnit = "decimal degrees"
                    }
                },
                Alignments = new LandXMLAlignments
                {
                    name = string.Empty,
                    Alignment = new LandXMLAlignmentsAlignment
                    {
                        name = guzergah.Ad ?? string.Empty,
                        desc = string.Empty,
                        length = (decimal)guzergah.ToplamUzunluk,
                        staStart = (decimal)guzergah.BaslangicKm,
                        CoordGeom = new LandXMLAlignmentsAlignmentCoordGeom { Items = elemanlar.ToArray() },
                        Profile = DuseyiYaz(guzergah),
                        Cant = DeveriYaz(guzergah)
                    }
                }
            };
        }

        private static CizgiNesnesi CizgiOlustur(LandXMLAlignmentsAlignmentCoordGeomLine line)
        {
            var (bx, by, bz) = NoktaAyikla(line.Start);
            var (ex, ey, ez) = NoktaAyikla(line.End);
            return new CizgiNesnesi { BaslangicX = bx, BaslangicY = by, BaslangicZ = bz, BitisX = ex, BitisY = ey, BitisZ = ez };
        }

        private static YayNesnesi YayOlustur(LandXMLAlignmentsAlignmentCoordGeomCurve curve)
        {
            if (string.IsNullOrWhiteSpace(curve.Center))
                return null;

            var (sx, sy, _) = NoktaAyikla(curve.Start);
            var (ex, ey, _) = NoktaAyikla(curve.End);
            var (mx, my, mz) = NoktaAyikla(curve.Center);
            var yaricap = Convert.ToDouble(curve.radius);

            var baslangicAcisi = NormalizeAciDerece(Math.Atan2(sy - my, sx - mx) * 180.0 / Math.PI);
            var bitisAcisi = NormalizeAciDerece(Math.Atan2(ey - my, ex - mx) * 180.0 / Math.PI);

            // Yay nesnemiz BaslangicAcisi->BitisAcisi'ni her zaman CCW süpürür; fiziksel yön "cw" ise
            // aynı fiziksel yayı temsil etmek için açıları yer değiştiriyoruz.
            if (curve.rot == "cw")
            {
                var geciciAci = baslangicAcisi;
                baslangicAcisi = bitisAcisi;
                bitisAcisi = geciciAci;
            }

            return new YayNesnesi { MerkezX = mx, MerkezY = my, MerkezZ = mz, Yaricap = yaricap, BaslangicAcisi = baslangicAcisi, BitisAcisi = bitisAcisi };
        }

        private static SpiralNesnesi SpiralOlustur(LandXMLAlignmentsAlignmentCoordGeomSpiral spiral)
        {
            var length = Convert.ToDouble(spiral.length);
            if (length < 1e-6)
                return null;

            var (bx, by, bz) = NoktaAyikla(spiral.Start);
            var (ex, ey, ez) = NoktaAyikla(spiral.End);
            var (px, py, pz) = NoktaAyikla(spiral.PI);

            var yaricapBaslangic = spiral.radiusStart.ToUpper() == "INF" ? double.PositiveInfinity : Convert.ToDouble(spiral.radiusStart);
            var yaricapBitis = spiral.radiusEnd.ToUpper() == "INF" ? double.PositiveInfinity : Convert.ToDouble(spiral.radiusEnd);

            return new SpiralNesnesi
            {
                BaslangicX = bx,
                BaslangicY = by,
                BaslangicZ = bz,
                BitisX = ex,
                BitisY = ey,
                BitisZ = ez,
                PiX = px,
                PiY = py,
                PiZ = pz,
                YaricapBaslangic = yaricapBaslangic,
                YaricapBitis = yaricapBitis,
                Uzunluk = length,
                Yon = spiral.rot == "ccw" ? DonusYonu.Sol : DonusYonu.Sag
            };
        }

        /// <summary>Ham LandXML nokta metnini ayrıştırır: dönen sıralama (Kuzey, Doğu, Z) — Z (kot) yoksa 0 kabul edilir.</summary>
        private static (double X, double Y, double Z) HamNoktaAyikla(string ham)
        {
            var parcalar = ham.Split();
            var z = parcalar.Length >= 3 ? Convert.ToDouble(parcalar[2]) : 0.0;
            return (Convert.ToDouble(parcalar[0]), Convert.ToDouble(parcalar[1]), z);
        }

        /// <summary>Ham LandXML nokta metnini çizim ekranı kuralına göre ayrıştırır: (X=Doğu/Sağa, Y=Kuzey/Yukarı, Z=Kot).</summary>
        private static (double X, double Y, double Z) NoktaAyikla(string ham)
        {
            var (kuzey, dogu, z) = HamNoktaAyikla(ham);
            return (dogu, kuzey, z);
        }

        private static double NormalizeAciDerece(double aciDerece)
        {
            var sonuc = aciDerece % 360.0;
            if (sonuc < 0) sonuc += 360.0;
            return sonuc;
        }
    }
}
