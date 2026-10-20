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
    /// Görünüm menüsü: çizim ekranını 1-4 pencereye bölme, arka plan rengi ve etiket ayarları (nokta adı/kotu/kodu,
    /// alan adları, çizgi kotları, kalın obje çizimleri).
    /// <para>Çok pencereli görünümde aktif pencere her zaman asıl çizim panelidir (bütün komutlar onunla çalışır);
    /// diğer pencereler aynı objeleri kendi pan/zoom'larıyla gösterir. Aktif olmayan bir pencereye tıklanınca o pencere
    /// ile asıl panel yer ve görünüm değiştirir; tıklanan yer aktif pencere olur.</para>
    /// </summary>
    public partial class CizimEkrani
    {
        private readonly GorunumSecenekleri _gorunumSecenekleri = new GorunumSecenekleri();
        private TableLayoutPanel _pencereTablosu;
        private readonly List<GorunumPenceresi> _ikincilPencereler = new List<GorunumPenceresi>();
        private readonly Timer _pencereYenileme = new Timer { Interval = 300 };
        private bool _pencerelerYenilenecek;
        private int _pencereSayisi = 1;

        private static string GorunumAyarDosyasi => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XmlToKtb", "gorunum.ini");

        private void GorunumMenusunuOlustur()
        {
            GorunumAyarlariniYukle();
            cizimPaneli1.Secenekler = _gorunumSecenekleri;

            var menu = new ToolStripMenuItem("Görünüm");
            var pencereOgeleri = new List<ToolStripMenuItem>();
            foreach (var adet in new[] { 1, 2, 3, 4 })
            {
                var oge = new ToolStripMenuItem(adet == 1 ? "Tek Pencere" : adet + " Pencerede Aç", PencereSimgesi(adet), (s, e) => PencereDuzeniKur(adet))
                {
                    Checked = adet == 1
                };
                pencereOgeleri.Add(oge);
                menu.DropDownItems.Add(oge);
            }
            var ozellikOgesi = new ToolStripMenuItem("Özellikler Penceresi", null, (s, e) => OzellikleriAcKapat());
            menu.DropDownOpening += (s, e) =>
            {
                for (var i = 0; i < pencereOgeleri.Count; i++)
                    pencereOgeleri[i].Checked = i + 1 == _pencereSayisi;
                ozellikOgesi.Checked = OzelliklerAcik;
            };
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(ozellikOgesi);
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add("Arka Plan Rengi Seç...", null, (s, e) => ArkaPlanRengiSec());
            menu.DropDownItems.Add(new ToolStripSeparator());

            var etiket = new ToolStripMenuItem("Etiket Ayarları");
            var etiketMenusu = (ToolStripDropDownMenu)etiket.DropDown;
            etiketMenusu.ShowCheckMargin = true;
            void Secenek(string metin, Func<bool> oku, Action<bool> yaz)
            {
                var oge = new ToolStripMenuItem(metin) { CheckOnClick = true, Checked = oku() };
                oge.CheckedChanged += (s, e) =>
                {
                    yaz(oge.Checked);
                    GorunumAyarlariniKaydet();
                    TumPencereleriYenile();
                };
                etiket.DropDownItems.Add(oge);
            }
            var o = _gorunumSecenekleri;
            Secenek("Nokta Adları", () => o.NoktaAdlari, v => o.NoktaAdlari = v);
            Secenek("Nokta Kotları", () => o.NoktaKotlari, v => o.NoktaKotlari = v);
            Secenek("Nokta Kodları", () => o.NoktaKodlari, v => o.NoktaKodlari = v);
            Secenek("Alan Adları", () => o.AlanAdlari, v => o.AlanAdlari = v);
            Secenek("Çizgi Kotları", () => o.CizgiKotlari, v => o.CizgiKotlari = v);
            etiket.DropDownItems.Add(new ToolStripSeparator());
            Secenek("Kalın Obje Çizimleri", () => o.KalinCizim, v => o.KalinCizim = v);
            etiket.DropDownItems.Add(new ToolStripSeparator());
            var boyOgesi = new ToolStripMenuItem();
            void BoyMetniniGuncelle() => boyOgesi.Text = "Etiket Yazı Büyüklüğü (" + o.EtiketBoyu.ToString("0.#", CultureInfo.InvariantCulture) + ")...";
            BoyMetniniGuncelle();
            boyOgesi.Click += (s, e) =>
            {
                etiketMenusu.Close();
                BeginInvoke((Action)(() =>
                {
                    using (var dlg = new OlcekFaktoruForm("Etiket Yazı Büyüklüğü", "Büyüklük (punto):", o.EtiketBoyu))
                    {
                        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Deger <= 0)
                            return;
                        o.EtiketBoyu = (float)Math.Min(dlg.Deger, 200);
                        BoyMetniniGuncelle();
                        GorunumAyarlariniKaydet();
                        TumPencereleriYenile();
                    }
                }));
            };
            etiket.DropDownItems.Add(boyOgesi);
            // Bir seçenek tıklanınca menü açık kalır; birkaç seçenek art arda değiştirilebilir.
            etiketMenusu.Closing += (s, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                    e.Cancel = true;
            };
            menu.DropDownItems.Add(etiket);

            var secimSirasi = anaMenuStrip.Items.Cast<ToolStripItem>().ToList().FindIndex(i => i.Text == "Seçim");
            anaMenuStrip.Items.Insert(secimSirasi >= 0 ? secimSirasi + 1 : anaMenuStrip.Items.Count, menu);

            // Asıl panel her yeniden çizildiğinde (obje eklendi, silindi, taşındı...) diğer pencereler de kısa bir
            // gecikmeyle yenilenir; fare hareketindeki sık çizimler böylece tek yenilemede birleşir.
            cizimPaneli1.Paint += (s, e) => _pencerelerYenilenecek = true;
            _pencereYenileme.Tick += (s, e) =>
            {
                if (!_pencerelerYenilenecek)
                    return;
                _pencerelerYenilenecek = false;
                foreach (var pencere in _ikincilPencereler)
                    pencere.Invalidate();
            };
            _pencereYenileme.Start();
            FormClosed += (s, e) => _pencereYenileme.Stop();
        }

        private void TumPencereleriYenile()
        {
            cizimPaneli1.Invalidate();
            foreach (var pencere in _ikincilPencereler)
                pencere.Invalidate();
        }

        private void ArkaPlanRengiSec()
        {
            using (var dlg = new ColorDialog { Color = cizimPaneli1.BackColor, FullOpen = true })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                cizimPaneli1.BackColor = dlg.Color;
                GorunumAyarlariniKaydet();
                TumPencereleriYenile();
            }
        }

        /// <summary>
        /// Çizim alanını 1-4 pencereye böler: 2 yan yana; 3 solda büyük, sağda alt alta iki; 4 ikiye iki. Asıl panel
        /// ilk pencerede kalır; yeni pencereler asıl panelin o anki görünümüyle açılır.
        /// </summary>
        private void PencereDuzeniKur(int adet)
        {
            if (_pencereTablosu == null)
            {
                _pencereTablosu = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.FromArgb(150, 150, 150) };
                _pencereTablosu.CellPaint += PencereTablosu_CellPaint;
                var ust = cizimPaneli1.Parent;
                var sira = ust.Controls.GetChildIndex(cizimPaneli1);
                ust.SuspendLayout();
                ust.Controls.Remove(cizimPaneli1);
                ust.Controls.Add(_pencereTablosu);
                ust.Controls.SetChildIndex(_pencereTablosu, sira);
                ust.ResumeLayout();
            }

            var tablo = _pencereTablosu;
            tablo.SuspendLayout();
            tablo.Controls.Clear();
            foreach (var pencere in _ikincilPencereler)
                pencere.Dispose();
            _ikincilPencereler.Clear();
            tablo.ColumnStyles.Clear();
            tablo.RowStyles.Clear();

            var sutun = adet == 1 ? 1 : 2;
            var satir = adet <= 2 ? 1 : 2;
            tablo.ColumnCount = sutun;
            tablo.RowCount = satir;
            for (var i = 0; i < sutun; i++)
                tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / sutun));
            for (var i = 0; i < satir; i++)
                tablo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / satir));

            // Hücreler: {sütun, satır, satır yayılımı}. İlki asıl paneldir.
            int[][] hucreler;
            switch (adet)
            {
                case 2: hucreler = new[] { new[] { 0, 0, 1 }, new[] { 1, 0, 1 } }; break;
                case 3: hucreler = new[] { new[] { 0, 0, 2 }, new[] { 1, 0, 1 }, new[] { 1, 1, 1 } }; break;
                case 4: hucreler = new[] { new[] { 0, 0, 1 }, new[] { 1, 0, 1 }, new[] { 0, 1, 1 }, new[] { 1, 1, 1 } }; break;
                default: hucreler = new[] { new[] { 0, 0, 1 } }; break;
            }

            var kenar = adet == 1 ? Padding.Empty : new Padding(2);
            cizimPaneli1.Dock = DockStyle.Fill;
            cizimPaneli1.Margin = kenar;
            tablo.Controls.Add(cizimPaneli1, hucreler[0][0], hucreler[0][1]);
            tablo.SetRowSpan(cizimPaneli1, hucreler[0][2]);
            for (var i = 1; i < hucreler.Length; i++)
            {
                var pencere = new GorunumPenceresi(cizimPaneli1) { Dock = DockStyle.Fill, Margin = kenar };
                pencere.Gorunum.Olcek = cizimPaneli1.Gorunum.Olcek;
                pencere.Gorunum.OfsetX = cizimPaneli1.Gorunum.OfsetX;
                pencere.Gorunum.OfsetY = cizimPaneli1.Gorunum.OfsetY;
                pencere.Aktiflestirildi += (s, e) => PencereyiAktifYap((GorunumPenceresi)s);
                _ikincilPencereler.Add(pencere);
                tablo.Controls.Add(pencere, hucreler[i][0], hucreler[i][1]);
                tablo.SetRowSpan(pencere, hucreler[i][2]);
            }
            tablo.ResumeLayout();
            _pencereSayisi = adet;
            tablo.Invalidate();
            cizimPaneli1.Focus();
            TumPencereleriYenile();
        }

        /// <summary>Tıklanan pencere ile asıl panelin yerlerini ve görünümlerini değiştirir: tıklanan yer aktif olur.</summary>
        private void PencereyiAktifYap(GorunumPenceresi pencere)
        {
            var tablo = _pencereTablosu;
            var pencereYeri = tablo.GetCellPosition(pencere);
            var pencereYayilimi = tablo.GetRowSpan(pencere);
            var panelYeri = tablo.GetCellPosition(cizimPaneli1);
            var panelYayilimi = tablo.GetRowSpan(cizimPaneli1);

            var g = cizimPaneli1.Gorunum;
            var p = pencere.Gorunum;
            double olcek = g.Olcek, ofsetX = g.OfsetX, ofsetY = g.OfsetY;
            g.Olcek = p.Olcek;
            g.OfsetX = p.OfsetX;
            g.OfsetY = p.OfsetY;
            p.Olcek = olcek;
            p.OfsetX = ofsetX;
            p.OfsetY = ofsetY;

            tablo.SuspendLayout();
            tablo.SetCellPosition(pencere, panelYeri);
            tablo.SetRowSpan(pencere, panelYayilimi);
            tablo.SetCellPosition(cizimPaneli1, pencereYeri);
            tablo.SetRowSpan(cizimPaneli1, pencereYayilimi);
            tablo.ResumeLayout();
            tablo.Invalidate();
            cizimPaneli1.Focus();
            TumPencereleriYenile();
        }

        // Aktif pencerenin hücresi mavi çerçeveyle gösterilir (pencerelerin 2 piksellik kenar boşluğunda görünür).
        private void PencereTablosu_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            if (_pencereSayisi < 2)
                return;
            var yer = _pencereTablosu.GetCellPosition(cizimPaneli1);
            var yayilim = _pencereTablosu.GetRowSpan(cizimPaneli1);
            if (e.Column == yer.Column && e.Row >= yer.Row && e.Row < yer.Row + yayilim)
                e.Graphics.FillRectangle(Brushes.DodgerBlue, e.CellBounds);
        }

        private static Bitmap PencereSimgesi(int adet)
        {
            var simge = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(simge))
            using (var kalem = new Pen(Color.FromArgb(60, 60, 60)))
            {
                g.FillRectangle(Brushes.White, 1, 2, 14, 12);
                g.DrawRectangle(kalem, 1, 2, 14, 12);
                if (adet >= 2)
                    g.DrawLine(kalem, 8, 2, 8, 14);
                if (adet == 3)
                    g.DrawLine(kalem, 8, 8, 15, 8);
                if (adet == 4)
                    g.DrawLine(kalem, 1, 8, 15, 8);
            }
            return simge;
        }

        // ------------------------------------------------------------------ ayarların saklanması

        private void GorunumAyarlariniYukle()
        {
            try
            {
                if (!File.Exists(GorunumAyarDosyasi))
                    return;
                var o = _gorunumSecenekleri;
                foreach (var satir in File.ReadAllLines(GorunumAyarDosyasi))
                {
                    var p = satir.Split(new[] { '=' }, 2);
                    if (p.Length != 2)
                        continue;
                    var acik = p[1].Trim() == "1";
                    switch (p[0].Trim())
                    {
                        case "ArkaPlan":
                            if (int.TryParse(p[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
                                cizimPaneli1.BackColor = Color.FromArgb(255, Color.FromArgb(argb));
                            break;
                        case "NoktaAdlari": o.NoktaAdlari = acik; break;
                        case "NoktaKotlari": o.NoktaKotlari = acik; break;
                        case "NoktaKodlari": o.NoktaKodlari = acik; break;
                        case "AlanAdlari": o.AlanAdlari = acik; break;
                        case "CizgiKotlari": o.CizgiKotlari = acik; break;
                        case "KalinCizim": o.KalinCizim = acik; break;
                        case "EtiketBoyu":
                            if (float.TryParse(p[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var boy) && boy > 0)
                                o.EtiketBoyu = boy;
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Ayar dosyası okunamazsa varsayılanlarla devam edilir.
            }
        }

        private void GorunumAyarlariniKaydet()
        {
            try
            {
                var o = _gorunumSecenekleri;
                string B(bool v) => v ? "1" : "0";
                Directory.CreateDirectory(Path.GetDirectoryName(GorunumAyarDosyasi));
                File.WriteAllLines(GorunumAyarDosyasi, new[]
                {
                    "ArkaPlan=" + (cizimPaneli1.BackColor.ToArgb() & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture),
                    "NoktaAdlari=" + B(o.NoktaAdlari),
                    "NoktaKotlari=" + B(o.NoktaKotlari),
                    "NoktaKodlari=" + B(o.NoktaKodlari),
                    "AlanAdlari=" + B(o.AlanAdlari),
                    "CizgiKotlari=" + B(o.CizgiKotlari),
                    "KalinCizim=" + B(o.KalinCizim),
                    "EtiketBoyu=" + o.EtiketBoyu.ToString(CultureInfo.InvariantCulture)
                });
            }
            catch (Exception)
            {
                // Ayarlar kaydedilemezse yalnızca bu oturumda geçerli olur.
            }
        }
    }
}
