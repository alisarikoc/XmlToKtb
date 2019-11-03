using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Enum;
using Common.Packages;

namespace XmlToKtb
{
    public static class Prfhesap
    {
        public static decimal Duzenleyici(LandXMLAlignmentsAlignment alignment, decimal kilometre)
        {
            if (alignment.Profile == null) { return 0; }
            var profAlign = alignment.Profile.ProfAlign;
            var profAlignItems = alignment.Profile.ProfAlign.Items.ToList();
            decimal kirmizikot = 0;
            List<Array> Profilsatir = new List<Array>();
            foreach (var item in profAlign.Items)
            {
                object oncekiItem;
                object sonrakiItem = null;
                if (profAlignItems.IndexOf(item) == 0)//ilk item
                {
                    oncekiItem = null;
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

                decimal oncekiKm = oncekiProfilAlign.KM;
                decimal oncekiKot = oncekiProfilAlign.Kot;
                decimal oncekiLength = oncekiProfilAlign.Length;

                decimal sonrakiKm = sonrakiProfilAlign.KM;
                decimal sonrakiKot = sonrakiProfilAlign.Kot;
                decimal sonrakiLength = sonrakiProfilAlign.Length;

                decimal guncelKm = guncelProfilAlign.KM;
                decimal guncelKot = guncelProfilAlign.Kot;
                decimal guncelLength = guncelProfilAlign.Length;




                //TODO Ali Sarıkoç hesaplamalarını burada yap onceki üç ve sonraki üç değer yukarıda oluşturdm onları kullanarak hesaplama yap.
                if (ilkItemIse)
                {

                    var g1 = (sonrakiKot - guncelKot) / (sonrakiKm - guncelKm);
                    
                    var Length = sonrakiKm - sonrakiLength / 2;
                    object[] array = { guncelKm, guncelKot, "Line", g1,0,Length,0 };
                    Profilsatir.Add(array);

                }
                else if (sonItemIse)
                {
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
                        if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Line)
                        {
                            object[] array = { guncelKm, guncelKot, "Line", g2, 0, (sonrakiKm - sonrakiLength / 2) - guncelKm, 0 };
                            Profilsatir.Add(array);
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Parabola)
                        { 
                            object[] array = { guncelKm , geriKot, "parabola", g1, g2, guncelLength, 0 };
                            Profilsatir.Add(array);
                            object[] array1 = { guncelKm+guncelLength , ileriKot, "Line", g2, 0, sonrakiLength, 0 };
                            Profilsatir.Add(array1);
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Curve)
                        {
                            object[] array = { guncelKm , geriKot, "curve", g1, g2, guncelLength, guncelProfilAlign.Radius };
                            Profilsatir.Add(array);
                            object[] array1 = { guncelKm + guncelLength, ileriKot, "Line", g2, 0, sonrakiLength, 0 };
                            Profilsatir.Add(array1);
                        }
                    }
                }

                

            }

            for (var i = 0; i < Profilsatir.Count - 1; i++)
            {
                decimal KM = Convert.ToDecimal(Profilsatir[i].GetValue(0));
                decimal Kot = Convert.ToDecimal(Profilsatir[i].GetValue(1));
                string Tip = Convert.ToString(Profilsatir[i].GetValue(2));
                decimal g1 = Convert.ToDecimal(Profilsatir[i].GetValue(3));
                decimal g2 = Convert.ToDecimal(Profilsatir[i].GetValue(4));
                decimal L = Convert.ToDecimal(Profilsatir[i].GetValue(5));
                decimal Radius = Convert.ToDecimal(Profilsatir[i].GetValue(6));
                decimal ileriKM = Convert.ToDecimal(Profilsatir[i + 1].GetValue(0));
                if (kilometre > KM && kilometre < ileriKM)
                {
                    var x = kilometre - KM;
                    var r = g1 * x;
                    if (Tip == "Line")
                    {
                        kirmizikot = r + Kot;
                        break;
                    }
                    if (Tip == "parabola")
                    {
                        var y = (g1 - g2 / 2 / L * x * x);
                        kirmizikot = Kot + r - y;
                        break;
                    }
                    if (Tip == "curve")
                    {
                        var y = (x * x / 2 / Radius);
                        kirmizikot = Kot + r - y;
                    if((g2-g1)>0) kirmizikot = Kot + r + y;

                        break;
                    }
                }

            }

            return kirmizikot;
        }

        private static bool EkstraNoktaMi(decimal oncekiKm, decimal sonrakiKm, decimal guncelKm, decimal oncekiLength, decimal sonrakiLength)
        {
            bool onceYakinNoktaVarMi = Math.Abs(oncekiKm + oncekiLength / 2 - guncelKm) < 0.1m;

            bool sonraYakinNotaVarMi = Math.Abs(sonrakiKm - sonrakiLength / 2 - guncelKm) < 0.1m;

            return onceYakinNoktaVarMi || sonraYakinNotaVarMi;
        }

    }

}
