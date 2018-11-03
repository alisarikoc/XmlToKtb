using System;
using Common.Enum;
using Common.Packages;

namespace XmlToKtb
{
    public static class Yardimci
    {
        public static decimal Yuvarla(object deger)
        {
            return Math.Round(Convert.ToDecimal(deger.ToString()), 4, MidpointRounding.AwayFromZero);
        }
        public static string SifirDolgusu(object deger)
        {
            return Convert.ToInt64(Math.Abs(Convert.ToDecimal(deger) * 1000)).ToString().PadLeft(16, '0');
        }

        public static ProfilAlignPackage GetirProfilAlignDetaylar(object item)
        {
            var profilAlignPackage = new ProfilAlignPackage();

            switch (item)
            {
                case string _:
                    {
                        var ikiDeger = item.ToString().Split();

                        profilAlignPackage.KM = Yuvarla(ikiDeger[0]);

                        profilAlignPackage.Kot = Yuvarla(ikiDeger[1]);

                        profilAlignPackage.Length = 0m;

                        profilAlignPackage.ProfilAlignTip = ProfileAlignTip.Line;

                        break;
                    }

                case LandXMLAlignmentsAlignmentProfileProfAlignParaCurve _:
                    {

                        var paraCurve = (LandXMLAlignmentsAlignmentProfileProfAlignParaCurve)item;

                        var ikiDeger = paraCurve.Value.Split();

                        profilAlignPackage.KM = Convert.ToDecimal(ikiDeger[0]);

                        profilAlignPackage.Kot = Convert.ToDecimal(ikiDeger[1]);

                        profilAlignPackage.Length = paraCurve.length;

                        profilAlignPackage.ProfilAlignTip = ProfileAlignTip.Parabola;

                        break;

                    }

                case LandXMLAlignmentsAlignmentProfileProfAlignCircCurve _:
                    {
                        var circCurve = (LandXMLAlignmentsAlignmentProfileProfAlignCircCurve)item;

                        var ikiDeger = circCurve.Value.Split();
                        profilAlignPackage.KM = Convert.ToDecimal(ikiDeger[0]);

                        profilAlignPackage.Kot = Convert.ToDecimal(ikiDeger[1]);

                        profilAlignPackage.Length = circCurve.length;
                        profilAlignPackage.ProfilAlignTip = ProfileAlignTip.Curve;
                        profilAlignPackage.Radius = Yuvarla(circCurve.radius);

                        break;
                    }
            }

            return profilAlignPackage;
        }
    }
}
