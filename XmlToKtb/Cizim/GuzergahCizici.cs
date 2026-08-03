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
