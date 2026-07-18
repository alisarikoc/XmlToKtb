using Common;
using Common.Enum;
using Common.Packages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace XmlToKtb
{
    public static class GeoCevir
    {
        public static string Duzenleyici(LandXML landXml)
        {
            var alignment = landXml.Alignments.Alignment;

            List<Array> eksKaydirList = new List<Array>();
            var ktb = new StringBuilder("SollGeom             1.0Forward__            ======EndOfFile").AppendLine();
            ktb.AppendLine("========= BeginOfTab =========");
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;
            //ktb.AppendLine(baslangicKM.ToString(CultureInfo.InvariantCulture));

            foreach (var item in alignment.CoordGeom.Items)
            {
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine _:
                        {
                            var line = (LandXMLAlignmentsAlignmentCoordGeomLine) item;

                            guncelKM = LineYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomLine) item, guncelKM);
                            break;
                        }
                    case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                        {
                            var curve = (LandXMLAlignmentsAlignmentCoordGeomCurve) item;
                            var deverlerSC = TcddteknikIcerigi.GetirDeverVeYon(alignment, guncelKM);
                            var yon = "";
                            if (deverlerSC.GetValue(1) == "cw") yon = "-";
                            var dever = Convert.ToDouble(deverlerSC.GetValue(0));

                            guncelKM = CurveYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomCurve) item, guncelKM,yon,dever);
                            break;
                        }
                    case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                        {
                            var spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral) item;
                            guncelKM = SpiralYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomSpiral) item, guncelKM);

                            break;
                        }
                }
    
                
            }
            var lastItem = alignment.CoordGeom.Items.Last();

            switch (lastItem)
            {

                case LandXMLAlignmentsAlignmentCoordGeomLine _:
                    {
                        var line = (LandXMLAlignmentsAlignmentCoordGeomLine) lastItem; 
                        guncelKM = LineYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomLine) lastItem, guncelKM);
                        break;
                    }
                case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                    {
                        var curve = (LandXMLAlignmentsAlignmentCoordGeomCurve) lastItem;
                        var deverlerSC = TcddteknikIcerigi.GetirDeverVeYon(alignment, guncelKM);
                        var yon = "";
                        if (deverlerSC.GetValue(1) == "cw") yon = "-";
                        var dever = Convert.ToDouble(deverlerSC.GetValue(0));

                        guncelKM = CurveYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomCurve) lastItem, guncelKM, yon, dever);
                        break;
                    }
                case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                    {
                        var spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral) lastItem;
                        guncelKM = SpiralYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomSpiral) lastItem, guncelKM);
                        break;
                    }
            }
            ktb.AppendLine("========== EndOfTab ==========");

            ktb.AppendLine("========= BeginOfTab =========");
            prfDuzenleyici(alignment, ktb);

            ktb.Append("========== EndOfTab ==========");
            return ktb.ToString();
        }
        public static string prfDuzenleyici(LandXMLAlignmentsAlignment alignment, StringBuilder ktb)
        {

            if (alignment.Profile == null) { return null; }
            var profAlign = alignment.Profile.ProfAlign;
            var profAlignItems = alignment.Profile.ProfAlign.Items.ToList();
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

                decimal oncekiKm = oncekiProfilAlign.KM;
                decimal oncekiKot = oncekiProfilAlign.Kot;
                decimal oncekiLength = oncekiProfilAlign.Length;

                decimal sonrakiKm = sonrakiProfilAlign.KM;
                decimal sonrakiKot = sonrakiProfilAlign.Kot;
                decimal sonrakiLength = sonrakiProfilAlign.Length;

                decimal guncelKm = guncelProfilAlign.KM;
                decimal guncelKot = guncelProfilAlign.Kot;
                decimal guncelLength = guncelProfilAlign.Length;

                if (oncekiLength == 0)
                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);

                    var Length = guncelKm - guncelLength / 2;
                    ktb.AppendLine(Math.Round(guncelKm, 3) + " " + "0.00000e+000" + " " + "0.00000e+000" + " " + g1.ToString("0.00000e+000") + " " + "480");
                }



                //TODO Ali Sarıkoç hesaplamalarını burada yap onceki üç ve sonraki üç değer yukarıda oluşturdm onları kullanarak hesaplama yap.
                if (ilkItemIse)
                {

                    var g2 = (sonrakiKot - guncelKot) / (sonrakiKm - guncelKm);
                    ktb.AppendLine(Math.Round(guncelKm, 3) + " " + "0.00000e+000" + " " + (guncelProfilAlign.Length/2).ToString("0.00000e+000") + " " + g2.ToString("0.00000e+000") + " " + "480");

                }
                else if (sonItemIse)
                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);
                    ktb.AppendLine(Math.Round(guncelKm, 3) + " " + "0.00000e+000" + " " + "0.00000e+000" + " " + g1.ToString("0.00000e+000") + " " + "478");
                    break;
                }
                else

                {
                    var g1 = (guncelKot - oncekiKot) / (guncelKm - oncekiKm);
                    var g2 = (sonrakiKot - guncelKot) / (sonrakiKm - guncelKm);
                    ktb.AppendLine(Math.Round(guncelKm, 3) + " " + "0.00000e+000" + " " + (guncelProfilAlign.Length / 2).ToString("0.00000e+000") + " " + g2.ToString("0.00000e+000") + " " + "478");
                }
            }

            return ktb.ToString();
        }


        private static decimal LineYazdirKmGuncelle(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomLine line, decimal guncelKm)
        {
            ktb.AppendLine(Math.Round(guncelKm,3) + " " + "0.00000e+000" + " " + "0.00000e+000" + " " + "449"+" "+"464");
            return guncelKm + line.length;
        }

        private static decimal CurveYazdirKmGuncelle(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomCurve curve, decimal guncelKm,string yon,double dever)
        {
            ktb.AppendLine(Math.Round(guncelKm, 3) + " " +yon+ curve.radius.ToString("0.00000e+000") + " " +yon+ dever.ToString("0.00000e+000") + " " + "450" +" "+ "464");
            return guncelKm + curve.length;
        }

        private static decimal SpiralYazdirKmGuncelle(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            ktb.AppendLine(Math.Round(guncelKm, 3) + " " + spiral.length.ToString("0.00000e+000") + " " + spiral.length.ToString("0.00000e+000") + " " + "451" +" "+ "465");
            return guncelKm + spiral.length;
        }


    }
}
