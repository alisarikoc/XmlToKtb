using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Bir çizim paneline (kesit editörü, tip kesit ekranı...) ana çizim ekranındaki komut düzenini ekler:
    /// <list type="bullet">
    /// <item>Boşluk (ya da Enter) son komutu tekrarlar; sağ tık komut tekrarlamaz.</item>
    /// <item>Ekranda çalışan komut yokken sağ tık bağlam menüsünü açar: Son Komutlar (son 10), Pano (kes, kopyala,
    /// baz noktasıyla kopyala, yapıştır, orijinal yerine yapıştır), Hızlı Seçim, Bul / Değiştir, geri al/ileri al,
    /// seçim komutları ve ekrana özel ek komutlar.</item>
    /// <item>Pano kısayolları: Ctrl+X, Ctrl+C, Ctrl+Shift+C, Ctrl+V, Ctrl+Shift+V; ayrıca Ctrl+A ve Ctrl+F.</item>
    /// </list>
    /// Komut seçim ya da nokta beklerken sağ tık eskisi gibi seçimi onaylar ya da nokta almayı bitirir.
    /// </summary>
    public sealed class PanelKomutMenusu
    {
        private enum NoktaIstegi
        {
            Yok,
            BazNoktasi,
            Yapistir
        }

        private readonly CizimPaneli _panel;
        private readonly Control _sahip;
        private readonly ContextMenuStrip _menu = new ContextMenuStrip { ShowImageMargin = true };
        private readonly List<KeyValuePair<string, Action>> _sonKomutlar = new List<KeyValuePair<string, Action>>();
        private readonly List<CizimNesnesi> _pano = new List<CizimNesnesi>();
        private double _panoX, _panoY;
        private NoktaIstegi _istek;
        private List<CizimNesnesi> _bazNesneleri;
        private bool _sagTikUygun, _tekrarUygun;

        /// <summary>Ekrana özel bir komut çalışıyorsa true döner (ör. kesit editöründe Nokta Sil); o sırada menü açılmaz, Boşluk tekrarlamaz.</summary>
        public Func<bool> MesgulMu { get; set; }

        /// <summary>Menünün sonuna ekrana özel komutlar eklemek için.</summary>
        public Action<ToolStripItemCollection> EkOgeler { get; set; }

        /// <summary>Çizim pano, silme ya da seçim komutuyla değişince çağrılır (ör. özellikler panelini yenilemek için).</summary>
        public Action Degisti { get; set; }

        /// <summary>Kullanıcıya gösterilecek kısa bilgi (ör. durum çubuğu).</summary>
        public Action<string> Ipucu { get; set; }

        public PanelKomutMenusu(CizimPaneli panel, Control sahip)
        {
            _panel = panel;
            _sahip = sahip;

            panel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    _sagTikUygun = EkranBos();
            };
            panel.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || !_sagTikUygun)
                    return;
                _sagTikUygun = false;
                MenuyuDoldur();
                _menu.Show(panel, e.Location);
            };
            // Tuşa basıldığı andaki durum, panel kendi işini yapmadan önce alınır (seçimi onaylayan Enter tekrarlamasın).
            panel.PreviewKeyDown += (s, e) => _tekrarUygun = EkranBos();
            panel.KeyDown += KlavyeKomutlari;
            panel.AracDegisti += (s, arac) =>
            {
                if (arac != CizimAraci.Sec && arac != CizimAraci.NoktaAl)
                    KomutKaydet(AracAdi(arac), () => _panel.AktifArac = arac);
            };
            panel.NoktaTiklandi += NoktaTiklandi;
            panel.NoktaAlmaBitti += (s, e) =>
            {
                if (_istek != NoktaIstegi.Yok)
                    Bilgi("");
                _istek = NoktaIstegi.Yok;
            };
        }

        private bool EkranBos() => _panel.AktifArac == CizimAraci.Sec && _istek == NoktaIstegi.Yok && !(MesgulMu?.Invoke() ?? false);

        private void Bilgi(string metin) => Ipucu?.Invoke(metin);

        /// <summary>Komutu "Son Komutlar" listesine ekler; Boşluk en son kaydedilen komutu tekrarlar.</summary>
        public void KomutKaydet(string ad, Action eylem)
        {
            _sonKomutlar.RemoveAll(k => k.Key == ad);
            _sonKomutlar.Insert(0, new KeyValuePair<string, Action>(ad, eylem));
            if (_sonKomutlar.Count > 10)
                _sonKomutlar.RemoveRange(10, _sonKomutlar.Count - 10);
        }

        // Komut adı verilerek çalıştırılır ve son komutlara kaydedilir.
        private void Calistir(string ad, Action eylem)
        {
            KomutKaydet(ad, () => Calistir(ad, eylem));
            eylem();
        }

        private void KlavyeKomutlari(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && !e.Control && _tekrarUygun)
            {
                _tekrarUygun = false;
                if (_sonKomutlar.Count > 0)
                {
                    Bilgi("Son komut tekrarlanıyor: " + _sonKomutlar[0].Key);
                    _sonKomutlar[0].Value();
                }
                e.Handled = e.SuppressKeyPress = true;
                return;
            }
            if (!e.Control || !EkranBos())
                return;
            switch (e.KeyCode)
            {
                case Keys.C: if (e.Shift) BazNoktasiylaKopyala(); else Kopyala(false); break;
                case Keys.X: Kopyala(true); break;
                case Keys.V: if (e.Shift) OrijinalYerineYapistir(); else Yapistir(); break;
                case Keys.A: TumunuSec(); break;
                case Keys.F: BulDegistir(); break;
                default: return;
            }
            e.Handled = e.SuppressKeyPress = true;
        }

        private List<CizimNesnesi> Seciliker() => _panel.Nesneler.Where(n => n.Secili).ToList();

        private void MenuyuDoldur()
        {
            _menu.Items.Clear();
            var seciliSayisi = _panel.Nesneler.Count(n => n.Secili);

            ToolStripMenuItem Oge(string metin, char simge, string kisayol, Action eylem, bool etkin = true)
            {
                var oge = new ToolStripMenuItem(metin, Simge(simge), (s, e) => eylem()) { Enabled = etkin };
                if (kisayol != null)
                    oge.ShortcutKeyDisplayString = kisayol;
                return oge;
            }

            var son = new ToolStripMenuItem("Son Komutlar", Simge('')) { Enabled = _sonKomutlar.Count > 0 };
            for (var i = 0; i < _sonKomutlar.Count; i++)
            {
                var komut = _sonKomutlar[i];
                var oge = new ToolStripMenuItem(komut.Key, i == 0 ? Simge('') : null, (s, e) => komut.Value());
                if (i == 0)
                {
                    oge.ShortcutKeyDisplayString = "Boşluk";
                    oge.Font = new Font(oge.Font, FontStyle.Bold);
                }
                son.DropDownItems.Add(oge);
            }
            _menu.Items.Add(son);

            var pano = new ToolStripMenuItem("Pano", Simge(''));
            pano.DropDownItems.Add(Oge("Kes", '', "Ctrl+X", () => Kopyala(true), seciliSayisi > 0));
            pano.DropDownItems.Add(Oge("Kopyala", '', "Ctrl+C", () => Kopyala(false), seciliSayisi > 0));
            pano.DropDownItems.Add(Oge("Baz Noktasıyla Kopyala", '', "Ctrl+Shift+C", BazNoktasiylaKopyala, seciliSayisi > 0));
            pano.DropDownItems.Add(new ToolStripSeparator());
            pano.DropDownItems.Add(Oge("Yapıştır", '', "Ctrl+V", Yapistir, _pano.Count > 0));
            pano.DropDownItems.Add(Oge("Orijinal Yerine Yapıştır", '', "Ctrl+Shift+V", OrijinalYerineYapistir, _pano.Count > 0));
            _menu.Items.Add(pano);
            _menu.Items.Add(new ToolStripSeparator());

            _menu.Items.Add(Oge("Hızlı Seçim...", '', null, HizliSecim));
            _menu.Items.Add(Oge("Bul / Değiştir...", '', "Ctrl+F", BulDegistir));
            _menu.Items.Add(new ToolStripSeparator());

            _menu.Items.Add(Oge("Geri Al", '', "Ctrl+Z", () => { _panel.GeriAl(); Degisti?.Invoke(); }, _panel.GeriAlinabilir));
            _menu.Items.Add(Oge("İleri Al", '', "Ctrl+Y", () => { _panel.Ilerle(); Degisti?.Invoke(); }, _panel.Ilerlenebilir));
            _menu.Items.Add(new ToolStripSeparator());

            _menu.Items.Add(Oge("Tümünü Seç", '', "Ctrl+A", TumunuSec));
            if (seciliSayisi > 0)
            {
                _menu.Items.Add(Oge("Seçimi Kaldır (" + seciliSayisi + ")", '', "Esc", () => SecimiUygula(n => false, 0)));
                _menu.Items.Add(Oge("Seçilenleri Sil", '', "Del", () =>
                {
                    _panel.SeciliNesneleriSil();
                    _panel.DegisiklikKaydet();
                    Degisti?.Invoke();
                }));
            }
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(Oge("Tümünü Göster", '', null, () =>
            {
                _panel.TumObjeleriGoster();
                _panel.Invalidate();
            }));

            if (EkOgeler != null)
            {
                _menu.Items.Add(new ToolStripSeparator());
                EkOgeler(_menu.Items);
            }
        }

        // ------------------------------------------------------------------ pano

        private bool SecimVarMi(string komut)
        {
            if (_panel.Nesneler.Any(n => n.Secili))
                return true;
            MessageBox.Show(_sahip, "Önce objeleri seçin.", komut, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private void PanoyaAl(List<CizimNesnesi> nesneler, double x, double y)
        {
            _pano.Clear();
            _pano.AddRange(nesneler.Select(NesneDonusumleri.Klonla));
            _panoX = x;
            _panoY = y;
        }

        /// <summary>Seçili objeleri panoya alır (kes ise çizimden siler); referans noktası seçimin sol alt köşesidir.</summary>
        private void Kopyala(bool kes)
        {
            Calistir(kes ? "Kes" : "Kopyala", () =>
            {
                if (!SecimVarMi(kes ? "Kes" : "Kopyala"))
                    return;
                var secili = Seciliker();
                var noktalar = secili.SelectMany(n => n.UcNoktalariAl()).ToList();
                PanoyaAl(secili, noktalar.Count > 0 ? noktalar.Min(p => p.X) : 0, noktalar.Count > 0 ? noktalar.Min(p => p.Y) : 0);
                if (kes)
                {
                    _panel.SeciliNesneleriSil();
                    _panel.DegisiklikKaydet();
                    Degisti?.Invoke();
                }
                Bilgi(_pano.Count + " obje panoya " + (kes ? "kesildi" : "kopyalandı") + ".");
            });
        }

        private void BazNoktasiylaKopyala()
        {
            Calistir("Baz Noktasıyla Kopyala", () =>
            {
                if (!SecimVarMi("Baz Noktasıyla Kopyala"))
                    return;
                _bazNesneleri = Seciliker();
                NoktaIste(NoktaIstegi.BazNoktasi, "Baz Noktasıyla Kopyala: baz (referans) noktasını tıklayın.");
            });
        }

        private void Yapistir()
        {
            Calistir("Yapıştır", () =>
            {
                if (_pano.Count == 0)
                {
                    MessageBox.Show(_sahip, "Pano boş. Önce objeleri kopyalayın.", "Yapıştır", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                NoktaIste(NoktaIstegi.Yapistir, "Yapıştır: " + _pano.Count + " obje; yapıştırma noktasını tıklayın (referans noktası buraya gelir).");
            });
        }

        private void OrijinalYerineYapistir()
        {
            Calistir("Orijinal Yerine Yapıştır", () =>
            {
                if (_pano.Count == 0)
                {
                    MessageBox.Show(_sahip, "Pano boş. Önce objeleri kopyalayın.", "Orijinal Yerine Yapıştır", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Ekle(_pano.Select(NesneDonusumleri.Klonla).ToList());
                Bilgi(_pano.Count + " obje kopyalandıkları yere yapıştırıldı.");
            });
        }

        private void NoktaIste(NoktaIstegi istek, string ipucu)
        {
            _istek = istek;
            _panel.NoktaAlmayaBasla(false);
            _panel.Focus();
            Bilgi(ipucu + " Vazgeçmek için sağ tık ya da Esc.");
        }

        private void NoktaTiklandi(double x, double y)
        {
            var istek = _istek;
            if (istek == NoktaIstegi.Yok)
                return;
            _istek = NoktaIstegi.Yok;
            _panel.AktifArac = CizimAraci.Sec;
            if (istek == NoktaIstegi.BazNoktasi)
            {
                PanoyaAl(_bazNesneleri, x, y);
                Bilgi(_pano.Count + " obje baz noktasıyla panoya kopyalandı. Yapıştır (Ctrl+V) ile tıklanan yere yapıştırılır.");
            }
            else
            {
                var kopyalar = _pano.Select(NesneDonusumleri.Klonla).ToList();
                foreach (var kopya in kopyalar)
                    NesneDonusumleri.Tasi(kopya, x - _panoX, y - _panoY);
                Ekle(kopyalar);
                Bilgi(kopyalar.Count + " obje yapıştırıldı.");
            }
        }

        // Kopyaları çizime ekler ve yalnız onları seçili yapar; geri al kaydı alınır.
        private void Ekle(List<CizimNesnesi> yeniler)
        {
            foreach (var nesne in _panel.Nesneler)
                nesne.Secili = false;
            foreach (var nesne in yeniler)
                nesne.Secili = true;
            _panel.Nesneler.AddRange(yeniler);
            _panel.SecimDegistiBildir();
            _panel.DegisiklikKaydet();
            _panel.Invalidate();
            Degisti?.Invoke();
        }

        // ------------------------------------------------------------------ seçim, bul/değiştir

        private static bool Secilebilir(CizimNesnesi n) => n.Tabaka == null || (n.Tabaka.Gorunur && !n.Tabaka.Kilitli);

        // birlesim: 0 yeni seçim, 1 seçime ekle, 2 seçimden çıkar, 3 seçim içinde (HizliSecimForm'daki sıra).
        private void SecimiUygula(Func<CizimNesnesi, bool> kosul, int birlesim)
        {
            foreach (var nesne in _panel.Nesneler)
            {
                var uygun = Secilebilir(nesne) && kosul(nesne);
                switch (birlesim)
                {
                    case 0: nesne.Secili = uygun; break;
                    case 1: if (uygun) nesne.Secili = true; break;
                    case 2: if (uygun) nesne.Secili = false; break;
                    case 3: nesne.Secili = nesne.Secili && uygun; break;
                }
            }
            _panel.SecimDegistiBildir();
            _panel.Invalidate();
            Degisti?.Invoke();
        }

        private void TumunuSec() => SecimiUygula(n => true, 0);

        private void HizliSecim()
        {
            Calistir("Hızlı Seçim", () =>
            {
                using (var dlg = new HizliSecimForm(_panel.Nesneler, _panel.Tabakalar))
                {
                    if (dlg.ShowDialog(_sahip) != DialogResult.OK)
                        return;
                    SecimiUygula(dlg.Uyar, dlg.BirlesimSirasi);
                }
            });
        }

        private void BulDegistir()
        {
            Calistir("Bul / Değiştir", () =>
            {
                using (var dlg = new BulDegistirForm(_panel))
                    dlg.ShowDialog(_sahip);
                _panel.Invalidate();
                Degisti?.Invoke();
            });
        }

        // ------------------------------------------------------------------ yardımcılar

        /// <summary>Çizim aracının kullanıcıya gösterilen adı (son komutlar listesi için).</summary>
        public static string AracAdi(CizimAraci arac)
        {
            switch (arac)
            {
                case CizimAraci.Cizgi: return "Çizgi";
                case CizimAraci.Kaydir: return "Kaydır";
                case CizimAraci.Kopyala: return "Kopyala (taşıyarak)";
                case CizimAraci.Dondur: return "Döndür";
                case CizimAraci.Olcekle: return "Ölçekle";
                case CizimAraci.Kes: return "Kırp";
                case CizimAraci.DaireMerkezYaricap: return "Daire (merkez, yarıçap)";
                case CizimAraci.Daire3Nokta: return "Daire (3 nokta)";
                case CizimAraci.Daire2NoktaYaricap: return "Daire (2 nokta, yarıçap)";
                case CizimAraci.DaireTegetIkiObje: return "Daire (2 objeye teğet)";
                case CizimAraci.DaireTegetUcObje: return "Daire (3 objeye teğet)";
                default: return arac.ToString();
            }
        }

        // Menü simgeleri Windows'un simge yazı tipinden (Segoe MDL2 Assets) çizilir; yazı tipi yoksa simgesiz kalır.
        private static readonly Dictionary<char, Bitmap> _simgeler = new Dictionary<char, Bitmap>();
        private static bool? _simgeYaziTipiVar;

        /// <summary>Segoe MDL2 Assets yazı tipindeki simgenin 16x16 resmi; yazı tipi yoksa null.</summary>
        public static Bitmap Simge(char kod)
        {
            if (_simgeYaziTipiVar == null)
            {
                using (var yazilar = new InstalledFontCollection())
                    _simgeYaziTipiVar = yazilar.Families.Any(f => f.Name == "Segoe MDL2 Assets");
            }
            if (_simgeYaziTipiVar != true)
                return null;
            if (_simgeler.TryGetValue(kod, out var hazir))
                return hazir;

            var simge = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(simge))
            using (var yazi = new Font("Segoe MDL2 Assets", 11f, GraphicsUnit.Pixel))
            using (var bicim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var firca = new SolidBrush(Color.FromArgb(40, 70, 120)))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.DrawString(kod.ToString(), yazi, firca, new RectangleF(0, 0, 16, 16), bicim);
            }
            _simgeler[kod] = simge;
            return simge;
        }
    }
}
