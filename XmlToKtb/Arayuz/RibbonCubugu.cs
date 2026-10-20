using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace XmlToKtb.Arayuz
{
    /// <summary>
    /// AutoCAD benzeri ribbon: solda Dosya (uygulama) düğmesi ve hızlı erişim simgeleri, yanında sekme başlıkları; altta
    /// seçili sekmenin grupları (büyük ve küçük düğmeler, grup adı altta). Düğmeler bir menü öğesine bağlıysa onun
    /// komutunu çalıştırır, etkin/tikli durumunu gösterir; alt menüsü olan öğe açılır düğme olur. Sekme başlığına çift
    /// tıklamak ribbon'u daraltır/genişletir.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class RibbonCubugu : Panel
    {
        public const int BaslikYuksekligi = 26;
        public const int GovdeYuksekligi = 94;

        internal static readonly Color GovdeRengi = Color.FromArgb(245, 246, 247);
        internal static readonly Color BaslikRengi = Color.FromArgb(225, 229, 234);
        internal static readonly Color CizgiRengi = Color.FromArgb(210, 213, 218);
        internal static readonly Color UzerindeRengi = Color.FromArgb(229, 241, 251);
        internal static readonly Color UzerindeCerceve = Color.FromArgb(160, 200, 235);
        internal static readonly Color BasiliRengi = Color.FromArgb(204, 228, 247);
        internal static readonly Color BasiliCerceve = Color.FromArgb(98, 162, 228);
        internal static readonly Font Yazi = new Font("Segoe UI", 8.25f);

        private readonly Panel _baslik = new Panel { Dock = DockStyle.Top, Height = BaslikYuksekligi, BackColor = BaslikRengi };
        private readonly FlowLayoutPanel _sol = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, Padding = new Padding(2, 1, 6, 0), BackColor = BaslikRengi };
        private readonly SekmeBasliklari _basliklar;
        private readonly Panel _govde = new Panel { Dock = DockStyle.Fill, BackColor = GovdeRengi };
        private readonly List<RibbonSekmesi> _sekmeler = new List<RibbonSekmesi>();
        private readonly Timer _durumZamanlayici = new Timer { Interval = 400 };
        internal readonly ToolTip IpucuKutusu = new ToolTip { InitialDelay = 500, ReshowDelay = 100 };
        private RibbonSekmesi _aktif;
        private bool _daraltilmis;

        /// <summary>Ribbon'dan bir komut çalıştırıldıktan sonra tetiklenir (ör. geri al kaydı için).</summary>
        public event Action KomutCalisti;

        public RibbonCubugu()
        {
            Dock = DockStyle.Top;
            Height = BaslikYuksekligi + GovdeYuksekligi;
            BackColor = GovdeRengi;
            DoubleBuffered = true;
            _basliklar = new SekmeBasliklari(this) { Dock = DockStyle.Fill };
            _baslik.Controls.Add(_basliklar);
            _baslik.Controls.Add(_sol);
            Controls.Add(_govde);
            Controls.Add(_baslik);
            _govde.Paint += (s, e) =>
            {
                using (var kalem = new Pen(CizgiRengi))
                    e.Graphics.DrawLine(kalem, 0, _govde.Height - 1, _govde.Width, _govde.Height - 1);
            };
            // Bağlı menü öğelerinin etkin/tikli durumu değişince düğmeler yenilenir.
            _durumZamanlayici.Tick += (s, e) =>
            {
                if (_aktif != null && !_daraltilmis)
                    foreach (var dugme in _aktif.Dugmeler())
                        dugme.DurumuDenetle();
                foreach (var dugme in _sol.Controls.OfType<RibbonDugmesi>())
                    dugme.DurumuDenetle();
            };
            _durumZamanlayici.Start();
            Disposed += (s, e) => _durumZamanlayici.Dispose();
        }

        public IReadOnlyList<RibbonSekmesi> Sekmeler => _sekmeler;
        public RibbonSekmesi AktifSekme => _aktif;

        public bool Daraltilmis
        {
            get => _daraltilmis;
            set
            {
                _daraltilmis = value;
                _govde.Visible = !value;
                Height = BaslikYuksekligi + (value ? 0 : GovdeYuksekligi);
                _basliklar.Invalidate();
            }
        }

        internal void KomutCalistiBildir()
        {
            KomutCalisti?.Invoke();
            // Komut menü durumlarını değiştirmiş olabilir (ör. pencere sayısı, tikli seçenekler).
            BeginInvoke((Action)(() => _aktif?.Yenile()));
        }

        /// <summary>Solda mavi "Dosya" düğmesi: tıklanınca verilen menünün kopyası açılır.</summary>
        public void UygulamaDugmesiEkle(string metin, ToolStripMenuItem menu)
        {
            _sol.Controls.Add(new UygulamaDugmesi(this, metin, menu));
        }

        /// <summary>Hızlı erişim (yalnızca simge) düğmesi.</summary>
        public void HizliErisimEkle(string ipucu, ToolStripMenuItem kaynak)
        {
            if (kaynak == null)
                return;
            var dugme = new RibbonDugmesi(this, ipucu, RibbonSimgeleri.Al(ipucu, 16), DugmeBoyu.Simge) { Kaynak = kaynak, Margin = new Padding(1, 1, 1, 0) };
            _sol.Controls.Add(dugme);
        }

        public RibbonSekmesi SekmeEkle(string ad)
        {
            var sekme = new RibbonSekmesi(this, ad);
            _sekmeler.Add(sekme);
            sekme.Icerik.Visible = false;
            _govde.Controls.Add(sekme.Icerik);
            if (_aktif == null)
                SekmeSec(sekme);
            _basliklar.Invalidate();
            return sekme;
        }

        public void SekmeSec(RibbonSekmesi sekme)
        {
            if (_aktif != null)
                _aktif.Icerik.Visible = false;
            _aktif = sekme;
            sekme.Yenile();
            sekme.Icerik.Visible = true;
            _basliklar.Invalidate();
        }

        // ------------------------------------------------------------------ sekme başlıkları

        private sealed class SekmeBasliklari : Control
        {
            private readonly RibbonCubugu _ribbon;
            private int _uzerinde = -1;

            public SekmeBasliklari(RibbonCubugu ribbon)
            {
                _ribbon = ribbon;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = BaslikRengi;
            }

            private List<Rectangle> Kutular(Graphics g)
            {
                var kutular = new List<Rectangle>();
                var x = 4;
                foreach (var sekme in _ribbon._sekmeler)
                {
                    var w = (int)Math.Ceiling(g.MeasureString(sekme.Ad, Yazi).Width) + 18;
                    kutular.Add(new Rectangle(x, 3, w, Height - 3));
                    x += w + 1;
                }
                return kutular;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var kutular = Kutular(e.Graphics);
                using (var cizgi = new Pen(CizgiRengi))
                {
                    e.Graphics.DrawLine(cizgi, 0, Height - 1, Width, Height - 1);
                    for (var i = 0; i < kutular.Count; i++)
                    {
                        var r = kutular[i];
                        var aktif = _ribbon._sekmeler[i] == _ribbon._aktif && !_ribbon._daraltilmis;
                        if (aktif)
                        {
                            using (var f = new SolidBrush(GovdeRengi))
                                e.Graphics.FillRectangle(f, r.X, r.Y, r.Width, r.Height);
                            e.Graphics.DrawLines(cizgi, new[] { new Point(r.X, r.Bottom), new Point(r.X, r.Y), new Point(r.Right, r.Y), new Point(r.Right, r.Bottom) });
                        }
                        else if (i == _uzerinde)
                        {
                            using (var f = new SolidBrush(Color.FromArgb(236, 239, 243)))
                                e.Graphics.FillRectangle(f, r.X, r.Y, r.Width, r.Height - 1);
                        }
                        TextRenderer.DrawText(e.Graphics, _ribbon._sekmeler[i].Ad, Yazi, r, aktif ? Color.FromArgb(0, 84, 166) : Color.FromArgb(50, 50, 50),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
            }

            private int Bul(Point p)
            {
                using (var g = CreateGraphics())
                    return Kutular(g).FindIndex(r => r.Contains(p));
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                var i = Bul(e.Location);
                if (i != _uzerinde)
                {
                    _uzerinde = i;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _uzerinde = -1;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                var i = Bul(e.Location);
                if (i < 0 || e.Button != MouseButtons.Left)
                    return;
                if (e.Clicks == 2)
                {
                    _ribbon.Daraltilmis = !_ribbon.Daraltilmis;
                    return;
                }
                if (_ribbon._daraltilmis)
                    _ribbon.Daraltilmis = false;
                _ribbon.SekmeSec(_ribbon._sekmeler[i]);
            }
        }

        // ------------------------------------------------------------------ menü kopyası

        private static readonly MethodInfo AcilisOlayi = typeof(ToolStripDropDownItem).GetMethod("OnDropDownOpening", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Menünün açılırken kendini güncelleyen (DropDownOpening) kodunu çalıştırır; tikler ve dinamik listeler güncellenir.</summary>
        internal static void AcilisiTetikle(ToolStripMenuItem menu)
        {
            try
            {
                AcilisOlayi?.Invoke(menu, new object[] { EventArgs.Empty });
            }
            catch (TargetInvocationException)
            {
                // Menünün kendi güncelleme kodu hata verirse kopya yine eski haliyle gösterilir.
            }
        }

        /// <summary>Menü öğesinin alt öğelerinden açılır menü kurar; tıklanan öğe asıl menü öğesinin komutunu çalıştırır.</summary>
        internal ContextMenuStrip MenuKopyasi(ToolStripMenuItem kaynak)
        {
            var menu = new ContextMenuStrip { ShowImageMargin = true, ShowCheckMargin = kaynak.DropDownItems.OfType<ToolStripMenuItem>().Any(m => m.CheckOnClick || m.Checked) };
            MenuOgeleriniKopyala(kaynak, menu.Items);
            menu.Closed += (s, e) => BeginInvoke((Action)menu.Dispose);
            return menu;
        }

        private void MenuOgeleriniKopyala(ToolStripMenuItem kaynak, ToolStripItemCollection hedef)
        {
            AcilisiTetikle(kaynak);
            foreach (ToolStripItem oge in kaynak.DropDownItems)
            {
                if (oge is ToolStripSeparator)
                {
                    hedef.Add(new ToolStripSeparator());
                    continue;
                }
                if (!(oge is ToolStripMenuItem m) || !m.Available)
                    continue;
                var kopya = new ToolStripMenuItem(m.Text, m.Image ?? RibbonSimgeleri.Al(m.Text, 16))
                {
                    Checked = m.Checked,
                    Enabled = m.Enabled,
                    ShortcutKeyDisplayString = m.ShortcutKeyDisplayString ?? (m.ShortcutKeys != Keys.None ? new KeysConverter().ConvertToString(m.ShortcutKeys) : null),
                    ToolTipText = m.ToolTipText
                };
                if (m.DropDownItems.Count > 0)
                    MenuOgeleriniKopyala(m, kopya.DropDownItems);
                else
                    kopya.Click += (s, e) =>
                    {
                        m.PerformClick();
                        KomutCalistiBildir();
                    };
                hedef.Add(kopya);
            }
        }

        // ------------------------------------------------------------------ Dosya düğmesi

        private sealed class UygulamaDugmesi : Control
        {
            private readonly RibbonCubugu _ribbon;
            private readonly ToolStripMenuItem _menu;
            private bool _uzerinde;

            public UygulamaDugmesi(RibbonCubugu ribbon, string metin, ToolStripMenuItem menu)
            {
                _ribbon = ribbon;
                _menu = menu;
                Text = metin;
                Size = new Size(64, BaslikYuksekligi - 2);
                Margin = new Padding(0, 0, 6, 0);
                Cursor = Cursors.Hand;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using (var f = new SolidBrush(_uzerinde ? Color.FromArgb(25, 110, 200) : Color.FromArgb(0, 90, 170)))
                    e.Graphics.FillRectangle(f, ClientRectangle);
                using (var yazi = new Font(Yazi, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, Text, yazi, ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            protected override void OnMouseEnter(EventArgs e) { _uzerinde = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { _uzerinde = false; Invalidate(); }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    _ribbon.MenuKopyasi(_menu).Show(this, new Point(0, Height));
            }
        }
    }

    public enum DugmeBoyu
    {
        Buyuk,
        Kucuk,
        Simge
    }

    /// <summary>Ribbon sekmesi: grupları yatay dizen kaydırılabilir içerik.</summary>
    public sealed class RibbonSekmesi
    {
        private readonly RibbonCubugu _ribbon;
        public string Ad { get; }
        internal readonly FlowLayoutPanel Icerik = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Padding = new Padding(2, 2, 2, 0), BackColor = RibbonCubugu.GovdeRengi
        };

        /// <summary>Sekme açılırken çalışır (ör. bağlı menülerin açılış güncellemesi).</summary>
        public List<ToolStripMenuItem> GuncellenecekMenuler { get; } = new List<ToolStripMenuItem>();

        internal RibbonSekmesi(RibbonCubugu ribbon, string ad)
        {
            _ribbon = ribbon;
            Ad = ad;
        }

        public RibbonGrubu GrupEkle(string ad)
        {
            var grup = new RibbonGrubu(_ribbon, ad);
            Icerik.Controls.Add(grup);
            return grup;
        }

        internal IEnumerable<RibbonDugmesi> Dugmeler() => Icerik.Controls.OfType<RibbonGrubu>().SelectMany(g => g.Controls.OfType<RibbonDugmesi>());

        internal void Yenile()
        {
            foreach (var menu in GuncellenecekMenuler)
                RibbonCubugu.AcilisiTetikle(menu);
            foreach (var dugme in Dugmeler())
                dugme.DurumuDenetle();
        }
    }

    /// <summary>
    /// Ribbon grubu: büyük düğmeler yan yana, küçük düğmeler üçer satırlık sütunlar halinde dizilir; grup adı altta,
    /// sağda ayraç çizgisi.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public sealed class RibbonGrubu : Control
    {
        private const int AdYuksekligi = 17;
        private readonly RibbonCubugu _ribbon;

        internal RibbonGrubu(RibbonCubugu ribbon, string ad)
        {
            _ribbon = ribbon;
            Text = ad;
            Height = RibbonCubugu.GovdeYuksekligi - 6;
            Margin = new Padding(0, 0, 0, 0);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = RibbonCubugu.GovdeRengi;
        }

        /// <summary>Menü öğesine bağlı düğme ekler; alt menüsü varsa açılır düğme olur.</summary>
        public RibbonDugmesi DugmeEkle(ToolStripMenuItem kaynak, DugmeBoyu boyu, string metin = null, string simgeAnahtari = null)
        {
            if (kaynak == null)
                return null;
            var yazi = metin ?? KisaMetin(kaynak.Text);
            var anahtar = simgeAnahtari ?? kaynak.Text;
            var dugme = new RibbonDugmesi(_ribbon, yazi, RibbonSimgeleri.Al(anahtar, boyu == DugmeBoyu.Buyuk ? 32 : 16), boyu)
            {
                Kaynak = kaynak,
                Acilir = kaynak.DropDownItems.Count > 0,
                Ipucu = kaynak.Text.Replace("&", "") + (kaynak.ShortcutKeys != Keys.None ? "  (" + new KeysConverter().ConvertToString(kaynak.ShortcutKeys) + ")" : "") +
                        (string.IsNullOrEmpty(kaynak.ToolTipText) ? "" : "\n" + kaynak.ToolTipText)
            };
            Controls.Add(dugme);
            Duzenle();
            return dugme;
        }

        /// <summary>Ribbon düğmesinde gösterilecek kısa ad: "..." ve parantez içi açıklama atılır.</summary>
        public static string KisaMetin(string metin)
        {
            var t = (metin ?? "").Replace("&", "").Replace("...", "").Trim();
            var p = t.IndexOf(" (", StringComparison.Ordinal);
            if (p > 0)
                t = t.Substring(0, p).Trim();
            return t;
        }

        internal void Duzenle()
        {
            var x = 3;
            var satir = 0;
            var sutunGenisligi = 0;
            const int ust = 2;
            foreach (var dugme in Controls.OfType<RibbonDugmesi>())
            {
                if (dugme.Boyu == DugmeBoyu.Buyuk)
                {
                    if (satir > 0)
                    {
                        x += sutunGenisligi + 2;
                        satir = 0;
                        sutunGenisligi = 0;
                    }
                    dugme.Location = new Point(x, ust);
                    x += dugme.Width + 2;
                }
                else
                {
                    dugme.Location = new Point(x, ust + satir * 23);
                    sutunGenisligi = Math.Max(sutunGenisligi, dugme.Width);
                    satir++;
                    if (satir == 3)
                    {
                        x += sutunGenisligi + 2;
                        satir = 0;
                        sutunGenisligi = 0;
                    }
                }
            }
            if (satir > 0)
                x += sutunGenisligi + 2;
            using (var g = CreateGraphics())
                Width = Math.Max(x + 4, (int)g.MeasureString(Text, RibbonCubugu.Yazi).Width + 14);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var adAlani = new Rectangle(0, Height - AdYuksekligi, Width - 1, AdYuksekligi);
            TextRenderer.DrawText(e.Graphics, Text, RibbonCubugu.Yazi, adAlani, Color.FromArgb(95, 95, 95), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (var kalem = new Pen(RibbonCubugu.CizgiRengi))
                e.Graphics.DrawLine(kalem, Width - 1, 4, Width - 1, Height - 4);
        }
    }

    /// <summary>Ribbon düğmesi: büyük (simge üstte, yazı altta), küçük (simge solda) ya da yalnızca simge.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public sealed class RibbonDugmesi : Control
    {
        private readonly RibbonCubugu _ribbon;
        private readonly Bitmap _simge;
        private bool _uzerinde, _basili;
        private bool _sonEtkin = true, _sonTikli;

        public DugmeBoyu Boyu { get; }
        public ToolStripMenuItem Kaynak { get; set; }
        public Action Eylem { get; set; }

        private bool _acilir;

        /// <summary>Alt menüsü olan düğme: tıklanınca menü açılır, yazının yanında ▾ görünür.</summary>
        public bool Acilir
        {
            get => _acilir;
            set
            {
                _acilir = value;
                Boyutlandir();
            }
        }

        public string Ipucu
        {
            set => _ribbon.IpucuKutusu.SetToolTip(this, value);
        }

        internal RibbonDugmesi(RibbonCubugu ribbon, string metin, Bitmap simge, DugmeBoyu boyu)
        {
            _ribbon = ribbon;
            _simge = simge;
            Boyu = boyu;
            Text = metin;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = boyu == DugmeBoyu.Simge ? RibbonCubugu.BaslikRengi : RibbonCubugu.GovdeRengi;
            Boyutlandir();
            if (boyu == DugmeBoyu.Simge)
                _ribbon.IpucuKutusu.SetToolTip(this, metin);
        }

        private bool Etkin => Kaynak == null || Kaynak.Enabled;
        private bool Tikli => Kaynak != null && Kaynak.Checked;

        internal void DurumuDenetle()
        {
            if (Etkin != _sonEtkin || Tikli != _sonTikli)
            {
                _sonEtkin = Etkin;
                _sonTikli = Tikli;
                Invalidate();
            }
        }

        private string[] Satirlar()
        {
            // Büyük düğmenin yazısı en fazla iki satır: sığmazsa kelimelerden bölünür.
            var kelimeler = Text.Split(' ');
            if (kelimeler.Length < 2 || TextRenderer.MeasureText(Text, RibbonCubugu.Yazi).Width <= 66)
                return new[] { Text };
            var enIyi = 1;
            var enKisa = int.MaxValue;
            for (var i = 1; i < kelimeler.Length; i++)
            {
                var a = TextRenderer.MeasureText(string.Join(" ", kelimeler.Take(i)), RibbonCubugu.Yazi).Width;
                var b = TextRenderer.MeasureText(string.Join(" ", kelimeler.Skip(i)), RibbonCubugu.Yazi).Width;
                if (Math.Max(a, b) < enKisa)
                {
                    enKisa = Math.Max(a, b);
                    enIyi = i;
                }
            }
            return new[] { string.Join(" ", kelimeler.Take(enIyi)), string.Join(" ", kelimeler.Skip(enIyi)) };
        }

        private void Boyutlandir()
        {
            switch (Boyu)
            {
                case DugmeBoyu.Buyuk:
                {
                    var genislik = Satirlar().Max(s => TextRenderer.MeasureText(s, RibbonCubugu.Yazi).Width) + 10 + (Acilir ? 4 : 0);
                    Size = new Size(Math.Max(46, genislik), RibbonCubugu.GovdeYuksekligi - 6 - 19);
                    break;
                }
                case DugmeBoyu.Kucuk:
                    Size = new Size(16 + 8 + TextRenderer.MeasureText(Text, RibbonCubugu.Yazi).Width + (Acilir ? 12 : 4), 22);
                    break;
                default:
                    Size = new Size(22, 22);
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Etkin && (_basili || Tikli || _uzerinde))
            {
                using (var f = new SolidBrush(_basili || Tikli ? RibbonCubugu.BasiliRengi : RibbonCubugu.UzerindeRengi))
                    g.FillRectangle(f, r);
                using (var k = new Pen(_basili || Tikli ? RibbonCubugu.BasiliCerceve : RibbonCubugu.UzerindeCerceve))
                    g.DrawRectangle(k, r);
            }
            var yaziRengi = Etkin ? Color.FromArgb(30, 30, 30) : Color.FromArgb(160, 160, 160);
            switch (Boyu)
            {
                case DugmeBoyu.Buyuk:
                {
                    var sx = (Width - 32) / 2;
                    SimgeCiz(g, new Rectangle(sx, 4, 32, 32));
                    var satirlar = Satirlar();
                    var y = 38;
                    for (var i = 0; i < satirlar.Length; i++)
                    {
                        var metin = satirlar[i] + (Acilir && i == satirlar.Length - 1 ? " ▾" : "");
                        TextRenderer.DrawText(g, metin, RibbonCubugu.Yazi, new Rectangle(0, y, Width, 15), yaziRengi, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                        y += 14;
                    }
                    if (Acilir && satirlar.Length == 1)
                        TextRenderer.DrawText(g, "▾", RibbonCubugu.Yazi, new Rectangle(0, y, Width, 14), yaziRengi, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                    break;
                }
                case DugmeBoyu.Kucuk:
                    SimgeCiz(g, new Rectangle(3, 3, 16, 16));
                    TextRenderer.DrawText(g, Text + (Acilir ? " ▾" : ""), RibbonCubugu.Yazi, new Rectangle(22, 0, Width - 22, Height), yaziRengi, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                    break;
                default:
                    SimgeCiz(g, new Rectangle(3, 3, 16, 16));
                    break;
            }
        }

        private void SimgeCiz(Graphics g, Rectangle r)
        {
            if (_simge == null)
                return;
            if (Etkin)
                g.DrawImage(_simge, r);
            else
                ControlPaint.DrawImageDisabled(g, _simge, r.X, r.Y, BackColor);
        }

        protected override void OnMouseEnter(EventArgs e) { _uzerinde = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _uzerinde = false; _basili = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !Etkin)
                return;
            _basili = true;
            Invalidate();
            if (Acilir && Kaynak != null)
            {
                _basili = false;
                _ribbon.MenuKopyasi(Kaynak).Show(this, new Point(0, Height));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!_basili)
                return;
            _basili = false;
            Invalidate();
            if (!ClientRectangle.Contains(e.Location) || !Etkin)
                return;
            Calistir();
        }

        /// <summary>Düğmenin komutunu çalıştırır (bağlı menü öğesine tıklanmış gibi).</summary>
        public void Calistir()
        {
            if (Eylem != null)
                Eylem();
            else if (Kaynak != null)
                Kaynak.PerformClick();
            else
                return;
            _ribbon.KomutCalistiBildir();
        }
    }
}
