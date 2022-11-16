using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Shapes;
using Common.Modeller;
using DXFLib;

namespace XmlToKtb
{

    public class yuzeyXML
    {

        internal static LandXML yuzeyXmlOlustur(List<Nokta> noktaList, List<Ucgen> ucgenList, string shortFileName)
        {
            UcgenListeleriniSadelestir(ref noktaList, ref ucgenList);

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
                Project = new LandXMLProject
                {
                    name = shortFileName
                },

                Surfaces = new LandXMLSurfaces
                {
                    Surface = new LandXMLSurfacesSurface
                    {
                        name = "yuzey",
                        desc = "Description",
                        Definition = new LandXMLSurfacesSurfaceDefinition
                        {
                            Pnts = new LandXMLSurfacesSurfaceDefinitionPnts
                            {

                            },
                            Faces = new LandXMLSurfacesSurfaceDefinitionFaces
                            {

                            }
                        }
                    }
                }
            };
            landXml.Surfaces.Surface.Definition.Pnts.Items = new object[noktaList.Count];
            List<object> koorsItems = new List<object>();
            foreach (var nokta in noktaList)
            {
                LandXMLSurfacesSurfaceDefinitionPntsP Pnt = new LandXMLSurfacesSurfaceDefinitionPntsP
                {
                    id = nokta.Numara.ToString()
                };
                string[] koors = { Convert.ToString((double) nokta.X), " ", Convert.ToString((double) nokta.Y), " ", Convert.ToString((double) nokta.Z) };
                Pnt.Value = string.Concat(koors);
                koorsItems.Add(Pnt);
            }
            landXml.Surfaces.Surface.Definition.Pnts.Items = koorsItems.ToArray();

            landXml.Surfaces.Surface.Definition.Faces.Items = new object[ucgenList.Count];
            List<object> ucgenItems = new List<object>();
            foreach (var ucgen in ucgenList)
            {
                LandXMLSurfacesSurfaceDefinitionFacesF yuzeyUcgeni = new LandXMLSurfacesSurfaceDefinitionFacesF();
                string[] ucgenNoktalari = { Convert.ToString(ucgen.Numara0), " ", Convert.ToString(ucgen.Numara1), " ", Convert.ToString(ucgen.Numara2) };
                yuzeyUcgeni.Value = string.Concat(ucgenNoktalari);
                ucgenItems.Add(yuzeyUcgeni);
            }
            landXml.Surfaces.Surface.Definition.Faces.Items = ucgenItems.ToArray();

            var saveFileDialog = new SaveFileDialog() { FileName = "yuzey" };
            var icerik = KTBtoXML.Serialize(landXml);
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveFileDialog.FileName))
                    sw.WriteLine(icerik);
            }

            return landXml;
        }

        private static void UcgenListeleriniSadelestir(ref List<Nokta> noktaList, ref List<Ucgen> ucgenList)
        {
            Dictionary<DXFPoint, List<int>> numaralarSozlugu = new Dictionary<DXFPoint, List<int>>();
            List<Nokta> yeniNoktaList = new List<Nokta>();

            foreach (var nokta in noktaList)
            {
                var dxfPoint = numaralarSozlugu.Keys.FirstOrDefault(x => x.X == nokta.X && x.Y == nokta.Y && x.Z == nokta.Z);

                if (dxfPoint != null && numaralarSozlugu.TryGetValue(dxfPoint, out List<int> numaralar))
                {
                    numaralar.Add(nokta.Numara);
                    numaralarSozlugu[dxfPoint] = numaralar;
                }
                else
                {
                    //nokta koordinatıyla ilk kez karşılaştık.
                    numaralarSozlugu.Add(new DXFPoint { X = nokta.X, Y = nokta.Y, Z = nokta.Z }, new List<int> { nokta.Numara });
                    yeniNoktaList.Add(nokta);
                }
            }

            foreach (var ucgen in ucgenList)
            {
                var numaralar = numaralarSozlugu.Values.FirstOrDefault(p => p.Contains(ucgen.Numara0));
                ucgen.Numara0 = numaralar.Min();

                var numaralar1 = numaralarSozlugu.Values.FirstOrDefault(p => p.Contains(ucgen.Numara1));
                ucgen.Numara1 = numaralar1.Min();

                var numaralar2 = numaralarSozlugu.Values.FirstOrDefault(p => p.Contains(ucgen.Numara2));
                ucgen.Numara2 = numaralar2.Min();
            }

            noktaList = yeniNoktaList;
        }
    }
}
