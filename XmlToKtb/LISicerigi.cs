using System;
using System.Text;

namespace XmlToKtb
{
    public static class LISIcerigi
    {
        
        public static string Duzenleyici(LandXML landXml, string dosyaAdi)
        {

            var alignment = landXml.Alignments.Alignment;

            var aln = new StringBuilder(alignment.name).AppendLine(); //00000000001_CRT1 yazısı kayıtta yazılan dosya isimi olacak ve başına sıfır koyarak 16 hane olacak
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;

            foreach (var item in alignment.CoordGeom.Items)
            {
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine _:
                        {
                            aln.Append("\n" +alignment.name +" ");
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


            return aln.ToString();

        }

        private static decimal LineYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomLine line, decimal guncelKm)
        {
            //aln.AppendLine("*11....+" + Yardimci.SifirDolgusu(guncelKm) + " 71....+00000000STRAIGHT 72....+0000000000000NON 73....+0000000000000000" + " 81..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(line.Start.Split()[1])) + " 82..10+" + Yardimci.SifirDolgusu(Convert.ToDecimal(line.Start.Split()[0])) + " ");

            return guncelKm + line.length;
        }
        private static decimal CurveYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomCurve curve, decimal guncelKm)
        {
            var yon = curve.rot == "ccw" ? "SOL" : "SAĞ";
            var koor =Convert.ToString(curve.Start.Split()[1]);
            aln.Append(Convert.ToString(guncelKm)+" "+Convert.ToString(curve.Start.Split()[1])+" "+ Convert.ToString(curve.Start.Split()[0])+" "+ Convert.ToString(curve.radius) +" "+ yon+" "+" "+ Convert.ToString(guncelKm+curve.length)+" "+ Convert.ToString(curve.End.Split()[1])+" "+ Convert.ToString(curve.End.Split()[0])+" ");
            return guncelKm + curve.length;
        }
        private static decimal SpiralYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            if (spiral.radiusStart.ToUpper() == "INF")
            {
               
                aln.Append(Convert.ToString(guncelKm) + " " + Convert.ToString(spiral.Start.Split()[1]) + " " + Convert.ToString(spiral.Start.Split()[0]) + " "  + Convert.ToString(spiral.length) + " "); 
            }
            else if (spiral.radiusEnd.ToUpper() == "INF")
            {
                aln.Append(Convert.ToString(guncelKm + spiral.length) + " " + Convert.ToString(spiral.End.Split()[1]) + " " + Convert.ToString(spiral.End.Split()[0]) + " " + Convert.ToString(spiral.length) + " ");
            }
            else
            {
                if (Convert.ToDecimal(spiral.radiusStart) >= Convert.ToDecimal(spiral.radiusEnd))
                {
                    aln.Append(Convert.ToString(guncelKm) + " " + Convert.ToString(spiral.Start.Split()[1]) + " " + Convert.ToString(spiral.Start.Split()[0]) + " " + Convert.ToString(spiral.length) + " ");
                }
                else
                {
                    aln.Append(Convert.ToString(guncelKm + spiral.length) + " " + Convert.ToString(spiral.End.Split()[1]) + " " + Convert.ToString(spiral.End.Split()[0]) + " " + Convert.ToString(spiral.length) + " ");
                }
            }

            return guncelKm + spiral.length;
        }
    }
}
