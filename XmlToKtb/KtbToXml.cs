using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Common;
using DXFLib;

namespace XmlToKtb
{
    public class KTBtoXML
    {
        private static decimal _km0;
        internal static string Serialize(LandXML landXml)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(LandXML));
            string xml;

            var xmlWriterSettings = new XmlWriterSettings {Encoding = Encoding.UTF8, Indent = true};
            // no BOM in a .NET string

            //xmlWriterSettings.OmitXmlDeclaration = true;

            using (var sww = new Utf8StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww, xmlWriterSettings))
                {
                    xsSubmit.Serialize(writer, landXml);
                    xml = sww.ToString(); // Your XML
                }
            }

            //File.WriteAllText("C:\\Users\\sysadmin\\Desktop\\hat4gardoğu.xml", xml, Encoding.UTF8);

            return xml;
        }

        internal static LandXML LandXmlOlustur(string shortFileName, List<Array> xmlData)
        {

            LandXML landXml = new LandXML
            {
                Units = new LandXMLUnits
                {
                    Metric = new LandXMLUnitsMetric
                    {
                        areaUnit = "squareMeter",
                        linearUnit = "meter",
                        volumeUnit = "cubicMeter",
                        temperatureUnit = "celsius",
                        pressureUnit = "milliBars",
                        diameterUnit = "millimeter",
                        angularUnit = "grads",
                        directionUnit = "grads"
                    }
                },

                Alignments = new LandXMLAlignments
                {
                    name = string.Empty,
                    Alignment = new LandXMLAlignmentsAlignment
                    {
                        name = shortFileName,
                        desc = string.Empty,
                        staStart = _km0,
                        CoordGeom = new LandXMLAlignmentsAlignmentCoordGeom(),
                        Cant = new LandXMLAlignmentsAlignmentCant(),
                        Profile = new LandXMLAlignmentsAlignmentProfile
                        {
                            ProfAlign = new LandXMLAlignmentsAlignmentProfileProfAlign
                            {
                                name = shortFileName
                            }
                        }

                    }
                }
            };

            landXml.Alignments.Alignment.CoordGeom.Items = new object[xmlData.Count];
            landXml.Alignments.Alignment.Profile.name = shortFileName;
            landXml.Alignments.Alignment.Profile.ProfAlign.Items = new object[xmlData.Count];
            List<object> coordGeomItems = new List<object>();
            List<object> profAlignItems = new List<object>();
            List<LandXMLAlignmentsAlignmentCantCantStation> cantStations = new List<LandXMLAlignmentsAlignmentCantCantStation>();
            foreach (Array satir in xmlData)
            {
                string satirTipi = satir.GetValue(0).ToString();
                if (satirTipi == "LN")
                {
                    LandXMLAlignmentsAlignmentCoordGeomLine line = new LandXMLAlignmentsAlignmentCoordGeomLine
                    {
                        Start = satir.GetValue(3) + " " + satir.GetValue(4),
                        End = satir.GetValue(5) + " " + satir.GetValue(6),
                        dir = (decimal) (double) satir.GetValue(1),
                        length = (decimal) (double) satir.GetValue(2)
                    };

                    coordGeomItems.Add(line);
                }
                else if (satirTipi == "SP")
                {
                    LandXMLAlignmentsAlignmentCoordGeomSpiral spiral = new LandXMLAlignmentsAlignmentCoordGeomSpiral
                    {
                        length = (decimal)(double)satir.GetValue(1),
                        radiusEnd = satir.GetValue(2).ToString(),
                        radiusStart = satir.GetValue(3).ToString(),
                        rot = satir.GetValue(4).ToString(),
                        spiType = "clothoid",
                        theta = (decimal)(double)satir.GetValue(5),
                        totalX = (decimal)(double)satir.GetValue(6),
                        totalY = (decimal)(double)satir.GetValue(7),
                        tanLong = (decimal)(double)satir.GetValue(8),
                        tanShort = (decimal)(double)satir.GetValue(9),
                        Start = satir.GetValue(10) + " " + satir.GetValue(11),
                        PI = satir.GetValue(12) + " " + satir.GetValue(13),
                        End = satir.GetValue(14) + " " + satir.GetValue(15)
                    };
                    coordGeomItems.Add(spiral);
                }
                else if (satirTipi == "AR")
                {
                    LandXMLAlignmentsAlignmentCoordGeomCurve curve = new LandXMLAlignmentsAlignmentCoordGeomCurve
                    {
                        rot = satir.GetValue(1).ToString(),
                        chord = (decimal)(double)satir.GetValue(2),
                        crvType = satir.GetValue(3).ToString(),
                        delta = (decimal)(double)satir.GetValue(4),
                        dirEnd = (decimal)(double)satir.GetValue(5),
                        dirStart = (decimal)(double)satir.GetValue(6),
                        external = (decimal)(double)satir.GetValue(7),
                        length = (decimal)(double)satir.GetValue(8),
                        midOrd = (decimal)(double)satir.GetValue(9),
                        radius = (decimal)(double)satir.GetValue(10),
                        tangent = (decimal)(double)satir.GetValue(11),
                        Start = satir.GetValue(12) + " " + satir.GetValue(13),
                        Center = satir.GetValue(14) + " " + satir.GetValue(15),
                        End = satir.GetValue(16) + " " + satir.GetValue(17),
                        PI = satir.GetValue(18) + " " + satir.GetValue(19)
                    };

                    coordGeomItems.Add(curve);

                }
                else if (satirTipi == "PR")
                {
                    var duseykurptipi = satir.GetValue(1).ToString();
                    if ((double)satir.GetValue(4) == 0)
                    {
                        //object[] array = { "PR", type, km, kot, boy, radius };
                        var pvi = new LandXMLAlignmentsAlignmentProfileProfAlignPVI();
                        {
                            string[] kmkot = { Convert.ToString((double)satir.GetValue(2)), " ", Convert.ToString((double)satir.GetValue(3)) };
                            pvi.Value = string.Concat(kmkot);
                        }
                        profAlignItems.Add(pvi);
                    }
                    else if (duseykurptipi == "0"|| Convert.ToDouble(satir.GetValue(5)) == 0)
                    {
                        LandXMLAlignmentsAlignmentProfileProfAlignParaCurve paraCurve = new LandXMLAlignmentsAlignmentProfileProfAlignParaCurve
                        {
                            length = (decimal)(double)satir.GetValue(4)

                        };
                        string[] kmkot = { Convert.ToString((double)satir.GetValue(2)), " ", Convert.ToString((double)satir.GetValue(3)) };
                        paraCurve.Value = string.Concat(kmkot);
                        profAlignItems.Add(paraCurve);
                    }
                    else if (duseykurptipi == "1" && Convert.ToDouble(satir.GetValue(5))!=0)
                    {
                        LandXMLAlignmentsAlignmentProfileProfAlignCircCurve circCurve = new LandXMLAlignmentsAlignmentProfileProfAlignCircCurve
                        {
                            length = (decimal)(double)satir.GetValue(4),
                            radius = (decimal)(double)satir.GetValue(5)

                        };
                        string[] kmkot = { Convert.ToString((double)satir.GetValue(2)), " ", Convert.ToString((double)satir.GetValue(3)) };
                        circCurve.Value = string.Concat(kmkot);
                        profAlignItems.Add(circCurve);
                    }

                }
                else if (satirTipi == "DEVER")
                {

                    landXml.Alignments.Alignment.Cant.name = shortFileName;
                    landXml.Alignments.Alignment.Cant.gauge = (decimal)1.5;
                    landXml.Alignments.Alignment.Cant.rotationPoint = "insideRail";

                    LandXMLAlignmentsAlignmentCantCantStation cantStation = new LandXMLAlignmentsAlignmentCantCantStation
                    {
                        station = (decimal)(double)satir.GetValue(1),
                        appliedCant =(decimal)Math.Abs((double)satir.GetValue(2)) * 15,
                        curvature = (string)satir.GetValue(3),
                    };
                    cantStations.Add(cantStation);
                    landXml.Alignments.Alignment.Cant.CantStation = cantStations.ToArray();
                }

                landXml.Alignments.Alignment.CoordGeom.Items = coordGeomItems.ToArray();
                landXml.Alignments.Alignment.Profile.ProfAlign.Items = profAlignItems.ToArray();
            }

            return landXml;
        }

        internal static List<Array> GetirXmlData(StreamReader veriReader)
        {
            List<Array> tumsatirlar = new List<Array>();
            List<Array> yataydizi = new List<Array>();
            List<Array> gecisdizi = new List<Array>();
            List<Array> XMLDATA = new List<Array>();


            while (!veriReader.EndOfStream)
            {
                char[] separator = { ' ', '\t' };
                var veriSatir = veriReader.ReadLine();
                if (veriSatir != null&&veriSatir!="")
                {
                    var veriKelimeleri = veriSatir.Split(separator, (StringSplitOptions)1);
                    tumsatirlar.Add(veriKelimeleri);
                }
            }
            veriReader.Close();

            var jj = 0;
            _km0 = 0;

            for (int j = 0; j < tumsatirlar.Count - 1; j++)
            {
                if (Convert.ToString(tumsatirlar[jj].GetValue(0)) == "$KM0")
                {
                    _km0 = Convert.ToDecimal(tumsatirlar[jj + 1].GetValue(0));
                }

                if (Convert.ToString(tumsatirlar[jj].GetValue(0)) == "$YATAY")
                {
                    for (int k = jj + 1; k < tumsatirlar.Count - 1; k++)

                    {
                        if (Convert.ToString(tumsatirlar[k].GetValue(0)) == "$SON") break;

                        var some = Convert.ToString(tumsatirlar[k].GetValue(0));
                        double Y = Convert.ToDouble(tumsatirlar[k].GetValue(1));
                        double X = Convert.ToDouble(tumsatirlar[k].GetValue(2));
                        double R = Convert.ToDouble(tumsatirlar[k].GetValue(3));

                        object[] array = { some, Y, X, R };
                        yataydizi.Add(array);
                    }
                }

                if (Convert.ToString(tumsatirlar[jj].GetValue(0)) == "$GECIS")
                {
                    for (int l = jj + 1; l < tumsatirlar.Count - 1; l++)

                    {
                        if (Convert.ToString(tumsatirlar[l].GetValue(0)) == "$SON") break;

                        var some = Convert.ToString(tumsatirlar[l].GetValue(0));
                        double gir = Convert.ToDouble(tumsatirlar[l].GetValue(2));
                        double cik = Convert.ToDouble(tumsatirlar[l].GetValue(3));

                        object[] array = { some, gir, cik };
                        gecisdizi.Add(array);
                    }
                }

                if (Convert.ToString(tumsatirlar[jj].GetValue(0)) == "$DUSEY")
                {
                    for (int l = jj + 1; l < tumsatirlar.Count - 1; l++)


                    {

                        if (Convert.ToString(tumsatirlar[l].GetValue(0)) == "$SON") break;

                        var km = Convert.ToDouble(tumsatirlar[l].GetValue(0));
                        double kot = Convert.ToDouble(tumsatirlar[l].GetValue(1));
                        double boy = Convert.ToDouble(tumsatirlar[l].GetValue(2));
                        string type = "0";
                        if(tumsatirlar[l].Length>3)//Son sütünda değer yok ise parabolik kurp ata
                        {
                            type = Convert.ToString(tumsatirlar[l].GetValue(3));
                        }
                        double radius = 0;
                        if (tumsatirlar[l].Length > 4)
                        {
                            radius = Convert.ToDouble(tumsatirlar[l].GetValue(6));
                        }
                        object[] array = { "PR", type, km, kot, boy, radius };
                        XMLDATA.Add(array);
                    }
                }
                if (Convert.ToString(tumsatirlar[jj].GetValue(0)) == "$DEVER")
                {
                    for (int l = jj + 1; l < tumsatirlar.Count - 1; l++)


                    {
                        double km;
                        double dever;
                        string yon = null;
                        if (Convert.ToString(tumsatirlar[l].GetValue(0)) == "$SON")
                        {
                            km = Convert.ToDouble(tumsatirlar[l - 1].GetValue(1));
                            dever = Convert.ToDouble(tumsatirlar[l - 1].GetValue(4));
                            if (dever < 0) { yon = "ccw"; }
                            if (dever >= 0) { yon = "cw"; }
                            object[] array1 = { "DEVER", km, dever, yon };
                            XMLDATA.Add(array1);
                            break;
                        }
                        km = Convert.ToDouble(tumsatirlar[l].GetValue(0));
                        dever = Convert.ToDouble(tumsatirlar[l].GetValue(3));
                       
                        if (dever < 0) { yon = "ccw"; }
                        if (dever >= 0) { yon = "cw"; }





                        object[] array = { "DEVER", km, dever, yon };
                        XMLDATA.Add(array);
                    }
                }

                if (Convert.ToString(tumsatirlar[jj].GetValue(0)) == "$ELEMENTS") //    ELEMANLAR YÖNTEMİ İLE ALINAN KTB HESAPLARI
                {
                    int eleman_sayisi = 0;
                    for (int l = jj + 1; l < tumsatirlar.Count - 1; l++)
                    {
                        if (l == jj + 1)
                        {
                            _km0= Convert.ToDecimal(tumsatirlar[l].GetValue(1));
                        }

                        if (Convert.ToString(tumsatirlar[l].GetValue(0)) == "$SON") break;

                        string type = Convert.ToString(tumsatirlar[l].GetValue(0));
                        if (type == "LN")
                        {
                            double Line_Sta_Y = Convert.ToDouble(tumsatirlar[l].GetValue(2));
                            double Line_Sta_X = Convert.ToDouble(tumsatirlar[l].GetValue(3));
                            double Line_Son_Y = Convert.ToDouble(tumsatirlar[l].GetValue(4));
                            double Line_Son_X = Convert.ToDouble(tumsatirlar[l].GetValue(5));
                            double semt_ilk = Math.Atan2((Line_Son_Y - Line_Sta_Y), (Line_Son_X - Line_Sta_X));
                            if (semt_ilk < 0)
                            {
                                semt_ilk += 2 * Math.PI;
                            }
                            double line_mes = Math.Sqrt(Math.Pow((Line_Sta_Y - Line_Son_Y), 2) + Math.Pow((Line_Sta_X - Line_Son_X), 2));
                            object[] array =
                            {
                                "LN", 450-semt_ilk * 180 / Math.PI, line_mes, Line_Sta_X, Line_Sta_Y,
                                Line_Son_X, Line_Son_Y
                            };

                            XMLDATA.Add(array);
                            eleman_sayisi += 1;
                        }
                        if (type == "SP")
                        {
                            double L1 = Convert.ToDouble(tumsatirlar[l].GetValue(2));
                            double L2 = Convert.ToDouble(tumsatirlar[l].GetValue(10));
                            double Akare = Convert.ToDouble(tumsatirlar[l].GetValue(3));
                            double R1 = Akare / L1;
                            double R2 = Akare / L2;
                            if (L2 != 0) { R2 = Akare / L2; }

                            double Yon = Convert.ToDouble(tumsatirlar[l].GetValue(4));
                            double Spi_type2 = Convert.ToDouble(tumsatirlar[l].GetValue(5));
                            if(Convert.ToString(tumsatirlar[l-1].GetValue(0))=="LN")Spi_type2=1;
                            if (Convert.ToString(tumsatirlar[l - 1].GetValue(0)) == "SP") Spi_type2 = -1;
                            if (Convert.ToString(tumsatirlar[l - 1].GetValue(0)) == "AR") Spi_type2 = -1;
                            double Spi_Sta_Y = Convert.ToDouble(tumsatirlar[l].GetValue(6));
                            double Spi_Sta_X = Convert.ToDouble(tumsatirlar[l].GetValue(7));
                            double Spi_PI_Y = Convert.ToDouble(tumsatirlar[l].GetValue(8));
                            double Spi_PI_X = Convert.ToDouble(tumsatirlar[l].GetValue(9));
                            double koordinatlardanMesafe = Math.Sqrt(Math.Pow((Spi_PI_Y - Spi_Sta_Y), 2) + Math.Pow((Spi_PI_X - Spi_Sta_X), 2));
                            string Yonstring = "cw";
                            if (Yon == -1) { Yonstring = "ccw"; }
                            double tetha = L1 / (2 * R1);
                            double X = Spiral_X_Hesapla(L1, Math.Sqrt(Akare));
                            double Y = Spiral_Y_Hesapla(L1, Math.Sqrt(Akare));
                            //....................................
                            double Tu = Y - X / Math.Tan(tetha);
                            double Tk = X / Math.Sin(tetha);
                            
                            double Xm_Gir = X + R1 * Math.Cos(tetha);
                            double Ym_Gir = Y - R1 * Math.Sin(tetha);
                            double semt_ilk = Math.Atan2((Spi_PI_Y - Spi_Sta_Y), (Spi_PI_X - Spi_Sta_X)); if (semt_ilk < 0) { semt_ilk += 2 * Math.PI; }

                            double Tk_Semt = semt_ilk + tetha;
                            if (Yonstring == "ccw") { Tk_Semt = semt_ilk - tetha; }
                            double Spi_End_Y = Spi_PI_Y + Tk * Math.Sin(Tk_Semt);
                            double Spi_End_X = Spi_PI_X + Tk * Math.Cos(Tk_Semt);

                            if (Spi_type2 == 1)
                            {
                                if (L2 == 0)
                                {
                                    if (Convert.ToString(XMLDATA.Last().GetValue(0)) == "LN")
                                    {
                                        Spi_Sta_Y = Convert.ToDouble(XMLDATA.Last().GetValue(6));
                                        Spi_Sta_X = Convert.ToDouble(XMLDATA.Last().GetValue(5));
                                        double semt_clotoid = Math.Atan2((Spi_PI_Y - Spi_Sta_Y), (Spi_PI_X - Spi_Sta_X));
                                        if (semt_clotoid < 0) { semt_clotoid += 2 * Math.PI; }
                                        Spi_PI_Y = Spi_Sta_Y + Tu * Math.Sin(semt_clotoid);
                                        Spi_PI_X = Spi_Sta_X + Tu * Math.Cos(semt_clotoid);
                                        Spi_End_Y = Spi_PI_Y + Tk * Math.Sin(Tk_Semt);
                                        Spi_End_X = Spi_PI_X + Tk * Math.Cos(Tk_Semt);
                                    }
                                    object[] array1 =
                                    {
                                        "SP", L1, R1, "INF", Yonstring, Math.Atan(Ym_Gir / Xm_Gir) * 180 / Math.PI, Y,X,Tu, Tk,
                                        Spi_Sta_X, Spi_Sta_Y, Spi_PI_X, Spi_PI_Y, Spi_End_X, Spi_End_Y
                                    };
                                    XMLDATA.Add(array1);
                                }

                            }
                            if (Spi_type2 == -1)
                            {
                                Yonstring = "ccw";
                                if (Yon == -1) { Yonstring = "cw"; }
                                if (L2 == 0)
                                {
                                    if (Convert.ToString(XMLDATA.Last().GetValue(0)) == "AR")
                                    {
                                        Spi_End_X = Convert.ToDouble(XMLDATA.Last().GetValue(16));
                                        Spi_End_Y = Convert.ToDouble(XMLDATA.Last().GetValue(17));
                                        double semt_clotoid = Math.Atan2((Spi_PI_Y - Spi_Sta_Y), (Spi_PI_X - Spi_Sta_X)); 
                                        if (semt_clotoid < 0) { semt_clotoid += 2 * Math.PI; }
                                        Spi_PI_Y = Spi_Sta_Y + Tu * Math.Sin(semt_clotoid);
                                        Spi_PI_X = Spi_Sta_X + Tu * Math.Cos(semt_clotoid);
                                    }
                                    object[] array1 =
                                    {
                                        "SP", L1, "INF", R1, Yonstring, Math.Atan(Ym_Gir / Xm_Gir) * 180 / Math.PI, Y,X, Tu, Tk,
                                        Spi_End_X, Spi_End_Y, Spi_PI_X, Spi_PI_Y, Spi_Sta_X, Spi_Sta_Y
                                    };
                                    XMLDATA.Add(array1);
                                }

                            }

                            if (L1 != 0 && L2 != 0)
                            {
                                Spi_Sta_Y = 0;
                                Spi_Sta_X = 0;
                                double Geri_Y = 0;
                                double Geri_X = 0;
                                double Geri_R = 0;
                                double Ileri_R = 0;
                                if (Convert.ToString(XMLDATA.Last().GetValue(0)) == "LN")
                                {
                                    Geri_Y = Convert.ToDouble(XMLDATA.Last().GetValue(4));
                                    Geri_X = Convert.ToDouble(XMLDATA.Last().GetValue(3));
                                    Spi_Sta_Y = Convert.ToDouble(XMLDATA.Last().GetValue(6));
                                    Spi_Sta_X = Convert.ToDouble(XMLDATA.Last().GetValue(5));
                                }
                                if (Convert.ToString(XMLDATA.Last().GetValue(0)) == "AR")
                                {
                                    Spi_Sta_Y = Convert.ToDouble(XMLDATA.Last().GetValue(17));
                                    Spi_Sta_X = Convert.ToDouble(XMLDATA.Last().GetValue(16));
                                    Geri_Y = Convert.ToDouble(XMLDATA.Last().GetValue(19));
                                    Geri_X = Convert.ToDouble(XMLDATA.Last().GetValue(18));
                                    Geri_R = Convert.ToDouble(XMLDATA.Last().GetValue(10));
                                    Yonstring= Convert.ToString(XMLDATA.Last().GetValue(1));
                                }
                                if (Convert.ToString(XMLDATA.Last().GetValue(0)) == "SP")
                                {
                                    Spi_Sta_Y = Convert.ToDouble(XMLDATA.Last().GetValue(15));
                                    Spi_Sta_X = Convert.ToDouble(XMLDATA.Last().GetValue(14));
                                    Geri_Y = Convert.ToDouble(XMLDATA.Last().GetValue(13));
                                    Geri_X = Convert.ToDouble(XMLDATA.Last().GetValue(12));
                                    Geri_R = Convert.ToDouble(XMLDATA.Last().GetValue(2));
                                    Yonstring = Convert.ToString(XMLDATA.Last().GetValue(4));
                                }

                                if (Convert.ToString(tumsatirlar[l + 1].GetValue(0)) == "SP" && tumsatirlar.Count>=(l+1))
                                {
                                    Spi_End_Y = Convert.ToDouble(tumsatirlar[l+1].GetValue(6));
                                    Spi_End_X = Convert.ToDouble(tumsatirlar[l+1].GetValue(7));
                                    Geri_Y = Convert.ToDouble(tumsatirlar[l+1].GetValue(8));
                                    Geri_X = Convert.ToDouble(tumsatirlar[l+1].GetValue(9));
                                }

                                if (Convert.ToString(tumsatirlar[l + 1].GetValue(0)) == "AR" && tumsatirlar.Count >= (l + 1))
                                {

                                    Spi_End_Y = Convert.ToDouble(tumsatirlar[l+1].GetValue(8));
                                    Spi_End_X = Convert.ToDouble(tumsatirlar[l+1].GetValue(9));
                                    Ileri_R = Convert.ToDouble(tumsatirlar[l+1].GetValue(2));
                                    Yon = Convert.ToDouble(tumsatirlar[l+1].GetValue(3));
                                }

                                double tetha1 = L1 / (2 * R1);
                                double X1 = Spiral_X_Hesapla(L1, Math.Sqrt(Akare));
                                double Y1 = Spiral_Y_Hesapla(L1, Math.Sqrt(Akare));
                                double tetha2 = L2 / (2 * R2);
                                double X2 = Spiral_X_Hesapla(L2, Math.Sqrt(Akare));
                                double Y2 = Spiral_Y_Hesapla(L2, Math.Sqrt(Akare));

                                //....................................
                                double theta_tum = (Math.Atan2((X1 - X2), (Y1 - Y2)));
                                double tetha2_donuk = theta_tum - tetha2;
                                double SL = (Math.Sqrt(Math.Pow((Y1 - Y2), 2) + Math.Pow((X1 - X2), 2)));

                                double Spi_Sta_Y_0 = Convert.ToDouble(tumsatirlar[l].GetValue(6));
                                double Spi_Sta_X_0 = Convert.ToDouble(tumsatirlar[l].GetValue(7));
                                double Spi_PI_Y_0 = Convert.ToDouble(tumsatirlar[l].GetValue(8));
                                double Spi_PI_X_0 = Convert.ToDouble(tumsatirlar[l].GetValue(9));
                                double X_0 = SL * Math.Sin(tetha2_donuk);
                                double Y_0 = SL * Math.Cos(tetha2_donuk);
                                Tu = Y_0 - X_0 / Math.Tan(tetha1 - tetha2);
                                Tk = X_0 / Math.Sin(tetha1 - tetha2);
                                double semtilk_0 = Math.Atan2((Spi_Sta_Y - Geri_Y), (Spi_Sta_X - Geri_X)); if (semtilk_0 < 0) { semtilk_0 += 2 * Math.PI; }

                                Spi_PI_Y = Spi_Sta_Y + Tu * Math.Sin(semtilk_0 );
                                Spi_PI_X = Spi_Sta_X + Tu * Math.Cos(semtilk_0 );

                                //double semtilk_0 = Math.Atan2((Spi_PI_Y_0 - Spi_Sta_Y_0), (Spi_PI_X_0 - Spi_Sta_X_0));
                                //if(Spi_type2==-1) { semtilk_0 = Math.Atan2((Spi_Sta_Y_0 - Spi_PI_Y_0), (Spi_Sta_X_0 - Spi_PI_X_0)); }
                                //if (semtilk_0 < 0) { semtilk_0 += 2 * Math.PI; }
                                //Spi_PI_Y_0 = Spi_Sta_Y + Tu * Math.Sin(semtilk_0);
                                //Spi_PI_X_0 = Spi_Sta_X + Tu * Math.Cos(semtilk_0);

                                //double s1 = semtilk_0 + Yon * (tetha2);

                                //Spi_PI_Y = Spi_Sta_Y + Tu * Math.Sin(semtilk_0 + Yon * (tetha2));
                                // Spi_PI_X = Spi_Sta_X + Tu * Math.Cos(semtilk_0 + Yon * (tetha2));
                                //var R_yedek = R1;
                                //if (Spi_type2 == -1){Spi_PI_Y = Spi_Sta_Y + Tk * Math.Sin((semtilk_0 + Yon * (tetha1)));Spi_PI_X = Spi_Sta_X + Tk * Math.Cos((semtilk_0 + Yon * (tetha1)));R1 = R2;R2 = R_yedek;}
                                //Spi_End_Y = Spi_Sta_Y + SL * Math.Sin(semtilk_0 + Yon * (theta_tum));
                                //Spi_End_X = Spi_Sta_X + SL * Math.Cos(semtilk_0 + Yon * (theta_tum));


                                object[] array =
                                {
                                        "SP", Math.Abs((L1-L2)), R1, R2,Yonstring, Math.Atan(tetha1-tetha2) * 180 / Math.PI,Y_0, X_0, Tu, Tk,
                                        Spi_Sta_X, Spi_Sta_Y, Spi_PI_X, Spi_PI_Y, Spi_End_X, Spi_End_Y
                                    };
                                XMLDATA.Add(array);
                            }

                            eleman_sayisi += 1;
                        }
                        if (type == "AR")
                        {
                            double R = Convert.ToDouble(tumsatirlar[l].GetValue(2));
                            double Yon = Convert.ToDouble(tumsatirlar[l].GetValue(3));
                            double Cur_Cen_Y = Convert.ToDouble(tumsatirlar[l].GetValue(6));
                            double Cur_Cen_X = Convert.ToDouble(tumsatirlar[l].GetValue(7));
                            double Cur_Sta_Y = Convert.ToDouble(tumsatirlar[l].GetValue(8));
                            double Cur_Sta_X = Convert.ToDouble(tumsatirlar[l].GetValue(9));
                            double Cur_End_Y = Convert.ToDouble(tumsatirlar[l].GetValue(10));
                            double Cur_End_X = Convert.ToDouble(tumsatirlar[l].GetValue(11));
                            double chord = Math.Sqrt(Math.Pow((Cur_Sta_Y - Cur_End_Y), 2) + Math.Pow((Cur_Sta_X - Cur_End_X), 2));

                            double dir_Start = Math.Atan2((Cur_Cen_Y - Cur_Sta_Y), (Cur_Cen_X - Cur_Sta_X));
                            if (dir_Start < 0)
                            {
                                dir_Start += 2 * Math.PI;
                            }
                            dir_Start -= (Convert.ToDouble(Yon) * Math.PI / 2);


                            double dir_End = Math.Atan2((Cur_Cen_Y - Cur_End_Y), (Cur_Cen_X - Cur_End_X));
                            if (dir_End < 0)
                            {
                                dir_End += 2 * Math.PI;
                            }
                            dir_End -= (Convert.ToDouble(Yon) * Math.PI / 2);
                            double kurp_acisi = Math.Abs(dir_End - dir_Start); if (kurp_acisi > 2 * Math.PI) kurp_acisi -= Math.PI * 2;
                            if (kurp_acisi > Math.PI) kurp_acisi = 2 * Math.Asin(chord / 2 / R);
                            double kurp_t = R * Math.Tan(kurp_acisi / 2);
                            double external = R / Math.Cos(kurp_acisi / 2) - R;
                            double kurp_boyu = R * kurp_acisi;
                            double midOrd = R - R * Math.Cos(kurp_acisi / 2);
                            double Cur_PI_Y = Cur_Sta_Y + kurp_t * Math.Sin(dir_Start);
                            double Cur_PI_X = Cur_Sta_X + kurp_t * Math.Cos(dir_Start);
                            string Yonsaat = "cw";
                            if (Yon == -1) Yonsaat = "ccw";
                            object[] array2 =
                            {
                                "AR", Yonsaat, chord, "arc", kurp_acisi * 180 / Math.PI, 450-dir_End * 180 / Math.PI,
                                450-dir_Start * 180 / Math.PI, external, kurp_boyu, midOrd, R, kurp_t, Cur_Sta_X, Cur_Sta_Y,
                                Cur_Cen_X, Cur_Cen_Y, Cur_End_X, Cur_End_Y, Cur_PI_X, Cur_PI_Y
                            };
                            XMLDATA.Add(array2);
                            eleman_sayisi += 1;
                        }

                    }
                }

                jj += 1;
            }

            //double Line_Start_Y = 0;
            //double Line_Start_X = 0;
            DXFLib.DXFPoint Line_Start=new DXFPoint();
            DXFLib.DXFPoint Geri_Some= new DXFPoint();
            DXFLib.DXFPoint Some = new DXFPoint();
            DXFLib.DXFPoint Ileri_Some = new DXFPoint();
            //...................SOMELER YÖNTEMİ YATAY GÜZERGAH HESAPLARI......................
            for (int i = 1; i < yataydizi.Count - 1; i++)
            {
                if (i == 0 || Convert.ToDouble(yataydizi[i - 1].GetValue(3)) == 0)
                {
                    Line_Start.Y = Convert.ToDouble(yataydizi[i - 1].GetValue(1));
                    Line_Start.X = Convert.ToDouble(yataydizi[i - 1].GetValue(2));
                    //Line_Start_Y = Convert.ToDouble(yataydizi[i - 1].GetValue(1));
                    //Line_Start_X = Convert.ToDouble(yataydizi[i - 1].GetValue(2));
                }

                Geri_Some.Y = Convert.ToDouble(yataydizi[i-1].GetValue(1));
                Geri_Some.X = Convert.ToDouble(yataydizi[i-1].GetValue(2));
                Some.Y = Convert.ToDouble(yataydizi[i].GetValue(1));
                Some.X = Convert.ToDouble(yataydizi[i].GetValue(2));
                Ileri_Some.Y = Convert.ToDouble(yataydizi[i+1].GetValue(1));
                Ileri_Some.X = Convert.ToDouble(yataydizi[i+1].GetValue(2));
                double Somelerarası_mes = Math.Sqrt(Math.Pow(Convert.ToDouble(Some.Y) - Convert.ToDouble(Geri_Some.Y), 2) +
                                                    Math.Pow(Convert.ToDouble(Some.X) - Convert.ToDouble(Geri_Some.X), 2));
                //.....................SOME AÇISI.................
                double semt_ilk =
                    Math.Atan2(Convert.ToDouble(Some.Y) - Convert.ToDouble(Geri_Some.Y),
                        Convert.ToDouble(Some.X) - Convert.ToDouble(Geri_Some.X));
                if (semt_ilk < 0)
                {
                    semt_ilk += 2 * Math.PI;
                }

                double semt_ikinci =
                    Math.Atan2(Convert.ToDouble(Ileri_Some.Y) - Convert.ToDouble(Some.Y),
                        Convert.ToDouble(Ileri_Some.X) - Convert.ToDouble(Some.X));
                if (semt_ikinci < 0)
                {
                    semt_ikinci += 2 * Math.PI;
                }
                
                
                
                var dikayak = -(Some.Y - Geri_Some.Y) /Math.Sqrt(Math.Pow(Convert.ToDouble(Some.X - Geri_Some.X), 2) + Math.Pow(Convert.ToDouble(Some.Y - Geri_Some.Y), 2)) *(Ileri_Some.X - Geri_Some.X) +(Some.X - Geri_Some.X) / 
                              Math.Sqrt(Math.Pow(Convert.ToDouble(Some.X - Geri_Some.X), 2) + Math.Pow(Convert.ToDouble(Some.Y - Geri_Some.Y), 2)) * (Ileri_Some.Y - Geri_Some.Y);
                var some_yon = dikayak < 0 ? "ccw" : "cw";

                double some_acisi = Math.PI - (semt_ilk + Math.PI - semt_ikinci);
                if (some_yon == "cw")
                {
                    some_acisi = semt_ikinci - semt_ilk;
                }
                else
                {
                    some_acisi = semt_ilk - semt_ikinci;
                }

                if (some_acisi < 0) { some_acisi = Math.PI * 2 + some_acisi; }
                //....................SPİRAL HESAPLARI....................//////////////////////////////////////////////////////////////////////////////////////////////
                double L_Giris = Convert.ToDouble(gecisdizi[i].GetValue(1));
                double L_Cikis = Convert.ToDouble(gecisdizi[i].GetValue(2));
                double R = Convert.ToDouble(yataydizi[i].GetValue(3));
                if (R == 0)
                {
                    DXFLib.DXFPoint Line_End = new DXFPoint();
                    Line_End.Y = Geri_Some.Y;
                    Line_End.X = Convert.ToDouble(yataydizi[i].GetValue(2));
                    object[] array =
                        {"LN", semt_ilk * 180 / Math.PI, Math.Sqrt(Math.Pow((Convert.ToDouble(Line_Start.Y - Line_End.Y)), 2) + Math.Pow((Convert.ToDouble(Line_Start.X - Line_End.X)), 2)), Line_Start.X, Line_Start.Y, Line_End.X, Line_End.Y};
                    XMLDATA.Add(array);
                    if (i == yataydizi.Count - 2)
                    {
                        Line_End.Y = Convert.ToDouble(yataydizi[i + 1].GetValue(1));
                        Line_End.X = Convert.ToDouble(yataydizi[i + 1].GetValue(2));

                        double line_mes = Math.Sqrt(Math.Pow((Convert.ToDouble(Line_Start.Y - Line_End.Y)), 2) + Math.Pow((Convert.ToDouble(Line_Start.X - Line_End.X)), 2));
                        object[] array1 =
                        {

                            "LN", semt_ikinci * 180 / Math.PI, Math.Sqrt(Math.Pow((Convert.ToDouble(Line_Start.Y - Line_End.Y)), 2) + Math.Pow((Convert.ToDouble(Line_Start.X - Line_End.X)), 2)), Line_Start.X, Line_Start.Y,
                            Line_End.X, Line_End.Y
                        };

                        XMLDATA.Add(array1);
                    }

                    continue;
                }

                double tethaGiris = L_Giris / 2 / R;
                double tethaCikis = L_Cikis / 2 / R;
                double S_kat_G = Math.Sqrt(R * L_Giris);
                double S_kat_C = Math.Sqrt(R * L_Cikis);
                double X_Gir = Spiral_X_Hesapla(L_Giris, S_kat_G);
                double X_Cik = Spiral_X_Hesapla(L_Cikis, S_kat_C);
                double Y_Gir = Spiral_Y_Hesapla(L_Giris, S_kat_G);
                double Y_Cik = Spiral_Y_Hesapla(L_Cikis, S_kat_C);

                double Tu_Gir = Y_Gir - X_Gir / Math.Tan(tethaGiris);
                double Tu_Cik = Y_Cik - X_Cik / Math.Tan(tethaCikis);
                double Tk_Gir = X_Gir / Math.Sin(tethaGiris);
                double Tk_Cik = X_Cik / Math.Sin(tethaCikis);

                double Xm_Gir = X_Gir + R * Math.Cos(tethaGiris);
                double Xm_Cik = X_Cik + R * Math.Cos(tethaCikis);
                double Ym_Gir = Y_Gir - R * Math.Sin(tethaGiris);
                double Ym_Cik = Y_Cik - R * Math.Sin(tethaCikis);
                double Delte_R_Gir = Xm_Gir - R;
                double Delte_R_Cik = Xm_Cik - R;
                //.........................................................//////////////////////////////////////////////////////////////////////////////////////////
                double Ana_teget_Gir = Ym_Gir + R * Math.Tan(some_acisi / 2) + (Delte_R_Cik - Math.Cos(some_acisi) * Delte_R_Gir) / Math.Sin(some_acisi);

                double Spi_Gir_Start_Y = Convert.ToDouble(Geri_Some.Y) + Math.Sin(semt_ilk) * (Somelerarası_mes - Ana_teget_Gir);
                double Spi_Gir_Start_X = Convert.ToDouble(Geri_Some.X) + Math.Cos(semt_ilk) * (Somelerarası_mes - Ana_teget_Gir);
                double Spi_Gir_PI_Y = Spi_Gir_Start_Y + Tu_Gir * Math.Sin(semt_ilk);
                double Spi_Gir_PI_X = Spi_Gir_Start_X + Tu_Gir * Math.Cos(semt_ilk);
                double Spi_Gir_End_Y = Spi_Gir_PI_Y + Tk_Gir * Math.Sin(semt_ilk + tethaGiris);
                double Spi_Gir_End_X = Spi_Gir_PI_X + Tk_Gir * Math.Cos(semt_ilk + tethaGiris);

                if (some_yon == "ccw")
                {
                    Spi_Gir_End_Y = Spi_Gir_PI_Y + Tk_Gir * Math.Sin(semt_ilk - tethaGiris);
                    Spi_Gir_End_X = Spi_Gir_PI_X + Tk_Gir * Math.Cos(semt_ilk - tethaGiris);
                }

                if (L_Giris > 0)
                {
                    object[] array1 =
                    {
                        "LN", semt_ilk * 180 / Math.PI, Math.Sqrt(Math.Pow((Convert.ToDouble(Line_Start.Y - Spi_Gir_Start_Y)), 2) + Math.Pow((Convert.ToDouble(Line_Start.X - Spi_Gir_Start_X)), 2)), Line_Start.X, Line_Start.Y,
                        Spi_Gir_Start_X, Spi_Gir_Start_Y
                    }; XMLDATA.Add(array1);
                    object[] array =
                    {
                        "SP", L_Giris, R, "INF",some_yon, Math.Atan2(Xm_Gir , Ym_Gir) * 180 / Math.PI, X_Gir, Y_Gir, Tu_Gir, Tk_Gir,
                        Spi_Gir_Start_X, Spi_Gir_Start_Y, Spi_Gir_PI_X, Spi_Gir_PI_Y, Spi_Gir_End_X, Spi_Gir_End_Y
                    }; XMLDATA.Add(array);
                }

                //....................KURP HESAPLARI...................////////////////////////////////////////////////////////////////////////////////////////////
                double kurp_acisi = some_acisi - tethaGiris - tethaCikis;
                double kurp_t = R * Math.Tan(kurp_acisi / 2);
                double kurp_boyu = R * kurp_acisi;
                double external = R / Math.Cos(kurp_acisi / 2) - R;
                double midOrd = R - R * Math.Cos(kurp_acisi / 2);
                double chord = R * Math.Sin(kurp_acisi / 2) * 2;

                //............................KOORDİNAT HESAPLARI...........................................

                double Curve_Start_Y = Spi_Gir_End_Y;
                double Curve_Start_X = Spi_Gir_End_X;
                if (L_Giris == 0)
                {
                    Curve_Start_Y = Convert.ToDouble(Some.Y) + kurp_t * Math.Sin(semt_ilk+Math.PI);
                    Curve_Start_X = Convert.ToDouble(Some.X) + kurp_t * Math.Cos(semt_ilk+Math.PI);

                    object[] array =
                        {"LN", semt_ilk * 180 / Math.PI, Math.Sqrt(Math.Pow((Convert.ToDouble(Line_Start.Y - Curve_Start_Y)), 2) + Math.Pow((Convert.ToDouble(Line_Start.X - Curve_Start_X)), 2)), Line_Start.X, Line_Start.Y, Curve_Start_X, Curve_Start_Y};
                    XMLDATA.Add(array);
                }

                double dir_Start = semt_ilk + tethaGiris;
                double dir_End = semt_ilk + tethaGiris + kurp_acisi;
                if (some_yon == "ccw")
                {
                    dir_Start = semt_ilk - tethaGiris;
                    dir_End = semt_ilk - tethaGiris - kurp_acisi;
                }

                double Curve_PI_Y = Curve_Start_Y + kurp_t * Math.Sin(dir_Start);
                double Curve_PI_X = Curve_Start_X + kurp_t * Math.Cos(dir_Start);
                double Curve_End_Y = Curve_PI_Y + kurp_t * Math.Sin(dir_End);
                double Curve_End_x = Curve_PI_X + kurp_t * Math.Cos(dir_End);
                double Curve_Cen_Y = Curve_Start_Y + R * Math.Sin(semt_ilk + tethaGiris + Math.PI / 2);
                double Curve_Cen_X = Curve_Start_X + R * Math.Cos(semt_ilk + tethaGiris + Math.PI / 2);
                if (some_yon == "ccw")
                {
                    Curve_Cen_Y = Curve_Start_Y + R * Math.Sin(semt_ilk - tethaGiris - Math.PI / 2);
                    Curve_Cen_X = Curve_Start_X + R * Math.Cos(semt_ilk - tethaGiris - Math.PI / 2);
                }

                object[] array2 =
                {
                    "AR", some_yon, chord, "arc", kurp_acisi * 180 / Math.PI, dir_End * 180 / Math.PI,
                    dir_Start * 180 / Math.PI, external, kurp_boyu, midOrd, R, kurp_t, Curve_Start_X, Curve_Start_Y,
                    Curve_Cen_X, Curve_Cen_Y, Curve_End_x, Curve_End_Y, Curve_PI_X, Curve_PI_Y
                };
                XMLDATA.Add(array2);
                Line_Start.Y = Curve_End_Y;
                Line_Start.X = Curve_End_x;
                double dir_spi_cik_End = 0;
                //............................SPİRAL ÇIKIŞ...............................
                if (L_Cikis > 0)
                {
                    double Spi_Cik_Start_Y = Curve_End_Y;
                    double Spi_Cik_Start_X = Curve_End_x;
                    double Spi_Cik_PI_Y = Spi_Cik_Start_Y + Tk_Cik * Math.Sin(dir_End);
                    double Spi_Cik_PI_X = Spi_Cik_Start_X + Tk_Cik * Math.Cos(dir_End);
                    dir_spi_cik_End = dir_End + tethaCikis;
                    if (some_yon == "ccw")
                    {
                        dir_spi_cik_End = dir_End - tethaCikis;
                    }

                    double Spi_Cik_End_Y = Spi_Cik_PI_Y + Tu_Cik * Math.Sin(dir_spi_cik_End);
                    double Spi_Cik_End_X = Spi_Cik_PI_X + Tu_Cik * Math.Cos(dir_spi_cik_End);
                    object[] array =
                    {
                        "SP", L_Cikis, "INF", R, some_yon, Math.Atan2(Xm_Cik , Ym_Cik) * 180 / Math.PI, X_Cik, Y_Cik, Tu_Cik, Tk_Cik,
                        Spi_Cik_Start_X, Spi_Cik_Start_Y, Spi_Cik_PI_X, Spi_Cik_PI_Y, Spi_Cik_End_X, Spi_Cik_End_Y
                    };
                    XMLDATA.Add(array);
                    Line_Start.Y = Spi_Cik_End_Y;
                    Line_Start.X = Spi_Cik_End_X;


                }
                if (i == yataydizi.Count - 2)
                {

                    DXFPoint Line_End = new DXFPoint();
                    Line_End.Y = Convert.ToDouble(yataydizi[i + 1].GetValue(1));
                    Line_End.X = Convert.ToDouble(yataydizi[i + 1].GetValue(2));
                    object[] array1 =
                    {

                        "LN", dir_spi_cik_End * 180 / Math.PI, Math.Sqrt(Math.Pow((Convert.ToDouble(Line_Start.Y - Line_End.Y)), 2) + Math.Pow((Convert.ToDouble(Line_Start.X - Line_End.X)), 2)), Line_Start.X, Line_Start.Y,
                        Line_End.X, Line_End.Y
                    };

                    XMLDATA.Add(array1);
                }
            }
            return XMLDATA;
        }


        /////////////////////

        private static double Faktoryel(double faktor)
        {

            double sonuc = 1;
            for (int i = 1; i <= faktor; i++)
            {
                sonuc *= i;
            }
            if (faktor == 0) sonuc = 1;
            return sonuc;
        }

        private static double Spiral_X_Hesapla(double L, double Sip_kat)
        {
            double X = 0;
            for (int n = 1; n <= 4; n++)
            {
                int faktorx = 2 * n - 1;
                X += Math.Pow(-1, n - 1) * Math.Pow(L, 4 * n - 1) /
                     ((4 * n - 1) * Faktoryel(faktorx) * Math.Pow(Sip_kat, 4 * n - 2) * Math.Pow(2, 2 * n - 1));
            }

            return X;
        }

        private static double Spiral_Y_Hesapla(double L, double Sip_kat)
        {
            double Y = 0;
            for (int n = 1; n <= 5; n++)
            {
                int faktory = 2 * n - 2;
                Y += Math.Pow(-1, n - 1) * Math.Pow(L, 4 * n - 3) /
                     ((4 * n - 3) * Faktoryel(faktory) * Math.Pow(Sip_kat, 4 * n - 4) * Math.Pow(2, 2 * n - 2));
            }

            return Y;
        }


    }
}
