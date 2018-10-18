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

        private static decimal SpiralIn(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "-1" : "1";
            var aKare = Convert.ToDecimal(spiral.radiusEnd) * spiral.length;

            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " 1 " + spiral.Start.Split()[1] + " " + spiral.Start.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");

            return guncelKm + spiral.length;
        }

        private static decimal SpiralOut(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "1" : "-1";
            var aKare = Convert.ToDecimal(spiral.radiusStart) * spiral.length;

            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " -1 " + spiral.End.Split()[1] + " " + spiral.End.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");
            return guncelKm + spiral.length;
        }

        private static decimal CurveIn(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "-1" : "1";
            var aKare = Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length;

            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " 1 " + spiral.Start.Split()[1] + " " + spiral.Start.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");
            return guncelKm + spiral.length;
        }

        private static decimal CurveOut(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "1" : "-1";
            var aKare = Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length;

            ktb.AppendLine("SP " + guncelKm + " " + spiral.length + " " + aKare + " " + yon + " -1 " + spiral.End.Split()[1] + " " + spiral.End.Split()[0] + " " + spiral.PI.Split()[1] + " " + spiral.PI.Split()[0] + " 0 0");
            return guncelKm + spiral.length;
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

        private static decimal yuvarla(object deger)
        {
            return Math.Round(Convert.ToDecimal(deger.ToString()), 4, MidpointRounding.AwayFromZero);
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