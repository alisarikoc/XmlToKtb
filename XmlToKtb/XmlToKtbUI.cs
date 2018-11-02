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
        private static string SifirDolgusu(decimal deger)
        {
            return Convert.ToInt32(deger*1000).ToString().PadLeft(16, '0');
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
        //TODO bunu kopyala
        private static string alnIcerigiDuzenleyici(LandXML landXml)
        {
            var alignment = landXml.Alignments.Alignment;

            var aln = new StringBuilder("*41....+"+"00000000001_CRT1"+" 42....+00000000HZALIGNM 43....+00000000STACOORD").AppendLine();//00000000001_CRT1 yazısı kayıtta yazılan dosya isimi olacak ve başına sıfır koyarak 16 hane olacak
            var baslangicKM = alignment.staStart;
            var guncelKM = baslangicKM;
            aln.AppendLine(baslangicKM.ToString(CultureInfo.InvariantCulture));
            aln.AppendLine("$SON"); //son noktanın koordinatı EOP olarak yazılacak

            foreach (var item in alignment.CoordGeom.Items)
            {
                switch (item)
                {
                    case LandXMLAlignmentsAlignmentCoordGeomLine _:
                        {
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
            aln.AppendLine("*11....+" + SifirDolgusu(guncelKm) + " 71....+00000000STRAIGHT 72....+0000000000000NON 73....+0000000000000000" + " 81..10+"+SifirDolgusu(Convert.ToDecimal(line.Start.Split()[1])) + " 82..10+" + SifirDolgusu(Convert.ToDecimal(line.Start.Split()[0])));

            return guncelKm + line.length;
        }
        private static decimal CurveYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomCurve curve, decimal guncelKm)
        {
            var yon = curve.rot == "ccw" ? "-1" : "1";
            aln.AppendLine("*11....+" + SifirDolgusu(guncelKm) + " 71....+00000000000CURVE" + " 72...."+yon+SifirDolgusu(Convert.ToDecimal(curve.radius))+ " 73....+0000000000000000"+ " 81..10+" + SifirDolgusu(Convert.ToDecimal(curve.Start.Split()[1])) + " 82..10+" + SifirDolgusu(Convert.ToDecimal(curve.Start.Split()[0])));
            return guncelKm + curve.length;
        }
        private static decimal SpiralYazdirALN(StringBuilder aln, LandXMLAlignmentsAlignmentCoordGeomSpiral spiral, decimal guncelKm)
        {
            if (spiral.radiusStart.ToUpper() == "INF")
            {
                var yon = spiral.rot == "ccw" ? "-1" : "1";
                var aKare = Convert.ToDecimal(spiral.radiusEnd) * spiral.length;
                aln.AppendLine("*11....+" + SifirDolgusu(guncelKm) + " 71....+0000000000SPIRIN" + " 72...." + yon + SifirDolgusu(Convert.ToDecimal(spiral.radiusEnd))+ " 73....+ 0000000000000000"+ " 81..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));
             }
            else if (spiral.radiusEnd.ToUpper() == "INF")
            {
                var yon = spiral.rot == "ccw" ? "-1" : "1";
                var aKare = Convert.ToDecimal(spiral.radiusStart) * spiral.length;
                aln.AppendLine("*11....+" + SifirDolgusu(guncelKm) + " 71....+000000000SPIROUT" + " 72...." + yon + SifirDolgusu(Convert.ToDecimal(spiral.radiusStart)) + " 73....+ 0000000000000000" + " 81..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));
            }
            else
            {
                if (Convert.ToDecimal(spiral.radiusStart) >= Convert.ToDecimal(spiral.radiusEnd))
                {
                    var yon = spiral.rot == "ccw" ? "-1" : "1";
                    var aKare = Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length;
                    aln.AppendLine("*11....+" + SifirDolgusu(guncelKm) + " 71....+000000000CURVEIN" + " 72...." + yon + SifirDolgusu(Convert.ToDecimal(spiral.radiusStart)) + " 73....+ 0000000000000000" + " 74....+" + SifirDolgusu(Convert.ToDecimal(spiral.radiusEnd)) + " 81..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));
                }
                else
                {
                    var yon = spiral.rot == "ccw" ? "-1" : "1";
                    var aKare = Math.Abs(Convert.ToDecimal(spiral.radiusStart) * Convert.ToDecimal(spiral.radiusEnd) / (Convert.ToDecimal(spiral.radiusStart) - Convert.ToDecimal(spiral.radiusEnd))) * spiral.length;
                    aln.AppendLine("*11....+" + SifirDolgusu(guncelKm) + " 71....+00000000CURVEOUT" + " 72...." + yon + SifirDolgusu(Convert.ToDecimal(spiral.radiusStart)) + " 73....+ 0000000000000000" + " 74....+" + SifirDolgusu(Convert.ToDecimal(spiral.radiusEnd)) + " 81..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[1])) + " 82..10 + " + SifirDolgusu(Convert.ToDecimal(spiral.Start.Split()[0])));

                }
            }
            // aln.AppendLine("*11....+" + SifirDolgusu(guncelKm) + " 71....+0000000000000EOP 72....+0000000000000NON 73....+0000000000000000" + " 81..10+"+SifirDolgusu(Convert.ToDecimal(line.Start.Split()[1])) + " 82..10+" + SifirDolgusu(Convert.ToDecimal(line.Start.Split()[0])));
            // SifirDolgusu(guncelKm)                                   yerine ; guncelKm+ sonobjenin length'i 
            // SifirDolgusu(Convert.ToDecimal(line.Start.Split()[1]))   yerine ; son objenin End.Split()[1] 'i
            // SifirDolgusu(Convert.ToDecimal(line.Start.Split()[0]))   yerine ; son objenin End.Split()[0] 'ı son satır olarak yazılmarı



            return guncelKm + spiral.length;
        }

        private static void PRFicerigiDoldur(LandXMLAlignmentsAlignment alignment, StringBuilder prf)
        {
            if (alignment.Profile == null) { return; }
            var prf = new StringBuilder("*41....+0000000000" + "1_CRT1" + " 42....+000000000VALIGNM 43....+00000000STACOORD").AppendLine();//1-CTR1 dosya ismi olacak
            var profAlign = alignment.Profile.ProfAlign;
            var profAlignItems = alignment.Profile.ProfAlign.Items.ToList();
            foreach (var item in profAlign.Items)
            {
                object oncekiItem;
                object sonrakiItem;
                if (profAlignItems.IndexOf(item) == 0)//ilk item
                {
                    oncekiItem = null;
                    sonrakiItem = profAlignItems[profAlignItems.IndexOf(item) + 1];
                }
                else if (profAlignItems.IndexOf(item) == profAlignItems.Count - 1)//son item
                {
                    oncekiItem = profAlignItems[profAlignItems.IndexOf(item) - 1];
                    sonrakiItem = null;
                }
                else//aradaki itemlar

                {

                    oncekiItem = profAlignItems[profAlignItems.IndexOf(item) - 1];

                    sonrakiItem = profAlignItems[profAlignItems.IndexOf(item) + 1];

                }

                bool ilkItemIse = oncekiItem == null;
                bool sonItemIse = sonrakiItem == null;

                var detaylar = GetirProfilAlignDetaylar(item);
                var oncekiDetaylar = ilkItemIse ? new decimal[] { 0, 0, 0 } : GetirProfilAlignDetaylar(oncekiItem);
                var sonrakiDetaylar = sonItemIse ? new decimal[] { 0, 0, 0 } : GetirProfilAlignDetaylar(sonrakiItem);
                decimal oncekiKM = oncekiDetaylar[0];
                decimal oncekiKOT = oncekiDetaylar[1];
                decimal oncekiLength = oncekiDetaylar[2];

                decimal sonrakiKM = sonrakiDetaylar[0];
                decimal sonrakiKOT = sonrakiDetaylar[1];
                decimal sonrakilength = sonrakiDetaylar[2];

                decimal KM = detaylar[0];
                decimal KOT = detaylar[1];
                decimal Length = detaylar[2];
                //TODO Ali Sarıkoç hesaplamalarını burada yap onceki üç ve sonraki üç değer yukarıda oluşturdm onları kullanarak hesaplama yap.
                if (ilkItemIse)

                {
                    prf.AppendLine("*11....+"+ SifirDolgusu(KM) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + SifirDolgusu(KOT));
                }
                else if (sonItemIse)

                {
                    prf.AppendLine("*11....+" + SifirDolgusu(KM) + " 71....+0000000000000EOP 72....+" + "0000000000000NON 83..10+" + SifirDolgusu(KOT));
                }
                else

                {
                    var g1 = (KOT - oncekiKOT) / (KM - oncekiKM);
                    var g2 = (sonrakiKOT - KOT) / (sonrakiKM - KM);
                    if (Math.Abs(g1-g2) > 0.001m )
                    {
                        KM = KM - Length / 2;
                        KOT=KOT-(g1*Length/2);
                        if (Length==0)
                        {
                            prf.AppendLine("*11....+" + SifirDolgusu(KM) + " 71....+" + "00000000STRAIGHT 72....+" + "0000000000000NON 83..10+" + SifirDolgusu(KOT));
                        }
                        else if(detaylar[3]==1)
                        {
                            var K100 = 100*Length / Math.Abs(g1 - g2);
                            prf.AppendLine("*11....+" + SifirDolgusu(KM) + " 71....+" + "00000000PARABOLA 72....+" + SifirDolgusu(K100)+" 83..10+" + SifirDolgusu(KOT));
                        }
                        else if(detaylar[3]==2)
                        {
                            prf.AppendLine("*11....+" + SifirDolgusu(KM) + " 71....+" + "00000000000CURVE 72....+" + SifirDolgusu(detaylar[4]) + " 83..10+" + SifirDolgusu(KOT));
                        }
                    }


                }
               // prf.AppendLine(Ilk + "  " + Ikinci + "  " + length + "  0");//TODO Ali Sarıkoç Burayı hesapladıktan sonra hesaplananlarla güncelle
            }
            return prf.ToString();
        }

        private static decimal[] GetirProfilAlignDetaylar(object item)

        {

            var detaylar = new decimal[3];



            switch (item)

            {

                case string _:

                    {

                        var ikiDeger = item.ToString().Split();

                        detaylar[0] = yuvarla(ikiDeger[0]);

                        detaylar[1] = yuvarla(ikiDeger[1]);

                        detaylar[2] = 0m;

                        //sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  0  0");



                        break;

                    }

                case LandXMLAlignmentsAlignmentProfileProfAlignParaCurve _:

                    {

                        var paraCurve = (LandXMLAlignmentsAlignmentProfileProfAlignParaCurve)item;

                        var ikiDeger = paraCurve.Value.Split();

                        detaylar[0] = yuvarla(ikiDeger[0]);

                        detaylar[1] = yuvarla(ikiDeger[1]);

                        detaylar[2] = yuvarla(paraCurve.length);
                        detaylar[3] = 1m;

                        //sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  " + length + "  1");

                        break;

                    }

                case LandXMLAlignmentsAlignmentProfileProfAlignCircCurve _:

                    {

                        var circCurve = (LandXMLAlignmentsAlignmentProfileProfAlignCircCurve)item;

                        var ikiDeger = circCurve.Value.Split();



                        detaylar[0] = yuvarla(ikiDeger[0]);

                        detaylar[1] = yuvarla(ikiDeger[1]);

                        detaylar[2] = yuvarla(circCurve.length);
                        detaylar[3] = 2m;
                        detaylar[4] = yuvarla(circCurve.radius);

                        //sb.AppendLine(ilkDeger + "  " + ikinciDeger + "  " + length + "  1");

                        break;

                    }

            }



            return detaylar;

        }



    }
    
}