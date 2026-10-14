using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Hızlı Seçim: obje tipine, tabakaya, metne (yazı içeriği ya da nokta adı) ve kot aralığına göre seçim; sonuç yeni
    /// seçim olabilir, mevcut seçime eklenebilir, ondan çıkarılabilir ya da mevcut seçimin içinden ayıklanabilir.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class HizliSecimForm : Form
    {
        private readonly CheckedListBox _tipListesi = new CheckedListBox { CheckOnClick = true, Width = 210, Height = 170, IntegralHeight = false };
        private readonly CheckedListBox _tabakaListesi = new CheckedListBox { CheckOnClick = true, Width = 210, Height = 170, IntegralHeight = false };
        private readonly TextBox _metinKutu = new TextBox { Width = 200 };
        private readonly TextBox _kotAltKutu = new TextBox { Width = 80 }, _kotUstKutu = new TextBox { Width = 80 };
        private readonly ComboBox _birlesimKutu = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly List<Tabaka> _tabakalar;
        private HashSet<string> _tipler;
        private HashSet<Tabaka> _secilenTabakalar;
        private bool _tabakasizlar;
        private double? _kotAlt, _kotUst;

        /// <summary>0: yeni seçim, 1: seçime ekle, 2: seçimden çıkar, 3: seçimin içinden ayıkla.</summary>
        public int BirlesimSirasi => _birlesimKutu.SelectedIndex;

        public HizliSecimForm(IList<CizimNesnesi> nesneler, IList<Tabaka> tabakalar)
        {
            _tabakalar = tabakalar.ToList();
            Text = "Hızlı Seçim";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 390);

            Controls.Add(new Label { Text = "Obje tipi:", Location = new Point(12, 10), AutoSize = true });
            _tipListesi.Location = new Point(14, 28);
            foreach (var grup in nesneler.GroupBy(n => n.Tip).OrderBy(g => g.Key))
                _tipListesi.Items.Add(new Satir(grup.Key, grup.Key + "  (" + grup.Count() + ")"), true);
            Controls.Add(_tipListesi);

            Controls.Add(new Label { Text = "Tabaka:", Location = new Point(236, 10), AutoSize = true });
            _tabakaListesi.Location = new Point(238, 28);
            foreach (var tabaka in _tabakalar)
                _tabakaListesi.Items.Add(new Satir(tabaka, tabaka.Ad + "  (" + nesneler.Count(n => n.Tabaka == tabaka) + ")"), true);
            if (nesneler.Any(n => n.Tabaka == null))
                _tabakaListesi.Items.Add(new Satir(null, "(tabakasız)"), true);
            Controls.Add(_tabakaListesi);

            var y = 210;
            Controls.Add(new Label { Text = "Metin içerir (yazı / nokta adı):", Location = new Point(12, y + 3), AutoSize = true });
            _metinKutu.Location = new Point(238, y);
            Controls.Add(_metinKutu);
            y += 30;
            Controls.Add(new Label { Text = "Kot aralığı (boş: sınır yok):", Location = new Point(12, y + 3), AutoSize = true });
            _kotAltKutu.Location = new Point(238, y);
            Controls.Add(_kotAltKutu);
            Controls.Add(new Label { Text = "—", Location = new Point(323, y + 3), AutoSize = true });
            _kotUstKutu.Location = new Point(342, y);
            Controls.Add(_kotUstKutu);
            y += 30;
            Controls.Add(new Label { Text = "Sonuç:", Location = new Point(12, y + 3), AutoSize = true });
            _birlesimKutu.Items.AddRange(new object[] { "Yeni seçim", "Seçime ekle", "Seçimden çıkar", "Seçimin içinden ayıkla" });
            _birlesimKutu.SelectedIndex = 0;
            _birlesimKutu.Location = new Point(238, y);
            Controls.Add(_birlesimKutu);

            var tamam = new Button { Text = "Seç", Location = new Point(292, 350), Size = new Size(75, 28) };
            var iptal = new Button { Text = "İptal", Location = new Point(373, 350), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };
            tamam.Click += (s, e) =>
            {
                if (Hazirla())
                    DialogResult = DialogResult.OK;
            };
            Controls.Add(tamam);
            Controls.Add(iptal);
            AcceptButton = tamam;
            CancelButton = iptal;
        }

        private class Satir
        {
            public readonly object Deger;
            private readonly string _metin;
            public Satir(object deger, string metin) { Deger = deger; _metin = metin; }
            public override string ToString() => _metin;
        }

        private bool Hazirla()
        {
            _tipler = new HashSet<string>(_tipListesi.CheckedItems.Cast<Satir>().Select(s => (string)s.Deger));
            var tabakaSatirlari = _tabakaListesi.CheckedItems.Cast<Satir>().ToList();
            _secilenTabakalar = new HashSet<Tabaka>(tabakaSatirlari.Where(s => s.Deger != null).Select(s => (Tabaka)s.Deger));
            _tabakasizlar = tabakaSatirlari.Any(s => s.Deger == null);
            _kotAlt = _kotUst = null;
            if (_kotAltKutu.Text.Trim().Length > 0)
            {
                if (!NcnDosyasi.SayiOku(_kotAltKutu.Text, out var alt))
                    return Hata("Kot alt sınırı sayı olmalıdır.");
                _kotAlt = alt;
            }
            if (_kotUstKutu.Text.Trim().Length > 0)
            {
                if (!NcnDosyasi.SayiOku(_kotUstKutu.Text, out var ust))
                    return Hata("Kot üst sınırı sayı olmalıdır.");
                _kotUst = ust;
            }
            return true;
        }

        private bool Hata(string metin)
        {
            MessageBox.Show(metin, "Hızlı Seçim", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // Objenin kotu (kotu olmayan objede null).
        private static double? Kot(CizimNesnesi n)
        {
            switch (n)
            {
                case NoktaNesnesi nokta: return nokta.Z;
                case YaziNesnesi yazi: return yazi.KonumZ;
                case CizgiNesnesi cizgi: return (cizgi.BaslangicZ + cizgi.BitisZ) / 2;
                case DaireNesnesi daire: return daire.MerkezZ;
                case YayNesnesi yay: return yay.MerkezZ;
                case CokluDogruNesnesi cd: return cd.Kot;
                default: return null;
            }
        }

        /// <summary>Obje seçim koşullarına uyuyor mu?</summary>
        public bool Uyar(CizimNesnesi n)
        {
            if (!_tipler.Contains(n.Tip))
                return false;
            if (n.Tabaka == null ? !_tabakasizlar : !_secilenTabakalar.Contains(n.Tabaka))
                return false;

            var metin = _metinKutu.Text.Trim();
            if (metin.Length > 0)
            {
                var objeMetni = n is YaziNesnesi y ? y.Metin : n is NoktaNesnesi p ? p.NoktaAdi : null;
                if (objeMetni == null || objeMetni.IndexOf(metin, StringComparison.CurrentCultureIgnoreCase) < 0)
                    return false;
            }

            if (_kotAlt != null || _kotUst != null)
            {
                var kot = Kot(n);
                if (kot == null || (_kotAlt != null && kot < _kotAlt) || (_kotUst != null && kot > _kotUst))
                    return false;
            }
            return true;
        }
    }
}
