using System;
using System.Linq;
using System.Text;

namespace XmlToKtb
{
    public static class ALNIcerigi
    {
        public static string Duzenleyici(LandXML landXml, string dosyaAdi)
        {
            var dolguluDosyaAdi = dosyaAdi.PadLeft(16, '0');

            var alignment = landXml.Alignments.Alignment;

            var aln = new StringBuilder("*41....+" + dolguluDosyaAdi + " 42....+00000000HZALIGNM 43....+00000000STACOORD").AppendLine(); //00000000001_CRT1 yazısı kayıtta yazılan dosya isimi olacak ve başına sıfır koyarak 16 hane olacak
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;

            foreach (var item in alignment.CoordGeom.Items)
            {
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine _:
                        {
                            guncelKM = LineYazdirALN(aln, (LandXMLAlignmentsAlignmentCoordGeomLine)item, guncelKM);
                            break;
                        }
                    case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                        {
                            guncelKM = CurveYazdirALN(aln, (LandXMLAlignmentsAlignmentCoordGeomCurve)item, guncelKM);
                            break;
                        }
                    case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                        {
                            guncelKM = SpiralYazdirALN(aln, (LandXMLAlignmentsAlignmentCoordGeomSpiral)item, guncelKM);
                            break;
                        }
                }
            }

            var lastItem = alignment.CoordGeom.Items.Last();

            var lastY = string.Empty;
            var lastX = string.Empty;

            switch (lastItem)
            {

                case LandXMLAlignmentsAlignmentCoordGeomLine _:
                    {
                        var line = (LandXMLAlignmentsAlignmentCoordGeomLine)lastItem;
                        lastY = line.End.Split()[1];
                        lastX = line.End.Split()[0];
                        break;
                    }
                case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                    {
                        var curve = (LandXMLAlignmentsAlignmentCoordGeomCurve)lastItem;
                        lastY = curve.End.Split()[1];
                        lastX = curve.End.Split()[0];
                        break;
                    }
                case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                    {
                        var spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral)lastItem;
                        lastY = spiral.End.Split()[1];
                        lastX = spiral.End.Split()[0];
                        break;
                    }
            }

            var sign = guncelKM >= 0 ? "+" : "-";

            aln.AppendLine("*11...." + sign + Yardimci.SifirDolgusu(guncelKM) + " 71....+0000000000000EOP 72....+0000000000000NON 73....+0000000000000000" + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(lastY)) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(lastX)));

            return aln.ToString();

        }

        private static decimal LineYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomLine line, decimal guncelKm)
        {
            aln.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+00000000STRAIGHT 72....+0000000000000NON 73....+0000000000000000" + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(line.Start.Split()[1])) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(line.Start.Split()[0])));

            return guncelKm + line.length;
        }
        private static decimal CurveYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomCurve curve, decimal guncelKm)
        {
            var yon = curve.rot == "ccw" ? "-" : "+";
            aln.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+00000000000CURVE" + " 72...." + yon + Yardimci.SifirDolgusu(Convert.ToDecimal(curve.radius)) + " 73....+0000000000000000" + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(curve.Start.Split()[1])) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(curve.Start.Split()[0])));
            return guncelKm + curve.length;
        }
        private static decimal SpiralYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            var yon = spiral.rot == "ccw" ? "-" : "+";
            if (spiral.radiusStart.ToUpper() == "INF")
            {
                var a = Math.Sqrt(Convert.ToDouble(Convert.ToDecimal(spiral.radiusEnd) * spiral.length));
                aln.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+0000000000SPIRIN" + " 72...." + yon + Yardimci.SifirDolgusu(a) + " 73....+0000000000000000" + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));
            }
            else if (spiral.radiusEnd.ToUpper() == "INF")
            {
                var a = Math.Sqrt(Convert.ToDouble(Convert.ToDecimal(spiral.radiusStart) * spiral.length));
                aln.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+000000000SPIROUT" + " 72...." + yon + Yardimci.SifirDolgusu(a) + " 73....+0000000000000000" + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));
            }
            else
            {
                if (Convert.ToDecimal(spiral.radiusStart) >= Convert.ToDecimal(spiral.radiusEnd))
                {
                    aln.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+000000000CURVEIN" + " 72...." + yon + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.radiusStart)) + " 73....+0000000000000000" + " 74...." + yon + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.radiusEnd)) + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));
                }
                else
                {
                    aln.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+00000000CURVEOUT" + " 72...." + yon + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.radiusStart)) + " 73....+0000000000000000" + " 74...." + yon + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.radiusEnd)) + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));
                }
            }

            return guncelKm + spiral.length;
        }
    }
}
