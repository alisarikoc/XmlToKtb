using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb.Las
{
    /// <summary>
    /// Nokta bulutu (LAS) görüntüleyici: dosyayı açar, noktaları üstten (plan) görünümde kot, sınıf, yoğunluk ya da
    /// renge göre boyayarak gösterir; sınıflar tek tek açılıp kapatılabilir, görünen noktalar NCN olarak dışa
    /// aktarılabilir. Çizim ekranından bağımsız, ayrı bir penceredir.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class LasGoruntuleyiciForm : Form
    {
        /// <summary>Belleğe alınacak en fazla nokta sayısı; daha büyük dosyalar eşit aralıkla örneklenir.</summary>
        private const int EnFazlaNokta = 6000000;

        private enum Renklendirme
        {
            Kot,
            Sinif,
            Yogunluk,
            Renk
        }

        private class SinifSatiri
        {
            public int Sinif;
            public int Sayi;

            public override string ToString()
            {
                return Sinif + " - " + LasBulutu.SinifAdi(Sinif) + "  (" + Sayi.ToString("N0") + ")";
            }
        }

        private class CizimAlani : Panel
        {
            public CizimAlani()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                SetStyle(ControlStyles.Selectable, true);
                TabStop = true;
            }
        }

        private LasBulutu _bulut;
        private int[] _renkler;                       // noktaların o anki renklendirmeye göre rengi
        private readonly bool[] _sinifGorunur = new bool[256];
        private Renklendirme _renklendirme = Renklendirme.Kot;
        private int _noktaBoyutu = 1;

        // Görünüm (3B, perspektifsiz): bulut _pivot noktası etrafında döner. Pivot ekranda, ekran ortasından
        // _kayX/_kayY piksel ötede durur. _azimut düşey eksen etrafındaki dönüş, _egim bakışın yataydan yukarı
        // açısıdır (90°: tam üstten, 0°: yandan). _olcek piksel/metre.
        private double _pivotX, _pivotY, _pivotZ;
        private double _kayX, _kayY;
        private double _olcek = 1;
        private double _azimut = -Math.PI / 4, _egim = Math.PI / 6;
        private Bitmap _resim;
        private int[] _pikselNoktasi;                 // her pikselde görünen noktanın sırası (-1: boş)
        private int[] _pikseller;
        private float[] _derinlik;
        private bool _resimGecerli;
        private MouseButtons _surukDugme = MouseButtons.None;
        private Point _surukSon;
        private bool _hizliCizim;                     // sürükleme sırasında noktaların bir kısmı çizilir
        private const string Ipucu = "Sol tuş: döndür   Sağ/orta tuş: kaydır   Tekerlek: yakınlaştır   Çift tık: tümünü göster";
        private bool _sinifListesiDolduruluyor;

        private readonly CizimAlani _alan = new CizimAlani { Dock = DockStyle.Fill, BackColor = Color.Black };
        private readonly TextBox _bilgiKutusu = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Top, Height = 240, BorderStyle = BorderStyle.None, BackColor = SystemColors.Control, ScrollBars = ScrollBars.Vertical };
        private readonly CheckedListBox _sinifListesi = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
        private readonly ToolStripStatusLabel _konumLabel = new ToolStripStatusLabel("Bir LAS dosyası açın (Dosya > Aç).") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ToolStripProgressBar _ilerleme = new ToolStripProgressBar { Visible = false, Width = 160 };
        private readonly List<ToolStripMenuItem> _renkMenuleri = new List<ToolStripMenuItem>();
        private readonly List<ToolStripMenuItem> _boyutMenuleri = new List<ToolStripMenuItem>();
        private ToolStripMenuItem _disaAktarMenusu;
        private ToolStripMenuItem _enkesitMenusu;
        private ToolStripMenuItem _sistemMenusu;

        // Bulutun koordinat sistemi (tanımsızsa null) ve nereden geldiği (dosyadan, elle, dönüşümle).
        private KoordinatSistemi _sistem;
        private string _sistemKaynagi;
        private readonly DatumParametreleri _datum = new DatumParametreleri();
        private readonly ToolStripStatusLabel _sistemLabel = new ToolStripStatusLabel("") { IsLink = true, ToolTipText = "Koordinat sistemini tanımlamak ya da dönüştürmek için tıklayın" };

        // Yüklenen güzergah ve ekranda çizilecek örnek noktaları (orijine göre; kot düşey geometriden, yoksa bulutun
        // ortanca kotundan). _gzBasKm/_gzBitKm güzergahın nokta bulutu alanından geçen kısmıdır.
        private GuzergahNesnesi _guzergah;
        private double[] _gzX, _gzY, _gzZ;
        private double _gzBasKm, _gzBitKm;
        private readonly LasEnkesitAyarlari _enkesitAyarlari = new LasEnkesitAyarlari();

        public LasGoruntuleyiciForm()
        {
            Text = "Nokta Bulutu (LAS)";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1200, 760);
            MinimumSize = new Size(760, 480);

            var menu = new MenuStrip();
            var dosya = new ToolStripMenuItem("Dosya");
            dosya.DropDownItems.Add("Aç (LAS)...", null, (s, e) => DosyaAc());
            _disaAktarMenusu = new ToolStripMenuItem("Görünen Noktaları Dışa Aktar (NCN)...", null, (s, e) => DisaAktar()) { Enabled = false };
            dosya.DropDownItems.Add(_disaAktarMenusu);
            _sistemMenusu = new ToolStripMenuItem("Koordinat Sistemi / Dönüşüm...", null, (s, e) => KoordinatSisteminiAyarla()) { Enabled = false };
            dosya.DropDownItems.Add(_sistemMenusu);

            var gorunum = new ToolStripMenuItem("Görünüm");
            void RenkMenusu(string metin, Renklendirme tur)
            {
                var oge = new ToolStripMenuItem(metin, null, (s, e) => RenklendirmeyiSec(tur)) { Tag = tur };
                _renkMenuleri.Add(oge);
                gorunum.DropDownItems.Add(oge);
            }
            RenkMenusu("Kota Göre Renklendir", Renklendirme.Kot);
            RenkMenusu("Sınıfa Göre Renklendir", Renklendirme.Sinif);
            RenkMenusu("Yoğunluğa Göre Renklendir", Renklendirme.Yogunluk);
            RenkMenusu("Dosyadaki Renklerle (RGB)", Renklendirme.Renk);
            gorunum.DropDownItems.Add(new ToolStripSeparator());
            foreach (var boyut in new[] { 1, 2, 3 })
            {
                var oge = new ToolStripMenuItem("Nokta Boyutu: " + boyut, null, (s, e) =>
                {
                    _noktaBoyutu = boyut;
                    MenuleriGuncelle();
                    Yenile();
                }) { Tag = boyut };
                _boyutMenuleri.Add(oge);
                gorunum.DropDownItems.Add(oge);
            }
            gorunum.DropDownItems.Add(new ToolStripSeparator());
            gorunum.DropDownItems.Add("İzometrik Bakış", null, (s, e) => Bakis(-45, 30));
            gorunum.DropDownItems.Add("Üstten Bakış (Plan)", null, (s, e) => Bakis(0, 90));
            gorunum.DropDownItems.Add("Önden Bakış (Güneyden)", null, (s, e) => Bakis(0, 0));
            gorunum.DropDownItems.Add("Yandan Bakış (Doğudan)", null, (s, e) => Bakis(90, 0));
            gorunum.DropDownItems.Add(new ToolStripSeparator());
            gorunum.DropDownItems.Add("Tümünü Göster", null, (s, e) => TumunuGoster());
            menu.Items.Add(dosya);
            menu.Items.Add(gorunum);

            var guzergahMenu = new ToolStripMenuItem("Güzergah");
            guzergahMenu.DropDownItems.Add("XML'den Güzergah Yükle...", null, (s, e) => GuzergahSec());
            _enkesitMenusu = new ToolStripMenuItem("Enkesit Al (KSE)...", null, (s, e) => EnkesitAl()) { Enabled = false };
            guzergahMenu.DropDownItems.Add(_enkesitMenusu);
            menu.Items.Add(guzergahMenu);

            var solPanel = new Panel { Dock = DockStyle.Left, Width = 280, Padding = new Padding(6) };
            var sinifBasligi = new Label { Text = "Sınıflar (tikli olanlar gösterilir)", Dock = DockStyle.Top, Height = 22, Font = new Font(Font, FontStyle.Bold) };
            solPanel.Controls.Add(_sinifListesi);
            solPanel.Controls.Add(sinifBasligi);
            solPanel.Controls.Add(_bilgiKutusu);

            var durum = new StatusStrip();
            durum.Items.Add(_konumLabel);
            durum.Items.Add(_ilerleme);
            durum.Items.Add(_sistemLabel);
            durum.ShowItemToolTips = true;
            _sistemLabel.Click += (s, e) => KoordinatSisteminiAyarla();

            Controls.Add(_alan);
            Controls.Add(solPanel);
            Controls.Add(durum);
            Controls.Add(menu);
            MainMenuStrip = menu;

            _sinifListesi.ItemCheck += (s, e) =>
            {
                if (_sinifListesiDolduruluyor)
                    return;
                _sinifGorunur[((SinifSatiri)_sinifListesi.Items[e.Index]).Sinif] = e.NewValue == CheckState.Checked;
                Yenile();
            };

            _alan.Paint += Alan_Paint;
            _alan.Resize += (s, e) => _resimGecerli = false;
            _alan.MouseEnter += (s, e) => _alan.Focus();
            _alan.MouseWheel += Alan_MouseWheel;
            _alan.MouseDown += (s, e) =>
            {
                if (_bulut == null)
                    return;
                if (e.Clicks >= 2)
                {
                    TumunuGoster();
                    return;
                }
                _surukDugme = e.Button;
                _surukSon = e.Location;

                // Döndürme, tıklanan nokta etrafında yapılır: pivot o noktaya alınır, görüntü yerinden oynamaz.
                var nokta = e.Button == MouseButtons.Left ? PikseldekiNokta(e.X, e.Y) : -1;
                if (nokta >= 0)
                {
                    _pivotX = _bulut.X[nokta];
                    _pivotY = _bulut.Y[nokta];
                    _pivotZ = _bulut.Z[nokta];
                    _kayX = e.X - _alan.Width / 2.0;
                    _kayY = e.Y - _alan.Height / 2.0;
                }
            };
            _alan.MouseUp += (s, e) =>
            {
                _surukDugme = MouseButtons.None;
                if (_hizliCizim)
                {
                    _hizliCizim = false;
                    Yenile();
                }
            };
            _alan.MouseMove += Alan_MouseMove;

            MenuleriGuncelle();
            FormClosed += (s, e) => _resim?.Dispose();
        }

        // ------------------------------------------------------------------ dosya

        private void DosyaAc()
        {
            using (var dlg = new OpenFileDialog { Title = "LAS Dosyası Seçin", Filter = "LAS nokta bulutu (*.las)|*.las|Tüm dosyalar (*.*)|*.*" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                DosyayiYukle(dlg.FileName);
            }
        }

        /// <summary>Verilen LAS dosyasını yükler ve gösterir.</summary>
        public void DosyayiYukle(string dosyaYolu)
        {
            LasBulutu bulut;
            Cursor = Cursors.WaitCursor;
            _ilerleme.Value = 0;
            _ilerleme.Visible = true;
            _konumLabel.Text = Path.GetFileName(dosyaYolu) + " okunuyor...";
            Refresh();
            try
            {
                var sonYuzde = -1;
                bulut = LasBulutu.Oku(dosyaYolu, EnFazlaNokta, oran =>
                {
                    var yuzde = (int)(oran * 100);
                    if (yuzde == sonYuzde)
                        return;
                    sonYuzde = yuzde;
                    _ilerleme.Value = Math.Max(0, Math.Min(100, yuzde));
                    _ilerleme.GetCurrentParent()?.Refresh();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("LAS dosyası okunamadı: " + ex.Message, "Nokta Bulutu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _konumLabel.Text = "";
                return;
            }
            finally
            {
                _ilerleme.Visible = false;
                Cursor = Cursors.Default;
            }

            _bulut = bulut;
            _renkler = new int[bulut.Sayi];
            Text = "Nokta Bulutu (LAS) - " + Path.GetFileName(dosyaYolu);
            _disaAktarMenusu.Enabled = true;
            _sistemMenusu.Enabled = true;
            _sistem = KoordinatSistemi.Bul(bulut.Epsg);
            _sistemKaynagi = _sistem != null ? "dosyadan" : null;
            SistemEtiketiniGuncelle();
            _konumLabel.Text = Ipucu;

            // Dosyada renk varsa renkli, yoksa kota göre açılır.
            _renklendirme = bulut.Rgb != null ? Renklendirme.Renk : Renklendirme.Kot;

            _sinifListesiDolduruluyor = true;
            _sinifListesi.Items.Clear();
            for (var sinif = 0; sinif < 256; sinif++)
            {
                _sinifGorunur[sinif] = true;
                if (bulut.SinifSayilari[sinif] > 0)
                    _sinifListesi.Items.Add(new SinifSatiri { Sinif = sinif, Sayi = bulut.SinifSayilari[sinif] }, true);
            }
            _sinifListesiDolduruluyor = false;

            GuzergahCizgisiniHazirla();
            BilgiyiYaz();
            MenuleriGuncelle();
            RenkleriHesapla();
            TumunuGoster();
        }

        private static string Sayi(double deger, string bicim = "0.000")
        {
            return deger.ToString(bicim, CultureInfo.InvariantCulture);
        }

        private void BilgiyiYaz()
        {
            var b = _bulut;
            var satirlar = new List<string>
            {
                "Dosya: " + Path.GetFileName(b.DosyaYolu),
                "LAS sürümü: " + b.Surum + "   Nokta biçimi: " + b.NoktaBicimi,
                "Üreten: " + (string.IsNullOrWhiteSpace(b.UretenYazilim) ? "-" : b.UretenYazilim),
                "Toplam nokta: " + b.ToplamNokta.ToString("N0"),
                "Yüklenen: " + b.Sayi.ToString("N0") + (b.Adim > 1 ? "  (her " + b.Adim + " noktadan biri)" : "  (tamamı)"),
                "",
                "Y (sağa):   " + Sayi(b.MinX) + " ... " + Sayi(b.MaxX),
                "X (yukarı): " + Sayi(b.MinY) + " ... " + Sayi(b.MaxY),
                "Z (kot):    " + Sayi(b.MinZ) + " ... " + Sayi(b.MaxZ),
                "Boyut: " + Sayi(b.MaxX - b.MinX, "0.0") + " m x " + Sayi(b.MaxY - b.MinY, "0.0") + " m",
                "Yoğunluk: " + b.MinYogunluk + " ... " + b.MaxYogunluk,
                "Renk (RGB): " + (b.Rgb != null ? "var" : "yok")
            };
            if (_guzergah != null)
            {
                satirlar.Add("");
                satirlar.Add("Güzergah: " + _guzergah.Ad);
                satirlar.Add(double.IsNaN(_gzBasKm)
                    ? "Nokta bulutu alanından geçmiyor!"
                    : "Buluttaki kısmı: " + GuzergahNesnesi.KmMetni(_gzBasKm) + " - " + GuzergahNesnesi.KmMetni(_gzBitKm));
            }
            _bilgiKutusu.Text = string.Join(Environment.NewLine, satirlar);
        }

        private void MenuleriGuncelle()
        {
            foreach (var oge in _renkMenuleri)
            {
                var tur = (Renklendirme)oge.Tag;
                oge.Checked = tur == _renklendirme;
                oge.Enabled = tur != Renklendirme.Renk || (_bulut != null && _bulut.Rgb != null);
            }
            foreach (var oge in _boyutMenuleri)
                oge.Checked = (int)oge.Tag == _noktaBoyutu;
            _enkesitMenusu.Enabled = _bulut != null && _guzergah != null;
        }

        // ------------------------------------------------------------------ renklendirme

        private void RenklendirmeyiSec(Renklendirme tur)
        {
            _renklendirme = tur;
            MenuleriGuncelle();
            RenkleriHesapla();
            Yenile();
        }

        // Sınıfların alışılmış renkleri: zemin kahverengi, bitki yeşil tonları, bina turuncu, su mavi...
        private static int SinifRengi(int sinif)
        {
            switch (sinif)
            {
                case 0: return 0x808080;
                case 1: return 0xC8C8C8;
                case 2: return 0xB07840;
                case 3: return 0x90EE90;
                case 4: return 0x32CD32;
                case 5: return 0x1E8B1E;
                case 6: return 0xFF8C28;
                case 7: case 18: return 0xFF00FF;
                case 9: return 0x3C78FF;
                case 10: return 0xFFFF00;
                case 11: return 0x9A9A9A;
                case 17: return 0xFFD700;
                default:
                    // Diğer sınıflar için numaradan türetilen, birbirinden ayırt edilebilir renkler.
                    return ((sinif * 97 % 200 + 55) << 16) | ((sinif * 57 % 200 + 55) << 8) | (sinif * 151 % 200 + 55);
            }
        }

        // 0-1 arası değeri mavi-camgöbeği-yeşil-sarı-kırmızı renk skalasına çevirir.
        private static int SkalaRengi(double t)
        {
            t = Math.Max(0, Math.Min(1, t)) * 4;
            int r, g, b;
            if (t < 1) { r = 0; g = (int)(255 * t); b = 255; }
            else if (t < 2) { r = 0; g = 255; b = (int)(255 * (2 - t)); }
            else if (t < 3) { r = (int)(255 * (t - 2)); g = 255; b = 0; }
            else { r = 255; g = (int)(255 * (4 - t)); b = 0; }
            return (r << 16) | (g << 8) | b;
        }

        // Değerlerin %1 ile %99'u arasındaki aralık: birkaç uç değer (hatalı nokta) bütün skalayı bozmasın diye.
        private static void Aralik(Func<int, double> deger, int sayi, out double alt, out double ust)
        {
            var ornek = new List<double>();
            var adim = Math.Max(1, sayi / 200000);
            for (var i = 0; i < sayi; i += adim)
                ornek.Add(deger(i));
            ornek.Sort();
            alt = ornek[(int)(ornek.Count * 0.01)];
            ust = ornek[Math.Min(ornek.Count - 1, (int)(ornek.Count * 0.99))];
            if (ust - alt < 1e-9)
                ust = alt + 1;
        }

        private void RenkleriHesapla()
        {
            var b = _bulut;
            if (b == null)
                return;

            switch (_renklendirme)
            {
                case Renklendirme.Renk when b.Rgb != null:
                    Array.Copy(b.Rgb, _renkler, b.Sayi);
                    break;

                case Renklendirme.Sinif:
                    for (var i = 0; i < b.Sayi; i++)
                        _renkler[i] = SinifRengi(b.Sinif[i]);
                    break;

                case Renklendirme.Yogunluk:
                {
                    Aralik(i => b.Yogunluk[i], b.Sayi, out var alt, out var ust);
                    for (var i = 0; i < b.Sayi; i++)
                    {
                        var gri = (int)(Math.Max(0, Math.Min(1, (b.Yogunluk[i] - alt) / (ust - alt))) * 235) + 20;
                        _renkler[i] = (gri << 16) | (gri << 8) | gri;
                    }
                    break;
                }

                default:
                {
                    Aralik(i => b.Z[i], b.Sayi, out var alt, out var ust);
                    for (var i = 0; i < b.Sayi; i++)
                        _renkler[i] = SkalaRengi((b.Z[i] - alt) / (ust - alt));
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ görünüm ve çizim

        // Bakış eksenleri: ekranda sağa (s), yukarı (u) ve izleyiciye doğru (d) birim vektörler.
        private void Eksenler(out double sx, out double sy, out double ux, out double uy, out double uz, out double dx, out double dy, out double dz)
        {
            double ca = Math.Cos(_azimut), sa = Math.Sin(_azimut), ce = Math.Cos(_egim), se = Math.Sin(_egim);
            sx = ca; sy = sa;
            ux = -sa * se; uy = ca * se; uz = ce;
            dx = sa * ce; dy = -ca * ce; dz = se;
        }

        private void Bakis(double azimutDerece, double egimDerece)
        {
            _azimut = azimutDerece * Math.PI / 180;
            _egim = egimDerece * Math.PI / 180;
            TumunuGoster();
        }

        private void TumunuGoster()
        {
            var b = _bulut;
            if (b == null || _alan.Width < 10 || _alan.Height < 10)
                return;

            _pivotX = (b.MinX + b.MaxX) / 2 - b.OrijinX;
            _pivotY = (b.MinY + b.MaxY) / 2 - b.OrijinY;
            _pivotZ = (b.MinZ + b.MaxZ) / 2;
            _kayX = _kayY = 0;

            // Sınır kutusunun bu bakıştaki ekran izdüşümü pencereye sığdırılır.
            Eksenler(out var sx, out var sy, out var ux, out var uy, out var uz, out _, out _, out _);
            double hx = (b.MaxX - b.MinX) / 2, hy = (b.MaxY - b.MinY) / 2, hz = (b.MaxZ - b.MinZ) / 2;
            var yariGenislik = Math.Max(Math.Abs(sx) * hx + Math.Abs(sy) * hy, 1e-3);
            var yariYukseklik = Math.Max(Math.Abs(ux) * hx + Math.Abs(uy) * hy + Math.Abs(uz) * hz, 1e-3);
            _olcek = Math.Min(_alan.Width / (2 * yariGenislik), _alan.Height / (2 * yariYukseklik)) * 0.95;
            Yenile();
        }

        private void Yenile()
        {
            _resimGecerli = false;
            _alan.Invalidate();
        }

        // İmlecin altındaki (en çok 3 piksel yakınındaki) noktanın sırası; yoksa -1.
        private int PikseldekiNokta(int x, int y)
        {
            if (_pikselNoktasi == null || _resim == null || _bulut == null)
                return -1;

            int enIyi = -1, enIyiUzaklik = int.MaxValue;
            for (var dy = -3; dy <= 3; dy++)
            {
                var py = y + dy;
                if (py < 0 || py >= _resim.Height)
                    continue;
                for (var dx = -3; dx <= 3; dx++)
                {
                    var px = x + dx;
                    if (px < 0 || px >= _resim.Width)
                        continue;
                    var nokta = _pikselNoktasi[py * _resim.Width + px];
                    var uzaklik = dx * dx + dy * dy;
                    if (nokta >= 0 && nokta < _bulut.Sayi && uzaklik < enIyiUzaklik)
                    {
                        enIyi = nokta;
                        enIyiUzaklik = uzaklik;
                    }
                }
            }
            return enIyi;
        }

        private void Alan_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_bulut == null)
                return;

            // İmlecin altındaki yer sabit kalacak şekilde yakınlaştırılır.
            var yeni = Math.Max(1e-6, Math.Min(1e5, _olcek * (e.Delta > 0 ? 1.25 : 1 / 1.25)));
            var oran = yeni / _olcek;
            double mx = e.X - _alan.Width / 2.0, my = e.Y - _alan.Height / 2.0;
            _kayX = mx - (mx - _kayX) * oran;
            _kayY = my - (my - _kayY) * oran;
            _olcek = yeni;
            Yenile();
        }

        private void Alan_MouseMove(object sender, MouseEventArgs e)
        {
            var b = _bulut;
            if (b == null)
                return;

            if (_surukDugme != MouseButtons.None)
            {
                int dx = e.X - _surukSon.X, dy = e.Y - _surukSon.Y;
                if (dx == 0 && dy == 0)
                    return;
                _surukSon = e.Location;
                if (_surukDugme == MouseButtons.Left)
                {
                    _azimut -= dx * 0.008;
                    _egim = Math.Max(-Math.PI / 2, Math.Min(Math.PI / 2, _egim + dy * 0.008));
                }
                else
                {
                    _kayX += dx;
                    _kayY += dy;
                }
                _hizliCizim = true;
                Yenile();
                return;
            }

            // İmlecin altında nokta varsa koordinatı, sınıfı ve yoğunluğu gösterilir.
            var nokta = PikseldekiNokta(e.X, e.Y);
            _konumLabel.Text = nokta < 0
                ? Ipucu
                : "Y: " + Sayi(b.OrijinX + b.X[nokta]) + "   X: " + Sayi(b.OrijinY + b.Y[nokta]) + "   Z: " + Sayi(b.Z[nokta]) +
                  "   Sınıf: " + b.Sinif[nokta] + " (" + LasBulutu.SinifAdi(b.Sinif[nokta]) + ")   Yoğunluk: " + b.Yogunluk[nokta];
        }

        private void Alan_Paint(object sender, PaintEventArgs e)
        {
            if (_bulut == null || _alan.Width < 2 || _alan.Height < 2)
                return;

            if (!_resimGecerli || _resim == null || _resim.Width != _alan.Width || _resim.Height != _alan.Height)
                ResmiCiz();
            e.Graphics.DrawImageUnscaled(_resim, 0, 0);
            GuzergahiCiz(e.Graphics);
            EksenGostergesiniCiz(e.Graphics);
        }

        // Sol alt köşede bakış yönünü gösteren eksen üçlüsü: Y (doğu), X (kuzey), Z (kot).
        private void EksenGostergesiniCiz(Graphics g)
        {
            Eksenler(out var sx, out var sy, out var ux, out var uy, out var uz, out _, out _, out _);
            const float boy = 34;
            var merkez = new PointF(50, _alan.Height - 50);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            void Eksen(double ex, double ey, Color renk, string ad)
            {
                var uc = new PointF(merkez.X + (float)ex * boy, merkez.Y - (float)ey * boy);
                using (var kalem = new Pen(renk, 2))
                using (var firca = new SolidBrush(renk))
                {
                    g.DrawLine(kalem, merkez, uc);
                    g.DrawString(ad, Font, firca, uc.X + (float)ex * 8 - 5, uc.Y - (float)ey * 8 - 7);
                }
            }
            Eksen(sx, ux, Color.FromArgb(255, 90, 90), "Y");
            Eksen(sy, uy, Color.FromArgb(90, 220, 90), "X");
            Eksen(0, uz, Color.FromArgb(110, 160, 255), "Z");
        }

        /// <summary>
        /// Noktaları doğrudan piksel dizisine yazar (GDI+ ile tek tek çizmek milyonlarca noktada çok yavaş olurdu).
        /// Aynı piksele düşen noktalardan izleyiciye en yakın olan görünür. Sürükleme sırasında akıcı olması için
        /// noktaların bir kısmı çizilir; fare bırakılınca tamamı çizilir.
        /// </summary>
        private void ResmiCiz()
        {
            var b = _bulut;
            int genislik = _alan.Width, yukseklik = _alan.Height;
            if (_resim == null || _resim.Width != genislik || _resim.Height != yukseklik)
            {
                _resim?.Dispose();
                _resim = new Bitmap(genislik, yukseklik, PixelFormat.Format32bppRgb);
                _pikselNoktasi = new int[genislik * yukseklik];
                _pikseller = new int[genislik * yukseklik];
                _derinlik = new float[genislik * yukseklik];
            }

            int[] pikseller = _pikseller, pikselNoktasi = _pikselNoktasi;
            var derinlik = _derinlik;
            Array.Clear(pikseller, 0, pikseller.Length);
            for (var i = 0; i < derinlik.Length; i++)
            {
                derinlik[i] = float.MinValue;
                pikselNoktasi[i] = -1;
            }

            Eksenler(out var sx, out var sy, out var ux, out var uy, out var uz, out var dx, out var dy, out var dz);
            float a1 = (float)(sx * _olcek), a2 = (float)(sy * _olcek);
            float b1 = (float)(ux * _olcek), b2 = (float)(uy * _olcek), b3 = (float)(uz * _olcek);
            float d1 = (float)dx, d2 = (float)dy, d3 = (float)dz;
            var kaydirX = (float)(genislik / 2.0 + _kayX - (sx * _pivotX + sy * _pivotY) * _olcek);
            var kaydirY = (float)(yukseklik / 2.0 + _kayY + (ux * _pivotX + uy * _pivotY + uz * _pivotZ) * _olcek);

            var boyut = _noktaBoyutu;
            var adim = _hizliCizim ? Math.Max(1, b.Sayi / 1200000) : 1;
            float[] xs = b.X, ys = b.Y, zs = b.Z;
            var siniflar = b.Sinif;
            var renkler = _renkler;
            var gorunur = _sinifGorunur;
            for (var i = 0; i < b.Sayi; i += adim)
            {
                if (!gorunur[siniflar[i]])
                    continue;

                float x = xs[i], y = ys[i], z = zs[i];
                var fx = a1 * x + a2 * y + kaydirX;
                var fy = kaydirY - (b1 * x + b2 * y + b3 * z);
                if (!(fx >= 0 && fy >= 0 && fx < genislik && fy < yukseklik))
                    continue;

                int px = (int)fx, py = (int)fy;
                var yakinlik = d1 * x + d2 * y + d3 * z;
                if (boyut == 1)
                {
                    var yer = py * genislik + px;
                    if (yakinlik > derinlik[yer])
                    {
                        derinlik[yer] = yakinlik;
                        pikseller[yer] = renkler[i];
                        pikselNoktasi[yer] = i;
                    }
                    continue;
                }

                for (var ky = 0; ky < boyut && py + ky < yukseklik; ky++)
                {
                    var satir = (py + ky) * genislik;
                    for (var kx = 0; kx < boyut && px + kx < genislik; kx++)
                    {
                        var yer = satir + px + kx;
                        if (yakinlik <= derinlik[yer])
                            continue;
                        derinlik[yer] = yakinlik;
                        pikseller[yer] = renkler[i];
                        pikselNoktasi[yer] = i;
                    }
                }
            }

            var veri = _resim.LockBits(new Rectangle(0, 0, genislik, yukseklik), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            try
            {
                if (veri.Stride == genislik * 4)
                {
                    Marshal.Copy(pikseller, 0, veri.Scan0, pikseller.Length);
                }
                else
                {
                    for (var satirNo = 0; satirNo < yukseklik; satirNo++)
                        Marshal.Copy(pikseller, satirNo * genislik, IntPtr.Add(veri.Scan0, satirNo * veri.Stride), genislik);
                }
            }
            finally
            {
                _resim.UnlockBits(veri);
            }
            _resimGecerli = true;
        }

        // ------------------------------------------------------------------ koordinat sistemi

        // Dosyadaki koordinat sistemi kaydının kısa açıklaması.
        private string DosyadakiSistemMetni()
        {
            var b = _bulut;
            if (b.Epsg == 0)
                return "tanımlı değil" + (string.IsNullOrWhiteSpace(b.KoordinatSistemiAdi) ? "" : " (" + b.KoordinatSistemiAdi + ")");
            var bilinen = KoordinatSistemi.Bul(b.Epsg);
            return bilinen != null ? bilinen.ToString() : "EPSG:" + b.Epsg + (string.IsNullOrWhiteSpace(b.KoordinatSistemiAdi) ? "" : " - " + b.KoordinatSistemiAdi) + " (desteklenmiyor)";
        }

        private void SistemEtiketiniGuncelle()
        {
            if (_bulut == null)
                _sistemLabel.Text = "";
            else if (_sistem != null)
                _sistemLabel.Text = "Koordinat sistemi: " + _sistem + " (" + _sistemKaynagi + ")";
            else if (_bulut.Epsg != 0)
                _sistemLabel.Text = "Koordinat sistemi: " + DosyadakiSistemMetni();
            else
                _sistemLabel.Text = "Koordinat sistemi: dosyada tanımlı değil";
        }

        private void KoordinatSisteminiAyarla()
        {
            var b = _bulut;
            if (b == null)
                return;

            KoordinatSistemi kaynak, hedef;
            using (var dlg = new KoordinatSistemiForm(_sistem, DosyadakiSistemMetni(), _datum))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                kaynak = dlg.Kaynak;
                hedef = dlg.Hedef;
            }

            if (hedef == null)
            {
                // Yalnızca tanımlama: koordinatlar değişmez.
                if (kaynak != _sistem)
                {
                    _sistem = kaynak;
                    _sistemKaynagi = kaynak == null ? null : kaynak.Epsg == b.Epsg ? "dosyadan" : "elle tanımlandı";
                }
                SistemEtiketiniGuncelle();
                return;
            }

            // Koordinatlar seçilen kaynak sistemle bağdaşmıyorsa (ör. yerel koordinatlı dosya) dönüşüm anlamsız olur.
            if (!kaynak.TurkiyeIcindeMi((b.MinX + b.MaxX) / 2, (b.MinY + b.MaxY) / 2) &&
                MessageBox.Show("Bulutun koordinatları (Y " + Sayi((b.MinX + b.MaxX) / 2) + ", X " + Sayi((b.MinY + b.MaxY) / 2) + ") seçilen mevcut sistemde (" + kaynak +
                                ") Türkiye içine düşmüyor. Mevcut sistem yanlış seçilmiş ya da dosya yerel koordinatlarda olabilir; dönüşüm sonucu anlamsız olur.\n\nYine de dönüştürülsün mü?",
                    "Koordinat Sistemi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            Cursor = Cursors.WaitCursor;
            try
            {
                b.KoordinatlariDonustur(new KoordinatDonusturucu(kaynak, hedef, _datum));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koordinatlar dönüştürülemedi: " + ex.Message, "Koordinat Sistemi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            _sistem = hedef;
            _sistemKaynagi = "EPSG:" + kaynak.Epsg + " sisteminden dönüştürüldü";
            SistemEtiketiniGuncelle();
            GuzergahCizgisiniHazirla();
            BilgiyiYaz();
            TumunuGoster();
        }

        // ------------------------------------------------------------------ güzergah ve enkesit

        private void GuzergahSec()
        {
            using (var dlg = new OpenFileDialog { Title = "Güzergah Dosyası Seçin", Filter = "Güzergah (*.xml;*.ktb)|*.xml;*.ktb|Tüm dosyalar (*.*)|*.*" })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    GuzergahiYukle(dlg.FileName);
            }
        }

        /// <summary>Verilen LandXML (ya da KTB) dosyasındaki güzergahı yükler ve bulutun üzerinde gösterir.</summary>
        public void GuzergahiYukle(string dosyaYolu)
        {
            try
            {
                var guzergah = GuzergahCizici.GuzergahOlustur(ProjeKesitiForm.GuzergahOku(dosyaYolu, out _));
                if (guzergah == null || guzergah.Parcalar.Count == 0)
                    throw new InvalidDataException("Dosyada güzergah (yatay geometri) bulunamadı.");
                if (string.IsNullOrWhiteSpace(guzergah.Ad))
                    guzergah.Ad = Path.GetFileNameWithoutExtension(dosyaYolu);
                _guzergah = guzergah;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Güzergah okunamadı: " + (ex.InnerException ?? ex).Message, "Nokta Bulutu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            GuzergahCizgisiniHazirla();
            MenuleriGuncelle();
            if (_bulut != null)
            {
                BilgiyiYaz();
                if (double.IsNaN(_gzBasKm))
                    MessageBox.Show("Güzergah yüklendi ancak nokta bulutunun kapsadığı alandan geçmiyor. Güzergah ile LAS dosyasının aynı koordinat sisteminde olduğunu kontrol edin.",
                        "Nokta Bulutu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            _alan.Invalidate();
        }

        // Güzergahı ekranda çizmek için örnekler ve bulut alanından geçen kilometre aralığını bulur.
        private void GuzergahCizgisiniHazirla()
        {
            _gzX = _gzY = _gzZ = null;
            _gzBasKm = _gzBitKm = double.NaN;
            var g = _guzergah;
            var b = _bulut;
            if (g == null || b == null || g.ToplamUzunluk <= 0)
                return;

            // Düşey geometrisi olmayan güzergah bulutun ortanca kotunda çizilir.
            var ornekKotlar = new List<float>();
            for (var i = 0; i < b.Sayi; i += Math.Max(1, b.Sayi / 50000))
                ornekKotlar.Add(b.Z[i]);
            ornekKotlar.Sort();
            double ortancaKot = ornekKotlar[ornekKotlar.Count / 2];

            var adim = Math.Max(0.5, g.ToplamUzunluk / 20000);
            var sayi = (int)Math.Ceiling(g.ToplamUzunluk / adim) + 1;
            List<double> xs = new List<double>(sayi), ys = new List<double>(sayi), zs = new List<double>(sayi);
            for (var i = 0; i < sayi; i++)
            {
                var km = Math.Min(g.BitisKm, g.BaslangicKm + i * adim);
                if (!g.KonumAl(km, out var x, out var y, out _))
                    continue;

                xs.Add(x - b.OrijinX);
                ys.Add(y - b.OrijinY);
                zs.Add((g.Dusey != null && g.Dusey.Count > 1 ? DuseyGeometri.KotAl(g.Dusey, km) : null) ?? ortancaKot);
                if (x >= b.MinX && x <= b.MaxX && y >= b.MinY && y <= b.MaxY)
                {
                    if (double.IsNaN(_gzBasKm))
                        _gzBasKm = km;
                    _gzBitKm = km;
                }
            }
            _gzX = xs.ToArray();
            _gzY = ys.ToArray();
            _gzZ = zs.ToArray();

            // Enkesit penceresi, güzergahın buluttaki kısmıyla (aralığın katlarına yuvarlanmış) açılır.
            if (!double.IsNaN(_gzBasKm))
            {
                _enkesitAyarlari.BaslangicKm = Math.Ceiling(_gzBasKm / _enkesitAyarlari.Aralik - 1e-9) * _enkesitAyarlari.Aralik;
                _enkesitAyarlari.BitisKm = Math.Floor(_gzBitKm / _enkesitAyarlari.Aralik + 1e-9) * _enkesitAyarlari.Aralik;
                if (_enkesitAyarlari.BitisKm < _enkesitAyarlari.BaslangicKm)
                {
                    _enkesitAyarlari.BaslangicKm = _gzBasKm;
                    _enkesitAyarlari.BitisKm = _gzBitKm;
                }
            }
            else
            {
                _enkesitAyarlari.BaslangicKm = g.BaslangicKm;
                _enkesitAyarlari.BitisKm = g.BitisKm;
            }
        }

        // Güzergahı noktaların üzerine (derinliğe bakmadan) çizer.
        private void GuzergahiCiz(Graphics grafik)
        {
            if (_gzX == null || _gzX.Length < 2)
                return;

            Eksenler(out var sx, out var sy, out var ux, out var uy, out var uz, out _, out _, out _);
            var kaydirX = _alan.Width / 2.0 + _kayX - (sx * _pivotX + sy * _pivotY) * _olcek;
            var kaydirY = _alan.Height / 2.0 + _kayY + (ux * _pivotX + uy * _pivotY + uz * _pivotZ) * _olcek;

            grafik.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var kalem = new Pen(Color.Magenta, 2))
            {
                var parca = new List<PointF>();
                void Bosalt()
                {
                    if (parca.Count >= 2)
                        grafik.DrawLines(kalem, parca.ToArray());
                    parca.Clear();
                }

                for (var i = 0; i < _gzX.Length; i++)
                {
                    var fx = (sx * _gzX[i] + sy * _gzY[i]) * _olcek + kaydirX;
                    var fy = kaydirY - (ux * _gzX[i] + uy * _gzY[i] + uz * _gzZ[i]) * _olcek;
                    // Ekrandan çok uzaktaki noktalar çizilmez (GDI+ çok büyük koordinatlarda hata verir).
                    if (Math.Abs(fx) > 20000 || Math.Abs(fy) > 20000)
                        Bosalt();
                    else
                        parca.Add(new PointF((float)fx, (float)fy));
                }
                Bosalt();
            }
        }

        private void EnkesitAl()
        {
            if (_bulut == null || _guzergah == null)
                return;

            using (var dlg = new LasEnkesitForm(_enkesitAyarlari, _guzergah.BaslangicKm, _guzergah.BitisKm))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
            }

            using (var dlg = new SaveFileDialog
            {
                Title = "Enkesitleri Kaydet",
                Filter = "NetCAD enkesit (*.kse)|*.kse",
                FileName = Path.GetFileNameWithoutExtension(_bulut.DosyaYolu) + ".kse"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                KseDosyasi dosya;
                int bosKesit;
                Cursor = Cursors.WaitCursor;
                _ilerleme.Value = 0;
                _ilerleme.Visible = true;
                try
                {
                    var sonYuzde = -1;
                    dosya = LasEnkesit.Olustur(_bulut, _guzergah, _enkesitAyarlari, _sinifGorunur, oran =>
                    {
                        var yuzde = (int)(oran * 100);
                        if (yuzde == sonYuzde)
                            return;
                        sonYuzde = yuzde;
                        _ilerleme.Value = Math.Max(0, Math.Min(100, yuzde));
                        _ilerleme.GetCurrentParent()?.Refresh();
                    }, out bosKesit);

                    if (dosya.Kesitler.Count == 0)
                    {
                        MessageBox.Show("Verilen kilometre aralığında, eksenin çevresinde nokta bulunamadı; kesit oluşturulmadı.",
                            "Enkesit Al", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    File.WriteAllText(dlg.FileName, dosya.Yaz(), Encoding.Default);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Enkesitler kaydedilemedi: " + ex.Message, "Enkesit Al", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    _ilerleme.Visible = false;
                    Cursor = Cursors.Default;
                }

                MessageBox.Show(dosya.Kesitler.Count.ToString("N0") + " enkesit kaydedildi" +
                                (bosKesit > 0 ? " (" + bosKesit.ToString("N0") + " kilometrede nokta bulunamadığı için kesit oluşmadı)" : "") +
                                ":\n" + dlg.FileName, "Enkesit Al", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ------------------------------------------------------------------ dışa aktarım

        private void DisaAktar()
        {
            var b = _bulut;
            if (b == null)
                return;

            double hucre;
            using (var dlg = new Form
            {
                Text = "Dışa Aktar (NCN)",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(400, 150)
            })
            {
                dlg.Controls.Add(new Label
                {
                    Text = "Tikli sınıflardaki noktalar NCN dosyasına yazılır. Nokta sayısını azaltmak için noktalar bir ızgaraya " +
                           "oturtulur ve her ızgara hücresinden en düşük kotlu nokta alınır. 0 yazarsanız bütün noktalar yazılır.",
                    Location = new Point(12, 10),
                    Size = new Size(376, 62)
                });
                dlg.Controls.Add(new Label { Text = "Izgara aralığı (m):", Location = new Point(12, 82), AutoSize = true });
                var kutu = new TextBox { Text = "1", Location = new Point(130, 79), Size = new Size(90, 20) };
                var tamam = new Button { Text = "Tamam", Location = new Point(232, 112), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(313, 112), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { kutu, tamam, iptal });
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                if (!NcnDosyasi.SayiOku(kutu.Text, out hucre) || hucre < 0)
                {
                    MessageBox.Show("Izgara aralığı sıfır ya da daha büyük bir sayı olmalıdır.", "Dışa Aktar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Yazılacak noktaların sıraları: ızgara verilmişse her hücrenin en düşük kotlu noktası.
            var secilenler = new List<int>();
            if (hucre > 0)
            {
                var hucreler = new Dictionary<long, int>();
                for (var i = 0; i < b.Sayi; i++)
                {
                    if (!_sinifGorunur[b.Sinif[i]])
                        continue;
                    var anahtar = ((long)Math.Floor(b.X[i] / hucre) << 32) ^ (uint)(int)Math.Floor(b.Y[i] / hucre);
                    if (!hucreler.TryGetValue(anahtar, out var mevcut) || b.Z[i] < b.Z[mevcut])
                        hucreler[anahtar] = i;
                }
                secilenler.AddRange(hucreler.Values);
                secilenler.Sort();
            }
            else
            {
                for (var i = 0; i < b.Sayi; i++)
                {
                    if (_sinifGorunur[b.Sinif[i]])
                        secilenler.Add(i);
                }
            }

            if (secilenler.Count == 0)
            {
                MessageBox.Show("Yazılacak nokta yok (tikli sınıf kalmamış).", "Dışa Aktar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog
            {
                Title = "NCN Olarak Kaydet (" + secilenler.Count.ToString("N0") + " nokta)",
                Filter = "NCN (*.ncn)|*.ncn",
                FileName = Path.GetFileNameWithoutExtension(b.DosyaYolu) + ".ncn"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    Cursor = Cursors.WaitCursor;
                    var no = 0;
                    NcnDosyasi.Yaz(secilenler.Select(i => new NoktaKaydi
                    {
                        Ad = (++no).ToString(CultureInfo.InvariantCulture),
                        Y = b.OrijinX + b.X[i],
                        X = b.OrijinY + b.Y[i],
                        Z = b.Z[i],
                        Kod = b.Sinif[i].ToString(CultureInfo.InvariantCulture)
                    }), dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya kaydedilemedi: " + ex.Message, "Dışa Aktar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }

                MessageBox.Show(secilenler.Count.ToString("N0") + " nokta kaydedildi:\n" + dlg.FileName, "Dışa Aktar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
