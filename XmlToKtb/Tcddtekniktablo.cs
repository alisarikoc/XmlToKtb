using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Common;
using Common.Enum;
using Common.Packages;

namespace XmlToKtb
{
    public static class TcddteknikIcerigi
    {

        public static List<Array> GzgGetir(LandXML landXml, List<Array> ncndata)
        {
            var alignment = landXml.Alignments.Alignment;
            //var kse = new StringBuilder("2  @=COLOR=SECTION=DISPLAY=-12796356").AppendLine();
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;
            List<Array> kesitliste = new List<Array>();

            List<Array> yataykurpliste = new List<Array>();
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
                            var line = (LandXMLAlignmentsAlignmentCoordGeomLine)item;
                            var starty = (Convert.ToDouble(line.Start.Split()[1]));
                            var startx = (Convert.ToDouble(line.Start.Split()[0]));
                            var endy = (Convert.ToDouble(line.End.Split()[1]));
                            var endx = (Convert.ToDouble(line.End.Split()[0]));

                            if (guncelKM == baslangicKM)
                            {
                                var kirmizikotSC = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, guncelKM));
                                var deverlerSC = GetirDeverVeYon(alignment, guncelKM);
                                object[] array3 = { Math.Round(baslangicKM, 3), "PB", kirmizikotSC, starty, startx, " ", " ", " ", deverlerSC.GetValue(0), deverlerSC.GetValue(1), " ", " ", " " };
                                yataykurpliste.Add(array3);
                            }
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
                                    var deverler = GetirDeverVeYon(alignment, KesitKM);
                                    var kirmizikot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, KesitKM));
                                    object[] array = { Math.Round(KesitKM, 3), NoktaNo, kirmizikot, NoktaY, NoktaX, NoktaKot, " ", " ", deverler.GetValue(0), deverler.GetValue(1), " ", Math.Round((NoktaKot - kirmizikot), 3), Math.Round(KesitMES, 3) };
                                    kesitliste.Add(array);
                                }
                            }

                            guncelKM = guncelKM + line.length;
                            break;
                        }
                    case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                        {
                            var curve = (LandXMLAlignmentsAlignmentCoordGeomCurve)item;
                            var starty = (Convert.ToDouble(curve.Start.Split()[1]));
                            var startx = (Convert.ToDouble(curve.Start.Split()[0]));
                            var endy = (Convert.ToDouble(curve.End.Split()[1]));
                            var endx = (Convert.ToDouble(curve.End.Split()[0]));
                            var My = (Convert.ToDouble(curve.Center.Split()[1]));
                            var Mx = (Convert.ToDouble(curve.Center.Split()[0]));
                            var radius = (Convert.ToDouble(curve.radius));
                            var kirmizikotSC = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, guncelKM));
                            var deverlerSC = GetirDeverVeYon(alignment, guncelKM);

                            object[] array3 = { Math.Round(guncelKM, 3), "SC", kirmizikotSC, starty, startx, " ", "R=" + ucle(radius) + " L=" + ucle(Convert.ToDouble(curve.length)), " ", deverlerSC.GetValue(0), deverlerSC.GetValue(1), " ", " ", " " };
                            yataykurpliste.Add(array3);

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
                                    var deverler = GetirDeverVeYon(alignment, KesitKM);
                                    object[] array = { Math.Round(KesitKM, 3), NoktaNo, kirmizikot, NoktaY, NoktaX, NoktaKot, " ", " ", deverler.GetValue(0), deverler.GetValue(1), " ", Math.Round((NoktaKot - kirmizikot), 3), Math.Round(KesitMES, 3) };
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
                                    var deverler = GetirDeverVeYon(alignment, KesitKM);
                                    object[] array = { Math.Round(KesitKM, 3), NoktaNo, kirmizikot, NoktaY, NoktaX, NoktaKot, " ", " ", deverler.GetValue(0), deverler.GetValue(1), " ", Math.Round((NoktaKot - kirmizikot), 3), Math.Round(KesitMES, 3) };
                                    kesitliste.Add(array);
                                }
                            }

                            guncelKM = guncelKM + curve.length;
                            var kirmizikotCS = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, guncelKM));
                            var deverlerCS = GetirDeverVeYon(alignment, guncelKM);
                            object[] arraypf = { Math.Round(guncelKM, 3), "CS", kirmizikotCS, endy, endx, " ", " ", " ", deverlerCS.GetValue(0), deverlerCS.GetValue(1), " ", " ", " " };
                            yataykurpliste.Add(arraypf);
                            break;
                        }
                    case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                        {
                            var Spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral)item;
                            var starty = (Convert.ToDouble(Spiral.Start.Split()[1]));
                            var startx = (Convert.ToDouble(Spiral.Start.Split()[0]));
                            var endy = (Convert.ToDouble(Spiral.End.Split()[1]));
                            var endx = (Convert.ToDouble(Spiral.End.Split()[0]));
                            var PIy = (Convert.ToDouble(Spiral.PI.Split()[1]));
                            var PIx = (Convert.ToDouble(Spiral.PI.Split()[0]));

                            var yon = Spiral.rot;
                            var Length = (Convert.ToDouble(Spiral.length));
                            var kirmizikoSP = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, guncelKM));
                            var deverlerSP = GetirDeverVeYon(alignment, guncelKM);
                            if (Spiral.radiusStart.ToUpper() == "INF") // GİRİŞ SPİRAL
                            {
                                var aKare = Convert.ToDouble(Spiral.radiusEnd) * Length;
                                object[] arraypf = { Math.Round(guncelKM, 3), "TS", kirmizikoSP, starty, startx, " ", "L=" + ucle(Length) + " " + "A=" + ucle(Math.Sqrt(aKare)), " ", deverlerSP.GetValue(0), deverlerSP.GetValue(1), " ", " ", " " };
                                yataykurpliste.Add(arraypf);
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

                                    if (dikboy > 0 && dikboyKisa > 0 && Math.Abs(dikayak) < 50)
                                    {

                                        var L_point = ofset_L_hesapla(Convert.ToDouble(Spiral.radiusEnd), Length, dikboy, dikayak);
                                        if (yon == "cw")
                                            L_point = ofset_L_hesapla(Convert.ToDouble(Spiral.radiusEnd), Length, dikboy, -dikayak);
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
                                            var deverler = GetirDeverVeYon(alignment, KesitKM);
                                            object[] array = { Math.Round(KesitKM, 3), NoktaNo, kirmizikot, NoktaY, NoktaX, NoktaKot, " ", " ", deverler.GetValue(0), deverler.GetValue(1), " ", Math.Round((NoktaKot - kirmizikot), 3), Math.Round(KesitMES, 3) };
                                            kesitliste.Add(array);
                                        }
                                    }
                                }
                            }

                            if (Spiral.radiusEnd.ToUpper() == "INF") // ÇIKIŞ SPİRAL
                            {
                                var aKare = Convert.ToDouble(Spiral.radiusStart) * Length;
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
                                            var deverler = GetirDeverVeYon(alignment, KesitKM);
                                            object[] array = { Math.Round(KesitKM, 3), NoktaNo, kirmizikot, NoktaY, NoktaX, NoktaKot, " ", " ", deverler.GetValue(0), deverler.GetValue(1), " ", Math.Round((NoktaKot - kirmizikot), 3), Math.Round(KesitMES, 3) };
                                            kesitliste.Add(array);
                                        }
                                    }
                                }
                                var kirmizikoST = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, guncelKM));
                                var deverlerST = GetirDeverVeYon(alignment, guncelKM + Convert.ToDecimal(Length));
                                object[] arraypf = { Math.Round(guncelKM+Convert.ToDecimal(Length), 3), "ST", kirmizikoST, endy, endx, " ", "L=" + ucle(Length) + " " + "A=" + ucle(Math.Sqrt(aKare)), " ", deverlerST.GetValue(0), deverlerST.GetValue(1), " ", " ", " " };
                                yataykurpliste.Add(arraypf);
                            }
                            guncelKM = guncelKM + Spiral.length;
                            break;
                        }
                }
            }
            yataykurpliste = yataykurpliste.OrderBy(x => x.GetValue(0)).ToList();
            for (var i = 0; i < yataykurpliste.Count; i++)
            {
                var KM = Convert.ToDecimal(yataykurpliste[i].GetValue(0));
                string Kod = Convert.ToString(yataykurpliste[i].GetValue(1));
                decimal Kot = Convert.ToDecimal(yataykurpliste[i].GetValue(2));

                decimal Y = Convert.ToDecimal(yataykurpliste[i].GetValue(3));

                decimal X = Convert.ToDecimal(yataykurpliste[i].GetValue(4));
                string yaricapegimbilgi = Convert.ToString(yataykurpliste[i].GetValue(6));

                if (Kod == "TS" && i != 0)
                {
                    var lineUzunluk = Convert.ToDouble(Math.Round(KM - Convert.ToDecimal(yataykurpliste[i - 1].GetValue(0)), 2));
                    object[] array2 =
                    {
                        Math.Round(KM, 3),Kod,Math.Round(Kot,3),Y , X, " ", " ",yaricapegimbilgi,yataykurpliste[i].GetValue(8),yataykurpliste[i].GetValue(9),"L="+ucle(lineUzunluk)," "," "
                    };
                    kesitliste.Add(array2);
                }
                else
                {
                    object[] array1 =
                    {
                        Math.Round(KM, 3),Kod,Math.Round(Kot,3),Y , X, " ", " ",yaricapegimbilgi,yataykurpliste[i].GetValue(8),yataykurpliste[i].GetValue(9)," "," "," "
                    };
                    kesitliste.Add(array1);
                }
            }

            List<Array> prfList = new List<Array>();
            var profAlign = alignment.Profile.ProfAlign;
            var profAlignItems = alignment.Profile.ProfAlign.Items.ToList();
            var prfbaslangicKm = Yardimci.GetirProfilAlignDetaylar(profAlignItems[0]).KM;
            List<Array> Profilsatir = new List<Array>();
            foreach (var item in profAlign.Items)
            {
                object oncekiItem;
                object sonrakiItem = null;
                if (profAlignItems.IndexOf(item) == 0)//ilk item
                {
                    oncekiItem = null;
                    continue;
                    //sonrakiItem = profAlignItems[profAlignItems.IndexOf(item) + 1];
                }
                else if (profAlignItems.IndexOf(item) == profAlignItems.Count - 1)//son item
                {
                    oncekiItem = profAlignItems[profAlignItems.IndexOf(item) - 1];
                }
                else//aradaki itemlar
                {
                    oncekiItem = profAlignItems[profAlignItems.IndexOf(item) - 1];
                    sonrakiItem = profAlignItems[profAlignItems.IndexOf(item) + 1];
                }

                bool ilkItemIse = oncekiItem == null;
                bool sonItemIse = sonrakiItem == null;

                var guncelProfilAlign = Yardimci.GetirProfilAlignDetaylar(item);
                var oncekiProfilAlign = ilkItemIse ? new ProfilAlignPackage() : Yardimci.GetirProfilAlignDetaylar(oncekiItem);
                var sonrakiProfilAlign = sonItemIse ? new ProfilAlignPackage() : Yardimci.GetirProfilAlignDetaylar(sonrakiItem);
                var guncelPVIsatir = new ProfilAlignPackage();

                decimal oncekiKm = oncekiProfilAlign.KM;
                decimal oncekiKot = oncekiProfilAlign.Kot;
                decimal oncekiLength = oncekiProfilAlign.Length;

                decimal guncelKm = guncelProfilAlign.KM;
                decimal guncelKot = guncelProfilAlign.Kot;
                decimal guncelLength = guncelProfilAlign.Length;

                decimal sonrakiKm = sonrakiProfilAlign.KM;
                decimal sonrakiKot = sonrakiProfilAlign.Kot;
                decimal sonrakiLength = sonrakiProfilAlign.Length;



                if (oncekiLength == 0)
                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);

                    var Length = guncelKm - guncelLength / 2;
                    object[] array = { oncekiKm, oncekiKot, "Line", g1, 0, Length, 0, "PVI" };
                    Profilsatir.Add(array);
                }

                //TODO Ali Sarıkoç hesaplamalarını burada yap onceki üç ve sonraki üç değer yukarıda oluşturdm onları kullanarak hesaplama yap.
                if (ilkItemIse)
                {

                    var g2 = (sonrakiKot - guncelKot) / (sonrakiKm - guncelKm);

                    var Length = sonrakiKm - sonrakiLength / 2;
                    object[] array = { guncelKm, guncelKot, "Line", 0, g2, Length, 0, "M1" };
                    Profilsatir.Add(array);

                }
                else if (sonItemIse)
                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);

                    var Length = (guncelKm - oncekiKm) - (oncekiLength / 2);
                    object[] array = { guncelKm, guncelKot, "Line", g1, 0, Length, 0, "S1" };
                    Profilsatir.Add(array);
                    break;
                }
                else
                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);
                    var g2 = (sonrakiKot - guncelKot) / (sonrakiKm - guncelKm);


                    bool ekstraNoktaVarIse = EkstraNoktaMi(oncekiKm, sonrakiKm, guncelKm, oncekiLength, sonrakiLength);

                    if (!ekstraNoktaVarIse)
                    {
                        guncelKm = guncelKm - guncelLength / 2;
                        var geriKot = guncelKot - g1 * guncelLength / 2;
                        var ileriKot = guncelKot + g2 * guncelLength / 2;
                        var kirmizikotPVI = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, guncelKm + guncelLength / 2));
                        if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Line)
                        {
                            object[] array = { guncelKm, guncelKot, "Line", g2, 0, (sonrakiKm - sonrakiLength / 2) - guncelKm, 0, "PVI" };
                            Profilsatir.Add(array);
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Parabola)
                        {
                            object[] array = { guncelKm, geriKot, "parabola", g1, g2, guncelLength, 0, "PVC" };
                            Profilsatir.Add(array);
                            object[] array2 = { guncelKm + guncelLength / 2, kirmizikotPVI, "PVI", g1, g2, guncelLength, 0, "PVI" };
                            Profilsatir.Add(array2);
                            object[] array1 = { guncelKm + guncelLength, ileriKot, "Line", g2, 0, sonrakiLength, 0, "PVT" };
                            Profilsatir.Add(array1);
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Curve)
                        {
                            object[] array = { guncelKm, geriKot, "curve", g1, g2, guncelLength, guncelProfilAlign.Radius, "PVC" };
                            Profilsatir.Add(array);
                            object[] array2 = { guncelKm + guncelLength / 2, kirmizikotPVI, "PVI", g1, g2, guncelLength, 0, "PVI" };
                            Profilsatir.Add(array2);
                            object[] array1 = { guncelKm + guncelLength, ileriKot, "Line", g2, 0, sonrakiLength, 0, "PVT" };
                            Profilsatir.Add(array1);
                        }
                    }
                }
            }
            for (var i = 0; i < Profilsatir.Count; i++)
            {
                decimal KM = Convert.ToDecimal(Profilsatir[i].GetValue(0));
                decimal Kot = Convert.ToDecimal(Profilsatir[i].GetValue(1));
                string Tip = Convert.ToString(Profilsatir[i].GetValue(2));
                decimal g1 = Convert.ToDecimal(Profilsatir[i].GetValue(3));
                decimal g2 = Convert.ToDecimal(Profilsatir[i].GetValue(4));
                decimal L = Convert.ToDecimal(Profilsatir[i].GetValue(5));
                decimal Radius = Convert.ToDecimal(Profilsatir[i].GetValue(6));
                string Kod = Convert.ToString(Profilsatir[i].GetValue(7));
                var deverVeYon = GetirDeverVeYon(alignment, KM);
                var yaricapegimbilgi = "";
                if (i != 0)
                {
                    if (Kod == "PVC")
                    {
                        yaricapegimbilgi = ucle(Convert.ToDouble(g1 * 1000)) + "‰";
                    }
                    else if (Kod == "PVI")
                    {
                        if (Tip == "curve") yaricapegimbilgi = "R=" + ucle(Convert.ToDouble(Radius));
                        else yaricapegimbilgi = "R=" + ucle(Convert.ToDouble(L / (g2 - g1)));
                    }
                    else if (Kod == "S1")
                    {
                        yaricapegimbilgi = ucle(Convert.ToDouble(g1 * 1000)) + "‰";
                    }
                }
                else { yaricapegimbilgi = " "; Kod = "M1"; }



                object[] array1 =
                {
                    Math.Round(KM, 3),Kod,Math.Round(Kot,3)," " , " ", " ", yaricapegimbilgi," ",deverVeYon.GetValue(0),deverVeYon.GetValue(1)," "," "," "
                };
                kesitliste.Add(array1); // PVC



            }

            kesitliste = kesitliste.OrderBy(x => x.GetValue(0)).ToList();

            return kesitliste;
        }
        private static bool EkstraNoktaMi(decimal oncekiKm, decimal sonrakiKm, decimal guncelKm, decimal oncekiLength, decimal sonrakiLength)
        {
            bool onceYakinNoktaVarMi = Math.Abs(oncekiKm + oncekiLength / 2 - guncelKm) < 0.1m;

            bool sonraYakinNotaVarMi = Math.Abs(sonrakiKm - sonrakiLength / 2 - guncelKm) < 0.1m;

            return onceYakinNoktaVarMi || sonraYakinNotaVarMi;
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

        public static Array GetirDeverVeYon(LandXMLAlignmentsAlignment alignment, decimal kilometre)
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
            object[] deverVeYon;
            if (yon == "cw")
            {
                deverVeYon = new object[] { (Convert.ToString(dever, CultureInfo.InvariantCulture)), 0, yon };

                return deverVeYon;
            }
            deverVeYon = new object[] { 0, (Convert.ToString(dever, CultureInfo.InvariantCulture)), yon };

            return deverVeYon;

        }

        public static string NoktalardanTabloOlustur(List<Array> kesitler)
        {

            var kseSb = new StringBuilder();
            foreach (var kesit in kesitler)
            {
                kseSb.AppendLine(kesit.GetValue(0) + "," +
                                 kesit.GetValue(1) + "," +
                                 kesit.GetValue(2) + "," +
                                 kesit.GetValue(3) + "," +
                                 kesit.GetValue(4) + "," +
                                 kesit.GetValue(5) + "," +
                                 kesit.GetValue(7) + "," +
                                 kesit.GetValue(6) + "," +
                                 kesit.GetValue(8) + "," +
                                 kesit.GetValue(9) + "," +
                                 kesit.GetValue(10) + "," +
                                 kesit.GetValue(11) + "," +
                                 kesit.GetValue(12));
            }
            return kseSb.ToString();
        }

        public static LandXML deverprofil(LandXML landXml, List<Array> kesitler)
        {
            var deverlist = new StringBuilder();
            var nok_ofset = new StringBuilder();
            var nok_kotfarki = new StringBuilder();
            List<LandXMLCgPoint> CgPoints = new List<LandXMLCgPoint>();
            foreach (var kesit in kesitler)
            {
                var km = (kesit.GetValue(0));
                if (Convert.ToString(kesit.GetValue(5)) != " ")
                {
                    LandXMLCgPoint CgPoint = new LandXMLCgPoint
                    {
                        name = Convert.ToString(kesit.GetValue(1)),
                        code = Convert.ToString(kesit.GetValue(11)),
                        value = Convert.ToString(kesit.GetValue(4)) + " " + Convert.ToString(kesit.GetValue(3)) + " " + Convert.ToString(kesit.GetValue(5))
                    };
                    CgPoints.Add(CgPoint);
                    var offset = Convert.ToDecimal((kesit.GetValue(12)));
                    var kotfarki = Convert.ToDecimal((kesit.GetValue(11)));
                    nok_ofset.Append(km + " " + offset + " ");
                    nok_kotfarki.Append(km + " " + kotfarki + " ");
                }
                decimal dever = Convert.ToDecimal(kesit.GetValue(8));
                if (dever < Convert.ToDecimal(kesit.GetValue(9)))
                {
                    dever = Convert.ToDecimal(kesit.GetValue(9));
                }
                deverlist.Append(km + " " + dever + " ");



            }
            landXml.CgPoints = new LandXMLCgPoints { CgPoint = CgPoints.ToArray() };


            LandXMLAlignmentsAlignmentProfileProfSurf profSurf = new LandXMLAlignmentsAlignmentProfileProfSurf
            { name = "deverlistesi", PntList2D = deverlist.ToString(), state = "existing" };
            LandXMLAlignmentsAlignmentProfileProfSurf profSurf2 = new LandXMLAlignmentsAlignmentProfileProfSurf
            { name = "nok_ofset", PntList2D = nok_ofset.ToString(), state = "existing" };
            LandXMLAlignmentsAlignmentProfileProfSurf profSurf3 = new LandXMLAlignmentsAlignmentProfileProfSurf
            { name = "nok_kotfarki", PntList2D = nok_kotfarki.ToString(), state = "existing" };
            landXml.Alignments.Alignment.Profile.ProfSurf = new LandXMLAlignmentsAlignmentProfileProfSurf[]
            {
                profSurf,profSurf2,profSurf3
            };
            //landXml.Alignments.Alignment.Profile.ProfSurf = profSurf;





            return landXml;
        }



        public static string ucle(double sayi)
        {
            var sayiuc = String.Format("{0:0.000}", Math.Round(sayi, 3));
            return sayiuc;
        }


    }


}
