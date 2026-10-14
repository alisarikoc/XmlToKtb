using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Common;
using Common.Common;
using netDxf;
using netDxf.Entities;
using XmlToKtb.Cizim;
using XmlToKtb.Projeksiyon;

namespace XmlToKtb
{
    public partial class CizimEkrani : Form
    {
        private ProjeksiyonSistemi _aktifSistem = ProjeksiyonSistemi.Tanimsiz;
        private DilimBilgisi? _aktifDilim;

        public CizimEkrani()
        {
            InitializeComponent();
            cizimPaneli1.SecimDegisti += CizimPaneli1_SecimDegisti;
            cizimPaneli1.FareKonumuDegisti += CizimPaneli1_FareKonumuDegisti;
            cizimPaneli1.NesnelerGuncellendi += (s, e) => ozellikGrid.Refresh();
            cizimPaneli1.AracDegisti += (s, arac) => YuzeyDuzenleIpucunuGuncelle(arac);
            YuzeyDuzenlePaneliniOlustur();
            EsYukseklikPaneliniOlustur();
            cizimPaneli1.TabakalarAcik = true;

            // Nokta bulutu (LAS) görüntüleyicisi ayrı bir pencerede açılır.
            var noktaBulutuMenuItem = new ToolStripMenuItem("Nokta Bulutu (LAS)");
            noktaBulutuMenuItem.Click += (s, e) => new Las.LasGoruntuleyiciForm().Show();
            anaMenuStrip.Items.Insert(anaMenuStrip.Items.IndexOf(kesitEditoruMenuItem) + 1, noktaBulutuMenuItem);

            // Durum çubuğunun sağında proje ölçeği gösterilir; tıklanınca değiştirilir.
            _olcekDurumLabel = new ToolStripStatusLabel
            {
                Alignment = ToolStripItemAlignment.Right,
                IsLink = true,
                ToolTipText = "Proje ölçeği: kilometre yazıları gibi kağıtta sabit boyda olan yazıların boyunu belirler. Değiştirmek için tıklayın."
            };
            _olcekDurumLabel.Click += olcekDurumLabel_Click;
            durumStrip.Items.Add(_olcekDurumLabel);
            OlcekEtiketiniGuncelle();

            // Koordinat yazısı aradaki boşluğu doldurur; böylece koordinat sistemi ve ölçek çubuğun sağ ucunda durur.
            konumDurumLabel.Spring = true;
            konumDurumLabel.TextAlign = ContentAlignment.MiddleLeft;

            // Seçili güzergaha sağ tıklanınca "Güzergah Editörü" menüsü çıkar.
            _guzergahMenusu.Items.Add("Güzergah Editörü", null, (s, e) => GuzergahEditorunuAc(SeciliGuzergah()));
            _guzergahMenusu.Items.Add("Düşey Editörü", null, (s, e) => DuseyEditorunuAc(SeciliGuzergah()));
            _guzergahMenusu.Items.Add("Dever Editörü", null, (s, e) => DeverEditorunuAc(SeciliGuzergah()));
            _guzergahMenusu.Items.Add(new ToolStripSeparator());
            _guzergahMenusu.Items.Add("Farklı Kaydet...", null, (s, e) => GuzergahiFarkliKaydet(SeciliGuzergah()));
            _guzergahMenusu.Items.Add("Kilometre Etiketi Ekle", null, (s, e) =>
            {
                var guzergah = SeciliGuzergah();
                if (guzergah == null)
                    return;
                // Aynı güzergaha birden fazla etiket seti eklenebilir (ör. biri birleşim noktaları, biri her 100 m için).
                cizimPaneli1.Nesneler.Add(new GuzergahEtiketNesnesi(guzergah));
                cizimPaneli1.Invalidate();
            });
            cizimPaneli1.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && cizimPaneli1.AktifArac == CizimAraci.Sec && !_noktaSecimiBekleniyor && SeciliGuzergah() != null)
                    _guzergahMenusu.Show(cizimPaneli1, e.Location);
            };
        }

        private ToolStripStatusLabel _olcekDurumLabel;

        private void OlcekEtiketiniGuncelle()
        {
            _olcekDurumLabel.Text = "Ölçek: 1/" + CizimAyarlari.ProjeOlcegi.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void olcekDurumLabel_Click(object sender, EventArgs e)
        {
            using (var dlg = new OlcekFaktoruForm("Proje Ölçeği", "Ölçek  1 /", CizimAyarlari.ProjeOlcegi))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                CizimAyarlari.ProjeOlcegi = dlg.Deger;
                OlcekEtiketiniGuncelle();
                // Ölçeğe bağlı yazıların (güzergah kilometreleri) boyu değişti.
                cizimPaneli1.Invalidate();
            }
        }

        private readonly ContextMenuStrip _guzergahMenusu = new ContextMenuStrip();
        private readonly Dictionary<GuzergahNesnesi, GuzergahEditoruForm> _acikGuzergahEditorleri = new Dictionary<GuzergahNesnesi, GuzergahEditoruForm>();

        /// <summary>Çizimde tek başına seçili olan güzergah; seçim güzergah değilse ya da birden fazla obje seçiliyse null.</summary>
        private GuzergahNesnesi SeciliGuzergah()
        {
            var secililer = cizimPaneli1.Nesneler.Where(n => n.Secili).Take(2).ToList();
            return secililer.Count == 1 ? secililer[0] as GuzergahNesnesi : null;
        }

        /// <summary>
        /// Güzergah editörünü çizim ekranının üzerinde, ekran kullanılmaya devam edilebilecek şekilde açar; aynı
        /// güzergahın editörü zaten açıksa onu öne getirir.
        /// </summary>
        private void GuzergahEditorunuAc(GuzergahNesnesi guzergah)
        {
            if (guzergah == null)
                return;

            if (_acikGuzergahEditorleri.TryGetValue(guzergah, out var acikEditor))
            {
                acikEditor.Activate();
                return;
            }

            var editor = new GuzergahEditoruForm(cizimPaneli1, guzergah);
            // Pencere çizimi kapatmasın diye ana pencerenin alt kısmına yerleştirilir.
            editor.Location = new System.Drawing.Point(
                Math.Max(Left, Left + (Width - editor.Width) / 2),
                Math.Max(Top, Bottom - editor.Height - 40));
            _acikGuzergahEditorleri[guzergah] = editor;
            editor.FormClosed += (s, e) =>
            {
                _acikGuzergahEditorleri.Remove(guzergah);
                // Hiç çizilmeden (ya da vazgeçilerek) boş kalan güzergah çizimde bırakılmaz.
                if (guzergah.Parcalar.Count == 0)
                    cizimPaneli1.Nesneler.Remove(guzergah);
                ozellikGrid.Refresh();
                cizimPaneli1.Invalidate();
            };
            editor.Show(this);
        }

        private void DuseyEditorunuAc(GuzergahNesnesi guzergah)
        {
            if (guzergah == null)
                return;

            using (var editor = new DuseyEditoruForm(guzergah))
                editor.ShowDialog(this);
            ozellikGrid.Refresh();
        }

        private void DeverEditorunuAc(GuzergahNesnesi guzergah)
        {
            if (guzergah == null)
                return;

            using (var editor = new DeverListesiForm(guzergah.Dever, "Dever Editörü - " + guzergah.Ad))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                    return;

                guzergah.Dever.Satirlar.Clear();
                guzergah.Dever.Satirlar.AddRange(editor.Satirlar);
            }
            ozellikGrid.Refresh();
        }

        /// <summary>Güzergahı LandXML ya da NetCAD KTB dosyası olarak kaydeder (kayıt penceresinde seçilen türe göre).</summary>
        private void GuzergahiFarkliKaydet(GuzergahNesnesi guzergah)
        {
            if (guzergah == null)
                return;

            if (guzergah.Parcalar.Count == 0)
            {
                MessageBox.Show("Güzergahta kaydedilecek parça yok.", "Farklı Kaydet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new SaveFileDialog
            {
                Title = "Güzergahı Farklı Kaydet",
                Filter = "LandXML (*.xml)|*.xml|NetCAD KTB (*.ktb)|*.ktb",
                FileName = string.IsNullOrWhiteSpace(guzergah.Ad) ? "guzergah" : string.Join("_", guzergah.Ad.Split(Path.GetInvalidFileNameChars()))
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    var landXml = GuzergahCizici.LandXmlOlustur(guzergah);
                    var ktbOlarak = Path.GetExtension(dlg.FileName).Equals(".ktb", StringComparison.OrdinalIgnoreCase);
                    // KTB içeriği, ana dönüştürme ekranındaki XML -> KTB dönüşümüyle aynı kodla üretilir.
                    File.WriteAllText(dlg.FileName, ktbOlarak ? KTBIcerigi.Duzenleyici(landXml) : KTBtoXML.Serialize(landXml), new UTF8Encoding(false));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Güzergah kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void guzergahEditoruMenuItem_Click(object sender, EventArgs e)
        {
            // Seçili güzergah yoksa ve çizimde tek güzergah varsa o düzenlenir.
            var guzergah = SeciliGuzergah();
            if (guzergah == null)
            {
                var hepsi = cizimPaneli1.Nesneler.OfType<GuzergahNesnesi>().ToList();
                if (hepsi.Count == 1)
                    guzergah = hepsi[0];
            }

            if (guzergah == null)
            {
                // Çizimde birden fazla güzergah varken hiçbiri seçili değilse hangisinin istendiği sorulur.
                if (cizimPaneli1.Nesneler.OfType<GuzergahNesnesi>().Any() &&
                    MessageBox.Show("Seçili güzergah yok. Yeni bir güzergah çizilsin mi?\n\nMevcut bir güzergahı düzenlemek için Hayır deyip önce onu çizimden seçin.",
                        "Güzergah Editörü", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                // Güzergah yok: boş bir güzergah oluşturulur ve editör ekrandan çizim komutuyla açılır.
                var yeni = new GuzergahNesnesi { Ad = "Güzergah " + (cizimPaneli1.Nesneler.OfType<GuzergahNesnesi>().Count() + 1) };
                cizimPaneli1.Nesneler.Add(yeni);
                GuzergahEditorunuAc(yeni);
                if (_acikGuzergahEditorleri.TryGetValue(yeni, out var yeniEditor))
                    yeniEditor.CizimeBasla();
                return;
            }

            GuzergahEditorunuAc(guzergah);
        }

        // "Grafikten Seç" ile başlatılan nokta seçimi sürüyor mu? Sürüyorsa seçim onaylanınca nokta editörü açılır.
        private bool _noktaSecimiBekleniyor;
        private ToolStripStatusLabel _ipucuDurumLabel;

        private void noktaEditoruMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new NoktaSecimForm(cizimPaneli1.Nesneler))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                if (!dlg.GrafiktenSecilecek)
                {
                    NoktaEditorunuAc(dlg.Secilenler);
                    return;
                }
            }

            // Grafikten seçim: kullanıcı Seç aracıyla noktaları seçer, Enter ya da sağ tıkla bitirir.
            cizimPaneli1.AktifArac = CizimAraci.Sec;
            foreach (var nesne in cizimPaneli1.Nesneler)
                nesne.Secili = false;
            ozellikGrid.SelectedObject = null;
            cizimPaneli1.Invalidate();

            if (_ipucuDurumLabel == null)
            {
                _ipucuDurumLabel = new ToolStripStatusLabel { ForeColor = Color.Firebrick };
                durumStrip.Items.Insert(1, _ipucuDurumLabel);
                cizimPaneli1.SecimOnaylandi += CizimPaneli1_SecimOnaylandi;
                // Seçim sırasında Esc'ye basılırsa nokta seçiminden vazgeçilmiş sayılır.
                cizimPaneli1.SecimIptalEdildi += (s, a) => NoktaSeciminiBitir();
            }
            _ipucuDurumLabel.Text = "   Nokta Editörü: noktaları seçin (tıklayın ya da pencere çekin; Shift ile ekleyin). Bitirmek için Enter ya da sağ tık, vazgeçmek için Esc.";
            _noktaSecimiBekleniyor = true;
            cizimPaneli1.Focus();
        }

        private void CizimPaneli1_SecimOnaylandi(object sender, EventArgs e)
        {
            if (!_noktaSecimiBekleniyor)
                return;

            var secilenler = cizimPaneli1.Nesneler.OfType<NoktaNesnesi>().Where(n => n.Secili).ToList();
            if (secilenler.Count == 0)
            {
                // Hiç nokta seçilmeden onaylandıysa seçim sürer; vazgeçmek için Esc'ye basılır.
                MessageBox.Show("Seçimde nokta yok. Noktaları seçip tekrar Enter'a basın ya da Esc ile vazgeçin.", "Nokta Editörü", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            NoktaSeciminiBitir();
            NoktaEditorunuAc(secilenler);
        }

        private void NoktaSeciminiBitir()
        {
            _noktaSecimiBekleniyor = false;
            if (_ipucuDurumLabel != null)
                _ipucuDurumLabel.Text = "";
        }

        private void NoktaEditorunuAc(List<NoktaNesnesi> noktalar)
        {
            using (var editor = new NoktaEditoruForm(cizimPaneli1, noktalar))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                    return;

                // Dosyadan yüklenen noktalar ekranın dışında kalabilir; yeni nokta eklendiyse çizim ekrana sığdırılır.
                if (editor.YeniNoktaEklendi)
                    cizimPaneli1.TumObjeleriGoster();
            }

            ozellikGrid.Refresh();
            cizimPaneli1.Invalidate();
        }

        private void tabakaAyarlariMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new TabakaAyarlariForm(cizimPaneli1))
                dlg.ShowDialog(this);

            // Seçili objenin tabakası ya da tabaka adı değişmiş olabilir.
            ozellikGrid.Refresh();
        }

        /// <summary>
        /// Özellikler ile Yüzey Düzenle arasındaki "Eş Yükseklik Eğrileri" bölümünü kurar: ana (major) ve ara (minor)
        /// eğriler için göster kutusu, kot aralığı ve renk. Değişiklikler çizime hemen yansır.
        /// </summary>
        private void EsYukseklikPaneliniOlustur()
        {
            var panel = new Panel { Dock = DockStyle.Bottom, Height = 86, Visible = false };
            var baslik = new Label
            {
                Text = "Eş Yükseklik Eğrileri",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(6, 4, 0, 4),
                Font = ozelliklerBaslikLabel.Font
            };
            panel.Controls.Add(baslik);

            // Bir eğri satırı: [göster] ad  [aralık] m  [renk]. Ayar her değiştiğinde uygula(goster, aralik, renk) çağrılır.
            void SatirEkle(string ad, int ust, bool goster, double aralik, Color renk, Action<bool, double, Color> uygula)
            {
                var gosterKutusu = new CheckBox { Text = ad, Checked = goster, Location = new System.Drawing.Point(8, ust + 2), Size = new Size(92, 20) };
                var aralikKutusu = new TextBox { Text = aralik.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), Location = new System.Drawing.Point(104, ust), Size = new Size(56, 20), TextAlign = HorizontalAlignment.Right };
                var birim = new Label { Text = "m", Location = new System.Drawing.Point(163, ust + 3), AutoSize = true };
                var renkDugmesi = new Button { BackColor = renk, FlatStyle = FlatStyle.Flat, Location = new System.Drawing.Point(186, ust - 1), Size = new Size(62, 22) };
                var gecerliAralik = aralik;

                void Uygula()
                {
                    var metin = aralikKutusu.Text.Trim().Replace(',', '.');
                    if (double.TryParse(metin, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var yeniAralik) && yeniAralik > 0)
                        gecerliAralik = yeniAralik;
                    // Geçersiz ya da sıfır/eksi değer girildiyse son geçerli aralık geri yazılır.
                    aralikKutusu.Text = gecerliAralik.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                    uygula(gosterKutusu.Checked, gecerliAralik, renkDugmesi.BackColor);
                    cizimPaneli1.Invalidate();
                }

                gosterKutusu.CheckedChanged += (s, e) => Uygula();
                aralikKutusu.Leave += (s, e) => Uygula();
                aralikKutusu.KeyDown += (s, e) =>
                {
                    if (e.KeyCode != Keys.Enter)
                        return;
                    Uygula();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                };
                renkDugmesi.Click += (s, e) =>
                {
                    using (var dlg = new ColorDialog { Color = renkDugmesi.BackColor, FullOpen = true })
                    {
                        if (dlg.ShowDialog(this) != DialogResult.OK)
                            return;
                        renkDugmesi.BackColor = dlg.Color;
                        Uygula();
                    }
                };

                panel.Controls.AddRange(new Control[] { gosterKutusu, aralikKutusu, birim, renkDugmesi });
            }

            SatirEkle("Major eğri", 28, EsYukseklikAyarlari.MajorGoster, EsYukseklikAyarlari.MajorAralik, EsYukseklikAyarlari.MajorRenk, EsYukseklikAyarlari.MajorAyarla);
            SatirEkle("Minor eğri", 56, EsYukseklikAyarlari.MinorGoster, EsYukseklikAyarlari.MinorAralik, EsYukseklikAyarlari.MinorRenk, EsYukseklikAyarlari.MinorAyarla);

            // Bölüm Yüzey Düzenle'nin hemen üstünde dursun diye alt kenara ondan sonra yerleşecek sıraya konur
            // (dock'ta listede sonda olan önce yerleşir).
            ozelliklerPaneli.Controls.Add(panel);
            ozelliklerPaneli.Controls.SetChildIndex(panel, ozelliklerPaneli.Controls.GetChildIndex(_yuzeyDuzenlePaneli));
            _egriPaneli = panel;
        }

        private void CizimPaneli1_SecimDegisti(object sender, CizimNesnesi secilenNesne)
        {
            ozellikGrid.SelectedObject = secilenNesne;


            // Eş Yükseklik Eğrileri ve Yüzey Düzenle bölümleri yalnızca tek bir yüzey seçiliyken görünür.
            _seciliYuzey = secilenNesne as YuzeyNesnesi;
            _egriPaneli.Visible = _seciliYuzey != null;
            _yuzeyDuzenlePaneli.Visible = _seciliYuzey != null;
        }

        private Panel _egriPaneli;
        private Panel _yuzeyDuzenlePaneli;
        private Label _yuzeyDuzenleIpucuLabel;
        private YuzeyNesnesi _seciliYuzey;

        /// <summary>Özellikler panelinin altındaki "Yüzey Düzenle" bölümünü (başlık, komut düğmeleri, ipucu) kurar.</summary>
        private void YuzeyDuzenlePaneliniOlustur()
        {
            _yuzeyDuzenlePaneli = new Panel { Dock = DockStyle.Bottom, Height = 196, Visible = false };

            var baslik = new Label
            {
                Text = "Yüzey Düzenle",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(6, 4, 0, 4),
                Font = ozelliklerBaslikLabel.Font
            };

            var dugmeler = new TableLayoutPanel { Dock = DockStyle.Top, Height = 124, ColumnCount = 2, RowCount = 4, Padding = new Padding(4, 0, 4, 0) };
            dugmeler.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            dugmeler.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (var i = 0; i < 4; i++)
                dugmeler.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            void DugmeEkle(string metin, EventHandler tiklama)
            {
                var dugme = new Button { Text = metin, Dock = DockStyle.Fill, Margin = new Padding(2) };
                dugme.Click += tiklama;
                dugmeler.Controls.Add(dugme);
            }

            void AracDugmesiEkle(string metin, CizimAraci arac)
            {
                DugmeEkle(metin, (s, e) =>
                {
                    if (_seciliYuzey == null)
                        return;
                    cizimPaneli1.DuzenlenenYuzey = _seciliYuzey;
                    cizimPaneli1.AktifArac = arac;
                    cizimPaneli1.Focus();
                });
            }

            AracDugmesiEkle("Çizgi Ekle", CizimAraci.YuzeyCizgiEkle);
            AracDugmesiEkle("Çizgi Sil", CizimAraci.YuzeyCizgiSil);
            AracDugmesiEkle("Üçgen Döndür", CizimAraci.YuzeyUcgenDondur);
            AracDugmesiEkle("Nokta Ekle", CizimAraci.YuzeyNoktaEkle);
            AracDugmesiEkle("Nokta Sil", CizimAraci.YuzeyNoktaSil);
            AracDugmesiEkle("Nokta Düzelt", CizimAraci.YuzeyNoktaDuzelt);
            AracDugmesiEkle("Nokta Taşı", CizimAraci.YuzeyNoktaTasi);
            DugmeEkle("Geri Al", (s, e) =>
            {
                if (_seciliYuzey == null)
                    return;
                if (!_seciliYuzey.GeriAlinabilir)
                {
                    MessageBox.Show("Geri alınacak bir yüzey düzenlemesi yok.", "Yüzey Düzenle", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                _seciliYuzey.GeriAl();
                // Yarım kalmış iki tıklamalı işlem eski noktalara bağlı kalmasın diye araç yeniden başlatılır.
                var arac = cizimPaneli1.AktifArac;
                cizimPaneli1.AktifArac = CizimAraci.Sec;
                if (CizimPaneli.YuzeyAraciMi(arac))
                    cizimPaneli1.AktifArac = arac;
                ozellikGrid.Refresh();
                cizimPaneli1.Invalidate();
            });

            _yuzeyDuzenleIpucuLabel = new Label { Dock = DockStyle.Fill, Padding = new Padding(6, 4, 6, 0), ForeColor = SystemColors.GrayText };

            // Dock sırası: sonra eklenen önce yerleşir; ipucu kalan alanı doldurduğu için ilk eklenir.
            _yuzeyDuzenlePaneli.Controls.Add(_yuzeyDuzenleIpucuLabel);
            _yuzeyDuzenlePaneli.Controls.Add(dugmeler);
            _yuzeyDuzenlePaneli.Controls.Add(baslik);
            ozelliklerPaneli.Controls.Add(_yuzeyDuzenlePaneli);

            YuzeyDuzenleIpucunuGuncelle(CizimAraci.Sec);
        }

        private void YuzeyDuzenleIpucunuGuncelle(CizimAraci arac)
        {
            switch (arac)
            {
                case CizimAraci.YuzeyCizgiEkle:
                    _yuzeyDuzenleIpucuLabel.Text = "Çizgi Ekle: çizginin iki ucundaki noktaları sırayla tıklayın.";
                    break;
                case CizimAraci.YuzeyCizgiSil:
                    _yuzeyDuzenleIpucuLabel.Text = "Çizgi Sil: silinecek çizgiyi tıklayın.";
                    break;
                case CizimAraci.YuzeyUcgenDondur:
                    _yuzeyDuzenleIpucuLabel.Text = "Üçgen Döndür: iki üçgenin ortak kenarını tıklayın.";
                    break;
                case CizimAraci.YuzeyNoktaEkle:
                    _yuzeyDuzenleIpucuLabel.Text = "Nokta Ekle: noktanın yerini tıklayın.";
                    break;
                case CizimAraci.YuzeyNoktaSil:
                    _yuzeyDuzenleIpucuLabel.Text = "Nokta Sil: silinecek noktayı tıklayın.";
                    break;
                case CizimAraci.YuzeyNoktaDuzelt:
                    _yuzeyDuzenleIpucuLabel.Text = "Nokta Düzelt: düzeltilecek noktayı tıklayın.";
                    break;
                case CizimAraci.YuzeyNoktaTasi:
                    _yuzeyDuzenleIpucuLabel.Text = "Nokta Taşı: önce noktayı, sonra yeni yerini tıklayın.";
                    break;
                default:
                    _yuzeyDuzenleIpucuLabel.Text = "Bir yüzey seçip komut verin. Sağ tık ya da Esc komuttan çıkar.";
                    break;
            }
        }

        private void CizimPaneli1_FareKonumuDegisti(object sender, System.Drawing.PointF dunyaKonumu)
        {
            konumDurumLabel.Text = $"X: {dunyaKonumu.X:0.000}   Y: {dunyaKonumu.Y:0.000}";
        }

        private void koordinatSistemiLabel_Click(object sender, EventArgs e)
        {
            using (var dlg = new KoordinatSistemiForm(_aktifSistem, _aktifDilim))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                var yeniSistem = dlg.SecilenSistem;
                var yeniDilim = dlg.SecilenDilim;
                var ilkTanim = _aktifSistem == ProjeksiyonSistemi.Tanimsiz;
                var degisti = yeniSistem != _aktifSistem || !Equals(yeniDilim, _aktifDilim);

                if (degisti && !ilkTanim && yeniSistem != ProjeksiyonSistemi.Tanimsiz)
                {
                    var cevap = MessageBox.Show(
                        "Koordinat sistemi değişti.\n\n" +
                        "Evet: çizimdeki mevcut koordinatlar yeni sisteme dönüştürülür.\n" +
                        "Hayır: koordinatlar aynı kalır, sadece sistem etiketi değişir.",
                        "Koordinat Sistemi Değişti", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (cevap == DialogResult.Yes)
                        CizimiDonustur(_aktifSistem, _aktifDilim, yeniSistem, yeniDilim);
                }

                _aktifSistem = yeniSistem;
                _aktifDilim = yeniDilim;
                koordinatSistemiLabel.Text = ProjeksiyonBilgisi.TamAdGetir(_aktifSistem, _aktifDilim);
            }
        }

        /// <summary>
        /// Bir noktayı yeni sisteme çevirir ve o noktadaki yerel dönüklüğü (radyan, saat yönünün tersine) ve ölçeği
        /// verir. Projeksiyon değişimi küçük bir alanda öteleme + dönme + ölçekleme gibi davrandığı için daire, yay,
        /// yazı ve güzergah gibi objelerin açı ve boyları bu iki değerle düzeltilir.
        /// </summary>
        private static bool YerelDonusum(ProjeksiyonSistemi eskiSistem, DilimBilgisi? eskiDilim, ProjeksiyonSistemi yeniSistem, DilimBilgisi? yeniDilim,
            double x, double y, out double yeniX, out double yeniY, out double donus, out double olcek)
        {
            donus = 0;
            olcek = 1;
            if (!KoordinatDonusturucu.Donustur(eskiSistem, eskiDilim, x, y, yeniSistem, yeniDilim, out yeniX, out yeniY))
                return false;

            // Sağa doğru kısa bir adımın yeni sistemdeki karşılığından dönüklük ve ölçek bulunur.
            var adim = ProjeksiyonBilgisi.CografiMi(eskiSistem) ? 1e-4 : 10.0;
            if (KoordinatDonusturucu.Donustur(eskiSistem, eskiDilim, x + adim, y, yeniSistem, yeniDilim, out var ax, out var ay))
            {
                donus = Math.Atan2(ay - yeniY, ax - yeniX);
                olcek = Math.Sqrt((ax - yeniX) * (ax - yeniX) + (ay - yeniY) * (ay - yeniY)) / adim;
            }
            return true;
        }

        private void CizimiDonustur(ProjeksiyonSistemi eskiSistem, DilimBilgisi? eskiDilim, ProjeksiyonSistemi yeniSistem, DilimBilgisi? yeniDilim)
        {
            bool Nokta(double x, double y, out double yeniX, out double yeniY)
            {
                return KoordinatDonusturucu.Donustur(eskiSistem, eskiDilim, x, y, yeniSistem, yeniDilim, out yeniX, out yeniY);
            }
            bool Yerel(double x, double y, out double yeniX, out double yeniY, out double donus, out double olcek)
            {
                return YerelDonusum(eskiSistem, eskiDilim, yeniSistem, yeniDilim, x, y, out yeniX, out yeniY, out donus, out olcek);
            }
            const double radyandanDereceye = 180.0 / Math.PI;

            var donusturulemeyen = 0;
            foreach (var nesne in cizimPaneli1.Nesneler)
            {
                if (nesne is CizgiNesnesi cizgi)
                {
                    if (Nokta(cizgi.BaslangicX, cizgi.BaslangicY, out var bx, out var by))
                    {
                        cizgi.BaslangicX = bx;
                        cizgi.BaslangicY = by;
                    }

                    if (Nokta(cizgi.BitisX, cizgi.BitisY, out var ex, out var ey))
                    {
                        cizgi.BitisX = ex;
                        cizgi.BitisY = ey;
                    }
                }
                else if (nesne is YaziNesnesi yazi)
                {
                    if (Yerel(yazi.KonumX, yazi.KonumY, out var yx, out var yy, out var donus, out var olcek))
                    {
                        yazi.KonumX = yx;
                        yazi.KonumY = yy;
                        yazi.Donus += donus * radyandanDereceye;
                        yazi.Yukseklik *= olcek;
                    }
                }
                else if (nesne is NoktaNesnesi nokta)
                {
                    if (Nokta(nokta.X, nokta.Y, out var nx, out var ny))
                    {
                        nokta.X = nx;
                        nokta.Y = ny;
                    }
                }
                else if (nesne is DaireNesnesi daire)
                {
                    if (Yerel(daire.MerkezX, daire.MerkezY, out var mx, out var my, out _, out var olcek))
                    {
                        daire.MerkezX = mx;
                        daire.MerkezY = my;
                        daire.Yaricap *= olcek;
                    }
                }
                else if (nesne is YayNesnesi yay)
                {
                    if (Yerel(yay.MerkezX, yay.MerkezY, out var mx, out var my, out var donus, out var olcek))
                    {
                        yay.MerkezX = mx;
                        yay.MerkezY = my;
                        yay.Yaricap *= olcek;
                        yay.BaslangicAcisi += donus * radyandanDereceye;
                        yay.BitisAcisi += donus * radyandanDereceye;
                    }
                }
                else if (nesne is SpiralNesnesi spiral)
                {
                    if (Yerel(spiral.BaslangicX, spiral.BaslangicY, out var bx, out var by, out _, out var olcek) &&
                        Nokta(spiral.BitisX, spiral.BitisY, out var ex, out var ey) &&
                        Nokta(spiral.PiX, spiral.PiY, out var px, out var py))
                    {
                        spiral.BaslangicX = bx;
                        spiral.BaslangicY = by;
                        spiral.BitisX = ex;
                        spiral.BitisY = ey;
                        spiral.PiX = px;
                        spiral.PiY = py;
                        spiral.Uzunluk *= olcek;
                        // Sonsuz yarıçap (doğruya bağlanan uç) ölçekle değişmez.
                        spiral.YaricapBaslangic *= olcek;
                        spiral.YaricapBitis *= olcek;
                    }
                }
                else if (nesne is YuzeyNesnesi yuzey)
                {
                    for (var i = 0; i < yuzey.X.Count; i++)
                    {
                        if (Nokta(yuzey.X[i], yuzey.Y[i], out var ux, out var uy))
                        {
                            yuzey.X[i] = ux;
                            yuzey.Y[i] = uy;
                        }
                    }
                    yuzey.KoordinatlarDegisti();
                }
                else if (nesne is GuzergahNesnesi guzergah)
                {
                    if (!GuzergahiDonustur(guzergah, Yerel))
                        donusturulemeyen++;
                }
                else if (nesne is GuzergahEtiketNesnesi)
                {
                    // Etiketler güzergahtan hesaplanır; güzergahla birlikte kendiliğinden yer değiştirir.
                }
                else
                {
                    donusturulemeyen++;
                }
            }

            if (donusturulemeyen > 0)
                MessageBox.Show(donusturulemeyen + " obje yeni sisteme dönüştürülemedi ve eski koordinatlarında kaldı.",
                    "Koordinat Sistemi", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            cizimPaneli1.TumObjeleriGoster();
            cizimPaneli1.Invalidate();
        }

        private delegate bool YerelDonusumIslevi(double x, double y, out double yeniX, out double yeniY, out double donus, out double olcek);

        /// <summary>
        /// Güzergahı yeni sisteme taşır: başlangıç noktası ve dosyaya sabitlenmiş parça başlangıçları dönüştürülür,
        /// doğrultular yerel dönüklük kadar döndürülür, parça boyları ve yarıçapları yerel ölçekle çarpılır.
        /// </summary>
        private static bool GuzergahiDonustur(GuzergahNesnesi guzergah, YerelDonusumIslevi yerel)
        {
            if (!yerel(guzergah.BaslangicX, guzergah.BaslangicY, out var bx, out var by, out var baslangicDonusu, out _))
                return false;

            // Ölçek ve sabit noktalar, parçaların dönüşümden önceki konumlarından hesaplanır.
            foreach (var parca in guzergah.Parcalar)
            {
                if (!yerel(parca.BaslangicX, parca.BaslangicY, out _, out _, out _, out var olcek))
                    return false;

                if (parca.Sabit && yerel(parca.SabitX, parca.SabitY, out var sx, out var sy, out var donus, out _))
                {
                    parca.SabitX = sx;
                    parca.SabitY = sy;
                    parca.SabitAci += donus;
                }

                parca.Uzunluk *= olcek;
                parca.YaricapBaslangic *= olcek;
                parca.YaricapBitis *= olcek;
            }

            guzergah.BaslangicX = bx;
            guzergah.BaslangicY = by;
            guzergah.BaslangicAcisi += baslangicDonusu;
            guzergah.Hesapla();
            return true;
        }

        private void cizgiCizMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Cizgi;
        }

        private void daireMerkezYaricapMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.DaireMerkezYaricap;
        }

        private void daire3NoktaMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Daire3Nokta;
        }

        private void daire2NoktaYaricapMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Daire2NoktaYaricap;
        }

        private void daireTegetIkiObjeMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.DaireTegetIkiObje;
        }

        private void daireTegetUcObjeMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.DaireTegetUcObje;
        }

        private void guzergahXmlCizMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Güzergah XML Dosyası Seçin", Filter = "XML (*.xml)|*.xml" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                LandXML landXml;
                try
                {
                    var serializer = new DosyaDeserializer();
                    landXml = serializer.XmlDeserializer<LandXML>(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("XML dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Güzergah, parçaları (doğru, kurp, spiral) tek bir obje içinde tutulacak şekilde çizilir.
                var guzergah = GuzergahCizici.GuzergahOlustur(landXml);
                if (guzergah == null || guzergah.Parcalar.Count == 0)
                {
                    MessageBox.Show("Dosyada bir güzergah ekseni (Alignment) bulunamadı.", "Güzergah Çiz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                cizimPaneli1.Nesneler.Add(guzergah);
                // Kilometre etiketleri güzergaha bağlı ayrı bir objedir: ayrıca seçilir, özellikleri değiştirilir, silinir.
                cizimPaneli1.Nesneler.Add(new GuzergahEtiketNesnesi(guzergah));
                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }

        private void kaydirMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kaydir;
        }

        private void dondurMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Dondur;
        }

        private void kesMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kes;
        }

        private void kopyalaMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kopyala;
        }

        private void olcekleMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Olcekle;
        }

        private void ozellikGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            cizimPaneli1.SeciliNesneGuncellendi();
        }

        private void yakalamaButonu_CheckedChanged(object sender, EventArgs e)
        {
            var mod = YakalamaModu.Yok;
            if (noktaYakalaButton.Checked) mod |= YakalamaModu.Nokta;
            if (ucYakalaButton.Checked) mod |= YakalamaModu.Uc;
            if (ortaYakalaButton.Checked) mod |= YakalamaModu.Orta;
            if (dikYakalaButton.Checked) mod |= YakalamaModu.Dik;
            if (merkezYakalaButton.Checked) mod |= YakalamaModu.Merkez;
            cizimPaneli1.AktifYakalamaModu = mod;
        }

        // Yüzey Oluştur ile eklenen yüzeyler birbirinden ayırt edilsin diye sırayla bu renkleri alır.
        private static readonly Color[] YuzeyRenkleri =
        {
            Color.SaddleBrown, Color.DarkGreen, Color.DarkSlateBlue, Color.Firebrick, Color.Teal, Color.DarkGoldenrod
        };

        /// <summary>Yüklenen yüzeyleri çizime ekler ve hepsini ekrana sığdırır.</summary>
        private void YuzeyleriCizimeEkle(IEnumerable<Yuzey> yuzeyler)
        {
            foreach (var yuzey in yuzeyler)
            {
                var nesne = YuzeyNoktalari.NesneyeCevir(yuzey);
                nesne.Renk = YuzeyRenkleri[cizimPaneli1.Nesneler.OfType<YuzeyNesnesi>().Count() % YuzeyRenkleri.Length];
                cizimPaneli1.Nesneler.Add(nesne);
            }

            cizimPaneli1.TumObjeleriGoster();
            cizimPaneli1.Invalidate();
        }

        private void yuzeyXmldenMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Yüzey (LandXML) Dosyası Seçin", Filter = "XML (*.xml)|*.xml" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                List<Yuzey> yuzeyler;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    yuzeyler = YuzeyNoktalari.Oku(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Yüzey dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }

                yuzeyler.RemoveAll(y => y.Noktalar.Count == 0);
                if (yuzeyler.Count == 0)
                {
                    MessageBox.Show("Dosyada noktası olan bir yüzey (Surface) bulunamadı.", "Yüzey Oluştur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Dosyada birden fazla yüzey varsa hepsi ayrı birer obje olarak çizilir.
                YuzeyleriCizimeEkle(yuzeyler);
            }
        }

        private void yuzeyDxftenMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Üçgen Model DXF Dosyası Seçin", Filter = "DXF (*.dxf)|*.dxf" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                Yuzey yuzey;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    yuzey = YuzeyNoktalari.DxftenOku(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }

                if (yuzey.Ucgenler.Count == 0)
                {
                    MessageBox.Show(
                        "DXF dosyasında üçgen bulunamadı. Yüzey; 3DFACE, üç köşeli kapalı 3B polyline ya da polyface mesh objelerinden okunur.",
                        "Yüzey Oluştur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                YuzeyleriCizimeEkle(new[] { yuzey });
            }
        }

        private void yuzeyNoktalardanMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Nokta Dosyası Seçin", Filter = "Nokta dosyası (*.ncn;*.txt;*.csv)|*.ncn;*.txt;*.csv|Tüm dosyalar (*.*)|*.*" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                Yuzey yuzey;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    yuzey = YuzeyNoktalari.NoktaDosyasindanOlustur(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nokta dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }

                if (yuzey.Noktalar.Count < 3)
                {
                    MessageBox.Show(
                        "Dosyada üçgen oluşturmaya yetecek nokta bulunamadı. Satırlar \"nokta no, Y, X, Z\" sırasında olmalıdır.",
                        "Yüzey Oluştur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                YuzeyleriCizimeEkle(new[] { yuzey });
            }
        }

        /// <summary>
        /// Noktaları kaydedilecek yüzeyi belirler: çizimde seçili yüzey varsa o, çizimde tek yüzey varsa o; çizimde hiç
        /// yüzey yoksa bir yüzey dosyası sorulur. Vazgeçilirse ya da yüzey belirlenemezse null döner.
        /// </summary>
        private Yuzey KaydedilecekYuzeyiGetir(out string varsayilanDosyaYolu)
        {
            varsayilanDosyaYolu = null;

            var cizimdekiYuzeyler = cizimPaneli1.Nesneler.OfType<YuzeyNesnesi>().ToList();
            if (cizimdekiYuzeyler.Count > 0)
            {
                var seciliYuzeyler = cizimdekiYuzeyler.Where(y => y.Secili).ToList();
                if (seciliYuzeyler.Count == 1)
                    return YuzeyNoktalari.NesnedenAl(seciliYuzeyler[0]);
                if (seciliYuzeyler.Count == 0 && cizimdekiYuzeyler.Count == 1)
                    return YuzeyNoktalari.NesnedenAl(cizimdekiYuzeyler[0]);

                MessageBox.Show(
                    "Çizimde birden fazla yüzey var. Noktaları kaydedilecek yüzeyi (yalnızca birini) seçip komutu tekrar çalıştırın.",
                    "Yüzey", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            List<Yuzey> yuzeyler;
            using (var dlg = new OpenFileDialog { Title = "Yüzey (LandXML) Dosyası Seçin", Filter = "XML (*.xml)|*.xml" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return null;

                varsayilanDosyaYolu = dlg.FileName;
                try
                {
                    yuzeyler = YuzeyNoktalari.Oku(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Yüzey dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }

            yuzeyler.RemoveAll(y => y.Noktalar.Count == 0);
            if (yuzeyler.Count == 0)
            {
                MessageBox.Show("Dosyada noktası olan bir yüzey (Surface) bulunamadı.", "Yüzey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            return yuzeyler.Count == 1 ? yuzeyler[0] : YuzeySec(yuzeyler);
        }

        private void yuzeyNoktalariniKaydetMenuItem_Click(object sender, EventArgs e)
        {
            var yuzey = KaydedilecekYuzeyiGetir(out var yuzeyDosyaYolu);
            if (yuzey == null)
                return;

            using (var dlg = new SaveFileDialog
            {
                Title = "Yüzey Noktalarını Kaydet - " + yuzey.Ad,
                Filter = "NCN (*.ncn)|*.ncn|Excel (*.xlsx)|*.xlsx",
                FileName = yuzeyDosyaYolu != null ? Path.GetFileNameWithoutExtension(yuzeyDosyaYolu) : yuzey.Ad,
                InitialDirectory = yuzeyDosyaYolu != null ? Path.GetDirectoryName(yuzeyDosyaYolu) : null
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    if (Path.GetExtension(dlg.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                        YuzeyNoktalari.ExcelKaydet(yuzey, dlg.FileName);
                    else
                        YuzeyNoktalari.NcnKaydet(yuzey, dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show(yuzey.Noktalar.Count + " nokta kaydedildi:\n" + dlg.FileName, "Yüzey", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>Dosyada birden fazla yüzey varsa hangisinin noktalarının kaydedileceğini sorar; vazgeçilirse null döner.</summary>
        private Yuzey YuzeySec(List<Yuzey> yuzeyler)
        {
            using (var dlg = new Form
            {
                Text = "Yüzey Seçin",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(360, 280)
            })
            {
                var liste = new ListBox { Location = new System.Drawing.Point(12, 12), Size = new Size(336, 216) };
                liste.Items.AddRange(yuzeyler.ToArray());
                liste.SelectedIndex = 0;
                var tamamButton = new Button { Text = "Tamam", Location = new System.Drawing.Point(192, 242), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptalButton = new Button { Text = "İptal", Location = new System.Drawing.Point(273, 242), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                liste.DoubleClick += (s, a) => dlg.DialogResult = DialogResult.OK;

                dlg.Controls.AddRange(new Control[] { liste, tamamButton, iptalButton });
                dlg.AcceptButton = tamamButton;
                dlg.CancelButton = iptalButton;

                return dlg.ShowDialog(this) == DialogResult.OK ? liste.SelectedItem as Yuzey : null;
            }
        }

        private void kesitEditoruMenuItem_Click(object sender, EventArgs e)
        {
            var kesitEditoru = new KesitEditoruEkrani();
            kesitEditoru.Show();
        }

        private void girisEkraniMenuItem_Click(object sender, EventArgs e)
        {
            var girisEkrani = new XmlToKtbUI();
            girisEkrani.Show();
        }

        private void gorunumHassasiyetiMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new GorunumAyarlariForm(GorunumAyarlari.OndalikBasamakSayisi))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                GorunumAyarlari.OndalikBasamakSayisi = dlg.OndalikBasamakSayisi;

                // Özellikler panelindeki hücreleri yeni hassasiyetle yeniden biçimlendirmek için gridi zorla yeniler.
                var seciliNesne = ozellikGrid.SelectedObject;
                ozellikGrid.SelectedObject = null;
                ozellikGrid.SelectedObject = seciliNesne;
            }
        }

        private void dosyaAcMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "DXF Dosyası Seçin", Filter = "DXF (*.dxf)|*.dxf" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                DxfDocument dxfBelge;
                try
                {
                    dxfBelge = DxfDocument.Load(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (dxfBelge == null)
                {
                    MessageBox.Show("DXF dosyası okunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Objeler DXF'teki tabakalarına yerleştirilir; çizimde aynı adda tabaka varsa o kullanılır.
                var nesneler = DxfIceAktarici.Ayikla(dxfBelge, katman => cizimPaneli1.TabakaBulVeyaEkle(
                    katman.Name, DxfIceAktarici.RenkCevir(katman.Color), katman.IsVisible && !katman.IsFrozen, katman.IsLocked));
                cizimPaneli1.Nesneler.AddRange(nesneler);

                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }

        private void dosyaKaydetMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog { Title = "DXF Olarak Kaydet", Filter = "DXF (*.dxf)|*.dxf", FileName = "cizim.dxf" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    DxfDisaAktarici.Kaydet(cizimPaneli1.Nesneler, dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void googleEarthAktarMenuItem_Click(object sender, EventArgs e)
        {
            if (_aktifSistem == ProjeksiyonSistemi.Tanimsiz)
            {
                MessageBox.Show(
                    "Google Earth'e aktarmak için önce koordinat sistemini tanımlamanız gerekir (durum çubuğundaki 'Tanımsız' yazısına tıklayın).",
                    "Koordinat Sistemi Tanımsız", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var kml = GoogleEarthAktarici.KmlOlustur(cizimPaneli1.Nesneler, _aktifSistem, _aktifDilim);
            if (kml == null)
            {
                MessageBox.Show("Aktarılacak (koordinat sistemine göre dönüştürülebilir) obje bulunamadı.", "Google Earth'e Aktar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Title = "Google Earth Olarak Kaydet", Filter = "KMZ (*.kmz)|*.kmz|KML (*.kml)|*.kml", FileName = "cizim.kmz" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    if (Path.GetExtension(dlg.FileName).Equals(".kmz", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var dosyaAkisi = new FileStream(dlg.FileName, FileMode.Create))
                        using (var arsiv = new ZipArchive(dosyaAkisi, ZipArchiveMode.Create))
                        {
                            var girdi = arsiv.CreateEntry("doc.kml");
                            using (var yazici = new StreamWriter(girdi.Open(), new UTF8Encoding(false)))
                                yazici.Write(kml);
                        }
                    }
                    else
                    {
                        File.WriteAllText(dlg.FileName, kml, new UTF8Encoding(false));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void googleEarthIceAktarMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Google Earth Dosyası Seçin", Filter = "KML/KMZ (*.kml;*.kmz)|*.kml;*.kmz|KMZ (*.kmz)|*.kmz|KML (*.kml)|*.kml" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                List<CizimNesnesi> nesneler;
                try
                {
                    // Mevcut koordinat sisteminden bağımsız olarak, KML/KMZ'deki enlem/boylam doğrudan çizim
                    // ekranı koordinatı olarak alınır; içe aktarımdan sonra aktif sistem Coğrafi'ye ayarlanır.
                    nesneler = GoogleEarthIceAktarici.DosyadanOku(dlg.FileName, ProjeksiyonSistemi.Cografi, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (nesneler.Count == 0)
                {
                    MessageBox.Show("Dosyada aktarılabilecek bir obje bulunamadı.", "Google Earth İçe Aktar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                cizimPaneli1.Nesneler.AddRange(nesneler);
                _aktifSistem = ProjeksiyonSistemi.Cografi;
                _aktifDilim = null;
                koordinatSistemiLabel.Text = ProjeksiyonBilgisi.TamAdGetir(_aktifSistem, _aktifDilim);
                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }

        private void jsonIceAktarMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "JSON Dosyası Seçin", Filter = "JSON (*.json)|*.json" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                RootObject data;
                try
                {
                    var metin = File.ReadAllText(dlg.FileName);
                    data = JsonSerializer.Deserialize<RootObject>(metin);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("JSON dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (data?.Features == null)
                    return;

                foreach (var feature in data.Features)
                {
                    if (feature.Geometry?.Type != "Polygon" || feature.Geometry.Coordinates == null)
                        continue;

                    var koseNoktalari = new List<PointF>();
                    foreach (var halka in feature.Geometry.Coordinates.Values)
                    foreach (var koordinat in halka.Values)
                        koseNoktalari.Add(new PointF((float)koordinat.Longitude, (float)koordinat.Latitude));

                    if (koseNoktalari.Count < 2)
                        continue;

                    for (var i = 0; i < koseNoktalari.Count; i++)
                    {
                        var bas = koseNoktalari[i];
                        var bit = koseNoktalari[(i + 1) % koseNoktalari.Count];
                        cizimPaneli1.Nesneler.Add(new CizgiNesnesi
                        {
                            BaslangicX = bas.X,
                            BaslangicY = bas.Y,
                            BitisX = bit.X,
                            BitisY = bit.Y
                        });
                    }

                    var etiket = $"{feature.Properties?.AdaNo}/{feature.Properties?.ParselNo}";
                    cizimPaneli1.Nesneler.Add(new YaziNesnesi
                    {
                        KonumX = koseNoktalari.Average(p => p.X),
                        KonumY = koseNoktalari.Average(p => p.Y),
                        Metin = etiket,
                        Yukseklik = 1.5
                    });
                }

                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }
    }
}
