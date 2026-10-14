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
    /// Güzergah editörü: güzergahı oluşturan parçaları sırasıyla ve özellikleriyle listeler, değiştirilmesine izin
    /// verir. Pencere çizim ekranının üzerinde açık kalır; her değişiklik güzergahı hemen yeniden hesaplar ve çizime
    /// yansır, tabloda seçili parça çizimde vurgulanır. Vazgeç, güzergahı pencere açıldığındaki haline döndürür.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class GuzergahEditoruForm : Form
    {
        private const int SiraKolon = 0, TipKolon = 1, UzunlukKolon = 2, YaricapBasKolon = 3, YaricapBitKolon = 4, YonKolon = 5,
            BasKmKolon = 6, BitKmKolon = 7, BasYKolon = 8, BasXKolon = 9, BitYKolon = 10, BitXKolon = 11;

        private static readonly string[] TipAdlari = { "Doğru", "Kurp", "Spiral" };
        private const string SagMetni = "Sağ", SolMetni = "Sol", SonsuzMetni = "INF";

        private readonly CizimPaneli _panel;
        private readonly GuzergahNesnesi _guzergah;
        private readonly GuzergahNesnesi _acilisHali;
        private readonly List<GuzergahNesnesi> _geriAlListesi = new List<GuzergahNesnesi>();
        private readonly List<GuzergahParcasi> _pano = new List<GuzergahParcasi>();
        private bool _dolduruluyor;
        private bool _vazgecildi;

        private readonly TextBox _adKutusu = new TextBox { Width = 130 };
        private readonly TextBox _kmKutusu = new TextBox { Width = 90, TextAlign = HorizontalAlignment.Right };
        private readonly TextBox _yKutusu = new TextBox { Width = 100, TextAlign = HorizontalAlignment.Right };
        private readonly TextBox _xKutusu = new TextBox { Width = 100, TextAlign = HorizontalAlignment.Right };
        private readonly TextBox _semtKutusu = new TextBox { Width = 80, TextAlign = HorizontalAlignment.Right };
        private readonly Label _ozetLabel = new Label { AutoSize = true, Location = new Point(8, 5) };
        private readonly Label _komutLabel = new Label { AutoSize = true, Location = new Point(8, 5), ForeColor = Color.Firebrick, Visible = false };

        /// <summary>Ekrandan nokta bekleyen komut.</summary>
        private enum EkranKomutu
        {
            Yok,
            SomeCiz,
            SomeEkle,
            SomeSil,
            KurpYerlestir,
            SpiralliKurpYerlestir,
            ParcaSil,
            /// <summary>Sona teğet kurp: bitiş noktası tıklanır, yarıçap hesaplanır.</summary>
            KurpTegetNokta,
            /// <summary>Sona teğet kurp: yarıçap önceden verilir, bitiş yönü tıklanır.</summary>
            KurpYaricapNokta,
            /// <summary>Sona kurp: önce bitiş noktası, sonra kurbun üzerinden geçeceği nokta tıklanır.</summary>
            KurpUcNokta,
            /// <summary>Tıklanan kurbun girişine/çıkışına spiral eklenir.</summary>
            SpiralKurbaEkle
        }

        // Yarıçapı önceden verilen kurp komutunun yarıçapı ve üç noktalı kurpta tıklanan bitiş noktası.
        private double _komutYaricapi;
        private double[] _araNokta;

        private EkranKomutu _komut = EkranKomutu.Yok;
        // Boş güzergah çizilirken tıklanan ilk nokta (ikinci tıklamayla ilk doğru oluşur).
        private double[] _ilkNokta;
        // Kurp yerleştirme pencerelerinde son kullanılan değerler.
        private double _sonYaricap = 500, _sonGirisSpirali = 60, _sonCikisSpirali = 60;
        private ToolStripButton _geriAlDugmesi, _yapistirDugmesi;

        private readonly DataGridView _tablo = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersWidth = 28,
            SelectionMode = DataGridViewSelectionMode.RowHeaderSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        public GuzergahEditoruForm(CizimPaneli panel, GuzergahNesnesi guzergah)
        {
            _panel = panel;
            _guzergah = guzergah;
            _acilisHali = guzergah.Kopyala();

            Text = "Güzergah Editörü";
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(1040, 420);
            MinimumSize = new Size(760, 300);
            MinimizeBox = false;
            ShowInTaskbar = false;

            // --- üst menü: ekranda çizen/düzenleyen komutlar ---
            var menu = new MenuStrip();
            var dogruMenu = new ToolStripMenuItem("Doğru");
            dogruMenu.DropDownItems.Add("Some Noktalarıyla Çiz (ekrandan)", null, (s, e) => KomutuBaslat(EkranKomutu.SomeCiz));
            dogruMenu.DropDownItems.Add("Tıklanan Yere Some Ekle (ekrandan)", null, (s, e) => KomutuBaslat(EkranKomutu.SomeEkle));
            dogruMenu.DropDownItems.Add("Some Sil (ekrandan)", null, (s, e) => KomutuBaslat(EkranKomutu.SomeSil));
            var kurpMenu = new ToolStripMenuItem("Kurp");
            kurpMenu.DropDownItems.Add("Someye Kurp Yerleştir / Değiştir (ekrandan)", null, (s, e) => KomutuBaslat(EkranKomutu.KurpYerlestir));
            kurpMenu.DropDownItems.Add(new ToolStripSeparator());
            kurpMenu.DropDownItems.Add("Sona Kurp Ekle: Teğet, bitiş noktasını tıkla (yarıçap hesaplanır)", null, (s, e) => KomutuBaslat(EkranKomutu.KurpTegetNokta));
            kurpMenu.DropDownItems.Add("Sona Kurp Ekle: Teğet, yarıçap ver, bitiş yönünü tıkla", null, (s, e) => KomutuBaslat(EkranKomutu.KurpYaricapNokta));
            kurpMenu.DropDownItems.Add("Sona Kurp Ekle: Bitiş noktası ve üzerinden geçeceği nokta (3 nokta)", null, (s, e) => KomutuBaslat(EkranKomutu.KurpUcNokta));
            kurpMenu.DropDownItems.Add("Sona Kurp Ekle: Yarıçap, boy ve yön gir...", null, (s, e) => SonaKurpDegerleEkle());
            var sadeSpiralMenu = new ToolStripMenuItem("Spiral");
            sadeSpiralMenu.DropDownItems.Add("Sona Spiral Ekle: Boy ve yarıçap (ya da A) gir...", null, (s, e) => SonaSpiralEkle());
            sadeSpiralMenu.DropDownItems.Add("Kurba Giriş/Çıkış Spirali Ekle (ekrandan kurp tıkla)", null, (s, e) => KomutuBaslat(EkranKomutu.SpiralKurbaEkle));
            var spiralMenu = new ToolStripMenuItem("Geçiş Eğrili Kurp");
            spiralMenu.DropDownItems.Add("Someye Geçiş Eğrili Kurp Yerleştir / Değiştir (ekrandan)", null, (s, e) => KomutuBaslat(EkranKomutu.SpiralliKurpYerlestir));
            var silMenu = new ToolStripMenuItem("Parça Sil");
            silMenu.DropDownItems.Add("Parça Sil (ekrandan)", null, (s, e) => KomutuBaslat(EkranKomutu.ParcaSil));
            silMenu.DropDownItems.Add("Seçili Satırları Sil", null, (s, e) => SecilenleriCikar(false));
            menu.Items.AddRange(new ToolStripItem[] { dogruMenu, kurpMenu, sadeSpiralMenu, spiralMenu, silMenu });

            // --- en alttaki araç çubuğu: parça işlemleri ---
            var araclar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Bottom };
            ToolStripButton Dugme(string metin, string ipucu, EventHandler tiklama)
            {
                var dugme = new ToolStripButton(metin) { DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = ipucu };
                dugme.Click += tiklama;
                araclar.Items.Add(dugme);
                return dugme;
            }

            var ekle = new ToolStripDropDownButton("Ekle") { ToolTipText = "Seçili parçadan sonra yeni parça ekler" };
            ekle.DropDownItems.Add("Doğru", null, (s, e) => ParcaEkle(GuzergahParcaTipi.Dogru));
            ekle.DropDownItems.Add("Kurp", null, (s, e) => ParcaEkle(GuzergahParcaTipi.Kurp));
            ekle.DropDownItems.Add("Spiral", null, (s, e) => ParcaEkle(GuzergahParcaTipi.Spiral));
            araclar.Items.Add(ekle);
            Dugme("Çıkar", "Seçili parçaları güzergahtan çıkarır", (s, e) => SecilenleriCikar(false));
            araclar.Items.Add(new ToolStripSeparator());
            Dugme("Kes", "Seçili parçaları çıkarır ve panoya alır", (s, e) => SecilenleriCikar(true));
            Dugme("Kopyala", "Seçili parçaları panoya kopyalar", (s, e) => SecilenleriKopyala());
            _yapistirDugmesi = Dugme("Yapıştır", "Panodaki parçaları seçili parçadan sonra ekler", (s, e) => Yapistir());
            araclar.Items.Add(new ToolStripSeparator());
            Dugme("Uzat", "Seçili parçanın uzunluğunu verilen miktar kadar artırır (kısaltmak için eksi)", (s, e) => Uzat());
            Dugme("Yukarı", "Seçili parçayı bir sıra öne alır", (s, e) => SiraDegistir(-1));
            Dugme("Aşağı", "Seçili parçayı bir sıra sona alır", (s, e) => SiraDegistir(1));
            araclar.Items.Add(new ToolStripSeparator());
            _geriAlDugmesi = Dugme("Geri Al", "Son değişikliği geri alır", (s, e) => GeriAl());

            // --- başlangıç değerleri ---
            var baslangicPaneli = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(6, 6, 0, 0), WrapContents = false };
            void Alan(string etiket, TextBox kutu)
            {
                baslangicPaneli.Controls.Add(new Label { Text = etiket, AutoSize = true, Margin = new Padding(6, 4, 2, 0) });
                kutu.Margin = new Padding(0, 0, 6, 0);
                baslangicPaneli.Controls.Add(kutu);
                kutu.Leave += (s, e) => BaslangiciUygula();
                kutu.KeyDown += (s, e) =>
                {
                    if (e.KeyCode != Keys.Enter)
                        return;
                    BaslangiciUygula();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                };
            }
            Alan("Ad:", _adKutusu);
            Alan("Başlangıç Km:", _kmKutusu);
            Alan("Başlangıç Y:", _yKutusu);
            Alan("X:", _xKutusu);
            Alan("Semt (grad):", _semtKutusu);

            // --- parça tablosu ---
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sıra", ReadOnly = true, FillWeight = 5 });
            var tipKolonu = new DataGridViewComboBoxColumn { HeaderText = "Tip", FillWeight = 8, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            tipKolonu.Items.AddRange(TipAdlari);
            _tablo.Columns.Add(tipKolonu);
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Uzunluk", FillWeight = 9 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Yarıçap (Baş.)", FillWeight = 10 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Yarıçap (Bitiş)", FillWeight = 10 });
            var yonKolonu = new DataGridViewComboBoxColumn { HeaderText = "Yön", FillWeight = 7, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            yonKolonu.Items.AddRange(SagMetni, SolMetni);
            _tablo.Columns.Add(yonKolonu);
            foreach (var baslik in new[] { "Baş. Km", "Bitiş Km", "Baş. Y", "Baş. X", "Bitiş Y", "Bitiş X" })
                _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = baslik, ReadOnly = true, FillWeight = 10 });
            foreach (DataGridViewColumn kolon in _tablo.Columns)
            {
                kolon.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (kolon.ReadOnly)
                    kolon.DefaultCellStyle.BackColor = Color.FromArgb(244, 244, 244);
                if (!(kolon is DataGridViewComboBoxColumn))
                    kolon.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // Açılır listelerdeki seçim, hücreden çıkmayı beklemeden uygulanır.
            _tablo.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_tablo.IsCurrentCellDirty && _tablo.CurrentCell is DataGridViewComboBoxCell)
                    _tablo.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _tablo.CellValueChanged += Tablo_CellValueChanged;
            _tablo.DataError += (s, e) => e.ThrowException = false;
            _tablo.SelectionChanged += (s, e) => VurguyuGuncelle();
            _tablo.CurrentCellChanged += (s, e) => VurguyuGuncelle();

            // Kapat ve Vazgeç, araç çubuğunun sağ ucundadır.
            var kapatDugmesi = new ToolStripButton("Kapat") { DisplayStyle = ToolStripItemDisplayStyle.Text, Alignment = ToolStripItemAlignment.Right, ToolTipText = "Değişiklikleri koruyarak kapatır" };
            var vazgecDugmesi = new ToolStripButton("Vazgeç") { DisplayStyle = ToolStripItemDisplayStyle.Text, Alignment = ToolStripItemAlignment.Right, ToolTipText = "Güzergahı pencere açıldığındaki haline döndürür" };
            kapatDugmesi.Click += (s, e) => Close();
            vazgecDugmesi.Click += (s, e) =>
            {
                _vazgecildi = true;
                Close();
            };
            araclar.Items.Add(kapatDugmesi);
            araclar.Items.Add(vazgecDugmesi);

            // --- araç çubuğunun üstünde: özet ya da çalışan komutun yönergesi ---
            var ozetPaneli = new Panel { Dock = DockStyle.Bottom, Height = 24 };
            ozetPaneli.Controls.Add(_ozetLabel);
            ozetPaneli.Controls.Add(_komutLabel);

            Controls.Add(_tablo);
            Controls.Add(ozetPaneli);
            Controls.Add(araclar);
            Controls.Add(baslangicPaneli);
            Controls.Add(menu);
            MainMenuStrip = menu;

            // Ekrandan komutlar ve tutamaçla sürükleme için çizim panelinin olayları dinlenir.
            _panel.NoktaTiklandi += Panel_NoktaTiklandi;
            _panel.NoktaAlmaBitti += Panel_NoktaAlmaBitti;
            _panel.TutamacSuruklendi += Panel_TutamacSuruklendi;
            _panel.UstCizim += Panel_UstCizim;
            _panel.MouseMove += Panel_MouseMove;

            FormClosed += GuzergahEditoruForm_FormClosed;

            Doldur(0);
        }

        // ------------------------------------------------------------------ biçimlendirme

        private static string Sayi(double deger)
        {
            return deger.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static string YaricapMetni(double yaricap)
        {
            return double.IsInfinity(yaricap) ? SonsuzMetni : Sayi(yaricap);
        }

        // "INF" (ya da boş) sonsuz yarıçap demektir; diğer girişler sıfırdan büyük bir sayı olmalıdır.
        private static bool YaricapOku(string metin, bool sonsuzOlabilir, out double yaricap)
        {
            metin = (metin ?? "").Trim();
            if (sonsuzOlabilir && (metin == "" || metin.Equals(SonsuzMetni, StringComparison.OrdinalIgnoreCase) || metin == "∞"))
            {
                yaricap = double.PositiveInfinity;
                return true;
            }
            return NcnDosyasi.SayiOku(metin, out yaricap) && yaricap > 0;
        }

        // Semt: kuzeyden saat yönünde, grad. Çizim açısı: +X'ten (doğu) saat yönünün tersine, radyan.
        private static double SemtGrad(double aciRadyan)
        {
            var semt = (Math.PI / 2 - aciRadyan) * 200.0 / Math.PI % 400.0;
            return semt < 0 ? semt + 400.0 : semt;
        }

        private static double AciRadyan(double semtGrad)
        {
            return Math.PI / 2 - semtGrad * Math.PI / 200.0;
        }

        // ------------------------------------------------------------------ tablo

        /// <summary>Güzergahı yeniden hesaplar, tabloyu ve başlangıç kutularını baştan doldurur, çizimi yeniler.</summary>
        private void Doldur(int secilecekSatir)
        {
            _guzergah.Hesapla();

            _dolduruluyor = true;
            _adKutusu.Text = _guzergah.Ad;
            _kmKutusu.Text = Sayi(_guzergah.BaslangicKm);
            _yKutusu.Text = Sayi(_guzergah.BaslangicX);
            _xKutusu.Text = Sayi(_guzergah.BaslangicY);
            _semtKutusu.Text = SemtGrad(_guzergah.BaslangicAcisi).ToString("0.0000", CultureInfo.InvariantCulture);

            _tablo.Rows.Clear();
            for (var i = 0; i < _guzergah.Parcalar.Count; i++)
            {
                _tablo.Rows.Add();
                SatiriYaz(i);
            }
            if (_tablo.Rows.Count > 0)
            {
                var satir = Math.Max(0, Math.Min(secilecekSatir, _tablo.Rows.Count - 1));
                _tablo.ClearSelection();
                _tablo.CurrentCell = _tablo.Rows[satir].Cells[UzunlukKolon];
            }
            _dolduruluyor = false;

            DurumuGuncelle();
            VurguyuGuncelle();
        }

        private void SatiriYaz(int i)
        {
            var parca = _guzergah.Parcalar[i];
            var satir = _tablo.Rows[i];
            var dogru = parca.Tip == GuzergahParcaTipi.Dogru;
            var kurp = parca.Tip == GuzergahParcaTipi.Kurp;

            satir.Cells[SiraKolon].Value = (i + 1).ToString();
            satir.Cells[TipKolon].Value = TipAdlari[(int)parca.Tip];
            satir.Cells[UzunlukKolon].Value = Sayi(parca.Uzunluk);
            satir.Cells[YaricapBasKolon].Value = dogru ? "" : YaricapMetni(parca.YaricapBaslangic);
            satir.Cells[YaricapBitKolon].Value = dogru ? "" : YaricapMetni(kurp ? parca.YaricapBaslangic : parca.YaricapBitis);
            satir.Cells[YonKolon].Value = dogru ? null : parca.Yon == DonusYonu.Sol ? SolMetni : SagMetni;
            satir.Cells[BasKmKolon].Value = Sayi(parca.BaslangicKm);
            satir.Cells[BitKmKolon].Value = Sayi(parca.BitisKm);
            // Tabloda Y sağa, X yukarı değerdir (NCN düzeni); çizimde bunlar sırasıyla X ve Y'dir.
            satir.Cells[BasYKolon].Value = Sayi(parca.BaslangicX);
            satir.Cells[BasXKolon].Value = Sayi(parca.BaslangicY);
            satir.Cells[BitYKolon].Value = Sayi(parca.BitisX);
            satir.Cells[BitXKolon].Value = Sayi(parca.BitisY);

            // Doğruda yarıçap ve yön yoktur; kurpta tek yarıçap vardır (bitiş kutusu başlangıcı gösterir).
            satir.Cells[YaricapBasKolon].ReadOnly = dogru;
            satir.Cells[YaricapBitKolon].ReadOnly = dogru || kurp;
            satir.Cells[YonKolon].ReadOnly = dogru;
        }

        /// <summary>Parça değerleri değiştikten sonra: yeniden hesaplar, bütün satırları yeniden yazar, çizimi yeniler.</summary>
        private void DegerlerDegisti()
        {
            _guzergah.Hesapla();
            _dolduruluyor = true;
            for (var i = 0; i < _guzergah.Parcalar.Count && i < _tablo.Rows.Count; i++)
                SatiriYaz(i);
            _dolduruluyor = false;
            DurumuGuncelle();
        }

        private void DurumuGuncelle()
        {
            _ozetLabel.Text = _guzergah.Parcalar.Count + " parça   |   Uzunluk: " + Sayi(_guzergah.ToplamUzunluk) +
                              " m   |   Km " + Sayi(_guzergah.BaslangicKm) + " - " + Sayi(_guzergah.BitisKm);
            _geriAlDugmesi.Enabled = _geriAlListesi.Count > 0;
            _yapistirDugmesi.Enabled = _pano.Count > 0;
            _panel.Invalidate();
        }

        // Tabloda üzerinde durulan parça çizimde vurgulanır.
        private void VurguyuGuncelle()
        {
            if (_dolduruluyor)
                return;
            var satir = _tablo.CurrentCell?.RowIndex ?? -1;
            if (_guzergah.VurguluParca == satir)
                return;
            _guzergah.VurguluParca = satir;
            _panel.Invalidate();
        }

        private List<int> SeciliSatirlar()
        {
            var satirlar = _tablo.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).ToList();
            if (satirlar.Count == 0 && _tablo.CurrentCell != null)
                satirlar.Add(_tablo.CurrentCell.RowIndex);
            satirlar.Sort();
            return satirlar;
        }

        private void GeriAlKaydet()
        {
            _geriAlListesi.Add(_guzergah.Kopyala());
            if (_geriAlListesi.Count > 100)
                _geriAlListesi.RemoveAt(0);
        }

        private void HaleDon(GuzergahNesnesi hal)
        {
            _guzergah.Ad = hal.Ad;
            _guzergah.BaslangicKm = hal.BaslangicKm;
            _guzergah.BaslangicX = hal.BaslangicX;
            _guzergah.BaslangicY = hal.BaslangicY;
            _guzergah.BaslangicAcisi = hal.BaslangicAcisi;
            _guzergah.Parcalar.Clear();
            _guzergah.Parcalar.AddRange(hal.Parcalar.Select(p => p.Kopyala(true)));
        }

        private void GeriAl()
        {
            if (_geriAlListesi.Count == 0)
                return;
            _tablo.CancelEdit();
            var satir = _tablo.CurrentCell?.RowIndex ?? 0;
            HaleDon(_geriAlListesi[_geriAlListesi.Count - 1]);
            _geriAlListesi.RemoveAt(_geriAlListesi.Count - 1);
            Doldur(satir);
        }

        // ------------------------------------------------------------------ hücre düzenleme

        private void Tablo_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_dolduruluyor || e.RowIndex < 0 || e.RowIndex >= _guzergah.Parcalar.Count)
                return;

            var parca = _guzergah.Parcalar[e.RowIndex];
            var metin = Convert.ToString(_tablo.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? "";
            var onceki = parca.Kopyala();
            string hata = null;

            switch (e.ColumnIndex)
            {
                case TipKolon:
                {
                    var tip = (GuzergahParcaTipi)Math.Max(0, Array.IndexOf(TipAdlari, metin));
                    if (tip == parca.Tip)
                        return;
                    parca.Tip = tip;
                    // Tipi değişen parçaya geçerli yarıçaplar verilir: kurpta tek yarıçap, spiralde doğrudan kurba geçiş.
                    var yaricap = !double.IsInfinity(onceki.YaricapBaslangic) ? onceki.YaricapBaslangic
                        : !double.IsInfinity(onceki.YaricapBitis) ? onceki.YaricapBitis : 500.0;
                    if (tip == GuzergahParcaTipi.Kurp)
                    {
                        parca.YaricapBaslangic = parca.YaricapBitis = yaricap;
                    }
                    else if (tip == GuzergahParcaTipi.Spiral)
                    {
                        parca.YaricapBaslangic = double.PositiveInfinity;
                        parca.YaricapBitis = yaricap;
                    }
                    break;
                }

                case UzunlukKolon:
                    if (NcnDosyasi.SayiOku(metin, out var uzunluk) && uzunluk > 0)
                        parca.Uzunluk = uzunluk;
                    else
                        hata = "Uzunluk sıfırdan büyük bir sayı olmalıdır.";
                    break;

                case YaricapBasKolon:
                    if (YaricapOku(metin, parca.Tip == GuzergahParcaTipi.Spiral, out var yaricapBas))
                    {
                        parca.YaricapBaslangic = yaricapBas;
                        if (parca.Tip == GuzergahParcaTipi.Kurp)
                            parca.YaricapBitis = yaricapBas;
                    }
                    else
                    {
                        hata = "Yarıçap sıfırdan büyük bir sayı olmalıdır" + (parca.Tip == GuzergahParcaTipi.Spiral ? " (doğruya bağlanan uç için INF)." : ".");
                    }
                    break;

                case YaricapBitKolon:
                    if (YaricapOku(metin, true, out var yaricapBit))
                        parca.YaricapBitis = yaricapBit;
                    else
                        hata = "Yarıçap sıfırdan büyük bir sayı olmalıdır (doğruya bağlanan uç için INF).";
                    break;

                case YonKolon:
                    parca.Yon = metin == SolMetni ? DonusYonu.Sol : DonusYonu.Sag;
                    break;

                default:
                    return;
            }

            if (hata == null && parca.Tip == GuzergahParcaTipi.Spiral && double.IsInfinity(parca.YaricapBaslangic) && double.IsInfinity(parca.YaricapBitis))
                hata = "Spiralin iki ucu da sonsuz (INF) yarıçaplı olamaz; en az bir ucuna yarıçap verin.";

            if (hata != null)
            {
                // Geçersiz giriş: parça eski haline döner, hücre eski değeriyle yeniden yazılır.
                parca.Tip = onceki.Tip;
                parca.Uzunluk = onceki.Uzunluk;
                parca.YaricapBaslangic = onceki.YaricapBaslangic;
                parca.YaricapBitis = onceki.YaricapBitis;
                parca.Yon = onceki.Yon;
                MessageBox.Show(hata, "Güzergah Editörü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BeginInvoke((Action)DegerlerDegisti);
                return;
            }

            // Geri al kaydı, parçanın değişiklikten önceki haliyle alınır.
            var simdiki = parca.Kopyala();
            parca.Tip = onceki.Tip; parca.Uzunluk = onceki.Uzunluk; parca.YaricapBaslangic = onceki.YaricapBaslangic; parca.YaricapBitis = onceki.YaricapBitis; parca.Yon = onceki.Yon;
            GeriAlKaydet();
            parca.Tip = simdiki.Tip; parca.Uzunluk = simdiki.Uzunluk; parca.YaricapBaslangic = simdiki.YaricapBaslangic; parca.YaricapBitis = simdiki.YaricapBitis; parca.Yon = simdiki.Yon;

            // Kurbun yarıçapı ya da yönü değişince ona bağlanan spirallerin kurp tarafındaki yarıçapı ve yönü de
            // değişir; yoksa birleşim yerinde eğrilik sıçrar.
            var ilkDegisen = e.RowIndex + 1;
            if (parca.Tip == GuzergahParcaTipi.Kurp && (e.ColumnIndex == YaricapBasKolon || e.ColumnIndex == YonKolon))
            {
                var oncekiParca = e.RowIndex > 0 ? _guzergah.Parcalar[e.RowIndex - 1] : null;
                var sonrakiParca = e.RowIndex + 1 < _guzergah.Parcalar.Count ? _guzergah.Parcalar[e.RowIndex + 1] : null;
                if (oncekiParca != null && oncekiParca.Tip == GuzergahParcaTipi.Spiral && !double.IsInfinity(oncekiParca.YaricapBitis))
                {
                    oncekiParca.YaricapBitis = parca.YaricapBaslangic;
                    oncekiParca.Yon = parca.Yon;
                    // Giriş spiralinin bitişi (kurbun başlangıcı) da yer değiştirir.
                    ilkDegisen = e.RowIndex;
                }
                if (sonrakiParca != null && sonrakiParca.Tip == GuzergahParcaTipi.Spiral && !double.IsInfinity(sonrakiParca.YaricapBaslangic))
                {
                    sonrakiParca.YaricapBaslangic = parca.YaricapBaslangic;
                    sonrakiParca.Yon = parca.Yon;
                }
            }

            // Değişen parçadan sonrakiler artık dosyadaki yerlerinde duramaz; uç uca yeniden hesaplanırlar.
            _guzergah.SabitleriBirak(ilkDegisen);

            // Hücre olayı sürerken satırlar yeniden yazılamaz; yazma olay bittikten sonraya bırakılır.
            _guzergah.Hesapla();
            _panel.Invalidate();
            BeginInvoke((Action)DegerlerDegisti);
        }

        private void BaslangiciUygula()
        {
            if (_dolduruluyor)
                return;

            if (!NcnDosyasi.SayiOku(_kmKutusu.Text, out var km) || !NcnDosyasi.SayiOku(_yKutusu.Text, out var y)
                || !NcnDosyasi.SayiOku(_xKutusu.Text, out var x) || !NcnDosyasi.SayiOku(_semtKutusu.Text, out var semt))
            {
                // Geçersiz giriş: kutular güzergahtaki değerlerle yeniden yazılır.
                Doldur(_tablo.CurrentCell?.RowIndex ?? 0);
                return;
            }

            var yeniAci = AciRadyan(semt);
            var degisti = _adKutusu.Text != _guzergah.Ad || Math.Abs(km - _guzergah.BaslangicKm) > 1e-9
                          || Math.Abs(y - _guzergah.BaslangicX) > 1e-9 || Math.Abs(x - _guzergah.BaslangicY) > 1e-9
                          || Math.Abs(semt - SemtGrad(_guzergah.BaslangicAcisi)) > 5e-5;
            if (!degisti)
                return;

            GeriAlKaydet();
            // Başlangıç noktası ya da semti değişince bütün parçalar yeni başlangıçtan uç uca hesaplanır.
            if (Math.Abs(y - _guzergah.BaslangicX) > 1e-9 || Math.Abs(x - _guzergah.BaslangicY) > 1e-9 || Math.Abs(semt - SemtGrad(_guzergah.BaslangicAcisi)) > 5e-5)
                _guzergah.SabitleriBirak(0);
            _guzergah.Ad = _adKutusu.Text;
            _guzergah.BaslangicKm = km;
            _guzergah.BaslangicX = y;
            _guzergah.BaslangicY = x;
            if (Math.Abs(semt - SemtGrad(_guzergah.BaslangicAcisi)) > 5e-5)
                _guzergah.BaslangicAcisi = yeniAci;
            DegerlerDegisti();
        }

        // ------------------------------------------------------------------ araç çubuğu komutları

        private void ParcaEkle(GuzergahParcaTipi tip)
        {
            _tablo.EndEdit();
            GeriAlKaydet();

            // Yeni parça seçili parçadan sonra eklenir; yön ve yarıçap için komşu parçadan başlangıç değeri alınır.
            var satirlar = SeciliSatirlar();
            var yer = satirlar.Count > 0 ? satirlar[satirlar.Count - 1] + 1 : _guzergah.Parcalar.Count;
            var komsu = yer > 0 ? _guzergah.Parcalar[yer - 1] : null;
            var yaricap = komsu != null && !double.IsInfinity(komsu.YaricapBitis) && komsu.Tip != GuzergahParcaTipi.Dogru ? komsu.YaricapBitis
                : komsu != null && !double.IsInfinity(komsu.YaricapBaslangic) && komsu.Tip != GuzergahParcaTipi.Dogru ? komsu.YaricapBaslangic : 500.0;

            var parca = new GuzergahParcasi { Tip = tip, Uzunluk = tip == GuzergahParcaTipi.Spiral ? 50 : 100, Yon = komsu?.Yon ?? DonusYonu.Sag };
            if (tip == GuzergahParcaTipi.Kurp)
            {
                parca.YaricapBaslangic = parca.YaricapBitis = yaricap;
            }
            else if (tip == GuzergahParcaTipi.Spiral)
            {
                // Kurptan sonra eklenen spiral kurptan doğruya, diğer durumda doğrudan kurba geçer.
                var kurptanCikis = komsu != null && komsu.Tip == GuzergahParcaTipi.Kurp;
                parca.YaricapBaslangic = kurptanCikis ? yaricap : double.PositiveInfinity;
                parca.YaricapBitis = kurptanCikis ? double.PositiveInfinity : yaricap;
            }

            _guzergah.SabitleriBirak(yer);
            _guzergah.Parcalar.Insert(yer, parca);
            Doldur(yer);
        }

        private void SecilenleriKopyala()
        {
            _tablo.EndEdit();
            var satirlar = SeciliSatirlar();
            if (satirlar.Count == 0)
                return;
            _pano.Clear();
            _pano.AddRange(satirlar.Select(i => _guzergah.Parcalar[i].Kopyala()));
            DurumuGuncelle();
        }

        private void SecilenleriCikar(bool panoyaAl)
        {
            _tablo.EndEdit();
            var satirlar = SeciliSatirlar();
            if (satirlar.Count == 0)
                return;

            if (panoyaAl)
            {
                _pano.Clear();
                _pano.AddRange(satirlar.Select(i => _guzergah.Parcalar[i].Kopyala()));
            }

            GeriAlKaydet();
            _guzergah.SabitleriBirak(satirlar[0]);
            for (var k = satirlar.Count - 1; k >= 0; k--)
                _guzergah.Parcalar.RemoveAt(satirlar[k]);
            Doldur(satirlar[0]);
        }

        private void Yapistir()
        {
            _tablo.EndEdit();
            if (_pano.Count == 0)
                return;

            GeriAlKaydet();
            var satirlar = SeciliSatirlar();
            var yer = satirlar.Count > 0 ? satirlar[satirlar.Count - 1] + 1 : _guzergah.Parcalar.Count;
            _guzergah.SabitleriBirak(yer);
            _guzergah.Parcalar.InsertRange(yer, _pano.Select(p => p.Kopyala()));
            Doldur(yer);
        }

        private void Uzat()
        {
            _tablo.EndEdit();
            var satirlar = SeciliSatirlar();
            if (satirlar.Count == 0)
                return;

            double miktar;
            using (var dlg = new Form
            {
                Text = "Uzat",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(300, 90)
            })
            {
                dlg.Controls.Add(new Label { Text = "Uzatma miktarı (m):", Location = new Point(12, 17), AutoSize = true });
                var kutu = new TextBox { Text = "10", Location = new Point(150, 14), Size = new Size(138, 20) };
                var tamam = new Button { Text = "Tamam", Location = new Point(132, 52), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(213, 52), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { kutu, tamam, iptal });
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                if (!NcnDosyasi.SayiOku(kutu.Text, out miktar))
                {
                    MessageBox.Show("Lütfen geçerli bir sayı girin.", "Uzat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (satirlar.Any(i => _guzergah.Parcalar[i].Uzunluk + miktar <= 0))
            {
                MessageBox.Show("Bu kadar kısaltılınca parçanın uzunluğu sıfırın altına düşüyor.", "Uzat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GeriAlKaydet();
            _guzergah.SabitleriBirak(satirlar[0] + 1);
            foreach (var i in satirlar)
                _guzergah.Parcalar[i].Uzunluk += miktar;
            DegerlerDegisti();
        }

        private void SiraDegistir(int yon)
        {
            _tablo.EndEdit();
            var satir = _tablo.CurrentCell?.RowIndex ?? -1;
            var hedef = satir + yon;
            if (satir < 0 || hedef < 0 || hedef >= _guzergah.Parcalar.Count)
                return;

            GeriAlKaydet();
            _guzergah.SabitleriBirak(Math.Min(satir, hedef));
            var parca = _guzergah.Parcalar[satir];
            _guzergah.Parcalar.RemoveAt(satir);
            _guzergah.Parcalar.Insert(hedef, parca);
            Doldur(hedef);
        }

        // ------------------------------------------------------------------ ekrandan komutlar

        /// <summary>Boş güzergahta doğrudan çizime başlamak için "Some Noktalarıyla Çiz" komutunu başlatır.</summary>
        public void CizimeBasla()
        {
            KomutuBaslat(EkranKomutu.SomeCiz);
        }

        private void KomutuBaslat(EkranKomutu komut)
        {
            _tablo.EndEdit();
            if (komut != EkranKomutu.SomeCiz && _guzergah.Parcalar.Count == 0)
            {
                MessageBox.Show("Güzergah boş. Önce Doğru menüsündeki \"Some Noktalarıyla Çiz\" ile güzergahı çizin.", "Güzergah Editörü", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var sonaEkleme = komut == EkranKomutu.KurpTegetNokta || komut == EkranKomutu.KurpYaricapNokta || komut == EkranKomutu.KurpUcNokta;
            if (komut == EkranKomutu.KurpYaricapNokta)
            {
                if (!DegerleriSor("Sona Kurp Ekle", new[] { "Yarıçap R (m):" }, new[] { _sonYaricap }, out var degerler) || degerler[0] <= 0)
                    return;
                _komutYaricapi = _sonYaricap = degerler[0];
            }
            if (!sonaEkleme && komut != EkranKomutu.SomeCiz && komut != EkranKomutu.ParcaSil && !_guzergah.SomeModeliGecerli)
            {
                MessageBox.Show("Bu güzergahta doğrultuları kesişmeyen (paralel ya da 180°'den fazla dönen) bir kurp grubu var; some noktalarıyla düzenlenemiyor. Tablodan düzenleyin.",
                    "Güzergah Editörü", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _komut = komut;
            _ilkNokta = null;
            _araNokta = null;
            switch (komut)
            {
                case EkranKomutu.SomeCiz:
                    _komutLabel.Text = _guzergah.Parcalar.Count == 0
                        ? "Güzergahın başlangıç noktasını, sonra sırayla some noktalarını tıklayın. Bitirmek için sağ tık ya da Esc."
                        : "Güzergahın sonuna eklenecek noktaları sırayla tıklayın. Bitirmek için sağ tık ya da Esc.";
                    break;
                case EkranKomutu.SomeEkle: _komutLabel.Text = "Yeni somenin yerini tıklayın (en yakın kenara eklenir). Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.KurpTegetNokta: _komutLabel.Text = "Kurbun bitiş noktasını tıklayın (güzergahın sonuna teğet eklenir). Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.KurpYaricapNokta: _komutLabel.Text = "R=" + Sayi(_komutYaricapi) + " kurbun biteceği yönü tıklayın. Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.KurpUcNokta: _komutLabel.Text = "Kurbun bitiş noktasını, sonra üzerinden geçeceği noktayı tıklayın. Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.SpiralKurbaEkle: _komutLabel.Text = "Spiral eklenecek kurbu tıklayın. Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.SomeSil: _komutLabel.Text = "Silinecek some noktasını (üçgen) tıklayın. Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.KurpYerlestir: _komutLabel.Text = "Kurp yerleştirilecek some noktasını (üçgen) tıklayın. Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.SpiralliKurpYerlestir: _komutLabel.Text = "Geçiş eğrili kurp yerleştirilecek some noktasını (üçgen) tıklayın. Bitirmek için sağ tık ya da Esc."; break;
                case EkranKomutu.ParcaSil: _komutLabel.Text = "Silinecek parçayı tıklayın (kurp/spiral tıklanırsa o somenin kurbu kaldırılır). Bitirmek için sağ tık ya da Esc."; break;
            }
            _komutLabel.Visible = true;
            _ozetLabel.Visible = false;

            // Some noktaları görünsün diye güzergah vurgulu duruma alınır; çizim paneli nokta bekler.
            if (_guzergah.VurguluParca < 0)
                _guzergah.VurguluParca = Math.Max(0, _tablo.CurrentCell?.RowIndex ?? 0);
            // Some, parça ya da kurp tıklatan komutlarda imleç kare (obje seçimi), nokta verilenlerde artıdır.
            _panel.NoktaAlmayaBasla(komut == EkranKomutu.SomeSil || komut == EkranKomutu.KurpYerlestir || komut == EkranKomutu.SpiralliKurpYerlestir
                                    || komut == EkranKomutu.ParcaSil || komut == EkranKomutu.SpiralKurbaEkle);
            _panel.Focus();
            _panel.Invalidate();
        }

        private void KomutuBitir()
        {
            if (_komut == EkranKomutu.Yok)
                return;
            _komut = EkranKomutu.Yok;
            _ilkNokta = null;
            _komutLabel.Visible = false;
            _ozetLabel.Visible = true;
            _panel.Invalidate();
        }

        private void Panel_NoktaAlmaBitti(object sender, EventArgs e)
        {
            KomutuBitir();
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            // Çizim sırasında son noktadan fareye giden lastik çizgi güncellensin.
            if (_komut == EkranKomutu.SomeCiz)
                _panel.Invalidate();
        }

        private void Panel_UstCizim(object sender, PaintEventArgs e)
        {
            // Çizim panelinde başka bir araca geçildiyse bekleyen komut biter.
            if (_komut != EkranKomutu.Yok && _panel.AktifArac != CizimAraci.NoktaAl)
            {
                BeginInvoke((Action)KomutuBitir);
                return;
            }
            if (_komut != EkranKomutu.SomeCiz)
                return;

            double[] son = null;
            if (_guzergah.Parcalar.Count > 0)
            {
                var sonParca = _guzergah.Parcalar[_guzergah.Parcalar.Count - 1];
                son = new[] { sonParca.BitisX, sonParca.BitisY };
            }
            else if (_ilkNokta != null)
            {
                son = _ilkNokta;
            }
            if (son == null)
                return;

            var ekran = _panel.Gorunum.DunyadanEkrana(son[0], son[1], _panel.Width, _panel.Height);
            using (var kalem = new Pen(Color.Firebrick, 1.2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                e.Graphics.DrawLine(kalem, ekran, _panel.PointToClient(Cursor.Position));
                e.Graphics.DrawRectangle(kalem, ekran.X - 4, ekran.Y - 4, 8, 8);
            }
        }

        // Tıklanan yere ekranda en çok 10 piksel uzaklıktaki some noktasının sırası (0'dan); yoksa -1.
        private int SomeBul(double x, double y)
        {
            var tolerans = 10 / _panel.Gorunum.Olcek;
            int enYakin = -1;
            var enYakinKare = tolerans * tolerans;
            for (var i = 0; i < _guzergah.Someler.Count; i++)
            {
                double dx = _guzergah.Someler[i].X - x, dy = _guzergah.Someler[i].Y - y;
                if (dx * dx + dy * dy <= enYakinKare)
                {
                    enYakinKare = dx * dx + dy * dy;
                    enYakin = i;
                }
            }
            return enYakin;
        }

        private void Panel_NoktaTiklandi(double x, double y)
        {
            if (_komut == EkranKomutu.Yok)
                return;

            var onceki = _guzergah.Kopyala();
            string hata = null;
            var degisti = false;
            var parcaToleransi = 8 / _panel.Gorunum.Olcek;

            switch (_komut)
            {
                case EkranKomutu.SomeCiz:
                    if (_guzergah.Parcalar.Count == 0)
                    {
                        if (_ilkNokta == null)
                        {
                            _ilkNokta = new[] { x, y };
                        }
                        else
                        {
                            degisti = _guzergah.SomeModelindenKur(new List<double[]> { _ilkNokta, new[] { x, y } }, new List<List<GuzergahParcasi>>(), out hata);
                            if (degisti)
                                _ilkNokta = null;
                        }
                    }
                    else if (_guzergah.SomeModeliAl(out var noktalar, out var gruplar))
                    {
                        // Eski bitiş noktası kurpsuz bir some olur, tıklanan yer yeni bitiş noktasıdır.
                        gruplar.Add(new List<GuzergahParcasi>());
                        noktalar.Add(new[] { x, y });
                        degisti = _guzergah.SomeModelindenKur(noktalar, gruplar, out hata);
                    }
                    else
                    {
                        hata = "Bu güzergah some noktalarıyla düzenlenemiyor.";
                    }
                    break;

                case EkranKomutu.SomeEkle:
                {
                    if (!_guzergah.SomeModeliAl(out var noktalar, out var gruplar))
                        break;

                    // Yeni some, noktaları birleştiren kırık çizginin tıklanan yere en yakın kenarına eklenir.
                    int kenar = -1;
                    var enKucuk = double.MaxValue;
                    for (var i = 0; i + 1 < noktalar.Count; i++)
                    {
                        double ax = noktalar[i][0], ay = noktalar[i][1], kx = noktalar[i + 1][0] - ax, ky = noktalar[i + 1][1] - ay;
                        var boyKare = kx * kx + ky * ky;
                        var t = boyKare < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((x - ax) * kx + (y - ay) * ky) / boyKare));
                        var uzaklik = Math.Pow(ax + t * kx - x, 2) + Math.Pow(ay + t * ky - y, 2);
                        if (uzaklik < enKucuk)
                        {
                            enKucuk = uzaklik;
                            kenar = i;
                        }
                    }
                    if (kenar < 0)
                        break;

                    noktalar.Insert(kenar + 1, new[] { x, y });
                    gruplar.Insert(kenar, new List<GuzergahParcasi>());
                    degisti = _guzergah.SomeModelindenKur(noktalar, gruplar, out hata);
                    break;
                }

                case EkranKomutu.KurpTegetNokta:
                case EkranKomutu.KurpYaricapNokta:
                case EkranKomutu.KurpUcNokta:
                {
                    var sonParca = _guzergah.Parcalar[_guzergah.Parcalar.Count - 1];
                    double sx = sonParca.BitisX, sy = sonParca.BitisY, aci = sonParca.BitisAcisi;
                    double cx = x - sx, cy = y - sy, kiris = Math.Sqrt(cx * cx + cy * cy);
                    if (kiris < 1e-6)
                        break;

                    if (_komut == EkranKomutu.KurpTegetNokta)
                    {
                        // Başlangıçta teğet olup tıklanan noktadan geçen tek bir yay vardır.
                        var alfa = AciFarki(Math.Atan2(cy, cx) - aci);
                        if (Math.Abs(alfa) < 1e-6 || Math.Abs(alfa) > Math.PI - 1e-6)
                        {
                            hata = "Tıklanan nokta güzergahın doğrultusu üzerinde; kurp oluşmuyor.";
                            break;
                        }
                        var yaricap = kiris / (2 * Math.Abs(Math.Sin(alfa)));
                        degisti = SonaParcaEkle(new GuzergahParcasi { Tip = GuzergahParcaTipi.Kurp, Uzunluk = yaricap * 2 * Math.Abs(alfa), YaricapBaslangic = yaricap, YaricapBitis = yaricap, Yon = alfa > 0 ? DonusYonu.Sol : DonusYonu.Sag });
                    }
                    else if (_komut == EkranKomutu.KurpYaricapNokta)
                    {
                        // Yarıçap belli: kurp tıklanan tarafa döner ve merkezden tıklanan yere bakan doğrultuda biter.
                        var sol = Math.Cos(aci) * cy - Math.Sin(aci) * cx > 0;
                        double yon = sol ? 1 : -1, r = _komutYaricapi;
                        double mx = sx - yon * r * Math.Sin(aci), my = sy + yon * r * Math.Cos(aci);
                        var donus = yon * AciFarki(Math.Atan2(y - my, x - mx) - Math.Atan2(sy - my, sx - mx));
                        if (donus <= 0)
                            donus += 2 * Math.PI;
                        if (donus < 1e-6)
                            break;
                        degisti = SonaParcaEkle(new GuzergahParcasi { Tip = GuzergahParcaTipi.Kurp, Uzunluk = r * donus, YaricapBaslangic = r, YaricapBitis = r, Yon = sol ? DonusYonu.Sol : DonusYonu.Sag });
                    }
                    else if (_araNokta == null)
                    {
                        _araNokta = new[] { x, y };
                        _komutLabel.Text = "Şimdi kurbun üzerinden geçeceği noktayı tıklayın.";
                    }
                    else
                    {
                        // Üç noktadan (güzergahın sonu, tıklanan ara nokta, bitiş) geçen yay. Güzergaha teğet olmayabilir;
                        // aradaki açı farkı kurbun başında kırıklık olarak saklanır.
                        double ex = _araNokta[0], ey = _araNokta[1];
                        _araNokta = null;
                        _komutLabel.Text = "Kurbun bitiş noktasını, sonra üzerinden geçeceği noktayı tıklayın. Bitirmek için sağ tık ya da Esc.";
                        double payda = 2 * (sx * (y - ey) + x * (ey - sy) + ex * (sy - y));
                        if (Math.Abs(payda) < 1e-9)
                        {
                            hata = "Üç nokta aynı doğru üzerinde; kurp oluşmuyor.";
                            break;
                        }
                        double s2 = sx * sx + sy * sy, m2 = x * x + y * y, e2 = ex * ex + ey * ey;
                        double mx = (s2 * (y - ey) + m2 * (ey - sy) + e2 * (sy - y)) / payda, my = (s2 * (ex - x) + m2 * (sx - ex) + e2 * (x - sx)) / payda;
                        var r = Math.Sqrt((sx - mx) * (sx - mx) + (sy - my) * (sy - my));
                        // Dönüş yönü: baştan ara noktaya, oradan bitişe giderken sola mı sağa mı dönülüyor.
                        var sol = (x - sx) * (ey - y) - (y - sy) * (ex - x) > 0;
                        double yon = sol ? 1 : -1;
                        var donus = yon * AciFarki(Math.Atan2(ey - my, ex - mx) - Math.Atan2(sy - my, sx - mx));
                        if (donus <= 0)
                            donus += 2 * Math.PI;
                        var baslangicDogrultusu = Math.Atan2(sy - my, sx - mx) + yon * Math.PI / 2;
                        degisti = SonaParcaEkle(new GuzergahParcasi
                        {
                            Tip = GuzergahParcaTipi.Kurp, Uzunluk = r * donus, YaricapBaslangic = r, YaricapBitis = r, Yon = sol ? DonusYonu.Sol : DonusYonu.Sag,
                            KirikAcisi = AciFarki(baslangicDogrultusu - aci)
                        });
                    }
                    break;
                }

                case EkranKomutu.SpiralKurbaEkle:
                {
                    var parcaNo = _guzergah.ParcaBul(x, y, parcaToleransi);
                    if (parcaNo < 0)
                        break;
                    var some = _guzergah.Someler.FindIndex(sm => sm.ParcaSiralari.Contains(parcaNo));
                    if (_guzergah.Parcalar[parcaNo].Tip != GuzergahParcaTipi.Kurp || some < 0 || !_guzergah.SomeModeliAl(out var noktalar, out var gruplar))
                    {
                        hata = "Bir some noktasına bağlı kurbu tıklayın.";
                        break;
                    }

                    var kurp = _guzergah.Parcalar[parcaNo];
                    var mevcutGiris = gruplar[some].FirstOrDefault(p => p.Tip == GuzergahParcaTipi.Spiral && double.IsInfinity(p.YaricapBaslangic));
                    var mevcutCikis = gruplar[some].LastOrDefault(p => p.Tip == GuzergahParcaTipi.Spiral && double.IsInfinity(p.YaricapBitis));
                    if (!DegerleriSor("Kurba Spiral Ekle (R=" + Sayi(kurp.YaricapBaslangic) + ")", new[] { "Giriş spirali boyu L1 (m, 0: yok):", "Çıkış spirali boyu L2 (m, 0: yok):" },
                            new[] { mevcutGiris?.Uzunluk ?? _sonGirisSpirali, mevcutCikis?.Uzunluk ?? _sonCikisSpirali }, out var boylar) || boylar[0] < 0 || boylar[1] < 0)
                        break;
                    _sonGirisSpirali = boylar[0];
                    _sonCikisSpirali = boylar[1];

                    var r = kurp.YaricapBaslangic;
                    var grup = new List<GuzergahParcasi>();
                    if (boylar[0] > 0)
                        grup.Add(new GuzergahParcasi { Tip = GuzergahParcaTipi.Spiral, Uzunluk = boylar[0], YaricapBaslangic = double.PositiveInfinity, YaricapBitis = r });
                    grup.Add(new GuzergahParcasi { Tip = GuzergahParcaTipi.Kurp, Uzunluk = 1, YaricapBaslangic = r, YaricapBitis = r });
                    if (boylar[1] > 0)
                        grup.Add(new GuzergahParcasi { Tip = GuzergahParcaTipi.Spiral, Uzunluk = boylar[1], YaricapBaslangic = r, YaricapBitis = double.PositiveInfinity });
                    gruplar[some] = grup;
                    degisti = _guzergah.SomeModelindenKur(noktalar, gruplar, out hata);
                    break;
                }

                case EkranKomutu.SomeSil:
                {
                    var some = SomeBul(x, y);
                    if (some < 0 || !_guzergah.SomeModeliAl(out var noktalar, out var gruplar))
                        break;
                    noktalar.RemoveAt(some + 1);
                    gruplar.RemoveAt(some);
                    degisti = _guzergah.SomeModelindenKur(noktalar, gruplar, out hata);
                    break;
                }

                case EkranKomutu.KurpYerlestir:
                case EkranKomutu.SpiralliKurpYerlestir:
                {
                    var some = SomeBul(x, y);
                    if (some < 0 || !_guzergah.SomeModeliAl(out var noktalar, out var gruplar))
                        break;

                    // Somede zaten kurp varsa pencere onun değerleriyle açılır.
                    var mevcutKurp = gruplar[some].FirstOrDefault(p => p.Tip == GuzergahParcaTipi.Kurp);
                    var mevcutGiris = gruplar[some].FirstOrDefault(p => p.Tip == GuzergahParcaTipi.Spiral && double.IsInfinity(p.YaricapBaslangic));
                    var mevcutCikis = gruplar[some].LastOrDefault(p => p.Tip == GuzergahParcaTipi.Spiral && double.IsInfinity(p.YaricapBitis));
                    var spiralli = _komut == EkranKomutu.SpiralliKurpYerlestir;
                    if (!KurpDegerleriniSor(spiralli, mevcutKurp?.YaricapBaslangic ?? _sonYaricap, mevcutGiris?.Uzunluk ?? _sonGirisSpirali, mevcutCikis?.Uzunluk ?? _sonCikisSpirali,
                            out var yaricap, out var girisBoyu, out var cikisBoyu))
                        break;
                    _sonYaricap = yaricap;
                    if (spiralli)
                    {
                        _sonGirisSpirali = girisBoyu;
                        _sonCikisSpirali = cikisBoyu;
                    }

                    var grup = new List<GuzergahParcasi>();
                    // Yalnızca yarıçap değiştirilirken somedeki mevcut spiraller boylarıyla korunur ve yeni yarıçapa bağlanır.
                    if (!spiralli)
                    {
                        girisBoyu = mevcutGiris?.Uzunluk ?? 0;
                        cikisBoyu = mevcutCikis?.Uzunluk ?? 0;
                    }
                    if (girisBoyu > 0)
                        grup.Add(new GuzergahParcasi { Tip = GuzergahParcaTipi.Spiral, Uzunluk = girisBoyu, YaricapBaslangic = double.PositiveInfinity, YaricapBitis = yaricap });
                    grup.Add(new GuzergahParcasi { Tip = GuzergahParcaTipi.Kurp, Uzunluk = 1, YaricapBaslangic = yaricap, YaricapBitis = yaricap });
                    if (cikisBoyu > 0)
                        grup.Add(new GuzergahParcasi { Tip = GuzergahParcaTipi.Spiral, Uzunluk = cikisBoyu, YaricapBaslangic = yaricap, YaricapBitis = double.PositiveInfinity });
                    gruplar[some] = grup;
                    degisti = _guzergah.SomeModelindenKur(noktalar, gruplar, out hata);
                    break;
                }

                case EkranKomutu.ParcaSil:
                {
                    var parcaNo = _guzergah.ParcaBul(x, y, parcaToleransi);
                    if (parcaNo < 0)
                        break;

                    // Kurp ya da spiral tıklandıysa o somenin bütün kurp parçaları kaldırılır; some yerinde kurpsuz kalır.
                    var some = _guzergah.Someler.FindIndex(sm => sm.ParcaSiralari.Contains(parcaNo));
                    if (_guzergah.Parcalar[parcaNo].Tip != GuzergahParcaTipi.Dogru && some >= 0 && _guzergah.SomeModeliAl(out var noktalar, out var gruplar))
                    {
                        gruplar[some] = new List<GuzergahParcasi>();
                        degisti = _guzergah.SomeModelindenKur(noktalar, gruplar, out hata);
                        break;
                    }

                    _guzergah.SabitleriBirak(parcaNo);
                    _guzergah.Parcalar.RemoveAt(parcaNo);
                    _guzergah.Hesapla();
                    degisti = true;
                    break;
                }
            }

            if (hata != null)
                MessageBox.Show(hata, "Güzergah Editörü", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            if (degisti)
            {
                _geriAlListesi.Add(onceki);
                _guzergah.SabitleriBirak(0);
                Doldur(_tablo.CurrentCell?.RowIndex ?? 0);
                if (_guzergah.VurguluParca < 0)
                    _guzergah.VurguluParca = 0;
                if (_komut == EkranKomutu.SomeCiz)
                    _komutLabel.Text = "Güzergahın sonuna eklenecek noktaları sırayla tıklayın. Bitirmek için sağ tık ya da Esc.";
            }
            _panel.Invalidate();
        }

        private static double AciFarki(double aci)
        {
            while (aci > Math.PI) aci -= 2 * Math.PI;
            while (aci <= -Math.PI) aci += 2 * Math.PI;
            return aci;
        }

        // Güzergahın sonuna bir parça ekler ve yeniden hesaplar.
        private bool SonaParcaEkle(GuzergahParcasi parca)
        {
            if (parca.Uzunluk < 1e-6 || double.IsNaN(parca.Uzunluk))
                return false;
            _guzergah.Parcalar.Add(parca);
            _guzergah.Hesapla();
            return true;
        }

        // Güzergahın sonundaki yarıçap (doğruda ve boş güzergahta sonsuz) ve dönüş yönü.
        private void SonDurum(out double yaricap, out DonusYonu yon, out bool yonBelli)
        {
            yaricap = double.PositiveInfinity;
            yon = DonusYonu.Sag;
            yonBelli = false;
            if (_guzergah.Parcalar.Count == 0)
                return;
            var son = _guzergah.Parcalar[_guzergah.Parcalar.Count - 1];
            if (son.Tip == GuzergahParcaTipi.Dogru)
                return;
            yaricap = son.Tip == GuzergahParcaTipi.Kurp ? son.YaricapBaslangic : son.YaricapBitis;
            yon = son.Yon;
            yonBelli = !double.IsInfinity(yaricap);
        }

        /// <summary>Sona, değerleri pencereden girilen (yarıçap, boy, yön) bir kurp ekler.</summary>
        private void SonaKurpDegerleEkle()
        {
            _tablo.EndEdit();
            if (_guzergah.Parcalar.Count == 0)
            {
                MessageBox.Show("Güzergah boş. Önce Doğru menüsünden güzergahın ilk doğrusunu çizin.", "Güzergah Editörü", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!DegerleriSor("Sona Kurp Ekle", new[] { "Yarıçap R (m):", "Boy L (m):", "Yön (1: sağ, 2: sol):" }, new[] { _sonYaricap, 100, 1 }, out var d) || d[0] <= 0 || d[1] <= 0)
                return;

            _sonYaricap = d[0];
            GeriAlKaydet();
            SonaParcaEkle(new GuzergahParcasi { Tip = GuzergahParcaTipi.Kurp, Uzunluk = d[1], YaricapBaslangic = d[0], YaricapBitis = d[0], Yon = d[2] == 2 ? DonusYonu.Sol : DonusYonu.Sag });
            Doldur(_guzergah.Parcalar.Count - 1);
        }

        /// <summary>
        /// Sona spiral ekler. Spiral, güzergahın sonundaki yarıçaptan (doğruda sonsuz) başlar; bitiş yarıçapı doğrudan
        /// ya da A parametresiyle (R = A² / L) verilir. Bitiş yarıçapı 0 girilirse spiral doğruya (sonsuza) açılır.
        /// </summary>
        private void SonaSpiralEkle()
        {
            _tablo.EndEdit();
            if (_guzergah.Parcalar.Count == 0)
            {
                MessageBox.Show("Güzergah boş. Önce Doğru menüsünden güzergahın ilk doğrusunu çizin.", "Güzergah Editörü", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SonDurum(out var baslangicYaricapi, out var yon, out var yonBelli);
            var baslik = "Sona Spiral Ekle (başlangıç yarıçapı: " + YaricapMetni(baslangicYaricapi) + ")";
            var etiketler = new List<string> { "Boy L (m):", "Bitiş yarıçapı R (m, 0: doğruya açılır):", "ya da parametre A (0: kullanma):" };
            var oneriler = new List<double> { _sonGirisSpirali, yonBelli ? 0 : _sonYaricap, 0 };
            if (!yonBelli)
            {
                etiketler.Add("Yön (1: sağ, 2: sol):");
                oneriler.Add(1);
            }
            if (!DegerleriSor(baslik, etiketler.ToArray(), oneriler.ToArray(), out var d) || d[0] <= 0 || d[1] < 0 || d[2] < 0)
                return;

            var bitisYaricapi = d[2] > 0 ? d[2] * d[2] / d[0] : d[1] > 0 ? d[1] : double.PositiveInfinity;
            if (double.IsInfinity(baslangicYaricapi) && double.IsInfinity(bitisYaricapi))
            {
                MessageBox.Show("Güzergah doğruyla bitiyor; spiralin bitiş yarıçapını (ya da A parametresini) verin.", "Sona Spiral Ekle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _sonGirisSpirali = d[0];
            GeriAlKaydet();
            SonaParcaEkle(new GuzergahParcasi
            {
                Tip = GuzergahParcaTipi.Spiral, Uzunluk = d[0], YaricapBaslangic = baslangicYaricapi, YaricapBitis = bitisYaricapi,
                Yon = yonBelli ? yon : d[3] == 2 ? DonusYonu.Sol : DonusYonu.Sag
            });
            Doldur(_guzergah.Parcalar.Count - 1);
        }

        // Birkaç sayısal değeri tek pencerede sorar; hepsi geçerli sayıysa true döner.
        private bool DegerleriSor(string baslik, string[] etiketler, double[] oneriler, out double[] degerler)
        {
            degerler = new double[etiketler.Length];
            using (var dlg = new Form
            {
                Text = baslik,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(380, 60 + etiketler.Length * 30)
            })
            {
                var kutular = new TextBox[etiketler.Length];
                for (var i = 0; i < etiketler.Length; i++)
                {
                    dlg.Controls.Add(new Label { Text = etiketler[i], Location = new Point(12, 17 + i * 30), AutoSize = true });
                    kutular[i] = new TextBox { Text = oneriler[i].ToString("0.###", CultureInfo.InvariantCulture), Location = new Point(250, 14 + i * 30), Size = new Size(118, 20) };
                    dlg.Controls.Add(kutular[i]);
                }
                var tamam = new Button { Text = "Tamam", Location = new Point(212, dlg.ClientSize.Height - 38), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(293, dlg.ClientSize.Height - 38), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.Add(tamam);
                dlg.Controls.Add(iptal);
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                for (var i = 0; i < etiketler.Length; i++)
                {
                    if (!NcnDosyasi.SayiOku(kutular[i].Text, out degerler[i]))
                    {
                        MessageBox.Show("Lütfen bütün alanlara geçerli birer sayı girin.", baslik, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                return true;
            }
        }

        // Kurp yarıçapını (ve geçiş eğrili kurpta giriş/çıkış spiral boylarını) sorar.
        private bool KurpDegerleriniSor(bool spiralli, double yaricapOnerisi, double girisOnerisi, double cikisOnerisi, out double yaricap, out double girisBoyu, out double cikisBoyu)
        {
            yaricap = girisBoyu = cikisBoyu = 0;
            using (var dlg = new Form
            {
                Text = spiralli ? "Geçiş Eğrili Kurp" : "Kurp",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(320, spiralli ? 150 : 90)
            })
            {
                TextBox Satir(string etiket, double deger, int y)
                {
                    dlg.Controls.Add(new Label { Text = etiket, Location = new Point(12, y + 3), AutoSize = true });
                    var kutu = new TextBox { Text = deger.ToString("0.###", CultureInfo.InvariantCulture), Location = new Point(190, y), Size = new Size(118, 20) };
                    dlg.Controls.Add(kutu);
                    return kutu;
                }
                var yaricapKutu = Satir("Yarıçap R (m):", yaricapOnerisi, 14);
                var girisKutu = spiralli ? Satir("Giriş spirali boyu L1 (m):", girisOnerisi, 44) : null;
                var cikisKutu = spiralli ? Satir("Çıkış spirali boyu L2 (m):", cikisOnerisi, 74) : null;
                var tamam = new Button { Text = "Tamam", Location = new Point(152, dlg.ClientSize.Height - 38), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(233, dlg.ClientSize.Height - 38), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.Add(tamam);
                dlg.Controls.Add(iptal);
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                if (!NcnDosyasi.SayiOku(yaricapKutu.Text, out yaricap) || yaricap <= 0 ||
                    (spiralli && (!NcnDosyasi.SayiOku(girisKutu.Text, out girisBoyu) || girisBoyu < 0 || !NcnDosyasi.SayiOku(cikisKutu.Text, out cikisBoyu) || cikisBoyu < 0)))
                {
                    MessageBox.Show("Yarıçap sıfırdan büyük, spiral boyları sıfır ya da daha büyük birer sayı olmalıdır.", dlg.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }
        }

        // Güzergah ekranda ucundan ya da somesinden sürüklenirken tablo ve geri al kaydı güncel tutulur.
        private void Panel_TutamacSuruklendi(CizimNesnesi nesne, TutamacAsamasi asama)
        {
            if (!ReferenceEquals(nesne, _guzergah))
                return;

            if (asama == TutamacAsamasi.Basladi)
            {
                _tablo.EndEdit();
                GeriAlKaydet();
            }
            else if (asama == TutamacAsamasi.Bitti)
            {
                _guzergah.SabitleriBirak(0);
                Doldur(_tablo.CurrentCell?.RowIndex ?? 0);
            }
            else
            {
                _ozetLabel.Text = _guzergah.Parcalar.Count + " parça   |   Uzunluk: " + Sayi(_guzergah.ToplamUzunluk) + " m";
            }
        }

        // ------------------------------------------------------------------ kapanış

        private void GuzergahEditoruForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _panel.NoktaTiklandi -= Panel_NoktaTiklandi;
            _panel.NoktaAlmaBitti -= Panel_NoktaAlmaBitti;
            _panel.TutamacSuruklendi -= Panel_TutamacSuruklendi;
            _panel.UstCizim -= Panel_UstCizim;
            _panel.MouseMove -= Panel_MouseMove;
            if (_komut != EkranKomutu.Yok && _panel.AktifArac == CizimAraci.NoktaAl)
                _panel.AktifArac = CizimAraci.Sec;
            _komut = EkranKomutu.Yok;

            if (_vazgecildi)
                HaleDon(_acilisHali);

            _guzergah.VurguluParca = -1;
            _guzergah.Hesapla();
            _panel.Invalidate();
        }
    }
}
