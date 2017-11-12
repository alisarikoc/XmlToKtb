using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XmlToKtb
{
    public partial class XmlToKtbUI : Form
    {
        public XmlToKtbUI()
        {
            InitializeComponent();
        }

        private void openLandXmlButton_Click(object sender, EventArgs e)
        {
            var dialogResult = xmlOpenFileDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                var serializer = new Deserializer();
                var landXml = serializer.XmlDeserializer<LandXML>(xmlOpenFileDialog.FileName);
                var ktbContent = ktbFileStringBuilder(landXml);
                saveToKtb(ktbContent);
            }
        }

        private static string ktbFileStringBuilder(LandXML landXml)
        {
            var alignment = landXml.Alignments.Alignment;
            var start = ((LandXMLAlignmentsAlignmentCoordGeomLine)alignment.CoordGeom.Items.First()).Start
                .Split();

            var end = ((LandXMLAlignmentsAlignmentCoordGeomLine)alignment.CoordGeom.Items.Last()).End
                .Split();

            var lines = alignment.CoordGeom.Items.Where(p => p.GetType() == typeof(LandXMLAlignmentsAlignmentCoordGeomLine)).Select(p => (LandXMLAlignmentsAlignmentCoordGeomLine)p).ToArray();
            var curves = alignment.CoordGeom.Items.Where(p => p.GetType() == typeof(LandXMLAlignmentsAlignmentCoordGeomCurve)).Select(p => (LandXMLAlignmentsAlignmentCoordGeomCurve)p).ToArray();
            var spirals = alignment.CoordGeom.Items.Where(p => p.GetType() == typeof(LandXMLAlignmentsAlignmentCoordGeomSpiral)).Select(p => (LandXMLAlignmentsAlignmentCoordGeomSpiral)p).ToArray();


            var sb = new StringBuilder("$KM0").AppendLine();
            sb.AppendLine(alignment.staStart.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("$SON");
            sb.AppendLine("$YATAY");
            sb.AppendLine("PB  " + yuvarla(start[1]) + "  " + yuvarla(start[0]) + "  0  1");

            for (var i = 0; i < lines.Length - 1; i++)
            {
                var oncekiLine = lines[i];
                var sonrakiLine = lines[i + 1];
                var kesisimNoktasi = kesisimNoktasiHesapla(oncekiLine, sonrakiLine);
                var radius = Convert.ToInt32(curves[i].radius);
                sb.AppendLine(2 * (i+1) + "  " + yuvarla(kesisimNoktasi[0]) + "  " + yuvarla(kesisimNoktasi[1]) + " " + radius + "  1");
            }
            sb.AppendLine("PS  " + yuvarla(end[1]) + "  " + yuvarla(end[0]) + "  0  1");
            sb.AppendLine("$SON");
            sb.AppendLine("$GECIS");
            sb.AppendLine("PB  1  0  0");

            for (var i = 0; i < lines.Length - 1; i++)
            {
                var index = 2 * i;
                var satirNo = 2 * (i + 1);
                var oncekiSpiral = spirals[index];
                var sonrakiSpiral = spirals[index + 1];
                sb.AppendLine(satirNo + "  1" + "  " + Convert.ToInt32(oncekiSpiral.length) + "  " + Convert.ToInt32(sonrakiSpiral.length));
            }
            sb.AppendLine("PS  1  0  0");
            sb.AppendLine("$SON");
            sb.AppendLine("$DUSEY");

            var profAlign = alignment.Profile.ProfAlign;
            foreach (var item in profAlign.Items)
            {
                switch (item)
                {
                    case string _:
                    {
                        var ikiDeger = item.ToString().Split();
                        var ilkDeger = yuvarla(ikiDeger[0]);
                        var ikinciDeger = yuvarla(ikiDeger[1]);
                        sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  0  0");

                        break;
                    }
                    case LandXMLAlignmentsAlignmentProfileProfAlignParaCurve _:
                    {
                        var paraCurve = (LandXMLAlignmentsAlignmentProfileProfAlignParaCurve)item;
                        var ikiDeger = paraCurve.Value.Split();

                        var ilkDeger = yuvarla(ikiDeger[0]);
                        var ikinciDeger = yuvarla(ikiDeger[1]);
                        var length = yuvarla(paraCurve.length);
                        sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  " + length + "  1");
                        break;
                    }
                    case LandXMLAlignmentsAlignmentProfileProfAlignCircCurve _:
                    {
                        var circCurve = (LandXMLAlignmentsAlignmentProfileProfAlignCircCurve)item;
                        var ikiDeger = circCurve.Value.Split();

                        var ilkDeger = yuvarla(ikiDeger[0]);
                        var ikinciDeger = yuvarla(ikiDeger[1]);
                        var length = yuvarla(circCurve.length);
                        sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  " + length + "  1");
                        break;
                        }
                }
            }
            sb.AppendLine("$SON");

            return sb.ToString();
        }

        private static decimal yuvarla(object deger)
        {
            return Math.Round(Convert.ToDecimal(deger.ToString()), 4, MidpointRounding.AwayFromZero);
        }

        private static double[] kesisimNoktasiHesapla(LandXMLAlignmentsAlignmentCoordGeomLine oncekiLine, LandXMLAlignmentsAlignmentCoordGeomLine sonrakiLine)
        {
            var sonrakiLineEndKoordinatlari = sonrakiLine.End.Split();
            var Dy = Convert.ToDouble(sonrakiLineEndKoordinatlari[0]);
            var Dx = Convert.ToDouble(sonrakiLineEndKoordinatlari[1]);

            var sonrakiLineStartKoordinatlari = sonrakiLine.Start.Split();
            var Cy = Convert.ToDouble(sonrakiLineStartKoordinatlari[0]);
            var Cx = Convert.ToDouble(sonrakiLineStartKoordinatlari[1]);


            var oncekiLineEndKoordinatlari = oncekiLine.End.Split();
            var By = Convert.ToDouble(oncekiLineEndKoordinatlari[0]);
            var Bx = Convert.ToDouble(oncekiLineEndKoordinatlari[1]);

            var oncekiLineStartKoordinatlari = oncekiLine.Start.Split();
            var Ay = Convert.ToDouble(oncekiLineStartKoordinatlari[0]);
            var Ax = Convert.ToDouble(oncekiLineStartKoordinatlari[1]);

            var T = ((Dy - Cy) * (Cx - Ax) - (Cy - Ay) * (Dx - Cx)) /
                       ((Dy - Cy) * (Bx - Ax) - (By - Ay) * (Dx - Cx));

            var Ex = Ax + (Bx - Ax) * T;
            var Ey = Ay + (By - Ay) * T;

            var kesisimKoordinatlari = new[] { Ex, Ey };

            return kesisimKoordinatlari;
        }

        private static void saveToKtb(string content)
        {
            var saveKtbfile = new SaveFileDialog { FileName = "YN2.KTB" };

            if (saveKtbfile.ShowDialog() == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveKtbfile.FileName))
                    sw.WriteLine(content);
            }
        }
    }
}
