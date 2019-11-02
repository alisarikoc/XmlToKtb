using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Common;
using Common.Enum;
using XmlToKtb.Properties;

namespace XmlToKtb
{
    public partial class XmlToKtbUI : Form
    {

        private string ktbIcerigi;
        private string alnIcerigi;
        private string prfIcerigi;
        private string xmlIcerigi;
        private static string dosyaAdi;
        private static string rootPath;
        private DurumTip durumTip;
        private DosyaTip dosyaTip;
        private readonly Random _rastgele = new Random();

        public XmlToKtbUI()
        {
            InitializeComponent();
            durumTip = DurumTip.Basladi;
            dosyaTuruComboBox.SelectedIndex = (int)DosyaTip.KTB;
            gorunumuAyarla();
        }

        private void xmlDonustur_Tikla(object sender, EventArgs e)
        {
            var dialogResult = openFileDialog.ShowDialog();

            var dosyaUzantisi = Path.GetExtension(openFileDialog.FileName);
            if (dosyaUzantisi != null && dosyaUzantisi.ToLower().Equals(".ktb"))
            {
                kayitTuruLabel.Visible = false;
                dosyaTuruComboBox.Visible = false;
                dosyaTip = DosyaTip.XML;

                StreamReader ktbStreamReader = new StreamReader(openFileDialog.FileName);
                string shortName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                var xmlData = KTBtoXML.GetirXmlData(ktbStreamReader);
                LandXML landXml = KTBtoXML.LandXmlOlustur(shortName, xmlData);

                xmlIcerigi = KTBtoXML.Serialize(landXml);
            }
            else if (dialogResult == DialogResult.OK)
            {
                kayitTuruLabel.Visible = true;
                dosyaTuruComboBox.Visible = true;

                var serializer = new DosyaDeserializer();
                var landXml = serializer.XmlDeserializer<LandXML>(openFileDialog.FileName);
                dosyaTip = (DosyaTip)dosyaTuruComboBox.SelectedIndex;

                switch (dosyaTip)
                {
                    case DosyaTip.KTB:

                        ktbIcerigi = KTBIcerigi.Duzenleyici(landXml);

                        break;

                    case DosyaTip.ALNPRF:

                        dosyaAdi = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                        rootPath = Path.GetDirectoryName(openFileDialog.FileName);
                        alnIcerigi = ALNIcerigi.Duzenleyici(landXml, dosyaAdi);
                        prfIcerigi = PrfIcerigi.Duzenleyici(landXml.Alignments.Alignment, dosyaAdi);

                        break;
                }
            }
            durumTip = DurumTip.Donusturuldu;
            gorunumuAyarla();
        }

