using System;
using System.Drawing;
using System.IO;
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
            var dialogResult = xmlOpenFileDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                var serializer = new DosyaDeserializer();
                var landXml = serializer.XmlDeserializer<LandXML>(xmlOpenFileDialog.FileName);
                dosyaTip = (DosyaTip)dosyaTuruComboBox.SelectedIndex;

                switch (dosyaTip)
                {
                    case DosyaTip.KTB:

                        ktbIcerigi = KTBIcerigi.Duzenleyici(landXml);

                        break;

                    case DosyaTip.ALNPRF:
                        
                        dosyaAdi = Path.GetFileNameWithoutExtension(xmlOpenFileDialog.FileName);
                        rootPath = Path.GetDirectoryName(xmlOpenFileDialog.FileName);
                        alnIcerigi = ALNIcerigi.Duzenleyici(landXml, dosyaAdi);
                        prfIcerigi = PrfIcerigi.Duzenleyici(landXml.Alignments.Alignment, dosyaAdi);

                        break;
                }

                durumTip = DurumTip.Donusturuldu;
                gorunumuAyarla();
            }
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

                    if (xmlOpenFileDialog.SafeFileName != null)
                    {
                        bilgiLabel.Text = xmlOpenFileDialog.SafeFileName.Length > 8
                            ? xmlOpenFileDialog.SafeFileName.Substring(8)
                            : xmlOpenFileDialog.SafeFileName;
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

                    ktbDosyasiniKaydet(ktbIcerigi);
                    break;

                case DosyaTip.ALNPRF:

                    alnPrfDosyasiniKaydet(alnIcerigi, prfIcerigi);
                    break;
            }
            durumTip = DurumTip.Kaydedildi;
            gorunumuAyarla();
        }

        private void dosyaTuruComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            dosyaTip = (DosyaTip)dosyaTuruComboBox.SelectedIndex;
        }
    }
}