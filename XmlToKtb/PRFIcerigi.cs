using System;
using System.Linq;
using System.Text;

namespace XmlToKtb
{
    public static class PRFIcerigi
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
                object sonrakiItem;
                if (profAlignItems.IndexOf(item) == 0)//ilk item
                {
                    oncekiItem = null;
                    sonrakiItem = profAlignItems[profAlignItems.IndexOf(item) + 1];
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

                var detaylar = Yardimci.GetirProfilAlignDetaylar(item);
                var oncekiDetaylar = ilkItemIse ? new decimal[] { 0, 0, 0 } : Yardimci.GetirProfilAlignDetaylar(oncekiItem);
                var sonrakiDetaylar = sonItemIse ? new decimal[] { 0, 0, 0 } : Yardimci.GetirProfilAlignDetaylar(sonrakiItem);
                decimal oncekiKM = oncekiDetaylar[0];
                decimal oncekiKOT = oncekiDetaylar[1];

                decimal sonrakiKM = sonrakiDetaylar[0];
                decimal sonrakiKOT = sonrakiDetaylar[1];

                decimal KM = detaylar[0];
                decimal KOT = detaylar[1];
                decimal Length = detaylar[2];
                //TODO Ali Sarıkoç hesaplamalarını burada yap onceki üç ve sonraki üç değer yukarıda oluşturdm onları kullanarak hesaplama yap.
                if (ilkItemIse)
                {
                    prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(KM) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(KOT));
                }
                else if (sonItemIse)
                {
                    prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(KM) + " 71....+0000000000000EOP 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(KOT));
                }
                else

                {
                    var g1 = (KOT - oncekiKOT) / (KM - oncekiKM);
                    var g2 = (sonrakiKOT - KOT) / (sonrakiKM - KM);
                    if (Math.Abs(g1 - g2) > 0.001m)
                    {
                        KM = KM - Length / 2;
                        KOT = KOT - g1 * Length / 2;
                        if (Length == 0)
                        {
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(KM) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + Yardimci.SifirDolgusu(KOT));
                        }
                        else if (detaylar[3] == 1)
                        {
                            var K100 = 100 * Length / Math.Abs(g1 - g2);
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(KM) + " 71....+" + "00000000PARABOLA 72....+" + Yardimci.SifirDolgusu(K100) + " 83..10+" + Yardimci.SifirDolgusu(KOT));
                        }
                        else if (detaylar[3] == 2)
                        {
                            prf.AppendLine("*11....+" + Yardimci.SifirDolgusu(KM) + " 71....+" + "00000000000CURVE 72....+" + Yardimci.SifirDolgusu(detaylar[4]) + " 83..10+" + Yardimci.SifirDolgusu(KOT));
                        }
                    }
                }
            }
            return prf.ToString();
        }
    }
}
