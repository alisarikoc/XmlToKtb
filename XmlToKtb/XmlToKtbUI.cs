using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XmlToKtb.Properties;

namespace XmlToKtb
{
    public partial class XmlToKtbUI : Form
    {
        private string _ktbIcerigi;
        private Durumlar.Durum _durum;
        private readonly Random _rastgele = new Random();

        public XmlToKtbUI()
        {
            InitializeComponent();
            _durum = Durumlar.Durum.Basladi;
            gorunumuAyarla();
        }

        private void xmlDonustur_Tikla(object sender, EventArgs e)
        {
            var dialogResult = xmlOpenFileDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                var serializer = new DosyaDeserializer();
                var landXml = serializer.XmlDeserializer<LandXML>(xmlOpenFileDialog.FileName);
                _ktbIcerigi = ktbIcerigiDuzenleyici(landXml);
                _durum = Durumlar.Durum.Donusturuldu;
                gorunumuAyarla();
            }
        }

        private void gorunumuAyarla()
        {
            switch (_durum)
            {
                case Durumlar.Durum.Hicbiri:
                    break;
                case Durumlar.Durum.Basladi:
                    kaydetButton.Enabled = false;
                    break;
                case Durumlar.Durum.Donusturuldu:
                    kaydetButton.Enabled = true;
                    donusturButton.Enabled = false;

                    if (xmlOpenFileDialog.SafeFileName != null)
                    {
                        bilgiLabel.Text = xmlOpenFileDialog.SafeFileName.Length > 8
                            ? xmlOpenFileDialog.SafeFileName.Substring(8)
                            : xmlOpenFileDialog.SafeFileName;
                        bilgiLabel.Text += @" dosyası dönüştürüldü.";
                    }
                    break;
                case Durumlar.Durum.Kaydedildi:
                    kaydetButton.Enabled = false;
                    donusturButton.Enabled = true;
                    bilgiLabel.Text = @"Dosya kaydedildi. Yeni dosya dönüştürebilirsiniz.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var rastgeleRenk = Color.FromArgb(_rastgele.Next(256), _rastgele.Next(256), _rastgele.Next(256));
            bilgiLabel.ForeColor = rastgeleRenk;
        }

        private static string ktbIcerigiDuzenleyici(LandXML landXml)
        {
            var alignment = landXml.Alignments.Alignment;
            var start = ((LandXMLAlignmentsAlignmentCoordGeomLine)alignment.CoordGeom.Items.First()).Start.Split();
            var end = ((LandXMLAlignmentsAlignmentCoordGeomLine)alignment.CoordGeom.Items.Last()).End.Split();

            var lines = alignment.CoordGeom.Items.Where(p => p.GetType() == typeof(LandXMLAlignmentsAlignmentCoordGeomLine)).Select(p => (LandXMLAlignmentsAlignmentCoordGeomLine)p).ToArray();
            var curves = alignment.CoordGeom.Items.Where(p => p.GetType() == typeof(LandXMLAlignmentsAlignmentCoordGeomCurve)).Select(p => (LandXMLAlignmentsAlignmentCoordGeomCurve)p).ToArray();
            var spirals = alignment.CoordGeom.Items.Where(p => p.GetType() == typeof(LandXMLAlignmentsAlignmentCoordGeomSpiral)).Select(p => (LandXMLAlignmentsAlignmentCoordGeomSpiral)p).ToArray();


            var ktb = new StringBuilder("$KM0").AppendLine();
            ktb.AppendLine(alignment.staStart.ToString(CultureInfo.InvariantCulture));
            ktb.AppendLine("$SON");
            ktb.AppendLine("$YATAY");
            ktb.AppendLine("PB  " + yuvarla(start[1]) + "  " + yuvarla(start[0]) + "  0  1");

            yatayLinelariDoldur(lines, curves, ktb);

            ktb.AppendLine("PS  " + yuvarla(end[1]) + "  " + yuvarla(end[0]) + "  0  1");
            ktb.AppendLine("$SON");
            ktb.AppendLine("$GECIS");
            ktb.AppendLine("PB  1  0  0");

            gecisBilgileriniDoldur(lines, spirals, ktb);

            ktb.AppendLine("PS  1  0  0");
            ktb.AppendLine("$SON");
            ktb.AppendLine("$DUSEY");

            duseyBilgileriDoldur(alignment, ktb);

            ktb.AppendLine("$SON");

            return ktb.ToString();
        }

        private static void duseyBilgileriDoldur(LandXMLAlignmentsAlignment alignment, StringBuilder sb)
        {
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
        }

        private static void gecisBilgileriniDoldur(IReadOnlyList<LandXMLAlignmentsAlignmentCoordGeomLine> lines, IReadOnlyList<LandXMLAlignmentsAlignmentCoordGeomSpiral> spirals, StringBuilder sb)
        {
            var j = 0;
            for (var i = 0; i < lines.Count - 1; i++)
            {
                var satirNo = 2 * (i + 1);
                var oncekiLine = lines[i];
                var sonrakiLine = lines[i + 1];
                var oncekiLineEndKoordinatlari = oncekiLine.End.Split();
                var By = Convert.ToDouble(oncekiLineEndKoordinatlari[0]);
                var Bx = Convert.ToDouble(oncekiLineEndKoordinatlari[1]);
                var sonrakiLineStartKoordinatlari = sonrakiLine.Start.Split();
                var Cy = Convert.ToDouble(sonrakiLineStartKoordinatlari[0]);
                var Cx = Convert.ToDouble(sonrakiLineStartKoordinatlari[1]);


                if (spirals.Count <= j)
                { j = j - 2; }
                var oncesp = spirals[j];
                var oncekispiralStartKoordinatlari = oncesp.Start.Split();
                var OSy = Convert.ToDouble(oncekispiralStartKoordinatlari[0]);
                var OSx = Convert.ToDouble(oncekispiralStartKoordinatlari[1]);
                var sonrasp = spirals[j + 1];
                var sonraspiralStartKoordinatlari = sonrasp.End.Split();
                var SSy = Convert.ToDouble(sonraspiralStartKoordinatlari[0]);
                var SSx = Convert.ToDouble(sonraspiralStartKoordinatlari[1]);
                var oncekiSpiral = spirals[j];
                var sonrakiSpiral = spirals[j + 1];
                if (By == OSy && Bx == OSx && Cy == SSy && Cx == SSx)
                {
                    sb.AppendLine(satirNo + "  1" + "  " + Convert.ToInt32(oncekiSpiral.length) + "  " + Convert.ToInt32(sonrakiSpiral.length));
                    j = j + 2;
                }
                else if (By == OSy && Bx == OSx)
                {
                    sb.AppendLine(satirNo + "  1" + "  " + Convert.ToInt32(oncekiSpiral.length) + "  " + "0");
                    j = j + 1;
                }
                else if (Cy == SSy && Cx == SSx)
                {
                    sb.AppendLine(satirNo + "  1" + "  " + "0" + "  " + Convert.ToInt32(sonrakiSpiral.length));
                    j = j + 1;
                }

                else
                {
                    sb.AppendLine(satirNo + "  1" + "  " + "0" + "  " + "0");
                }

            }
        }

        private static void yatayLinelariDoldur(IReadOnlyList<LandXMLAlignmentsAlignmentCoordGeomLine> lines, IReadOnlyList<LandXMLAlignmentsAlignmentCoordGeomCurve> curves, StringBuilder sb)
        {
            var j = 0;
            for (var i = 0; i < lines.Count - 1; i++)
            {
                var oncekiLine = lines[i];
                var sonrakiLine = lines[i + 1];
                var kesisimNoktasi = kesisimNoktasiHesapla(oncekiLine, sonrakiLine);
                var oncekiLineEndKoordinatlari = oncekiLine.End.Split();
                var By = Convert.ToDouble(oncekiLineEndKoordinatlari[0]);
                var Bx = Convert.ToDouble(oncekiLineEndKoordinatlari[1]);
                var sonrakiLineStartKoordinatlari = sonrakiLine.Start.Split();
                var Cy = Convert.ToDouble(sonrakiLineStartKoordinatlari[0]);
                var Cx = Convert.ToDouble(sonrakiLineStartKoordinatlari[1]);
                if (By == Cy && Bx == Cx)
                    sb.AppendLine(2 * (i + 1) + "  " + yuvarla(kesisimNoktasi[0]) + "  " + yuvarla(kesisimNoktasi[1]) + " " +   "0" + "  1");
                else
                {
                    var radius1 = yuvarla(Convert.ToDouble(curves[j].radius));
                    sb.AppendLine(2 * (i + 1) + "  " + yuvarla(kesisimNoktasi[0]) + "  " + yuvarla(kesisimNoktasi[1]) + " " +    radius1 + "  1");
                    j = j + 1;
                }

            }
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



        private static void ktbDosyasiniKaydet(string content)
        {
            var saveKtbfile = new SaveFileDialog
            {
                FileName = "output",
                Filter = Resources.ktbExtensionFilter
            };

            if (saveKtbfile.ShowDialog() == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveKtbfile.FileName))
                    sw.WriteLine(content);
            }
        }

        private void kaydet_Tikla(object sender, EventArgs e)
        {
            ktbDosyasiniKaydet(_ktbIcerigi);
            _durum = Durumlar.Durum.Kaydedildi;
            gorunumuAyarla();
        }
    }
}