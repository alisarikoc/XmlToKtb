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
    /// Nokta editöründe düzenlenecek noktaların seçildiği pencere: nokta adı, kot ve tabaka süzgeçleriyle ya da
    /// çizim ekranından (grafikten) seçim yapılır.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class NoktaSecimForm : Form
    {
        private readonly List<NoktaNesnesi> _tumNoktalar;
        private readonly TextBox _adSuzgeci = new TextBox { Location = new Point(120, 14), Size = new Size(228, 20) };
        private readonly TextBox _enDusukKot = new TextBox { Location = new Point(120, 62), Size = new Size(96, 20) };
        private readonly TextBox _enYuksekKot = new TextBox { Location = new Point(252, 62), Size = new Size(96, 20) };
        private readonly CheckedListBox _tabakalar = new CheckedListBox { Location = new Point(120, 94), Size = new Size(228, 124), CheckOnClick = true };

        /// <summary>"Listele" ile kapatıldığında süzgeçlere uyan noktalar.</summary>
        public List<NoktaNesnesi> Secilenler { get; private set; } = new List<NoktaNesnesi>();

        /// <summary>Pencere "Grafikten Seç" ile kapatıldıysa true: noktalar çizim ekranından seçilecektir.</summary>
        public bool GrafiktenSecilecek { get; private set; }

        public NoktaSecimForm(IEnumerable<CizimNesnesi> nesneler)
        {
            _tumNoktalar = nesneler.OfType<NoktaNesnesi>().ToList();

            Text = "Nokta Editörü - Nokta Seçimi";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(362, 318);

            Controls.Add(new Label { Text = "Nokta adı süzgeci:", Location = new Point(12, 17), AutoSize = true });
            Controls.Add(_adSuzgeci);
            Controls.Add(new Label
            {
                Text = "* birden çok, ? tek karakter yerine geçer (ör. P*, 1??). Boş: hepsi.",
                Location = new Point(12, 38),
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            });

            Controls.Add(new Label { Text = "Nokta kotu süzgeci:", Location = new Point(12, 65), AutoSize = true });
            Controls.Add(_enDusukKot);
            Controls.Add(new Label { Text = "ile", Location = new Point(224, 65), AutoSize = true });
            Controls.Add(_enYuksekKot);

            Controls.Add(new Label { Text = "Tabaka süzgeci:", Location = new Point(12, 97), AutoSize = true });
            Controls.Add(_tabakalar);

            // Yalnızca üzerinde nokta olan tabakalar listelenir; başlangıçta hepsi tiklidir.
            foreach (var grup in _tumNoktalar.GroupBy(n => n.TabakaAdi).OrderBy(g => g.Key))
                _tabakalar.Items.Add(new TabakaSatiri { Ad = grup.Key, NoktaSayisi = grup.Count() }, true);

            var grafiktenSecButton = new Button { Text = "Grafikten Seç", Location = new Point(12, 230), Size = new Size(336, 28) };
            var listeleButton = new Button { Text = "Süzgece Göre Listele", Location = new Point(12, 276), Size = new Size(200, 28) };
            var iptalButton = new Button { Text = "İptal", Location = new Point(258, 276), Size = new Size(90, 28), DialogResult = DialogResult.Cancel };
            grafiktenSecButton.Click += (s, e) =>
            {
                GrafiktenSecilecek = true;
                DialogResult = DialogResult.OK;
            };
            listeleButton.Click += ListeleButton_Click;

            Controls.Add(grafiktenSecButton);
            Controls.Add(listeleButton);
            Controls.Add(iptalButton);
            AcceptButton = listeleButton;
            CancelButton = iptalButton;
        }

        private class TabakaSatiri
        {
            public string Ad;
            public int NoktaSayisi;

            public override string ToString()
            {
                return (Ad == "" ? "(tabakasız)" : Ad) + "  (" + NoktaSayisi + " nokta)";
            }
        }

        private void ListeleButton_Click(object sender, EventArgs e)
        {
            double enDusuk = double.MinValue, enYuksek = double.MaxValue;
            if ((_enDusukKot.Text.Trim() != "" && !NcnDosyasi.SayiOku(_enDusukKot.Text, out enDusuk))
                || (_enYuksekKot.Text.Trim() != "" && !NcnDosyasi.SayiOku(_enYuksekKot.Text, out enYuksek)))
            {
                MessageBox.Show("Kot süzgecine geçerli sayılar girin ya da kutuları boş bırakın.", "Nokta Seçimi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Süzgeçteki * ve ? dışındaki karakterler olduğu gibi aranır; büyük/küçük harf ayrımı yapılmaz.
            Regex adDeseni = null;
            var desen = _adSuzgeci.Text.Trim();
            if (desen != "")
                adDeseni = new Regex("^" + Regex.Escape(desen).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);

            var tikliTabakalar = new HashSet<string>(_tabakalar.CheckedItems.Cast<TabakaSatiri>().Select(t => t.Ad));

            Secilenler = _tumNoktalar.Where(n =>
                (adDeseni == null || adDeseni.IsMatch(n.NoktaAdi ?? ""))
                && n.Z >= enDusuk && n.Z <= enYuksek
                && tikliTabakalar.Contains(n.TabakaAdi)).ToList();

            GrafiktenSecilecek = false;
            DialogResult = DialogResult.OK;
        }
    }
}
