using System;
using System.Linq;
using System.Text;
using Common.Enum;
using Common.Packages;

namespace XmlToKtb
{
    public static class PrfIcerigi
    {
        public static string Duzenleyici(LandXMLAlignmentsAlignment alignment, string dosyaAdi)
        {
            var dolguluDosyaAdi = dosyaAdi.PadLeft(16, '0');

            if (alignment.Profile == null) { return string.Empty; }
            StringBuilder prf = new StringBuilder("*41....+" + dolguluDosyaAdi + " 42....+000000000VALIGNM 43....+00000000STACOORD").AppendLine();//1-CTR1 dosya ismi olacak
            var profAlign = alignment.Profile.ProfAlign;
            var profAlignItems = alignment.Profile.ProfAlign.Items.ToList();
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
                    sonrakiItem = null;
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
                    prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(guncelKot));
                }
                else if (sonItemIse)
                {
                    prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+0000000000000EOP 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(guncelKot));
                }
                else

                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);
                    var g2 = (sonrakiKot - guncelKot) / (sonrakiKm - guncelKm);


                    bool ekstraNoktaVarIse = EkstraNoktaMi(oncekiKm, guncelLength, sonrakiKm, guncelKm, oncekiLength, sonrakiLength);

                    if (!ekstraNoktaVarIse)
                    {
                        var katSayiIsareti = g1 - g2 < 0 ? "+" : "-";
                        guncelKm = guncelKm - guncelLength / 2;
                        var geriKot = guncelKot - g1 * guncelLength / 2;
                        var ileriKot = guncelKot + g2 * guncelLength / 2;
                        if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Line)
                        {
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(guncelKot));
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Parabola)
                        {

                            var kKatSayisi = guncelLength / Math.Abs(g1 - g2);
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+" + "00000000PARABOLA 72...." + katSayiIsareti + Yardimci.SifirDolgusu(kKatSayisi) + " 83..10+" + Yardimci.SifirDolgusu(geriKot));
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm + guncelLength) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(ileriKot));
                        }
                        else if (guncelProfilAlign.ProfilAlignTip == ProfileAlignTip.Curve)
                        {
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+" + "00000000000CURVE 72...." + katSayiIsareti + Yardimci.SifirDolgusu(guncelProfilAlign.Radius) + " 83..10+" + Yardimci.SifirDolgusu(geriKot));
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm + guncelLength) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(ileriKot));
                        }
                    }
                }
            }
            return prf.ToString();
        }

        private static bool EkstraNoktaMi(decimal oncekiKm, decimal guncelLength, decimal sonrakiKm, decimal guncelKm, decimal oncekiLength, decimal sonrakiLength)
        {
            bool onceYakinNoktaVarMi = Math.Abs(oncekiKm + oncekiLength / 2 - guncelKm) < 0.1m;

            bool sonraYakinNotaVarMi = Math.Abs(sonrakiKm - sonrakiLength / 2 - guncelKm) < 0.1m;

            return onceYakinNoktaVarMi || sonraYakinNotaVarMi;
        }
    }
}
