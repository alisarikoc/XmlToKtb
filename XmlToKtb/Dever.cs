using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlToKtb.Properties;
using Path = System.IO.Path;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Data;
using System.Windows.Forms.VisualStyles;
using System.Windows.Shapes;
using Common;
using Common.Enum;
using Common.Modeller;
using DXFLib;
using System.Windows.Documents;

namespace XmlToKtb
{
    public static class Dever
    {

        public static string Devergoster()
        {
            OpenFileDialog file = new OpenFileDialog();
            file.FilterIndex = 0;
            file.DefaultExt = "xml";
            file.Title = "Güzergah XML Dosyası Seçiniz..";
            file.Filter = "XML Dosyası |*.xml";
            if (file.ShowDialog() == DialogResult.OK)
            {
                
            }
            
            var serializer = new DosyaDeserializer();
            var landXml = serializer.XmlDeserializer<LandXML>(file.FileName);
            var alignment = landXml.Alignments.Alignment;
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;
            DataTable table = new DataTable();
            DataColumn Km_kolon = new DataColumn("Kilometre");
            Km_kolon.DefaultValue = 0;
            Km_kolon.DataType = System.Type.GetType("System.Decimal");
            table.Columns.Add(Km_kolon);
            DataColumn appliedCant_kolon = new DataColumn("Uygulama Dever");
            appliedCant_kolon.DefaultValue = 0;
            appliedCant_kolon.DataType = System.Type.GetType("System.Decimal");
            table.Columns.Add(appliedCant_kolon);
            DataColumn curvature_kolon = new DataColumn("Yön");
            curvature_kolon.DefaultValue = "cw";
            curvature_kolon.DataType = System.Type.GetType("System.String");
            table.Columns.Add(curvature_kolon);
            DataRow drow = table.NewRow();
            List<LandXMLAlignmentsAlignmentCantCantStation> cantStations = new List<LandXMLAlignmentsAlignmentCantCantStation>();
            var Guzergahtanokunandever = alignment.Cant.CantStation;
            if (Guzergahtanokunandever == null)
            {
                foreach (var item in alignment.CoordGeom.Items)
                {
                    switch (item)
                    {
                        case LandXMLAlignmentsAlignmentCoordGeomLine _:
                            {
                                var line = (LandXMLAlignmentsAlignmentCoordGeomLine) item;
                                var starty = (Convert.ToDouble(line.Start.Split()[1]));
                                var startx = (Convert.ToDouble(line.Start.Split()[0]));
                                var endy = (Convert.ToDouble(line.End.Split()[1]));
                                var endx = (Convert.ToDouble(line.End.Split()[0]));
                                double semt = Math.Atan2((endy - starty), (endx - startx)); if (semt < 0) { semt += 2 * Math.PI; }
                                
                                LandXMLAlignmentsAlignmentCantCantStation cantStation = new LandXMLAlignmentsAlignmentCantCantStation
                                {
                                    station =  guncelKM,
                                    appliedCant = 0,
                                    curvature = "ccw"
                                };
                                cantStations.Add(cantStation);
                                landXml.Alignments.Alignment.Cant.CantStation = cantStations.ToArray();
                                guncelKM += line.length;
                                break;
                            }
                        case LandXMLAlignmentsAlignmentCoordGeomCurve _:
                            {
                                var curve = (LandXMLAlignmentsAlignmentCoordGeomCurve) item;
                                var yon = curve.rot;
                                LandXMLAlignmentsAlignmentCantCantStation cantStation = new LandXMLAlignmentsAlignmentCantCantStation
                                {
                                    station = guncelKM,
                                    appliedCant = 0,
                                    curvature = yon
                                };
                                cantStations.Add(cantStation);
                                landXml.Alignments.Alignment.Cant.CantStation = cantStations.ToArray();
                                guncelKM += curve.length;
                                break;
                            }
                        case LandXMLAlignmentsAlignmentCoordGeomSpiral _:
                            {
                                var Spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral) item;
                                var yon = Spiral.rot;
                                LandXMLAlignmentsAlignmentCantCantStation cantStation = new LandXMLAlignmentsAlignmentCantCantStation
                                {
                                    station = guncelKM,
                                    appliedCant = 0,
                                    curvature = yon
                                };
                                cantStations.Add(cantStation);
                                landXml.Alignments.Alignment.Cant.CantStation = cantStations.ToArray();

                                guncelKM = guncelKM + Spiral.length;
                                break;
                            }
                    }

                    if (item == alignment.CoordGeom.Items.Last())
                    {
                        var yon = "ccw";
                        if (item == typeof(LandXMLAlignmentsAlignmentCoordGeomSpiral))
                        {
                            var Spiral = (LandXMLAlignmentsAlignmentCoordGeomSpiral) item;
                            yon = Spiral.rot;
                        }
                        if (item == typeof(LandXMLAlignmentsAlignmentCoordGeomCurve))
                        {
                            var Curve = (LandXMLAlignmentsAlignmentCoordGeomCurve) item;
                            yon = Curve.rot;
                        }
                        LandXMLAlignmentsAlignmentCantCantStation cantStation = new LandXMLAlignmentsAlignmentCantCantStation
                        {
                            station = guncelKM,
                            appliedCant = 0,
                            curvature = yon
                        };
                        cantStations.Add(cantStation);
                        landXml.Alignments.Alignment.Cant.CantStation = cantStations.ToArray();

                    }
                }
            }



