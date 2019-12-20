using System;
using System.Linq;
using System.Text;
using Common;
using Common.Enum;
using Common.Packages;
using System.Collections.Generic;

namespace XmlToKtb
{
    public static class PrfIcerigi
    {
        public static string Duzenleyici(LandXMLAlignmentsAlignment alignment, string dosyaAdi)
        {
            var dolguluDosyaAdi = dosyaAdi.PadLeft(16, '0');

            if (alignment.Profile == null) { return string.Empty; }
            StringBuilder prf = new StringBuilder("*41....+" + dolguluDosyaAdi + " 42....+000000000VALIGNM 43....+00000000STACOORD ").AppendLine();//1-CTR1 dosya ismi olacak
            List<Array> prfList = new List<Array>();
            var profAlign = alignment.Profile.ProfAlign;
            var profAlignItems = alignment.Profile.ProfAlign.Items.ToList();
            foreach (var item in profAlign.Items)
            {
                object oncekiItem;
                object sonrakiItem = null;
                if (profAlignItems.IndexOf(item) == 0)//ilk item
                {
                    oncekiItem = null;
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
                    //prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(guncelKot)+" ");

                    object[] array = { "STRAIGHT", guncelKm, guncelKot};
                    prfList.Add(array);
                }
                else if (sonItemIse)
                {
                    //prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+0000000000000EOP 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(guncelKot) + " ");
                    object[] array = { "EOP", guncelKm, guncelKot };
                    prfList.Add(array);
                }
                else

                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);
                    var g2 = (sonrakiKot - guncelKot) / (sonrakiKm - guncelKm);


                    bool ekstraNoktaVarIse = EkstraNoktaMi(oncekiKm, sonrakiKm, guncelKm, oncekiLength, sonrakiLength);

                    if (!ekstraNoktaVarIse)
                    {
                        var katSayiIsareti = g1 - g2 < 0 ? "+" : "-";
                        if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Line)
                        {
                            //prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(guncelKot) + " ");
                            object[] array = { "STRAIGHT", guncelKm, guncelKot };
                            prfList.Add(array);
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Parabola)
                        {

                            var kKatSayisi = guncelLength / Math.Abs(g1 - g2);
                            guncelKm = guncelKm - guncelLength / 2;
                            var geriKot = guncelKot - g1 * guncelLength / 2;
                            var ileriKot = guncelKot + g2 * guncelLength / 2;
                            //prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+" + "00000000PARABOLA 72...." + katSayiIsareti + Yardimci.SifirDolgusu(kKatSayisi) + " 83..10+" + Yardimci.SifirDolgusu(geriKot) + " ");
                            object[] array = { "PARABOLA", guncelKm, katSayiIsareti, kKatSayisi, geriKot };
                            prfList.Add(array);
                            //prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm + guncelLength) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(ileriKot) + " ");
                            object[] array1 = { "STRAIGHT", guncelKm+ guncelLength, ileriKot };
                            prfList.Add(array1);
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Curve)
                        {
                            var teget = guncelProfilAlign.Radius * (decimal) Math.Tan(Convert.ToDouble(guncelLength / guncelProfilAlign.Radius / 2));
                            var yatayLengthGeri = teget * (decimal) Math.Cos(Math.Atan((Math.Abs((double) g1))));
                            var yatayLengthİleri = teget * ((decimal)Math.Cos(Math.Atan((Math.Abs((double)g2)))));
                            var TOKm = guncelKm - yatayLengthGeri;
                            var TFKm = guncelKm + yatayLengthİleri;
                            var geriKot = guncelKot - g1 * yatayLengthGeri;
                            var ileriKot = guncelKot + g2 * yatayLengthİleri;
                            //prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(TOKm) + " 71....+" + "00000000000CURVE 72...." + katSayiIsareti + Yardimci.SifirDolgusu(guncelProfilAlign.Radius) + " 83..10+" + Yardimci.SifirDolgusu(geriKot) + " ");
                            object[] array = { "CURVE", TOKm, katSayiIsareti, guncelProfilAlign.Radius, geriKot };
                            prfList.Add(array);
                            if (Math.Abs(TFKm - sonrakiKm) > (decimal) 0.005)
                            {
                                //prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(TFKm) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(ileriKot) + " ");
                                object[] array1 = { "STRAIGHT", TFKm, ileriKot };
                                prfList.Add(array1);
                            }
                         
                        }
                    }
                }


            }
            for (int i = 0; i < prfList.Count - 1; i++)
            {
                var oncekiSatirKm = (Convert.ToDouble(prfList[i].GetValue(1)));
                var sonrakiSatirKm = (Convert.ToDouble(prfList[i + 1].GetValue(1)));
                if (Math.Abs(oncekiSatirKm - sonrakiSatirKm)>0.002)
                {
                    if ((Convert.ToString(prfList[i].GetValue(0))) == "STRAIGHT")
                    {
                        prf.AppendLine("*11....+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(1)))) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(2)))) + " ");
                    }
                    if ((Convert.ToString(prfList[i].GetValue(0))) == "PARABOLA")
                    {
                        prf.AppendLine("*11....+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(1)))) + " 71....+" + "00000000PARABOLA 72...." + (Convert.ToString(prfList[i].GetValue(2))) + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(3)))) + " 83..10+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(4)))) + " ");
                    }

                    if ((Convert.ToString(prfList[i].GetValue(0))) == "CURVE")
                    {
                        prf.AppendLine("*11....+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(1)))) + " 71....+" + "00000000000CURVE 72...." + (Convert.ToString(prfList[i].GetValue(2))) + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(3)))) + " 83..10+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i].GetValue(4)))) + " ");
                    }

                }
                if (i==(prfList.Count - 2))
                {
                    prf.AppendLine("*11....+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i+1].GetValue(1)))) + " 71....+0000000000000EOP 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu((Convert.ToString(prfList[i+1].GetValue(2)))) + " ");
                }
            }
            return prf.ToString();
        }

        private static bool EkstraNoktaMi(decimal oncekiKm, decimal sonrakiKm, decimal guncelKm, decimal oncekiLength, decimal sonrakiLength)
        {
            bool onceYakinNoktaVarMi = Math.Abs(oncekiKm + oncekiLength / 2 - guncelKm) < 0.1m;

            bool sonraYakinNotaVarMi = Math.Abs(sonrakiKm - sonrakiLength / 2 - guncelKm) < 0.1m;

            return onceYakinNoktaVarMi || sonraYakinNotaVarMi;
        }
    }
}
