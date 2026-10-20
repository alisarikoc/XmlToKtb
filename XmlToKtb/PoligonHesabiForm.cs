using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Poligon hesabı (iki uçtan bağlı, dayalı poligon: başta 2, sonda 2 bilinen nokta). Açılar grad, semt kuzeyden
    /// saat yönünde; Y sağa (doğu), X yukarı (kuzey) değerdir.
    /// <list type="bullet">
    /// <item>Karne: silsile yöntemiyle ölçülen doğrultular (I. ve II. durum) ve yatay mesafeler; ortalama ve
    /// indirgenmiş doğrultular, kırılma açıları ve ortalama kenarlar sağ kolonlarda hesaplanır.</item>
    /// <item>Hesap: Hesaba Aktar ile karneden gelen kırılma açıları ve kenarlarla poligon çözülür; açı kapanması
    /// açılara eşit, koordinat kapanmaları kenar boylarıyla orantılı dağıtılır. Aktarımdan sonra karnedeki her
    /// değişiklik hesaba kendiliğinden yansır.</item>
    /// <item>Koordinatlar: bilinen (gerçek, kalın yazılı) noktalar elle ya da çizimden; hesaplanan noktalar normal
    /// yazıyla buraya gelir.</item>
    /// </list>
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class PoligonHesabiForm : Form
    {
        // Karne kolonları
        private const int KDurulan = 0, KBakilan = 1, KDurum1 = 2, KDurum2 = 3, KMesafe1 = 4, KMesafe2 = 5, KOrtalama = 6, KIndirgenmis = 7, KKirilma = 8, KKenar = 9;
        // Hesap kolonları (Excel "Dayalı Poligon Hesabı" düzeni). Her nokta için iki satır vardır: 2k. satır k. nokta
        // (β, kenar düzeltmeleri vY/vX, koordinatlar), 2k+1. satır k. noktadan sonrakine giden kenar (açı düzeltmesi,
        // semt, kenar, Δy, Δx). Son noktanın altında toplam ve kapanma satırları bulunur.
        private const int HNokta = 0, HAci = 1, HSemt = 2, HKenar = 3, HDy = 4, HDx = 5, HY = 6, HX = 7, HNokta2 = 8;
        private static readonly Color RenkHesap = Color.FromArgb(238, 244, 250), RenkBilinen = Color.FromArgb(198, 239, 206), RenkToplam = Color.FromArgb(255, 242, 204);
        // Koordinat kolonları
        private const int CNo = 0, CY = 1, CX = 2, CDurum = 3;
        private const string Gercek = "Gerçek", Hesaplanan = "Hesaplanan";

        private readonly CizimPaneli _panel;
        private readonly TabControl _sekmeler = new TabControl { Dock = DockStyle.Fill };
        private readonly DataGridView _karne = Tablo();
        private readonly DataGridView _hesap = Tablo();
        private readonly DataGridView _koordinatlar = Tablo();
        private readonly DataGridView _kontrol = Tablo();
        private readonly TabPage _kontrolSayfa = new TabPage("Kontrol");
        private readonly Label _kontrolDurum = new Label { Dock = DockStyle.Bottom, Height = 76, Padding = new Padding(6, 4, 0, 0), ForeColor = Color.DarkBlue };
        private readonly Label _hesapOzet = new Label { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(6), BackColor = Color.FromArgb(245, 245, 235) };
        // Hesap sayfasının en altında BÖHHBÜY tecvizleri (hata sınırları) ve hesaplanan hatalar.
        private readonly DataGridView _tecvizTablosu = new DataGridView
        {
            Dock = DockStyle.Bottom,
            Height = 140,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Control
        };
        private string _dosyaYolu;
        private readonly Label _karneDurum = new Label { Dock = DockStyle.Bottom, Height = 22, Padding = new Padding(6, 4, 0, 0), ForeColor = Color.DarkBlue };
        private readonly Label _koordinatDurum = new Label { Dock = DockStyle.Bottom, Height = 22, Padding = new Padding(6, 4, 0, 0), ForeColor = Color.DarkBlue };
        private bool _guncelleniyor;
        private bool _hesabaBagli;
        private bool _ekrandanAliniyor;
        private Font _kalinYazi;
        private Font _kucukYazi;
        private int _hesapNoktaSayisi;
        private readonly DateTimePicker _tarih = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy", Width = 110 };
        // Hesaplanan ölçek: bilinen B–C uzunluğu / ölçülen kenarlardan çıkan uzunluk (girilen ölçek uygulanmadan).
        // Girilen ölçek: hesapta bütün kenarlar bununla çarpılır.
        private readonly TextBox _hesaplananOlcek = new TextBox { Width = 105, ReadOnly = true, TextAlign = HorizontalAlignment.Right };
        private readonly TextBox _girilenOlcek = new TextBox { Width = 105, Text = "1", TextAlign = HorizontalAlignment.Right };
        // Dayalı poligon: başta 2, sonda 2 bilinen nokta. Açık poligon: yalnızca başta 2 bilinen nokta; karnedeki
        // bütün istasyonlar ileriye doğru, kapanma ve düzeltme olmadan hesaplanır.
        private readonly ComboBox _poligonTuru = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        private readonly Label _baslikYazi = new Label { Text = "DAYALI POLİGON HESABI", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        private bool AcikPoligon => _poligonTuru.SelectedIndex == 1;

        // Karneden çıkan istasyon bilgileri (Hesaba Aktar bunları kullanır).
        private class Istasyon
        {
            public string Durulan, Geri, Ileri;
            public double? KirilmaAcisi;
        }
        private List<Istasyon> _istasyonlar = new List<Istasyon>();
        private readonly Dictionary<string, double> _kenarlar = new Dictionary<string, double>();

        private static DataGridView Tablo()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToResizeRows = false,
                RowHeadersWidth = 30,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText
            };
        }

        public PoligonHesabiForm(CizimPaneli panel)
        {
            _panel = panel;
            Text = "Poligon Hesabı";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1150, 560);
            MinimumSize = new Size(800, 400);
            ShowInTaskbar = false;
            _kalinYazi = new Font(Font, FontStyle.Bold);
            _kucukYazi = new Font(Font.FontFamily, Font.Size - 1f);

            // --- Karne ---
            KolonEkle(_karne, "Durulan Nokta", false, 9);
            KolonEkle(_karne, "Bakılan Nokta", false, 9);
            KolonEkle(_karne, "I. Durum (g)", false, 10);
            KolonEkle(_karne, "II. Durum (g)", false, 10);
            KolonEkle(_karne, "Yatay Mesafe I. Durum (m)", false, 10);
            KolonEkle(_karne, "Yatay Mesafe II. Durum (m)", false, 10);
            KolonEkle(_karne, "Ortalama Doğrultu (g)", true, 11);
            KolonEkle(_karne, "İndirgenmiş Doğrultu (g)", true, 11);
            KolonEkle(_karne, "Kırılma Açısı (g)", true, 10);
            KolonEkle(_karne, "Ortalama Kenar (m)", true, 10);
            var karneSayfa = new TabPage("Karne");
            var karneArac = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            karneArac.Items.Add(new ToolStripButton("Hesaba Aktar", null, (s, e) => HesabaAktar(true)) { ToolTipText = "Kırılma açılarını ve kenarları hesap sayfasına aktarır; sonraki değişiklikler kendiliğinden yansır" });
            karneArac.Items.Add(new ToolStripSeparator());
            karneArac.Items.Add(new ToolStripButton("Yapıştır (Ctrl+V)", null, (s, e) => Yapistir(_karne)) { ToolTipText = "Excel'den kopyalanan hücreleri seçili hücreden başlayarak yapıştırır" });
            karneArac.Items.Add(new ToolStripButton("Satır Sil", null, (s, e) => SatirlariSil(_karne)));
            karneArac.Items.Add(new ToolStripButton("Temizle", null, (s, e) => { _karne.Rows.Clear(); KarneHesapla(); }));
            karneArac.Items.Add(new ToolStripSeparator());
            karneArac.Items.Add(new ToolStripLabel("Durulan nokta yalnızca ilk satıra yazılabilir; her durulan noktada ilk bakılan nokta geri, ikincisi ileri sayılır. Aynı istasyonda tekrar eden satırlar ayrı silsiledir.") { ForeColor = SystemColors.GrayText });
            karneSayfa.Controls.Add(_karne);
            karneSayfa.Controls.Add(_karneDurum);
            karneSayfa.Controls.Add(karneArac);

            // --- Hesap ---
            // Hücrelerin salt okunurluğu ve renkleri satır satır HesapTablosunuKur'da verilir.
            KolonEkle(_hesap, "Nokta No", false, 8);
            KolonEkle(_hesap, "Poligon Açısı β (g)", false, 10);
            KolonEkle(_hesap, "Semt α (g)", false, 10);
            KolonEkle(_hesap, "Kenar S (m)", false, 9);
            KolonEkle(_hesap, "S·Sinα  Δy (m)", false, 10);
            KolonEkle(_hesap, "S·Cosα  Δx (m)", false, 10);
            KolonEkle(_hesap, "Y (m)", false, 12);
            KolonEkle(_hesap, "X (m)", false, 12);
            KolonEkle(_hesap, "Nokta No", false, 8);
            _hesap.AllowUserToAddRows = false;
            _hesap.AllowUserToDeleteRows = false;
            _hesap.RowHeadersVisible = false;
            _hesap.EnableHeadersVisualStyles = false;
            _hesap.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(221, 235, 247);
            _hesap.ColumnHeadersDefaultCellStyle.Font = _kalinYazi;
            _hesap.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _hesap.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _hesap.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _hesap.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _hesap.Columns[HNokta].DefaultCellStyle.Alignment = _hesap.Columns[HNokta2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            var baslikPanel = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.White };
            var tarihPanel = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 340, Padding = new Padding(0, 5, 6, 0), WrapContents = false };
            tarihPanel.Controls.Add(new Label { Text = "Tür:", AutoSize = true, Margin = new Padding(0, 4, 4, 0) });
            tarihPanel.Controls.Add(_poligonTuru);
            tarihPanel.Controls.Add(new Label { Text = "Tarih:", AutoSize = true, Margin = new Padding(10, 4, 4, 0) });
            tarihPanel.Controls.Add(_tarih);
            var olcekPanel = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 430, Padding = new Padding(6, 5, 0, 0), WrapContents = false };
            olcekPanel.Controls.Add(new Label { Text = "Hesaplanan Ölçek:", AutoSize = true, Margin = new Padding(0, 4, 4, 0) });
            olcekPanel.Controls.Add(_hesaplananOlcek);
            olcekPanel.Controls.Add(new Label { Text = "Girilen Ölçek:", AutoSize = true, Margin = new Padding(10, 4, 4, 0) });
            olcekPanel.Controls.Add(_girilenOlcek);
            _baslikYazi.Font = new Font(Font.FontFamily, 13f, FontStyle.Bold);
            baslikPanel.Controls.Add(_baslikYazi);
            _poligonTuru.Items.AddRange(new object[] { "Dayalı poligon", "Açık poligon" });
            _poligonTuru.SelectedIndex = 0;
            _poligonTuru.SelectedIndexChanged += (s, e) => PoligonTuruDegisti();
            baslikPanel.Controls.Add(tarihPanel);
            baslikPanel.Controls.Add(olcekPanel);
            new ToolTip().SetToolTip(_girilenOlcek, "Hesaptaki bütün kenarlar bu ölçekle çarpılır (varsayılan 1). Hesaplanan ölçeği kopyalayıp buraya yapıştırabilirsiniz.");
            _girilenOlcek.TextChanged += (s, e) => { if (!_guncelleniyor) PoligonHesapla(); };
            var hesapSayfa = new TabPage("Hesap");
            var hesapArac = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            hesapArac.Items.Add(new ToolStripButton("Yeni", null, (s, e) => YeniHesap()) { ToolTipText = "Bütün tabloları boşaltıp yeni bir poligon hesabı başlatır (kaydedilmemiş değişiklik varsa sorar)" });
            hesapArac.Items.Add(new ToolStripButton("Aç...", null, (s, e) => DosyaAc()) { ToolTipText = "Kaydedilmiş poligon hesabını (.pol) bütün sayfalarıyla açar" });
            hesapArac.Items.Add(new ToolStripButton("Kaydet...", null, (s, e) => DosyaKaydet()) { ToolTipText = "Karne, koordinatlar ve hesabın tamamını .pol dosyasına kaydeder" });
            hesapArac.Items.Add(new ToolStripSeparator());
            hesapArac.Items.Add(new ToolStripButton("Hesapla", null, (s, e) => PoligonHesapla()));
            hesapArac.Items.Add(new ToolStripButton("Tabloyu Kopyala", null, (s, e) => TabloyuKopyala(_hesap)) { ToolTipText = "Hesap tablosunu Excel'e yapıştırmak için panoya kopyalar" });
            hesapArac.Items.Add(new ToolStripSeparator());
            hesapArac.Items.Add(new ToolStripLabel("Bilinen noktalar (yeşil): dayalıda ilk iki ve son iki, açıkta ilk iki nokta. β ve kenarlar beyaz hücrelerde düzeltilebilir; kenar iki noktanın arasındaki satırdadır.") { ForeColor = SystemColors.GrayText });
            foreach (var baslik in new[] { "Kontrol (BÖHHBÜY Madde 27)", "Hesaplanan Hata", "Tecviz (Sınır)", "Sonuç" })
                _tecvizTablosu.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = baslik, SortMode = DataGridViewColumnSortMode.NotSortable });
            _tecvizTablosu.Columns[0].FillWeight = 160;
            // Satırlar seçilince renk değiştirmesin (tablo yalnızca bilgi verir).
            _tecvizTablosu.DefaultCellStyle.SelectionBackColor = _tecvizTablosu.DefaultCellStyle.BackColor.IsEmpty ? SystemColors.Window : _tecvizTablosu.DefaultCellStyle.BackColor;
            _tecvizTablosu.DefaultCellStyle.SelectionForeColor = SystemColors.ControlText;
            hesapSayfa.Controls.Add(_hesap);
            hesapSayfa.Controls.Add(_hesapOzet);
            hesapSayfa.Controls.Add(_tecvizTablosu);
            hesapSayfa.Controls.Add(baslikPanel);
            hesapSayfa.Controls.Add(hesapArac);

            // --- Koordinatlar ---
            KolonEkle(_koordinatlar, "Nokta No", false, 10);
            KolonEkle(_koordinatlar, "Y (sağa) (m)", false, 12);
            KolonEkle(_koordinatlar, "X (yukarı) (m)", false, 12);
            KolonEkle(_koordinatlar, "Durum", true, 8);
            var koorSayfa = new TabPage("Koordinatlar");
            var koorArac = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            koorArac.Items.Add(new ToolStripButton("Kaydet (NCN)...", null, (s, e) => NcnKaydet()) { ToolTipText = "Koordinatları NCN dosyasına kaydeder (Z = 0)" });
            koorArac.Items.Add(new ToolStripSeparator());
            koorArac.Items.Add(new ToolStripButton("Ekrandan Al", null, (s, e) => EkrandanAlBaslat()) { ToolTipText = "Çizimdeki noktaları tıklayarak gerçek koordinat olarak alır" });
            koorArac.Items.Add(new ToolStripButton("Yapıştır (Ctrl+V)", null, (s, e) => Yapistir(_koordinatlar)));
            koorArac.Items.Add(new ToolStripButton("Satır Sil", null, (s, e) => SatirlariSil(_koordinatlar)));
            koorArac.Items.Add(new ToolStripSeparator());
            koorArac.Items.Add(new ToolStripButton("Noktaları Çizime Aktar", null, (s, e) => CizimeAktar()) { ToolTipText = "Bütün noktaları çizime aktarır; sabit (bilinen) noktalar kırmızı gösterilir" });
            koorArac.Items.Add(new ToolStripButton("Tabloyu Kopyala", null, (s, e) => TabloyuKopyala(_koordinatlar)));
            koorSayfa.Controls.Add(_koordinatlar);
            koorSayfa.Controls.Add(_koordinatDurum);
            koorSayfa.Controls.Add(koorArac);
            _koordinatDurum.Text = "Gerçek (bilinen) koordinatlar kalın, hesaplananlar normal yazılır. Elle girilen ya da ekrandan alınan noktalar gerçek sayılır.";

            _sekmeler.TabPages.Add(hesapSayfa);
            _sekmeler.TabPages.Add(SilsileSayfasiniOlustur());
            _sekmeler.TabPages.Add(karneSayfa);
            _sekmeler.TabPages.Add(koorSayfa);

            // --- Kontrol ---
            KolonEkle(_kontrol, "Nokta No", true, 9);
            KolonEkle(_kontrol, "dY (m)", true, 7);
            KolonEkle(_kontrol, "dX (m)", true, 7);
            KolonEkle(_kontrol, "dS (m)", true, 7);
            KolonEkle(_kontrol, "Tahmini Açı (g)", true, 9);
            KolonEkle(_kontrol, "Ölçülen Açı (g)", true, 9);
            KolonEkle(_kontrol, "Açı Hatası (cc)", true, 7);
            KolonEkle(_kontrol, "Düzeltilirse Enine fq (m)", true, 8);
            KolonEkle(_kontrol, "Düzeltilirse Boyuna fl (m)", true, 8);
            KolonEkle(_kontrol, "Düzeltilirse fs (m)", true, 8);
            KolonEkle(_kontrol, "Y Baştan (m)", true, 11);
            KolonEkle(_kontrol, "X Baştan (m)", true, 11);
            KolonEkle(_kontrol, "Y Sondan (m)", true, 11);
            KolonEkle(_kontrol, "X Sondan (m)", true, 11);
            _kontrol.AllowUserToAddRows = false;
            _kontrol.AllowUserToDeleteRows = false;
            _kontrol.ReadOnly = true;
            _kontrol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _kontrol.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            var kontrolArac = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            kontrolArac.Items.Add(new ToolStripButton("Tabloyu Kopyala", null, (s, e) => TabloyuKopyala(_kontrol)));
            kontrolArac.Items.Add(new ToolStripSeparator());
            kontrolArac.Items.Add(new ToolStripLabel("Her hesaplanan nokta, ölçülen açı ve kenarlarla (düzeltmesiz) baştaki iki noktadan ileriye ve sondaki iki noktadan geriye açık poligon olarak hesaplanır.") { ForeColor = SystemColors.GrayText });
            _kontrolSayfa.Controls.Add(_kontrol);
            _kontrolSayfa.Controls.Add(_kontrolDurum);
            _kontrolSayfa.Controls.Add(kontrolArac);
            _sekmeler.TabPages.Add(_kontrolSayfa);
            _sekmeler.Selecting += (s, e) =>
            {
                if (e.TabPage == _kontrolSayfa && !_kontrolSayfa.Enabled)
                {
                    e.Cancel = true;
                    MessageBox.Show(this, "Kontrol, dayalı poligon hesabı yapıldıktan sonra açılır.", "Kontrol", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            KontrolTemizle();
            Controls.Add(_sekmeler);

            foreach (var tablo in new[] { _karne, _hesap, _koordinatlar, _silsileHam })
            {
                tablo.KeyDown += (s, e) =>
                {
                    if (e.Control && e.KeyCode == Keys.V)
                    {
                        Yapistir(tablo);
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Delete && !tablo.IsCurrentCellInEditMode)
                    {
                        foreach (DataGridViewCell hucre in tablo.SelectedCells)
                            if (!hucre.ReadOnly) hucre.Value = null;
                        e.Handled = true;
                    }
                };
                tablo.DataError += (s, e) => e.ThrowException = false;
            }
            _karne.CellValueChanged += (s, e) => { if (!_guncelleniyor) KarneDegisti(); };
            _karne.RowsRemoved += (s, e) => { if (!_guncelleniyor) KarneDegisti(); };
            _hesap.CellValueChanged += (s, e) => { if (!_guncelleniyor) PoligonHesapla(); };
            _koordinatlar.RowsAdded += (s, e) =>
            {
                if (_guncelleniyor)
                    return;
                for (var i = e.RowIndex; i < e.RowIndex + e.RowCount && i < _koordinatlar.Rows.Count; i++)
                {
                    var satir = _koordinatlar.Rows[i];
                    if (!satir.IsNewRow && Metin(satir, CNo).Length > 0 && Metin(satir, CDurum).Length == 0)
                        GercekYap(satir);
                }
            };
            _koordinatlar.CellValueChanged += (s, e) =>
            {
                if (_guncelleniyor || e.RowIndex < 0)
                    return;
                // Elle girilen/düzeltilen satır gerçek koordinattır.
                if (e.ColumnIndex != CDurum)
                    GercekYap(_koordinatlar.Rows[e.RowIndex]);
                PoligonHesapla();
            };

            _panel.NoktaTiklandi += Panel_NoktaTiklandi;
            _panel.NoktaAlmaBitti += Panel_NoktaAlmaBitti;
            FormClosed += (s, e) =>
            {
                _panel.NoktaTiklandi -= Panel_NoktaTiklandi;
                _panel.NoktaAlmaBitti -= Panel_NoktaAlmaBitti;
                if (_ekrandanAliniyor && _panel.AktifArac == CizimAraci.NoktaAl)
                    _panel.AktifArac = CizimAraci.Sec;
            };

            KarneHesapla();
            _hesapOzet.Text = "Karne sayfasında ölçüleri girin (ya da Excel'den yapıştırın) ve Hesaba Aktar'a basın. Bilinen 4 noktanın koordinatlarını Koordinatlar sayfasına girin.";
            _kayitliMetin = PolMetni();
        }

        private static void KolonEkle(DataGridView tablo, string baslik, bool hesaplanan, float agirlik)
        {
            var kolon = new DataGridViewTextBoxColumn { HeaderText = baslik, ReadOnly = hesaplanan, FillWeight = agirlik, SortMode = DataGridViewColumnSortMode.NotSortable };
            if (hesaplanan)
                kolon.DefaultCellStyle.BackColor = Color.FromArgb(238, 244, 250);
            tablo.Columns.Add(kolon);
        }

        // ------------------------------------------------------------------ yardımcılar

        private static string Metin(DataGridViewRow satir, int kolon) => Convert.ToString(satir.Cells[kolon].Value)?.Trim() ?? "";

        private static double? Sayi(DataGridViewRow satir, int kolon) => NcnDosyasi.SayiOku(Metin(satir, kolon), out var deger) && Metin(satir, kolon).Length > 0 ? deger : (double?)null;

        private static string G(double grad) => grad.ToString("0.0000", CultureInfo.InvariantCulture);
        private static string M(double metre) => metre.ToString("0.000", CultureInfo.InvariantCulture);

        private static double Mod400(double grad)
        {
            grad %= 400;
            return grad < 0 ? grad + 400 : grad;
        }

        // (-200, 200] aralığına getirir.
        private static double Fark(double grad)
        {
            grad = Mod400(grad);
            return grad > 200 ? grad - 400 : grad;
        }

        /// <summary>Semt (grad): A'dan B'ye, kuzeyden saat yönünde. Y sağa, X yukarıdır.</summary>
        public static double Semt(double ya, double xa, double yb, double xb)
        {
            return Mod400(Math.Atan2(yb - ya, xb - xa) * 200 / Math.PI);
        }

        private static string KenarAnahtari(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + "\u0001" + b : b + "\u0001" + a;

        private void Yapistir(DataGridView tablo)
        {
            var metin = Clipboard.GetText();
            if (string.IsNullOrEmpty(metin))
                return;
            var satirlar = metin.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            var basSatir = tablo.CurrentCell?.RowIndex ?? 0;
            var basKolon = tablo.CurrentCell?.ColumnIndex ?? 0;
            _guncelleniyor = true;
            try
            {
                for (var i = 0; i < satirlar.Length; i++)
                {
                    var satirNo = basSatir + i;
                    // Hesap tablosunun satır düzeni sabittir; yapıştırma yeni satır açmaz.
                    if (tablo == _hesap && satirNo >= tablo.Rows.Count)
                        break;
                    while (satirNo >= tablo.Rows.Count - (tablo.AllowUserToAddRows ? 1 : 0))
                        tablo.Rows.Add();
                    var hucreler = satirlar[i].Split('\t');
                    for (var k = 0; k < hucreler.Length && basKolon + k < tablo.Columns.Count; k++)
                    {
                        var hucre = tablo.Rows[satirNo].Cells[basKolon + k];
                        if (!hucre.ReadOnly)
                            hucre.Value = hucreler[k].Trim();
                    }
                    if (tablo == _koordinatlar)
                        GercekYap(tablo.Rows[satirNo]);
                }
            }
            finally
            {
                _guncelleniyor = false;
            }
            TabloDegisti(tablo);
        }

        private void SatirlariSil(DataGridView tablo)
        {
            var satirlar = tablo.SelectedCells.Cast<DataGridViewCell>().Select(c => c.RowIndex).Distinct().OrderByDescending(i => i).ToList();
            foreach (var i in satirlar)
            {
                if (i < tablo.Rows.Count && !tablo.Rows[i].IsNewRow)
                    tablo.Rows.RemoveAt(i);
            }
            TabloDegisti(tablo);
        }

        private void TabloDegisti(DataGridView tablo)
        {
            if (tablo == _karne) KarneDegisti();
            else if (tablo == _silsileHam) SilsileHesapla();
            else PoligonHesapla();
        }

        private static void TabloyuKopyala(DataGridView tablo)
        {
            var satirlar = new List<string> { string.Join("\t", tablo.Columns.Cast<DataGridViewColumn>().Select(k => k.HeaderText)) };
            foreach (DataGridViewRow satir in tablo.Rows)
            {
                if (!satir.IsNewRow)
                    satirlar.Add(string.Join("\t", satir.Cells.Cast<DataGridViewCell>().Select(c => Convert.ToString(c.Value))));
            }
            Clipboard.SetText(string.Join("\r\n", satirlar));
        }

        // ------------------------------------------------------------------ karne

        private void KarneDegisti()
        {
            KarneHesapla();
            if (_hesabaBagli)
                HesabaAktar(false);
        }

        /// <summary>
        /// Karnenin sağ kolonlarını hesaplar: ortalama doğrultu ((I + II ± 200) / 2), istasyondaki ilk bakılan noktaya
        /// indirgenmiş doğrultu, kırılma açısı (ikinci bakılan noktanın silsilelerdeki indirgenmiş doğrultularının
        /// ortalaması) ve aynı kenarın gidiş-dönüş ölçülerinin ortalaması.
        /// </summary>
        private void KarneHesapla()
        {
            _guncelleniyor = true;
            try
            {
                _istasyonlar = new List<Istasyon>();
                _kenarlar.Clear();
                var mesafeler = new Dictionary<string, List<double>>();
                var kenarIlkSatir = new Dictionary<string, int>();

                // İstasyon blokları: durulan nokta yazılmayan satır bir öncekinin istasyonudur.
                string durulan = null;
                var bloklar = new List<List<int>>();
                var satirDurulan = new string[_karne.Rows.Count];
                for (var i = 0; i < _karne.Rows.Count; i++)
                {
                    var satir = _karne.Rows[i];
                    if (satir.IsNewRow)
                        continue;
                    foreach (var k in new[] { KOrtalama, KIndirgenmis, KKirilma, KKenar })
                        satir.Cells[k].Value = null;
                    var yazilan = Metin(satir, KDurulan);
                    if (Metin(satir, KBakilan).Length == 0)
                        continue;
                    var yeniBlok = yazilan.Length > 0 && yazilan != durulan;
                    if (yazilan.Length > 0)
                        durulan = yazilan;
                    if (durulan == null)
                        continue;
                    if (yeniBlok || bloklar.Count == 0)
                        bloklar.Add(new List<int>());
                    bloklar[bloklar.Count - 1].Add(i);
                    satirDurulan[i] = durulan;
                }

                foreach (var blok in bloklar)
                {
                    var istasyon = new Istasyon { Durulan = satirDurulan[blok[0]], Geri = Metin(_karne.Rows[blok[0]], KBakilan) };
                    double? seriBasi = null;
                    var kirilmalar = new List<double>();
                    var ilkIleriSatiri = -1;
                    foreach (var i in blok)
                    {
                        var satir = _karne.Rows[i];
                        var bakilan = Metin(satir, KBakilan);
                        var d1 = Sayi(satir, KDurum1);
                        var d2 = Sayi(satir, KDurum2);
                        double? ortalama = null;
                        if (d1 != null && d2 != null)
                        {
                            // II. durum 200 g farklıdır; iki okuma 0/400 sınırının iki yanındaysa yaklaştırılır.
                            var ikinci = Mod400(d2.Value - 200);
                            if (ikinci - d1.Value > 200) ikinci -= 400;
                            if (d1.Value - ikinci > 200) ikinci += 400;
                            ortalama = Mod400((d1.Value + ikinci) / 2);
                        }
                        else if (d1 != null) ortalama = d1;
                        else if (d2 != null) ortalama = Mod400(d2.Value - 200);

                        if (ortalama != null)
                        {
                            satir.Cells[KOrtalama].Value = G(ortalama.Value);
                            // Geri noktaya bakış yeni bir silsile başlatır; doğrultular ona indirgenir.
                            if (bakilan == istasyon.Geri)
                                seriBasi = ortalama;
                            if (seriBasi != null)
                            {
                                var indirgenmis = Mod400(ortalama.Value - seriBasi.Value);
                                satir.Cells[KIndirgenmis].Value = G(indirgenmis);
                                if (bakilan != istasyon.Geri)
                                {
                                    if (istasyon.Ileri == null)
                                        istasyon.Ileri = bakilan;
                                    if (bakilan == istasyon.Ileri)
                                    {
                                        // Silsileler arasında 0/400 atlaması olmasın diye ilk değere yaklaştırılır.
                                        if (kirilmalar.Count > 0)
                                            indirgenmis = kirilmalar[0] + Fark(indirgenmis - kirilmalar[0]);
                                        kirilmalar.Add(indirgenmis);
                                        if (ilkIleriSatiri < 0)
                                            ilkIleriSatiri = i;
                                    }
                                }
                            }
                        }
                        else if (bakilan != istasyon.Geri && istasyon.Ileri == null)
                        {
                            istasyon.Ileri = bakilan;
                        }

                        // I. ve II. durumda okunan yatay mesafelerin her biri ayrı ölçüdür; kenar hepsinin ortalamasıdır.
                        foreach (var mesafe in new[] { Sayi(satir, KMesafe1), Sayi(satir, KMesafe2) })
                        {
                            if (mesafe == null || mesafe <= 0)
                                continue;
                            var anahtar = KenarAnahtari(istasyon.Durulan, bakilan);
                            if (!mesafeler.TryGetValue(anahtar, out var liste))
                            {
                                mesafeler[anahtar] = liste = new List<double>();
                                kenarIlkSatir[anahtar] = i;
                            }
                            liste.Add(mesafe.Value);
                        }
                    }
                    if (kirilmalar.Count > 0)
                    {
                        istasyon.KirilmaAcisi = Mod400(kirilmalar.Average());
                        _karne.Rows[ilkIleriSatiri].Cells[KKirilma].Value = G(istasyon.KirilmaAcisi.Value);
                    }
                    _istasyonlar.Add(istasyon);
                }

                foreach (var kenar in mesafeler)
                {
                    _kenarlar[kenar.Key] = kenar.Value.Average();
                    _karne.Rows[kenarIlkSatir[kenar.Key]].Cells[KKenar].Value = M(_kenarlar[kenar.Key]) + (kenar.Value.Count > 1 ? "  (" + kenar.Value.Count + " ölçü)" : "");
                }

                _karneDurum.Text = _istasyonlar.Count + " istasyon, " + _kenarlar.Count + " kenar." + (_hesabaBagli ? "  Hesaba bağlı: değişiklikler hesap sayfasına kendiliğinden aktarılıyor." : "");
            }
            finally
            {
                _guncelleniyor = false;
            }
        }

        /// <summary>
        /// Karneden poligon dizisini kurup hesap tablosuna aktarır: [geri dayanak, istasyon 1, ..., son istasyon,
        /// ileri dayanak]. Her istasyonun ileri noktası bir sonraki istasyon olmalıdır.
        /// </summary>
        private void HesabaAktar(bool kullanicidan)
        {
            KarneHesapla();
            string hata = null;
            var acik = AcikPoligon;
            if (acik && _istasyonlar.Count < 1)
                hata = "Karnede en az bir istasyon (başlangıç bilinen noktası) olmalıdır.";
            else if (!acik && _istasyonlar.Count < 2)
                hata = "Karnede en az iki istasyon (başlangıç ve bitiş bilinen noktaları) olmalıdır.";
            else
            {
                for (var k = 0; k < _istasyonlar.Count && hata == null; k++)
                {
                    var ist = _istasyonlar[k];
                    if (ist.Ileri == null)
                        hata = ist.Durulan + " istasyonunda ileri nokta (ikinci bakılan nokta) yok.";
                    else if (ist.KirilmaAcisi == null)
                        hata = ist.Durulan + " istasyonunda kırılma açısı hesaplanamadı (doğrultu okumalarını kontrol edin).";
                    else if (k + 1 < _istasyonlar.Count && _istasyonlar[k + 1].Durulan != ist.Ileri)
                        hata = ist.Durulan + " istasyonunun ileri noktası (" + ist.Ileri + ") sonraki istasyon (" + _istasyonlar[k + 1].Durulan + ") değil. İstasyonlar poligon sırasıyla yazılmalıdır.";
                    else if ((acik || k + 1 < _istasyonlar.Count) && !_kenarlar.ContainsKey(KenarAnahtari(ist.Durulan, ist.Ileri)))
                        hata = ist.Durulan + " - " + ist.Ileri + " kenarının mesafesi karnede yok.";
                }
            }
            if (hata != null)
            {
                if (kullanicidan)
                    MessageBox.Show(hata, "Hesaba Aktar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    _hesapOzet.Text = "Karne hesaba aktarılamadı: " + hata;
                return;
            }

            var noktalar = new List<string[]> { new[] { _istasyonlar[0].Geri, "", "" } };
            for (var k = 0; k < _istasyonlar.Count; k++)
            {
                var ist = _istasyonlar[k];
                noktalar.Add(new[] { ist.Durulan, G(ist.KirilmaAcisi.Value), acik || k + 1 < _istasyonlar.Count ? M(_kenarlar[KenarAnahtari(ist.Durulan, ist.Ileri)]) : "" });
            }
            noktalar.Add(new[] { _istasyonlar[_istasyonlar.Count - 1].Ileri, "", "" });
            HesapTablosunuKur(noktalar);

            _hesabaBagli = true;
            KarneHesapla();
            PoligonHesapla();
            if (kullanicidan)
                _sekmeler.SelectedIndex = 0;
        }

        // ------------------------------------------------------------------ hesap

        /// <summary>
        /// Hesap tablosunu Excel düzeninde kurar. Her eleman {nokta no, kırılma açısı, sonraki noktaya kenar}'dır;
        /// ilk ve son noktanın açısı, ilk, sondan bir önceki ve son noktanın kenarı kullanılmaz.
        /// </summary>
        private void HesapTablosunuKur(IList<string[]> noktalar)
        {
            var onceki = _guncelleniyor;
            _guncelleniyor = true;
            try
            {
                _hesap.Rows.Clear();
                var n = _hesapNoktaSayisi = noktalar.Count;
                for (var k = 0; k < n; k++)
                {
                    var bilinen = k < 2 || (!AcikPoligon && k >= n - 2);
                    var satir = _hesap.Rows[_hesap.Rows.Add()];
                    foreach (DataGridViewCell hucre in satir.Cells)
                    {
                        hucre.ReadOnly = true;
                        hucre.Style.BackColor = RenkHesap;
                    }
                    var no = satir.Cells[HNokta];
                    no.Value = noktalar[k][0];
                    no.ReadOnly = false;
                    no.Style.BackColor = Color.White;
                    if (bilinen)
                    {
                        no.Style.Font = satir.Cells[HNokta2].Style.Font = _kalinYazi;
                        foreach (var kolon in new[] { HY, HX })
                        {
                            satir.Cells[kolon].Style.BackColor = RenkBilinen;
                            satir.Cells[kolon].Style.Font = _kalinYazi;
                        }
                    }
                    if (k >= 1 && k <= n - 2)
                    {
                        var beta = satir.Cells[HAci];
                        beta.Value = noktalar[k][1];
                        beta.ReadOnly = false;
                        beta.Style.BackColor = Color.White;
                        beta.Style.ForeColor = Color.Red;
                    }
                    // Nokta satırında Δy/Δx kolonları alttaki kenarın koordinat düzeltmeleridir (mm).
                    foreach (var kolon in new[] { HDy, HDx })
                    {
                        satir.Cells[kolon].Style.ForeColor = Color.Red;
                        satir.Cells[kolon].Style.Font = _kucukYazi;
                    }
                    if (k == n - 1)
                        break;

                    var ara = _hesap.Rows[_hesap.Rows.Add()];
                    foreach (DataGridViewCell hucre in ara.Cells)
                    {
                        hucre.ReadOnly = true;
                        hucre.Style.BackColor = RenkHesap;
                    }
                    // Ara satırın açı kolonu üstteki noktanın açı düzeltmesidir (cc).
                    ara.Cells[HAci].Style.ForeColor = Color.Red;
                    ara.Cells[HAci].Style.Font = _kucukYazi;
                    if (k >= 1 && k <= SonKenarIndisi(n))
                    {
                        var kenar = ara.Cells[HKenar];
                        kenar.Value = noktalar[k][2];
                        kenar.ReadOnly = false;
                        kenar.Style.BackColor = Color.White;
                    }
                }
            }
            finally
            {
                _guncelleniyor = onceki;
            }
        }

        // Ölçülen son kenarın başlangıç noktası: dayalıda son kenar (C→D) bilinen iki nokta arasındadır, ölçülmez.
        private int SonKenarIndisi(int noktaSayisi) => AcikPoligon ? noktaSayisi - 2 : noktaSayisi - 3;

        private void PoligonTuruDegisti()
        {
            _baslikYazi.Text = AcikPoligon ? "AÇIK POLİGON HESABI" : "DAYALI POLİGON HESABI";
            _hesaplananOlcek.Enabled = !AcikPoligon;
            _tecvizTablosu.Visible = !AcikPoligon;
            // Kontrol yalnızca dayalı poligon içindir; açık poligona geçilirse (aktarma başarısız olsa bile) kapanır.
            if (AcikPoligon)
                KontrolTemizle();
            if (_guncelleniyor)
                return;
            // Bilinen noktalar ve ölçülen kenarlar türe göre değiştiği için tablo yeniden kurulur.
            if (_hesabaBagli)
                HesabaAktar(false);
            else
            {
                HesapTablosunuKur(HesapGirdileri());
                PoligonHesapla();
            }
        }

        /// <summary>Hesap tablosundaki girdiler: {nokta no, kırılma açısı, sonraki noktaya kenar}.</summary>
        private List<string[]> HesapGirdileri()
        {
            var n = _hesapNoktaSayisi;
            var liste = new List<string[]>();
            for (var k = 0; k < n && 2 * k < _hesap.Rows.Count; k++)
            {
                var satir = _hesap.Rows[2 * k];
                var beta = k >= 1 && k <= n - 2 ? Metin(satir, HAci) : "";
                var kenar = k >= 1 && k <= SonKenarIndisi(n) && 2 * k + 1 < _hesap.Rows.Count ? Metin(_hesap.Rows[2 * k + 1], HKenar) : "";
                liste.Add(new[] { Metin(satir, HNokta), beta, kenar });
            }
            return liste;
        }

        /// <summary>
        /// Tam sayı birimlerle verilen kapanmayı ağırlıklarla orantılı dağıtır (en büyük kalan yöntemi): her pay aşağı
        /// yuvarlanır, eksik kalan birimler kalanı en büyük olanlara birer birer verilir. Payların toplamı kapanmaya eşittir.
        /// </summary>
        private static long[] TamDagit(long toplam, double[] agirliklar)
        {
            var paylar = new long[agirliklar.Length];
            var agirlikToplami = agirliklar.Sum();
            if (paylar.Length == 0 || agirlikToplami <= 0)
                return paylar;
            var isaret = Math.Sign(toplam);
            var mutlak = Math.Abs(toplam);
            var kalanlar = new double[paylar.Length];
            for (var i = 0; i < paylar.Length; i++)
            {
                var tam = mutlak * agirliklar[i] / agirlikToplami;
                paylar[i] = (long)Math.Floor(tam);
                kalanlar[i] = tam - paylar[i];
            }
            var eksik = mutlak - paylar.Sum();
            foreach (var i in Enumerable.Range(0, paylar.Length).OrderByDescending(k => kalanlar[k]).ThenByDescending(k => agirliklar[k]).Take((int)eksik))
                paylar[i]++;
            for (var i = 0; i < paylar.Length; i++)
                paylar[i] *= isaret;
            return paylar;
        }

        private static string Cc(double grad) => (grad * 10000).ToString("+0.0;-0.0;0", CultureInfo.InvariantCulture);
        private static string Mm(double metre) => (metre * 1000).ToString("+0;-0;0", CultureInfo.InvariantCulture);

        private bool KoordinatBul(string no, out double y, out double x)
        {
            y = x = 0;
            foreach (DataGridViewRow satir in _koordinatlar.Rows)
            {
                // Hesaplanan olarak işaretlenmemiş her satır gerçek (bilinen) koordinattır.
                if (satir.IsNewRow || Metin(satir, CNo) != no || Metin(satir, CDurum) == Hesaplanan)
                    continue;
                var ys = Sayi(satir, CY);
                var xs = Sayi(satir, CX);
                if (ys == null || xs == null)
                    return false;
                y = ys.Value;
                x = xs.Value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Dayalı poligonu çözer. Semtler t(i) = t(i-1) + β(i) − 200 ile taşınır; açı kapanması (bilinen bitiş semti
        /// − taşınan semt) bütün kırılma açılarına eşit dağıtılır. Koordinat farklarının kapanmaları kenar boylarıyla
        /// orantılı dağıtılır. Yeni noktaların koordinatları Koordinatlar sayfasına aktarılır.
        /// </summary>
        private void PoligonHesapla()
        {
            if (_guncelleniyor)
                return;
            // Kontrol sekmesi yalnızca başarılı bir dayalı poligon hesabından sonra dolar ve açılır.
            KontrolTemizle();
            var noktaSayisi = _hesapNoktaSayisi;
            if (noktaSayisi < (AcikPoligon ? 3 : 4) || _hesap.Rows.Count < 2 * noktaSayisi - 1)
                return;

            _guncelleniyor = true;
            try
            {
                _tecvizTablosu.Rows.Clear();
                _hesaplananOlcek.Text = "";
                var olcekGecerli = NcnDosyasi.SayiOku(_girilenOlcek.Text, out var olcek) && olcek > 0;
                _girilenOlcek.BackColor = olcekGecerli ? SystemColors.Window : Color.MistyRose;
                if (!olcekGecerli)
                    olcek = 1;
                while (_hesap.Rows.Count > 2 * noktaSayisi - 1)
                    _hesap.Rows.RemoveAt(_hesap.Rows.Count - 1);
                for (var k = 0; k < noktaSayisi; k++)
                {
                    var satir = _hesap.Rows[2 * k];
                    foreach (var kolon in new[] { HSemt, HKenar, HDy, HDx, HY, HX })
                        satir.Cells[kolon].Value = null;
                    satir.Cells[HNokta2].Value = Metin(satir, HNokta);
                    if (k < noktaSayisi - 1)
                    {
                        var ara = _hesap.Rows[2 * k + 1];
                        foreach (var kolon in new[] { HAci, HSemt, HDy, HDx })
                            ara.Cells[kolon].Value = null;
                    }
                }

                var noktalar = Enumerable.Range(0, noktaSayisi).Select(k => Metin(_hesap.Rows[2 * k], HNokta)).ToList();
                if (AcikPoligon)
                {
                    AcikPoligonHesapla(noktalar, olcek);
                    return;
                }
                string A = noktalar[0], B = noktalar[1], C = noktalar[noktaSayisi - 2], D = noktalar[noktaSayisi - 1];
                var eksikler = new[] { A, B, C, D }.Where(no => !KoordinatBul(no, out _, out _)).Distinct().ToList();
                if (eksikler.Count > 0)
                {
                    _hesapOzet.Text = "Bilinen noktaların koordinatları eksik: " + string.Join(", ", eksikler) + ". Koordinatlar sayfasına gerçek koordinat olarak girin ya da ekrandan alın.";
                    return;
                }
                KoordinatBul(A, out var ya, out var xa);
                KoordinatBul(B, out var yb, out var xb);
                KoordinatBul(C, out var yc, out var xc);
                KoordinatBul(D, out var yd, out var xd);

                // İstasyonlar: 2. noktadan sondan bir önceki noktaya kadar. i. istasyon (i+1). noktadır; nokta satırı
                // 2(i+1), sonraki noktaya giden kenarın satırı 2(i+1)+1'dir.
                var n = noktaSayisi - 2;
                var betalar = new double[n];
                var kenarlar = new double[n - 1];
                for (var i = 0; i < n; i++)
                {
                    var beta = Sayi(_hesap.Rows[2 * (i + 1)], HAci);
                    if (beta == null)
                    {
                        _hesapOzet.Text = noktalar[i + 1] + " noktasının kırılma açısı eksik.";
                        return;
                    }
                    betalar[i] = beta.Value;
                    if (i < n - 1)
                    {
                        var kenarHucresi = _hesap.Rows[2 * (i + 1) + 1].Cells[HKenar];
                        var s = Sayi(_hesap.Rows[2 * (i + 1) + 1], HKenar);
                        if (s == null || s <= 0)
                        {
                            _hesapOzet.Text = noktalar[i + 1] + " - " + noktalar[i + 2] + " kenarı eksik.";
                            return;
                        }
                        // Girilen kenar olduğu gibi kalır; hesapta ölçekle çarpılmış hali kullanılır.
                        kenarlar[i] = s.Value * olcek;
                        kenarHucresi.ToolTipText = olcek != 1 ? "Hesapta kullanılan: " + M(s.Value) + " × " + olcek.ToString("0.0000000000", CultureInfo.InvariantCulture) + " = " + M(kenarlar[i]) + " m" : "";
                    }
                }

                // Semt taşıma ve açı kapanması.
                var baslangicSemti = Semt(ya, xa, yb, xb);
                var bitisSemti = Semt(yc, xc, yd, xd);
                var semt = baslangicSemti;
                for (var i = 0; i < n; i++)
                    semt = Mod400(semt + betalar[i] - 200);
                var tasinanSemt = semt;
                var aciKapanmasi = Fark(bitisSemti - semt);
                // Açı kapanması 0,1 cc birimlerle eşit dağıtılır; düzeltmelerin toplamı kapanmaya tam eşittir.
                var aciDuzeltmeleri = TamDagit((long)Math.Round(aciKapanmasi * 100000), Enumerable.Repeat(1.0, n).ToArray());

                _hesap.Rows[1].Cells[HSemt].Value = G(baslangicSemti);
                var semtler = new double[n];
                semt = baslangicSemti;
                for (var i = 0; i < n; i++)
                {
                    var aciDuzeltmesi = aciDuzeltmeleri[i] / 100000.0;
                    semt = Mod400(semt + betalar[i] + aciDuzeltmesi - 200);
                    semtler[i] = semt;
                    var ara = _hesap.Rows[2 * (i + 1) + 1];
                    ara.Cells[HAci].Value = Cc(aciDuzeltmesi);
                    ara.Cells[HSemt].Value = G(semt);
                }

                // Koordinat farkları mm'ye yuvarlanır, kapanmalar mm birimlerle kenar boylarıyla orantılı dağıtılır.
                // Böylece tabloda görünen Δy + vY toplamları koordinat farkını ve düzeltmelerin toplamı kapanmayı tam verir.
                var toplamS = kenarlar.Sum();
                var dyMm = new long[n - 1];
                var dxMm = new long[n - 1];
                for (var i = 0; i < n - 1; i++)
                {
                    var t = semtler[i] * Math.PI / 200;
                    dyMm[i] = (long)Math.Round(kenarlar[i] * Math.Sin(t) * 1000);
                    dxMm[i] = (long)Math.Round(kenarlar[i] * Math.Cos(t) * 1000);
                }
                var fyMm = (long)Math.Round((yc - yb) * 1000) - dyMm.Sum();
                var fxMm = (long)Math.Round((xc - xb) * 1000) - dxMm.Sum();
                var vyMm = TamDagit(fyMm, kenarlar);
                var vxMm = TamDagit(fxMm, kenarlar);
                var toplamDy = dyMm.Sum() / 1000.0;
                var toplamDx = dxMm.Sum() / 1000.0;
                var fy = fyMm / 1000.0;
                var fx = fxMm / 1000.0;

                double y = yb, x = xb;
                var hesaplananlar = new List<Tuple<string, double, double>>();
                for (var i = 0; i < n - 1; i++)
                {
                    var satir = _hesap.Rows[2 * (i + 1)];
                    var ara = _hesap.Rows[2 * (i + 1) + 1];
                    ara.Cells[HDy].Value = M(dyMm[i] / 1000.0);
                    ara.Cells[HDx].Value = M(dxMm[i] / 1000.0);
                    satir.Cells[HDy].Value = Mm(vyMm[i] / 1000.0);
                    satir.Cells[HDx].Value = Mm(vxMm[i] / 1000.0);
                    y += (dyMm[i] + vyMm[i]) / 1000.0;
                    x += (dxMm[i] + vxMm[i]) / 1000.0;
                    if (i < n - 2)
                        hesaplananlar.Add(Tuple.Create(noktalar[i + 2], y, x));
                }

                // Koordinatlar (bilinenler yeşil zeminde kalın, hesaplananlar normal).
                for (var i = 0; i < noktaSayisi; i++)
                {
                    var satir = _hesap.Rows[2 * i];
                    double py, px;
                    if (i < 2 || i >= noktaSayisi - 2)
                        KoordinatBul(noktalar[i], out py, out px);
                    else
                    {
                        py = hesaplananlar[i - 2].Item2;
                        px = hesaplananlar[i - 2].Item3;
                    }
                    satir.Cells[HY].Value = M(py);
                    satir.Cells[HX].Value = M(px);
                }

                // Toplam ve kapanma satırları.
                var toplamBeta = betalar.Sum();
                void ToplamSatiri(string semtMetni, string kenar, string dyMetin, string dxMetin, string yMetin, string xMetin)
                {
                    var satir = _hesap.Rows[_hesap.Rows.Add()];
                    foreach (DataGridViewCell hucre in satir.Cells)
                    {
                        hucre.ReadOnly = true;
                        hucre.Style.BackColor = RenkToplam;
                        hucre.Style.Font = _kalinYazi;
                    }
                    satir.Cells[HAci].Value = semtMetni;
                    satir.Cells[HKenar].Value = kenar;
                    satir.Cells[HDy].Value = dyMetin;
                    satir.Cells[HDx].Value = dxMetin;
                    satir.Cells[HY].Value = yMetin;
                    satir.Cells[HX].Value = xMetin;
                }
                // 1. satır: ölçülen açılarla taşınan son semt, [S], [Δy], [Δx], gereken Yc−Yb ve Xc−Xb.
                // 2. satır: koordinatlardan hesaplanan son semt (C→D), fy, fx.
                ToplamSatiri(G(tasinanSemt), M(toplamS), M(toplamDy), M(toplamDx), M(yc - yb), M(xc - xb));
                ToplamSatiri(G(bitisSemti), "", M(fy), M(fx), "", "");

                var fs = Math.Sqrt(fy * fy + fx * fx);
                TecvizleriYaz(n, aciKapanmasi, fy, fx, yc - yb, xc - xb, toplamS, toplamDy / olcek, toplamDx / olcek);
                _hesapOzet.Text = string.Format(CultureInfo.InvariantCulture,
                    "Başlangıç semti ({0}→{1}) = {2} g      Bitiş semti ({3}→{4}) = {5} g      İstasyon sayısı n = {6}\r\n" +
                    "Açı kapanması fβ = {7:0.0} cc   (açı başına düzeltme {8:0.0} cc)\r\n" +
                    "Koordinat kapanmaları: fy = {9:0.000} m,  fx = {10:0.000} m,  fs = {11:0.000} m,  [s] = {12:0.000} m,  bağıl kapanma = {13}",
                    A, B, G(baslangicSemti), C, D, G(bitisSemti), n, aciKapanmasi * 10000, aciKapanmasi * 10000 / n, fy, fx, fs, toplamS,
                    fs > 1e-9 ? "1 / " + (toplamS / fs).ToString("0", CultureInfo.InvariantCulture) : "kapanma yok");

                if (olcek != 1)
                    _hesapOzet.Text += "      (kenarlar girilen ölçek " + olcek.ToString("0.0000000000", CultureInfo.InvariantCulture) + " ile çarpıldı)";
                KoordinatlariAktar(hesaplananlar);
                KontrolHesapla(noktalar, betalar, kenarlar, yb, xb, yc, xc, baslangicSemti, bitisSemti, olcek);
            }
            finally
            {
                _guncelleniyor = false;
            }
        }

        private void KontrolTemizle()
        {
            _kontrol.Rows.Clear();
            _kontrolSayfa.Enabled = false;
            _kontrolDurum.Text = "Kontrol, dayalı poligon hesabı yapıldıktan sonra hesaplanır.";
        }

        /// <summary>
        /// Dayalı poligon kontrolü. Ölçülen kırılma açıları ve (ölçekli) kenarlarla, düzeltme yapılmadan:
        /// <list type="bullet">
        /// <item>Her hesaplanan nokta iki kez açık poligon olarak hesaplanır (baştaki A, B'den ileri; sondaki D, C'den
        /// geri) ve iki koordinat arasındaki farklar yazılır; dS'si en küçük ilk üç nokta kalın ve yeşildir.</item>
        /// <item>Ölçülen açısı olan her nokta (B, hesaplanan noktalar, C) için: açı kapanmasının tamamı o açının hatası
        /// sayılır, açı düzeltilip poligon yeniden çözülür ve kalan enine (fq), boyuna (fl) ve konum (fs) hataları
        /// yazılır. Hatalar olmasaydı kapanma sıfır olurdu; fs'yi en çok düşüren açı, kaba hatanın en olası yeridir.</item>
        /// </list>
        /// <para>Geriye taşımada t(i→i−1) = t(i→i+1) − β(i) kullanılır; başlangıç C→D semti koordinatlardan alınır.</para>
        /// </summary>
        private void KontrolHesapla(List<string> noktalar, double[] betalar, double[] kenarlar, double yb, double xb, double yc, double xc, double baslangicSemti, double bitisSemti, double olcek)
        {
            var c = CultureInfo.InvariantCulture;
            var n = betalar.Length;
            if (n < 3)
            {
                _kontrolDurum.Text = "Kontrol için en az bir hesaplanan nokta olmalıdır.";
                return;
            }

            // İstasyon i, noktalar[i + 1]'dir; kenarlar[i], istasyon i ile i + 1 arasındadır.
            // İleri taşımada her istasyona varış semti (önceki noktadan bu noktaya) da saklanır.
            var ileriY = new double[n];
            var ileriX = new double[n];
            var ileriVaris = new double[n];
            ileriY[0] = yb;
            ileriX[0] = xb;
            ileriVaris[0] = baslangicSemti;
            var semt = baslangicSemti;
            for (var i = 0; i < n - 1; i++)
            {
                semt = Mod400(semt + betalar[i] - 200);
                var t = semt * Math.PI / 200;
                ileriY[i + 1] = ileriY[i] + kenarlar[i] * Math.Sin(t);
                ileriX[i + 1] = ileriX[i] + kenarlar[i] * Math.Cos(t);
                ileriVaris[i + 1] = semt;
            }

            // Geri taşımada varış semti, sonraki noktadan bu noktaya olan semttir.
            var geriY = new double[n];
            var geriX = new double[n];
            var geriVaris = new double[n];
            geriY[n - 1] = yc;
            geriX[n - 1] = xc;
            geriVaris[n - 1] = Mod400(bitisSemti + 200);
            var ileriSemt = bitisSemti;
            for (var i = n - 1; i >= 1; i--)
            {
                var geriSemt = Mod400(ileriSemt - betalar[i]);
                var t = geriSemt * Math.PI / 200;
                geriY[i - 1] = geriY[i] + kenarlar[i - 1] * Math.Sin(t);
                geriX[i - 1] = geriX[i] + kenarlar[i - 1] * Math.Cos(t);
                geriVaris[i - 1] = geriSemt;
                ileriSemt = Mod400(geriSemt + 200);
            }

            // Kapanmanın enine/boyuna bileşenleri: B→C doğrultusuna dik ve paralel (TecvizleriYaz ile aynı).
            double bcY = yc - yb, bcX = xc - xb, bc = Math.Sqrt(bcY * bcY + bcX * bcX);
            void Ayir(double fy, double fx, out double fq, out double fl)
            {
                fl = bc > 1e-9 ? (fy * bcY + fx * bcX) / bc : Math.Sqrt(fy * fy + fx * fx);
                fq = bc > 1e-9 ? (fy * bcX - fx * bcY) / bc : 0;
            }

            AcikTasima(betalar, kenarlar, yb, xb, baslangicSemti, out _, out _, out var tasinan);
            var fBeta = Fark(bitisSemti - tasinan);

            // Her ölçülen açı için: açı kapanmasının tamamı bu açıda olsaydı (açı fβ kadar düzeltilince) kalan kapanma.
            // Tahmini açı = (sondan geri gelen son semt) − (baştan ileri gelen son semt) = ölçülen + fβ.
            var dsler = new List<Tuple<int, double>>();
            var varsayimlar = new List<Tuple<int, double>>();
            for (var i = 0; i < n; i++)
            {
                var deneme = (double[])betalar.Clone();
                deneme[i] += fBeta;
                AcikTasima(deneme, kenarlar, yb, xb, baslangicSemti, out var y, out var x, out _);
                double kfy = yc - y, kfx = xc - x;
                Ayir(kfy, kfx, out var kfq, out var kfl);
                var kfs = Math.Sqrt(kfy * kfy + kfx * kfx);
                var tahmini = Mod400(geriVaris[i] - ileriVaris[i]);
                var hesaplanan = i > 0 && i < n - 1;
                string dyM = "", dxM = "", dsM = "";
                if (hesaplanan)
                {
                    var dy = ileriY[i] - geriY[i];
                    var dx = ileriX[i] - geriX[i];
                    var ds = Math.Sqrt(dy * dy + dx * dx);
                    dyM = M(dy);
                    dxM = M(dx);
                    dsM = M(ds);
                    dsler.Add(Tuple.Create(i, ds));
                }
                var satir = _kontrol.Rows[_kontrol.Rows.Add(noktalar[i + 1] + (hesaplanan ? "" : " (bilinen)"), dyM, dxM, dsM,
                    G(tahmini), G(betalar[i]), (Fark(betalar[i] - tahmini) * 10000).ToString("+0.0;-0.0;0", c),
                    M(kfq), M(kfl), M(kfs),
                    hesaplanan ? M(ileriY[i]) : "", hesaplanan ? M(ileriX[i]) : "", hesaplanan ? M(geriY[i]) : "", hesaplanan ? M(geriX[i]) : "")];
                if (!hesaplanan)
                    satir.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                varsayimlar.Add(Tuple.Create(i, kfs));
            }

            // Satır i, istasyon i'dir.
            var enKucukDsler = dsler.OrderBy(d => d.Item2).Take(3).ToList();
            foreach (var d in enKucukDsler)
            {
                _kontrol.Rows[d.Item1].DefaultCellStyle.Font = _kalinYazi;
                _kontrol.Rows[d.Item1].DefaultCellStyle.ForeColor = Color.DarkGreen;
            }
            var sirali = varsayimlar.OrderBy(v => v.Item2).ToList();
            var enIyi = sirali[0].Item1;

            // Olağan çözüm: açı kapanması eşit dağıtılır.
            var duzeltilmis = betalar.Select(b => b + fBeta / n).ToArray();
            AcikTasima(duzeltilmis, kenarlar, yb, xb, baslangicSemti, out var y0, out var x0, out _);
            double fy0 = yc - y0, fx0 = xc - x0, fs0 = Math.Sqrt(fy0 * fy0 + fx0 * fx0);
            Ayir(fy0, fx0, out var fq0, out var fl0);

            var deneme2 = (double[])betalar.Clone();
            deneme2[enIyi] += fBeta;
            AcikTasima(deneme2, kenarlar, yb, xb, baslangicSemti, out var y1, out var x1, out _);
            Ayir(yc - y1, xc - x1, out var fq1, out var fl1);

            var ozet = string.Format(c,
                "Açı kapanması fβ = {0:+0.0;-0.0} cc (sınır {1:0.0} cc). Hata olmasaydı kapanmalar sıfır olurdu. Açı kapanması açılara eşit dağıtılırsa: enine fq = {2} m, boyuna fl = {3} m, fs = {4} m.",
                fBeta * 10000, 150 * Math.Sqrt(n), M(fq0), M(fl0), M(fs0));
            var aciMetni = string.Format(c,
                "açı hatası {0} noktasında {1:+0.0;-0.0} cc: açı {2} g yerine {3} g alınırsa enine hata {4} m, boyuna hata {5} m, fs {6} m olur.",
                noktalar[enIyi + 1], -fBeta * 10000, G(betalar[enIyi]), G(Mod400(betalar[enIyi] + fBeta)), M(fq1), M(fl1), M(sirali[0].Item2));
            if (sirali.Count > 1 && sirali[1].Item2 < 1.5 * sirali[0].Item2 + 0.005)
                aciMetni += string.Format(c, " {0} noktası da benzer sonuç veriyor (fs {1} m); ikisi ayırt edilemiyor.", noktalar[sirali[1].Item1 + 1], M(sirali[1].Item2));

            // Kenar varsayımı: açılar dağıtıldıktan sonra kalan kapanmaya en paralel kenar; o kenar düzeltilince kalan.
            string kenarMetni = null;
            var kenarKalan = double.MaxValue;
            if (fs0 > 1e-6)
            {
                var kapanmaSemti = Mod400(Math.Atan2(fy0, fx0) * 200 / Math.PI);
                int kenarIdx = -1;
                double enKucukSapma = double.MaxValue, kenarSemti = 0;
                semt = baslangicSemti;
                for (var i = 0; i < kenarlar.Length; i++)
                {
                    semt = Mod400(semt + duzeltilmis[i] - 200);
                    // Kapanma vektörüyle aynı ya da ters yönde olan kenar (200 g'lik belirsizlik).
                    var sapma = Math.Abs(Fark(2 * (semt - kapanmaSemti)) / 2);
                    if (sapma < enKucukSapma)
                    {
                        enKucukSapma = sapma;
                        kenarIdx = i;
                        kenarSemti = semt;
                    }
                }
                var tk = kenarSemti * Math.PI / 200;
                double uy = Math.Sin(tk), ux = Math.Cos(tk);
                var boyuna = fy0 * uy + fx0 * ux;
                Ayir(fy0 - boyuna * uy, fx0 - boyuna * ux, out var fq2, out var fl2);
                kenarKalan = Math.Sqrt(fq2 * fq2 + fl2 * fl2);
                kenarMetni = string.Format(c,
                    "kenar hatası {0} - {1} kenarında (açılar dağıtıldıktan sonra kalan kapanmaya paralel, sapma {2:0.0} g): kenar {3} m yerine {4} m ({5:+0.000;-0.000} m) alınırsa enine hata {6} m, boyuna hata {7} m, fs {8} m olur.",
                    noktalar[kenarIdx + 1], noktalar[kenarIdx + 2], enKucukSapma, M(kenarlar[kenarIdx] / olcek), M((kenarlar[kenarIdx] + boyuna) / olcek), boyuna / olcek, M(fq2), M(fl2), M(kenarKalan));
            }

            // Kenar hatası açı kapanması üretemez; açı kapanması dağıtıldıktan sonra kalan küçük bir kapanmayı ise tek
            // kenarla gidermek her zaman kolaydır. Bu yüzden bir açı düzeltmesi kapanmayı en az yarıya indiriyorsa açı
            // varsayımı öne alınır; kenar ancak hiçbir açı düzeltmesi işe yaramıyorsa öne çıkar.
            var aciOnde = kenarMetni == null || sirali[0].Item2 <= 0.5 * fs0 || sirali[0].Item2 <= kenarKalan;
            if (aciOnde)
                foreach (var k in new[] { 6, 7, 8, 9 })
                    _kontrol.Rows[enIyi].Cells[k].Style.BackColor = Color.MistyRose;
            var metin = ozet + "\r\nEn olası: " + (aciOnde ? aciMetni + " (satır pembe)" : kenarMetni);
            if (kenarMetni != null)
                metin += "\r\nDiğer varsayım: " + (aciOnde ? kenarMetni : aciMetni);

            _kontrolSayfa.Enabled = true;
            _kontrolDurum.Text = metin;
        }

        /// <summary>Ölçülen açı ve kenarlarla B'den ileri açık poligon: son noktanın koordinatı ve son semt.</summary>
        private static void AcikTasima(double[] betalar, double[] kenarlar, double yb, double xb, double baslangicSemti, out double y, out double x, out double sonSemt)
        {
            y = yb;
            x = xb;
            var semt = baslangicSemti;
            for (var i = 0; i < betalar.Length; i++)
            {
                semt = Mod400(semt + betalar[i] - 200);
                if (i < kenarlar.Length)
                {
                    var t = semt * Math.PI / 200;
                    y += kenarlar[i] * Math.Sin(t);
                    x += kenarlar[i] * Math.Cos(t);
                }
            }
            sonSemt = semt;
        }

        /// <summary>
        /// Açık poligon: başlangıçtaki iki bilinen noktadan (A, B) çıkılır; semt t(i) = t(i-1) + β(i) − 200 ile taşınır
        /// ve karnede ölçülen bütün noktaların koordinatları ileriye doğru hesaplanır. Bitişte bilinen nokta olmadığından
        /// kapanma ve düzeltme yoktur. Kenarlar girilen ölçekle çarpılır. PoligonHesapla içinden çağrılır.
        /// </summary>
        private void AcikPoligonHesapla(List<string> noktalar, double olcek)
        {
            var noktaSayisi = noktalar.Count;
            string A = noktalar[0], B = noktalar[1];
            var eksikler = new[] { A, B }.Where(no => !KoordinatBul(no, out _, out _)).Distinct().ToList();
            if (eksikler.Count > 0)
            {
                _hesapOzet.Text = "Başlangıç noktalarının koordinatları eksik: " + string.Join(", ", eksikler) + ". Koordinatlar sayfasına gerçek koordinat olarak girin ya da ekrandan alın.";
                return;
            }
            KoordinatBul(A, out var ya, out var xa);
            KoordinatBul(B, out var yb, out var xb);

            // n istasyon (B'den sondan bir önceki noktaya), her istasyondan sonraki noktaya ölçülmüş n kenar.
            var n = noktaSayisi - 2;
            var betalar = new double[n];
            var kenarlar = new double[n];
            for (var i = 0; i < n; i++)
            {
                var beta = Sayi(_hesap.Rows[2 * (i + 1)], HAci);
                if (beta == null)
                {
                    _hesapOzet.Text = noktalar[i + 1] + " noktasının kırılma açısı eksik.";
                    return;
                }
                betalar[i] = beta.Value;
                var kenarHucresi = _hesap.Rows[2 * (i + 1) + 1].Cells[HKenar];
                var s = Sayi(_hesap.Rows[2 * (i + 1) + 1], HKenar);
                if (s == null || s <= 0)
                {
                    _hesapOzet.Text = noktalar[i + 1] + " - " + noktalar[i + 2] + " kenarı eksik.";
                    return;
                }
                kenarlar[i] = s.Value * olcek;
                kenarHucresi.ToolTipText = olcek != 1 ? "Hesapta kullanılan: " + M(s.Value) + " × " + olcek.ToString("0.0000000000", CultureInfo.InvariantCulture) + " = " + M(kenarlar[i]) + " m" : "";
            }

            var baslangicSemti = Semt(ya, xa, yb, xb);
            _hesap.Rows[1].Cells[HSemt].Value = G(baslangicSemti);
            var semt = baslangicSemti;
            long toplamDyMm = 0, toplamDxMm = 0;
            double y = yb, x = xb;
            var hesaplananlar = new List<Tuple<string, double, double>>();
            for (var i = 0; i < n; i++)
            {
                semt = Mod400(semt + betalar[i] - 200);
                var t = semt * Math.PI / 200;
                // Dayalı poligondaki gibi Δy/Δx mm'ye yuvarlanır; koordinatlar tablodaki değerlerin toplamıdır.
                var dyMm = (long)Math.Round(kenarlar[i] * Math.Sin(t) * 1000);
                var dxMm = (long)Math.Round(kenarlar[i] * Math.Cos(t) * 1000);
                toplamDyMm += dyMm;
                toplamDxMm += dxMm;
                var ara = _hesap.Rows[2 * (i + 1) + 1];
                ara.Cells[HSemt].Value = G(semt);
                ara.Cells[HDy].Value = M(dyMm / 1000.0);
                ara.Cells[HDx].Value = M(dxMm / 1000.0);
                y += dyMm / 1000.0;
                x += dxMm / 1000.0;
                hesaplananlar.Add(Tuple.Create(noktalar[i + 2], y, x));
            }

            for (var i = 0; i < noktaSayisi; i++)
            {
                var satir = _hesap.Rows[2 * i];
                double py, px;
                if (i < 2)
                    KoordinatBul(noktalar[i], out py, out px);
                else
                {
                    py = hesaplananlar[i - 2].Item2;
                    px = hesaplananlar[i - 2].Item3;
                }
                satir.Cells[HY].Value = M(py);
                satir.Cells[HX].Value = M(px);
            }

            // Toplam satırı: son semt, [S], [Δy], [Δx].
            var toplam = _hesap.Rows[_hesap.Rows.Add()];
            foreach (DataGridViewCell hucre in toplam.Cells)
            {
                hucre.ReadOnly = true;
                hucre.Style.BackColor = RenkToplam;
                hucre.Style.Font = _kalinYazi;
            }
            toplam.Cells[HAci].Value = G(semt);
            toplam.Cells[HKenar].Value = M(kenarlar.Sum());
            toplam.Cells[HDy].Value = M(toplamDyMm / 1000.0);
            toplam.Cells[HDx].Value = M(toplamDxMm / 1000.0);

            _hesapOzet.Text = string.Format(CultureInfo.InvariantCulture,
                "Açık poligon: başlangıç semti ({0}→{1}) = {2} g      İstasyon sayısı n = {3}      Hesaplanan nokta sayısı = {4}\r\n" +
                "[s] = {5:0.000} m      Son nokta {6}: Y = {7:0.000}  X = {8:0.000}\r\n" +
                "Bitişte bilinen nokta olmadığından açı ve koordinat kapanması denetlenemez; ölçüler düzeltilmeden kullanılmıştır.",
                A, B, G(baslangicSemti), n, hesaplananlar.Count, kenarlar.Sum(), noktalar[noktaSayisi - 1], y, x);
            if (olcek != 1)
                _hesapOzet.Text += "      (kenarlar girilen ölçek " + olcek.ToString("0.0000000000", CultureInfo.InvariantCulture) + " ile çarpıldı)";
            KoordinatlariAktar(hesaplananlar);
        }

        /// <summary>
        /// BÖHHBÜY (2005) Madde 27'ye göre poligon geçkisi hata sınırlarını hesaplayıp hesaplanan hatalarla karşılaştırır:
        /// açı kapanması Fβ = 1,5ᶜ·√n (1ᶜ = 100 cc), enine hata F_Q = 0,05 + 0,15·√S[km] m, boyuna hata
        /// F_L = 0,05 + 0,04·√(n−1) m; n başlangıç ve son noktalar dahil kırık nokta sayısı, S geçki uzunluğudur.
        /// Enine/boyuna hata, koordinat kapanmasının başlangıç-son noktası doğrultusuna dik ve paralel bileşenleridir.
        /// </summary>
        private void TecvizleriYaz(int n, double aciKapanmasiGrad, double fy, double fx, double bcY, double bcX, double toplamS, double toplamDy, double toplamDx)
        {
            _tecvizTablosu.Rows.Clear();
            var bc = Math.Sqrt(bcY * bcY + bcX * bcX);
            double fL = bc > 1e-9 ? (fy * bcY + fx * bcX) / bc : Math.Sqrt(fy * fy + fx * fx);
            double fQ = bc > 1e-9 ? (fy * bcX - fx * bcY) / bc : 0;

            var aciSiniriCc = 150 * Math.Sqrt(n);
            var enineSinir = 0.05 + 0.15 * Math.Sqrt(toplamS / 1000);
            var boyunaSinir = 0.05 + 0.04 * Math.Sqrt(Math.Max(0, n - 1));

            void Satir(string kontrol, string hata, string sinir, bool uygun)
            {
                var satir = _tecvizTablosu.Rows[_tecvizTablosu.Rows.Add(kontrol, hata, sinir, uygun ? "Uygun" : "SINIRI AŞIYOR")];
                satir.Cells[3].Style.ForeColor = uygun ? Color.DarkGreen : Color.Firebrick;
                satir.Cells[3].Style.Font = _kalinYazi;
            }
            var c = CultureInfo.InvariantCulture;
            Satir("Açı kapanması  fβ  ≤  Fβ = 1,5ᶜ·√n   (n = " + n + ")", (aciKapanmasiGrad * 10000).ToString("0.0", c) + " cc", aciSiniriCc.ToString("0.0", c) + " cc",
                Math.Abs(aciKapanmasiGrad * 10000) <= aciSiniriCc);
            var fs = Math.Sqrt(fy * fy + fx * fx);
            var fsSiniri = Math.Sqrt(enineSinir * enineSinir + boyunaSinir * boyunaSinir);
            Satir("Konum hatası  fs = √(fy² + fx²)  ≤  FS = √(FQ² + FL²)", fs.ToString("0.000", c) + " m", fsSiniri.ToString("0.000", c) + " m", fs <= fsSiniri);
            Satir("Enine hata  fq = (fy·[Δx] − fx·[Δy]) / S  ≤  FQ = 0,05 + 0,15·√S[km]   (S = " + (toplamS / 1000).ToString("0.000", c) + " km)", fQ.ToString("0.000", c) + " m", enineSinir.ToString("0.000", c) + " m",
                Math.Abs(fQ) <= enineSinir);
            Satir("Boyuna hata  fl = (fy·[Δy] + fx·[Δx]) / S  ≤  FL = 0,05 + 0,04·√(n−1)", fL.ToString("0.000", c) + " m", boyunaSinir.ToString("0.000", c) + " m", Math.Abs(fL) <= boyunaSinir);

            // Hesaplanan ölçek: bilinen B–C uzunluğunun ölçülen kenarlardan (girilen ölçek uygulanmadan) çıkan uzunluğa
            // oranı. Girilen Ölçek kutusuna yazılırsa hesaba uygulanır.
            var olculen = Math.Sqrt(toplamDy * toplamDy + toplamDx * toplamDx);
            if (olculen > 1e-9)
            {
                var olcekMetni = (bc / olculen).ToString("0.0000000000", c);
                _hesaplananOlcek.Text = olcekMetni;
                var bilgi = _tecvizTablosu.Rows[_tecvizTablosu.Rows.Add("Hesaplanan ölçek  k = bilinen B–C uzunluğu / √([Δy]² + [Δx]²)  (ölçek uygulanmadan)", olcekMetni, "—", "Bilgi")];
                bilgi.Cells[3].Style.ForeColor = Color.DarkBlue;
            }
        }

        // ------------------------------------------------------------------ dosya

        /// <summary>
        /// Poligon hesabının tamamını (.pol) kaydeder: karne, koordinatlar (durumlarıyla) ve hesap tablosunun girdileri.
        /// Düz metin, UTF-8; bölümler köşeli parantezli başlıklarla, değerler sekmeyle ayrılır.
        /// </summary>
        private bool DosyaKaydet()
        {
            using (var dlg = new SaveFileDialog { Title = "Poligon Hesabını Kaydet", Filter = "Poligon hesabı (*.pol)|*.pol", FileName = _dosyaYolu != null ? Path.GetFileName(_dosyaYolu) : "poligon.pol" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;
                try
                {
                    var metin = PolMetni();
                    File.WriteAllText(dlg.FileName, metin, new UTF8Encoding(false));
                    _kayitliMetin = metin;
                    _dosyaYolu = dlg.FileName;
                    Text = "Poligon Hesabı - " + Path.GetFileName(_dosyaYolu);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya kaydedilemedi: " + ex.Message, "Poligon Hesabı", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        // Son kaydedilen ya da açılan (yeni hesapta boş formun) içerik; kaydedilmemiş değişiklik bununla anlaşılır.
        private string _kayitliMetin;

        /// <summary>
        /// Yeni hesap: kaydedilmemiş değişiklik varsa kaydedilsin mi diye sorar (İptal'de hiçbir şey yapmaz), sonra
        /// karne, koordinatlar, hesap ve kontrol tablolarını boşaltıp formu ilk açıldığı hale getirir.
        /// </summary>
        private void YeniHesap()
        {
            if (_kayitliMetin != null && PolMetni() != _kayitliMetin)
            {
                var cevap = MessageBox.Show(this, "Mevcut poligon hesabında kaydedilmemiş değişiklikler var. Kaydedilsin mi?", "Yeni Poligon Hesabı",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (cevap == DialogResult.Cancel || (cevap == DialogResult.Yes && !DosyaKaydet()))
                    return;
            }

            _guncelleniyor = true;
            try
            {
                _karne.Rows.Clear();
                _koordinatlar.Rows.Clear();
                SilsileTemizle();
                HesapTablosunuKur(new List<string[]>());
                _girilenOlcek.Text = "1";
                _girilenOlcek.BackColor = SystemColors.Window;
                _poligonTuru.SelectedIndex = 0;
                _tarih.Value = DateTime.Today;
            }
            finally
            {
                _guncelleniyor = false;
            }
            _hesabaBagli = false;
            PoligonTuruDegisti();
            _tecvizTablosu.Rows.Clear();
            _hesaplananOlcek.Text = "";
            KontrolTemizle();
            KarneHesapla();
            SilsileHesapla();
            _dosyaYolu = null;
            Text = "Poligon Hesabı";
            _hesapOzet.Text = "Karne sayfasında ölçüleri girin (ya da Excel'den yapıştırın) ve Hesaba Aktar'a basın. Bilinen 4 noktanın koordinatlarını Koordinatlar sayfasına girin.";
            _koordinatDurum.Text = "Gerçek (bilinen) koordinatlar kalın, hesaplananlar normal yazılır. Elle girilen ya da ekrandan alınan noktalar gerçek sayılır.";
            _sekmeler.SelectedIndex = 0;
            _kayitliMetin = PolMetni();
        }

        private static string Temiz(object deger) => (Convert.ToString(deger) ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

        /// <summary>Formun bütün içeriğinin .pol dosya metni.</summary>
        public string PolMetni()
        {
            var sb = new StringBuilder();
            sb.AppendLine("#XMLTOKTB POLIGON 2");
            sb.AppendLine("[KARNE]");
            foreach (DataGridViewRow satir in _karne.Rows)
            {
                if (!satir.IsNewRow)
                    sb.AppendLine(string.Join("\t", new[] { KDurulan, KBakilan, KDurum1, KDurum2, KMesafe1, KMesafe2 }.Select(k => Temiz(satir.Cells[k].Value))));
            }
            sb.AppendLine("[KOORDINATLAR]");
            foreach (DataGridViewRow satir in _koordinatlar.Rows)
            {
                if (!satir.IsNewRow)
                    sb.AppendLine(string.Join("\t", new[] { CNo, CY, CX, CDurum }.Select(k => Temiz(satir.Cells[k].Value))));
            }
            sb.AppendLine("[HESAP]");
            // Her nokta için bir satır: nokta no, kırılma açısı, sonraki noktaya kenar.
            foreach (var nokta in HesapGirdileri())
                sb.AppendLine(string.Join("\t", nokta.Select(Temiz)));
            SilsileMetniniYaz(sb);
            sb.AppendLine("[DURUM]");
            sb.AppendLine("HesabaBagli\t" + (_hesabaBagli ? "1" : "0"));
            sb.AppendLine("Tur\t" + (AcikPoligon ? "Acik" : "Dayali"));
            sb.AppendLine("Tarih\t" + _tarih.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
            sb.AppendLine("Olcek\t" + Temiz(_girilenOlcek.Text.Trim()));
            return sb.ToString();
        }

        private void DosyaAc()
        {
            using (var dlg = new OpenFileDialog { Title = "Poligon Hesabı Aç", Filter = "Poligon hesabı (*.pol)|*.pol|Tüm dosyalar (*.*)|*.*" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    PolYukle(File.ReadAllText(dlg.FileName, Encoding.UTF8));
                    _kayitliMetin = PolMetni();
                    _dosyaYolu = dlg.FileName;
                    Text = "Poligon Hesabı - " + Path.GetFileName(_dosyaYolu);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya açılamadı: " + ex.Message, "Poligon Hesabı", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>.pol metnini yükler; bütün sayfalar kaydedildiği haliyle dolar ve hesap yeniden yapılır.</summary>
        public void PolYukle(string metin)
        {
            var satirlar = metin.Replace("\r\n", "\n").Split('\n');
            if (satirlar.Length == 0 || !satirlar[0].StartsWith("#XMLTOKTB POLIGON"))
                throw new InvalidDataException("Bu bir poligon hesabı (.pol) dosyası değil.");

            _guncelleniyor = true;
            var bagli = false;
            var hesapNoktalari = new List<string[]>();
            try
            {
                _karne.Rows.Clear();
                _koordinatlar.Rows.Clear();
                SilsileTemizle();
                _girilenOlcek.Text = "1";
                _poligonTuru.SelectedIndex = 0;
                string bolum = null;
                foreach (var ham in satirlar.Skip(1))
                {
                    if (ham.Length == 0)
                        continue;
                    if (ham.StartsWith("[") && ham.EndsWith("]"))
                    {
                        bolum = ham;
                        continue;
                    }
                    var p = ham.Split('\t');
                    string Al(int i) => i < p.Length ? p[i] : "";
                    switch (bolum)
                    {
                        case "[KARNE]":
                            // Sürüm 1 dosyalarında tek mesafe kolonu vardı; o mesafe I. durum mesafesi sayılır.
                            _karne.Rows.Add(Al(0), Al(1), Al(2), Al(3), Al(4), Al(5));
                            break;
                        case "[KOORDINATLAR]":
                        {
                            var satir = _koordinatlar.Rows[_koordinatlar.Rows.Add(Al(0), Al(1), Al(2), Al(3))];
                            if (Al(3) == Hesaplanan)
                                satir.DefaultCellStyle.Font = null;
                            else
                                GercekYap(satir);
                            break;
                        }
                        case "[HESAP]":
                            hesapNoktalari.Add(new[] { Al(0), Al(1), Al(2) });
                            break;
                        case "[SILSILE]":
                        case "[SILSILE_SECIM]":
                            SilsileSatiriniYukle(bolum, p);
                            break;
                        case "[DURUM]":
                            if (Al(0) == "HesabaBagli")
                                bagli = Al(1) == "1";
                            else if (Al(0) == "Tur")
                                _poligonTuru.SelectedIndex = Al(1) == "Acik" ? 1 : 0;
                            else if (Al(0) == "Tarih" && DateTime.TryParseExact(Al(1), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarih))
                                _tarih.Value = tarih;
                            else if (Al(0) == "Olcek" && Al(1).Length > 0)
                                _girilenOlcek.Text = Al(1);
                            break;
                    }
                }
                HesapTablosunuKur(hesapNoktalari);
            }
            finally
            {
                _guncelleniyor = false;
            }

            _hesabaBagli = bagli;
            KarneHesapla();
            SilsileHesapla();
            if (bagli)
                HesabaAktar(false);
            else
                PoligonHesapla();
            _sekmeler.SelectedIndex = 0;
        }

        /// <summary>Koordinat tablosundaki bütün noktaları NCN dosyasına yazar (Z = 0).</summary>
        private void NcnKaydet()
        {
            var noktalar = new List<NoktaKaydi>();
            foreach (DataGridViewRow satir in _koordinatlar.Rows)
            {
                if (satir.IsNewRow)
                    continue;
                var y = Sayi(satir, CY);
                var x = Sayi(satir, CX);
                if (y == null || x == null)
                    continue;
                noktalar.Add(new NoktaKaydi { Ad = Metin(satir, CNo), Y = y.Value, X = x.Value, Z = 0 });
            }
            if (noktalar.Count == 0)
            {
                MessageBox.Show("Kaydedilecek koordinat yok.", "Kaydet (NCN)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog { Title = "Koordinatları NCN Olarak Kaydet", Filter = "NCN (*.ncn)|*.ncn", FileName = _dosyaYolu != null ? Path.GetFileNameWithoutExtension(_dosyaYolu) + ".ncn" : "poligon.ncn" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    NcnDosyasi.Yaz(noktalar, dlg.FileName);
                    _koordinatDurum.Text = noktalar.Count + " nokta NCN olarak kaydedildi (Z = 0): " + dlg.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya kaydedilemedi: " + ex.Message, "Kaydet (NCN)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ------------------------------------------------------------------ koordinatlar

        private void GercekYap(DataGridViewRow satir)
        {
            satir.Cells[CDurum].Value = Gercek;
            satir.DefaultCellStyle.Font = _kalinYazi;
        }

        // Hesaplanan noktaları koordinat tablosuna yazar; aynı adlı gerçek nokta varsa ona dokunmaz.
        private void KoordinatlariAktar(List<Tuple<string, double, double>> hesaplananlar)
        {
            var gecerli = new HashSet<string>(hesaplananlar.Select(h => h.Item1));
            for (var i = _koordinatlar.Rows.Count - 1; i >= 0; i--)
            {
                var satir = _koordinatlar.Rows[i];
                if (!satir.IsNewRow && Metin(satir, CDurum) == Hesaplanan && !gecerli.Contains(Metin(satir, CNo)))
                    _koordinatlar.Rows.RemoveAt(i);
            }
            foreach (var h in hesaplananlar)
            {
                var satir = _koordinatlar.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => !r.IsNewRow && Metin(r, CNo) == h.Item1);
                if (satir != null && Metin(satir, CDurum) != Hesaplanan)
                    continue;
                if (satir == null)
                    satir = _koordinatlar.Rows[_koordinatlar.Rows.Add()];
                satir.Cells[CNo].Value = h.Item1;
                satir.Cells[CY].Value = M(h.Item2);
                satir.Cells[CX].Value = M(h.Item3);
                satir.Cells[CDurum].Value = Hesaplanan;
                satir.DefaultCellStyle.Font = null;
            }
        }

        private void EkrandanAlBaslat()
        {
            _ekrandanAliniyor = true;
            _panel.NoktaAlmayaBasla(true);
            _koordinatDurum.Text = "Ekrandan Al: çizimdeki noktaları tıklayın (her nokta gerçek koordinat olarak eklenir). Bitirmek için çizimde sağ tık ya da Esc.";
            Owner?.Activate();
            _panel.Focus();
        }

        private void Panel_NoktaTiklandi(double x, double y)
        {
            if (!_ekrandanAliniyor)
                return;
            var tolerans = 8 / _panel.Gorunum.Olcek;
            var nokta = _panel.Nesneler.OfType<NoktaNesnesi>()
                .Where(n => n.Tabaka == null || n.Tabaka.Gorunur)
                .OrderBy(n => (n.X - x) * (n.X - x) + (n.Y - y) * (n.Y - y))
                .FirstOrDefault(n => Math.Sqrt((n.X - x) * (n.X - x) + (n.Y - y) * (n.Y - y)) <= tolerans);
            if (nokta == null)
            {
                _koordinatDurum.Text = "Ekrandan Al: tıklanan yerde nokta objesi yok. Bir noktanın üzerine tıklayın.";
                return;
            }

            _guncelleniyor = true;
            try
            {
                var no = string.IsNullOrWhiteSpace(nokta.NoktaAdi) ? "?" : nokta.NoktaAdi;
                var satir = _koordinatlar.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => !r.IsNewRow && Metin(r, CNo) == no)
                            ?? _koordinatlar.Rows[_koordinatlar.Rows.Add()];
                satir.Cells[CNo].Value = no;
                // Çizimde X sağa (Y), Y yukarı (X) değerdir.
                satir.Cells[CY].Value = M(nokta.X);
                satir.Cells[CX].Value = M(nokta.Y);
                GercekYap(satir);
                _koordinatDurum.Text = "Ekrandan Al: " + no + " noktası alındı. Başka nokta tıklayın ya da sağ tıkla bitirin.";
            }
            finally
            {
                _guncelleniyor = false;
            }
            PoligonHesapla();
        }

        private void Panel_NoktaAlmaBitti(object sender, EventArgs e)
        {
            if (!_ekrandanAliniyor)
                return;
            _ekrandanAliniyor = false;
            _koordinatDurum.Text = "Gerçek (bilinen) koordinatlar kalın, hesaplananlar normal yazılır.";
            Activate();
        }

        /// <summary>Sabit (bilinen) noktaların çizimdeki rengi; hesaplananlar tabaka rengiyle çizilir.</summary>
        private static readonly Color SabitNoktaRengi = Color.Red;

        /// <summary>
        /// Koordinatlar sayfasındaki bütün noktaları çizime aktarır: sabit (bilinen) noktalar kırmızı, hesaplananlar
        /// tabaka renginde. Aynı numaralı nokta çizimde varsa yeri ve rengi güncellenir.
        /// </summary>
        private void CizimeAktar()
        {
            int sabit = 0, hesaplanan = 0;
            foreach (DataGridViewRow satir in _koordinatlar.Rows)
            {
                if (satir.IsNewRow)
                    continue;
                var no = Metin(satir, CNo);
                var y = Sayi(satir, CY);
                var x = Sayi(satir, CX);
                if (no.Length == 0 || y == null || x == null)
                    continue;
                var sabitMi = Metin(satir, CDurum) != Hesaplanan;
                var nokta = _panel.Nesneler.OfType<NoktaNesnesi>().FirstOrDefault(n => n.NoktaAdi == no);
                if (nokta == null)
                {
                    nokta = new NoktaNesnesi { NoktaAdi = no };
                    _panel.Nesneler.Add(nokta);
                }
                nokta.X = y.Value;
                nokta.Y = x.Value;
                nokta.OzelRenk = sabitMi ? SabitNoktaRengi : (Color?)null;
                if (sabitMi)
                    sabit++;
                else
                    hesaplanan++;
            }
            _panel.DegisiklikKaydet();
            _panel.Invalidate();
            _koordinatDurum.Text = (sabit + hesaplanan) + " nokta çizime aktarıldı: " + sabit + " sabit (kırmızı), " + hesaplanan + " hesaplanan. Aynı numaralı nokta varsa yeri güncellendi.";
        }
    }
}
