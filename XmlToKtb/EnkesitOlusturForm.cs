using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Enkesit Oluştur penceresi. İlk sekmede kesitlerin yeri (güzergah, kilometre aralığı, aralık, geometri
    /// noktaları, ek kilometreler, genişlikler), ikinci sekmede kotların nereden alınacağı (üçgen model, tabakalardaki
    /// kotlu objeler ya da noktalar) ve çıktı seçenekleri belirlenir.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class EnkesitOlusturForm : Form
    {
        private readonly List<GuzergahNesnesi> _guzergahlar;
        private readonly List<YuzeyNesnesi> _yuzeyler;

        private readonly ComboBox _guzergahKutu = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        private readonly TextBox _basKmKutu = Kutu(), _bitKmKutu = Kutu(), _aralikKutu = Kutu("20"), _solKutu = Kutu("30"), _sagKutu = Kutu("30");
        private readonly TextBox _ekKmKutu = new TextBox { Width = 260 };
        private readonly CheckBox _yuvarlaKutu = new CheckBox { Text = "Aralıklı kesitler aralığın katlarına otursun (0+000, 0+020, ...)", AutoSize = true, Checked = true };
        private readonly CheckBox _geometriKutu = new CheckBox { Text = "Geometri noktalarında da kesit al (doğru / kurp / spiral birleşimleri)", AutoSize = true, Checked = true };
        private readonly CheckBox _basSonKutu = new CheckBox { Text = "Başlangıç ve bitiş kilometresinde kesit al", AutoSize = true, Checked = true };
        private readonly Label _sayiLabel = new Label { AutoSize = true, ForeColor = Color.DarkBlue };

        private readonly RadioButton _yuzeyRadio = new RadioButton { Text = "Üçgen modelden (yüzey objesi):", AutoSize = true };
        private readonly ComboBox _yuzeyKutu = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly RadioButton _objeRadio = new RadioButton { Text = "Seçilen tabakalardaki kotlu çizgi, yay ve dairelerden (eş yükseklik eğrileri, 3B çizgiler)", AutoSize = true };
        private readonly RadioButton _noktaRadio = new RadioButton { Text = "Seçilen tabakalardaki noktalardan (kesit çizgisine en yakın ilerideki ve gerideki noktalar)", AutoSize = true };
        private readonly TextBox _aramaKutu = Kutu("5"), _noktaAralikKutu = Kutu("0.1"), _birlestirKutu = Kutu("0.01");
        private readonly CheckedListBox _tabakaListesi = new CheckedListBox { CheckOnClick = true, Width = 300, Height = 120, IntegralHeight = false };
        private readonly CheckBox _eksenKutu = new CheckBox { Text = "Eksende (mesafe 0) nokta yoksa iki yanındakilerden enterpolasyonla ekle", AutoSize = true, Checked = true };
        private readonly CheckBox _cizimeEkleKutu = new CheckBox { Text = "Kesit çizgilerini ve kilometrelerini çizime ekle (ENKESİT tabakası)", AutoSize = true, Checked = true };
        private readonly CheckBox _editordeAcKutu = new CheckBox { Text = "Oluşan KSE dosyasını Kesit Editörü'nde aç", AutoSize = true, Checked = true };

        /// <summary>Tamam'a basıldıysa kullanıcının seçtiği ayarlar.</summary>
        public EnkesitAyarlari Ayarlar { get; private set; }
        public bool CizimeEkle => _cizimeEkleKutu.Checked;
        public bool EditordeAc => _editordeAcKutu.Checked;

        private static TextBox Kutu(string metin = "")
        {
            return new TextBox { Width = 90, Text = metin, TextAlign = HorizontalAlignment.Right };
        }

        public EnkesitOlusturForm(IList<CizimNesnesi> nesneler, IList<Tabaka> tabakalar, GuzergahNesnesi seciliGuzergah)
        {
            _guzergahlar = nesneler.OfType<GuzergahNesnesi>().Where(g => g.Parcalar.Count > 0).ToList();
            _yuzeyler = nesneler.OfType<YuzeyNesnesi>().ToList();

            Text = "Enkesit Oluştur";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(600, 450);

            var sekmeler = new TabControl { Location = new Point(8, 8), Size = new Size(584, 396) };
            var yerSekmesi = new TabPage("Kesit Yerleri");
            var hesapSekmesi = new TabPage("Hesaplama");
            sekmeler.TabPages.Add(yerSekmesi);
            sekmeler.TabPages.Add(hesapSekmesi);
            Controls.Add(sekmeler);

            // --- 1. sekme: kesit yerleri ---
            var y = 14;
            void Satir(Control kap, string etiket, Control kontrol, string sonEk = null)
            {
                kap.Controls.Add(new Label { Text = etiket, Location = new Point(12, y + 3), AutoSize = true });
                kontrol.Location = new Point(200, y);
                kap.Controls.Add(kontrol);
                if (sonEk != null)
                    kap.Controls.Add(new Label { Text = sonEk, Location = new Point(200 + kontrol.Width + 6, y + 3), AutoSize = true, ForeColor = SystemColors.GrayText });
                y += 28;
            }
            foreach (var g in _guzergahlar)
                _guzergahKutu.Items.Add(string.IsNullOrWhiteSpace(g.Ad) ? "(adsız güzergah)" : g.Ad);
            Satir(yerSekmesi, "Güzergah:", _guzergahKutu);
            Satir(yerSekmesi, "Başlangıç kilometresi:", _basKmKutu, "m");
            Satir(yerSekmesi, "Bitiş kilometresi:", _bitKmKutu, "m");
            Satir(yerSekmesi, "Kesit aralığı:", _aralikKutu, "m (0: aralıklı kesit yok)");
            Satir(yerSekmesi, "Sol genişlik:", _solKutu, "m (eksenin solu)");
            Satir(yerSekmesi, "Sağ genişlik:", _sagKutu, "m (eksenin sağı)");
            Satir(yerSekmesi, "Ek kilometreler:", _ekKmKutu);
            yerSekmesi.Controls.Add(new Label { Text = "(ör. 125.5; 0+340; 1250 — ';' ya da boşlukla ayırın)", Location = new Point(200, y - 4), AutoSize = true, ForeColor = SystemColors.GrayText });
            y += 18;
            foreach (var kutu in new Control[] { _yuvarlaKutu, _geometriKutu, _basSonKutu })
            {
                kutu.Location = new Point(14, y);
                yerSekmesi.Controls.Add(kutu);
                y += 24;
            }
            _sayiLabel.Location = new Point(14, y + 8);
            yerSekmesi.Controls.Add(_sayiLabel);

            // --- 2. sekme: hesaplama ---
            y = 12;
            _yuzeyRadio.Location = new Point(12, y);
            _yuzeyKutu.Location = new Point(230, y - 2);
            foreach (var yuzey in _yuzeyler)
                _yuzeyKutu.Items.Add(string.IsNullOrWhiteSpace(yuzey.Ad) ? "(adsız yüzey)" : yuzey.Ad + "  (" + yuzey.UcgenSayisi.ToString("N0") + " üçgen)");
            if (_yuzeyKutu.Items.Count > 0)
                _yuzeyKutu.SelectedIndex = 0;
            hesapSekmesi.Controls.Add(_yuzeyRadio);
            hesapSekmesi.Controls.Add(_yuzeyKutu);
            y += 28;
            _objeRadio.Location = new Point(12, y);
            hesapSekmesi.Controls.Add(_objeRadio);
            y += 24;
            _noktaRadio.Location = new Point(12, y);
            hesapSekmesi.Controls.Add(_noktaRadio);
            y += 26;
            hesapSekmesi.Controls.Add(new Label { Text = "Arama mesafesi (ileri/geri):", Location = new Point(34, y + 3), AutoSize = true });
            _aramaKutu.Location = new Point(200, y);
            hesapSekmesi.Controls.Add(_aramaKutu);
            hesapSekmesi.Controls.Add(new Label { Text = "m    Nokta aralığı:", Location = new Point(296, y + 3), AutoSize = true });
            _noktaAralikKutu.Location = new Point(400, y);
            _noktaAralikKutu.Width = 60;
            hesapSekmesi.Controls.Add(_noktaAralikKutu);
            hesapSekmesi.Controls.Add(new Label { Text = "m", Location = new Point(464, y + 3), AutoSize = true });
            y += 32;

            hesapSekmesi.Controls.Add(new Label { Text = "Tabakalar (kotlu objeler ve noktalar için):", Location = new Point(12, y), AutoSize = true });
            _tabakaListesi.Location = new Point(14, y + 18);
            foreach (var tabaka in tabakalar)
            {
                // Kotlu obje ya da nokta içeren tabakalar başlangıçta seçili gelir.
                var icerik = nesneler.Any(n => n.Tabaka == tabaka && (n is NoktaNesnesi || n is CizgiNesnesi || n is YayNesnesi || n is DaireNesnesi || n is CokluDogruNesnesi || n is BlokNesnesi));
                _tabakaListesi.Items.Add(new TabakaSatiri(tabaka), icerik && tabaka.Gorunur);
            }
            hesapSekmesi.Controls.Add(_tabakaListesi);
            var hepsiButton = new Button { Text = "Hepsi", Location = new Point(324, y + 18), Size = new Size(70, 24) };
            var hicbiriButton = new Button { Text = "Hiçbiri", Location = new Point(324, y + 46), Size = new Size(70, 24) };
            hepsiButton.Click += (s, e) => { for (var i = 0; i < _tabakaListesi.Items.Count; i++) _tabakaListesi.SetItemChecked(i, true); };
            hicbiriButton.Click += (s, e) => { for (var i = 0; i < _tabakaListesi.Items.Count; i++) _tabakaListesi.SetItemChecked(i, false); };
            hesapSekmesi.Controls.Add(hepsiButton);
            hesapSekmesi.Controls.Add(hicbiriButton);
            y += 146;

            _eksenKutu.Location = new Point(12, y);
            hesapSekmesi.Controls.Add(_eksenKutu);
            y += 26;
            hesapSekmesi.Controls.Add(new Label { Text = "Kesit boyunca birleştirme hassasiyeti:", Location = new Point(12, y + 3), AutoSize = true });
            _birlestirKutu.Location = new Point(230, y);
            hesapSekmesi.Controls.Add(_birlestirKutu);
            hesapSekmesi.Controls.Add(new Label { Text = "m (bundan yakın noktalar birleşir; 0: kapalı)", Location = new Point(326, y + 3), AutoSize = true, ForeColor = SystemColors.GrayText });
            y += 30;
            _cizimeEkleKutu.Location = new Point(12, y);
            hesapSekmesi.Controls.Add(_cizimeEkleKutu);
            y += 22;
            _editordeAcKutu.Location = new Point(12, y);
            hesapSekmesi.Controls.Add(_editordeAcKutu);

            // --- düğmeler ---
            var tamam = new Button { Text = "Oluştur ve Kaydet...", Location = new Point(386, 414), Size = new Size(125, 28) };
            var iptal = new Button { Text = "İptal", Location = new Point(517, 414), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };
            tamam.Click += (s, e) =>
            {
                if (AyarlariAl())
                    DialogResult = DialogResult.OK;
            };
            Controls.Add(tamam);
            Controls.Add(iptal);
            AcceptButton = tamam;
            CancelButton = iptal;

            // Varsayılan kaynak: yüzey varsa üçgen model, yoksa noktalar, o da yoksa kotlu objeler.
            if (_yuzeyler.Count > 0)
                _yuzeyRadio.Checked = true;
            else if (nesneler.OfType<NoktaNesnesi>().Any())
                _noktaRadio.Checked = true;
            else
                _objeRadio.Checked = true;
            _yuzeyRadio.Enabled = _yuzeyler.Count > 0;

            _guzergahKutu.SelectedIndexChanged += (s, e) =>
            {
                var g = SeciliGuzergah();
                if (g == null)
                    return;
                _basKmKutu.Text = g.BaslangicKm.ToString("0.000", CultureInfo.InvariantCulture);
                _bitKmKutu.Text = g.BitisKm.ToString("0.000", CultureInfo.InvariantCulture);
                SayiyiGuncelle();
            };
            foreach (var kontrol in new Control[] { _basKmKutu, _bitKmKutu, _aralikKutu, _ekKmKutu })
                kontrol.TextChanged += (s, e) => SayiyiGuncelle();
            foreach (var kutu in new[] { _yuvarlaKutu, _geometriKutu, _basSonKutu })
                kutu.CheckedChanged += (s, e) => SayiyiGuncelle();
            foreach (var radio in new[] { _yuzeyRadio, _objeRadio, _noktaRadio })
                radio.CheckedChanged += (s, e) => KaynakDurumunuGuncelle();

            var secili = seciliGuzergah != null ? _guzergahlar.IndexOf(seciliGuzergah) : -1;
            if (_guzergahlar.Count > 0)
                _guzergahKutu.SelectedIndex = Math.Max(0, secili);
            KaynakDurumunuGuncelle();
        }

        private class TabakaSatiri
        {
            public readonly Tabaka Tabaka;
            public TabakaSatiri(Tabaka tabaka) { Tabaka = tabaka; }
            public override string ToString() => Tabaka.Ad + (Tabaka.Gorunur ? "" : "  (kapalı)");
        }

        private GuzergahNesnesi SeciliGuzergah()
        {
            return _guzergahKutu.SelectedIndex >= 0 ? _guzergahlar[_guzergahKutu.SelectedIndex] : null;
        }

        private void KaynakDurumunuGuncelle()
        {
            _yuzeyKutu.Enabled = _yuzeyRadio.Checked;
            _aramaKutu.Enabled = _noktaAralikKutu.Enabled = _noktaRadio.Checked;
            _tabakaListesi.Enabled = !_yuzeyRadio.Checked;
        }

        // "0+340", "340" ve "1+250.5" biçimlerindeki kilometreyi okur.
        private static bool KmOku(string metin, out double km)
        {
            metin = (metin ?? "").Trim();
            var arti = metin.IndexOf('+');
            if (arti > 0 && NcnDosyasi.SayiOku(metin.Substring(0, arti), out var binler) && NcnDosyasi.SayiOku(metin.Substring(arti + 1), out var metre))
            {
                km = binler * 1000 + metre;
                return true;
            }
            return NcnDosyasi.SayiOku(metin, out km);
        }

        // Ayarları kutulardan okur; hata varsa mesajını verir.
        private EnkesitAyarlari AyarlariOku(out string hata)
        {
            hata = null;
            var g = SeciliGuzergah();
            if (g == null)
            {
                hata = "Çizimde güzergah yok.";
                return null;
            }
            if (!KmOku(_basKmKutu.Text, out var bas) || !KmOku(_bitKmKutu.Text, out var bit) || bit < bas)
            {
                hata = "Başlangıç ve bitiş kilometresi sayı olmalı; bitiş başlangıçtan küçük olamaz.";
                return null;
            }
            if (!NcnDosyasi.SayiOku(_aralikKutu.Text, out var aralik) || aralik < 0)
            {
                hata = "Kesit aralığı sıfır ya da daha büyük bir sayı olmalıdır.";
                return null;
            }
            if (!NcnDosyasi.SayiOku(_solKutu.Text, out var sol) || !NcnDosyasi.SayiOku(_sagKutu.Text, out var sag) || sol < 0 || sag < 0 || sol + sag <= 0)
            {
                hata = "Sol ve sağ genişlik sıfır ya da daha büyük olmalı; toplamları sıfırdan büyük olmalıdır.";
                return null;
            }

            var ayar = new EnkesitAyarlari
            {
                Guzergah = g, BaslangicKm = bas, BitisKm = bit, Aralik = aralik, SolGenislik = sol, SagGenislik = sag,
                AraligaYuvarla = _yuvarlaKutu.Checked, GeometriNoktalari = _geometriKutu.Checked, BaslangicVeBitis = _basSonKutu.Checked
            };
            foreach (var parca in _ekKmKutu.Text.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!KmOku(parca, out var ekKm))
                {
                    hata = "Ek kilometre okunamadı: " + parca;
                    return null;
                }
                ayar.EkKilometreler.Add(ekKm);
            }
            return ayar;
        }

        private void SayiyiGuncelle()
        {
            var ayar = AyarlariOku(out _);
            _sayiLabel.Text = ayar == null ? "" : "Oluşturulacak kesit sayısı: " + EnkesitHesaplayici.KilometreleriBelirle(ayar).Count.ToString("N0");
        }

        private bool AyarlariAl()
        {
            var ayar = AyarlariOku(out var hata);
            if (ayar != null)
            {
                ayar.Kaynak = _yuzeyRadio.Checked ? EnkesitKaynagi.Yuzey : _objeRadio.Checked ? EnkesitKaynagi.TabakaObjeleri : EnkesitKaynagi.Noktalar;
                ayar.Yuzey = _yuzeyKutu.SelectedIndex >= 0 ? _yuzeyler[_yuzeyKutu.SelectedIndex] : null;
                ayar.Tabakalar = new HashSet<Tabaka>(_tabakaListesi.CheckedItems.Cast<TabakaSatiri>().Select(t => t.Tabaka));
                ayar.EksenNoktasiEkle = _eksenKutu.Checked;

                if (ayar.Kaynak == EnkesitKaynagi.Yuzey && ayar.Yuzey == null)
                    hata = "Üçgen model seçilmedi.";
                else if (ayar.Kaynak != EnkesitKaynagi.Yuzey && ayar.Tabakalar.Count == 0)
                    hata = "En az bir tabaka seçin.";
                else if (!NcnDosyasi.SayiOku(_aramaKutu.Text, out ayar.AramaMesafesi) || ayar.AramaMesafesi <= 0
                         || !NcnDosyasi.SayiOku(_noktaAralikKutu.Text, out ayar.NoktaAraligi) || ayar.NoktaAraligi <= 0)
                    hata = "Arama mesafesi ve nokta aralığı sıfırdan büyük olmalıdır.";
                else if (!NcnDosyasi.SayiOku(_birlestirKutu.Text, out ayar.BirlestirmeHassasiyeti) || ayar.BirlestirmeHassasiyeti < 0)
                    hata = "Birleştirme hassasiyeti sıfır ya da daha büyük olmalıdır.";
                else if (EnkesitHesaplayici.KilometreleriBelirle(ayar).Count == 0)
                    hata = "Bu ayarlarla kesit alınacak kilometre yok.";
            }

            if (hata != null)
            {
                MessageBox.Show(hata, "Enkesit Oluştur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            Ayarlar = ayar;
            return true;
        }
    }
}
