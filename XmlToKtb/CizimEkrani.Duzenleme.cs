using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Ana çizim ekranının düzenleme ve seçim komutları: pano (kopyala, kes, yapıştır), uzat, paralel, aynala, iki
    /// noktadan dönüşüm, patlat, çoklu doğruya dönüştür, toplu obje değiştir, bul/değiştir ve seçim seçenekleri.
    /// </summary>
    public partial class CizimEkrani
    {
        /// <summary>Ekrandan nokta ya da obje bekleyen (ya da önce obje seçimi bekleyen) düzenleme komutu.</summary>
        private enum DuzenKomutu
        {
            Yok,
            PanoyaKopyala,
            PanoyaKes,
            ReferansKopyala,
            Yapistir,
            Uzat,
            Paralel,
            Aynala,
            Hizala,
            Patlat,
            CokluDogru,
            TopluDegistir,
            Cit
        }

        private static readonly Dictionary<DuzenKomutu, string> KomutAdlari = new Dictionary<DuzenKomutu, string>
        {
            { DuzenKomutu.PanoyaKopyala, "Panoya Kopyala" }, { DuzenKomutu.PanoyaKes, "Panoya Kes" },
            { DuzenKomutu.ReferansKopyala, "Referans Noktası ile Kopyala" }, { DuzenKomutu.Uzat, "Uzat" },
            { DuzenKomutu.Paralel, "Paralel" }, { DuzenKomutu.Aynala, "Aynala" }, { DuzenKomutu.Hizala, "2 Noktadan Dönüşüm" },
            { DuzenKomutu.Patlat, "Patlat" }, { DuzenKomutu.CokluDogru, "Çoklu Doğruya Dönüştür" }, { DuzenKomutu.TopluDegistir, "Toplu Obje Değiştir" }
        };

        private DuzenKomutu _duzenKomutu = DuzenKomutu.Yok;
        private DuzenKomutu _secimBekleyenKomut = DuzenKomutu.Yok;
        private readonly List<double[]> _komutNoktalari = new List<double[]>();
        private List<CizimNesnesi> _komutNesneleri = new List<CizimNesnesi>();
        private double _paralelMesafe = 1.0;
        // Uzat: önce seçilen sınır objeleri; null iken sınır seçimi bekleniyor.
        private List<CizimNesnesi> _uzatSinirlari;

        // Pano: kopyalanan objeler ve referans (taban) noktası.
        private readonly List<CizimNesnesi> _pano = new List<CizimNesnesi>();
        private double _panoX, _panoY;
        // Yapıştırma önizlemesi: farenin yerine taşınan pano kopyaları.
        private List<CizimNesnesi> _onizleme;
        private double _onizlemeX, _onizlemeY;

        private ToolStripStatusLabel _komutDurumLabel;
        private ToolStripStatusLabel _seciliSayisiLabel;
        private List<CizimNesnesi> _sonSecim = new List<CizimNesnesi>();
        private List<CizimNesnesi> _oncekiSecim = new List<CizimNesnesi>();

        private void DuzenlemeMenuleriniOlustur()
        {
            _komutDurumLabel = new ToolStripStatusLabel { ForeColor = Color.Firebrick };
            _seciliSayisiLabel = new ToolStripStatusLabel { ForeColor = Color.DarkBlue };
            durumStrip.Items.Insert(1, _komutDurumLabel);
            durumStrip.Items.Insert(durumStrip.Items.IndexOf(_olcekDurumLabel), _seciliSayisiLabel);

            ToolStripMenuItem Oge(string metin, string kisayol, Action eylem)
            {
                var oge = new ToolStripMenuItem(metin, null, (s, e) => eylem());
                if (kisayol != null)
                    oge.ShortcutKeyDisplayString = kisayol;
                return oge;
            }

            // --- Düzenle: pano komutları başa, yeni düzenleme komutları sona ---
            kesMenuItem.Text = "Kırp";
            kesMenuItem.ToolTipText = "Seçili objeler (seçim yoksa bütün objeler) kesici kabul edilir; tıklanan parçayı keser.";
            var panoOgeleri = new ToolStripItem[]
            {
                Oge("Panoya Kopyala", "Ctrl+C", () => KomutBaslat(DuzenKomutu.PanoyaKopyala)),
                Oge("Referans Noktası ile Kopyala", "Ctrl+Shift+C", () => KomutBaslat(DuzenKomutu.ReferansKopyala)),
                Oge("Panoya Kes", "Ctrl+X", () => KomutBaslat(DuzenKomutu.PanoyaKes)),
                Oge("Yapıştır", "Ctrl+V", () => KomutBaslat(DuzenKomutu.Yapistir)),
                Oge("Koordinatlı Yerine Yapıştır", "Ctrl+Shift+V", KoordinatliYapistir),
                new ToolStripSeparator()
            };
            for (var i = panoOgeleri.Length - 1; i >= 0; i--)
                duzenleMenuItem.DropDownItems.Insert(0, panoOgeleri[i]);

            var konum = duzenleMenuItem.DropDownItems.IndexOf(noktaEditoruMenuItem);
            var yeniOgeler = new ToolStripItem[]
            {
                new ToolStripSeparator(),
                Oge("Uzat", null, () => KomutBaslat(DuzenKomutu.Uzat)),
                Oge("Paralel...", null, () => KomutBaslat(DuzenKomutu.Paralel)),
                Oge("Aynala", null, () => KomutBaslat(DuzenKomutu.Aynala)),
                Oge("2 Noktadan Dönüşüm (Hizala)", null, () => KomutBaslat(DuzenKomutu.Hizala)),
                Oge("Patlat", null, () => KomutBaslat(DuzenKomutu.Patlat)),
                Oge("Çoklu Doğruya Dönüştür", null, () => KomutBaslat(DuzenKomutu.CokluDogru)),
                new ToolStripSeparator(),
                Oge("Toplu Obje Değiştir", null, () => KomutBaslat(DuzenKomutu.TopluDegistir)),
                Oge("Bul / Değiştir...", "Ctrl+F", BulDegistir),
                new ToolStripSeparator()
            };
            for (var i = 0; i < yeniOgeler.Length; i++)
                duzenleMenuItem.DropDownItems.Insert(Math.Max(0, konum) + i, yeniOgeler[i]);

            // --- Seçim menüsü ---
            var secimMenu = new ToolStripMenuItem("Seçim");
            secimMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                Oge("Tümünü Seç", "Ctrl+A", () => SecimiUygula(n => true, SecimBirlesimi.Yeni)),
                Oge("Seçimi Kaldır", "Esc", () => SecimiUygula(n => false, SecimBirlesimi.Yeni)),
                Oge("Seçimi Tersine Çevir", null, SecimiTersCevir),
                Oge("Benzerlerini Seç (aynı tip ve tabaka)", null, BenzerleriniSec),
                Oge("Önceki Seçim", null, OncekiSecim),
                new ToolStripSeparator(),
                Oge("Çit ile Seç (kestiği objeler)", null, () => KomutBaslat(DuzenKomutu.Cit)),
                Oge("Hızlı Seçim (tip, tabaka, metin, kot)...", null, HizliSecim),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Pencere: soldan sağa çekilen pencere içindekileri, sağdan sola çekilen değenleri seçer; Shift ile eklenir/çıkarılır.") { Enabled = false }
            });
            anaMenuStrip.Items.Insert(anaMenuStrip.Items.IndexOf(duzenleMenuItem) + 1, secimMenu);

            cizimPaneli1.NoktaTiklandi += DuzenNoktaTiklandi;
            cizimPaneli1.NoktaAlmaBitti += DuzenNoktaAlmaBitti;
            cizimPaneli1.SecimOnaylandi += (s, e) => SecimBekleyenKomutuBaslat();
            cizimPaneli1.SecimIptalEdildi += (s, e) =>
            {
                if (_secimBekleyenKomut != DuzenKomutu.Yok)
                    KomutuBitir();
            };
            cizimPaneli1.UstCizim += DuzenUstCizim;
            cizimPaneli1.MouseMove += DuzenFareHareketi;
            cizimPaneli1.KeyDown += DuzenKisayollari;
        }

        // Kısayollar çizim ekranı odaktayken çalışır (özellikler panelinde metin kopyalarken karışmasın diye).
        private void DuzenKisayollari(object sender, KeyEventArgs e)
        {
            if (!e.Control)
                return;
            DuzenKomutu komut = DuzenKomutu.Yok;
            switch (e.KeyCode)
            {
                case Keys.C: komut = e.Shift ? DuzenKomutu.ReferansKopyala : DuzenKomutu.PanoyaKopyala; break;
                case Keys.X: komut = DuzenKomutu.PanoyaKes; break;
                case Keys.V:
                    if (e.Shift) KoordinatliYapistir();
                    else komut = DuzenKomutu.Yapistir;
                    break;
                case Keys.A: SecimiUygula(n => true, SecimBirlesimi.Yeni); break;
                case Keys.F: BulDegistir(); break;
                default: return;
            }
            if (komut != DuzenKomutu.Yok)
                KomutBaslat(komut);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private List<CizimNesnesi> SeciliNesneler() => cizimPaneli1.Nesneler.Where(n => n.Secili).ToList();

        private void KomutIpucu(string metin)
        {
            _komutDurumLabel.Text = string.IsNullOrEmpty(metin) ? "" : "   " + metin;
        }

        // ------------------------------------------------------------------ komut akışı

        /// <summary>
        /// Komutu başlatır. Obje gerektiren komutta seçim yoksa önce seçim istenir; seçim Enter ya da sağ tıkla
        /// bitince komut kaldığı yerden devam eder.
        /// </summary>
        private void KomutBaslat(DuzenKomutu komut)
        {
            KomutuBitir();
            _tabakaKapatiliyor = false;

            var secili = SeciliNesneler();
            var secimGerekli = komut != DuzenKomutu.Yapistir && komut != DuzenKomutu.Cit && komut != DuzenKomutu.Uzat;
            if (secimGerekli && secili.Count == 0)
            {
                _secimBekleyenKomut = komut;
                cizimPaneli1.AktifArac = CizimAraci.Sec;
                cizimPaneli1.SecimBekleniyor = true;
                cizimPaneli1.Focus();
                KomutIpucu(KomutAdlari[komut] + ": objeleri seçin (tıklayın ya da pencere çekin), bitirince Enter ya da sağ tık. Vazgeçmek için Esc.");
                return;
            }

            _komutNesneleri = secili;
            switch (komut)
            {
                case DuzenKomutu.PanoyaKopyala:
                case DuzenKomutu.PanoyaKes:
                    PanoyaAl(secili, komut == DuzenKomutu.PanoyaKes);
                    return;
                case DuzenKomutu.Patlat:
                    Patlat(secili);
                    return;
                case DuzenKomutu.CokluDogru:
                    CokluDogruyaDonustur(secili);
                    return;
                case DuzenKomutu.TopluDegistir:
                    ozellikGrid.SelectedObjects = secili.Cast<object>().ToArray();
                    ozellikGrid.Focus();
                    KomutIpucu(secili.Count + " obje: özellikler panelinde ortak özellikler gösteriliyor; değiştirilen değer hepsine uygulanır.");
                    return;
                case DuzenKomutu.Yapistir:
                    if (_pano.Count == 0)
                    {
                        MessageBox.Show("Pano boş. Önce objeleri Panoya Kopyala ya da Referans Noktası ile Kopyala ile kopyalayın.", "Yapıştır", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    _onizleme = _pano.Select(NesneDonusumleri.Klonla).ToList();
                    foreach (var oge in _onizleme)
                        oge.Secili = true;
                    _onizlemeX = _panoX;
                    _onizlemeY = _panoY;
                    break;
                case DuzenKomutu.Uzat:
                    // Önceden seçili objeler varsa sınır onlardır; yoksa ilk tıklanan obje sınır olur.
                    _uzatSinirlari = secili.Count > 0 ? secili : null;
                    break;
                case DuzenKomutu.Paralel:
                    if (!SayiSor("Paralel", "Paralel mesafesi (m):", _paralelMesafe, out var mesafe) || mesafe <= 0)
                        return;
                    _paralelMesafe = mesafe;
                    break;
            }

            _duzenKomutu = komut;
            _komutNoktalari.Clear();
            // Obje seçtiren adımlarda imleç kare, nokta verilenlerde artıdır.
            cizimPaneli1.NoktaAlmayaBasla(komut == DuzenKomutu.Uzat);
            cizimPaneli1.Focus();
            KomutIpucunuGuncelle();
        }

        private void KomutIpucunuGuncelle()
        {
            string ipucu;
            switch (_duzenKomutu)
            {
                case DuzenKomutu.ReferansKopyala: ipucu = "Referans (taban) noktasını tıklayın."; break;
                case DuzenKomutu.Yapistir: ipucu = _pano.Count + " obje: yapıştırma noktasını tıklayın (referans noktası buraya gelir)."; break;
                case DuzenKomutu.Uzat:
                    ipucu = _uzatSinirlari == null
                        ? "önce SINIR objesini tıklayın (objeler buna kadar uzatılacak)."
                        : "şimdi UZATILACAK objeleri tıklayın (tıkladığınız uca yakın taraf uzar). Bitirmek için sağ tık ya da Esc.";
                    break;
                case DuzenKomutu.Paralel: ipucu = "Paralelin alınacağı tarafı tıklayın (mesafe " + _paralelMesafe.ToString("0.###", CultureInfo.InvariantCulture) + " m)."; break;
                case DuzenKomutu.Aynala: ipucu = _komutNoktalari.Count == 0 ? "Ayna ekseninin 1. noktasını tıklayın." : "Ayna ekseninin 2. noktasını tıklayın."; break;
                case DuzenKomutu.Hizala:
                    ipucu = new[] { "1. kaynak noktasını tıklayın.", "1. hedef noktasını tıklayın.", "2. kaynak noktasını tıklayın.", "2. hedef noktasını tıklayın." }[Math.Min(3, _komutNoktalari.Count)];
                    break;
                case DuzenKomutu.Cit: ipucu = "Çit noktalarını tıklayın; bitirince sağ tık ya da Esc (çitin kestiği objeler seçilir)."; break;
                default: ipucu = ""; break;
            }
            KomutIpucu(_duzenKomutu == DuzenKomutu.Yok ? "" : (KomutAdlari.TryGetValue(_duzenKomutu, out var ad) ? ad : _duzenKomutu == DuzenKomutu.Yapistir ? "Yapıştır" : "Çit ile Seç") + ": " + ipucu);
        }

        private void KomutuBitir()
        {
            var noktaBekliyordu = _duzenKomutu != DuzenKomutu.Yok;
            _duzenKomutu = DuzenKomutu.Yok;
            _secimBekleyenKomut = DuzenKomutu.Yok;
            _komutNoktalari.Clear();
            _onizleme = null;
            if (_uzatSinirlari != null)
                foreach (var sinir in _uzatSinirlari)
                    sinir.Secili = false;
            _uzatSinirlari = null;
            cizimPaneli1.SecimBekleniyor = false;
            KomutIpucu("");
            if (noktaBekliyordu && cizimPaneli1.AktifArac == CizimAraci.NoktaAl)
                cizimPaneli1.AktifArac = CizimAraci.Sec;
            cizimPaneli1.Invalidate();
        }

        private void SecimBekleyenKomutuBaslat()
        {
            if (_secimBekleyenKomut == DuzenKomutu.Yok)
                return;
            var komut = _secimBekleyenKomut;
            _secimBekleyenKomut = DuzenKomutu.Yok;
            cizimPaneli1.SecimBekleniyor = false;
            if (SeciliNesneler().Count == 0)
            {
                KomutIpucu("");
                return;
            }
            KomutBaslat(komut);
        }

        private void DuzenNoktaAlmaBitti(object sender, EventArgs e)
        {
            if (_duzenKomutu == DuzenKomutu.Yok)
                return;
            if (_duzenKomutu == DuzenKomutu.Cit && _komutNoktalari.Count >= 2)
            {
                var cit = new List<double[]>(_komutNoktalari);
                var tolerans = 6 / cizimPaneli1.Gorunum.Olcek;
                _duzenKomutu = DuzenKomutu.Yok;
                SecimiUygula(n => GeometriIslemleri.CitiKesiyorMu(n, cit, tolerans), Control.ModifierKeys.HasFlag(Keys.Shift) ? SecimBirlesimi.Ekle : SecimBirlesimi.Yeni);
            }
            KomutuBitir();
        }

        private void DuzenNoktaTiklandi(double x, double y)
        {
            if (_duzenKomutu == DuzenKomutu.Yok)
                return;

            switch (_duzenKomutu)
            {
                case DuzenKomutu.ReferansKopyala:
                    _pano.Clear();
                    _pano.AddRange(_komutNesneleri.Select(NesneDonusumleri.Klonla));
                    _panoX = x;
                    _panoY = y;
                    KomutuBitir();
                    KomutIpucu(_pano.Count + " obje referans noktasıyla panoya kopyalandı. Yapıştır (Ctrl+V) ile tıklanan yere yapıştırılır.");
                    break;

                case DuzenKomutu.Yapistir:
                {
                    var kopyalar = _pano.Select(NesneDonusumleri.Klonla).ToList();
                    foreach (var kopya in kopyalar)
                        NesneDonusumleri.Tasi(kopya, x - _panoX, y - _panoY);
                    Ekle(kopyalar, true);
                    KomutuBitir();
                    KomutIpucu(kopyalar.Count + " obje yapıştırıldı.");
                    break;
                }

                case DuzenKomutu.Uzat:
                {
                    var tiklanan = TiklananNesne(x, y);
                    if (tiklanan == null)
                        return;
                    if (_uzatSinirlari == null)
                    {
                        // 1. adım: sınır objesi seçilir (seçili gösterilir).
                        foreach (var nesne in cizimPaneli1.Nesneler)
                            nesne.Secili = false;
                        tiklanan.Secili = true;
                        _uzatSinirlari = new List<CizimNesnesi> { tiklanan };
                        KomutIpucunuGuncelle();
                        cizimPaneli1.Invalidate();
                        break;
                    }

                    // 2. adım: tıklanan obje, sınır(lar)a kadar uzatılır.
                    if (_uzatSinirlari.Contains(tiklanan))
                        return;
                    var primitifler = _uzatSinirlari.SelectMany(GeometriIslemleri.Primitifler).ToList();
                    if (GeometriIslemleri.Uzat(tiklanan, primitifler, x, y))
                    {
                        cizimPaneli1.DegisiklikKaydet();
                        cizimPaneli1.Invalidate();
                        KomutIpucu("Uzat: obje sınıra uzatıldı. Başka obje tıklayabilir ya da sağ tıkla bitirebilirsiniz.");
                    }
                    else
                    {
                        KomutIpucu("Uzat: bu obje sınıra uzatılamıyor (uzantısı sınırı kesmiyor ya da çizgi/çoklu doğru değil). Başka obje tıklayın.");
                    }
                    break;
                }

                case DuzenKomutu.Paralel:
                {
                    var yeniler = _komutNesneleri.Select(n => GeometriIslemleri.Paralel(n, _paralelMesafe, x, y)).Where(n => n != null).ToList();
                    Ekle(yeniler, false);
                    var atlanan = _komutNesneleri.Count - yeniler.Count;
                    KomutuBitir();
                    KomutIpucu("Paralel: " + yeniler.Count + " obje oluşturuldu" + (atlanan > 0 ? " (" + atlanan + " obje için paralel alınamadı: tipi desteklenmiyor ya da yarıçap sıfırın altına düşüyor)" : "") + ".");
                    break;
                }

                case DuzenKomutu.Aynala:
                    _komutNoktalari.Add(new[] { x, y });
                    if (_komutNoktalari.Count < 2)
                    {
                        KomutIpucunuGuncelle();
                        break;
                    }
                    if (Math.Abs(_komutNoktalari[0][0] - x) < 1e-9 && Math.Abs(_komutNoktalari[0][1] - y) < 1e-9)
                    {
                        _komutNoktalari.RemoveAt(1);
                        break;
                    }
                    {
                        var cevap = MessageBox.Show("Kaynak objeler silinsin mi?\n\nEvet: objeler aynalanır (yerlerinden kalkar).\nHayır: aynalanmış kopyaları eklenir, kaynaklar yerinde kalır.",
                            "Aynala", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                        if (cevap != DialogResult.Cancel)
                            DonusumUygula(Donusum.Aynalama(_komutNoktalari[0][0], _komutNoktalari[0][1], x, y), cevap == DialogResult.No);
                        KomutuBitir();
                    }
                    break;

                case DuzenKomutu.Hizala:
                    _komutNoktalari.Add(new[] { x, y });
                    if (_komutNoktalari.Count < 4)
                    {
                        KomutIpucunuGuncelle();
                        break;
                    }
                    {
                        var p = _komutNoktalari;
                        if (Math.Abs(p[0][0] - p[2][0]) < 1e-9 && Math.Abs(p[0][1] - p[2][1]) < 1e-9)
                        {
                            MessageBox.Show("1. ve 2. kaynak noktaları aynı olamaz.", "2 Noktadan Dönüşüm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            KomutuBitir();
                            break;
                        }
                        var cevap = MessageBox.Show("Objeler hizalama noktalarına göre ölçeklensin mi?\n\n(Evet: kaynak noktaları arasındaki uzunluk hedef noktaları arasındaki uzunluğa eşitlenir.)",
                            "2 Noktadan Dönüşüm", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                        if (cevap != DialogResult.Cancel)
                            DonusumUygula(Donusum.Hizalama(p[0][0], p[0][1], p[1][0], p[1][1], p[2][0], p[2][1], p[3][0], p[3][1], cevap == DialogResult.Yes), false);
                        KomutuBitir();
                    }
                    break;

                case DuzenKomutu.Cit:
                    _komutNoktalari.Add(new[] { x, y });
                    cizimPaneli1.Invalidate();
                    break;
            }
        }

        // Tıklanan yerdeki (görünür) en üstteki obje.
        private CizimNesnesi TiklananNesne(double x, double y)
        {
            var tolerans = 6 / cizimPaneli1.Gorunum.Olcek;
            var nokta = new PointF((float)x, (float)y);
            return cizimPaneli1.Nesneler.LastOrDefault(n => (n.Tabaka == null || n.Tabaka.Gorunur) && n.IsabetEttiMi(nokta, tolerans));
        }

        // Objeleri çizime ekler; istenirse yalnız onları seçili yapar. Geri al kaydı alınır.
        private void Ekle(List<CizimNesnesi> yeniler, bool sec)
        {
            if (sec)
                foreach (var nesne in cizimPaneli1.Nesneler)
                    nesne.Secili = false;
            foreach (var nesne in yeniler)
                nesne.Secili = sec;
            cizimPaneli1.Nesneler.AddRange(yeniler);
            cizimPaneli1.DegisiklikKaydet();
            cizimPaneli1.SecimDegistiBildir();
            cizimPaneli1.Invalidate();
        }

        // Seçili objelere dönüşümü uygular; kopya isteniyorsa dönüşmüş kopyalar eklenir.
        private void DonusumUygula(Donusum donusum, bool kopyala)
        {
            if (kopyala)
            {
                var kopyalar = _komutNesneleri.Select(NesneDonusumleri.Klonla).ToList();
                foreach (var kopya in kopyalar)
                    donusum.Uygula(kopya);
                Ekle(kopyalar, true);
                return;
            }
            foreach (var nesne in _komutNesneleri)
                donusum.Uygula(nesne);
            cizimPaneli1.DegisiklikKaydet();
            cizimPaneli1.Invalidate();
            ozellikGrid.Refresh();
        }

        // ------------------------------------------------------------------ önizlemeler

        private void DuzenFareHareketi(object sender, MouseEventArgs e)
        {
            if (_duzenKomutu == DuzenKomutu.Yok)
                return;
            if (_duzenKomutu == DuzenKomutu.Yapistir && _onizleme != null)
            {
                cizimPaneli1.Gorunum.EkrandanDunyayaHassas(e.Location, cizimPaneli1.Width, cizimPaneli1.Height, out var x, out var y);
                foreach (var oge in _onizleme)
                    NesneDonusumleri.Tasi(oge, x - _onizlemeX, y - _onizlemeY);
                _onizlemeX = x;
                _onizlemeY = y;
            }
            if (_duzenKomutu == DuzenKomutu.Yapistir || _komutNoktalari.Count > 0)
                cizimPaneli1.Invalidate();
        }

        private void DuzenUstCizim(object sender, PaintEventArgs e)
        {
            if (_duzenKomutu == DuzenKomutu.Yok)
                return;
            int w = cizimPaneli1.Width, h = cizimPaneli1.Height;
            if (_duzenKomutu == DuzenKomutu.Yapistir && _onizleme != null)
            {
                foreach (var oge in _onizleme)
                    oge.Ciz(e.Graphics, cizimPaneli1.Gorunum, w, h);
                return;
            }
            if (_komutNoktalari.Count == 0)
                return;

            var fare = cizimPaneli1.PointToClient(Cursor.Position);
            using (var kalem = new Pen(Color.Firebrick, 1.2f) { DashStyle = DashStyle.Dash })
            {
                var noktalar = _komutNoktalari.Select(p => cizimPaneli1.Gorunum.DunyadanEkrana(p[0], p[1], w, h)).ToList();
                if (_duzenKomutu == DuzenKomutu.Cit)
                {
                    noktalar.Add(fare);
                    if (noktalar.Count >= 2)
                        e.Graphics.DrawLines(kalem, noktalar.ToArray());
                }
                else
                {
                    // Aynalamada eksen, hizalamada son kaynak noktasından hedefe giden çizgi gösterilir.
                    var son = noktalar[noktalar.Count - 1];
                    e.Graphics.DrawLine(kalem, son, fare);
                    if (_duzenKomutu == DuzenKomutu.Hizala && noktalar.Count >= 2)
                        e.Graphics.DrawLine(kalem, noktalar[0], noktalar[1]);
                }
                foreach (var p in noktalar.Take(_komutNoktalari.Count))
                    e.Graphics.DrawRectangle(kalem, p.X - 3, p.Y - 3, 6, 6);
            }
        }

        // ------------------------------------------------------------------ pano

        private void PanoyaAl(List<CizimNesnesi> secili, bool kes)
        {
            _pano.Clear();
            _pano.AddRange(secili.Select(NesneDonusumleri.Klonla));
            // Referans noktası verilmeyen kopyada referans, objelerin sol alt köşesidir.
            var noktalar = secili.SelectMany(n => n.UcNoktalariAl()).ToList();
            _panoX = noktalar.Count > 0 ? noktalar.Min(p => p.X) : 0;
            _panoY = noktalar.Count > 0 ? noktalar.Min(p => p.Y) : 0;
            if (kes)
            {
                foreach (var nesne in secili)
                    cizimPaneli1.Nesneler.Remove(nesne);
                cizimPaneli1.DegisiklikKaydet();
                cizimPaneli1.SecimDegistiBildir();
                cizimPaneli1.Invalidate();
            }
            KomutIpucu(_pano.Count + " obje panoya " + (kes ? "kesildi" : "kopyalandı") + ". Yapıştır (Ctrl+V) tıklanan yere, Koordinatlı Yerine Yapıştır (Ctrl+Shift+V) aynı koordinata yapıştırır.");
        }

        private void KoordinatliYapistir()
        {
            if (_pano.Count == 0)
            {
                MessageBox.Show("Pano boş. Önce objeleri panoya kopyalayın.", "Koordinatlı Yerine Yapıştır", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            KomutuBitir();
            var kopyalar = _pano.Select(NesneDonusumleri.Klonla).ToList();
            Ekle(kopyalar, true);
            KomutIpucu(kopyalar.Count + " obje kopyalandıkları koordinata yapıştırıldı.");
        }

        // ------------------------------------------------------------------ patlat, çoklu doğru

        private void Patlat(List<CizimNesnesi> secili)
        {
            var koseNoktalari = false;
            if (secili.Any(n => n is CokluDogruNesnesi))
            {
                var cevap = MessageBox.Show("Çoklu doğruların köşelerine nokta objesi de konsun mu?", "Patlat", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (cevap == DialogResult.Cancel)
                    return;
                koseNoktalari = cevap == DialogResult.Yes;
            }
            var buyukYuzey = secili.OfType<YuzeyNesnesi>().Sum(y => y.UcgenSayisi);
            if (buyukYuzey > 100000 &&
                MessageBox.Show("Seçimde " + buyukYuzey.ToString("N0") + " üçgenli yüzey var; patlatılınca yaklaşık " + (buyukYuzey * 1.5).ToString("N0") +
                                " çizgi ve noktaları oluşacak. Devam edilsin mi?", "Patlat", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var parcalar = new List<CizimNesnesi>();
            var patlayan = 0;
            foreach (var nesne in secili)
            {
                var sonuc = GeometriIslemleri.Patlat(nesne, koseNoktalari);
                if (sonuc == null)
                    continue;
                // Kilometre etiketi güzergahla birlikte silinmesin diye yalnızca kendisi patlatılır.
                cizimPaneli1.Nesneler.Remove(nesne);
                parcalar.AddRange(sonuc);
                patlayan++;
            }
            if (patlayan == 0)
            {
                KomutIpucu("Patlat: seçimde patlatılabilir obje yok (çoklu doğru, blok, güzergah, kilometre etiketi ya da yüzey patlatılabilir).");
                return;
            }
            Ekle(parcalar, true);
            KomutIpucu("Patlat: " + patlayan + " obje " + parcalar.Count + " objeye ayrıldı" + (patlayan < secili.Count ? " (" + (secili.Count - patlayan) + " obje zaten parçalarına ayrılamaz)" : "") + ".");
        }

        private void CokluDogruyaDonustur(List<CizimNesnesi> secili)
        {
            var cokluDogrular = GeometriIslemleri.CokluDogruyaBirlestir(secili, out var kullanilanlar);
            if (cokluDogrular.Count == 0)
            {
                KomutIpucu("Çoklu Doğruya Dönüştür: seçimde çizgi, yay ya da açık çoklu doğru yok.");
                return;
            }
            foreach (var nesne in kullanilanlar)
                cizimPaneli1.Nesneler.Remove(nesne);
            Ekle(cokluDogrular.Cast<CizimNesnesi>().ToList(), true);
            var kullanilmayan = secili.Count - kullanilanlar.Count;
            KomutIpucu("Çoklu Doğruya Dönüştür: " + kullanilanlar.Count + " obje " + cokluDogrular.Count + " çoklu doğru oldu" +
                       (cokluDogrular.Count > 1 ? " (uçları birleşmeyen parçalar ayrı çoklu doğrulardır)" : "") +
                       (kullanilmayan > 0 ? "; " + kullanilmayan + " obje çizgi/yay olmadığı için alınmadı" : "") + ".");
        }

        // ------------------------------------------------------------------ bul / değiştir

        private void BulDegistir()
        {
            KomutuBitir();
            using (var dlg = new BulDegistirForm(cizimPaneli1))
                dlg.ShowDialog(this);
            cizimPaneli1.Invalidate();
            ozellikGrid.Refresh();
        }

        // ------------------------------------------------------------------ seçim

        private enum SecimBirlesimi
        {
            Yeni,
            Ekle,
            Cikar,
            SecimIcinde
        }

        private static bool Secilebilir(CizimNesnesi n) => n.Tabaka == null || (n.Tabaka.Gorunur && !n.Tabaka.Kilitli);

        private void SecimiUygula(Func<CizimNesnesi, bool> kosul, SecimBirlesimi birlesim)
        {
            foreach (var nesne in cizimPaneli1.Nesneler)
            {
                var uygun = Secilebilir(nesne) && kosul(nesne);
                switch (birlesim)
                {
                    case SecimBirlesimi.Yeni: nesne.Secili = uygun; break;
                    case SecimBirlesimi.Ekle: if (uygun) nesne.Secili = true; break;
                    case SecimBirlesimi.Cikar: if (uygun) nesne.Secili = false; break;
                    case SecimBirlesimi.SecimIcinde: nesne.Secili = nesne.Secili && uygun; break;
                }
            }
            cizimPaneli1.SecimDegistiBildir();
            cizimPaneli1.Invalidate();
        }

        private void SecimiTersCevir()
        {
            foreach (var nesne in cizimPaneli1.Nesneler)
                nesne.Secili = Secilebilir(nesne) && !nesne.Secili;
            cizimPaneli1.SecimDegistiBildir();
            cizimPaneli1.Invalidate();
        }

        private void BenzerleriniSec()
        {
            var ornekler = SeciliNesneler().Select(n => n.Tip + "|" + n.TabakaAdi).ToList();
            if (ornekler.Count == 0)
            {
                KomutIpucu("Benzerlerini Seç: önce örnek olacak objeleri seçin.");
                return;
            }
            var kume = new HashSet<string>(ornekler);
            SecimiUygula(n => kume.Contains(n.Tip + "|" + n.TabakaAdi), SecimBirlesimi.Yeni);
        }

        private void OncekiSecim()
        {
            var onceki = new HashSet<CizimNesnesi>(_oncekiSecim.Where(n => cizimPaneli1.Nesneler.Contains(n)));
            if (onceki.Count == 0)
            {
                KomutIpucu("Önceki Seçim: hatırlanan seçim yok.");
                return;
            }
            SecimiUygula(onceki.Contains, SecimBirlesimi.Yeni);
        }

        private void HizliSecim()
        {
            KomutuBitir();
            using (var dlg = new HizliSecimForm(cizimPaneli1.Nesneler, cizimPaneli1.Tabakalar))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                SecimiUygula(dlg.Uyar, (SecimBirlesimi)dlg.BirlesimSirasi);
            }
        }

        /// <summary>Seçim değişince: seçili sayısı yazılır, birden çok seçimde ortak özellikler gösterilir, önceki seçim hatırlanır.</summary>
        private void DuzenSecimDegisti()
        {
            var secili = SeciliNesneler();
            _seciliSayisiLabel.Text = secili.Count > 0 ? secili.Count + " obje seçili" : "";
            if (secili.Count > 1)
                ozellikGrid.SelectedObjects = secili.Cast<object>().ToArray();
            if (secili.Count == 0 && _sonSecim.Count > 0)
                _oncekiSecim = _sonSecim;
            _sonSecim = secili;
        }

        private bool SayiSor(string baslik, string etiket, double oneri, out double deger)
        {
            deger = 0;
            using (var dlg = new Form
            {
                Text = baslik, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(300, 90)
            })
            {
                dlg.Controls.Add(new Label { Text = etiket, Location = new Point(12, 17), AutoSize = true });
                var kutu = new TextBox { Text = oneri.ToString("0.###", CultureInfo.InvariantCulture), Location = new Point(170, 14), Size = new Size(118, 20) };
                var tamam = new Button { Text = "Tamam", Location = new Point(132, 52), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(213, 52), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { kutu, tamam, iptal });
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;
                if (NcnDosyasi.SayiOku(kutu.Text, out deger))
                    return true;
                MessageBox.Show("Lütfen geçerli bir sayı girin.", baslik, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
