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
    /// Ölçülendirme komutları (Çizim > Ölçülendirme): doğrusal, hizalı, açısal, yarıçap ve çap ölçüsü; Ölçü Stilleri
    /// penceresi. Yeni ölçüler aktif ölçü stilini kullanır.
    /// </summary>
    public partial class CizimEkrani
    {
        private static bool OlcuKomutuMu(DuzenKomutu komut) =>
            komut == DuzenKomutu.OlcuDogrusal || komut == DuzenKomutu.OlcuHizali || komut == DuzenKomutu.OlcuAcisal ||
            komut == DuzenKomutu.OlcuYaricap || komut == DuzenKomutu.OlcuCap || komut == DuzenKomutu.Kilavuz;

        private void OlcuMenusunuOlustur()
        {
            var olcu = new ToolStripMenuItem("Ölçülendirme");
            olcu.DropDownItems.Add("Doğrusal (Yatay / Düşey)", null, (s, e) => KomutBaslat(DuzenKomutu.OlcuDogrusal));
            olcu.DropDownItems.Add("Hizalı", null, (s, e) => KomutBaslat(DuzenKomutu.OlcuHizali));
            olcu.DropDownItems.Add("Açısal", null, (s, e) => KomutBaslat(DuzenKomutu.OlcuAcisal));
            olcu.DropDownItems.Add("Yarıçap", null, (s, e) => KomutBaslat(DuzenKomutu.OlcuYaricap));
            olcu.DropDownItems.Add("Çap", null, (s, e) => KomutBaslat(DuzenKomutu.OlcuCap));
            olcu.DropDownItems.Add(new ToolStripSeparator());
            olcu.DropDownItems.Add(new ToolStripMenuItem("Kılavuz Çizgi (Leader / MLeader)", null, (s, e) => KomutBaslat(DuzenKomutu.Kilavuz))
            {
                ToolTipText = "Ok ucundan başlayan, köşelerden geçen çizgi ve açıklama yazısı (AutoCAD LEADER / MLEADER)."
            });
            olcu.DropDownItems.Add(new ToolStripSeparator());
            var aktifStil = new ToolStripMenuItem("Aktif Ölçü Stili");
            olcu.DropDownItems.Add(aktifStil);
            olcu.DropDownItems.Add("Ölçü Stilleri...", null, (s, e) => OlcuStilleriniAc());
            // Aktif stil listesi her açılışta çizimin stillerinden yeniden kurulur.
            olcu.DropDownOpening += (s, e) =>
            {
                aktifStil.DropDownItems.Clear();
                var aktif = cizimPaneli1.AktifOlcuStili;
                foreach (var stil in cizimPaneli1.OlcuStilleri)
                {
                    var secilen = stil;
                    aktifStil.DropDownItems.Add(new ToolStripMenuItem(stil.Ad, null, (s2, e2) => cizimPaneli1.AktifOlcuStili = secilen) { Checked = stil == aktif });
                }
            };
            cizimMenuItem.DropDownItems.Add(new ToolStripSeparator());
            cizimMenuItem.DropDownItems.Add(olcu);
        }

        private void OlcuStilleriniAc()
        {
            KomutuBitir();
            using (var dlg = new OlcuStiliForm(cizimPaneli1))
                dlg.ShowDialog(this);
            cizimPaneli1.Invalidate();
            ozellikGrid.Refresh();
        }

        private string OlcuIpucu()
        {
            var n = _komutNoktalari.Count;
            switch (_duzenKomutu)
            {
                case DuzenKomutu.OlcuDogrusal:
                case DuzenKomutu.OlcuHizali:
                    return new[] { "1. noktayı tıklayın.", "2. noktayı tıklayın.", "ölçü çizgisinin yerini tıklayın" + (_duzenKomutu == DuzenKomutu.OlcuDogrusal ? " (fareyi yukarı/aşağı çekince yatay, sağa/sola çekince düşey ölçü)." : ".") }[Math.Min(2, n)];
                case DuzenKomutu.OlcuAcisal:
                    return new[] { "açının köşesini tıklayın.", "1. kolun üzerindeki bir noktayı tıklayın.", "2. kolun üzerindeki bir noktayı tıklayın.", "ölçü yayının yerini tıklayın (yayın olduğu taraftaki açı ölçülür)." }[Math.Min(3, n)];
                case DuzenKomutu.Kilavuz:
                    return n == 0
                        ? "okun ucunu (gösterilecek noktayı) tıklayın."
                        : (n + 1) + ". köşeyi tıklayın.  Enter / Boşluk / sağ tık: bitir ve yazıyı gir,  Backspace: son köşeyi geri al.";
                default:
                    return n == 0 ? "daire ya da yayı tıklayın." : "ölçünün yerini tıklayın.";
            }
        }

        private void OlcuNoktaTiklandi(double x, double y)
        {
            var komut = _duzenKomutu;
            if (komut == DuzenKomutu.Kilavuz)
            {
                // Köşeler bitirilene kadar toplanır; aynı noktaya art arda tıklama yok sayılır.
                var onceki = _komutNoktalari.LastOrDefault();
                if (onceki == null || Math.Abs(onceki[0] - x) > 1e-9 || Math.Abs(onceki[1] - y) > 1e-9)
                    _komutNoktalari.Add(new[] { x, y });
                KomutIpucunuGuncelle();
                cizimPaneli1.Invalidate();
                return;
            }
            if (komut == DuzenKomutu.OlcuYaricap || komut == DuzenKomutu.OlcuCap)
            {
                if (_komutNoktalari.Count == 0)
                {
                    // Daire ya da yay seçilir (üstte başka obje olsa da): merkezi ve yarıçapı saklanır.
                    var tolerans = 6 / cizimPaneli1.Gorunum.Olcek;
                    var tiklanan = new PointF((float)x, (float)y);
                    var nesne = cizimPaneli1.Nesneler.LastOrDefault(n => (n is DaireNesnesi || n is YayNesnesi) && (n.Tabaka == null || n.Tabaka.Gorunur) && n.IsabetEttiMi(tiklanan, tolerans));
                    double mx, my, r;
                    if (nesne is DaireNesnesi daire) { mx = daire.MerkezX; my = daire.MerkezY; r = daire.Yaricap; }
                    else if (nesne is YayNesnesi yay) { mx = yay.MerkezX; my = yay.MerkezY; r = yay.Yaricap; }
                    else
                    {
                        KomutIpucu(KomutAdlari[komut] + ": daire ya da yay bulunamadı; bir daire ya da yay tıklayın.");
                        return;
                    }
                    _komutNoktalari.Add(new[] { mx, my, r });
                    cizimPaneli1.NoktaAlmayaBasla(false);
                    KomutIpucunuGuncelle();
                    cizimPaneli1.Invalidate();
                    return;
                }
            }
            else
            {
                // Aynı noktaya art arda tıklama yok sayılır.
                var son = _komutNoktalari.LastOrDefault();
                if (son != null && Math.Abs(son[0] - x) < 1e-9 && Math.Abs(son[1] - y) < 1e-9)
                    return;
                var gereken = komut == DuzenKomutu.OlcuAcisal ? 3 : 2;
                if (_komutNoktalari.Count < gereken)
                {
                    _komutNoktalari.Add(new[] { x, y });
                    KomutIpucunuGuncelle();
                    cizimPaneli1.Invalidate();
                    return;
                }
            }

            var olcu = OlcuOlustur(x, y);
            KomutuBitir();
            if (olcu == null)
            {
                KomutIpucu("Ölçü oluşmadı: noktalar çakışıyor.");
                return;
            }
            Ekle(new List<CizimNesnesi> { olcu }, false);
            KomutIpucu("Ölçü eklendi: " + olcu.YaziMetni() + ". Yeni ölçü için Boşluk.");
        }

        /// <summary>Toplanan noktalar ve son (fare) noktasıyla ölçüyü kurar; geometri geçersizse null.</summary>
        private OlcuNesnesi OlcuOlustur(double x, double y)
        {
            var p = _komutNoktalari;
            var stil = cizimPaneli1.AktifOlcuStili;
            switch (_duzenKomutu)
            {
                case DuzenKomutu.OlcuDogrusal:
                case DuzenKomutu.OlcuHizali:
                {
                    if (p.Count < 2 || (Math.Abs(p[0][0] - p[1][0]) < 1e-9 && Math.Abs(p[0][1] - p[1][1]) < 1e-9))
                        return null;
                    var olcu = new OlcuNesnesi
                    {
                        Tur = _duzenKomutu == DuzenKomutu.OlcuDogrusal ? OlcuTuru.Dogrusal : OlcuTuru.Hizali,
                        AX = p[0][0], AY = p[0][1], BX = p[1][0], BY = p[1][1], CX = x, CY = y, DX = x, DY = y, Stil = stil
                    };
                    if (olcu.Tur == OlcuTuru.Dogrusal)
                    {
                        // Fare iki noktanın yatay aralığının dışına, düşey aralığının dışına olduğundan daha çok
                        // çıkmışsa düşey ölçü; değilse yatay ölçü (AutoCAD DIMLINEAR).
                        double ortaX = (p[0][0] + p[1][0]) / 2, ortaY = (p[0][1] + p[1][1]) / 2;
                        var tasmaX = Math.Abs(x - ortaX) - Math.Abs(p[1][0] - p[0][0]) / 2;
                        var tasmaY = Math.Abs(y - ortaY) - Math.Abs(p[1][1] - p[0][1]) / 2;
                        olcu.Aci = tasmaX > tasmaY ? 90 : 0;
                    }
                    return olcu;
                }
                case DuzenKomutu.OlcuAcisal:
                {
                    if (p.Count < 3)
                        return null;
                    return new OlcuNesnesi
                    {
                        Tur = OlcuTuru.Acisal, AX = p[0][0], AY = p[0][1], BX = p[1][0], BY = p[1][1], CX = p[2][0], CY = p[2][1], DX = x, DY = y, Stil = stil
                    };
                }
                default:
                {
                    if (p.Count < 1)
                        return null;
                    double mx = p[0][0], my = p[0][1], r = p[0][2];
                    double dx = x - mx, dy = y - my, l = Math.Sqrt(dx * dx + dy * dy);
                    if (l < 1e-9 || r < 1e-9)
                        return null;
                    double bx = mx + dx / l * r, by = my + dy / l * r;
                    return new OlcuNesnesi
                    {
                        Tur = _duzenKomutu == DuzenKomutu.OlcuYaricap ? OlcuTuru.Yaricap : OlcuTuru.Cap,
                        AX = mx, AY = my, BX = bx, BY = by, CX = bx, CY = by, DX = bx, DY = by, Stil = stil
                    };
                }
            }
        }

        /// <summary>Kılavuz çizgiyi bitirir: en az iki köşe varsa yazı sorulur (boş bırakılabilir) ve kılavuz eklenir.</summary>
        private void KilavuzuTamamla()
        {
            var koseler = _komutNoktalari.Select(k => new[] { k[0], k[1] }).ToList();
            KomutuBitir();
            if (koseler.Count < 2)
            {
                KomutIpucu("Kılavuz Çizgi: en az iki nokta (ok ucu ve bir köşe) gerekir.");
                return;
            }
            if (!KilavuzYazisiSor(out var metin))
                return;
            var kilavuz = new KilavuzNesnesi { Metin = metin, Stil = cizimPaneli1.AktifOlcuStili };
            kilavuz.Koseler.AddRange(koseler);
            Ekle(new List<CizimNesnesi> { kilavuz }, false);
            KomutIpucu("Kılavuz çizgi eklendi. Yeni kılavuz için Boşluk.");
        }

        private string _sonKilavuzYazisi = "";

        // Çok satırlı açıklama yazısı penceresi (Enter yeni satır, Ctrl+Enter ya da Tamam bitirir).
        private bool KilavuzYazisiSor(out string metin)
        {
            metin = null;
            using (var dlg = new Form
            {
                Text = "Kılavuz Çizgi Yazısı", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(380, 170)
            })
            {
                dlg.Controls.Add(new Label { Text = "Açıklama (birden çok satır olabilir; boş bırakılabilir):", Location = new Point(12, 10), AutoSize = true });
                var kutu = new TextBox
                {
                    Text = _sonKilavuzYazisi, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical,
                    Location = new Point(12, 30), Size = new Size(356, 92)
                };
                var tamam = new Button { Text = "Tamam", Location = new Point(212, 132), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(293, 132), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                kutu.KeyDown += (s, e) =>
                {
                    if (e.Control && e.KeyCode == Keys.Enter)
                    {
                        dlg.DialogResult = DialogResult.OK;
                        e.SuppressKeyPress = true;
                    }
                };
                dlg.Controls.AddRange(new Control[] { kutu, tamam, iptal });
                dlg.CancelButton = iptal;
                dlg.Shown += (s, e) => kutu.SelectAll();
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;
                metin = kutu.Text.TrimEnd();
                _sonKilavuzYazisi = metin;
                return true;
            }
        }

        // Fareyle değişen ölçü önizlemesi (yeterli nokta toplanınca), öncesinde verilen noktalar ve fareye kesikli çizgi.
        private void OlcuOnizlemesiCiz(Graphics grafik)
        {
            int w = cizimPaneli1.Width, h = cizimPaneli1.Height;
            var fare = cizimPaneli1.PointToClient(Cursor.Position);
            cizimPaneli1.Gorunum.EkrandanDunyayaHassas(fare, w, h, out var fx, out var fy);
            if (_duzenKomutu == DuzenKomutu.Kilavuz)
            {
                if (_komutNoktalari.Count == 0)
                    return;
                var onizleme = new KilavuzNesnesi { Stil = cizimPaneli1.AktifOlcuStili, OzelRenk = Color.Firebrick, Metin = "..." };
                onizleme.Koseler.AddRange(_komutNoktalari.Select(k => new[] { k[0], k[1] }));
                onizleme.Koseler.Add(new[] { fx, fy });
                onizleme.Ciz(grafik, cizimPaneli1.Gorunum, w, h);
                return;
            }
            var gereken = _duzenKomutu == DuzenKomutu.OlcuAcisal ? 3 : _duzenKomutu == DuzenKomutu.OlcuYaricap || _duzenKomutu == DuzenKomutu.OlcuCap ? 1 : 2;
            if (_komutNoktalari.Count >= gereken)
            {
                var onizleme = OlcuOlustur(fx, fy);
                if (onizleme != null)
                {
                    onizleme.OzelRenk = Color.Firebrick;
                    onizleme.Ciz(grafik, cizimPaneli1.Gorunum, w, h);
                }
                return;
            }
            if (_komutNoktalari.Count == 0)
                return;
            using (var kalem = new Pen(Color.Firebrick, 1.2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                var noktalar = _komutNoktalari.Select(p => cizimPaneli1.Gorunum.DunyadanEkrana(p[0], p[1], w, h)).ToList();
                // Açısal ölçüde kollar köşeden çıkar.
                if (_duzenKomutu == DuzenKomutu.OlcuAcisal)
                {
                    for (var i = 1; i < noktalar.Count; i++)
                        grafik.DrawLine(kalem, noktalar[0], noktalar[i]);
                    grafik.DrawLine(kalem, noktalar[0], fare);
                }
                else
                {
                    grafik.DrawLine(kalem, noktalar[noktalar.Count - 1], fare);
                }
                foreach (var p in noktalar)
                    grafik.DrawRectangle(kalem, p.X - 3, p.Y - 3, 6, 6);
            }
        }
    }

    /// <summary>
    /// Ölçü Stilleri penceresi: çizimin ölçü stilleri listelenir; seçili stilin ayarları sağda değiştirilir (değişiklik
    /// hemen çizime yansır). Yeni stil seçili stilin kopyası olarak açılır; aktif stil yeni ölçülerde kullanılır.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class OlcuStiliForm : Form
    {
        private readonly CizimPaneli _panel;
        private readonly ListBox _liste = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        private readonly PropertyGrid _ayarlar = new PropertyGrid { Dock = DockStyle.Fill, ToolbarVisible = false, PropertySort = PropertySort.Categorized };

        public OlcuStiliForm(CizimPaneli panel)
        {
            _panel = panel;
            Text = "Ölçü Stilleri";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 460);
            MinimumSize = new Size(560, 360);
            ShowInTaskbar = false;
            MinimizeBox = false;

            var solPanel = new Panel { Dock = DockStyle.Left, Width = 210, Padding = new Padding(8) };
            var dugmeler = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 70, FlowDirection = FlowDirection.LeftToRight };
            Button Dugme(string metin, Action eylem)
            {
                var d = new Button { Text = metin, Width = 92, Height = 26 };
                d.Click += (s, e) => eylem();
                dugmeler.Controls.Add(d);
                return d;
            }
            Dugme("Aktif Yap", AktifYap);
            Dugme("Yeni...", YeniStil);
            Dugme("Yeniden Adlandır...", YenidenAdlandir).Width = 120;
            Dugme("Sil", Sil).Width = 64;
            solPanel.Controls.Add(_liste);
            solPanel.Controls.Add(new Label { Text = "Stiller (✓ aktif):", Dock = DockStyle.Top, Height = 20 });
            solPanel.Controls.Add(dugmeler);

            var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            var kapat = new Button { Text = "Kapat", DialogResult = DialogResult.OK, Size = new Size(90, 26), Anchor = AnchorStyles.Right | AnchorStyles.Top };
            kapat.Location = new Point(ClientSize.Width - 100, 7);
            altPanel.Controls.Add(kapat);
            altPanel.Controls.Add(new Label
            {
                Text = "Boyutlar kağıt birimindedir (mm); çizimdeki boy = boyut × Genel Ölçek. Değişiklik bu stili kullanan bütün ölçülere hemen uygulanır.",
                Location = new Point(10, 4), Size = new Size(ClientSize.Width - 120, 32), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, ForeColor = SystemColors.GrayText
            });
            AcceptButton = kapat;
            CancelButton = kapat;

            Controls.Add(_ayarlar);
            Controls.Add(solPanel);
            Controls.Add(altPanel);

            _liste.Format += (s, e) => e.Value = (e.ListItem == _panel.AktifOlcuStili ? "✓ " : "    ") + ((OlcuStili)e.ListItem).Ad;
            _liste.SelectedIndexChanged += (s, e) => _ayarlar.SelectedObject = _liste.SelectedItem;
            _liste.DoubleClick += (s, e) => AktifYap();
            _ayarlar.PropertyValueChanged += (s, e) =>
            {
                // Ad boş ya da başka stilin adıyla aynı olamaz.
                var stil = (OlcuStili)_ayarlar.SelectedObject;
                if (e.ChangedItem.PropertyDescriptor?.Name == nameof(OlcuStili.Ad) &&
                    (string.IsNullOrWhiteSpace(stil.Ad) || _panel.OlcuStilleri.Any(x => x != stil && string.Equals(x.Ad, stil.Ad, StringComparison.OrdinalIgnoreCase))))
                {
                    stil.Ad = (string)e.OldValue;
                    MessageBox.Show(this, "Stil adı boş olamaz ve başka bir stilin adıyla aynı olamaz.", "Ölçü Stilleri", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _ayarlar.Refresh();
                }
                Yenile(stil);
                _panel.Invalidate();
            };
            Yenile(_panel.AktifOlcuStili);
        }

        private OlcuStili Secili => _liste.SelectedItem as OlcuStili;

        private void Yenile(OlcuStili secilecek)
        {
            _liste.BeginUpdate();
            _liste.Items.Clear();
            foreach (var stil in _panel.OlcuStilleri)
                _liste.Items.Add(stil);
            _liste.EndUpdate();
            if (secilecek != null)
                _liste.SelectedItem = secilecek;
        }

        private void AktifYap()
        {
            if (Secili == null)
                return;
            _panel.AktifOlcuStili = Secili;
            Yenile(Secili);
        }

        private string AdSor(string baslik, string oneri)
        {
            using (var dlg = new Form
            {
                Text = baslik, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(320, 90)
            })
            {
                dlg.Controls.Add(new Label { Text = "Stil adı:", Location = new Point(12, 17), AutoSize = true });
                var kutu = new TextBox { Text = oneri, Location = new Point(80, 14), Size = new Size(228, 20) };
                var tamam = new Button { Text = "Tamam", Location = new Point(152, 52), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(233, 52), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { kutu, tamam, iptal });
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return null;
                var ad = kutu.Text.Trim();
                if (ad.Length == 0 || _panel.OlcuStilleri.Any(s => string.Equals(s.Ad, ad, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(this, "Stil adı boş olamaz ve başka bir stilin adıyla aynı olamaz.", baslik, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                return ad;
            }
        }

        private void YeniStil()
        {
            var kaynak = Secili ?? _panel.AktifOlcuStili;
            var ad = AdSor("Yeni Ölçü Stili", kaynak.Ad + " Kopya");
            if (ad == null)
                return;
            var yeni = kaynak.Kopyala(ad);
            yeni.Liste = _panel.OlcuStilleri;
            _panel.OlcuStilleri.Add(yeni);
            Yenile(yeni);
        }

        private void YenidenAdlandir()
        {
            if (Secili == null)
                return;
            var ad = AdSor("Yeniden Adlandır", Secili.Ad);
            if (ad == null)
                return;
            Secili.Ad = ad;
            Yenile(Secili);
            _panel.Invalidate();
        }

        private void Sil()
        {
            var stil = Secili;
            if (stil == null)
                return;
            if (_panel.OlcuStilleri.Count <= 1)
            {
                MessageBox.Show(this, "Son ölçü stili silinemez.", "Ölçü Stilleri", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var kullanan = _panel.Nesneler.OfType<IOlcuStilli>().Where(o => o.Stil == stil).ToList();
            var yerine = _panel.OlcuStilleri.First(s => s != stil);
            if (_panel.AktifOlcuStili != stil)
                yerine = _panel.AktifOlcuStili;
            var soru = kullanan.Count > 0
                ? "\"" + stil.Ad + "\" stilini " + kullanan.Count + " ölçü / kılavuz çizgi kullanıyor. Bunlar \"" + yerine.Ad + "\" stiline geçirilip stil silinsin mi?"
                : "\"" + stil.Ad + "\" stili silinsin mi?";
            if (MessageBox.Show(this, soru, "Ölçü Stilleri", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            foreach (var olcu in kullanan)
                olcu.Stil = yerine;
            _panel.OlcuStilleri.Remove(stil);
            if (_panel.AktifOlcuStili == stil)
                _panel.AktifOlcuStili = yerine;
            Yenile(yerine);
            _panel.Invalidate();
        }
    }
}
