using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace XmlToKtb.Las
{
    /// <summary>
    /// Nokta bulutunun koordinat sistemini tanımlama ve başka bir sisteme dönüştürme penceresi. Dönüştürme
    /// istenmezse yalnızca mevcut sistem tanımlanır (koordinatlar değişmez).
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class KoordinatSistemiForm : Form
    {
        private const string Tanimsiz = "(Tanımsız / yerel koordinat)";

        private readonly DatumParametreleri _datum;
        private readonly ComboBox _kaynakKutu, _hedefKutu;
        private readonly CheckBox _donusturKutu;
        private readonly TextBox[] _datumKutulari = new TextBox[7];

        /// <summary>Bulutun mevcut sistemi olarak seçilen (tanımsızsa null).</summary>
        public KoordinatSistemi Kaynak { get; private set; }
        /// <summary>Dönüştürülecek sistem; dönüştürme istenmediyse null.</summary>
        public KoordinatSistemi Hedef { get; private set; }

        public KoordinatSistemiForm(KoordinatSistemi mevcut, string dosyadanOkunan, DatumParametreleri datum)
        {
            _datum = datum;
            Text = "Koordinat Sistemi";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(470, 372);

            Controls.Add(new Label { Text = "Dosyadan okunan: " + dosyadanOkunan, Location = new Point(12, 12), Size = new Size(446, 18), ForeColor = SystemColors.GrayText });

            Controls.Add(new Label { Text = "Bulutun mevcut sistemi:", Location = new Point(12, 41), AutoSize = true });
            _kaynakKutu = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(150, 38), Size = new Size(308, 21), DropDownHeight = 320 };
            _kaynakKutu.Items.Add(Tanimsiz);
            foreach (var sistem in KoordinatSistemi.Liste)
                _kaynakKutu.Items.Add(sistem);
            _kaynakKutu.SelectedItem = (object)mevcut ?? Tanimsiz;
            Controls.Add(_kaynakKutu);

            _donusturKutu = new CheckBox { Text = "Şu sisteme dönüştür:", Location = new Point(12, 72), AutoSize = true };
            _hedefKutu = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(150, 70), Size = new Size(308, 21), DropDownHeight = 320, Enabled = false };
            foreach (var sistem in KoordinatSistemi.Liste)
                _hedefKutu.Items.Add(sistem);
            _hedefKutu.SelectedIndex = 0;
            Controls.Add(_donusturKutu);
            Controls.Add(_hedefKutu);

            var grup = new GroupBox { Text = "ED50 ↔ ITRF96 datum dönüşümü (7 parametre, ED50'den ITRF96'ya)", Location = new Point(12, 104), Size = new Size(446, 138), Enabled = false };
            var etiketler = new[] { "dX (m):", "dY (m):", "dZ (m):", "rX (\"):", "rY (\"):", "rZ (\"):", "Ölçek (ppm):" };
            var degerler = new[] { datum.Dx, datum.Dy, datum.Dz, datum.Rx, datum.Ry, datum.Rz, datum.Ppm };
            for (var i = 0; i < 7; i++)
            {
                int sutun = i < 6 ? i % 3 : 0, satir = i < 6 ? i / 3 : 2;
                grup.Controls.Add(new Label { Text = etiketler[i], Location = new Point(12 + sutun * 145, 25 + satir * 28), Size = new Size(i == 6 ? 76 : 46, 16) });
                _datumKutulari[i] = new TextBox
                {
                    Text = degerler[i].ToString("0.#####", CultureInfo.InvariantCulture),
                    Location = new Point((i == 6 ? 90 : 58) + sutun * 145, 22 + satir * 28),
                    Size = new Size(70, 20)
                };
                grup.Controls.Add(_datumKutulari[i]);
            }
            grup.Controls.Add(new Label
            {
                Text = "Varsayılan: EPSG:1784 (Türkiye geneli, yaklaşık 2 m doğruluk).",
                Location = new Point(12, 112),
                Size = new Size(425, 16),
                ForeColor = SystemColors.GrayText
            });
            Controls.Add(grup);

            Controls.Add(new Label
            {
                Text = "Yalnızca yatay koordinatlar dönüştürülür, kotlar değişmez. Dönüşüm bellekteki noktalara uygulanır; LAS " +
                       "dosyası değiştirilmez. ED50 ile ITRF96 arasındaki ülke geneli parametreler metre düzeyinde doğrudur; " +
                       "hassas iş için bölgeye ait parametreleri girin ve sonucu koordinatı bilinen bir noktayla denetleyin.",
                Location = new Point(12, 250),
                Size = new Size(446, 70)
            });

            var tamam = new Button { Text = "Tamam", Location = new Point(302, 334), Size = new Size(75, 26) };
            var iptal = new Button { Text = "İptal", Location = new Point(383, 334), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
            Controls.Add(tamam);
            Controls.Add(iptal);
            AcceptButton = tamam;
            CancelButton = iptal;

            // Datum parametreleri yalnızca ED50 ile ITRF96/WGS84 arasında dönüşüm yapılacaksa kullanılır.
            void DurumuGuncelle()
            {
                _hedefKutu.Enabled = _donusturKutu.Checked;
                var kaynak = _kaynakKutu.SelectedItem as KoordinatSistemi;
                var hedef = _hedefKutu.SelectedItem as KoordinatSistemi;
                grup.Enabled = _donusturKutu.Checked && kaynak != null && hedef != null &&
                               (kaynak.Datum == KoordinatDatumu.Ed50) != (hedef.Datum == KoordinatDatumu.Ed50);
            }
            _donusturKutu.CheckedChanged += (s, e) => DurumuGuncelle();
            _kaynakKutu.SelectedIndexChanged += (s, e) => DurumuGuncelle();
            _hedefKutu.SelectedIndexChanged += (s, e) => DurumuGuncelle();
            tamam.Click += (s, e) =>
            {
                if (Onayla())
                    DialogResult = DialogResult.OK;
            };
        }

        private bool Onayla()
        {
            var kaynak = _kaynakKutu.SelectedItem as KoordinatSistemi;
            KoordinatSistemi hedef = null;
            string hata = null;
            var degerler = new double[7];

            if (_donusturKutu.Checked)
            {
                hedef = _hedefKutu.SelectedItem as KoordinatSistemi;
                if (kaynak == null)
                    hata = "Dönüşüm için önce bulutun mevcut koordinat sistemini seçin. Yerel (tanımsız) koordinatlar EPSG sistemleri arasında dönüştürülemez.";
                else if (hedef == null || hedef.Epsg == kaynak.Epsg)
                    hata = "Mevcut sistemden farklı bir hedef sistem seçin.";
            }

            for (var i = 0; i < 7 && hata == null; i++)
            {
                if (!NcnDosyasi.SayiOku(_datumKutulari[i].Text, out degerler[i]))
                    hata = "Datum dönüşüm parametreleri sayı olmalıdır.";
            }

            if (hata != null)
            {
                MessageBox.Show(hata, "Koordinat Sistemi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _datum.Dx = degerler[0]; _datum.Dy = degerler[1]; _datum.Dz = degerler[2];
            _datum.Rx = degerler[3]; _datum.Ry = degerler[4]; _datum.Rz = degerler[5];
            _datum.Ppm = degerler[6];
            Kaynak = kaynak;
            Hedef = hedef;
            return true;
        }
    }
}