        private void gorunumuAyarla()
        {
            switch (durumTip)
            {
                case DurumTip.Hicbiri:
                    break;
                case DurumTip.Basladi:
                    kaydetButton.Enabled = false;
                    break;
                case DurumTip.Donusturuldu:
                    kaydetButton.Enabled = true;
                    donusturButton.Enabled = false;

                    if (openFileDialog.SafeFileName != null)
                    {
                        bilgiLabel.Text = openFileDialog.SafeFileName.Length > 8
                            ? openFileDialog.SafeFileName.Substring(8)
                            : openFileDialog.SafeFileName;
                        bilgiLabel.Text += @" dosyası dönüştürüldü.";
                    }
                    break;
                case DurumTip.Kaydedildi:
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

        private static void DosyaKaydetFiltreIle(string icerik, string filtre)
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = "output",
                Filter = filtre
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveFileDialog.FileName))
                    sw.WriteLine(icerik);
            }
        }

        private static void alnPrfDosyasiniKaydet(string alnIcerigi, string prfIcerigi)
        {
            var saveAlnPrfFile = new FolderBrowserDialog
            {
                SelectedPath = rootPath

            };

            if (saveAlnPrfFile.ShowDialog() == DialogResult.OK)
            {
                string alnFileFullPath = saveAlnPrfFile.SelectedPath + "/ALN" + dosyaAdi + ".gsi";
                string prfFileFullPath = saveAlnPrfFile.SelectedPath + "/PRF" + dosyaAdi + ".gsi";

                if (!File.Exists(alnFileFullPath))
                {
                    using (var sw = new StreamWriter(alnFileFullPath))
                        sw.WriteLine(alnIcerigi);
                }
                else
                {
                    var dialogResult = MessageBox.Show("Aynı isimde bir ALN dosyası mevcut. Üzerine yazılsın mı?", "Uyarı", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        using (var sw = new StreamWriter(alnFileFullPath))
                            sw.WriteLine(alnIcerigi);
                    }
                }

                if (!File.Exists(prfFileFullPath))
                {
                    using (var sw = new StreamWriter(prfFileFullPath))
                        sw.WriteLine(prfIcerigi);
                }
                else
                {
                    var dialogResult = MessageBox.Show("Aynı isimde bir PRF dosyası mevcut. Üzerine yazılsın mı?", "Uyarı", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        using (var sw = new StreamWriter(prfFileFullPath))
                            sw.WriteLine(prfIcerigi);
                    }
                }
            }
        }

        private void kaydet_Tikla(object sender, EventArgs e)
        {
            switch (dosyaTip)
            {
                case DosyaTip.KTB:

                    DosyaKaydetFiltreIle(ktbIcerigi, Resources.ktbExtensionFilter);
                    break;

                case DosyaTip.ALNPRF:

                    alnPrfDosyasiniKaydet(alnIcerigi, prfIcerigi);
                    break;

                case DosyaTip.XML:

                    DosyaKaydetFiltreIle(xmlIcerigi, Resources.xmlExtensionFilter);
                    break;
            }
            durumTip = DurumTip.Kaydedildi;
            gorunumuAyarla();
        }

        private void dosyaTuruComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            dosyaTip = (DosyaTip)dosyaTuruComboBox.SelectedIndex;
        }

        private void enkesitButton_Click(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> ncnData = GetirNcnData();

            List<Array> kesitler = KSEIcerigi.KseGetir(landXml, ncnData);
            string kseIcerigi = KSEIcerigi.Duzenleyici(kesitler);

            DosyaKaydetFiltreIle(kseIcerigi, Resources.kseExtensionFilter);
        }

        private LandXML GetirLandXmlDosyaTipiIle()
        {
            openFileDialog.ShowDialog();

            var dosyaUzantisi = Path.GetExtension(openFileDialog.FileName);
            LandXML landXml = new LandXML();

            if (dosyaUzantisi != null && dosyaUzantisi.ToLower().Equals(".ktb"))
            {
                StreamReader ktbStreamReader = new StreamReader(openFileDialog.FileName);
                string shortName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                var xmlData = KTBtoXML.GetirXmlData(ktbStreamReader);
                landXml = KTBtoXML.LandXmlOlustur(shortName, xmlData);
            }
            else if (dosyaUzantisi != null && dosyaUzantisi.ToLower().Equals(".xml"))
            {
                var serializer = new DosyaDeserializer();
                landXml = serializer.XmlDeserializer<LandXML>(openFileDialog.FileName);
            }

            return landXml;
        }

        private List<Array> GetirNcnData()
        {
            ncnOpenFileDialog.ShowDialog();
            StreamReader ncnIcerigi = new StreamReader(ncnOpenFileDialog.FileName);

            List<Array> tumsatirlar = new List<Array>();

            while (!ncnIcerigi.EndOfStream)
            {
                char[] separator = { ' ', '\t' };
                var veriSatir = ncnIcerigi.ReadLine();
                var veriKelimeleri = veriSatir.Split(separator, (StringSplitOptions)1);
                tumsatirlar.Add(veriKelimeleri);
            }

            return tumsatirlar;
        }

        private void tipKesitButton_Click(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();

        }

        //TODO burak burası farklı sınıfa alınacak

        //private void openFileButton_Click(object sender, EventArgs e)
        //{
        //    openVeriFileDialog.FileName = "Veriler Aç";
        //    openVeriFileDialog.ShowDialog();

        //    var ktbIcerigi = new StreamReader(openVeriFileDialog.FileName);
        //    string shortName = Path.GetFileNameWithoutExtension(openVeriFileDialog.FileName);
        //    var xmlData = KTBtoXML.GetirXmlData(ktbIcerigi);

        //    LandXML landXml = KTBtoXML.LandXmlOlustur(shortName, xmlData);

        //    KTBtoXML.Serialize(landXml);
        //}



    }
}