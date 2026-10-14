using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Üçgen model (TIN) yüzey objesi. Çizim koordinatında X sağa (NCN'deki Y), Y yukarı (NCN'deki X) değeridir.
    /// Yüzey üçgenlerden ve henüz bir üçgene ait olmayan serbest çizgilerden oluşur; üçgeni olmayan yüzey yalnızca
    /// noktalarıyla çizilir.
    ///
    /// Büyük yüzeylerde (yüz binlerce üçgen) düzenleme anlık olsun diye:
    /// - Silinen nokta ve üçgenler listelerden çıkarılmaz, "silinmiş" işaretlenir (diğerlerinin indeksi kaymaz).
    /// - Kenar → üçgen sayısı ve nokta → üçgenleri indeksleri tutulur; düzenlemeler yalnızca dokunulan üçgenlerle çalışır.
    /// - Çizim bir resim önbelleğinde tutulur; görünüm ve yüzey değişmedikçe yeniden çizilmez.
    /// </summary>
    public class YuzeyNesnesi : CizimNesnesi
    {
        // Bu sayının üzerinde kenarı olan yüzeyler hızlı çizilsin diye kenar yumuşatma kapatılır.
        private const int YumusatmaKenarSiniri = 5000;
        // Seçili yüzeyde, ekranda bu sayıdan az nokta görünüyorsa noktalar da işaretlenir.
        private const int NoktaIsaretiSiniri = 4000;
        // Ekranda bu uzunluğa (piksel) kadar olan kenarlar çizgi yerine piksel piksel yazılır.
        private const int KisaKenarPikseli = 6;
        // Tek bir üçgenden geçebilecek en fazla eğri sayısı; aşılırsa o üçgenin eğrileri çizilmez.
        private const int UcgenBasinaEgriSiniri = 500;
        private const int GeriAlSiniri = 20;
        // Geri alma kayıtlarının toplamda tutabileceği liste elemanı sayısı (bellek sınırı).
        private const long GeriAlElemanButcesi = 20000000;

        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Yüzey";

        [DisplayName("Ad")]
        public string Ad { get; set; }

        [DisplayName("Nokta Sayısı")]
        public int NoktaSayisi => _canliNokta;

        [DisplayName("Üçgen Sayısı")]
        public int UcgenSayisi => _canliUcgen;

        [DisplayName("En Düşük Kot")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double EnDusukKot => KotSiniri(false);

        [DisplayName("En Yüksek Kot")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double EnYuksekKot => KotSiniri(true);

        [Browsable(false)]
        public Color Renk { get; set; } = Color.SaddleBrown;

        /// <summary>Nokta numaraları ve koordinatları (aynı indeks aynı noktadır). Numarası null olan nokta silinmiştir.</summary>
        public List<string> Nolar;
        public List<double> X;
        public List<double> Y;
        public List<double> Z;

        /// <summary>Üçgenler: her üçgen için art arda üç nokta indeksi. İlk köşesi -1 olan üçgen silinmiştir.</summary>
        public List<int> Ucgenler;

        /// <summary>Henüz bir üçgenin kenarı olmayan çizgiler: her çizgi için art arda iki nokta indeksi.</summary>
        public List<int> SerbestKenarlar = new List<int>();

        // Ucgenler listesindeki silinmiş (yeniden kullanılabilir) üçgenlerin başlangıç indeksleri.
        private List<int> _bosUcgenler = new List<int>();
        private int _canliNokta;
        private int _canliUcgen;

        // İndeksler ilk gerektiğinde kurulur, sonra her üçgen ekleme/silmede güncellenir.
        private Dictionary<long, int> _kenarSayaci;       // kenar → o kenarı kullanan üçgen sayısı
        private List<List<int>> _noktaUcgenleri;          // nokta → üçgenlerinin Ucgenler'deki başlangıç indeksleri

        // Kopyala ile üretilen kopya, koordinatlar dışındaki listeleri asılla paylaşır; ilk değişiklikte ayrılır.
        private bool _paylasimli;
        private long _enBuyukNo = -1;

        // Çizim önbelleği ve hangi görünüm/yüzey durumu için üretildiği.
        private Bitmap _onbellek;
        private double _obOlcek, _obOfsetX, _obOfsetY;
        private int _obSurum = -1;
        private int _obEgriSurum = -1;
        private bool _obSecili;
        private Color _obRenk;
        private int _surum;
        private bool _birKezCizildi;
        private float[] _ekranX, _ekranY;

        private class Anlik
        {
            public List<string> Nolar;
            public List<double> X, Y, Z;
            public List<int> Ucgenler, SerbestKenarlar, BosUcgenler;
            public int CanliNokta, CanliUcgen;
        }

        private readonly List<Anlik> _geriAlListesi = new List<Anlik>();

        public YuzeyNesnesi(string ad, List<string> nolar, List<double> x, List<double> y, List<double> z, List<int> ucgenler)
        {
            Ad = ad;
            Nolar = nolar;
            X = x;
            Y = y;
            Z = z;
            Ucgenler = ucgenler;
            _canliNokta = nolar.Count(n => n != null);
            for (var i = 0; i + 2 < Ucgenler.Count; i += 3)
            {
                if (Ucgenler[i] < 0)
                    _bosUcgenler.Add(i);
                else
                    _canliUcgen++;
            }
        }

        /// <summary>
        /// Kopya (kaydır/kopyala önizlemesi ve Kopyala komutu için). Koordinatlar kopyalanır; üçgenler ve numaralar
        /// kopyalardan biri düzenlenene kadar asılla paylaşılır.
        /// </summary>
        public YuzeyNesnesi Kopyala()
        {
            _paylasimli = true;
            return new YuzeyNesnesi(Ad, Nolar, new List<double>(X), new List<double>(Y), new List<double>(Z), Ucgenler)
            {
                Renk = Renk,
                SerbestKenarlar = SerbestKenarlar,
                _bosUcgenler = _bosUcgenler,
                _kenarSayaci = _kenarSayaci,
                _noktaUcgenleri = _noktaUcgenleri,
                _canliNokta = _canliNokta,
                _canliUcgen = _canliUcgen,
                _paylasimli = true
            };
        }

        /// <summary>Koordinatlar bu sınıfın dışında (kaydır/döndür/ölçekle) değiştirildiğinde çizim önbelleğini geçersiz kılar.</summary>
        public void KoordinatlarDegisti()
        {
            _surum++;
        }

        /// <summary>Silinmiş nokta ve üçgenler ayıklanmış, indeksleri yeniden sıralanmış veriyi verir (dışa aktarım için).</summary>
        public void CanliVeriAl(List<string> nolar, List<double> x, List<double> y, List<double> z, List<int> ucgenler)
        {
            var yeniIndeks = new int[X.Count];
            for (var i = 0; i < X.Count; i++)
            {
                if (Nolar[i] == null)
                {
                    yeniIndeks[i] = -1;
                    continue;
                }
                yeniIndeks[i] = nolar.Count;
                nolar.Add(Nolar[i]);
                x.Add(X[i]);
                y.Add(Y[i]);
                z.Add(Z[i]);
            }

            for (var i = 0; i + 2 < Ucgenler.Count; i += 3)
            {
                if (Ucgenler[i] < 0)
                    continue;
                ucgenler.Add(yeniIndeks[Ucgenler[i]]);
                ucgenler.Add(yeniIndeks[Ucgenler[i + 1]]);
                ucgenler.Add(yeniIndeks[Ucgenler[i + 2]]);
            }
        }

        private double KotSiniri(bool enYuksek)
        {
            var bulundu = false;
            double sonuc = 0;
            for (var i = 0; i < Z.Count; i++)
            {
                if (Nolar[i] == null)
                    continue;
                if (!bulundu || (enYuksek ? Z[i] > sonuc : Z[i] < sonuc))
                    sonuc = Z[i];
                bulundu = true;
            }
            return sonuc;
        }

        // ------------------------------------------------------------------ indeksler ve temel işlemler

        private static long KenarAnahtari(int a, int b)
        {
            return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        }

        private void IndeksleriHazirla()
        {
            if (_kenarSayaci != null)
                return;

            _kenarSayaci = new Dictionary<long, int>(Ucgenler.Count);
            _noktaUcgenleri = new List<List<int>>(X.Count);
            for (var i = 0; i < X.Count; i++)
                _noktaUcgenleri.Add(null);

            for (var i = 0; i + 2 < Ucgenler.Count; i += 3)
            {
                if (Ucgenler[i] >= 0)
                    IndekseEkle(i);
            }
        }

        private void IndekseEkle(int ucgen)
        {
            for (var k = 0; k < 3; k++)
            {
                int p = Ucgenler[ucgen + k], q = Ucgenler[ucgen + (k + 1) % 3];
                while (_noktaUcgenleri.Count <= p)
                    _noktaUcgenleri.Add(null);
                var liste = _noktaUcgenleri[p];
                if (liste == null)
                    _noktaUcgenleri[p] = liste = new List<int>(6);
                liste.Add(ucgen);

                if (p == q)
                    continue;
                var anahtar = KenarAnahtari(p, q);
                _kenarSayaci.TryGetValue(anahtar, out var sayi);
                _kenarSayaci[anahtar] = sayi + 1;
            }
        }

        private int UcgenEkle(int a, int b, int c)
        {
            int ucgen;
            if (_bosUcgenler.Count > 0)
            {
                ucgen = _bosUcgenler[_bosUcgenler.Count - 1];
                _bosUcgenler.RemoveAt(_bosUcgenler.Count - 1);
                Ucgenler[ucgen] = a;
                Ucgenler[ucgen + 1] = b;
                Ucgenler[ucgen + 2] = c;
            }
            else
            {
                ucgen = Ucgenler.Count;
                Ucgenler.Add(a);
                Ucgenler.Add(b);
                Ucgenler.Add(c);
            }

            _canliUcgen++;
            IndekseEkle(ucgen);
            return ucgen;
        }

        private void UcgenSil(int ucgen)
        {
            for (var k = 0; k < 3; k++)
            {
                int p = Ucgenler[ucgen + k], q = Ucgenler[ucgen + (k + 1) % 3];
                _noktaUcgenleri[p]?.Remove(ucgen);

                if (p == q)
                    continue;
                var anahtar = KenarAnahtari(p, q);
                if (_kenarSayaci.TryGetValue(anahtar, out var sayi))
                {
                    if (sayi <= 1)
                        _kenarSayaci.Remove(anahtar);
                    else
                        _kenarSayaci[anahtar] = sayi - 1;
                }
            }

            Ucgenler[ucgen] = -1;
            _bosUcgenler.Add(ucgen);
            _canliUcgen--;
        }

        private static readonly List<int> BosListe = new List<int>();

        private List<int> NoktaninUcgenleri(int v)
        {
            return v < _noktaUcgenleri.Count && _noktaUcgenleri[v] != null ? _noktaUcgenleri[v] : BosListe;
        }

        // a ve b köşelerini içeren üçgenlerin Ucgenler listesindeki başlangıç indeksleri.
        private List<int> KenarinUcgenleri(int a, int b)
        {
            var bulunanlar = new List<int>(2);
            foreach (var u in NoktaninUcgenleri(a))
            {
                if (Ucgenler[u] == b || Ucgenler[u + 1] == b || Ucgenler[u + 2] == b)
                    bulunanlar.Add(u);
            }
            return bulunanlar;
        }

        private int KarsiKose(int ucgen, int a, int b)
        {
            int p = Ucgenler[ucgen], q = Ucgenler[ucgen + 1], r = Ucgenler[ucgen + 2];
            return p != a && p != b ? p : q != a && q != b ? q : r;
        }

        private bool UcgenVarMi(int a, int b, int c)
        {
            foreach (var u in NoktaninUcgenleri(a))
            {
                int p = Ucgenler[u], q = Ucgenler[u + 1], r = Ucgenler[u + 2];
                if ((p == b || q == b || r == b) && (p == c || q == c || r == c))
                    return true;
            }
            return false;
        }

        private HashSet<long> SerbestKenarKumesi()
        {
            var kume = new HashSet<long>();
            for (var i = 0; i + 1 < SerbestKenarlar.Count; i += 2)
                kume.Add(KenarAnahtari(SerbestKenarlar[i], SerbestKenarlar[i + 1]));
            return kume;
        }

        // Bir düzenleme bittiğinde: üçgen kenarına dönüşmüş serbest çizgiler düşülür, çizim önbelleği geçersiz kılınır.
        private void Degisti()
        {
            for (var i = SerbestKenarlar.Count - 2; i >= 0; i -= 2)
            {
                if (_kenarSayaci.ContainsKey(KenarAnahtari(SerbestKenarlar[i], SerbestKenarlar[i + 1])))
                    SerbestKenarlar.RemoveRange(i, 2);
            }
            _surum++;
        }

        // ------------------------------------------------------------------ çizim ve seçim

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            if (panelGenislik <= 0 || panelYukseklik <= 0)
                return;

            // Önizleme kopyaları tek bir kez çizilip atılır; önbellek ancak aynı obje ikinci kez çizildiğinde kurulur.
            if (!_birKezCizildi)
            {
                _birKezCizildi = true;
                Cizdir(grafik, gorunum, panelGenislik, panelYukseklik, null, null, null);
                return;
            }

            var gecerli = _onbellek != null && _onbellek.Width == panelGenislik && _onbellek.Height == panelYukseklik
                          && _obSurum == _surum && _obSecili == Secili && _obRenk == Renk && _obEgriSurum == EsYukseklikAyarlari.Surum
                          && _obOlcek == gorunum.Olcek && _obOfsetX == gorunum.OfsetX && _obOfsetY == gorunum.OfsetY;
            if (!gecerli)
            {
                if (_onbellek == null || _onbellek.Width != panelGenislik || _onbellek.Height != panelYukseklik)
                {
                    _onbellek?.Dispose();
                    _onbellek = new Bitmap(panelGenislik, panelYukseklik, PixelFormat.Format32bppPArgb);
                }

                var pikseller = new List<int>();
                var minorPikseller = new List<int>();
                var majorPikseller = new List<int>();
                using (var g = Graphics.FromImage(_onbellek))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = grafik.SmoothingMode;
                    Cizdir(g, gorunum, panelGenislik, panelYukseklik, pikseller, minorPikseller, majorPikseller);
                }

                // Eğriler üçgen kenarlarının, ana eğriler ara eğrilerin üzerinde görünsün diye bu sırayla yazılır.
                PikselleriYaz(pikseller, (Secili ? Color.DodgerBlue : Renk).ToArgb());
                PikselleriYaz(minorPikseller, Color.FromArgb(255, EsYukseklikAyarlari.MinorRenk).ToArgb());
                PikselleriYaz(majorPikseller, Color.FromArgb(255, EsYukseklikAyarlari.MajorRenk).ToArgb());

                _obSurum = _surum;
                _obSecili = Secili;
                _obRenk = Renk;
                _obEgriSurum = EsYukseklikAyarlari.Surum;
                _obOlcek = gorunum.Olcek;
                _obOfsetX = gorunum.OfsetX;
                _obOfsetY = gorunum.OfsetY;
            }

            grafik.DrawImageUnscaled(_onbellek, 0, 0);
        }

        // Kısa çizgiler çizgi yerine piksel olarak, toplu halde yazılır (uzaktan bakışta hızlı çizim).
        private void PikselleriYaz(List<int> pikseller, int argb)
        {
            if (pikseller.Count == 0)
                return;

            var alan = new Rectangle(0, 0, _onbellek.Width, _onbellek.Height);
            var veri = _onbellek.LockBits(alan, ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
            try
            {
                var satirPikseli = veri.Stride / 4;
                var tampon = new int[satirPikseli * veri.Height];
                Marshal.Copy(veri.Scan0, tampon, 0, tampon.Length);
                var genislik = _onbellek.Width;
                foreach (var piksel in pikseller)
                    tampon[piksel / genislik * satirPikseli + piksel % genislik] = argb;
                Marshal.Copy(tampon, 0, veri.Scan0, tampon.Length);
            }
            finally
            {
                _onbellek.UnlockBits(veri);
            }
        }

        private void Cizdir(Graphics grafik, CizimGorunum gorunum, int genislik, int yukseklik,
            List<int> pikseller, List<int> minorPikseller, List<int> majorPikseller)
        {
            IndeksleriHazirla();

            var n = X.Count;
            if (_ekranX == null || _ekranX.Length < n)
            {
                _ekranX = new float[n];
                _ekranY = new float[n];
            }
            double olcek = gorunum.Olcek, ofsetX = gorunum.OfsetX, ofsetY = gorunum.OfsetY, yariG = genislik / 2.0, yariY = yukseklik / 2.0;
            for (var i = 0; i < n; i++)
            {
                _ekranX[i] = (float)(yariG + (X[i] - ofsetX) * olcek);
                _ekranY[i] = (float)(yariY - (Y[i] - ofsetY) * olcek);
            }

            var oncekiYumusatma = grafik.SmoothingMode;
            if (_kenarSayaci.Count > YumusatmaKenarSiniri)
                grafik.SmoothingMode = SmoothingMode.None;

            var renk = Secili ? Color.DodgerBlue : Renk;
            using (var kalem = new Pen(renk, Secili ? 1.5f : 1f))
            {
                foreach (var anahtar in _kenarSayaci.Keys)
                {
                    int a = (int)(anahtar >> 32), b = (int)anahtar;
                    ParcaCiz(grafik, kalem, _ekranX[a], _ekranY[a], _ekranX[b], _ekranY[b], genislik, yukseklik, pikseller);
                }
                for (var i = 0; i + 1 < SerbestKenarlar.Count; i += 2)
                {
                    int a = SerbestKenarlar[i], b = SerbestKenarlar[i + 1];
                    ParcaCiz(grafik, kalem, _ekranX[a], _ekranY[a], _ekranX[b], _ekranY[b], genislik, yukseklik, null);
                }
            }

            EgrileriCiz(grafik, genislik, yukseklik, minorPikseller, majorPikseller);

            var kenarYok = _kenarSayaci.Count == 0 && SerbestKenarlar.Count == 0;
            if (kenarYok || Secili)
            {
                var gorunenler = new List<int>();
                for (var i = 0; i < n && (kenarYok || gorunenler.Count <= NoktaIsaretiSiniri); i++)
                {
                    if (Nolar[i] != null && _ekranX[i] >= 0 && _ekranX[i] <= genislik && _ekranY[i] >= 0 && _ekranY[i] <= yukseklik)
                        gorunenler.Add(i);
                }

                if (kenarYok || gorunenler.Count <= NoktaIsaretiSiniri)
                {
                    using (var firca = new SolidBrush(renk))
                    {
                        foreach (var i in gorunenler)
                            grafik.FillRectangle(firca, _ekranX[i] - 2f, _ekranY[i] - 2f, 4f, 4f);
                    }
                }
            }

            grafik.SmoothingMode = oncekiYumusatma;
        }

        // Ekran koordinatları verilen doğru parçasını çizer. pikseller verilmişse birkaç pikselden kısa parçalar GDI+
        // çizgisi yerine doğrudan piksel olarak yazılır: uzaktan bakışta yüz binlerce minik parçanın tek tek çizgi
        // olarak çizilmesi çok yavaştır.
        private static void ParcaCiz(Graphics grafik, Pen kalem, float ax, float ay, float bx, float by, int genislik, int yukseklik, List<int> pikseller)
        {
            // Tamamen ekran dışında kalan parçalar çizilmez.
            if ((ax < 0 && bx < 0) || (ax > genislik && bx > genislik) || (ay < 0 && by < 0) || (ay > yukseklik && by > yukseklik))
                return;

            if (pikseller != null)
            {
                float dx = bx - ax, dy = by - ay;
                var adim = (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)));
                if (adim <= KisaKenarPikseli)
                {
                    for (var s = 0; s <= adim; s++)
                    {
                        var oran = adim == 0 ? 0f : (float)s / adim;
                        int px = (int)(ax + dx * oran), py = (int)(ay + dy * oran);
                        if (px >= 0 && px < genislik && py >= 0 && py < yukseklik)
                            pikseller.Add(py * genislik + px);
                    }
                    return;
                }
            }

            grafik.DrawLine(kalem, ax, ay, bx, by);
        }

        // Eş yükseklik eğrilerini çizer: her üçgenin içinden geçen eğri parçası, eğri kotunun üçgenin iki kenarını
        // kestiği noktalar arasındaki doğrudur. Eğriler her çizimde üçgenlerden yeniden üretildiği için yüzey
        // düzenlendiğinde değişen üçgenlerin eğrileri de kendiliğinden düzelir.
        private void EgrileriCiz(Graphics grafik, int genislik, int yukseklik, List<int> minorPikseller, List<int> majorPikseller)
        {
            var majorAralik = EsYukseklikAyarlari.MajorGoster && EsYukseklikAyarlari.MajorAralik > 0 ? EsYukseklikAyarlari.MajorAralik : 0;
            var minorAralik = EsYukseklikAyarlari.MinorGoster && EsYukseklikAyarlari.MinorAralik > 0 ? EsYukseklikAyarlari.MinorAralik : 0;
            if (majorAralik <= 0 && minorAralik <= 0)
                return;

            using (var majorKalem = new Pen(EsYukseklikAyarlari.MajorRenk, 1.5f))
            using (var minorKalem = new Pen(EsYukseklikAyarlari.MinorRenk, 1f))
            {
                for (var i = 0; i + 2 < Ucgenler.Count; i += 3)
                {
                    var a = Ucgenler[i];
                    if (a < 0)
                        continue;
                    int b = Ucgenler[i + 1], c = Ucgenler[i + 2];

                    float ax = _ekranX[a], bx = _ekranX[b], cx = _ekranX[c];
                    if ((ax < 0 && bx < 0 && cx < 0) || (ax > genislik && bx > genislik && cx > genislik))
                        continue;
                    float ay = _ekranY[a], by = _ekranY[b], cy = _ekranY[c];
                    if ((ay < 0 && by < 0 && cy < 0) || (ay > yukseklik && by > yukseklik && cy > yukseklik))
                        continue;

                    double za = Z[a], zb = Z[b], zc = Z[c];
                    var enDusuk = Math.Min(za, Math.Min(zb, zc));
                    var enYuksek = Math.Max(za, Math.Max(zb, zc));
                    if (enYuksek <= enDusuk)
                        continue;

                    // Ara eğriler önce çizilir; ana eğri kotuna denk gelen ara eğri atlanır (orada ana eğri çizilir).
                    if (minorAralik > 0)
                        UcgeninEgrileriniCiz(grafik, minorKalem, minorAralik, majorAralik, ax, ay, za, bx, by, zb, cx, cy, zc, enDusuk, enYuksek, genislik, yukseklik, minorPikseller);
                    if (majorAralik > 0)
                        UcgeninEgrileriniCiz(grafik, majorKalem, majorAralik, 0, ax, ay, za, bx, by, zb, cx, cy, zc, enDusuk, enYuksek, genislik, yukseklik, majorPikseller);
                }
            }
        }

        private static void UcgeninEgrileriniCiz(Graphics grafik, Pen kalem, double aralik, double atlanacakAralik,
            float ax, float ay, double za, float bx, float by, double zb, float cx, float cy, double zc,
            double enDusuk, double enYuksek, int genislik, int yukseklik, List<int> pikseller)
        {
            // Üçgenin en düşük kotundan büyük, en yüksek kotuna eşit ya da küçük olan eğri kotları üçgeni keser.
            var ilk = (long)Math.Floor(enDusuk / aralik) + 1;
            var son = (long)Math.Floor(enYuksek / aralik);

            // Hatalı kotlu (ör. kotu 0 girilmiş) noktalar bir üçgende binlerce eğri üretmesin.
            if (son - ilk > UcgenBasinaEgriSiniri)
                return;

            for (var k = ilk; k <= son; k++)
            {
                var kot = k * aralik;
                if (atlanacakAralik > 0)
                {
                    var oran = kot / atlanacakAralik;
                    if (Math.Abs(oran - Math.Round(oran)) < 1e-9)
                        continue;
                }

                // Bir köşe eğri kotunun altında, diğeri altında değilse eğri o kenarı keser; bu kuralla eğri her
                // üçgende ya hiç ya da tam iki kenarı keser.
                float x1 = 0, y1 = 0, x2 = 0, y2 = 0;
                var kesisim = 0;
                if ((za < kot) != (zb < kot))
                {
                    var t = (float)((kot - za) / (zb - za));
                    x1 = ax + (bx - ax) * t;
                    y1 = ay + (by - ay) * t;
                    kesisim++;
                }
                if ((zb < kot) != (zc < kot))
                {
                    var t = (float)((kot - zb) / (zc - zb));
                    if (kesisim == 0) { x1 = bx + (cx - bx) * t; y1 = by + (cy - by) * t; }
                    else { x2 = bx + (cx - bx) * t; y2 = by + (cy - by) * t; }
                    kesisim++;
                }
                if (kesisim < 2 && (zc < kot) != (za < kot))
                {
                    var t = (float)((kot - zc) / (za - zc));
                    if (kesisim == 0) { x1 = cx + (ax - cx) * t; y1 = cy + (ay - cy) * t; }
                    else { x2 = cx + (ax - cx) * t; y2 = cy + (ay - cy) * t; }
                    kesisim++;
                }

                if (kesisim == 2)
                    ParcaCiz(grafik, kalem, x1, y1, x2, y2, genislik, yukseklik, pikseller);
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            IndeksleriHazirla();
            if (_kenarSayaci.Count == 0 && SerbestKenarlar.Count == 0)
                return EnYakinNokta(dunyaNoktasi.X, dunyaNoktasi.Y, toleransDunya) >= 0;
            return EnYakinKenar(dunyaNoktasi.X, dunyaNoktasi.Y, toleransDunya, out _, out _);
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            for (var i = 0; i < X.Count; i++)
            {
                if (Nolar[i] != null)
                    yield return new PointF((float)X[i], (float)Y[i]);
            }
        }

        // ------------------------------------------------------------------ sorgular

        /// <summary>Verilen konuma tolerans içindeki en yakın noktanın indeksi; yoksa -1.</summary>
        public int EnYakinNokta(double x, double y, double tolerans)
        {
            var enYakin = -1;
            var enYakinKare = tolerans * tolerans;
            for (var i = 0; i < X.Count; i++)
            {
                var dx = X[i] - x;
                if (dx > tolerans || dx < -tolerans)
                    continue;
                var dy = Y[i] - y;
                var kare = dx * dx + dy * dy;
                if (kare > enYakinKare || Nolar[i] == null)
                    continue;
                enYakinKare = kare;
                enYakin = i;
            }
            return enYakin;
        }

        /// <summary>Verilen konuma tolerans içindeki en yakın çizgiyi (üçgen kenarı ya da serbest çizgi) bulur.</summary>
        public bool EnYakinKenar(double x, double y, double tolerans, out int a, out int b)
        {
            IndeksleriHazirla();
            a = b = -1;
            var enYakinKare = tolerans * tolerans;
            foreach (var anahtar in _kenarSayaci.Keys)
                KenarDene((int)(anahtar >> 32), (int)anahtar, x, y, tolerans, ref enYakinKare, ref a, ref b);
            for (var i = 0; i + 1 < SerbestKenarlar.Count; i += 2)
                KenarDene(SerbestKenarlar[i], SerbestKenarlar[i + 1], x, y, tolerans, ref enYakinKare, ref a, ref b);
            return a >= 0;
        }

        private void KenarDene(int p, int q, double px, double py, double tolerans, ref double enYakinKare, ref int a, ref int b)
        {
            double x1 = X[p], x2 = X[q];
            if ((px < x1 - tolerans && px < x2 - tolerans) || (px > x1 + tolerans && px > x2 + tolerans))
                return;
            double y1 = Y[p], y2 = Y[q];
            if ((py < y1 - tolerans && py < y2 - tolerans) || (py > y1 + tolerans && py > y2 + tolerans))
                return;

            double dx = x2 - x1, dy = y2 - y1;
            var uzunlukKare = dx * dx + dy * dy;
            var t = uzunlukKare < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / uzunlukKare));
            double ex = x1 + t * dx - px, ey = y1 + t * dy - py;
            var kare = ex * ex + ey * ey;
            if (kare > enYakinKare)
                return;
            enYakinKare = kare;
            a = p;
            b = q;
        }

        private double Yon(int a, int b, double x, double y)
        {
            return (X[b] - X[a]) * (y - Y[a]) - (Y[b] - Y[a]) * (x - X[a]);
        }

        /// <summary>Verilen konumu içeren üçgenin Ucgenler listesindeki başlangıç indeksi; yoksa -1.</summary>
        public int UcgenBul(double x, double y)
        {
            for (var i = 0; i + 2 < Ucgenler.Count; i += 3)
            {
                var a = Ucgenler[i];
                if (a < 0)
                    continue;
                int b = Ucgenler[i + 1], c = Ucgenler[i + 2];

                // Çoğu üçgen bu hızlı sınır kutusu denetiminde elenir.
                double ax = X[a], bx = X[b], cx = X[c];
                if ((x < ax && x < bx && x < cx) || (x > ax && x > bx && x > cx))
                    continue;
                double ay = Y[a], by = Y[b], cy = Y[c];
                if ((y < ay && y < by && y < cy) || (y > ay && y > by && y > cy))
                    continue;

                double d1 = (bx - ax) * (y - ay) - (by - ay) * (x - ax);
                double d2 = (cx - bx) * (y - by) - (cy - by) * (x - bx);
                double d3 = (ax - cx) * (y - cy) - (ay - cy) * (x - cx);
                var negatif = d1 < 0 || d2 < 0 || d3 < 0;
                var pozitif = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(negatif && pozitif))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Verilen konumdaki yüzey kotu: konum bir üçgenin içindeyse üçgen düzleminden, değilse en yakın noktanın
        /// kotundan (hiç nokta yoksa 0) hesaplanır.
        /// </summary>
        public double KotHesapla(double x, double y)
        {
            var u = UcgenBul(x, y);
            if (u >= 0)
            {
                int a = Ucgenler[u], b = Ucgenler[u + 1], c = Ucgenler[u + 2];
                var alan = Yon(a, b, X[c], Y[c]);
                if (Math.Abs(alan) > 1e-12)
                {
                    var wa = Yon(b, c, x, y) / alan;
                    var wb = Yon(c, a, x, y) / alan;
                    return wa * Z[a] + wb * Z[b] + (1 - wa - wb) * Z[c];
                }
            }

            var enYakin = -1;
            var enYakinKare = double.MaxValue;
            for (var i = 0; i < X.Count; i++)
            {
                if (Nolar[i] == null)
                    continue;
                double dx = X[i] - x, dy = Y[i] - y;
                var kare = dx * dx + dy * dy;
                if (kare >= enYakinKare)
                    continue;
                enYakinKare = kare;
                enYakin = i;
            }
            return enYakin >= 0 ? Z[enYakin] : 0;
        }

        /// <summary>Yeni nokta için önerilen numara: sayısal nokta numaralarının en büyüğünün bir fazlası.</summary>
        public string SonrakiNoktaNo()
        {
            if (_enBuyukNo < 0)
            {
                _enBuyukNo = 0;
                foreach (var no in Nolar)
                    EnBuyukNoyuGuncelle(no);
            }
            return (_enBuyukNo + 1).ToString(CultureInfo.InvariantCulture);
        }

        private void EnBuyukNoyuGuncelle(string no)
        {
            if (_enBuyukNo >= 0 && no != null && long.TryParse(no, NumberStyles.None, CultureInfo.InvariantCulture, out var sayi) && sayi > _enBuyukNo)
                _enBuyukNo = sayi;
        }

        // ------------------------------------------------------------------ geri alma

        [Browsable(false)]
        public bool GeriAlinabilir => _geriAlListesi.Count > 0;

        // Her düzenlemenin başında çağrılır: paylaşılan listeleri bu objeye özel kopyalar, geri alma kaydı alır ve
        // indeksleri hazırlar.
        private void DuzenlemeyeBasla()
        {
            if (_paylasimli)
            {
                Nolar = new List<string>(Nolar);
                Ucgenler = new List<int>(Ucgenler);
                SerbestKenarlar = new List<int>(SerbestKenarlar);
                _bosUcgenler = new List<int>(_bosUcgenler);
                _kenarSayaci = null;
                _noktaUcgenleri = null;
                _paylasimli = false;
            }

            _geriAlListesi.Add(new Anlik
            {
                Nolar = new List<string>(Nolar),
                X = new List<double>(X),
                Y = new List<double>(Y),
                Z = new List<double>(Z),
                Ucgenler = new List<int>(Ucgenler),
                SerbestKenarlar = new List<int>(SerbestKenarlar),
                BosUcgenler = new List<int>(_bosUcgenler),
                CanliNokta = _canliNokta,
                CanliUcgen = _canliUcgen
            });

            // Büyük yüzeylerde bellek dolmasın diye tutulan kayıt sayısı yüzeyin büyüklüğüne göre azalır.
            var kayitBoyutu = Math.Max(1L, 4L * X.Count + Ucgenler.Count);
            var sinir = (int)Math.Max(3, Math.Min(GeriAlSiniri, GeriAlElemanButcesi / kayitBoyutu));
            while (_geriAlListesi.Count > sinir)
                _geriAlListesi.RemoveAt(0);

            IndeksleriHazirla();
        }

        /// <summary>Son yüzey düzenlemesini geri alır.</summary>
        public void GeriAl()
        {
            if (_geriAlListesi.Count == 0)
                return;

            var anlik = _geriAlListesi[_geriAlListesi.Count - 1];
            _geriAlListesi.RemoveAt(_geriAlListesi.Count - 1);
            Nolar = anlik.Nolar;
            X = anlik.X;
            Y = anlik.Y;
            Z = anlik.Z;
            Ucgenler = anlik.Ucgenler;
            SerbestKenarlar = anlik.SerbestKenarlar;
            _bosUcgenler = anlik.BosUcgenler;
            _canliNokta = anlik.CanliNokta;
            _canliUcgen = anlik.CanliUcgen;
            _kenarSayaci = null;
            _noktaUcgenleri = null;
            _enBuyukNo = -1;
            _surum++;
        }

        // ------------------------------------------------------------------ düzenleme yardımcıları

        // İki doğru parçası uç noktaları dışında birbirini kesiyor mu?
        private bool Kesisiyor(int a, int b, int c, int d)
        {
            if (a == c || a == d || b == c || b == d)
                return false;
            double d1 = Yon(a, b, X[c], Y[c]), d2 = Yon(a, b, X[d], Y[d]);
            if (!((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)))
                return false;
            double d3 = Yon(c, d, X[a], Y[a]), d4 = Yon(c, d, X[b], Y[b]);
            return (d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0);
        }

        private double UzunlukKare(int a, int b)
        {
            double dx = X[a] - X[b], dy = Y[a] - Y[b];
            return dx * dx + dy * dy;
        }

        // c köşesi a-b doğru parçasının (uçları hariç) tam üzerinde mi?
        private bool DogruUzerindeMi(int a, int b, int c)
        {
            var uzunlukKare = UzunlukKare(a, b);
            var t = ((X[c] - X[a]) * (X[b] - X[a]) + (Y[c] - Y[a]) * (Y[b] - Y[a])) / uzunlukKare;
            if (t <= 0 || t >= 1)
                return false;
            var sapma = Yon(a, b, X[c], Y[c]);
            return sapma * sapma <= uzunlukKare * 1e-12;
        }

        /// <summary>
        /// Köşeleri sırayla verilen çokgeni (boşluğu) üçgenlere böler ("kulak kesme"): her adımda, içinde başka köşe
        /// bulunmayan dışbükey köşelerden en düzgün üçgeni vereni keser. yasakA-yasakB köşegeni üçgen kenarı olarak
        /// kullanılmaz. yasakKose verilirse o köşe çokgenin sınırını belirler ama hiçbir üçgende kullanılmaz.
        /// Çokgen tamamen bölünemiyorsa bölünebilen kısmı döner; kalan kısım boşluk olarak kalır.
        /// </summary>
        private List<int> CokgeniUcgenle(List<int> cokgen, int yasakA = -1, int yasakB = -1, int yasakKose = -1)
        {
            var sonuc = new List<int>();
            var p = new List<int>();
            foreach (var kose in cokgen)
            {
                if (p.Count == 0 || p[p.Count - 1] != kose)
                    p.Add(kose);
            }
            if (p.Count > 1 && p[0] == p[p.Count - 1])
                p.RemoveAt(p.Count - 1);
            if (p.Count < 3)
                return sonuc;

            // Köşeler saat yönünün tersine dönecek şekilde sıralanır.
            double alan = 0;
            for (var i = 0; i < p.Count; i++)
            {
                var j = (i + 1) % p.Count;
                alan += (X[p[i]] - X[p[0]]) * (Y[p[j]] - Y[p[0]]) - (X[p[j]] - X[p[0]]) * (Y[p[i]] - Y[p[0]]);
            }
            if (alan < 0)
                p.Reverse();

            while (p.Count >= 3)
            {
                var n = p.Count;
                var enIyi = -1;
                double enIyiOlcut = -1;
                for (var i = 0; i < n; i++)
                {
                    int o = p[(i - 1 + n) % n], c = p[i], s = p[(i + 1) % n];
                    if (o == s || o == yasakKose || c == yasakKose || s == yasakKose)
                        continue;

                    var alan2 = Yon(o, c, X[s], Y[s]);
                    if (alan2 <= 1e-12)
                        continue; // içbükey ya da doğrusal köşe kulak olamaz

                    if (n > 3 && ((o == yasakA && s == yasakB) || (o == yasakB && s == yasakA)))
                        continue;

                    var icindeKoseVar = false;
                    foreach (var q in p)
                    {
                        if (q == o || q == c || q == s)
                            continue;
                        if (Yon(o, c, X[q], Y[q]) >= 0 && Yon(c, s, X[q], Y[q]) >= 0 && Yon(s, o, X[q], Y[q]) >= 0)
                        {
                            icindeKoseVar = true;
                            break;
                        }
                    }
                    if (icindeKoseVar)
                        continue;

                    // İnce uzun üçgenlerden kaçınmak için alanı en uzun kenarına göre en büyük olan kulak seçilir.
                    var olcut = alan2 / Math.Max(UzunlukKare(o, c), Math.Max(UzunlukKare(c, s), UzunlukKare(s, o)));
                    if (olcut > enIyiOlcut)
                    {
                        enIyiOlcut = olcut;
                        enIyi = i;
                    }
                }

                if (enIyi < 0)
                    break;

                sonuc.Add(p[(enIyi - 1 + n) % n]);
                sonuc.Add(p[enIyi]);
                sonuc.Add(p[(enIyi + 1) % n]);
                p.RemoveAt(enIyi);
            }
            return sonuc;
        }

        private void UcgenleriEkle(List<int> koseler)
        {
            for (var i = 0; i + 2 < koseler.Count; i += 3)
                UcgenEkle(koseler[i], koseler[i + 1], koseler[i + 2]);
        }

        // Üçgenleri siler; başka üçgene ait olmayan kenarlarını (haricKenarlar ve haricNokta'ya değenler hariç)
        // serbest çizgi olarak bırakır.
        private void UcgenleriSil(IEnumerable<int> ucgenler, HashSet<long> haricKenarlar, int haricNokta = -1)
        {
            var adaylar = new List<int>();
            foreach (var u in ucgenler.ToList())
            {
                for (var k = 0; k < 3; k++)
                {
                    adaylar.Add(Ucgenler[u + k]);
                    adaylar.Add(Ucgenler[u + (k + 1) % 3]);
                }
                UcgenSil(u);
            }

            var serbestler = SerbestKenarKumesi();
            for (var i = 0; i + 1 < adaylar.Count; i += 2)
            {
                int p = adaylar[i], q = adaylar[i + 1];
                var anahtar = KenarAnahtari(p, q);
                if ((haricKenarlar != null && haricKenarlar.Contains(anahtar)) || p == haricNokta || q == haricNokta
                    || _kenarSayaci.ContainsKey(anahtar) || !serbestler.Add(anahtar))
                    continue;
                SerbestKenarlar.Add(p);
                SerbestKenarlar.Add(q);
            }
        }

        // ------------------------------------------------------------------ çizgi işlemleri

        /// <summary>
        /// İki köşe arasına çizgi ekler.
        /// - Çizgi mevcut çizgileri kesiyorsa kestiği üçgenler silinir ve oluşan boşluk, yeni çizgi kenar olacak
        ///   şekilde çevresindeki köşelerle yeniden üçgenlenir.
        /// - Kesmiyorsa, iki ucu da ortak bir komşuya bağlıysa o komşuyla üçgen oluşturur; oluşturmuyorsa serbest
        ///   çizgi olarak kalır ve sonradan eklenen çizgilerle üçgene tamamlanabilir.
        /// </summary>
        public bool CizgiEkle(int a, int b, out string hata)
        {
            hata = null;
            if (a == b)
                return false;

            IndeksleriHazirla();
            var abAnahtari = KenarAnahtari(a, b);
            if (_kenarSayaci.ContainsKey(abAnahtari) || SerbestKenarKumesi().Contains(abAnahtari))
            {
                hata = "Bu iki nokta arasında zaten bir çizgi var.";
                return false;
            }

            var kesilenKenarlar = new HashSet<long>();
            if (!KesilenKenarlariYuruyerekBul(a, b, kesilenKenarlar, out var uzerindekiKose))
            {
                if (uzerindekiKose >= 0)
                {
                    // Çizgi başka bir köşenin tam üzerinden geçiyorsa üçgenler yozlaşır.
                    hata = "Çizgi başka bir köşenin (nokta " + Nolar[uzerindekiKose] + ") üzerinden geçiyor. Çizgiyi o köşeye kadar çizin.";
                    return false;
                }

                // Yürüyüş b'ye ulaşamadı (çizgi yüzeyin dışından ya da bir boşluktan geçiyor): bütün kenarlar taranır.
                kesilenKenarlar.Clear();
                foreach (var anahtar in _kenarSayaci.Keys)
                {
                    if (Kesisiyor(a, b, (int)(anahtar >> 32), (int)anahtar))
                        kesilenKenarlar.Add(anahtar);
                }
            }

            DuzenlemeyeBasla();

            // Yeni çizginin kestiği serbest çizgiler kalkar.
            for (var i = SerbestKenarlar.Count - 2; i >= 0; i -= 2)
            {
                if (Kesisiyor(a, b, SerbestKenarlar[i], SerbestKenarlar[i + 1]))
                    SerbestKenarlar.RemoveRange(i, 2);
            }

            var bosluklarDoldu = false;
            if (kesilenKenarlar.Count > 0)
            {
                // Kesilen bir kenarı olan üçgenler silinir; silinen bölgenin sınırı (yalnızca bir silinen üçgene ait,
                // kesilmemiş kenarlar) çizginin iki yanında a'dan b'ye birer köşe zinciri oluşturur.
                var silinecekler = new HashSet<int>();
                foreach (var anahtar in kesilenKenarlar)
                {
                    foreach (var u in KenarinUcgenleri((int)(anahtar >> 32), (int)anahtar))
                        silinecekler.Add(u);
                }

                var sinir = new Dictionary<long, int[]>();
                foreach (var u in silinecekler)
                {
                    for (var k = 0; k < 3; k++)
                    {
                        int p = Ucgenler[u + k], q = Ucgenler[u + (k + 1) % 3];
                        var anahtar = KenarAnahtari(p, q);
                        if (kesilenKenarlar.Contains(anahtar))
                            continue;
                        if (!sinir.Remove(anahtar))
                            sinir[anahtar] = new[] { p, q };
                    }
                }

                UcgenleriSil(silinecekler, kesilenKenarlar);

                var solZincir = SinirZinciriBul(a, b, sinir.Values, 1);
                var sagZincir = SinirZinciriBul(a, b, sinir.Values, -1);
                if (solZincir != null && sagZincir != null)
                {
                    UcgenleriEkle(CokgeniUcgenle(solZincir));
                    UcgenleriEkle(CokgeniUcgenle(sagZincir));
                    bosluklarDoldu = true;
                }
            }

            if (!bosluklarDoldu)
                OrtakKomsuylaUcgenle(a, b);

            // Çizgi bir üçgenin kenarı olamadıysa serbest çizgi olarak kalır.
            if (!_kenarSayaci.ContainsKey(abAnahtari))
            {
                SerbestKenarlar.Add(a);
                SerbestKenarlar.Add(b);
            }
            Degisti();
            return true;
        }

        // a'dan b'ye üçgenden üçgene yürüyerek çizginin kestiği kenarları bulur (yalnızca yol üzerindeki üçgenlere
        // bakar). Çizgi hiçbir kenarı kesmiyorsa ya da yürüyüş b'ye ulaştıysa true döner. Çizgi bir köşenin üzerinden
        // geçiyorsa (uzerindekiKose >= 0) ya da yürüyüş yüzeyin kenarına/boşluğa takıldıysa false döner.
        private bool KesilenKenarlariYuruyerekBul(int a, int b, HashSet<long> kesilenKenarlar, out int uzerindekiKose)
        {
            uzerindekiKose = -1;

            int simdiki = -1, p = -1, q = -1;
            foreach (var u in NoktaninUcgenleri(a))
            {
                var kose = new[] { Ucgenler[u], Ucgenler[u + 1], Ucgenler[u + 2] };
                var aYeri = Array.IndexOf(kose, a);
                int k1 = kose[(aYeri + 1) % 3], k2 = kose[(aYeri + 2) % 3];
                if (k1 == b || k2 == b)
                    continue;

                foreach (var k in new[] { k1, k2 })
                {
                    if (!DogruUzerindeMi(a, b, k))
                        continue;
                    uzerindekiKose = k;
                    return false;
                }

                if (!Kesisiyor(a, b, k1, k2))
                    continue;
                simdiki = u;
                p = k1;
                q = k2;
                break;
            }

            if (simdiki < 0)
            {
                // a'nın çevresindeki hiçbir üçgenden geçmiyor: a'nın üçgeni yoksa ya da çizgi yüzeyin dışına doğru
                // gidiyorsa uzakta kesişme olabilir; bunu çağıran taraf tam taramayla denetler.
                return false;
            }

            for (var adim = 0; adim <= _canliUcgen; adim++)
            {
                kesilenKenarlar.Add(KenarAnahtari(p, q));

                var komsu = -1;
                foreach (var u in KenarinUcgenleri(p, q))
                {
                    if (u != simdiki)
                        komsu = u;
                }
                if (komsu < 0)
                    return false; // yüzeyin kenarına ya da bir boşluğa gelindi

                var r = KarsiKose(komsu, p, q);
                if (r == b)
                    return true;
                if (DogruUzerindeMi(a, b, r))
                {
                    uzerindekiKose = r;
                    return false;
                }

                if (Kesisiyor(a, b, p, r))
                    q = r;
                else if (Kesisiyor(a, b, q, r))
                    p = r;
                else
                    return false;
                simdiki = komsu;
            }
            return false;
        }

        // Silinen bölgenin sınır kenarlarından, a→b çizgisinin verilen yanında (1: sol, -1: sağ) kalan köşeleri a'dan
        // b'ye sırayla izler. Zincir kopuksa (çizgi yüzeyin dışından ya da bir boşluktan geçiyorsa) null döner.
        private List<int> SinirZinciriBul(int a, int b, IEnumerable<int[]> sinirKenarlari, int taraf)
        {
            bool Uygun(int p)
            {
                return p == a || p == b || Math.Sign(Yon(a, b, X[p], Y[p])) == taraf;
            }

            var komsular = new Dictionary<int, List<int>>();
            foreach (var kenar in sinirKenarlari)
            {
                int p = kenar[0], q = kenar[1];
                if (!Uygun(p) || !Uygun(q))
                    continue;
                if (!komsular.TryGetValue(p, out var pListe)) komsular[p] = pListe = new List<int>();
                if (!komsular.TryGetValue(q, out var qListe)) komsular[q] = qListe = new List<int>();
                pListe.Add(q);
                qListe.Add(p);
            }

            var zincir = new List<int> { a };
            int onceki = -1, simdiki = a;
            for (var adim = 0; adim <= komsular.Count + 1; adim++)
            {
                if (!komsular.TryGetValue(simdiki, out var liste))
                    return null;

                var sonraki = -1;
                foreach (var komsu in liste)
                {
                    if (komsu == onceki || (komsu != b && zincir.Contains(komsu)))
                        continue;
                    sonraki = komsu;
                    if (komsu == b)
                        break;
                }
                if (sonraki < 0)
                    return null;

                zincir.Add(sonraki);
                if (sonraki == b)
                    return zincir.Count >= 3 ? zincir : null;
                onceki = simdiki;
                simdiki = sonraki;
            }
            return null;
        }

        // v'ye bir çizgiyle (üçgen kenarı ya da serbest çizgi) bağlı köşeler.
        private HashSet<int> Komsular(int v)
        {
            var komsular = new HashSet<int>();
            foreach (var u in NoktaninUcgenleri(v))
            {
                komsular.Add(Ucgenler[u]);
                komsular.Add(Ucgenler[u + 1]);
                komsular.Add(Ucgenler[u + 2]);
            }
            for (var i = 0; i + 1 < SerbestKenarlar.Count; i += 2)
            {
                if (SerbestKenarlar[i] == v) komsular.Add(SerbestKenarlar[i + 1]);
                if (SerbestKenarlar[i + 1] == v) komsular.Add(SerbestKenarlar[i]);
            }
            komsular.Remove(v);
            return komsular;
        }

        // a-b çizgisinin iki ucuna da çizgiyle bağlı köşelerle üçgen kurar: çizginin her iki yanında, çizgiyi en geniş
        // açıyla gören (en yakın) ortak komşu seçilir.
        private void OrtakKomsuylaUcgenle(int a, int b)
        {
            var bKomsulari = Komsular(b);
            int solAday = -1, sagAday = -1;
            double solAci = -1, sagAci = -1;
            foreach (var c in Komsular(a))
            {
                if (c == b || !bKomsulari.Contains(c))
                    continue;

                var yon = Yon(a, b, X[c], Y[c]);
                if (Math.Abs(yon) < 1e-12)
                    continue;

                double ux = X[a] - X[c], uy = Y[a] - Y[c], vx = X[b] - X[c], vy = Y[b] - Y[c];
                var aci = Math.Acos(Math.Max(-1, Math.Min(1, (ux * vx + uy * vy) / Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy)))));
                if (yon > 0 && aci > solAci) { solAci = aci; solAday = c; }
                if (yon < 0 && aci > sagAci) { sagAci = aci; sagAday = c; }
            }

            foreach (var c in new[] { solAday, sagAday })
            {
                if (c >= 0 && !UcgenVarMi(a, b, c))
                    UcgenEkle(a, b, c);
            }
        }

        /// <summary>
        /// Çizgiyi siler. Serbest çizgiyse yalnızca kendisi silinir. İki üçgenin ortak kenarıysa iki üçgen silinir ve
        /// oluşan dörtgen boşluk, çevresindeki köşelerle (diğer köşegenle) yeniden üçgenlenir; dörtgen içbükey olduğu
        /// için doldurulamıyorsa boşluk kalır. Dış kenarsa yalnızca o kenarın üçgeni silinir.
        /// </summary>
        public bool CizgiSil(int a, int b)
        {
            IndeksleriHazirla();
            var abAnahtari = KenarAnahtari(a, b);
            for (var i = 0; i + 1 < SerbestKenarlar.Count; i += 2)
            {
                if (KenarAnahtari(SerbestKenarlar[i], SerbestKenarlar[i + 1]) != abAnahtari)
                    continue;
                DuzenlemeyeBasla();
                SerbestKenarlar.RemoveRange(i, 2);
                Degisti();
                return true;
            }

            if (KenarinUcgenleri(a, b).Count == 0)
                return false;

            DuzenlemeyeBasla();
            var silinecekler = KenarinUcgenleri(a, b);
            var karsiKoseler = silinecekler.Select(u => KarsiKose(u, a, b)).ToList();
            UcgenleriSil(silinecekler, new HashSet<long> { abAnahtari });

            if (karsiKoseler.Count == 2)
            {
                // Boşluğun köşeleri sırayla: a, çizginin sağındaki köşe, b, solundaki köşe.
                var sag = Yon(a, b, X[karsiKoseler[0]], Y[karsiKoseler[0]]) < 0 ? karsiKoseler[0] : karsiKoseler[1];
                var sol = sag == karsiKoseler[0] ? karsiKoseler[1] : karsiKoseler[0];
                UcgenleriEkle(CokgeniUcgenle(new List<int> { a, sag, b, sol }, a, b));
            }

            Degisti();
            return true;
        }

        /// <summary>
        /// İki üçgenin ortak kenarını çevirir: iki üçgenin oluşturduğu dörtgenin diğer köşegeni kenar olur. Kenar iki
        /// üçgene ait değilse ya da dörtgen dışbükey değilse çevrilemez.
        /// </summary>
        public bool UcgenDondur(int a, int b, out string hata)
        {
            hata = null;
            IndeksleriHazirla();
            var bulunanlar = KenarinUcgenleri(a, b);
            if (bulunanlar.Count != 2)
            {
                hata = "Bu çizgi iki üçgenin ortak kenarı değil; döndürülemez.";
                return false;
            }

            int c = KarsiKose(bulunanlar[0], a, b), d = KarsiKose(bulunanlar[1], a, b);
            if (!Kesisiyor(a, b, c, d))
            {
                hata = "İki üçgenin oluşturduğu dörtgen dışbükey değil; bu çizgi döndürülemez.";
                return false;
            }

            DuzenlemeyeBasla();
            foreach (var u in KenarinUcgenleri(a, b))
                UcgenSil(u);
            UcgenEkle(c, d, a);
            UcgenEkle(c, d, b);
            Degisti();
            return true;
        }

        // ------------------------------------------------------------------ nokta işlemleri

        /// <summary>
        /// Nokta ekler. Nokta bir üçgenin içindeyse üçgen üçe, bir üçgen kenarının üzerindeyse o kenarı paylaşan
        /// üçgenler ikişere bölünür; hiçbir üçgenin içinde değilse nokta tek başına eklenir.
        /// </summary>
        public int NoktaEkle(string no, double x, double y, double z)
        {
            DuzenlemeyeBasla();

            var yeni = X.Count;
            Nolar.Add(no);
            X.Add(x);
            Y.Add(y);
            Z.Add(z);
            _noktaUcgenleri.Add(null);
            _canliNokta++;
            EnBuyukNoyuGuncelle(no);

            NoktayiUcgenlereBagla(yeni, false);
            Degisti();
            return yeni;
        }

        // Hiçbir üçgene bağlı olmayan v noktasını bulunduğu konumdaki üçgen(ler)i bölerek yüzeye bağlar. Nokta hiçbir
        // üçgenin içinde değilse ve disaridaysaBagla verilmişse, kendisinden görünen dış kenarlarla üçgen kurar.
        private void NoktayiUcgenlereBagla(int v, bool disaridaysaBagla)
        {
            double x = X[v], y = Y[v];
            var u = UcgenBul(x, y);
            if (u < 0)
            {
                if (disaridaysaBagla)
                    GorunenDisKenarlaraBagla(v);
                return;
            }

            int a = Ucgenler[u], b = Ucgenler[u + 1], c = Ucgenler[u + 2];
            var alan = Math.Abs(Yon(a, b, X[c], Y[c]));
            var kenarEsigi = alan * 1e-6;

            // Noktanın üzerine düştüğü kenar (varsa): o kenarı paylaşan bütün üçgenler ikiye bölünür.
            int k1 = -1, k2 = -1;
            if (Math.Abs(Yon(a, b, x, y)) <= kenarEsigi) { k1 = a; k2 = b; }
            else if (Math.Abs(Yon(b, c, x, y)) <= kenarEsigi) { k1 = b; k2 = c; }
            else if (Math.Abs(Yon(c, a, x, y)) <= kenarEsigi) { k1 = c; k2 = a; }

            if (k1 < 0)
            {
                UcgenSil(u);
                UcgenEkle(a, b, v);
                UcgenEkle(b, c, v);
                UcgenEkle(c, a, v);
                return;
            }

            foreach (var bolunecek in KenarinUcgenleri(k1, k2))
            {
                var karsi = KarsiKose(bolunecek, k1, k2);
                UcgenSil(bolunecek);
                UcgenEkle(k1, v, karsi);
                UcgenEkle(v, k2, karsi);
            }
        }

        // Yüzeyin dışındaki (ya da bir boşluğun içindeki) v noktasını, arada başka dış kenar kalmadan görebildiği dış
        // kenarların her biriyle üçgen kurarak yüzeye bağlar.
        private void GorunenDisKenarlaraBagla(int v)
        {
            // Dış kenar: yalnızca bir üçgene ait kenar.
            var disKenarlar = new List<int[]>();
            foreach (var kayit in _kenarSayaci)
            {
                if (kayit.Value == 1)
                    disKenarlar.Add(new[] { (int)(kayit.Key >> 32), (int)kayit.Key });
            }

            var eklenecek = new List<int>();
            foreach (var kenar in disKenarlar)
            {
                int p = kenar[0], q = kenar[1];
                if (p == v || q == v)
                    continue;

                // Nokta, kenarın üçgeninin bulunduğu yanın karşısında (dış yanda) olmalı.
                var ucgenleri = KenarinUcgenleri(p, q);
                if (ucgenleri.Count != 1)
                    continue;
                var r = KarsiKose(ucgenleri[0], p, q);
                var noktaAlani = Yon(p, q, X[v], Y[v]);
                // Kenarla neredeyse aynı doğrultudaki nokta yozlaşmış (alansız) üçgen verir.
                if (noktaAlani * noktaAlani <= UzunlukKare(p, q) * 1e-12 || Math.Sign(noktaAlani) == Math.Sign(Yon(p, q, X[r], Y[r])))
                    continue;

                var gorunuyor = true;
                foreach (var diger in disKenarlar)
                {
                    if (Kesisiyor(v, p, diger[0], diger[1]) || Kesisiyor(v, q, diger[0], diger[1]))
                    {
                        gorunuyor = false;
                        break;
                    }
                }
                if (gorunuyor)
                    eklenecek.AddRange(new[] { p, q, v });
            }

            // Bir kenar en fazla iki üçgene ait olabilir: v'yi aynı köşeye üçüncü kez bağlayacak üçgen eklenmez
            // (dış sınırın tek bir köşede kendine değdiği yerlerde olur).
            for (var i = 0; i + 2 < eklenecek.Count; i += 3)
            {
                int p = eklenecek[i], q = eklenecek[i + 1];
                _kenarSayaci.TryGetValue(KenarAnahtari(p, v), out var pSayi);
                _kenarSayaci.TryGetValue(KenarAnahtari(q, v), out var qSayi);
                if (pSayi < 2 && qSayi < 2)
                    UcgenEkle(p, q, v);
            }
        }

        /// <summary>
        /// Noktayı, ona bağlı üçgenleri ve serbest çizgileri siler; silinen üçgenlerin bıraktığı boşluk, boşluğun
        /// çevresindeki köşelerden kurulan üçgenlerle kapatılır.
        /// </summary>
        public void NoktaSil(int indeks)
        {
            DuzenlemeyeBasla();
            YildiziSilVeDoldur(indeks);

            for (var i = SerbestKenarlar.Count - 2; i >= 0; i -= 2)
            {
                if (SerbestKenarlar[i] == indeks || SerbestKenarlar[i + 1] == indeks)
                    SerbestKenarlar.RemoveRange(i, 2);
            }

            // Nokta listelerden çıkarılmaz (diğer noktaların indeksi kaymasın diye), silinmiş olarak işaretlenir.
            Nolar[indeks] = null;
            _canliNokta--;
            _enBuyukNo = -1;
            Degisti();
        }

        /// <summary>
        /// Noktanın numarasını ve koordinatlarını değiştirir. Yeni konum noktayı çevresindeki üçgenlerin dışına
        /// çıkarıyorsa yüzey NoktaTasi'daki gibi yeniden üçgenlenir.
        /// </summary>
        public void NoktaDuzelt(int indeks, string no, double x, double y, double z)
        {
            DuzenlemeyeBasla();
            Nolar[indeks] = no;
            Z[indeks] = z;
            _enBuyukNo = -1;
            NoktayiYenidenKonumla(indeks, x, y);
            Degisti();
        }

        /// <summary>
        /// Noktayı yatayda yeni konumuna taşır (kotu aynı kalır). Nokta çevresindeki üçgenlerin içinde kalıyorsa
        /// üçgenler onunla birlikte esner. Dışına çıkıyorsa (üçgenler üst üste binecekse) noktanın eski üçgenleri
        /// silinir, bıraktığı boşluk çevredeki köşelerle doldurulur ve nokta yeni konumunda yüzeye yeniden bağlanır.
        /// </summary>
        public void NoktaTasi(int indeks, double x, double y)
        {
            DuzenlemeyeBasla();
            NoktayiYenidenKonumla(indeks, x, y);
            Degisti();
        }

        private void NoktayiYenidenKonumla(int v, double x, double y)
        {
            var yildiz = NoktaninUcgenleri(v).ToList();

            double eskiX = X[v], eskiY = Y[v];
            var eskiYonler = yildiz.Select(i => Math.Sign(Yon(Ucgenler[i], Ucgenler[i + 1], X[Ucgenler[i + 2]], Y[Ucgenler[i + 2]]))).ToList();
            X[v] = x;
            Y[v] = y;

            // Hiçbir üçgen ters dönmüyor ve yozlaşmıyorsa nokta çevresindeki üçgenlerin içinde kalmıştır.
            var bindirmeVar = false;
            for (var k = 0; k < yildiz.Count && !bindirmeVar; k++)
            {
                var i = yildiz[k];
                var yon = Math.Sign(Yon(Ucgenler[i], Ucgenler[i + 1], X[Ucgenler[i + 2]], Y[Ucgenler[i + 2]]));
                bindirmeVar = yon == 0 || yon != eskiYonler[k];
            }
            if (!bindirmeVar)
                return;

            // Eski konumuna göre noktanın üçgenleri silinir ve bıraktığı boşluk çevredeki köşelerle doldurulur.
            X[v] = eskiX;
            Y[v] = eskiY;
            YildiziSilVeDoldur(v);

            X[v] = x;
            Y[v] = y;
            NoktayiUcgenlereBagla(v, true);
        }

        // v noktasının bütün üçgenlerini siler ve oluşan boşluğu, boşluğun çevresindeki köşelerden üçgenler kurarak
        // doldurur. Doldurulamayan kısım varsa (çevresi içbükey olup bölünemeyen) boşluk olarak kalır.
        private void YildiziSilVeDoldur(int v)
        {
            var yildiz = NoktaninUcgenleri(v).ToList();
            if (yildiz.Count == 0)
                return;

            // Çevredeki köşeler, noktanın etrafında saat yönünün tersine sıralanır (p→q kenarları).
            var sonraki = new Dictionary<int, int>();
            var hedefler = new HashSet<int>();
            foreach (var i in yildiz)
            {
                var kose = new[] { Ucgenler[i], Ucgenler[i + 1], Ucgenler[i + 2] };
                var vYeri = Array.IndexOf(kose, v);
                int p = kose[(vYeri + 1) % 3], q = kose[(vYeri + 2) % 3];
                if (Yon(v, p, X[q], Y[q]) < 0)
                {
                    var gecici = p;
                    p = q;
                    q = gecici;
                }
                sonraki[p] = q;
                hedefler.Add(q);
            }

            // Nokta yüzeyin kenarındaysa köşeler açık bir zincir oluşturur; zincir, kendisine gelen kenarı olmayan köşeden başlar.
            var baslangic = sonraki.Keys.Where(k => !hedefler.Contains(k)).DefaultIfEmpty(sonraki.Keys.First()).First();
            var cevre = new List<int> { baslangic };
            var simdiki = baslangic;
            while (sonraki.TryGetValue(simdiki, out var siradaki) && siradaki != baslangic && cevre.Count <= sonraki.Count)
            {
                cevre.Add(siradaki);
                simdiki = siradaki;
            }

            // Doldurulamayan kısmın kenarları kaybolmasın diye, başka üçgene ait olmayan çevre kenarları serbest çizgi kalır.
            UcgenleriSil(yildiz, null, v);

            // Nokta yüzeyin içindeyse çevresi kapalı bir çokgendir ve tamamı doldurulur. Yüzeyin kenarındaysa çevre
            // açık bir zincirdir: zincirin iki ucunu doğrudan birleştirmek yüzeyin dışına taşıp başka üçgenlerin
            // üzerine binebilir. Bu yüzden nokta çokgenin sınırı olarak tutulur ama üçgenlere girmez; yalnızca eski
            // üçgenlerin kapladığı alanın içinde kalan kısım doldurulur.
            var kapali = hedefler.Contains(baslangic);
            if (kapali)
            {
                UcgenleriEkle(CokgeniUcgenle(cevre));
            }
            else
            {
                var sinirliCevre = new List<int> { v };
                sinirliCevre.AddRange(cevre);
                UcgenleriEkle(CokgeniUcgenle(sinirliCevre, -1, -1, v));
            }
        }

        /// <summary>
        /// Pencere içindeki çizgileri toplu siler: seçilen çizgilerden birini kenar olarak kullanan bütün üçgenler
        /// silinir (boşluk doldurulmaz). kesenlerDahil verilirse pencereye değen çizgiler, verilmezse yalnızca iki ucu
        /// da pencerenin içinde olan çizgiler seçilir. Silinen çizgi sayısını döner.
        /// </summary>
        public int CizgileriSil(double minX, double minY, double maxX, double maxY, bool kesenlerDahil)
        {
            IndeksleriHazirla();

            bool Secildi(int p, int q)
            {
                double x1 = X[p], y1 = Y[p], x2 = X[q], y2 = Y[q];
                var pIcinde = x1 >= minX && x1 <= maxX && y1 >= minY && y1 <= maxY;
                var qIcinde = x2 >= minX && x2 <= maxX && y2 >= minY && y2 <= maxY;
                if (pIcinde && qIcinde)
                    return true;
                if (!kesenlerDahil)
                    return false;
                if (pIcinde || qIcinde)
                    return true;

                // İki ucu da dışarıda: pencerenin kenarlarından birini kesiyorsa pencereye değiyordur.
                if ((x1 < minX && x2 < minX) || (x1 > maxX && x2 > maxX) || (y1 < minY && y2 < minY) || (y1 > maxY && y2 > maxY))
                    return false;
                return ParcalarKesisiyor(x1, y1, x2, y2, minX, minY, maxX, minY) || ParcalarKesisiyor(x1, y1, x2, y2, maxX, minY, maxX, maxY)
                    || ParcalarKesisiyor(x1, y1, x2, y2, maxX, maxY, minX, maxY) || ParcalarKesisiyor(x1, y1, x2, y2, minX, maxY, minX, minY);
            }

            var secilenler = new HashSet<long>();
            foreach (var anahtar in _kenarSayaci.Keys)
            {
                if (Secildi((int)(anahtar >> 32), (int)anahtar))
                    secilenler.Add(anahtar);
            }

            var secilenSerbestler = new List<int>();
            for (var i = 0; i + 1 < SerbestKenarlar.Count; i += 2)
            {
                if (Secildi(SerbestKenarlar[i], SerbestKenarlar[i + 1]))
                    secilenSerbestler.Add(i);
            }

            if (secilenler.Count == 0 && secilenSerbestler.Count == 0)
                return 0;

            DuzenlemeyeBasla();
            for (var k = secilenSerbestler.Count - 1; k >= 0; k--)
                SerbestKenarlar.RemoveRange(secilenSerbestler[k], 2);

            var silinecekler = new HashSet<int>();
            foreach (var anahtar in secilenler)
            {
                foreach (var u in KenarinUcgenleri((int)(anahtar >> 32), (int)anahtar))
                    silinecekler.Add(u);
            }
            UcgenleriSil(silinecekler, secilenler);
            Degisti();
            return secilenler.Count + secilenSerbestler.Count;
        }

        private static bool ParcalarKesisiyor(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            double d1 = (x2 - x1) * (y3 - y1) - (y2 - y1) * (x3 - x1), d2 = (x2 - x1) * (y4 - y1) - (y2 - y1) * (x4 - x1);
            double d3 = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3), d4 = (x4 - x3) * (y2 - y3) - (y4 - y3) * (x2 - x3);
            return ((d1 >= 0 && d2 <= 0) || (d1 <= 0 && d2 >= 0)) && ((d3 >= 0 && d4 <= 0) || (d3 <= 0 && d4 >= 0));
        }
    }
}
