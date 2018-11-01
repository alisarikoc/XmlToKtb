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

        //TODO bunu kopyala
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
            var aKare = Convert.ToDouble(Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length);
            var starty = (Convert.ToDouble(spiral.Start.Split()[1]));
            var startx = (Convert.ToDouble(spiral.Start.Split()[0]));
            var endy = (Convert.ToDouble(spiral.End.Split()[1]));
            var endx = (Convert.ToDouble(spiral.End.Split()[0]));
            var semt = Math.Atan2((endy - starty), (endx - startx));
            if (semt < 0)
            {
                semt = semt + (2 * Math.PI);
            }
            var L1 = aKare / Convert.ToDouble(spiral.radiusStart);// Büyük yarıçap KÜÇÜK DEĞER
            var L2 = aKare / Convert.ToDouble(spiral.radiusEnd);//Küçük yarıçap    BÜYÜK DEĞER
            var sipkatsayi = Math.Sqrt(aKare);
            double X1 = 0, X2 = 0, Y1 = 0, Y2 = 0;

            for (int n = 1; n <= 4; n++)
            {
                int faktorx = 2 * n - 1;
                int faktory = 2 * n - 2;
                X1 = X1 + Math.Pow(-1, n - 1) * Math.Pow(L1, 4 * n - 1) / ((4 * n - 1) * (Faktoryel(faktorx)) * Math.Pow(sipkatsayi, (4 * n - 2)) * Math.Pow(2, (2 * n - 1)));
                X2 = X2 + Math.Pow(-1, n - 1) * Math.Pow(L2, 4 * n - 1) / ((4 * n - 1) * (Faktoryel(faktorx)) * Math.Pow(sipkatsayi, (4 * n - 2)) * Math.Pow(2, (2 * n - 1)));
            }
            for (int n = 1; n <= 5; n++)
            {
                int faktorx = 2 * n - 1;
                int faktory = 2 * n - 2;
                Y1 = Y1 + Math.Pow(-1, n - 1) * Math.Pow(L1, 4 * n - 3) / ((4 * n - 3) * (Faktoryel(faktory)) * Math.Pow(sipkatsayi, (4 * n - 4)) * Math.Pow(2, (2 * n - 2)));
                Y2 = Y2 + Math.Pow(-1, n - 1) * Math.Pow(L2, 4 * n - 3) / ((4 * n - 3) * (Faktoryel(faktory)) * Math.Pow(sipkatsayi, (4 * n - 4)) * Math.Pow(2, (2 * n - 2)));
            }


            double semt1, spiralEndY, spiralEndX, semt2, spiralStartY, spiralStartX;
            
            if (L2 > L1 && Y1 > Y2)
            {
                var kiris = Math.Sqrt(Math.Pow((endy - starty), 2) + Math.Pow((endx - startx), 2));
                var ort_yaricap = (Convert.ToDouble(spiral.radiusStart) + Convert.ToDouble(spiral.radiusEnd)) / 2;
                var semt3 = semt + (Math.PI - 2 * Math.Asin(kiris / 2 / ort_yaricap)) / 2 * Convert.ToInt32(yon);
                var Ym = starty + Math.Sin(semt3) * ort_yaricap;
                var Xm = startx + Math.Cos(semt3) * ort_yaricap;
                ktb.AppendLine("AR " + guncelKm + " " + ort_yaricap + " " + yon + " 0 0 " + Ym + " " + Xm + " " + starty + " " + startx + " " + endy + " " + endx);
                return guncelKm + spiral.length;
            }
            else
            {
                semt1 = semt - Math.Atan(Math.Abs(X2 - X1) / Math.Abs(Y2 - Y1)) * (Convert.ToInt32(yon)) - (Convert.ToInt32(yon) * (Math.PI) / 2);
                spiralEndY = Math.Sin(semt1) * X2 + endy;
                spiralEndX = Math.Cos(semt1) * X2 + endx;
                semt2 = semt1 - (Convert.ToInt32(yon) * (Math.PI) / 2);
                spiralStartY = Math.Sin(semt2) * Y2 + spiralEndY;
                spiralStartX = Math.Cos(semt2) * Y2 + spiralEndX;
            }



            //ktb.AppendLine(L1+" " + L2 + " " + sipkatsayi + " " + Y1 + " " + Y2 + "  " + X1 + " " + X2 + " " + semt*200/Math.PI + " " + semt1*200/Math.PI + " " + L2 + " 0");
            ktb.AppendLine("SP " + guncelKm + " " + L2 + " " + aKare + " " + yon + " 1 " + spiralStartY + " " + spiralStartX + " " + spiralEndY + " " + spiralEndX + " " + L1 + " 0");
            return guncelKm + spiral.length;
        }

        private static decimal CurveOut(StringBuilder ktb, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            bool saatYonuIse = spiral.rot == "ccw";
            var yon = saatYonuIse ? "1" : "-1";
            var aKare = Convert.ToDouble(Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length);
            var starty = (Convert.ToDouble(spiral.Start.Split()[1]));
            var startx = (Convert.ToDouble(spiral.Start.Split()[0]));
            var endy = (Convert.ToDouble(spiral.End.Split()[1]));
            var endx = (Convert.ToDouble(spiral.End.Split()[0]));
            var semt = Math.Atan2((endy - starty), (endx - startx));
            if (semt < 0)
            {
                semt = semt + (2 * Math.PI);
            }

            var L1 = aKare / Convert.ToDouble(spiral.radiusStart);// Küçük yarıçap BÜYÜK DEĞER
            var L2 = aKare / Convert.ToDouble(spiral.radiusEnd);//Büyük yarıçap    KÜÇÜK DEĞER
            var sipkatsayi = Math.Sqrt(aKare);
            double X1 = 0, X2 = 0, Y1 = 0, Y2 = 0;
            for (int n = 1; n <= 4; n++)
            {
                int faktorx = 2 * n - 1;
                int faktory = 2 * n - 2;
                X1 = X1 + Math.Pow(-1, n - 1) * Math.Pow(L1, 4 * n - 1) / ((4 * n - 1) * (Faktoryel(faktorx)) * Math.Pow(sipkatsayi, (4 * n - 2)) * Math.Pow(2, (2 * n - 1)));
                X2 = X2 + Math.Pow(-1, n - 1) * Math.Pow(L2, 4 * n - 1) / ((4 * n - 1) * (Faktoryel(faktorx)) * Math.Pow(sipkatsayi, (4 * n - 2)) * Math.Pow(2, (2 * n - 1)));
            }
            for (int n = 1; n <= 5; n++)
            {
                int faktorx = 2 * n - 1;
                int faktory = 2 * n - 2;
                Y1 = Y1 + Math.Pow(-1, n - 1) * Math.Pow(L1, 4 * n - 3) / ((4 * n - 3) * (Faktoryel(faktory)) * Math.Pow(sipkatsayi, (4 * n - 4)) * Math.Pow(2, (2 * n - 2)));
                Y2 = Y2 + Math.Pow(-1, n - 1) * Math.Pow(L2, 4 * n - 3) / ((4 * n - 3) * (Faktoryel(faktory)) * Math.Pow(sipkatsayi, (4 * n - 4)) * Math.Pow(2, (2 * n - 2)));
            }
            double semt1;
            //var semt1 = semt - Math.Atan(Math.Abs(X2 - X1) / Math.Abs(Y2 - Y1)) * Convert.ToInt32(yon) + Math.PI / 2 * Convert.ToInt32(yon);
            if (L1 > L2 && Y2 > Y1)
            {
                var kiris = Math.Sqrt(Math.Pow((endy - starty), 2) + Math.Pow((endx - startx), 2));
                var ort_yaricap = (Convert.ToDouble(spiral.radiusStart) + Convert.ToDouble(spiral.radiusEnd)) / 2;
                var semt2 = semt - (Math.PI- 2*Math.Asin(kiris/2/ort_yaricap))/2 * Convert.ToInt32(yon);
                var Ym = starty +Math.Sin(semt2) * ort_yaricap;
                var Xm = startx + Math.Cos(semt2) * ort_yaricap;

                ktb.AppendLine("AR " + guncelKm + " " + ort_yaricap + " " + Convert.ToInt32(yon)*-1 + " 0 0 " + Ym + " " + Xm + " " + starty + " " + startx + " " + endy + " " + endx);
                return guncelKm + spiral.length;
            }
            else
            {
                semt1 = semt - Math.Atan(Math.Abs(X2 - X1) / Math.Abs(Y2 - Y1)) * Convert.ToInt32(yon) + Math.PI / 2 * Convert.ToInt32(yon);
                var spiralStartY = Math.Sin(semt1) * X1 + starty;
                var spiralStartX = Math.Cos(semt1) * X1 + startx;
                var spiralEndY = Math.Sin(semt1) * X2 + endy + Math.Sin(semt1 + (Convert.ToInt32(yon) * (-1) * (Math.PI) / 2)) * Y2;
                var spiralEndX = Math.Cos(semt1) * X2 + endx + Math.Cos(semt1 + (Convert.ToInt32(yon) * (-1) * (Math.PI) / 2)) * Y2;

                ktb.AppendLine("SP " + guncelKm + " " + L1 + " " + aKare + " " + yon + " -1 " + spiralEndY + " " + spiralEndX + " " + spiralStartY + " " + spiralStartX + " " + L2 + " 0");
                //ktb.AppendLine(L1+" " + L2 + " " + sipkatsayi + " " + Y1 + " " + Y2 + "  " + X1 + " " + X2 + " " + semt*200/Math.PI + " " + semt1*200/Math.PI + " " + L2 + " 0");
            }


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
        private static double Faktoryel(double faktor)
        {
            
            double  sonuc= 1;
            for (int i = 1; i <= faktor; i++)
            {
                sonuc = sonuc * i;
            }
            if (faktor == 0) sonuc = 1;
            return sonuc;
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
        
        private void alnPrfKaydet_Click(object sender, EventArgs e)
        {
            ktbDosyasiniKaydet(_ktbIcerigi);
            _durum = Durumlar.Durum.Kaydedildi;
            gorunumuAyarla();
        }

        private void alnPrfDonusturButton_Click(object sender, EventArgs e)
        {
            var dialogResult = xmlOpenFileDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                var serializer = new DosyaDeserializer();
                var landXml = serializer.XmlDeserializer<LandXML>(xmlOpenFileDialog.FileName);

                //TODO burası değişecek
                _ktbIcerigi = ktbIcerigiDuzenleyici(landXml);
                _durum = Durumlar.Durum.Donusturuldu;
                gorunumuAyarla();
            }
        }
    }
}