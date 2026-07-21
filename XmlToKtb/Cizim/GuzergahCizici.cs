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
        private const int SpiralOrnekSayisi = 20;

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
                        sonuc.AddRange(SpiralOlustur(spiral));
                        break;
                }
            }

            return sonuc;
        }

        private static CizgiNesnesi CizgiOlustur(LandXMLAlignmentsAlignmentCoordGeomLine line)
        {
            var (bx, by) = NoktaAyikla(line.Start);
            var (ex, ey) = NoktaAyikla(line.End);
            return new CizgiNesnesi { BaslangicX = bx, BaslangicY = by, BitisX = ex, BitisY = ey };
        }

        private static YayNesnesi YayOlustur(LandXMLAlignmentsAlignmentCoordGeomCurve curve)
        {
            if (string.IsNullOrWhiteSpace(curve.Center))
                return null;

            var (sx, sy) = NoktaAyikla(curve.Start);
            var (ex, ey) = NoktaAyikla(curve.End);
            var (mx, my) = NoktaAyikla(curve.Center);
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

            return new YayNesnesi { MerkezX = mx, MerkezY = my, Yaricap = yaricap, BaslangicAcisi = baslangicAcisi, BitisAcisi = bitisAcisi };
        }

        private static List<CizimNesnesi> SpiralOlustur(LandXMLAlignmentsAlignmentCoordGeomSpiral spiral)
        {
            var sonuc = new List<CizimNesnesi>();
            var length = Convert.ToDouble(spiral.length);
            if (length < 1e-6)
                return sonuc;

            var hamNoktalar = SpiralNoktalariHesapla(spiral, SpiralOrnekSayisi);
            for (var i = 0; i < hamNoktalar.Count - 1; i++)
            {
                var (kuzey1, dogu1) = hamNoktalar[i];
                var (kuzey2, dogu2) = hamNoktalar[i + 1];
                sonuc.Add(new CizgiNesnesi { BaslangicX = dogu1, BaslangicY = kuzey1, BitisX = dogu2, BitisY = kuzey2 });
            }

            return sonuc;
        }

        /// <summary>
        /// KmdenKoorHesapla.cs'teki tek-nokta klotoid hesabının, alignmentteki spiral boyunca
        /// birden çok noktayı örnekleyecek şekilde birebir (formülleri değiştirmeden) uyarlanmış hali.
        /// Dönen noktalar ham LandXML sırasındadır: (Kuzey, Doğu) — orijinal koddaki x/y adlandırmasıyla aynı.
        /// </summary>
        private static List<(double X, double Y)> SpiralNoktalariHesapla(LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, int ornekSayisi)
        {
            var noktalar = new List<(double X, double Y)>();
            var length = Convert.ToDouble(spiral.length);

            var (startx, starty) = HamNoktaAyikla(spiral.Start);
            var (endx, endy) = HamNoktaAyikla(spiral.End);
            var (pix, piy) = HamNoktaAyikla(spiral.PI);

            var yon = spiral.rot == "ccw" ? -1 : 1;

            if (spiral.radiusStart.ToUpper() == "INF")
            {
                var sKatsayi = Math.Sqrt(Convert.ToDouble(spiral.radiusEnd) * length);
                var uzunTegetDir = NormalizeRad(Math.Atan2(piy - starty, pix - startx));

                for (var i = 0; i <= ornekSayisi; i++)
                {
                    var mesafe = length * i / ornekSayisi;
                    var spiralY = SpiralYHesapla(mesafe, sKatsayi);
                    var spiralX = SpiralXHesapla(mesafe, sKatsayi);
                    var spiAci = Math.Atan2(spiralX, spiralY);
                    var spiS = Math.Sqrt(spiralY * spiralY + spiralX * spiralX);
                    var noktaDir = uzunTegetDir + spiAci * yon;
                    noktalar.Add((startx + spiS * Math.Cos(noktaDir), starty + spiS * Math.Sin(noktaDir)));
                }
            }
            else if (spiral.radiusEnd.ToUpper() == "INF")
            {
                var sKatsayi = Math.Sqrt(Convert.ToDouble(spiral.radiusStart) * length);
                var uzunTegetDir = NormalizeRad(Math.Atan2(piy - endy, pix - endx));

                for (var i = 0; i <= ornekSayisi; i++)
                {
                    var mesafe = length - length * i / ornekSayisi;
                    var spiralY = SpiralYHesapla(mesafe, sKatsayi);
                    var spiralX = SpiralXHesapla(mesafe, sKatsayi);
                    var spiAci = Math.Atan2(spiralX, spiralY);
                    var spiS = Math.Sqrt(spiralY * spiralY + spiralX * spiralX);
                    var noktaDir = uzunTegetDir - spiAci * yon;
                    noktalar.Add((endx + spiS * Math.Cos(noktaDir), endy + spiS * Math.Sin(noktaDir)));
                }
            }
            else
            {
                var radiusStart = Convert.ToDouble(spiral.radiusStart);
                var radiusEnd = Convert.ToDouble(spiral.radiusEnd);
                var endLength = length * radiusStart / (radiusStart - radiusEnd);
                double sKatsayi;
                if (endLength < 0)
                {
                    endLength = length * radiusEnd / (radiusEnd - radiusStart);
                    sKatsayi = Math.Sqrt(radiusStart * endLength);
                }
                else
                {
                    sKatsayi = Math.Sqrt(radiusEnd * endLength);
                }

                var buyuyenEnd = radiusEnd > radiusStart;

                for (var i = 0; i <= ornekSayisi; i++)
                {
                    var mesafe = length * i / ornekSayisi;
                    var girisSpiL = sKatsayi * sKatsayi / radiusStart;
                    var cikisSpiL = sKatsayi * sKatsayi / radiusEnd;
                    var mesafe0 = mesafe + girisSpiL;

                    if (buyuyenEnd)
                    {
                        mesafe = length - length * i / ornekSayisi;
                        girisSpiL = sKatsayi * sKatsayi / radiusEnd;
                        cikisSpiL = sKatsayi * sKatsayi / radiusStart;
                        mesafe0 = mesafe + girisSpiL;
                    }

                    var girisSpiY = SpiralYHesapla(girisSpiL, sKatsayi);
                    var girisSpiX = SpiralXHesapla(girisSpiL, sKatsayi);
                    var cikisSpiY = SpiralYHesapla(cikisSpiL, sKatsayi);
                    var cikisSpiX = SpiralXHesapla(cikisSpiL, sKatsayi);

                    var spiralY = SpiralYHesapla(mesafe0, sKatsayi);
                    var spiralX = SpiralXHesapla(mesafe0, sKatsayi);

                    var spiAci = Math.Atan2(spiralX - girisSpiX, spiralY - girisSpiY);
                    var spiS = Math.Sqrt(Math.Pow(spiralY - girisSpiY, 2) + Math.Pow(spiralX - girisSpiX, 2));

                    var uzunTegetDir = NormalizeRad(Math.Atan2(endy - starty, endx - startx));
                    if (buyuyenEnd)
                        uzunTegetDir = NormalizeRad(Math.Atan2(starty - endy, startx - endx));

                    var sigma = Math.Atan2(cikisSpiX - girisSpiX, cikisSpiY - girisSpiY) - spiAci;
                    var noktaDir = uzunTegetDir - sigma * yon;
                    var kmnoktaY = starty + spiS * Math.Sin(noktaDir);
                    var kmnoktaX = startx + spiS * Math.Cos(noktaDir);
                    if (buyuyenEnd)
                    {
                        noktaDir = uzunTegetDir + sigma * yon;
                        kmnoktaY = endy + spiS * Math.Sin(noktaDir);
                        kmnoktaX = endx + spiS * Math.Cos(noktaDir);
                    }

                    noktalar.Add((kmnoktaX, kmnoktaY));
                }
            }

            return noktalar;
        }

        private static double Faktoryel(double faktor)
        {
            double sonuc = 1;
            for (var i = 1; i <= faktor; i++)
                sonuc *= i;
            if (faktor == 0) sonuc = 1;
            return sonuc;
        }

        private static double SpiralXHesapla(double uzunluk, double spiralKatsayisi)
        {
            double x = 0;
            for (var n = 1; n <= 4; n++)
            {
                var faktorx = 2 * n - 1;
                x += Math.Pow(-1, n - 1) * Math.Pow(uzunluk, 4 * n - 1) /
                     ((4 * n - 1) * Faktoryel(faktorx) * Math.Pow(spiralKatsayisi, 4 * n - 2) * Math.Pow(2, 2 * n - 1));
            }
            return x;
        }

        private static double SpiralYHesapla(double uzunluk, double spiralKatsayisi)
        {
            double y = 0;
            for (var n = 1; n <= 5; n++)
            {
                var faktory = 2 * n - 2;
                y += Math.Pow(-1, n - 1) * Math.Pow(uzunluk, 4 * n - 3) /
                     ((4 * n - 3) * Faktoryel(faktory) * Math.Pow(spiralKatsayisi, 4 * n - 4) * Math.Pow(2, 2 * n - 2));
            }
            return y;
        }

        /// <summary>Ham LandXML nokta metnini ayrıştırır: dönen sıralama (Kuzey, Doğu) — dosyadaki ham sıradır.</summary>
        private static (double X, double Y) HamNoktaAyikla(string ham)
        {
            var parcalar = ham.Split();
            return (Convert.ToDouble(parcalar[0]), Convert.ToDouble(parcalar[1]));
        }

        /// <summary>Ham LandXML nokta metnini çizim ekranı kuralına göre ayrıştırır: (X=Doğu/Sağa, Y=Kuzey/Yukarı).</summary>
        private static (double X, double Y) NoktaAyikla(string ham)
        {
            var (kuzey, dogu) = HamNoktaAyikla(ham);
            return (dogu, kuzey);
        }

        private static double NormalizeRad(double radyan)
        {
            if (radyan < 0) radyan += 2 * Math.PI;
            return radyan;
        }

        private static double NormalizeAciDerece(double aciDerece)
        {
            var sonuc = aciDerece % 360.0;
            if (sonuc < 0) sonuc += 360.0;
            return sonuc;
        }
    }
}
