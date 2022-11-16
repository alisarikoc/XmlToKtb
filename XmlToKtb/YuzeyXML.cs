using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace XmlToKtb
{

    public class yuzeyXML
    {

        internal static LandXML yuzeyXmlOlustur(List<Array> noktaList, List<Array> kenarList,string shortFileName)
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
                Project = new LandXMLProject {
                    name = shortFileName
                },

                Surfaces = new LandXMLSurfaces {
                    Surface = new LandXMLSurfacesSurface {
                        name = "yuzey",desc = "Description",
                        Definition = new LandXMLSurfacesSurfaceDefinition {
                            Pnts = new LandXMLSurfacesSurfaceDefinitionPnts {

                            },
                            Faces =new LandXMLSurfacesSurfaceDefinitionFaces {

                            }
                        }
                    }
                }
            };
            landXml.Surfaces.Surface.Definition.Pnts.Items=new object[noktaList.Count];
            List<object> koorsItems = new List<object>();
            foreach (var noktalar in noktaList)
            {
                LandXMLSurfacesSurfaceDefinitionPntsP Pnt = new LandXMLSurfacesSurfaceDefinitionPntsP
                {
                    id = noktalar.GetValue(0).ToString()
                };
                string[] koors = {Convert.ToString((double) noktalar.GetValue(1)), " ", Convert.ToString((double) noktalar.GetValue(2)), " " ,Convert.ToString((double) noktalar.GetValue(3))};
                Pnt.Value = string.Concat(koors);
                koorsItems.Add(Pnt);
            } 
            landXml.Surfaces.Surface.Definition.Pnts.Items=koorsItems.ToArray();

            landXml.Surfaces.Surface.Definition.Faces.Items = new object[kenarList.Count];
            List<object> kenarItems = new List<object>();
            foreach (var kenarlar in kenarList)
            {
                LandXMLSurfacesSurfaceDefinitionFacesF Knr = new LandXMLSurfacesSurfaceDefinitionFacesF();
                string[] kenars = { Convert.ToString(kenarlar.GetValue(0)), " ", Convert.ToString( kenarlar.GetValue(1)), " ", Convert.ToString(kenarlar.GetValue(2)) };
                Knr.Value = string.Concat(kenars);
                kenarItems.Add(Knr);
            }
            landXml.Surfaces.Surface.Definition.Faces.Items = kenarItems.ToArray();

            var saveFileDialog = new SaveFileDialog() {FileName = "yuzey"};
            var icerik = KTBtoXML.Serialize(landXml);
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveFileDialog.FileName))
                    sw.WriteLine(icerik);
            }

            return landXml;
        }

        
    }
}
