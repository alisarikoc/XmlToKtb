using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlToKtb
{
    public static class Yardimci
    {
        public static decimal Yuvarla(object deger)
        {
            return Math.Round(Convert.ToDecimal(deger.ToString()), 4, MidpointRounding.AwayFromZero);
        }
        public static string SifirDolgusu(decimal deger)
        {
            return Convert.ToInt64(Math.Abs(deger * 1000)).ToString().PadLeft(16, '0');
        }

        public static decimal[] GetirProfilAlignDetaylar(object item)
        {
            var detaylar = new decimal[5];

            switch (item)
            {
                case string _:
                {
                    var ikiDeger = item.ToString().Split();

                    detaylar[0] = Yardimci.Yuvarla(ikiDeger[0]);

                    detaylar[1] = Yardimci.Yuvarla(ikiDeger[1]);

                    detaylar[2] = 0m;

                    break;
                }

                case LandXMLAlignmentsAlignmentProfileProfAlignParaCurve _:
                {

                    var paraCurve = (LandXMLAlignmentsAlignmentProfileProfAlignParaCurve)item;

                    var ikiDeger = paraCurve.Value.Split();

                    detaylar[0] = Yardimci.Yuvarla(ikiDeger[0]);

                    detaylar[1] = Yardimci.Yuvarla(ikiDeger[1]);

                    detaylar[2] = Yardimci.Yuvarla(paraCurve.length);
                    detaylar[3] = 1m;

                    break;

                }

                case LandXMLAlignmentsAlignmentProfileProfAlignCircCurve _:
                {
                    var circCurve = (LandXMLAlignmentsAlignmentProfileProfAlignCircCurve)item;

                    var ikiDeger = circCurve.Value.Split();
                    detaylar[0] = Yardimci.Yuvarla(ikiDeger[0]);

                    detaylar[1] = Yardimci.Yuvarla(ikiDeger[1]);

                    detaylar[2] = Yardimci.Yuvarla(circCurve.length);
                    detaylar[3] = 2m;
                    detaylar[4] = Yardimci.Yuvarla(circCurve.radius);

                    break;
                }
            }

            return detaylar;
        }
    }
}