            foreach (var cantStation in alignment.Cant.CantStation)
            {
                drow = table.NewRow();
                drow["Kilometre"] = Convert.ToDouble(cantStation.station);
                drow["Uygulama Dever"] = Convert.ToDouble(cantStation.appliedCant);
                drow["Yön"] = Convert.ToString(cantStation.curvature);
                table.Rows.Add(drow);
            }

            Form1 form1=new Form1();
            landXml.Alignments.Alignment.Cant.name = file.FileName;
            landXml.Alignments.Alignment.Cant.gauge = (decimal) 1.5;
            landXml.Alignments.Alignment.Cant.rotationPoint = "insideRail";

            var sonform=form1.goster(table);

            if (landXml.Alignments.Alignment.Cant != null)
            {
                landXml.Alignments.Alignment.Cant.CantStation = null;
            }
            List<LandXMLAlignmentsAlignmentCantCantStation> cantStations2 = new List<LandXMLAlignmentsAlignmentCantCantStation>();
            foreach (DataRow tablesatir in table.Rows)
            {
                LandXMLAlignmentsAlignmentCantCantStation cantStation2 = new LandXMLAlignmentsAlignmentCantCantStation
                {
                    station = (decimal) tablesatir.ItemArray.GetValue(0),
                    appliedCant = (decimal) tablesatir.ItemArray.GetValue(1) ,
                    curvature = (string) tablesatir.ItemArray.GetValue(2)
                };
                cantStations2.Add(cantStation2);
                landXml.Alignments.Alignment.Cant.CantStation = cantStations2.ToArray();
            }

            string xmlIcerigi = KTBtoXML.Serialize(landXml);
            DosyaKaydetFiltreIle(xmlIcerigi, Resources.xmlExtensionFilter, "DEVERxml");

            return String.Empty;
        }
        private static void DeverKaydetFiltreIle()
        {
            List<Array> DeverListeArray = new List<Array>();


        }
        private static void DosyaKaydetFiltreIle(string icerik, string filtre, string dosyaadi)
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = "output",
                Filter = filtre
            };
            saveFileDialog.Title = "Kaydedilecek Dosya Adı Girin";
            if (dosyaadi != null)
            {
                saveFileDialog.FileName = dosyaadi;
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveFileDialog.FileName))
                    sw.WriteLine(icerik);
            }
        }



    }
}
