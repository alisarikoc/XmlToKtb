using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Common;

namespace XmlToKtb
{
    public static class GzgIcerigi
    {

        public static List<Array> GzgGetir(LandXML landXml, List<Array> ncndata)
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

                                KesitMES =  dikayak;
                                KesitKM = guncelKM + (Convert.ToDecimal(dikboy));
                                var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                object[] array = {Math.Round(KesitKM,3), NoktaNo," ","NOKTA",Math.Round(KesitMES,3), Math.Round((NoktaKot-kirmizikot)*1000,0), GetirDeverVeYon(alignment, KesitKM).GetValue(0)};
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
                        var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, guncelKM));
                        object[] array3 = { Math.Round(guncelKM,3)," ", "PF/DF", " ", " "," ", GetirDeverVeYon(alignment, guncelKM).GetValue(0) };
                        kesitliste.Add(array3);

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
                                kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                object[] array = {Math.Round(KesitKM,3), NoktaNo, " ","NOKTA", Math.Round(KesitMES,3), Math.Round((NoktaKot-kirmizikot)*1000,0), GetirDeverVeYon(alignment, KesitKM).GetValue(0)};
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
                                kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                    object[] array =
                                {
                                    Math.Round(KesitKM,3), NoktaNo, " ","NOKTA", Math.Round(KesitMES,3), Math.Round((NoktaKot-kirmizikot)*1000,0), GetirDeverVeYon(alignment, KesitKM).GetValue(0) 
                                };
                                kesitliste.Add(array);
                            }
                        }

                        guncelKM = guncelKM + curve.length;
                        object[] arraypf = { Math.Round(guncelKM,3)," ", "PF/DF", " ", " "," ", GetirDeverVeYon(alignment, guncelKM).GetValue(0) };
                        kesitliste.Add(arraypf);
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
                            object[] arraypf = { Math.Round(guncelKM,3)," ", "PO/DO", " ", " ", " ", GetirDeverVeYon(alignment, guncelKM).GetValue(0) };
                            kesitliste.Add(arraypf);
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
                                        var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));

                                        object[] array =
                                            {Math.Round(KesitKM,3), NoktaNo ," ", "NOKTA", Math.Round(KesitMES,3), Math.Round((NoktaKot-kirmizikot)*1000,0), GetirDeverVeYon(alignment, KesitKM).GetValue(0)};
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
                                        object[] array =
                                            {Math.Round(KesitKM,3), NoktaNo," ", "NOKTA", Math.Round(KesitMES,3), Math.Round((NoktaKot-kirmizikot)*1000,0), GetirDeverVeYon(alignment, KesitKM).GetValue(0)};
                                        kesitliste.Add(array);
                                    }
                                }
                            }
                            object[] arraypf = {Math.Round((guncelKM+Spiral.length),3)," ", "PO/DO", " ", " ", " ", GetirDeverVeYon(alignment, (guncelKM+Spiral.length)).GetValue(0) };
                            kesitliste.Add(arraypf);
                        }

                        guncelKM = guncelKM + Spiral.length;

                        break;
                    } 
                        
                }
            }

            List<Array> prfList = new List<Array>();
            var profAlign = alignment.Profile.ProfAlign;
            foreach (var item in profAlign.Items)
            {
                var km = Yardimci.GetirProfilAlignDetaylar(item).KM;
                var kot = Yardimci.GetirProfilAlignDetaylar(item).Kot;
                var length = Yardimci.GetirProfilAlignDetaylar(item).Length;
                var kod = "";
                decimal kmyazi = 0;

                for (int index = 0; index < 3; index++)
                {
                    if (index == 0) { kod = "TO"; kmyazi = km - length / 2; }
                    if (index == 1) { kod = "KOT"; kmyazi = km ; }
                    if (index == 2) { kod = "TF"; kmyazi = km + length / 2; }
                    object[] arraypf =
                    {
                        Math.Round(kmyazi, 3), " ", kod, " ", " ", " ", GetirDeverVeYon(alignment, kmyazi).GetValue(0)
                    };
                    kesitliste.Add(arraypf);

                }



            }


            kesitliste = kesitliste.OrderBy(x => x.GetValue(0)).ToList();

            return kesitliste;
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
        
        private static Array GetirDeverVeYon(LandXMLAlignmentsAlignment alignment, decimal kilometre)
        {
            if (alignment.Cant.CantStation == null)
            {
                
                    object[] deverVeYon0 = { 0, "cw" };
                    return deverVeYon0;
            }

            var Cant = alignment.Cant;

            var CantItems = alignment.Cant.CantStation.ToList();
            decimal dever = 0;
            string yon = null;

            foreach (var cantStation in Cant.CantStation)
            {
                int cantStationIndex = CantItems.IndexOf(cantStation);
                decimal guncelKm = Yardimci.GetirCantDetaylar(cantStation).KM;
                if (cantStationIndex == 0 && kilometre < guncelKm)
                {

                    dever = 0;
                    yon = "cw";
                    break;
                }

                decimal guncelDever = Yardimci.GetirCantDetaylar(cantStation).Cant;
                string guncelYon = Yardimci.GetirCantDetaylar(cantStation).Yon;

                if (cantStationIndex == CantItems.Count - 1) //son item
                {
                    dever = 0;
                    yon = "cw";
                    break;
                }
                else
                {
                    var ilerilKm = Yardimci.GetirCantDetaylar(CantItems[cantStationIndex + 1]).KM;
                    var ileriDever = Yardimci.GetirCantDetaylar(CantItems[cantStationIndex + 1]).Cant;

                    if (kilometre >= guncelKm && kilometre < ilerilKm)
                    {
                        dever = (ileriDever - guncelDever) / (ilerilKm - guncelKm) * (kilometre - guncelKm) +
                                     guncelDever;
                        yon = guncelYon;
                        break;
                        
                    }
                }

            }

            dever = Math.Round(dever, 0);

            object[] deverVeYon = {(Convert.ToString(dever, CultureInfo.InvariantCulture)), yon};

            return deverVeYon;

        }

        public static string NoktalardanTabloOlustur(List<Array> kesitler)
        {
            
            var kseSb =new StringBuilder();
            foreach (var kesit in kesitler)
            {
                kseSb.AppendLine(kesit.GetValue(0)+","+ kesit.GetValue(1)+","+ kesit.GetValue(2)+","+ kesit.GetValue(3)+","+ kesit.GetValue(4)+","+ kesit.GetValue(5)+","+ kesit.GetValue(6));
            }
            return kseSb.ToString();
        }


    }
}
