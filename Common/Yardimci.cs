using System;
using System.Collections.Generic;
using System.Linq;
using Common.Enum;
using Common.Modeller;
using Common.Packages;
using DXFLib;

namespace Common
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
                case LandXMLAlignmentsAlignmentProfileProfAlignPVI _:
                    {
                        var paraline = (LandXMLAlignmentsAlignmentProfileProfAlignPVI) item;

                        var ikiDeger = paraline.Value.Split();

                        profilAlignPackage.KM = Convert.ToDecimal(ikiDeger[0]);

                        profilAlignPackage.Kot = Convert.ToDecimal(ikiDeger[1]);

                        profilAlignPackage.Length = 0;

                        profilAlignPackage.ProfilAlignTip = ProfileAlignTip.Line;

                        break;
                    }

                case LandXMLAlignmentsAlignmentProfileProfAlignParaCurve _:
                    {

                        var paraCurve = (LandXMLAlignmentsAlignmentProfileProfAlignParaCurve) item;

                        var ikiDeger = paraCurve.Value.Split();

                        profilAlignPackage.KM = Convert.ToDecimal(ikiDeger[0]);

                        profilAlignPackage.Kot = Convert.ToDecimal(ikiDeger[1]);

                        profilAlignPackage.Length = paraCurve.length;

                        profilAlignPackage.ProfilAlignTip = ProfileAlignTip.Parabola;

                        break;

                    }

                case LandXMLAlignmentsAlignmentProfileProfAlignCircCurve _:
                    {
                        var circCurve = (LandXMLAlignmentsAlignmentProfileProfAlignCircCurve) item;

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

        public static CantPackage GetirCantDetaylar(object item)
        {
            var CantPackage = new CantPackage();

            switch (item)
            {
                case LandXMLAlignmentsAlignmentCantCantStation _:
                    {
                        var satir = (LandXMLAlignmentsAlignmentCantCantStation) item;

                        CantPackage.KM = satir.station;

                        CantPackage.Cant = satir.appliedCant;

                        CantPackage.Yon = satir.curvature;


                        break;
                    }
            }

            return CantPackage;
        }
    }

    public class CantPackage
    {
        public decimal KM
        {
            get; set;
        }

        public decimal Cant
        {
            get; set;
        }

        public string Yon
        {
            get; set;
        }
    }
}
