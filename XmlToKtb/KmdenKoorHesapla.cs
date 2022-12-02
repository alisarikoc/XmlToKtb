using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace XmlToKtb
{
    public static class KmdenKoorHesapla
    {
        public static string KoordinatHesabi(List<Array> kmList, LandXML landXml)
        {
            var koorStringBuilder = new StringBuilder();

            List<Array> koordinatliste = new List<Array>();
            var alignment = landXml.Alignments.Alignment;
            foreach (var kesit in kmList)
            {
                string satir = kesit.GetValue(0).ToString();
                if (satir == "$ENKLIST")
                {
                    continue;
                }
                if (satir == "$SON") break;


                double kilometre = Convert.ToDouble(kesit.GetValue(0));
                string nokta_no = Convert.ToString(kesit.GetValue(1));
                var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, Convert.ToDecimal(kilometre)));
                if (kilometre != null)
                {
                    var baslangicKM = alignment.staStart;
                    var guncelKM = baslangicKM;
                    foreach (var item in alignment.CoordGeom.Items)
                    {
                        decimal KesitKM;
                        string NoktaNo;
                        double NoktaY;
                        double NoktaX;
                        double NoktaKot;
                        if (kilometre < (double)guncelKM)
                        {
                            break;
                        }
                        switch (item)
                        {
                            case LandXMLAlignmentsAlignmentCoordGeomLine _:
                                {
                                    var line = (LandXMLAlignmentsAlignmentCoordGeomLine) item;
                                    var starty = (Convert.ToDouble(line.Start.Split()[1]));
                                    var startx = (Convert.ToDouble(line.Start.Split()[0]));
                                    var endy = (Convert.ToDouble(line.End.Split()[1]));
                                    var endx = (Convert.ToDouble(line.End.Split()[0]));
                                    double semt = Math.Atan2((endy - starty), (endx - startx));
                                    if (semt < 0) { semt += 2 * Math.PI; }
                                    if (kilometre > (double) guncelKM && kilometre < (double) (guncelKM + line.length))
                                    {
                                        var uzunluk = kilometre - (double)guncelKM;
                                        var kmnoktaY = starty + uzunluk * Math.Sin(semt);
                                        var kmnoktaX = startx + uzunluk * Math.Cos(semt);
                                        koorStringBuilder.AppendLine(Convert.ToString(nokta_no) + " " + Convert.ToString(kmnoktaY) + " " + Convert.ToString(kmnoktaX) + " " + Convert.ToString(kirmizikot) + " " + Convert.ToString(kilometre));
                                    }
                                    guncelKM = guncelKM + line.length;
                                    break;
                                }
                            case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                                {
                                    var curve = (LandXMLAlignmentsAlignmentCoordGeomCurve) item;
                                    var starty = (Convert.ToDouble(curve.Start.Split()[1]));
                                    var startx = (Convert.ToDouble(curve.Start.Split()[0]));
                                    var endy = (Convert.ToDouble(curve.End.Split()[1]));
                                    var endx = (Convert.ToDouble(curve.End.Split()[0]));
                                    var My = (Convert.ToDouble(curve.Center.Split()[1]));
                                    var Mx = (Convert.ToDouble(curve.Center.Split()[0]));
                                    var radius = (Convert.ToDouble(curve.radius));
                                    if (kilometre > (double) guncelKM && kilometre < (double) (guncelKM + curve.length))
                                    {
                                        var noktaYayBoyu = kilometre - (double) guncelKM;
                                        var noktaAci = noktaYayBoyu / radius ;
                                        var noktaSemt = 0.00;
                                        var semtilk = Math.Atan2((starty - My), (startx - Mx));
                                        if (semtilk < 0) { semtilk = semtilk + (2 * Math.PI); }

                                        if (curve.rot == "cw") { noktaSemt = semtilk + noktaAci ; }
                                        if (curve.rot == "ccw") { noktaSemt = semtilk - noktaAci ; }

                                        var kmnoktaY = My + radius * Math.Sin(noktaSemt);
                                        var kmnoktaX = Mx + radius * Math.Cos(noktaSemt);
                                        //koorStringBuilder.AppendLine(Convert.ToString(nokta_no) + " " + Convert.ToString(kilometre) + " " + Convert.ToString(kmnoktaY) + " " + Convert.ToString(kmnoktaX) + " " + Convert.ToString(kirmizikot));
                                        koorStringBuilder.AppendLine(Convert.ToString(nokta_no) +  " " + Convert.ToString(kmnoktaY) + " " + Convert.ToString(kmnoktaX) + " " + Convert.ToString(kirmizikot) + " " + Convert.ToString(kilometre));
                                    }

                                    guncelKM = guncelKM + curve.length;
                                    break;
                                    

                                }
                            case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                                {
                                    var Spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral) item;
                                    var Length = (Convert.ToDouble(Spiral.length));

                                    if (kilometre > (double) guncelKM &&
                                        kilometre < (double) guncelKM + Length)
                                    {
                                        
                                        var starty = (Convert.ToDouble(Spiral.Start.Split()[1]));
                                        var startx = (Convert.ToDouble(Spiral.Start.Split()[0]));
                                        var endy = (Convert.ToDouble(Spiral.End.Split()[1]));
                                        var endx = (Convert.ToDouble(Spiral.End.Split()[0]));
                                        var PIy = (Convert.ToDouble(Spiral.PI.Split()[1]));
                                        var PIx = (Convert.ToDouble(Spiral.PI.Split()[0]));
                                        double kmnoktaY = 0, kmnoktaX = 0, noktaDir = 0.00; ;

                                        var yonstring = Spiral.rot;
                                        var yon = 1;
                                        if (yonstring == "ccw") yon = -1;

                                        if (Spiral.radiusStart.ToUpper() == "INF") // GİRİŞ SPİRAL
                                        {
                                            var noktapSpirlaBoyu = kilometre - (double) guncelKM;
                                            double S_katsayi = Math.Sqrt(Convert.ToDouble(Spiral.radiusEnd) * Length);
                                            double spiralY = Spiral_Y_Hesapla(noktapSpirlaBoyu, S_katsayi);
                                            double spiralX = Spiral_X_Hesapla(noktapSpirlaBoyu, S_katsayi);
                                            var spiAci = Math.Atan(spiralX / spiralY);
                                            var spiS = Math.Sqrt((spiralY * spiralY) + (spiralX * spiralX));
                                            var uzunTeget_dir = Math.Atan2((PIy - starty), (PIx - startx)); if (uzunTeget_dir < 0) { uzunTeget_dir = uzunTeget_dir + (2 * Math.PI); }
                                            noktaDir = uzunTeget_dir + spiAci * yon;
                                            kmnoktaY = starty + spiS * Math.Sin(noktaDir);
                                            kmnoktaX = startx + spiS * Math.Cos(noktaDir);
                                        }
                                        if (Spiral.radiusEnd.ToUpper() == "INF") // ÇIKIŞ SPİRAL
                                        {
                                            var noktapSpirlaBoyu = Length - (kilometre - (double) guncelKM);
                                            double S_katsayi = Math.Sqrt(Convert.ToDouble(Spiral.radiusStart) * Length);
                                            double spiralY = Spiral_Y_Hesapla(noktapSpirlaBoyu, S_katsayi);
                                            double spiralX = Spiral_X_Hesapla(noktapSpirlaBoyu, S_katsayi);
                                            var spiAci = Math.Atan(spiralX / spiralY);
                                            var spiS = Math.Sqrt((spiralY * spiralY) + (spiralX * spiralX));
                                            var uzunTeget_dir = Math.Atan2((PIy - endy), (PIx - endx)); if (uzunTeget_dir < 0) { uzunTeget_dir = uzunTeget_dir + (2 * Math.PI); }
                                            noktaDir = uzunTeget_dir - spiAci * yon;
                                            kmnoktaY = endy + spiS * Math.Sin(noktaDir);
                                            kmnoktaX = endx + spiS * Math.Cos(noktaDir);
                                            
                                        }
                                        if (Spiral.radiusStart.ToUpper() != "INF" && Spiral.radiusEnd.ToUpper() != "INF") // CURVE SPİRAL CURVE
                                        {
                                            var EndLength = Length * Convert.ToDouble(Spiral.radiusStart) / (Convert.ToDouble(Spiral.radiusStart) - Convert.ToDouble(Spiral.radiusEnd));
                                            var startLength = Length * Convert.ToDouble(Spiral.radiusEnd) / (Convert.ToDouble(Spiral.radiusEnd) - Convert.ToDouble(Spiral.radiusStart));
                                            double S_katsayi = Math.Sqrt(Convert.ToDouble(Spiral.radiusEnd) * EndLength);
                                            if (EndLength < 0)
                                            {
                                                EndLength = Length * Convert.ToDouble(Spiral.radiusEnd) / (Convert.ToDouble(Spiral.radiusEnd) - Convert.ToDouble(Spiral.radiusStart));
                                                S_katsayi = Math.Sqrt(Convert.ToDouble(Spiral.radiusStart) * EndLength);
                                            }
                                            //var StartLength = Convert.ToDouble(Spiral.radiusEnd) * EndLength / Convert.ToDouble(Spiral.radiusStart);
                                            

                                            var noktapSpirlaBoyu =  (kilometre - (double) guncelKM);
                                            var Giris_Spi_L = S_katsayi * S_katsayi / Convert.ToDouble(Spiral.radiusStart);
                                            var Cikis_Spi_L = S_katsayi * S_katsayi / Convert.ToDouble(Spiral.radiusEnd);
                                            var noktapSpirlaBoyu_0 = noktapSpirlaBoyu + Giris_Spi_L;
                                            if (Convert.ToDouble(Spiral.radiusEnd) > Convert.ToDouble(Spiral.radiusStart))
                                            {
                                                noktapSpirlaBoyu = Length - (kilometre - (double) guncelKM);
                                                Giris_Spi_L = S_katsayi * S_katsayi / Convert.ToDouble(Spiral.radiusEnd);
                                                Cikis_Spi_L = S_katsayi * S_katsayi / Convert.ToDouble(Spiral.radiusStart);
                                                noktapSpirlaBoyu_0 = noktapSpirlaBoyu + Giris_Spi_L;
                                            }
                                            double Giris_Spi_Y = Spiral_Y_Hesapla(Giris_Spi_L, S_katsayi);
                                            double Giris_Spi_X = Spiral_X_Hesapla(Giris_Spi_L, S_katsayi);
                                            double Cikis_Spi_Y = Spiral_Y_Hesapla(Cikis_Spi_L, S_katsayi);
                                            double Cikis_Spi_X = Spiral_X_Hesapla(Cikis_Spi_L, S_katsayi);

                                            double spiralY = Spiral_Y_Hesapla(noktapSpirlaBoyu_0, S_katsayi);
                                            double spiralX = Spiral_X_Hesapla(noktapSpirlaBoyu_0, S_katsayi);

                                            var spiAci = Math.Atan((spiralX- Giris_Spi_X) / (spiralY- Giris_Spi_Y));
                                            var spiS = Math.Sqrt(((spiralY - Giris_Spi_Y) * (spiralY - Giris_Spi_Y)) + ((spiralX - Giris_Spi_X) * (spiralX - Giris_Spi_X)));
                                            var uzunTeget_dir = Math.Atan2((endy - starty), (endx - startx)); if (uzunTeget_dir < 0) { uzunTeget_dir = uzunTeget_dir + (2 * Math.PI); }
                                            if (Convert.ToDouble(Spiral.radiusEnd) > Convert.ToDouble(Spiral.radiusStart))
                                            {
                                                uzunTeget_dir = Math.Atan2((starty - endy), (startx - endx)); if (uzunTeget_dir < 0) { uzunTeget_dir = uzunTeget_dir + (2 * Math.PI); }
                                            }
                                            var sigma = Math.Atan((Cikis_Spi_X - Giris_Spi_X) / (Cikis_Spi_Y - Giris_Spi_Y))-spiAci;
                                            noktaDir = uzunTeget_dir - sigma * yon;
                                            kmnoktaY = starty + spiS * Math.Sin(noktaDir);
                                            kmnoktaX = startx + spiS * Math.Cos(noktaDir);
                                            if (Convert.ToDouble(Spiral.radiusEnd) > Convert.ToDouble(Spiral.radiusStart))
                                            {
                                                noktaDir = uzunTeget_dir + sigma * yon;
                                                kmnoktaY = endy + spiS * Math.Sin(noktaDir);
                                                kmnoktaX = endx + spiS * Math.Cos(noktaDir);
                                            }

                                        }

                                        koorStringBuilder.AppendLine(Convert.ToString(nokta_no) + " " + Convert.ToString(kmnoktaY) + " " + Convert.ToString(kmnoktaX) + " " + Convert.ToString(kirmizikot) + " " + Convert.ToString(kilometre));

                                    }


                                    guncelKM = guncelKM + Spiral.length;
                                    break;
                                }
                        }
                    }
                }

            }
            
            return koorStringBuilder.ToString();
        }
        private static double Faktoryel(double faktor)
        {

            double sonuc = 1;
            for (int i = 1; i <= faktor; i++)
            {
                sonuc *= i;
            }
            if (faktor == 0) sonuc = 1;
            return sonuc;
        }

        private static double Spiral_X_Hesapla(double L, double Sip_kat)
        {
            double X = 0;
            for (int n = 1; n <= 4; n++)
            {
                int faktorx = 2 * n - 1;
                X += Math.Pow(-1, n - 1) * Math.Pow(L, 4 * n - 1) /
                     ((4 * n - 1) * Faktoryel(faktorx) * Math.Pow(Sip_kat, 4 * n - 2) * Math.Pow(2, 2 * n - 1));
            }

            return X;
        }

        private static double Spiral_Y_Hesapla(double L, double Sip_kat)
        {
            double Y = 0;
            for (int n = 1; n <= 5; n++)
            {
                int faktory = 2 * n - 2;
                Y += Math.Pow(-1, n - 1) * Math.Pow(L, 4 * n - 3) /
                     ((4 * n - 3) * Faktoryel(faktory) * Math.Pow(Sip_kat, 4 * n - 4) * Math.Pow(2, 2 * n - 2));
            }

            return Y;
        }
    }
}
