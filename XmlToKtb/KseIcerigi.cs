using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Common;

namespace XmlToKtb
{
    public static class KSEIcerigi
    {
        public static List<Array> KseGetir(LandXML landXml, List<Array> ncndata)
        {
            var alignment = landXml.Alignments.Alignment;
            //var kse = new StringBuilder("2  @=COLOR=SECTION=DISPLAY=-12796356").AppendLine();
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;
            List<Array> kesitliste = new List<Array>();
            double KesitMES = 0;

            foreach (var item in alignment.CoordGeom.Items)
            {
                decimal KesitKM;
                string NoktaNo;
                double NoktaY;
                double NoktaX;
                double NoktaKot;
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine _:
                    {
                        var line = (LandXMLAlignmentsAlignmentCoordGeomLine) item;
                        var starty = (Convert.ToDouble(line.Start.Split()[1]));
                        var startx = (Convert.ToDouble(line.Start.Split()[0]));
                        var endy = (Convert.ToDouble(line.End.Split()[1]));
                        var endx = (Convert.ToDouble(line.End.Split()[0]));
                        foreach (var ncn in ncndata)
                        {
                            NoktaNo = (Convert.ToString(ncn.GetValue(0)));
                            NoktaY = (Convert.ToDouble(ncn.GetValue(1)));
                            NoktaX = (Convert.ToDouble(ncn.GetValue(2)));
                            NoktaKot = (Convert.ToDouble(ncn.GetValue(3)));
                            var dikboy = (endx - startx) /
                                         Math.Sqrt(Math.Pow((endx - startx), 2) + Math.Pow((endy - starty), 2)) *
                                         (NoktaX - startx) +
                                         (endy - starty) /
                                         Math.Sqrt(Math.Pow((endx - startx), 2) + Math.Pow((endy - starty), 2)) *
                                         (NoktaY - starty);
                            var dikayak = -(endy - starty) /
                                          Math.Sqrt(Math.Pow((endx - startx), 2) + Math.Pow((endy - starty), 2)) *
                                          (NoktaX - startx) +
                                          (endx - startx) /
                                          Math.Sqrt(Math.Pow((endx - startx), 2) + Math.Pow((endy - starty), 2)) *
                                          (NoktaY - starty);
                            var dikmesafesi = Math.Sqrt(Math.Pow((starty - endy), 2) + Math.Pow((startx - endx), 2));
                            if (dikboy > 0 && (dikboy - dikmesafesi) < 0 && Math.Abs(dikayak) < 50)
                            {

                                KesitMES = dikayak;
                                KesitKM = guncelKM + (Convert.ToDecimal(dikboy));
                                var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                object[] array = {KesitKM, KesitMES, NoktaKot, NoktaNo, kirmizikot, 0};
                                kesitliste.Add(array);
                            }
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

                        foreach (var ncnArray in ncndata)
                        {
                            NoktaNo = (Convert.ToString(ncnArray.GetValue(0)));
                            NoktaY = (Convert.ToDouble(ncnArray.GetValue(1)));
                            NoktaX = (Convert.ToDouble(ncnArray.GetValue(2)));
                            NoktaKot = (Convert.ToDouble(ncnArray.GetValue(3)));

                            var semtilk = Math.Atan2((starty - My), (startx - Mx));
                            if (semtilk < 0)
                            {
                                semtilk = semtilk + (2 * Math.PI);
                            }

                            var semtnokta = Math.Atan2((NoktaY - My), (NoktaX - Mx));
                            if (semtnokta < 0)
                            {
                                semtnokta = semtnokta + (2 * Math.PI);
                            }

                            KesitMES = (Convert.ToDouble(curve.radius)) -
                                       Math.Sqrt(Math.Pow((My - NoktaY), 2) + Math.Pow((Mx - NoktaX), 2));
                            var kurp_uzeri_Y = My + Math.Sin(semtnokta) * radius;
                            var kurp_uzeri_X = Mx + Math.Cos(semtnokta) * radius;
                            var dikboy = (endx - startx) /
                                         Math.Sqrt(Math.Pow((endx - startx), 2) + Math.Pow((endy - starty), 2)) *
                                         (kurp_uzeri_X - startx) +
                                         (endy - starty) /
                                         Math.Sqrt(Math.Pow((endx - startx), 2) + Math.Pow((endy - starty), 2)) *
                                         (kurp_uzeri_Y - starty);
                            var dikmesafesi = Math.Sqrt(Math.Pow((endy - starty), 2) + Math.Pow((endx - startx), 2));



                            if (curve.rot == "cw" && dikboy > 0 && dikboy < dikmesafesi && Math.Abs(KesitMES) < 50)

                            {
                                var aci = semtnokta - semtilk;
                                if (aci < 0)
                                {
                                    aci = aci + Math.PI * 2;
                                }

                                KesitKM = guncelKM + (Convert.ToDecimal(aci * radius));
                                var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                object[] array =
                                    {KesitKM, KesitMES, NoktaKot, NoktaNo, kirmizikot, sapmaHesabi(radius)};
                                kesitliste.Add(array);
                            }

                            if (curve.rot == "ccw" && dikboy > 0 && dikboy < dikmesafesi && Math.Abs(KesitMES) < 50)
                            {
                                var aci = semtilk - semtnokta;
                                if (aci < 0)
                                {
                                    aci = aci + Math.PI * 2;
                                }

                                KesitMES = KesitMES * -1;
                                KesitKM = guncelKM + (Convert.ToDecimal(aci * radius));
                                var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                object[] array =
                                {
                                    KesitKM, KesitMES, NoktaKot, NoktaNo, kirmizikot, sapmaHesabi(radius) * -1
                                };
                                kesitliste.Add(array);
                            }
                        }

                        guncelKM = guncelKM + curve.length;
                        break;
                    }
                    case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                    {
                        var Spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral) item;
                        var starty = (Convert.ToDouble(Spiral.Start.Split()[1]));
                        var startx = (Convert.ToDouble(Spiral.Start.Split()[0]));
                        var endy = (Convert.ToDouble(Spiral.End.Split()[1]));
                        var endx = (Convert.ToDouble(Spiral.End.Split()[0]));
                        var PIy = (Convert.ToDouble(Spiral.PI.Split()[1]));
                        var PIx = (Convert.ToDouble(Spiral.PI.Split()[0]));

                        var yon = Spiral.rot;
                        var Length = (Convert.ToDouble(Spiral.length));

                        if (Spiral.radiusStart.ToUpper() == "INF") // GİRİŞ SPİRAL
                        {
                            var semtshort = Math.Atan2((PIy - starty), (PIx - startx));
                            if (semtshort < 0)
                            {
                                semtshort = semtshort + (2 * Math.PI);
                            }

                            foreach (var ncnArray in ncndata)
                            {
                                NoktaNo = (Convert.ToString(ncnArray.GetValue(0)));
                                NoktaY = (Convert.ToDouble(ncnArray.GetValue(1)));
                                NoktaX = (Convert.ToDouble(ncnArray.GetValue(2)));
                                NoktaKot = (Convert.ToDouble(ncnArray.GetValue(3)));
                                var dikboy = (PIx - startx) /
                                             Math.Sqrt(Math.Pow((PIx - startx), 2) + Math.Pow((PIy - starty), 2)) *
                                             (NoktaX - startx) +
                                             (PIy - starty) /
                                             Math.Sqrt(Math.Pow((PIx - startx), 2) + Math.Pow((PIy - starty), 2)) *
                                             (NoktaY - starty);
                                var dikayak = -(PIy - starty) /
                                              Math.Sqrt(Math.Pow((PIx - startx), 2) + Math.Pow((PIy - starty), 2)) *
                                              (NoktaX - startx) +
                                              (PIx - startx) /
                                              Math.Sqrt(Math.Pow((PIx - startx), 2) + Math.Pow((PIy - starty), 2)) *
                                              (NoktaY - starty);
                                var dikboyKisa =
                                    (PIx - endx) / Math.Sqrt(Math.Pow((PIx - endx), 2) + Math.Pow((PIy - endy), 2)) *
                                    (NoktaX - endx) +
                                    (PIy - endy) / Math.Sqrt(Math.Pow((PIx - endx), 2) + Math.Pow((PIy - endy), 2)) *
                                    (NoktaY - endy);

                                var aKare = Convert.ToDouble(Spiral.radiusEnd) * Length;
                                if (dikboy > 0 && dikboyKisa > 0 && Math.Abs(dikayak) < 50)
                                {

                                    var L_point = ofset_L_hesapla(Convert.ToDouble(Spiral.radiusEnd), Length, dikboy,
                                        dikayak);
                                    if (yon == "cw")
                                        L_point = ofset_L_hesapla(Convert.ToDouble(Spiral.radiusEnd), Length, dikboy,
                                            -dikayak);
                                    if (L_point > Length) break;
                                    var R_point = aKare / L_point;
                                    if (R_point >= Convert.ToDouble(Spiral.radiusEnd))
                                    {
                                        if (yon == "cw")
                                        {
                                            KesitMES = (-Spiral_X_Hesapla(L_point, Math.Sqrt(aKare)) + dikayak) /
                                                       Math.Cos(L_point * L_point / 2 / aKare);
                                        }

                                        if (yon == "ccw")
                                        {
                                            KesitMES = (Spiral_X_Hesapla(L_point, Math.Sqrt(aKare)) + dikayak) /
                                                       Math.Cos(L_point * L_point / 2 / aKare);
                                        }

                                        KesitKM = guncelKM + (Convert.ToDecimal(L_point)); //KESİT DEĞERİ BURDA
                                        var EksenKaydir = sapmaHesabi(R_point);
                                        if (yon == "ccw") EksenKaydir = EksenKaydir * -1;
                                        var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));

                                        object[] array =
                                            {KesitKM, KesitMES, NoktaKot, NoktaNo, kirmizikot, EksenKaydir};
                                        kesitliste.Add(array);
                                    }
                                }
                            }
                        }

                        if (Spiral.radiusEnd.ToUpper() == "INF") // ÇIKIŞ SPİRAL
                        {
                            foreach (var ncn in ncndata)
                            {
                                NoktaNo = (Convert.ToString(ncn.GetValue(0)));
                                NoktaY = (Convert.ToDouble(ncn.GetValue(1)));
                                NoktaX = (Convert.ToDouble(ncn.GetValue(2)));
                                NoktaKot = (Convert.ToDouble(ncn.GetValue(3)));
                                var dikboy = (PIx - endx) /
                                             Math.Sqrt(Math.Pow((PIx - endx), 2) + Math.Pow((PIy - endy), 2)) *
                                             (NoktaX - endx) +
                                             (PIy - endy) /
                                             Math.Sqrt(Math.Pow((PIx - endx), 2) + Math.Pow((PIy - endy), 2)) *
                                             (NoktaY - endy);
                                var dikayak = -(PIy - endy) /
                                              Math.Sqrt(Math.Pow((PIx - endx), 2) + Math.Pow((PIy - endy), 2)) *
                                              (NoktaX - endx) +
                                              (PIx - endx) /
                                              Math.Sqrt(Math.Pow((PIx - endx), 2) + Math.Pow((PIy - endy), 2)) *
                                              (NoktaY - endy);

                                var dikboyKisa =
                                    (PIx - startx) /
                                    Math.Sqrt(Math.Pow((PIx - startx), 2) + Math.Pow((PIy - starty), 2)) *
                                    (NoktaX - startx) +
                                    (PIy - starty) /
                                    Math.Sqrt(Math.Pow((PIx - startx), 2) + Math.Pow((PIy - starty), 2)) *
                                    (NoktaY - starty);

                                var aKare = Convert.ToDouble(Spiral.radiusStart) * Length;

                                if (dikboy > 0 && dikboyKisa > 0 && Math.Abs(dikayak) < 50)
                                {
                                    double L_point = ofset_L_hesapla(Convert.ToDouble(Spiral.radiusStart), Length,
                                        dikboy, dikayak);
                                    if (yon == "ccw")
                                        L_point = ofset_L_hesapla(Convert.ToDouble(Spiral.radiusStart), Length, dikboy,
                                            -dikayak);
                                    if (L_point > Length) break;
                                    var R_point = aKare / L_point;
                                    if (R_point >= Convert.ToDouble(Spiral.radiusStart))
                                    {
                                        if (yon == "cw")
                                        {
                                            KesitMES = (-Spiral_X_Hesapla(L_point, Math.Sqrt(aKare)) - dikayak) /
                                                       Math.Cos(L_point * L_point / 2 / aKare);
                                        }

                                        if (yon == "ccw")
                                        {
                                            KesitMES = (Spiral_X_Hesapla(L_point, Math.Sqrt(aKare)) - dikayak) /
                                                       Math.Cos(L_point * L_point / 2 / aKare);
                                        }

                                        KesitKM = guncelKM + (Convert.ToDecimal(Length - L_point)); //KESİT DEĞERİ BURDA
                                        var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                        var EksenKaydir = sapmaHesabi(R_point);
                                        if (yon == "ccw") EksenKaydir = EksenKaydir * -1;
                                        object[] array =
                                            {KesitKM, KesitMES, NoktaKot, NoktaNo, kirmizikot, EksenKaydir};
                                        kesitliste.Add(array);
                                    }
                                }
                            }
                        }

                        guncelKM = guncelKM + Spiral.length;
                        break;
                    }
                }
            }

            kesitliste = kesitliste.OrderBy(x => x.GetValue(0)).ToList();

            //for (int i = 0; i < kesitliste.Count - 1; i++)
            //{
            //    for (int j = i; j < kesitliste.Count; j++)
            //    {
            //        // >(büyük) işareti <(küçük ) olarak değiştirilirse büyükten küçüğe sıralanır
            //        if ((Convert.ToDecimal(kesitliste[i].GetValue(0))) > (Convert.ToDecimal(kesitliste[j].GetValue(0))))
            //        {
            //            var gecici = kesitliste[j];
            //            kesitliste[j] = kesitliste[i];
            //            kesitliste[i] = gecici;
            //        }
            //    }
            //}

            var k = 0;
            var c = 0;
            decimal toplam = 0;
            decimal Kes_KMSON = 0;
            List<Array> kseliste = new List<Array>();
            List<Array> aranoktaliste = new List<Array>();
            for (int i = 0; i < kesitliste.Count - 1; i++)
            {
                var Kes_No = (Convert.ToString(kesitliste[i].GetValue(3)));
                var Kes_KM = (Convert.ToDecimal(kesitliste[i].GetValue(0)));
                var Kes_Mes = kesitliste[i].GetValue(1);
                var Kes_Kot = (Convert.ToDecimal(kesitliste[i].GetValue(2)));
                var kirmizikot = (Convert.ToDecimal(kesitliste[i].GetValue(4)));
                var EksenKaydir = (Convert.ToDouble(kesitliste[i].GetValue(5)));
                if (i == 0)
                {
                    object[] array1 = {1, Kes_No, Kes_KM, kirmizikot, EksenKaydir};
                    kseliste.Add(array1);
                    c++;
                    Kes_KMSON = Kes_KM;
                }

                if (Math.Abs(Kes_KM - Kes_KMSON) > 2)
                {
                    GetirEkNokta(shortdizi(aranoktaliste));
                    foreach (var ara in aranoktaliste)
                    {
                        object[] array4 = {0, ara.GetValue(1), ara.GetValue(2), ara.GetValue(3), ara.GetValue(4)};
                        kseliste.Add(array4);
                        c++;
                    }

                    var Kes_Ortal_KM = toplam / k;
                    var indekx = kseliste.Count;
                    object[] array1 = {1, indekx - c, Kes_Ortal_KM, kirmizikot, EksenKaydir};
                    kseliste[indekx - c] = array1;
                    object[] array2 = {1, Kes_No, Kes_KM, kirmizikot, EksenKaydir};
                    kseliste.Add(array2);
                    c = 1;
                    toplam = 0;
                    k = 0;
                    aranoktaliste.Clear();
                }

                k++;
                var semt = Math.Atan2(((Convert.ToDouble(Kes_Mes)) - 0),
                    (Convert.ToDouble((Convert.ToDouble(kesitliste[i].GetValue(2))) -
                                      ((Convert.ToDouble(kesitliste[i].GetValue(4))) + 1.72))));
                if (semt < 0)
                {
                    semt = semt + (2 * Math.PI);
                }

                object[] array = {semt, Kes_Mes, Kes_Kot, k, kirmizikot};
                aranoktaliste.Add(array);
                Kes_KMSON = Kes_KM;
                toplam = Kes_KM + toplam;
                if ((kesitliste.Count - 2) == i)
                {
                    GetirEkNokta(shortdizi(aranoktaliste));

                    foreach (var ara in aranoktaliste)
                    {
                        object[] array4 = {0, ara.GetValue(1), ara.GetValue(2), ara.GetValue(3), ara.GetValue(4)};
                        kseliste.Add(array4);
                    }
                }
            }

            return kseliste;
        }


        private static void GetirEkNokta(List<Array> araNoktaListe)
        {
            List<Array> eknoktadizi = new List<Array>();
            
            var kirmizikot = (Convert.ToDouble(araNoktaListe[0].GetValue(4)));
            double Yson = (Convert.ToDouble(araNoktaListe[araNoktaListe.Count - 1].GetValue(1)));
            double Xson = (Convert.ToDouble(araNoktaListe[araNoktaListe.Count - 1].GetValue(2)));
            double semtson = (Convert.ToDouble(araNoktaListe[araNoktaListe.Count - 1].GetValue(0)));
            object[] array3 = {semtson, Yson, Xson, "NOK", kirmizikot};
            eknoktadizi.Add(array3);
            double Y1 = 0;
            double X1 = 0;
            for (int k = 0; k < araNoktaListe.Count; k++)
            {
                if (k == 0)
                {
                    Y1 = Yson;
                    X1 = Xson;
                    Yson = (Convert.ToDouble(araNoktaListe[k].GetValue(1)));
                    Xson = (Convert.ToDouble(araNoktaListe[k].GetValue(2)));


                }

                var Y2 = (Convert.ToDouble(araNoktaListe[k].GetValue(1)));
                var X2 = (Convert.ToDouble(araNoktaListe[k].GetValue(2)));
                var semt2 = (Convert.ToDouble(araNoktaListe[k].GetValue(0)));


                double Mes1ve2 = Math.Sqrt(Math.Pow((Y2 - Y1), 2) + Math.Pow((X2 - X1), 2));
                var semt1ve2 = Math.Atan2((Y2 - Y1), (X2 - X1));
                if (semt1ve2 < 0)
                {
                    semt1ve2 = semt1ve2 + (2 * Math.PI);
                }

                double H = Math.Sqrt(Math.Abs(Math.Pow(2.62, 2) - Math.Pow(Mes1ve2 / 2, 2)));
                double Yorta = (Y1 + Y2) / 2;
                double Xorta = (X1 + X2) / 2;
                double Ym = Yorta + Math.Sin(semt1ve2 + Math.PI / 2) * H;
                double Xm = Xorta + Math.Cos(semt1ve2 + Math.PI / 2) * H;
                double semtMve1 = Math.Atan2((Y1 - Ym), (X1 - Xm));
                if (semtMve1 < 0)
                {
                    semtMve1 = semtMve1 + (2 * Math.PI);
                }

                double semtMve2 = Math.Atan2((Y2 - Ym), (X2 - Xm));
                if (semtMve2 < 0)
                {
                    semtMve2 = semtMve2 + (2 * Math.PI);
                }

                double delta = semtMve2 - semtMve1;
                if (delta < 0)
                {
                    delta = delta + (2 * Math.PI);
                }

                int noktaadedi = Convert.ToInt32(Math.Ceiling(delta / 0.1908396947));
                for (int ni = 1; ni < noktaadedi; ni++)
                {
                    object[] array =
                    {
                        semtMve1 + ni * (delta / (noktaadedi + 1)),
                        Ym + Math.Sin(semtMve1 + delta / noktaadedi * ni) * 2.62,
                        Xm + Math.Cos(semtMve1 + delta / noktaadedi * ni) * 2.62, ni, kirmizikot
                    };
                    eknoktadizi.Add(array);
                }

                object[] array2 = {semt2, Y2, X2, "NOK", kirmizikot};
                eknoktadizi.Add(array2);

                Y1 = Y2;
                X1 = X2;



            }

            araNoktaListe.Clear();
            //shortdizi(eknoktadizi);
            foreach (var ara in eknoktadizi)
            {
                object[] array5 = {ara.GetValue(0), ara.GetValue(1), ara.GetValue(2), ara.GetValue(3), ara.GetValue(4)};
                araNoktaListe.Add(array5);
            }
        }

        private static List<Array> shortdizi(List<Array> dizi)
        {
            for (int i = 0; i < dizi.Count - 1; i++)
            {
                for (int j = i; j < dizi.Count; j++)
                {
                    // >(büyük) işareti <(küçük ) olarak değiştirilirse büyükten küçüğe sıralanır
                    if ((Convert.ToDecimal(dizi[i].GetValue(0))) > (Convert.ToDecimal(dizi[j].GetValue(0))))
                    {

                        var gecici = dizi[j];
                        dizi[j] = dizi[i];
                        dizi[i] = gecici;
                    }
                }
            }

            return dizi;
        }

        private static double Faktoryel(double faktor)
        {

            double sonuc = 1;
            for (int i = 1; i <= faktor; i++)
            {
                sonuc = sonuc * i;
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
                X = X + Math.Pow(-1, n - 1) * Math.Pow(L, 4 * n - 1) /
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
                Y = Y + Math.Pow(-1, n - 1) * Math.Pow(L, 4 * n - 3) /
                    ((4 * n - 3) * Faktoryel(faktory) * Math.Pow(Sip_kat, 4 * n - 4) * Math.Pow(2, 2 * n - 2));
            }

            return Y;
        }

        private static double ofset_L_hesapla(double R, double L, double Yp, double Xp)
        {
            double Qp = 0;
            double kucuk = 0;
            double buyuk = Math.PI;
            double Lp;
            double aKare = R * L;
            for (int i = 0; i < 1000; i++)
            {
                Qp = (kucuk + buyuk) / 2;
                Lp = Math.Sqrt(Qp * 2 * aKare);
                var YpI = Spiral_Y_Hesapla(Lp, Math.Sqrt(aKare)) +
                             Math.Tan(Qp) * (Xp + Spiral_X_Hesapla(Lp, Math.Sqrt(aKare)));
                if ((Yp - YpI) < 0.000001 && (Yp - YpI) > -0.000001)
                {
                    break;
                }

                if ((YpI > Yp) || ((Yp - YpI) < 0.0000001 && (Yp - YpI) > -0.0000001)) buyuk = Qp;
                if ((YpI < Yp) || ((Yp - YpI) < 0.0000001 && (Yp - YpI) > -0.0000001)) kucuk = Qp;
            }

            Lp = Math.Sqrt(2 * Qp * R * L);
            return Lp;
        }

        private static double sapmaHesabi(double radius)
        {
            double sapma = 0;
            if (radius >= 800) sapma = 0;
            if (radius >= 700 && radius < 800)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 700);
            }

            if (radius >= 600 && radius < 700)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 600) + 0.0375;
            }

            if (radius >= 500 && radius < 600)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 500) + 0.0750;
            }

            if (radius >= 400 && radius < 500)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 400) + 0.1125;
            }

            if (radius < 400) sapma = 0.150;
            return sapma;
        }

        private static Array GetirDeverVeYon(LandXMLAlignmentsAlignment alignment, decimal kilometre)
        {
            if (alignment.Cant == null)
            {
                return null;
            }

            var Cant = alignment.Cant;
            var CantItems = alignment.Cant.CantStation.ToList();
            decimal dever = 0;
            string yon = null;

            foreach (var cantStation in Cant.CantStation)
            {
                int cantStationIndex = CantItems.IndexOf(cantStation);
                decimal guncelKm = Yardimci.GetirCantDetaylar(cantStation).KM;
                decimal sonucDever = 0;
                if (cantStationIndex == 0 && kilometre < guncelKm)
                {
                    yon = "cw";
                    break;
                }

                decimal guncelDever = Yardimci.GetirCantDetaylar(cantStation).Cant;
                string guncelYon = Yardimci.GetirCantDetaylar(cantStation).Yon;

                if (cantStationIndex == CantItems.Count - 1) //son item
                {
                    sonucDever = 0;
                    yon = "cw";
                }
                else
                {
                    var ilerilKm = Yardimci.GetirCantDetaylar(CantItems[cantStationIndex + 1]).KM;
                    var ileriDever = Yardimci.GetirCantDetaylar(CantItems[cantStationIndex + 1]).Cant;

                    if (kilometre >= guncelKm && kilometre < ilerilKm)
                    {
                        sonucDever = (ileriDever - guncelDever) / (ilerilKm - guncelKm) * (kilometre - guncelKm) +
                                     guncelDever;
                        yon = guncelYon;
                    }
                }

                dever = sonucDever;
            }

            object[] deverVeYon = {(Convert.ToString(dever, CultureInfo.InvariantCulture)), yon};

            return deverVeYon;

        }

        public static string NoktalardanEnkesitOlustur(List<Array> kesitler)
        {
            var kseSb = new StringBuilder("2  @=COLOR=SECTION=DISPLAY=-12796356").AppendLine();
            foreach (var kesit in kesitler)
            {
                kseSb.AppendLine(kesit.GetValue(0) + "  " + kesit.GetValue(1) + "  " + kesit.GetValue(2) + "  " +
                                 kesit.GetValue(3));
            }

            return kseSb.ToString();
        }

        public static string TipkesittenEnkesitOlustur(List<Array> kesitler)
        {
            var daireSb = new StringBuilder();
            int kesitNo = 1;
            foreach (var kesit in kesitler)
            {
                if (Convert.ToInt32(kesit.GetValue(0)) == 1)
                {
                    double kilometre = Convert.ToDouble(kesit.GetValue(2));
                    double kirmizikotD = Convert.ToDouble(kesit.GetValue(3));
                    double sapma = Convert.ToDouble(kesit.GetValue(4));

                    daireSb.AppendLine("1" + " " + kesitNo + " " + kilometre + " " + sapma);

                    for (int j = 0; j < 21; j++)
                    {
                        double mesafe = sapma + Math.Sin(j * (2 * Math.PI) / 21) * 2.62;
                        double kot = kirmizikotD + 1.72 + Math.Cos(j * (2 * Math.PI) / 21) * 2.62;
                        daireSb.AppendLine("0" + " " + mesafe + " " + kot + " " + j);
                        if (j == 20)
                        {
                            var sonMesafe = sapma + Math.Sin(0 * (2 * Math.PI) / 21) * 2.62;
                            var sonKot = kirmizikotD + 1.72 + Math.Cos(0 * (2 * Math.PI) / 21) * 2.62;
                            daireSb.AppendLine("0" + " " + sonMesafe + " " + sonKot + " " + "21");
                        }
                    }

                    kesitNo++;
                }
            }

            return daireSb.ToString();
        }

        public static string GabariKesitleriOlustur(List<Array> kesitler, LandXML landXml)
        {
            var gabariCizim = new StringBuilder();
            foreach (var kesit in kesitler)
            {
                if (Convert.ToDecimal(kesit.GetValue(0)) == 1)
                {
                    double kilometre = Convert.ToDouble(kesit.GetValue(2));
                    double kirmizikotD = Convert.ToDouble(kesit.GetValue(3));
                    double sapma = Convert.ToDouble(kesit.GetValue(4));
                    var alignment = landXml.Alignments.Alignment;
                    Array deverveyon = GetirDeverVeYon(alignment, Convert.ToDecimal(kilometre));
                    double dever = Convert.ToDouble(deverveyon.GetValue(0));
                    string yon = Convert.ToString(deverveyon.GetValue(1));

                    gabariCizim.AppendLine(GabariCiz(kilometre, kirmizikotD, sapma, dever, yon));
                }
            }

            return gabariCizim.ToString();
        }

        private static string GabariCiz(double kilometre, double kirmizikot, double sapma, double dever, string yon)
        {
            var aci = Math.Atan(dever / 1000 / 1.50); if (yon == "ccw") { aci = aci * -1; }

            var gabariSb = new StringBuilder();
            gabariSb.Append("1" + " " + "1" + " " + Convert.ToString(kilometre) + " " + Convert.ToString(sapma));

            var mes = sapma + Math.Sin(3 * Math.PI / 2 + aci) * 1.356; 
            var kot = kirmizikot + dever / 2000 + Math.Cos(3 * Math.PI / 2 + aci) * 1.356; 
            gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "1");

            mes = mes + Math.Sin(0 + aci) * 0.031; kot = kot + Math.Cos(0 + aci) * 0.031; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "2");
            mes = mes + Math.Sin(3 * Math.PI / 2 + aci) * 0.202; kot = kot + Math.Cos(3 * Math.PI / 2 + aci) * 0.202; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "3");
            mes = mes + Math.Sin(0 + aci) * 0.171; kot = kot + Math.Cos(0 + aci) * 0.171; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "4");
            mes = mes + Math.Sin(3 * Math.PI / 2 + aci) * 0.222; kot = kot + Math.Cos(3 * Math.PI / 2 + aci) * 0.222; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "5");
            mes = mes + Math.Sin(6.2221 + aci) * 2.358; kot = kot + Math.Cos(6.2221 + aci) * 2.358; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "6");
            mes = mes + Math.Sin(5.0679 + aci) * 0.063; kot = kot + Math.Cos(5.0679 + aci) * 0.063; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "7");
            mes = mes + Math.Sin(5.9744 + aci) * 0.063; kot = kot + Math.Cos(5.9744 + aci) * 0.063; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "8");
            mes = mes + Math.Sin(0.1116 + aci) * 0.336; kot = kot + Math.Cos(0.1116 + aci) * 0.336; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "9");
            mes = mes + Math.Sin(Math.PI / 2 + aci) * 0.017; kot = kot + Math.Cos(Math.PI / 2 + aci) * 0.017; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "10");
            mes = mes + Math.Sin(0 + aci) * 0.170; kot = kot + Math.Cos(0 + aci) * 0.170; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "11");
            mes = mes + Math.Sin(0.7163 + aci) * 0.301; kot = kot + Math.Cos(0.7163 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "12");
            mes = mes + Math.Sin(0.9259 + aci) * 0.301; kot = kot + Math.Cos(0.9259 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "13");
            mes = mes + Math.Sin(1.1360 + aci) * 0.301; kot = kot + Math.Cos(1.1360 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "14");
            mes = mes + Math.Sin(1.3451 + aci) * 0.301; kot = kot + Math.Cos(1.3451 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "15");
            mes = mes + Math.Sin(Math.PI / 2 + aci) * 0.942; kot = kot + Math.Cos(Math.PI / 2 + aci) * 0.942; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "16");
            mes = mes + Math.Sin(Math.PI / 2 + aci) * 0.942; kot = kot + Math.Cos(Math.PI / 2 + aci) * 0.942; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "17");
            mes = mes + Math.Sin(1.7965 + aci) * 0.301; kot = kot + Math.Cos(1.7965 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "18");
            mes = mes + Math.Sin(2.0055 + aci) * 0.301; kot = kot + Math.Cos(2.0055 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "19");
            mes = mes + Math.Sin(2.2157 + aci) * 0.301; kot = kot + Math.Cos(2.2157 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "20");
            mes = mes + Math.Sin(2.4253 + aci) * 0.301; kot = kot + Math.Cos(2.4253 + aci) * 0.301; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "21");
            mes = mes + Math.Sin(Math.PI + aci) * 0.170; kot = kot + Math.Cos(Math.PI + aci) * 0.170; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "22");
            mes = mes + Math.Sin(Math.PI / 2 + aci) * 0.017; kot = kot + Math.Cos(Math.PI / 2 + aci) * 0.017; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "23");
            mes = mes + Math.Sin(3.0300 + aci) * 0.336; kot = kot + Math.Cos(3.0300 + aci) * 0.336; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "24");
            mes = mes + Math.Sin(3.4504 + aci) * 0.063; kot = kot + Math.Cos(3.4504 + aci) * 0.063; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "25");
            mes = mes + Math.Sin(4.3569 + aci) * 0.063; kot = kot + Math.Cos(4.3569 + aci) * 0.063; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "26");
            mes = mes + Math.Sin(3.2027 + aci) * 2.358; kot = kot + Math.Cos(3.2027 + aci) * 2.358; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "27");
            mes = mes + Math.Sin(3 * Math.PI / 2 + aci) * 0.222; kot = kot + Math.Cos(3 * Math.PI / 2 + aci) * 0.222; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "28");
            mes = mes + Math.Sin(Math.PI + aci) * 0.171; kot = kot + Math.Cos(Math.PI + aci) * 0.171; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "29");
            mes = mes + Math.Sin(3 * Math.PI / 2 + aci) * 0.202; kot = kot + Math.Cos(3 * Math.PI / 2 + aci) * 0.202; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "30");
            mes = mes + Math.Sin(Math.PI + aci) * 0.031; kot = kot + Math.Cos(Math.PI + aci) * 0.031; gabariSb.AppendLine("0" + " " + mes + " " + kot + " " + "31");
            mes = mes + Math.Sin(3 * Math.PI / 2 + aci) * 1.356; kot = kot + Math.Cos(3 * Math.PI / 2 + aci) * 1.356; gabariSb.Append("0" + " " + mes + " " + kot + " " + "32");

            return gabariSb.ToString();

        }
    }
}
