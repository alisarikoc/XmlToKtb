using System;
using System.Collections.Generic;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Klotoid (geçiş eğrisi/spiral) örnekleme matematiği — Fresnel serisi ile (KmdenKoorHesapla.cs'teki
    /// tek-nokta hesabının birden çok noktayı örnekleyecek şekilde uyarlanmış hali, formüller değiştirilmeden).
    /// ÖNEMLİ: Bu metot ham LandXML sırasını (Kuzey, Doğu) bekler — çizim ekranının Sağa(X)/Yukarı(Y)
    /// kuralıyla ÇAĞRIDAN ÖNCE/SONRA karşılıklı çevrilmelidir (bkz. SpiralNesnesi.OrnekNoktalariAl),
    /// aksi halde yön (rot=cw/ccw) ters çıkar.
    /// </summary>
    public static class SpiralHesaplayici
    {
        public const int VarsayilanOrnekSayisi = 20;

        public static List<PointF> NoktalariHesapla(
            double startx, double starty, double endx, double endy, double pix, double piy,
            double yaricapBaslangic, double yaricapBitis, double uzunluk, bool solaDonus,
            int ornekSayisi = VarsayilanOrnekSayisi)
        {
            var noktalar = new List<PointF>();
            if (uzunluk < 1e-6 || ornekSayisi < 1)
                return noktalar;

            var yon = solaDonus ? -1 : 1;

            if (double.IsPositiveInfinity(yaricapBaslangic))
            {
                var sKatsayi = Math.Sqrt(yaricapBitis * uzunluk);
                var uzunTegetDir = NormalizeRad(Math.Atan2(piy - starty, pix - startx));

                for (var i = 0; i <= ornekSayisi; i++)
                {
                    var mesafe = uzunluk * i / ornekSayisi;
                    var spiralY = SpiralYHesapla(mesafe, sKatsayi);
                    var spiralX = SpiralXHesapla(mesafe, sKatsayi);
                    var spiAci = Math.Atan2(spiralX, spiralY);
                    var spiS = Math.Sqrt(spiralY * spiralY + spiralX * spiralX);
                    var noktaDir = uzunTegetDir + spiAci * yon;
                    noktalar.Add(new PointF((float)(startx + spiS * Math.Cos(noktaDir)), (float)(starty + spiS * Math.Sin(noktaDir))));
                }
            }
            else if (double.IsPositiveInfinity(yaricapBitis))
            {
                var sKatsayi = Math.Sqrt(yaricapBaslangic * uzunluk);
                var uzunTegetDir = NormalizeRad(Math.Atan2(piy - endy, pix - endx));

                for (var i = 0; i <= ornekSayisi; i++)
                {
                    var mesafe = uzunluk - uzunluk * i / ornekSayisi;
                    var spiralY = SpiralYHesapla(mesafe, sKatsayi);
                    var spiralX = SpiralXHesapla(mesafe, sKatsayi);
                    var spiAci = Math.Atan2(spiralX, spiralY);
                    var spiS = Math.Sqrt(spiralY * spiralY + spiralX * spiralX);
                    var noktaDir = uzunTegetDir - spiAci * yon;
                    noktalar.Add(new PointF((float)(endx + spiS * Math.Cos(noktaDir)), (float)(endy + spiS * Math.Sin(noktaDir))));
                }
            }
            else
            {
                var endLength = uzunluk * yaricapBaslangic / (yaricapBaslangic - yaricapBitis);
                double sKatsayi;
                if (endLength < 0)
                {
                    endLength = uzunluk * yaricapBitis / (yaricapBitis - yaricapBaslangic);
                    sKatsayi = Math.Sqrt(yaricapBaslangic * endLength);
                }
                else
                {
                    sKatsayi = Math.Sqrt(yaricapBitis * endLength);
                }

                var buyuyenEnd = yaricapBitis > yaricapBaslangic;

                for (var i = 0; i <= ornekSayisi; i++)
                {
                    var mesafe = uzunluk * i / ornekSayisi;
                    var girisSpiL = sKatsayi * sKatsayi / yaricapBaslangic;
                    var cikisSpiL = sKatsayi * sKatsayi / yaricapBitis;
                    var mesafe0 = mesafe + girisSpiL;

                    if (buyuyenEnd)
                    {
                        mesafe = uzunluk - uzunluk * i / ornekSayisi;
                        girisSpiL = sKatsayi * sKatsayi / yaricapBitis;
                        cikisSpiL = sKatsayi * sKatsayi / yaricapBaslangic;
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
                    var noktaY = starty + spiS * Math.Sin(noktaDir);
                    var noktaX = startx + spiS * Math.Cos(noktaDir);
                    if (buyuyenEnd)
                    {
                        noktaDir = uzunTegetDir + sigma * yon;
                        noktaY = endy + spiS * Math.Sin(noktaDir);
                        noktaX = endx + spiS * Math.Cos(noktaDir);
                    }

                    noktalar.Add(new PointF((float)noktaX, (float)noktaY));
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

        private static double NormalizeRad(double radyan)
        {
            if (radyan < 0) radyan += 2 * Math.PI;
            return radyan;
        }
    }
}
