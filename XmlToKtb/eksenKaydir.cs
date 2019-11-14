using System;
namespace XmlToKtb
{
    public static class EKSENkaydir
    {

        public static double eksenkaydirGetir(LandXML landXml, decimal kilometre)
        {
            var alignment = landXml.Alignments.Alignment;
            //var kse = new StringBuilder("2  @=COLOR=SECTION=DISPLAY=-12796356").AppendLine();
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;
            decimal arananKm=0 ;
            double eksenKaydir = 0;

                arananKm = kilometre;

                foreach (var item in alignment.CoordGeom.Items)
                {
                    switch (item)
                    {
                        case LandXMLAlignmentsAlignmentCoordGeomLine _:
                        {
                            var line = (LandXMLAlignmentsAlignmentCoordGeomLine) item;

                            var Length = (Convert.ToDouble(line.length));
                            if ((arananKm - guncelKM) > 0 && (arananKm - guncelKM) < Convert.ToDecimal(Length))
                            {
                                eksenKaydir = 0;
                            }

                            guncelKM = guncelKM + line.length;
                            break;
                        }
                        case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                        {
                            var curve = (LandXMLAlignmentsAlignmentCoordGeomCurve) item;
                            var radius = (Convert.ToDouble(curve.radius));
                            var Length = (Convert.ToDouble(curve.length));
                            if ((arananKm - guncelKM) > 0 && (arananKm - guncelKM) < Convert.ToDecimal(Length))
                            {


                                if (curve.rot == "cw")
                                {
                                    eksenKaydir = sapmaHesabi(radius);
                                }

                                if (curve.rot == "ccw")
                                {
                                    eksenKaydir = sapmaHesabi(radius) * -1;
                                }
                            }

                            guncelKM = guncelKM + curve.length;
                            break;
                        }
                        case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                        {
                            var Spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral) item;
                            var yon = Spiral.rot;
                            var Length = (Convert.ToDouble(Spiral.length));
                            if (Spiral.radiusStart.ToUpper() == "INF") // GİRİŞ SPİRAL
                            {
                                    var aKare = Convert.ToDouble(Spiral.radiusEnd) * Length;
                                    if ((arananKm - guncelKM) > 0 && (arananKm - guncelKM) < Convert.ToDecimal(Length))
                                    {
                                        var L_point = Convert.ToDouble(arananKm - guncelKM);
                                        double R_point = aKare / L_point;
                                        eksenKaydir = sapmaHesabi(R_point);
                                        if (yon == "ccw") eksenKaydir = eksenKaydir * -1;
                                    }
                            }

                            if (Spiral.radiusEnd.ToUpper() == "INF") // ÇIKIŞ SPİRAL
                            {
                                    var aKare = Convert.ToDouble(Spiral.radiusStart) * Length;

                                        if ((arananKm - guncelKM) > 0 &&(arananKm - guncelKM) < Convert.ToDecimal(Length))
                                        {
                                            var L_point = Length-(Convert.ToDouble(arananKm - guncelKM));
                                            var R_point = aKare / L_point;
                                            eksenKaydir = sapmaHesabi(R_point);
                                            if (yon == "ccw") eksenKaydir = eksenKaydir * -1;
                                        }
                            }
                            guncelKM = guncelKM + Spiral.length;
                            break;
                        }
                    }
                }
            






            return eksenKaydir;
        }
        
        private static double sapmaHesabi(double radius)
        {
            double sapma = 0;
            if (radius >= 800) sapma = 0;
            if (radius >= 700 && radius < 800)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 700);
            }

            if (radius >= 600 && radius < 700)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 600) + 0.0375;
            }

            if (radius >= 500 && radius < 600)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 500) + 0.0750;
            }

            if (radius >= 400 && radius < 500)
            {
                sapma = 0.0375 / 100 * (Convert.ToDouble(radius) - 400) + 0.1125;
            }

            if (radius < 400) sapma = 0.150;
            return sapma;
        }


    }
}
