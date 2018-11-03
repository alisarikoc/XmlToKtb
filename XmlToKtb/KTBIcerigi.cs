using System;
using System.Globalization;
using System.Text;

namespace XmlToKtb
{
    public static class KTBIcerigi
    {
        public static string Duzenleyici(LandXML landXml)
        {
            var alignment = landXml.Alignments.Alignment;

            var ktb = new StringBuilder("$KM0").AppendLine();
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;
            ktb.AppendLine(baslangicKM.ToString(CultureInfo.InvariantCulture));
            ktb.AppendLine("$SON");
            ktb.AppendLine("$ELEMENTS");

            foreach (var item in alignment.CoordGeom.Items)
            {
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine _:
                    {
                        guncelKM = LineYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomLine)item, guncelKM);
                        break;
                    }
                    case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                    {
                        guncelKM = CurveYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomCurve)item, guncelKM);
                        break;
                    }
                    case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                    {
                        guncelKM = SpiralYazdirKmGuncelle(ktb, (LandXMLAlignmentsAlignmentCoordGeomSpiral)item, guncelKM);
                        break;
                    }
                }
            }

            ktb.AppendLine("$SON");

            ktb.AppendLine("$DUSEY");

            duseyBilgileriDoldur(alignment, ktb);

            ktb.AppendLine("$SON");

            return ktb.ToString();
        }

        private static decimal LineYazdirKmGuncelle(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomLine line, decimal guncelKm)
        {
            ktb.AppendLine("LN " + guncelKm + " " + line.Start.Split()[1] + " " + line.Start.Split()[0] + " " + line.End.Split()[1] + " " + line.End.Split()[0]);

            return guncelKm + line.length;
        }

        private static decimal CurveYazdirKmGuncelle(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomCurve curve, decimal guncelKm)
        {
            var yon = curve.rot == "ccw" ? "-1" : "1";
            ktb.AppendLine("AR " + guncelKm + " " + curve.radius + " " + yon + " 0 0 " + curve.Center.Split()[1] + " " + curve.Center.Split()[0] + " " + curve.Start.Split()[1] + " " + curve.Start.Split()[0] + " " + curve.End.Split()[1] + " " + curve.End.Split()[0]);
            return guncelKm + curve.length;
        }

        private static decimal SpiralYazdirKmGuncelle(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            if (spiral.radiusStart.ToUpper() == "INF")
            {
                SpiralIn(ktb, spiral, guncelKm);

            }
            else if (spiral.radiusEnd.ToUpper() == "INF")
            {
                SpiralOut(ktb, spiral, guncelKm);
            }
            else
            {
                if (Convert.ToDecimal(spiral.radiusStart) >= Convert.ToDecimal(spiral.radiusEnd))
                {
                    CurveIn(ktb, spiral, guncelKm);
                }
                else
                {
                    CurveOut(ktb, spiral, guncelKm);
                }
            }
            return guncelKm + spiral.length;
        }

        private static void SpiralIn(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral,
            decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "-1" : "1";
            var aKare = Convert.ToDecimal(spiral.radiusEnd) * spiral.length;
            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " 1 " + spiral.Start.Split()[1] + " " + spiral.Start.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");
        }
        private static void SpiralOut(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral,
            decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "1" : "-1";
            var aKare = Convert.ToDecimal(spiral.radiusStart) * spiral.length;
            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " -1 " + spiral.End.Split()[1] + " " + spiral.End.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");
        }
        private static void CurveIn(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral,
            decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "-1" : "1";
            var aKare = Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length;

            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " 1 " + spiral.Start.Split()[1] + " " + spiral.Start.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");
        }
        private static void CurveOut(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "1" : "-1";
            var aKare = Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length;

            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " -1 " + spiral.End.Split()[1] + " " + spiral.End.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");

        }
        private static void duseyBilgileriDoldur(LandXMLAlignmentsAlignment alignment, StringBuilder sb)
        {
            if (alignment.Profile == null) { return; }

            var profAlign = alignment.Profile.ProfAlign;
            foreach (var item in profAlign.Items)
            {
                switch (item)
                {
                    case string _:
                        {
                            var ikiDeger = item.ToString().Split();
                            var ilkDeger = Yardimci.Yuvarla(ikiDeger[0]);
                            var ikinciDeger = Yardimci.Yuvarla(ikiDeger[1]);
                            sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  0  0");

                            break;
                        }
                    case LandXMLAlignmentsAlignmentProfileProfAlignParaCurve _:
                        {
                            var paraCurve = (LandXMLAlignmentsAlignmentProfileProfAlignParaCurve)item;
                            var ikiDeger = paraCurve.Value.Split();

                            var ilkDeger = Yardimci.Yuvarla(ikiDeger[0]);
                            var ikinciDeger = Yardimci.Yuvarla(ikiDeger[1]);
                            var length = Yardimci.Yuvarla(paraCurve.length);
                            sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  " + length + "  1");
                            break;
                        }
                    case LandXMLAlignmentsAlignmentProfileProfAlignCircCurve _:
                        {
                            var circCurve = (LandXMLAlignmentsAlignmentProfileProfAlignCircCurve)item;
                            var ikiDeger = circCurve.Value.Split();

                            var ilkDeger = Yardimci.Yuvarla(ikiDeger[0]);
                            var ikinciDeger = Yardimci.Yuvarla(ikiDeger[1]);
                            var length = Yardimci.Yuvarla(circCurve.length);
                            sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  " + length + "  1");
                            break;
                        }
                }
            }
        }
    }
}
