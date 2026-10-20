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
            Cit,
            CokluDogruCiz,
            NoktaAt,
            YayCiz,
            YaziYaz,
            OlcuDogrusal,
            OlcuHizali,
            OlcuAcisal,
            OlcuYaricap,
            OlcuCap,
            Kilavuz,
            Sil
        }

        private static readonly Dictionary<DuzenKomutu, string> KomutAdlari = new Dictionary<DuzenKomutu, string>
        {
            { DuzenKomutu.PanoyaKopyala, "Panoya Kopyala" }, { DuzenKomutu.PanoyaKes, "Panoya Kes" },
            { DuzenKomutu.ReferansKopyala, "Referans Noktası ile Kopyala" }, { DuzenKomutu.Uzat, "Uzat" },
            { DuzenKomutu.Paralel, "Paralel" }, { DuzenKomutu.Aynala, "Aynala" }, { DuzenKomutu.Hizala, "2 Noktadan Dönüşüm" },
            { DuzenKomutu.Patlat, "Patlat" }, { DuzenKomutu.CokluDogruCiz, "Çoklu Doğru Çiz" }, { DuzenKomutu.NoktaAt, "Nokta At" }, { DuzenKomutu.YayCiz, "Yay Çiz" }, { DuzenKomutu.YaziYaz, "Yazı Yaz" }, { DuzenKomutu.CokluDogru, "Çoklu Doğruya Dönüştür" }, { DuzenKomutu.TopluDegistir, "Toplu Obje Değiştir" },
            { DuzenKomutu.OlcuDogrusal, "Doğrusal Ölçü" }, { DuzenKomutu.OlcuHizali, "Hizalı Ölçü" }, { DuzenKomutu.OlcuAcisal, "Açısal Ölçü" },
            { DuzenKomutu.OlcuYaricap, "Yarıçap Ölçüsü" }, { DuzenKomutu.OlcuCap, "Çap Ölçüsü" }, { DuzenKomutu.Kilavuz, "Kılavuz Çizgi" },
            { DuzenKomutu.Sil, "Sil" }
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

            // --- Geri Al / İleri Al (en fazla 10 adım) ---
            _geriAlOgesi = Oge("Geri Al", "Ctrl+Z", GeriAl);
            _ileriAlOgesi = Oge("İleri Al", "Ctrl+Y", IleriAl);
            duzenleMenuItem.DropDownItems.Insert(0, new ToolStripSeparator());
            duzenleMenuItem.DropDownItems.Insert(0, _ileriAlOgesi);
            duzenleMenuItem.DropDownItems.Insert(0, _geriAlOgesi);
            cizimPaneli1.GeriAlAcik = true;
            cizimPaneli1.GeriAlSiniri = 10;
            cizimPaneli1.GecmisDegisti += (s, e) => GeriAlMenusunuGuncelle();
            cizimPaneli1.GecmisiTemizle();
            // Menü komutları (dosya açma, yüzey/güzergah oluşturma, koordinat dönüşümü...) ve fare işlemleri
            // bittikten sonra çizim değiştiyse geri al adımı kaydedilir.
            anaMenuStrip.ItemClicked += (s, e) => BeginInvoke((Action)cizimPaneli1.DegisiklikKaydet);
            foreach (ToolStripMenuItem ust in anaMenuStrip.Items.OfType<ToolStripMenuItem>())
                ust.DropDownItemClicked += (s, e) => BeginInvoke((Action)cizimPaneli1.DegisiklikKaydet);
            cizimPaneli1.MouseUp += (s, e) => BeginInvoke((Action)cizimPaneli1.DegisiklikKaydet);

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
            // Sil: Kaydır, Döndür, Kırp, Kopyala, Ölçekle ile aynı blokta (ribbon'da "Değiştir" grubu).
            duzenleMenuItem.DropDownItems.Insert(duzenleMenuItem.DropDownItems.IndexOf(olcekleMenuItem) + 1,
                Oge("Sil", "Del", () => KomutBaslat(DuzenKomutu.Sil)));

            // --- Çizim: çoklu doğru (polyline) ---
            var cokluDogruCizOgesi = Oge("Çoklu Doğru (Polyline) Çiz", null, () => KomutBaslat(DuzenKomutu.CokluDogruCiz));
            cizimMenuItem.DropDownItems.Insert(cizimMenuItem.DropDownItems.IndexOf(cizgiCizMenuItem) + 1, cokluDogruCizOgesi);
            cizimMenuItem.DropDownItems.Add(Oge("Yay Çiz (3 nokta)", null, () => KomutBaslat(DuzenKomutu.YayCiz)));
            cizimMenuItem.DropDownItems.Add(new ToolStripSeparator());
            cizimMenuItem.DropDownItems.Add(Oge("Nokta At", null, () => KomutBaslat(DuzenKomutu.NoktaAt)));
            cizimMenuItem.DropDownItems.Add(Oge("Yazı Yaz", null, () => KomutBaslat(DuzenKomutu.YaziYaz)));

            // --- Hesap menüsü ---
            var hesapMenu = new ToolStripMenuItem("Hesap");
            hesapMenu.DropDownItems.Add(Oge("Poligon Hesabı...", null, PoligonHesabiniAc));
            var yolSirasi = anaMenuStrip.Items.Cast<ToolStripItem>().ToList().FindIndex(i => i.Text == "Yol");
            anaMenuStrip.Items.Insert(yolSirasi >= 0 ? yolSirasi + 1 : anaMenuStrip.Items.Count, hesapMenu);

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

            // Son komutu tekrarlama: komut bittikten sonra (Seç aracında, bekleyen seçim yokken) Boşluk ya da Enter son
            // komutu yeniden başlatır. Tuşa basıldığı andaki durum, panel kendi işini yapmadan önce alınır; böylece
            // seçimi onaylayan Enter komutu tekrar başlatmaz. Sağ tık komut tekrarlamaz, bağlam menüsünü açar.
            cizimPaneli1.PreviewKeyDown += (s, e) => _tekrarUygun = EkranBosBekliyor();
            cizimPaneli1.KeyDown += (s, e) =>
            {
                if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) && !e.Control && _tekrarUygun)
                {
                    _tekrarUygun = false;
                    SonKomutuTekrarla();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            SagTikMenusunuKur();
            cizimPaneli1.AracDegisti += (s, arac) =>
            {
                if (arac != CizimAraci.Sec && arac != CizimAraci.NoktaAl)
                    SonKomutuKaydet(AracAdi(arac), () => cizimPaneli1.AktifArac = arac);
            };
        }

        // ------------------------------------------------------------------ son komutu tekrarla

        private Action _sonKomut;
        private string _sonKomutAdi;
        private bool _tekrarUygun;

        // Ekranda çalışan bir komut ya da bekleyen bir seçim yok mu?
        private bool EkranBosBekliyor()
        {
            return cizimPaneli1.AktifArac == CizimAraci.Sec && _duzenKomutu == DuzenKomutu.Yok && _secimBekleyenKomut == DuzenKomutu.Yok
                   && !_noktaSecimiBekleniyor && !_tabakaKapatiliyor;
        }

        private void SonKomutuKaydet(string ad, Action eylem)
        {
            _sonKomutAdi = ad;
            _sonKomut = eylem;
            // Sağ tık menüsündeki "Son Komutlar" listesi: en yeni başta, aynı komut bir kez, en fazla 10 komut.
            _sonKomutlar.RemoveAll(k => k.Key == ad);
            _sonKomutlar.Insert(0, new KeyValuePair<string, Action>(ad, eylem));
            if (_sonKomutlar.Count > 10)
                _sonKomutlar.RemoveRange(10, _sonKomutlar.Count - 10);
        }

        private readonly List<KeyValuePair<string, Action>> _sonKomutlar = new List<KeyValuePair<string, Action>>();

        private void SonKomutuTekrarla()
        {
            if (_sonKomut == null)
                return;
            KomutIpucu("Son komut tekrarlanıyor: " + _sonKomutAdi);
            _sonKomut();
        }

        private static string AracAdi(CizimAraci arac)
        {
            switch (arac)
            {
                case CizimAraci.Cizgi: return "Çizgi";
                case CizimAraci.Kaydir: return "Kaydır";
                case CizimAraci.Kopyala: return "Kopyala";
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

        // Kısayollar çizim ekranı odaktayken çalışır (özellikler panelinde metin kopyalarken karışmasın diye).
        private void DuzenKisayollari(object sender, KeyEventArgs e)
        {
            if (_duzenKomutu == DuzenKomutu.Kilavuz && !e.Control)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                    KilavuzuTamamla();
                else if (e.KeyCode == Keys.Back && _komutNoktalari.Count > 0)
                {
                    _komutNoktalari.RemoveAt(_komutNoktalari.Count - 1);
                    KomutIpucunuGuncelle();
                    cizimPaneli1.Invalidate();
                }
                else
                    return;
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (_duzenKomutu == DuzenKomutu.CokluDogruCiz && !e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                    case Keys.Space:
                        CokluDogruyuTamamla(false);
                        break;
                    case Keys.K:
                    case Keys.C:
                        CokluDogruyuTamamla(true);
                        break;
                    case Keys.Back:
                        if (_komutNoktalari.Count > 0)
                            _komutNoktalari.RemoveAt(_komutNoktalari.Count - 1);
                        KomutIpucunuGuncelle();
                        cizimPaneli1.Invalidate();
                        break;
                    default:
                        return;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
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
                case Keys.Z: GeriAl(); break;
                case Keys.Y: IleriAl(); break;
                case Keys.F: BulDegistir(); break;
                default: return;
            }
            if (komut != DuzenKomutu.Yok)
                KomutBaslat(komut);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private List<CizimNesnesi> SeciliNesneler() => cizimPaneli1.Nesneler.Where(n => n.Secili).ToList();

        // ------------------------------------------------------------------ geri al / ileri al

        private ToolStripMenuItem _geriAlOgesi, _ileriAlOgesi;

        private void GeriAlMenusunuGuncelle()
        {
            _geriAlOgesi.Enabled = cizimPaneli1.GeriAlinabilir;
            _ileriAlOgesi.Enabled = cizimPaneli1.Ilerlenebilir;
        }

        private void GeriAl()
        {
            KomutuBitir();
            if (!cizimPaneli1.GeriAlinabilir)
            {
                KomutIpucu("Geri alınacak adım yok (en fazla 10 adım geri alınabilir).");
                return;
            }
            cizimPaneli1.GeriAl();
            ozellikGrid.Refresh();
            KomutIpucu("Geri alındı.");
        }

        private void IleriAl()
        {
            KomutuBitir();
            if (!cizimPaneli1.Ilerlenebilir)
                return;
            cizimPaneli1.Ilerle();
            ozellikGrid.Refresh();
            KomutIpucu("İleri alındı.");
        }

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
            SonKomutuKaydet(KomutAdlari.TryGetValue(komut, out var komutAdi) ? komutAdi : komut == DuzenKomutu.Yapistir ? "Yapıştır" : "Çit ile Seç", () => KomutBaslat(komut));
            _tabakaKapatiliyor = false;

            var secili = SeciliNesneler();
            var secimGerekli = komut != DuzenKomutu.Yapistir && komut != DuzenKomutu.Cit && komut != DuzenKomutu.Uzat && komut != DuzenKomutu.CokluDogruCiz
                               && komut != DuzenKomutu.NoktaAt && komut != DuzenKomutu.YayCiz && komut != DuzenKomutu.YaziYaz && !OlcuKomutuMu(komut);
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
                case DuzenKomutu.Sil:
                    cizimPaneli1.SeciliNesneleriSil();
                    cizimPaneli1.DegisiklikKaydet();
                    KomutIpucu("Sil: " + secili.Count + " obje silindi (geri almak için Ctrl+Z).");
                    return;
                case DuzenKomutu.CokluDogru:
                    CokluDogruyaDonustur(secili);
                    return;
                case DuzenKomutu.TopluDegistir:
                    ozellikGrid.SelectedObjects = secili.Cast<object>().ToArray();
                    OzellikleriGoster();
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
            cizimPaneli1.NoktaAlmayaBasla(komut == DuzenKomutu.Uzat || komut == DuzenKomutu.OlcuYaricap || komut == DuzenKomutu.OlcuCap);
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
                case DuzenKomutu.NoktaAt: ipucu = "noktanın yerini tıklayın (art arda atılabilir). Bitirmek için sağ tık ya da Esc."; break;
                case DuzenKomutu.YayCiz:
                    ipucu = new[] { "yayın başlangıç noktasını tıklayın.", "yayın üzerinden geçeceği ikinci noktayı tıklayın.", "yayın bitiş noktasını tıklayın." }[Math.Min(2, _komutNoktalari.Count)];
                    break;
                case DuzenKomutu.YaziYaz: ipucu = "yazının yerini (sol alt köşesini) tıklayın; metin sorulur. Bitirmek için sağ tık ya da Esc."; break;
                case DuzenKomutu.CokluDogruCiz:
                    ipucu = (_komutNoktalari.Count == 0 ? "ilk noktayı tıklayın." : (_komutNoktalari.Count + 1) + ". noktayı tıklayın.") +
                            "  Enter / sağ tık: bitir,  K: kapatarak bitir (ilk noktaya tıklamak da kapatır),  Backspace: son noktayı geri al,  Esc: bitir.";
                    break;
                default: ipucu = OlcuKomutuMu(_duzenKomutu) ? OlcuIpucu() : ""; break;
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
            if (_duzenKomutu == DuzenKomutu.CokluDogruCiz)
            {
                CokluDogruyuTamamla(false);
                return;
            }
            if (_duzenKomutu == DuzenKomutu.Kilavuz)
            {
                KilavuzuTamamla();
                return;
            }
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
            if (OlcuKomutuMu(_duzenKomutu))
            {
                OlcuNoktaTiklandi(x, y);
                return;
            }

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
                    TabakalariAktifBelgeyeBagla(kopyalar);
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

                case DuzenKomutu.NoktaAt:
                {
                    // Nokta numarası, çizimdeki en büyük sayısal nokta numarasından devam eder.
                    var enBuyuk = 0;
                    foreach (var mevcut in cizimPaneli1.Nesneler.OfType<NoktaNesnesi>())
                    {
                        if (int.TryParse(mevcut.NoktaAdi, out var no) && no > enBuyuk)
                            enBuyuk = no;
                    }
                    var nokta = new NoktaNesnesi { X = x, Y = y, Z = 0, NoktaAdi = (enBuyuk + 1).ToString(CultureInfo.InvariantCulture) };
                    // Pencerede numara ve koordinatlar düzeltilebilir; İptal'de nokta tıklanan yerde, verilen numarayla kalır.
                    NoktaBilgisiSor(nokta);
                    Ekle(new List<CizimNesnesi> { nokta }, false);
                    KomutIpucu("Nokta At: " + nokta.NoktaAdi + " numaralı nokta atıldı. Sıradaki noktayı tıklayın ya da sağ tıkla bitirin.");
                    break;
                }

                case DuzenKomutu.YayCiz:
                {
                    var son = _komutNoktalari.LastOrDefault();
                    if (son != null && Math.Abs(son[0] - x) < 1e-9 && Math.Abs(son[1] - y) < 1e-9)
                        break;
                    _komutNoktalari.Add(new[] { x, y });
                    if (_komutNoktalari.Count < 3)
                    {
                        KomutIpucunuGuncelle();
                        cizimPaneli1.Invalidate();
                        break;
                    }
                    var yay = UcNoktadanYay(_komutNoktalari[0], _komutNoktalari[1], _komutNoktalari[2]);
                    KomutuBitir();
                    if (yay == null)
                    {
                        KomutIpucu("Yay Çiz: üç nokta aynı doğru üzerinde; yay oluşmadı.");
                        break;
                    }
                    Ekle(new List<CizimNesnesi> { yay }, false);
                    KomutIpucu(string.Format(CultureInfo.InvariantCulture, "Yay çizildi: R = {0:0.###} m, uzunluk {1:0.###} m.", yay.Yaricap, yay.Uzunluk));
                    break;
                }

                case DuzenKomutu.YaziYaz:
                {
                    if (!YaziBilgisiSor(out var metin, out var yukseklik, out var donus))
                        break;
                    var yazi = new YaziNesnesi { Metin = metin, KonumX = x, KonumY = y, Yukseklik = yukseklik, Donus = donus, HizaYatay = YatayHizalama.Sol, HizaDikey = DikeyHizalama.Alt };
                    Ekle(new List<CizimNesnesi> { yazi }, false);
                    KomutIpucu("Yazı Yaz: yazı eklendi. Sıradaki yazının yerini tıklayın ya da sağ tıkla bitirin.");
                    break;
                }

                case DuzenKomutu.CokluDogruCiz:
                {
                    // İlk noktaya (yakalamayla) yeniden tıklanırsa çoklu doğru kapatılarak biter; aynı noktaya art arda
                    // tıklama yok sayılır.
                    if (_komutNoktalari.Count >= 3 && Math.Abs(_komutNoktalari[0][0] - x) < 1e-6 && Math.Abs(_komutNoktalari[0][1] - y) < 1e-6)
                    {
                        CokluDogruyuTamamla(true);
                        break;
                    }
                    var son = _komutNoktalari.LastOrDefault();
                    if (son != null && Math.Abs(son[0] - x) < 1e-9 && Math.Abs(son[1] - y) < 1e-9)
                        break;
                    _komutNoktalari.Add(new[] { x, y });
                    KomutIpucunuGuncelle();
                    cizimPaneli1.Invalidate();
                    break;
                }
            }
        }

        /// <summary>Çizilen noktalardan çoklu doğruyu oluşturur (en az iki nokta varsa) ve komutu bitirir.</summary>
        private void CokluDogruyuTamamla(bool kapali)
        {
            var noktalar = new List<double[]>(_komutNoktalari);
            KomutuBitir();
            if (noktalar.Count < 2 || (kapali && noktalar.Count < 3))
                return;

            var cokluDogru = new CokluDogruNesnesi { Kapali = kapali };
            foreach (var p in noktalar)
                cokluDogru.Koseler.Add(new CokluDogruKosesi { X = p[0], Y = p[1] });
            Ekle(new List<CizimNesnesi> { cokluDogru }, false);
            KomutIpucu("Çoklu doğru çizildi: " + noktalar.Count + " köşe" + (kapali ? ", kapalı" : "") + string.Format(CultureInfo.InvariantCulture, ", uzunluk {0:0.###} m.", cokluDogru.Uzunluk));
        }

        /// <summary>Başlangıç, ara ve bitiş noktasından geçen yay; noktalar aynı doğru üzerindeyse null.</summary>
        private static YayNesnesi UcNoktadanYay(double[] a, double[] b, double[] c)
        {
            var payda = 2 * (a[0] * (b[1] - c[1]) + b[0] * (c[1] - a[1]) + c[0] * (a[1] - b[1]));
            if (Math.Abs(payda) < 1e-12)
                return null;
            double a2 = a[0] * a[0] + a[1] * a[1], b2 = b[0] * b[0] + b[1] * b[1], c2 = c[0] * c[0] + c[1] * c[1];
            var mx = (a2 * (b[1] - c[1]) + b2 * (c[1] - a[1]) + c2 * (a[1] - b[1])) / payda;
            var my = (a2 * (c[0] - b[0]) + b2 * (a[0] - c[0]) + c2 * (b[0] - a[0])) / payda;
            var r = Math.Sqrt((a[0] - mx) * (a[0] - mx) + (a[1] - my) * (a[1] - my));
            double basAci = Math.Atan2(a[1] - my, a[0] - mx) * 180 / Math.PI, bitAci = Math.Atan2(c[1] - my, c[0] - mx) * 180 / Math.PI;
            // Yay saat yönünün tersine çizilir: başlangıç-ara-bitiş saat yönündeyse uçlar yer değiştirir.
            var saatYonunun = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0]) > 0;
            return new YayNesnesi { MerkezX = mx, MerkezY = my, Yaricap = r, BaslangicAcisi = saatYonunun ? basAci : bitAci, BitisAcisi = saatYonunun ? bitAci : basAci };
        }

        private string _sonYaziMetni = "";
        private double _sonYaziYuksekligi;
        private double _sonYaziDonusu;

        // Yazının metnini, yüksekliğini ve dönüşünü sorar. Yükseklik varsayılanı, proje ölçeğinde kağıtta 2.5 mm'dir.
        private bool YaziBilgisiSor(out string metin, out double yukseklik, out double donus)
        {
            metin = null;
            yukseklik = donus = 0;
            if (_sonYaziYuksekligi <= 0)
                _sonYaziYuksekligi = 2.5 * CizimAyarlari.ProjeOlcegi / 1000.0;
            using (var dlg = new Form
            {
                Text = "Yazı Yaz", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(360, 150)
            })
            {
                dlg.Controls.Add(new Label { Text = "Metin:", Location = new Point(12, 17), AutoSize = true });
                var metinKutu = new TextBox { Text = _sonYaziMetni, Location = new Point(130, 14), Size = new Size(218, 20) };
                dlg.Controls.Add(new Label { Text = "Yükseklik (m):", Location = new Point(12, 47), AutoSize = true });
                var yukseklikKutu = new TextBox { Text = _sonYaziYuksekligi.ToString("0.###", CultureInfo.InvariantCulture), Location = new Point(130, 44), Size = new Size(90, 20) };
                dlg.Controls.Add(new Label { Text = "Döndürme (derece):", Location = new Point(12, 77), AutoSize = true });
                var donusKutu = new TextBox { Text = _sonYaziDonusu.ToString("0.###", CultureInfo.InvariantCulture), Location = new Point(130, 74), Size = new Size(90, 20) };
                var tamam = new Button { Text = "Tamam", Location = new Point(192, 112), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(273, 112), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { metinKutu, yukseklikKutu, donusKutu, tamam, iptal });
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;
                if (metinKutu.Text.Trim().Length == 0 || !NcnDosyasi.SayiOku(yukseklikKutu.Text, out yukseklik) || yukseklik <= 0 || !NcnDosyasi.SayiOku(donusKutu.Text, out donus))
                {
                    MessageBox.Show("Metin boş olamaz; yükseklik sıfırdan büyük, döndürme bir sayı olmalıdır.", "Yazı Yaz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                metin = metinKutu.Text;
                _sonYaziMetni = metin;
                _sonYaziYuksekligi = yukseklik;
                _sonYaziDonusu = donus;
                return true;
            }
        }

        /// <summary>
        /// Atılan noktanın numarasını ve koordinatlarını (Y sağa, X yukarı, Z) gösterir; Tamam'da girilen değerler noktaya
        /// yazılır, İptal'de nokta olduğu gibi kalır. Değiştirilmeyen koordinat kutusu tıklanan tam değeri korur.
        /// </summary>
        private bool NoktaBilgisiSor(NoktaNesnesi nokta)
        {
            string Metin(double deger) => deger.ToString("0.000", CultureInfo.InvariantCulture);
            using (var dlg = new Form
            {
                Text = "Nokta At", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(300, 180)
            })
            {
                TextBox Satir(string etiket, string deger, int ust)
                {
                    dlg.Controls.Add(new Label { Text = etiket, Location = new Point(12, ust + 3), AutoSize = true });
                    var kutu = new TextBox { Text = deger, Location = new Point(110, ust), Size = new Size(178, 20), TextAlign = HorizontalAlignment.Right };
                    dlg.Controls.Add(kutu);
                    return kutu;
                }
                var noKutu = Satir("Nokta No:", nokta.NoktaAdi, 14);
                noKutu.TextAlign = HorizontalAlignment.Left;
                var yKutu = Satir("Y (sağa):", Metin(nokta.X), 44);
                var xKutu = Satir("X (yukarı):", Metin(nokta.Y), 74);
                var zKutu = Satir("Z (kot):", Metin(nokta.Z), 104);
                var tamam = new Button { Text = "Tamam", Location = new Point(132, 142), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(213, 142), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { tamam, iptal });
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                dlg.Shown += (s, e) => noKutu.SelectAll();

                double yeniY = 0, yeniX = 0, yeniZ = 0;
                dlg.FormClosing += (s, e) =>
                {
                    if (dlg.DialogResult != DialogResult.OK)
                        return;
                    if (noKutu.Text.Trim().Length == 0 || !NcnDosyasi.SayiOku(yKutu.Text, out yeniY) || !NcnDosyasi.SayiOku(xKutu.Text, out yeniX) || !NcnDosyasi.SayiOku(zKutu.Text, out yeniZ))
                    {
                        MessageBox.Show(dlg, "Nokta numarası boş olamaz; Y, X ve Z sayı olmalıdır.", "Nokta At", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                    }
                };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                nokta.NoktaAdi = noKutu.Text.Trim();
                if (yKutu.Text.Trim() != Metin(nokta.X))
                    nokta.X = yeniY;
                if (xKutu.Text.Trim() != Metin(nokta.Y))
                    nokta.Y = yeniX;
                nokta.Z = yeniZ;
                return true;
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
            if (OlcuKomutuMu(_duzenKomutu))
            {
                OlcuOnizlemesiCiz(e.Graphics);
                return;
            }
            if (_komutNoktalari.Count == 0)
                return;

            var fare = cizimPaneli1.PointToClient(Cursor.Position);
            using (var kalem = new Pen(Color.Firebrick, 1.2f) { DashStyle = DashStyle.Dash })
            {
                var noktalar = _komutNoktalari.Select(p => cizimPaneli1.Gorunum.DunyadanEkrana(p[0], p[1], w, h)).ToList();
                if (_duzenKomutu == DuzenKomutu.Cit || _duzenKomutu == DuzenKomutu.CokluDogruCiz)
                {
                    noktalar.Add(fare);
                    if (noktalar.Count >= 2)
                        e.Graphics.DrawLines(kalem, noktalar.ToArray());
                }
                else if (_duzenKomutu == DuzenKomutu.YayCiz && _komutNoktalari.Count == 2)
                {
                    // İki nokta verildiyse fareden geçen yayın önizlemesi.
                    cizimPaneli1.Gorunum.EkrandanDunyayaHassas(fare, w, h, out var fx, out var fy);
                    var onizleme = UcNoktadanYay(_komutNoktalari[0], _komutNoktalari[1], new[] { fx, fy });
                    if (onizleme != null)
                    {
                        var yayNoktalari = GeometriIslemleri.Primitifler(onizleme)[0].Noktalar(180)
                            .Select(p => cizimPaneli1.Gorunum.DunyadanEkrana(p[0], p[1], w, h)).ToArray();
                        if (yayNoktalari.Length >= 2)
                            e.Graphics.DrawLines(kalem, yayNoktalari);
                    }
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
            SonKomutuKaydet("Koordinatlı Yerine Yapıştır", KoordinatliYapistir);
            if (_pano.Count == 0)
            {
                MessageBox.Show("Pano boş. Önce objeleri panoya kopyalayın.", "Koordinatlı Yerine Yapıştır", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            KomutuBitir();
            var kopyalar = _pano.Select(NesneDonusumleri.Klonla).ToList();
            TabakalariAktifBelgeyeBagla(kopyalar);
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

        // ------------------------------------------------------------------ hesap

        private PoligonHesabiForm _poligonHesabi;

        // Poligon hesabı penceresi çizim ekranının üzerinde açık kalır (ekrandan nokta alabilmek için); tek tanedir.
        private void PoligonHesabiniAc()
        {
            if (_poligonHesabi == null || _poligonHesabi.IsDisposed)
            {
                _poligonHesabi = new PoligonHesabiForm(cizimPaneli1);
                _poligonHesabi.Show(this);
            }
            else
            {
                _poligonHesabi.Activate();
            }
        }

        // ------------------------------------------------------------------ bul / değiştir

        private void BulDegistir()
        {
            SonKomutuKaydet("Bul / Değiştir", BulDegistir);
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
            SonKomutuKaydet("Hızlı Seçim", HizliSecim);
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
