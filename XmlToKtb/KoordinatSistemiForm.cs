using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using XmlToKtb.Projeksiyon;

namespace XmlToKtb
{
    public partial class KoordinatSistemiForm : Form
    {
        private static readonly ProjeksiyonSistemi[] SistemSirasi =
        {
            ProjeksiyonSistemi.Tanimsiz,
            ProjeksiyonSistemi.Ed50,
            ProjeksiyonSistemi.Itrf96,
            ProjeksiyonSistemi.UtmWgs84,
            ProjeksiyonSistemi.Cografi,
            ProjeksiyonSistemi.CografiEd50
        };

        public ProjeksiyonSistemi SecilenSistem { get; private set; }
        public DilimBilgisi? SecilenDilim { get; private set; }

        public KoordinatSistemiForm(ProjeksiyonSistemi mevcutSistem, DilimBilgisi? mevcutDilim)
        {
            InitializeComponent();
            DomBulmaAlaniniOlustur();

            foreach (var sistem in SistemSirasi)
                sistemCombo.Items.Add(ProjeksiyonBilgisi.GoruntuAdiGetir(sistem));

            var mevcutIndex = Array.IndexOf(SistemSirasi, mevcutSistem);
            sistemCombo.SelectedIndex = mevcutIndex < 0 ? 0 : mevcutIndex;

            if (mevcutDilim != null)
            {
                for (var i = 0; i < dilimCombo.Items.Count; i++)
                {
                    if (((DilimBilgisi)dilimCombo.Items[i]).Etiket == mevcutDilim.Value.Etiket)
                    {
                        dilimCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private TextBox _boylamKutu;
        private Label _domSonucLabel;

        /// <summary>
        /// Pencerenin altına "DOM bul" alanını ekler: girilen boylamın düştüğü 3° ve 6° dilimlerin orta meridyenini
        /// (DOM) gösterir ve istenirse o dilimi yukarıdaki listede seçer.
        /// </summary>
        private void DomBulmaAlaniniOlustur()
        {
            const int ekYukseklik = 114;
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + ekYukseklik);
            tamamButton.Top += ekYukseklik;
            iptalButton.Top += ekYukseklik;

            var grup = new GroupBox { Text = "DOM bul (boylama göre dilim orta meridyeni)", Location = new Point(12, 72), Size = new Size(408, 110) };
            grup.Controls.Add(new Label { Text = "Boylam (°):", Location = new Point(10, 24), AutoSize = true });
            _boylamKutu = new TextBox { Location = new Point(78, 21), Size = new Size(70, 20) };
            var ucDereceButton = new Button { Text = "3° dilimi seç", Location = new Point(160, 19), Size = new Size(110, 24) };
            var altiDereceButton = new Button { Text = "6° dilimi seç", Location = new Point(276, 19), Size = new Size(110, 24) };
            _domSonucLabel = new Label { Text = "Boylamı ondalık derece olarak yazın (ör. 32.85).", Location = new Point(10, 53), Size = new Size(390, 16), ForeColor = SystemColors.GrayText };
            var haritaButton = new Button { Text = "Türkiye haritasından seç...", Location = new Point(10, 76), Size = new Size(170, 24) };
            haritaButton.Click += (s, e) => HaritadanSec();
            grup.Controls.AddRange(new Control[] { _boylamKutu, ucDereceButton, altiDereceButton, _domSonucLabel, haritaButton });
            Controls.Add(grup);

            _boylamKutu.TextChanged += (s, e) => DomSonucunuGoster();
            ucDereceButton.Click += (s, e) => DomDiliminiSec(false);
            altiDereceButton.Click += (s, e) => DomDiliminiSec(true);
        }

        // Dilim haritasını açar; tıklanan yerin boylamını kutuya yazar ve o dilimi seçer.
        private void HaritadanSec()
        {
            // Harita, seçili dilim 6° ise 6° dilimlerle açılır.
            var altiDerece = dilimCombo.SelectedItem is DilimBilgisi secili && secili.OlcekFaktoru < 1;
            using (var dlg = new DilimHaritasiForm(altiDerece, BoylamOku(out var mevcut) ? mevcut : (double?)null))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || double.IsNaN(dlg.SecilenBoylam))
                    return;

                _boylamKutu.Text = dlg.SecilenBoylam.ToString("0.00", CultureInfo.InvariantCulture);
                if (dilimCombo.Items.Count > 0)
                    DomDiliminiSec(dlg.AltiDerece);
            }
        }

        private bool BoylamOku(out double boylam)
        {
            return double.TryParse(_boylamKutu.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out boylam)
                   && boylam >= -180 && boylam <= 180;
        }

        /// <summary>3° dilimde DOM en yakın 3'ün katıdır; dilim numarası DOM/3'tür.</summary>
        public static int UcDereceDom(double boylam)
        {
            return (int)Math.Round(boylam / 3.0, MidpointRounding.AwayFromZero) * 3;
        }

        /// <summary>6° (UTM) dilimde DOM 6'nın katı + 3'tür; UTM dilim numarası boylam/6 + 31'dir.</summary>
        public static int AltiDereceDom(double boylam)
        {
            return (int)Math.Floor(boylam / 6.0) * 6 + 3;
        }

        private void DomSonucunuGoster()
        {
            if (!BoylamOku(out var boylam))
            {
                _domSonucLabel.ForeColor = SystemColors.GrayText;
                _domSonucLabel.Text = "Boylamı ondalık derece olarak yazın (ör. 32.85).";
                return;
            }

            int dom3 = UcDereceDom(boylam), dom6 = AltiDereceDom(boylam);
            _domSonucLabel.ForeColor = SystemColors.ControlText;
            _domSonucLabel.Text = "3°: DOM " + dom3 + " (dilim " + dom3 / 3 + ")      6°: DOM " + dom6 + " (UTM " + ((int)Math.Floor(boylam / 6.0) + 31) + "N)";
        }

        // Seçili sistemin dilim listesinde, girilen boylamın düştüğü 3° ya da 6° dilimi seçer.
        private void DomDiliminiSec(bool altiDerece)
        {
            if (!BoylamOku(out var boylam))
            {
                MessageBox.Show("Önce boylamı ondalık derece olarak yazın (ör. 32.85).", "DOM Bul", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dom = altiDerece ? AltiDereceDom(boylam) : UcDereceDom(boylam);
            for (var i = 0; i < dilimCombo.Items.Count; i++)
            {
                var dilim = (DilimBilgisi)dilimCombo.Items[i];
                // 6° dilimlerin ölçek katsayısı 0.9996, 3° dilimlerinki 1'dir.
                if (dilim.MerkeziMeridyenDerece == dom && (dilim.OlcekFaktoru < 1) == altiDerece)
                {
                    dilimCombo.SelectedIndex = i;
                    return;
                }
            }

            MessageBox.Show("Seçili sistemde DOM " + dom + " olan " + (altiDerece ? "6°" : "3°") + " dilim yok. Önce yukarıdan uygun sistemi (ED-50 ya da ITRF96) seçin; " +
                            "boylam Türkiye dışındaysa dilim listede bulunmaz.", "DOM Bul", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void sistemCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sistemCombo.SelectedIndex < 0)
                return;

            var sistem = SistemSirasi[sistemCombo.SelectedIndex];
            dilimCombo.Items.Clear();
            var dilimler = ProjeksiyonBilgisi.DilimleriGetir(sistem);
            foreach (var dilim in dilimler)
                dilimCombo.Items.Add(dilim);

            dilimCombo.Enabled = dilimler.Count > 0;
            dilimLabel.Enabled = dilimler.Count > 0;
            if (dilimler.Count > 0)
                dilimCombo.SelectedIndex = 0;
        }

        private void tamamButton_Click(object sender, EventArgs e)
        {
            var sistem = SistemSirasi[sistemCombo.SelectedIndex];
            if (ProjeksiyonBilgisi.DilimGereklidir(sistem) && dilimCombo.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir dilim seçin.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SecilenSistem = sistem;
            SecilenDilim = dilimCombo.SelectedItem is DilimBilgisi db ? db : (DilimBilgisi?)null;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
