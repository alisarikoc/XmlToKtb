using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Bul / Değiştir: çizimdeki (ya da seçili) yazı objelerinde metin arar, bulunanları listeler ve seçer; istenirse
    /// aranan metni yenisiyle değiştirir.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class BulDegistirForm : Form
    {
        private readonly CizimPaneli _panel;
        private readonly TextBox _arananKutu = new TextBox { Width = 300 };
        private readonly TextBox _yeniKutu = new TextBox { Width = 300 };
        private readonly CheckBox _harfKutu = new CheckBox { Text = "Büyük/küçük harf duyarlı", AutoSize = true };
        private readonly CheckBox _tamKutu = new CheckBox { Text = "Yalnızca metnin tamamı eşleşsin", AutoSize = true };
        private readonly CheckBox _joker = new CheckBox { Text = "Joker karakter kullan (* ve ?)", AutoSize = true };
        private readonly RadioButton _tumCizim = new RadioButton { Text = "Bütün çizimde", AutoSize = true, Checked = true };
        private readonly RadioButton _seciliObjeler = new RadioButton { Text = "Seçili objelerde", AutoSize = true };
        private readonly ListBox _sonucListesi = new ListBox { Width = 440, Height = 150, HorizontalScrollbar = true };
        private readonly Label _durumLabel = new Label { AutoSize = true, ForeColor = Color.DarkBlue };
        private List<YaziNesnesi> _bulunanlar = new List<YaziNesnesi>();

        public BulDegistirForm(CizimPaneli panel)
        {
            _panel = panel;
            Text = "Bul / Değiştir";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(464, 420);

            Controls.Add(new Label { Text = "Aranan metin:", Location = new Point(12, 15), AutoSize = true });
            _arananKutu.Location = new Point(150, 12);
            Controls.Add(new Label { Text = "Yeni metin:", Location = new Point(12, 43), AutoSize = true });
            _yeniKutu.Location = new Point(150, 40);
            _harfKutu.Location = new Point(14, 72);
            _tamKutu.Location = new Point(14, 94);
            _joker.Location = new Point(14, 116);
            _tumCizim.Location = new Point(260, 72);
            _seciliObjeler.Location = new Point(260, 94);
            _seciliObjeler.Enabled = _panel.Nesneler.Any(n => n.Secili);
            Controls.AddRange(new Control[] { _arananKutu, _yeniKutu, _harfKutu, _tamKutu, _joker, _tumCizim, _seciliObjeler });

            var bulButton = new Button { Text = "Bul ve Seç", Location = new Point(12, 146), Size = new Size(100, 28) };
            var degistirButton = new Button { Text = "Tümünü Değiştir", Location = new Point(118, 146), Size = new Size(120, 28) };
            var kapatButton = new Button { Text = "Kapat", Location = new Point(377, 380), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };
            bulButton.Click += (s, e) => Bul(true);
            degistirButton.Click += (s, e) => Degistir();
            Controls.AddRange(new Control[] { bulButton, degistirButton, kapatButton });

            _sonucListesi.Location = new Point(12, 184);
            _durumLabel.Location = new Point(12, 340);
            Controls.Add(_sonucListesi);
            Controls.Add(_durumLabel);
            Controls.Add(new Label { Text = "Listeden bir yazı seçince çizimde yalnız o seçilir.", Location = new Point(12, 360), AutoSize = true, ForeColor = SystemColors.GrayText });
            _sonucListesi.SelectedIndexChanged += (s, e) =>
            {
                if (_sonucListesi.SelectedIndex < 0 || _sonucListesi.SelectedIndex >= _bulunanlar.Count)
                    return;
                foreach (var nesne in _panel.Nesneler)
                    nesne.Secili = ReferenceEquals(nesne, _bulunanlar[_sonucListesi.SelectedIndex]);
                _panel.SecimDegistiBildir();
                _panel.Invalidate();
            };

            AcceptButton = bulButton;
            CancelButton = kapatButton;
        }

        // Aranan metni düzenli ifadeye çevirir (joker: * herhangi bir dizi, ? tek karakter).
        private Regex Ifade()
        {
            var desen = Regex.Escape(_arananKutu.Text);
            if (_joker.Checked)
                desen = desen.Replace("\\*", ".*").Replace("\\?", ".");
            if (_tamKutu.Checked)
                desen = "^" + desen + "$";
            return new Regex(desen, _harfKutu.Checked ? RegexOptions.None : RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private void Bul(bool sec)
        {
            if (_arananKutu.Text.Length == 0)
            {
                _durumLabel.Text = "Aranan metni yazın.";
                return;
            }

            var ifade = Ifade();
            var kapsam = _panel.Nesneler.OfType<YaziNesnesi>().Where(y => !_seciliObjeler.Checked || y.Secili);
            _bulunanlar = kapsam.Where(y => y.Metin != null && ifade.IsMatch(y.Metin)).ToList();
            _sonucListesi.Items.Clear();
            foreach (var yazi in _bulunanlar)
                _sonucListesi.Items.Add(yazi.Metin + "     (" + yazi.KonumX.ToString("0.00") + " ; " + yazi.KonumY.ToString("0.00") + ")");
            _durumLabel.Text = _bulunanlar.Count + " yazı bulundu.";

            if (sec)
            {
                var bulunan = new HashSet<CizimNesnesi>(_bulunanlar);
                foreach (var nesne in _panel.Nesneler)
                    nesne.Secili = bulunan.Contains(nesne);
                _panel.SecimDegistiBildir();
                _panel.Invalidate();
            }
        }

        private void Degistir()
        {
            Bul(false);
            if (_bulunanlar.Count == 0)
                return;
            if (MessageBox.Show(_bulunanlar.Count + " yazıda \"" + _arananKutu.Text + "\" → \"" + _yeniKutu.Text + "\" değiştirilsin mi?", "Bul / Değiştir",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var ifade = Ifade();
            foreach (var yazi in _bulunanlar)
                yazi.Metin = ifade.Replace(yazi.Metin, _yeniKutu.Text.Replace("$", "$$"));
            _panel.DegisiklikKaydet();
            _panel.Invalidate();
            _durumLabel.Text = _bulunanlar.Count + " yazı değiştirildi.";
            Bul(false);
        }
    }
}
