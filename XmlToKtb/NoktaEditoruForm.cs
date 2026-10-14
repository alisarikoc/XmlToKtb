using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Nokta editörü: seçilen noktaları "Sıra, Nokta Adı, Y, X, Z, Kod, Pafta, Tabaka" tablosunda listeler ve
    /// düzenletir. Tabloda Y sağa, X yukarı değerdir (NCN düzeni); çizimde bunlar sırasıyla X ve Y'dir.
    /// Tamam ile değişiklikler çizime işlenir; İptal ile çizim değişmez.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class NoktaEditoruForm : Form
    {
        private const int SiraKolon = 0, AdKolon = 1, YKolon = 2, XKolon = 3, ZKolon = 4, KodKolon = 5, PaftaKolon = 6, TabakaKolon = 7;

        private readonly CizimPaneli _panel;
        private readonly List<NoktaKaydi> _noktalar = new List<NoktaKaydi>();
        // Tablodan silinen, çizimde karşılığı olan noktalar; Tamam'da çizimden de silinir.
        private readonly List<NoktaNesnesi> _silinenler = new List<NoktaNesnesi>();
        private readonly DataGridViewComboBoxColumn _tabakaKolonu;
        private readonly Label _durumLabel = new Label { AutoSize = true, Location = new Point(10, 13) };

        // Tablo sanal modda çalışır (hücreler _noktalar listesinden okunur) ki on binlerce nokta hızlı listelensin.
        private readonly DataGridView _tablo = new DataGridView
        {
            Dock = DockStyle.Fill,
            VirtualMode = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersWidth = 28,
            SelectionMode = DataGridViewSelectionMode.RowHeaderSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        /// <summary>Tamam'da çizime yeni nokta eklendiyse true (çağıran taraf ekranı yeni noktalara sığdırabilir).</summary>
        public bool YeniNoktaEklendi { get; private set; }

        public NoktaEditoruForm(CizimPaneli panel, IEnumerable<NoktaNesnesi> duzenlenecekNoktalar)
        {
            _panel = panel;

            Text = "Nokta Editörü";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(860, 560);
            MinimumSize = new Size(640, 320);
            MinimizeBox = false;
            ShowInTaskbar = false;

            foreach (var nokta in duzenlenecekNoktalar)
            {
                _noktalar.Add(new NoktaKaydi
                {
                    Ad = nokta.NoktaAdi,
                    Y = nokta.X,
                    X = nokta.Y,
                    Z = nokta.Z,
                    Kod = nokta.Kod ?? "",
                    Pafta = nokta.Pafta ?? "",
                    Tabaka = nokta.TabakaAdi,
                    Kaynak = nokta
                });
            }

            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sıra", ReadOnly = true, FillWeight = 8 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nokta Adı", FillWeight = 14 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Y", FillWeight = 15 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "X", FillWeight = 15 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Z", FillWeight = 12 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kod", FillWeight = 12 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pafta", FillWeight = 10 });
            _tabakaKolonu = new DataGridViewComboBoxColumn { HeaderText = "Tabaka", FillWeight = 14, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            _tablo.Columns.Add(_tabakaKolonu);
            // Sıralamayı tablo değil bu pencere yapar (liste sıralanır); başlıktaki ok işareti için kolonlar
            // "programla sıralanır" olarak işaretlenir. Sıra kolonu satır numarası olduğu için sıralanmaz.
            foreach (DataGridViewColumn kolon in _tablo.Columns)
                kolon.SortMode = kolon.Index == SiraKolon ? DataGridViewColumnSortMode.NotSortable : DataGridViewColumnSortMode.Programmatic;
            _tablo.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || e.ColumnIndex == SiraKolon)
                    return;
                // Aynı başlığa ikinci tıklama sıralamayı tersine çevirir.
                KolonaGoreSirala(e.ColumnIndex, !(e.ColumnIndex == _siraliKolon && _artanSirali));
            };
            foreach (var sayiKolonu in new[] { SiraKolon, YKolon, XKolon, ZKolon })
                _tablo.Columns[sayiKolonu].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            _tablo.CellValueNeeded += Tablo_CellValueNeeded;
            _tablo.CellValuePushed += Tablo_CellValuePushed;
            _tablo.DataError += (s, e) => e.ThrowException = false;

            var menu = new MenuStrip();
            var dosya = new ToolStripMenuItem("Dosya");
            dosya.DropDownItems.Add("Aç (NCN)...", null, DosyaAc_Click);
            dosya.DropDownItems.Add("Kaydet (NCN)...", null, DosyaKaydet_Click);
            var duzenle = new ToolStripMenuItem("Düzenle");
            duzenle.DropDownItems.Add("Sırala", null, Sirala_Click);
            duzenle.DropDownItems.Add("Çift Noktaları Ayıkla", null, CiftNoktalariAyikla_Click);
            duzenle.DropDownItems.Add("Sıralı Numara Ver...", null, SiraliNumaraVer_Click);
            duzenle.DropDownItems.Add("Kolon İşlemleri...", null, KolonIslemleri_Click);
            duzenle.DropDownItems.Add("Sil", null, Sil_Click);
            menu.Items.Add(dosya);
            menu.Items.Add(duzenle);

            var tamamButton = new Button { Text = "Tamam", Size = new Size(90, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var iptalButton = new Button { Text = "İptal", Size = new Size(90, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            tamamButton.Location = new Point(altPanel.Width - 196, 7);
            iptalButton.Location = new Point(altPanel.Width - 100, 7);
            altPanel.Controls.Add(_durumLabel);
            altPanel.Controls.Add(tamamButton);
            altPanel.Controls.Add(iptalButton);
            tamamButton.Click += TamamButton_Click;
            CancelButton = iptalButton;

            Controls.Add(_tablo);
            Controls.Add(altPanel);
            Controls.Add(menu);
            MainMenuStrip = menu;

            TabloyuYenile();
        }

        private static string Sayi(double deger)
        {
            return deger.ToString("0.000", CultureInfo.InvariantCulture);
        }

        /// <summary>Liste değiştikten sonra satır sayısını, tabaka seçeneklerini ve durum yazısını günceller.</summary>
        private void TabloyuYenile()
        {
            // Açılır listede çizimdeki tabakalar ve satırlarda geçen (ör. silinmiş) tabaka adları bulunur.
            var tabakaAdlari = new List<string>(_panel.Tabakalar.Select(t => t.Ad));
            if (tabakaAdlari.Count == 0)
                tabakaAdlari.Add(_panel.AktifTabaka.Ad);
            foreach (var ad in _noktalar.Select(n => n.Tabaka).Distinct())
            {
                if (!string.IsNullOrEmpty(ad) && !tabakaAdlari.Contains(ad))
                    tabakaAdlari.Add(ad);
            }
            _tabakaKolonu.Items.Clear();
            _tabakaKolonu.Items.AddRange(tabakaAdlari.Cast<object>().ToArray());

            // Tabakası boş olan (dosyadan yüklenen) satırlar aktif tabakaya girer.
            foreach (var nokta in _noktalar)
            {
                if (string.IsNullOrEmpty(nokta.Tabaka))
                    nokta.Tabaka = _panel.AktifTabaka.Ad;
            }

            _tablo.RowCount = _noktalar.Count;
            _tablo.Invalidate();
            _durumLabel.Text = _noktalar.Count + " nokta";
        }

        private void Tablo_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _noktalar.Count)
                return;

            var nokta = _noktalar[e.RowIndex];
            switch (e.ColumnIndex)
            {
                case SiraKolon: e.Value = (e.RowIndex + 1).ToString(); break;
                case AdKolon: e.Value = nokta.Ad; break;
                case YKolon: e.Value = Sayi(nokta.Y); break;
                case XKolon: e.Value = Sayi(nokta.X); break;
                case ZKolon: e.Value = Sayi(nokta.Z); break;
                case KodKolon: e.Value = nokta.Kod; break;
                case PaftaKolon: e.Value = nokta.Pafta; break;
                case TabakaKolon: e.Value = nokta.Tabaka; break;
            }
        }

        private void Tablo_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _noktalar.Count)
                return;

            var nokta = _noktalar[e.RowIndex];
            var metin = Convert.ToString(e.Value)?.Trim() ?? "";
            switch (e.ColumnIndex)
            {
                case AdKolon:
                    // Nokta adı boş bırakılamaz; boş girilirse eski ad kalır.
                    if (metin != "")
                        nokta.Ad = metin;
                    break;
                case YKolon:
                    if (NcnDosyasi.SayiOku(metin, out var y)) nokta.Y = y;
                    break;
                case XKolon:
                    if (NcnDosyasi.SayiOku(metin, out var x)) nokta.X = x;
                    break;
                case ZKolon:
                    if (NcnDosyasi.SayiOku(metin, out var z)) nokta.Z = z;
                    break;
                case KodKolon: nokta.Kod = metin; break;
                case PaftaKolon: nokta.Pafta = metin; break;
                case TabakaKolon:
                    if (metin != "")
                        nokta.Tabaka = metin;
                    break;
            }
        }

        // ------------------------------------------------------------------ Dosya

        private void DosyaAc_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "NCN Dosyası Seçin", Filter = "NCN (*.ncn)|*.ncn|Tüm dosyalar (*.*)|*.*" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                List<NoktaKaydi> yuklenenler;
                try
                {
                    yuklenenler = NcnDosyasi.Oku(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("NCN dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (yuklenenler.Count == 0)
                {
                    MessageBox.Show("Dosyada nokta bulunamadı.", "Nokta Editörü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Dosyadaki noktalar listenin sonuna eklenir; Tamam'da çizime yeni nokta olarak girer.
                _tablo.EndEdit();
                _noktalar.AddRange(yuklenenler);
                TabloyuYenile();
            }
        }

        private void DosyaKaydet_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();
            using (var dlg = new SaveFileDialog { Title = "NCN Olarak Kaydet", Filter = "NCN (*.ncn)|*.ncn", FileName = "noktalar.ncn" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    NcnDosyasi.Yaz(_noktalar, dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("NCN dosyası kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ------------------------------------------------------------------ Düzenle

        // Sayısal adlar sayı değerine göre (2, 10'dan önce), diğerleri alfabetik sıralanır; sayılar harflerden önce gelir.
        private static int AdKarsilastir(string a, string b)
        {
            var aSayi = long.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out var x);
            var bSayi = long.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out var y);
            if (aSayi && bSayi)
                return x.CompareTo(y);
            if (aSayi != bSayi)
                return aSayi ? -1 : 1;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private void Sirala_Click(object sender, EventArgs e)
        {
            KolonaGoreSirala(AdKolon, true);
        }

        // Tablonun o an hangi kolona göre, hangi yönde sıralı olduğu (-1: sıralı değil).
        private int _siraliKolon = -1;
        private bool _artanSirali;

        /// <summary>
        /// Listeyi verilen kolona göre sıralar ve başlığında sıralama okunu gösterir. Sayı kolonları (Y, X, Z) sayı
        /// değerine, Nokta Adı sayısal adlar önce gelecek şekilde, diğerleri alfabetik sıralanır.
        /// </summary>
        private void KolonaGoreSirala(int kolon, bool artan)
        {
            _tablo.EndEdit();

            Comparison<NoktaKaydi> karsilastir;
            switch (kolon)
            {
                case AdKolon: karsilastir = (a, b) => AdKarsilastir(a.Ad, b.Ad); break;
                case YKolon: karsilastir = (a, b) => a.Y.CompareTo(b.Y); break;
                case XKolon: karsilastir = (a, b) => a.X.CompareTo(b.X); break;
                case ZKolon: karsilastir = (a, b) => a.Z.CompareTo(b.Z); break;
                case KodKolon: karsilastir = (a, b) => string.Compare(a.Kod, b.Kod, StringComparison.OrdinalIgnoreCase); break;
                case PaftaKolon: karsilastir = (a, b) => string.Compare(a.Pafta, b.Pafta, StringComparison.OrdinalIgnoreCase); break;
                case TabakaKolon: karsilastir = (a, b) => string.Compare(a.Tabaka, b.Tabaka, StringComparison.OrdinalIgnoreCase); break;
                default: return;
            }

            // Değeri aynı olan satırların kendi aralarındaki sırası korunsun diye kararlı sıralama yapılır.
            var yon = artan ? 1 : -1;
            var sirali = _noktalar.Select((nokta, sira) => new KeyValuePair<NoktaKaydi, int>(nokta, sira)).ToList();
            sirali.Sort((a, b) =>
            {
                var sonuc = yon * karsilastir(a.Key, b.Key);
                return sonuc != 0 ? sonuc : a.Value.CompareTo(b.Value);
            });
            _noktalar.Clear();
            _noktalar.AddRange(sirali.Select(k => k.Key));

            _siraliKolon = kolon;
            _artanSirali = artan;
            foreach (DataGridViewColumn baslik in _tablo.Columns)
            {
                if (baslik.SortMode != DataGridViewColumnSortMode.NotSortable)
                    baslik.HeaderCell.SortGlyphDirection = baslik.Index != kolon ? SortOrder.None : artan ? SortOrder.Ascending : SortOrder.Descending;
            }
            _tablo.ClearSelection();
            TabloyuYenile();
        }

        private void CiftNoktalariAyikla_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();

            // Y ve X'i milimetreye kadar aynı olan noktalar çift sayılır; ilki dışındakilerin adının başına Z eklenir.
            var gorulenler = new HashSet<string>();
            var isaretlenen = 0;
            foreach (var nokta in _noktalar)
            {
                var anahtar = Math.Round(nokta.Y, 3).ToString("R", CultureInfo.InvariantCulture) + "|" + Math.Round(nokta.X, 3).ToString("R", CultureInfo.InvariantCulture);
                if (gorulenler.Add(anahtar))
                    continue;

                // Daha önce işaretlenmiş nokta, komut yeniden çalıştırıldığında ikinci bir Z almaz.
                if (!nokta.Ad.StartsWith("Z", StringComparison.Ordinal))
                    nokta.Ad = "Z" + nokta.Ad;
                isaretlenen++;
            }

            _tablo.Invalidate();
            MessageBox.Show(
                isaretlenen == 0 ? "Koordinatı aynı olan nokta bulunamadı." : isaretlenen + " çift noktanın adının başına Z eklendi.",
                "Çift Noktaları Ayıkla", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SiraliNumaraVer_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();
            var degerler = SayilariSor("Sıralı Numara Ver", new[] { "Başlangıç numarası:" }, new[] { 1.0 });
            if (degerler == null)
                return;

            // Noktalar tablodaki sırasıyla, verilen numaradan başlayarak birer artan numara alır.
            var numara = (long)Math.Round(degerler[0]);
            foreach (var nokta in _noktalar)
                nokta.Ad = (numara++).ToString(CultureInfo.InvariantCulture);
            _tablo.Invalidate();
        }

        private void KolonIslemleri_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();
            var degerler = SayilariSor("Kolon İşlemleri - Öteleme", new[] { "Y'ye eklenecek:", "X'e eklenecek:", "Z'ye eklenecek:" }, new[] { 0.0, 0.0, 0.0 });
            if (degerler == null)
                return;

            // Girilen değerler (çıkarmak için eksi) bütün satırların Y, X ve Z'sine eklenir.
            foreach (var nokta in _noktalar)
            {
                nokta.Y += degerler[0];
                nokta.X += degerler[1];
                nokta.Z += degerler[2];
            }
            _tablo.Invalidate();
        }

        private void Sil_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();
            var satirlar = _tablo.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).ToList();
            if (satirlar.Count == 0 && _tablo.CurrentCell != null)
                satirlar.Add(_tablo.CurrentCell.RowIndex);
            if (satirlar.Count == 0)
                return;

            if (satirlar.Count > 1 && MessageBox.Show(satirlar.Count + " nokta silinsin mi?", "Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            foreach (var satir in satirlar.OrderByDescending(s => s))
            {
                if (_noktalar[satir].Kaynak != null)
                    _silinenler.Add(_noktalar[satir].Kaynak);
                _noktalar.RemoveAt(satir);
            }
            _tablo.ClearSelection();
            TabloyuYenile();
        }

        /// <summary>Etiketleri verilen sayı kutularını soran küçük pencere; vazgeçilirse null döner.</summary>
        private double[] SayilariSor(string baslik, string[] etiketler, double[] varsayilanlar)
        {
            using (var dlg = new Form
            {
                Text = baslik,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(300, 20 + etiketler.Length * 30 + 44)
            })
            {
                var kutular = new List<TextBox>();
                for (var i = 0; i < etiketler.Length; i++)
                {
                    dlg.Controls.Add(new Label { Text = etiketler[i], Location = new Point(12, 17 + i * 30), AutoSize = true });
                    var kutu = new TextBox { Text = varsayilanlar[i].ToString("0.###", CultureInfo.InvariantCulture), Location = new Point(150, 14 + i * 30), Size = new Size(138, 20) };
                    dlg.Controls.Add(kutu);
                    kutular.Add(kutu);
                }

                var ust = 24 + etiketler.Length * 30;
                var tamam = new Button { Text = "Tamam", Location = new Point(132, ust), Size = new Size(75, 26) };
                var iptal = new Button { Text = "İptal", Location = new Point(213, ust), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                double[] sonuc = null;
                tamam.Click += (s, e) =>
                {
                    var degerler = new double[kutular.Count];
                    for (var i = 0; i < kutular.Count; i++)
                    {
                        if (NcnDosyasi.SayiOku(kutular[i].Text, out degerler[i]))
                            continue;
                        MessageBox.Show("Lütfen geçerli bir sayı girin.", baslik, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        kutular[i].Focus();
                        return;
                    }
                    sonuc = degerler;
                    dlg.DialogResult = DialogResult.OK;
                };
                dlg.Controls.Add(tamam);
                dlg.Controls.Add(iptal);
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;

                return dlg.ShowDialog(this) == DialogResult.OK ? sonuc : null;
            }
        }

        // ------------------------------------------------------------------ Tamam

        private void TamamButton_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();

            foreach (var silinen in _silinenler)
                _panel.Nesneler.Remove(silinen);

            foreach (var kayit in _noktalar)
            {
                var nokta = kayit.Kaynak;
                if (nokta == null)
                {
                    nokta = new NoktaNesnesi();
                    _panel.Nesneler.Add(nokta);
                    YeniNoktaEklendi = true;
                }

                // Tabloda Y sağa, X yukarı değerdir; çizimde bunlar sırasıyla X ve Y'dir.
                nokta.NoktaAdi = kayit.Ad;
                nokta.X = kayit.Y;
                nokta.Y = kayit.X;
                nokta.Z = kayit.Z;
                nokta.Kod = kayit.Kod;
                nokta.Pafta = kayit.Pafta;
                nokta.Tabaka = _panel.TabakaBulVeyaEkle(kayit.Tabaka, Color.Black, true, false);
            }

            _panel.TabakalarDegisti();
            DialogResult = DialogResult.OK;
        }
    }
}
