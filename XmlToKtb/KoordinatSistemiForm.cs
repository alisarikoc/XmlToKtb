using System;
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
            ProjeksiyonSistemi.Cografi
        };

        public ProjeksiyonSistemi SecilenSistem { get; private set; }
        public DilimBilgisi? SecilenDilim { get; private set; }

        public KoordinatSistemiForm(ProjeksiyonSistemi mevcutSistem, DilimBilgisi? mevcutDilim)
        {
            InitializeComponent();

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
