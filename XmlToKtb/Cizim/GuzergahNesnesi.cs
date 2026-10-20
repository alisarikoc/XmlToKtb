using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace XmlToKtb.Cizim
{
    public enum GuzergahParcaTipi
    {
        Dogru,
        Kurp,
        Spiral
    }

    /// <summary>
    /// Güzergahı oluşturan parçalardan biri (doğru, kurp ya da spiral). Parça yalnızca uzunluğu, yarıçap(lar)ı ve
    /// dönüş yönüyle tanımlanır; konumu ve doğrultusu kendinden önceki parçanın bittiği yerden hesaplanır. Bu yüzden
    /// bir parça değiştiğinde ondan sonrakiler teğetliği bozulmadan kendiliğinden yer değiştirir.
    /// </summary>
    public class GuzergahParcasi
    {
        public GuzergahParcaTipi Tip;
        public double Uzunluk;
        /// <summary>Kurpta yarıçap; spiralde başlangıç yarıçapı (sonsuz = doğruya bağlanan uç). Doğruda kullanılmaz.</summary>
        public double YaricapBaslangic = double.PositiveInfinity;
        /// <summary>Spiralde bitiş yarıçapı (sonsuz = doğruya bağlanan uç). Kurpta YaricapBaslangic ile aynıdır.</summary>
        public double YaricapBitis = double.PositiveInfinity;
        public DonusYonu Yon = DonusYonu.Sag;
        /// <summary>
        /// Parçanın başındaki kırıklık: önceki parçanın bittiği doğrultudan bu kadar (radyan; sola artı) sapılarak
        /// başlanır. Kurpsuz some noktalarında (iki doğrunun köşe yaparak birleştiği yerlerde) sıfırdan farklıdır.
        /// </summary>
        public double KirikAcisi;
        // --- Güzergah yeniden hesaplandığında doldurulan değerler ---
        public double BaslangicKm, BitisKm;
        public double BaslangicX, BaslangicY, BitisX, BitisY;
        /// <summary>Parçanın başındaki ve sonundaki gidiş doğrultusu: +X'ten saat yönünün tersine, radyan.</summary>
        public double BaslangicAcisi, BitisAcisi;
        /// <summary>Çizim ve seçim için parça boyunca örneklenmiş noktalar.</summary>
        public double[] OrnekX = new double[0], OrnekY = new double[0];

        /// <summary>
        /// Dosyadan okunan parçanın başlangıç noktası ve doğrultusu dosyadaki değerlere sabitlenir: koordinatları
        /// yuvarlanmış dosyalarda (ör. Leica çıktıları) parçaları uç uca hesaplamak kilometrelerce sonra santimetrelerce
        /// kayma biriktirir. Sabit parça öncekinin bittiği yerden değil, kendi sabit noktasından başlar. Güzergah
        /// düzenlendiğinde değişen parçadan sonrakilerin sabiti kaldırılır ve onlar yeniden uç uca hesaplanır.
        /// </summary>
        public bool Sabit;
        public double SabitX, SabitY, SabitAci;

        /// <summary>
        /// Kopuk parçalar açıklık kapanmadan (elle) güzergaha bağlandıysa zincirin ilk parçası: sabiti hiç bırakılmaz,
        /// böylece önceki parçalar düzenlense de kopuk kısmın yeri ve someleri değişmez.
        /// </summary>
        public bool AraBaglanti;

        /// <summary>Parçanın tanım değerlerini kopyalar; sabitleriyle verilirse dosyadaki sabit başlangıcını da taşır.</summary>
        public GuzergahParcasi Kopyala(bool sabitleriyle = false)
        {
            var kopya = new GuzergahParcasi { Tip = Tip, Uzunluk = Uzunluk, YaricapBaslangic = YaricapBaslangic, YaricapBitis = YaricapBitis, Yon = Yon, KirikAcisi = KirikAcisi };
            if (sabitleriyle && Sabit)
            {
                kopya.Sabit = true;
                kopya.SabitX = SabitX;
                kopya.SabitY = SabitY;
                kopya.SabitAci = SabitAci;
                kopya.AraBaglanti = AraBaglanti;
            }
            return kopya;
        }

        // Verilen yarıçapın eğriliği: sola dönüş artı, sağa dönüş eksi; sonsuz yarıçap (doğru) sıfır.
        private double Egrilik(double yaricap)
        {
            if (Tip == GuzergahParcaTipi.Dogru || double.IsInfinity(yaricap) || yaricap <= 0)
                return 0;
            return (Yon == DonusYonu.Sol ? 1.0 : -1.0) / yaricap;
        }

        public double BaslangicEgriligi => Egrilik(YaricapBaslangic);
        public double BitisEgriligi => Egrilik(Tip == GuzergahParcaTipi.Kurp ? YaricapBaslangic : YaricapBitis);
    }

    /// <summary>
    /// Güzergah (yol/demiryolu ekseni): başlangıç noktası ve doğrultusundan başlayıp art arda dizilen doğru, kurp ve
    /// spiral parçalarından oluşan tek bir çizim objesi.
    /// </summary>
    public partial class GuzergahNesnesi : CizimNesnesi, ITutamacli
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Güzergah";

        [DisplayName("Ad")]
        public string Ad { get; set; } = "";

        [DisplayName("Başlangıç Km")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BaslangicKm { get; set; }

        [DisplayName("Bitiş Km")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BitisKm => BaslangicKm + ToplamUzunluk;

        [DisplayName("Uzunluk")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double ToplamUzunluk => Parcalar.Sum(p => p.Uzunluk);

        [DisplayName("Parça Sayısı")]
        public int ParcaSayisi => Parcalar.Count;

        /// <summary>Güzergahın başladığı nokta (çizim koordinatı: X sağa, Y yukarı).</summary>
        [Browsable(false)]
        public double BaslangicX { get; set; }

        [Browsable(false)]
        public double BaslangicY { get; set; }

        /// <summary>Başlangıçtaki gidiş doğrultusu: +X'ten saat yönünün tersine, radyan.</summary>
        [Browsable(false)]
        public double BaslangicAcisi { get; set; }

        [Browsable(false)]
        public List<GuzergahParcasi> Parcalar { get; } = new List<GuzergahParcasi>();

        /// <summary>
        /// Kopuk parçalar: aradan parça silinince güzergahtan kopan, silinmeden önceki yerlerinde (sabit başlangıçlarıyla)
        /// duran parçalar. Yalnızca görüntüdür; kilometre almazlar, güzergahın hesaplarında, çıktılarında ve some modelinde
        /// yer almazlar. Güzergahın sonu ilk kopuk parçanın başına ulaşınca (ya da Kopuklari Bagla ile) yeniden güzergaha
        /// katılır ve kilometre onlarla devam eder.
        /// </summary>
        [Browsable(false)]
        public List<GuzergahParcasi> KopukParcalar { get; } = new List<GuzergahParcasi>();

        /// <summary>Güzergah ve kopuk parçaların hepsi (konum dönüşümleri, yakalama, patlatma için).</summary>
        [Browsable(false)]
        public IEnumerable<GuzergahParcasi> TumParcalar => Parcalar.Concat(KopukParcalar);

        [DisplayName("Kopuk Parça Sayısı")]
        public int KopukParcaSayisi => KopukParcalar.Count;

        /// <summary>Güzergah sonu ile ilk kopuk parçanın başı arasındaki açıklık (kopuk parça yoksa 0).</summary>
        [Browsable(false)]
        public double KopuklukAcikligi
        {
            get
            {
                if (KopukParcalar.Count == 0)
                    return 0;
                var k = KopukParcalar[0];
                double sx = Parcalar.Count > 0 ? Parcalar[Parcalar.Count - 1].BitisX : BaslangicX;
                double sy = Parcalar.Count > 0 ? Parcalar[Parcalar.Count - 1].BitisY : BaslangicY;
                return Math.Sqrt((k.BaslangicX - sx) * (k.BaslangicX - sx) + (k.BaslangicY - sy) * (k.BaslangicY - sy));
            }
        }

        /// <summary>Kopuk parçalarda arayı birleşmiş sayan uzaklık.</summary>
        public const double KopuklukToleransi = 1e-3;

        /// <summary>
        /// Parçayı güzergahtan siler; arkasında kalan parçalar koordinatları değişmeden kopuk parça olur (kilometre
        /// almazlar). İlk parça silinirse geri kalanlar yerinde kalır ve güzergah ikinci parçanın başından, onun
        /// kilometresiyle başlar.
        /// </summary>
        public void ParcaSilKopukBirak(IEnumerable<int> siralar)
        {
            var silinecek = new HashSet<int>(siralar.Where(i => i >= 0 && i < Parcalar.Count));
            if (silinecek.Count == 0)
                return;

            // Baştaki silinen parçalar: güzergah ilk kalan parçanın başından, onun kilometresiyle başlar.
            var bastan = 0;
            while (bastan < Parcalar.Count && silinecek.Contains(bastan))
                bastan++;
            if (bastan > 0 && bastan < Parcalar.Count)
            {
                var ilk = Parcalar[bastan];
                BaslangicX = ilk.BaslangicX;
                BaslangicY = ilk.BaslangicY;
                BaslangicAcisi = ilk.BaslangicAcisi;
                BaslangicKm = ilk.BaslangicKm;
            }

            // Bağlı kalan kısım: baştaki silinenlerden sonra, ilk aradaki silinen parçaya kadar.
            var kopuktan = bastan;
            while (kopuktan < Parcalar.Count && !silinecek.Contains(kopuktan))
                kopuktan++;

            var yeniKopuklar = new List<GuzergahParcasi>();
            for (var i = kopuktan; i < Parcalar.Count; i++)
            {
                if (silinecek.Contains(i))
                    continue;
                yeniKopuklar.Add(SabitKopya(Parcalar[i], true));
            }
            var kalan = Parcalar.Skip(bastan).Take(kopuktan - bastan).ToList();
            if (kalan.Count > 0 && bastan > 0)
                kalan[0] = SabitKopya(kalan[0], false);
            Parcalar.Clear();
            Parcalar.AddRange(kalan);
            KopukParcalar.InsertRange(0, yeniKopuklar);
            Hesapla();
        }

        // Parçanın şimdiki başlangıcına sabitlenmiş kopyası: yeniden hesaplanınca da aynı yerde durur. Kırıklık
        // korunursa (kopuk zincirin içindeki kurpsuz someler) sabit sonradan bırakılsa da doğrultular değişmez.
        private static GuzergahParcasi SabitKopya(GuzergahParcasi parca, bool kirikKoru)
        {
            var kopya = parca.Kopyala();
            if (!kirikKoru)
                kopya.KirikAcisi = 0;
            kopya.Sabit = true;
            kopya.SabitX = parca.BaslangicX;
            kopya.SabitY = parca.BaslangicY;
            kopya.SabitAci = parca.BaslangicAcisi;
            return kopya;
        }

        /// <summary>
        /// Kopuk parçaları (birbirine uç uca bağlı ilk zinciri) güzergaha katar: yerlerinde kalırlar, kilometre
        /// güzergahın sonundan devam eder. Katılan parça sayısını verir.
        /// </summary>
        public int KopuklariBagla()
        {
            if (KopukParcalar.Count == 0)
                return 0;
            // İlk kopuk parça ve ona uç uca bağlı olanlar bir zincirdir.
            var adet = 1;
            while (adet < KopukParcalar.Count)
            {
                GuzergahParcasi onceki = KopukParcalar[adet - 1], sonraki = KopukParcalar[adet];
                if (Math.Abs(onceki.BitisX - sonraki.BaslangicX) > KopuklukToleransi || Math.Abs(onceki.BitisY - sonraki.BaslangicY) > KopuklukToleransi)
                    break;
                adet++;
            }
            var araKapali = KopuklukAcikligi <= KopuklukToleransi;
            var zincir = KopukParcalar.Take(adet).ToList();
            KopukParcalar.RemoveRange(0, adet);
            if (Parcalar.Count == 0)
            {
                BaslangicX = zincir[0].BaslangicX;
                BaslangicY = zincir[0].BaslangicY;
                BaslangicAcisi = zincir[0].BaslangicAcisi;
                zincir[0].KirikAcisi = 0;
            }
            else
            {
                // Bağlanma yerindeki doğrultu farkı kırıklık olarak saklanır: parçaların sabiti sonradan bırakılıp uç
                // uca hesaplansalar da kopuk parçaların doğrultuları ve someleri değişmez.
                zincir[0].KirikAcisi = AciyiSar(zincir[0].BaslangicAcisi - Parcalar[Parcalar.Count - 1].BitisAcisi);
                // Ara kapanmışsa parçalar artık güzergahın devamıdır: sabitleri bırakılır (geometri aynı kalır) ki some
                // modeli bağlanma yerindeki kırıklığı ve kopuk kısmın somelerini görsün.
                if (araKapali)
                {
                    foreach (var parca in zincir)
                        parca.Sabit = false;
                }
                else
                {
                    zincir[0].AraBaglanti = true;
                }
            }
            Parcalar.AddRange(zincir);
            Hesapla();
            return adet;
        }

        /// <summary>Kopuk parçaların hepsini siler.</summary>
        public void KopuklariSil()
        {
            KopukParcalar.Clear();
        }

        /// <summary>Düşey geometri: kilometreye göre sıralı some noktaları. Boşsa güzergahın düşey geometrisi yoktur.</summary>
        [Browsable(false)]
        public List<DuseyNokta> Dusey { get; } = new List<DuseyNokta>();

        /// <summary>Dever (superelevation) tablosu. Satırı yoksa güzergahta dever tanımlı değildir.</summary>
        [Browsable(false)]
        public DeverTablosu Dever { get; set; } = new DeverTablosu();

        [DisplayName("Düşey Some Sayısı")]
        public int DuseySomeSayisi => Dusey.Count;

        [DisplayName("Dever İstasyonu Sayısı")]
        public int DeverIstasyonuSayisi => Dever.Satirlar.Count;

        /// <summary>Editörde seçili olan parçanın sırası; ekranda farklı renkte çizilir (-1: yok).</summary>
        [Browsable(false)]
        public int VurguluParca { get; set; } = -1;

        public GuzergahNesnesi Kopyala()
        {
            var kopya = new GuzergahNesnesi { Ad = Ad, BaslangicKm = BaslangicKm, BaslangicX = BaslangicX, BaslangicY = BaslangicY, BaslangicAcisi = BaslangicAcisi };
            kopya.Parcalar.AddRange(Parcalar.Select(p => p.Kopyala(true)));
            kopya.KopukParcalar.AddRange(KopukParcalar.Select(p => p.Kopyala(true)));
            kopya.Dusey.AddRange(Dusey.Select(n => n.Kopyala()));
            kopya.Dever = Dever.Kopyala();
            kopya.Hesapla();
            return kopya;
        }

        /// <summary>Başka bir güzergahın (ör. geri al kaydındaki kopyanın) bütün tanımını bu güzergaha alır.</summary>
        public void DurumunuYukle(GuzergahNesnesi kaynak)
        {
            Ad = kaynak.Ad;
            BaslangicKm = kaynak.BaslangicKm;
            BaslangicX = kaynak.BaslangicX;
            BaslangicY = kaynak.BaslangicY;
            BaslangicAcisi = kaynak.BaslangicAcisi;
            Parcalar.Clear();
            Parcalar.AddRange(kaynak.Parcalar.Select(p => p.Kopyala(true)));
            KopukParcalar.Clear();
            KopukParcalar.AddRange(kaynak.KopukParcalar.Select(p => p.Kopyala(true)));
            Dusey.Clear();
            Dusey.AddRange(kaynak.Dusey.Select(n => n.Kopyala()));
            Dever = kaynak.Dever.Kopyala();
            Hesapla();
        }

        /// <summary>Verilen sıradan itibaren parçaların dosyaya sabitlenmiş başlangıçlarını kaldırır; o parçalar uç uca hesaplanır.</summary>
        public void SabitleriBirak(int ilkSira)
        {
            // Kopuk parçalar (KopukParcalar) bu listede değildir; sabitleri hep korunur.
            for (var i = Math.Max(0, ilkSira); i < Parcalar.Count; i++)
            {
                // Açıklıkla bağlanmış kopuk zincirin başı yerinde kalır.
                if (!Parcalar[i].AraBaglanti)
                    Parcalar[i].Sabit = false;
            }
        }

        /// <summary>
        /// Parçaların konumlarını başlangıç noktasından itibaren sırayla yeniden hesaplar. Parçalar ya da başlangıç
        /// değerleri değiştikten sonra çağrılmalıdır.
        /// </summary>
        public void Hesapla()
        {
            ZinciriHesapla(Parcalar, BaslangicX, BaslangicY, BaslangicAcisi, BaslangicKm);
            // Kopuk parçalar kendi sabit başlangıçlarından çizilir; kilometreleri yoktur (NaN).
            ZinciriHesapla(KopukParcalar, BaslangicX, BaslangicY, BaslangicAcisi, double.NaN);
            SomeleriGuncelle();

            // Güzergahın sonu ilk kopuk parçanın başına ulaştıysa ara kapanmıştır: kopuklar güzergaha katılır.
            if (KopukParcalar.Count > 0 && Parcalar.Count > 0 && KopuklukAcikligi <= KopuklukToleransi)
                KopuklariBagla();
        }

        private static void ZinciriHesapla(List<GuzergahParcasi> parcalar, double x, double y, double aci, double km)
        {
            foreach (var parca in parcalar)
            {
                if (parca.Sabit)
                {
                    x = parca.SabitX;
                    y = parca.SabitY;
                    aci = parca.SabitAci;
                }
                else
                {
                    aci += parca.KirikAcisi;
                }

                parca.BaslangicKm = km;
                parca.BaslangicX = x;
                parca.BaslangicY = y;
                parca.BaslangicAcisi = aci;

                // Eğrilik parça boyunca başlangıç değerinden bitiş değerine doğrusal değişir: doğruda hep sıfır,
                // kurpta sabit, spiralde değişken. Doğrultu eğriliğin, konum doğrultunun integralidir.
                double k0 = parca.BaslangicEgriligi, k1 = parca.BitisEgriligi, uzunluk = Math.Max(parca.Uzunluk, 0);
                var toplamDonus = Math.Abs(k0 + k1) / 2 * uzunluk;
                var ornekSayisi = parca.Tip == GuzergahParcaTipi.Dogru
                    ? 1
                    : Math.Max(parca.Tip == GuzergahParcaTipi.Spiral ? 16 : 4, Math.Min(720, (int)Math.Ceiling(toplamDonus / (Math.PI / 90))));

                parca.OrnekX = new double[ornekSayisi + 1];
                parca.OrnekY = new double[ornekSayisi + 1];
                parca.OrnekX[0] = x;
                parca.OrnekY[0] = y;

                double DogrultuAl(double s)
                {
                    return uzunluk <= 0 ? aci : aci + k0 * s + (k1 - k0) * s * s / (2 * uzunluk);
                }

                const int altAdim = 8; // her örnek aralığı Simpson kuralıyla 8 alt aralıkta integre edilir
                for (var i = 1; i <= ornekSayisi; i++)
                {
                    double s0 = uzunluk * (i - 1) / ornekSayisi, s1 = uzunluk * i / ornekSayisi, h = (s1 - s0) / altAdim;
                    double toplamX = 0, toplamY = 0;
                    for (var j = 0; j <= altAdim; j++)
                    {
                        var agirlik = j == 0 || j == altAdim ? 1 : j % 2 == 1 ? 4 : 2;
                        var dogrultu = DogrultuAl(s0 + j * h);
                        toplamX += agirlik * Math.Cos(dogrultu);
                        toplamY += agirlik * Math.Sin(dogrultu);
                    }
                    x += toplamX * h / 3;
                    y += toplamY * h / 3;
                    parca.OrnekX[i] = x;
                    parca.OrnekY[i] = y;
                }

                aci = DogrultuAl(uzunluk);
                km += uzunluk;
                parca.BitisKm = km;
                parca.BitisX = x;
                parca.BitisY = y;
                parca.BitisAcisi = aci;
            }
        }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            var renk = Secili ? Color.DodgerBlue : CizimRengi(Color.Black);
            using (var kalem = new Pen(renk, Secili ? 2f : 1.5f))
            using (var vurguKalemi = new Pen(Color.OrangeRed, 3f))
            using (var firca = new SolidBrush(renk))
            {
                for (var i = 0; i < Parcalar.Count; i++)
                {
                    var parca = Parcalar[i];
                    if (parca.OrnekX.Length < 2)
                        continue;

                    var ekran = new PointF[parca.OrnekX.Length];
                    for (var k = 0; k < ekran.Length; k++)
                        ekran[k] = gorunum.DunyadanEkrana(parca.OrnekX[k], parca.OrnekY[k], panelGenislik, panelYukseklik);
                    grafik.DrawLines(i == VurguluParca ? vurguKalemi : kalem, ekran);

                    // Seçili güzergahta parçaların birleştiği yerler işaretlenir.
                    if (Secili || VurguluParca >= 0)
                    {
                        grafik.FillRectangle(firca, ekran[0].X - 3, ekran[0].Y - 3, 6, 6);
                        if (i == Parcalar.Count - 1)
                            grafik.FillRectangle(firca, ekran[ekran.Length - 1].X - 3, ekran[ekran.Length - 1].Y - 3, 6, 6);
                    }
                }
            }

            // Kopuk parçalar (yalnızca görüntü, kilometresiz): kesikli ve soluk çizilir.
            if (KopukParcalar.Count > 0)
            {
                var kopukRenk = Color.FromArgb(150, Secili ? Color.DodgerBlue : CizimRengi(Color.Gray));
                using (var kopukKalem = new Pen(kopukRenk, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                using (var kopukFirca = new SolidBrush(kopukRenk))
                {
                    foreach (var parca in KopukParcalar)
                    {
                        if (parca.OrnekX.Length < 2)
                            continue;
                        var ekran = new PointF[parca.OrnekX.Length];
                        for (var k = 0; k < ekran.Length; k++)
                            ekran[k] = gorunum.DunyadanEkrana(parca.OrnekX[k], parca.OrnekY[k], panelGenislik, panelYukseklik);
                        grafik.DrawLines(kopukKalem, ekran);
                        if (Secili || VurguluParca >= 0)
                            grafik.FillRectangle(kopukFirca, ekran[0].X - 3, ekran[0].Y - 3, 6, 6);
                    }
                }
            }

            if (TutamaclarEtkin)
                SomeleriCiz(grafik, gorunum, panelGenislik, panelYukseklik);
        }

        /// <summary>Kilometreyi "1+234.567" biçiminde yazar.</summary>
        public static string KmMetni(double km, int ondalikBasamak = 3)
        {
            var isaret = km < 0 ? "-" : "";
            // Yuvarlama 999.9996 gibi bir değeri "1000.000" yapıp binler hanesini bozmasın diye önce yuvarlanır.
            km = Math.Round(Math.Abs(km), ondalikBasamak);
            var binler = Math.Floor(km / 1000.0);
            var bicim = ondalikBasamak > 0 ? "000." + new string('0', ondalikBasamak) : "000";
            return isaret + binler.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "+" +
                   (km - binler * 1000.0).ToString(bicim, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Verilen kilometredeki noktanın konumunu ve gidiş doğrultusunu (+X'ten saat yönünün tersine, radyan) verir.
        /// Kilometre güzergahın dışındaysa false döner.
        /// </summary>
        public bool KonumAl(double km, out double x, out double y, out double aci)
        {
            x = y = aci = 0;
            foreach (var parca in Parcalar)
            {
                if (km < parca.BaslangicKm - 1e-6 || km > parca.BitisKm + 1e-6)
                    continue;

                double k0 = parca.BaslangicEgriligi, k1 = parca.BitisEgriligi, uzunluk = parca.Uzunluk, a0 = parca.BaslangicAcisi;
                var s = Math.Max(0, Math.Min(uzunluk, km - parca.BaslangicKm));

                double DogrultuAl(double t)
                {
                    return uzunluk <= 0 ? a0 : a0 + k0 * t + (k1 - k0) * t * t / (2 * uzunluk);
                }

                // Parça başından s kadar ilerideki konum, doğrultunun Simpson kuralıyla integralidir.
                const int adim = 64;
                var h = s / adim;
                double toplamX = 0, toplamY = 0;
                for (var j = 0; j <= adim; j++)
                {
                    var agirlik = j == 0 || j == adim ? 1 : j % 2 == 1 ? 4 : 2;
                    var dogrultu = DogrultuAl(j * h);
                    toplamX += agirlik * Math.Cos(dogrultu);
                    toplamY += agirlik * Math.Sin(dogrultu);
                }

                x = parca.BaslangicX + toplamX * h / 3;
                y = parca.BaslangicY + toplamY * h / 3;
                aci = DogrultuAl(s);
                return true;
            }
            return false;
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            // Kopuk parçalar da tıklanınca güzergahı seçer.
            return ParcaBul(dunyaNoktasi.X, dunyaNoktasi.Y, toleransDunya) >= 0 || KopukParcaBul(dunyaNoktasi.X, dunyaNoktasi.Y, toleransDunya) >= 0;
        }

        /// <summary>Verilen konuma tolerans içinde en yakın kopuk parçanın sırası; yoksa -1.</summary>
        public int KopukParcaBul(double x, double y, double tolerans)
        {
            return EnYakinParca(KopukParcalar, x, y, tolerans, null);
        }

        /// <summary>Verilen konuma tolerans içinde en yakın parçanın sırası; yoksa -1.</summary>
        public int ParcaBul(double x, double y, double tolerans, Func<GuzergahParcasi, bool> uygun = null)
        {
            return EnYakinParca(Parcalar, x, y, tolerans, uygun);
        }

        private static int EnYakinParca(List<GuzergahParcasi> parcalar, double x, double y, double tolerans, Func<GuzergahParcasi, bool> uygun)
        {
            var enYakin = -1;
            var enYakinKare = tolerans * tolerans;
            for (var i = 0; i < parcalar.Count; i++)
            {
                var parca = parcalar[i];
                if (uygun != null && !uygun(parca))
                    continue;
                for (var k = 0; k + 1 < parca.OrnekX.Length; k++)
                {
                    double x1 = parca.OrnekX[k], y1 = parca.OrnekY[k], dx = parca.OrnekX[k + 1] - x1, dy = parca.OrnekY[k + 1] - y1;
                    var uzunlukKare = dx * dx + dy * dy;
                    var t = uzunlukKare < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((x - x1) * dx + (y - y1) * dy) / uzunlukKare));
                    double ex = x1 + t * dx - x, ey = y1 + t * dy - y;
                    var kare = ex * ex + ey * ey;
                    if (kare > enYakinKare)
                        continue;
                    enYakinKare = kare;
                    enYakin = i;
                }
            }
            return enYakin;
        }

        /// <summary>Her parçanın (doğru, kurp, spiral) boyca ortası.</summary>
        public override IEnumerable<double[]> OrtaNoktalari()
        {
            foreach (var parca in Parcalar)
            {
                if (KonumAl(parca.BaslangicKm + parca.Uzunluk / 2, out var x, out var y, out _))
                    yield return new[] { x, y };
            }
            // Kopuk parçaların kilometresi yoktur; ortaları parça başından boyca hesaplanır.
            foreach (var parca in KopukParcalar)
            {
                ParcaSonu(parca, parca.Uzunluk / 2, out var x, out var y);
                yield return new[] { x, y };
            }
        }

        /// <summary>
        /// Uç yakalama noktaları (tam koordinatla): güzergah ve kopuk parçaların başları ve bitişleri. Kopuk parçaları
        /// güzergaha bağlarken uçlarına tam yakalanabilsin diye çift hassasiyetle verilir.
        /// </summary>
        public IEnumerable<double[]> YakalamaUclari()
        {
            foreach (var parca in TumParcalar)
            {
                yield return new[] { parca.BaslangicX, parca.BaslangicY };
                yield return new[] { parca.BitisX, parca.BitisY };
            }
        }

        /// <summary>Kurpların (kopuk parçalar dahil) merkezleri.</summary>
        public IEnumerable<double[]> KurpMerkezleri()
        {
            foreach (var parca in TumParcalar)
            {
                if (parca.Tip != GuzergahParcaTipi.Kurp)
                    continue;
                var dikme = parca.BaslangicAcisi + (parca.Yon == DonusYonu.Sol ? Math.PI / 2 : -Math.PI / 2);
                yield return new[] { parca.BaslangicX + parca.YaricapBaslangic * Math.Cos(dikme), parca.BaslangicY + parca.YaricapBaslangic * Math.Sin(dikme) };
            }
        }

        /// <summary>Dik yakalama: referans noktasından güzergahın ya da kopuk parçaların en yakın parçasına inen dikmenin ayağı.</summary>
        public override PointF? DikNoktaAl(PointF referans)
        {
            var dik = DikNoktaTam(referans.X, referans.Y);
            return dik == null ? (PointF?)null : new PointF((float)dik[0], (float)dik[1]);
        }

        public double[] DikNoktaTam(double rx, double ry)
        {
            double[] enIyi = null;
            var enKucuk = double.MaxValue;
            void Aday(double x, double y)
            {
                var d = (x - rx) * (x - rx) + (y - ry) * (y - ry);
                if (d < enKucuk) { enKucuk = d; enIyi = new[] { x, y }; }
            }

            foreach (var parca in TumParcalar)
            {
                if (parca.Tip == GuzergahParcaTipi.Dogru)
                {
                    double c = Math.Cos(parca.BaslangicAcisi), s = Math.Sin(parca.BaslangicAcisi);
                    var t = (rx - parca.BaslangicX) * c + (ry - parca.BaslangicY) * s;
                    if (t >= -1e-9 && t <= parca.Uzunluk + 1e-9)
                        Aday(parca.BaslangicX + t * c, parca.BaslangicY + t * s);
                }
                else if (parca.Tip == GuzergahParcaTipi.Kurp)
                {
                    var sola = parca.Yon == DonusYonu.Sol;
                    var r = parca.YaricapBaslangic;
                    var dikme = parca.BaslangicAcisi + (sola ? Math.PI / 2 : -Math.PI / 2);
                    double mx = parca.BaslangicX + r * Math.Cos(dikme), my = parca.BaslangicY + r * Math.Sin(dikme);
                    var donus = Math.Atan2(ry - my, rx - mx) - Math.Atan2(parca.BaslangicY - my, parca.BaslangicX - mx);
                    if (!sola) donus = -donus;
                    donus %= 2 * Math.PI;
                    if (donus < 0) donus += 2 * Math.PI;
                    if (donus * r <= parca.Uzunluk + 1e-9)
                    {
                        var a = Math.Atan2(ry - my, rx - mx);
                        Aday(mx + r * Math.Cos(a), my + r * Math.Sin(a));
                    }
                }
                else
                {
                    // Spiral: örnek noktaları arasında doğrultuya dik olan yer (yaklaşık, ince örneklemeyle).
                    for (var k = 0; k + 1 < parca.OrnekX.Length; k++)
                    {
                        double x1 = parca.OrnekX[k], y1 = parca.OrnekY[k], dx = parca.OrnekX[k + 1] - x1, dy = parca.OrnekY[k + 1] - y1;
                        var l2 = dx * dx + dy * dy;
                        if (l2 < 1e-12)
                            continue;
                        var t = ((rx - x1) * dx + (ry - y1) * dy) / l2;
                        if (t >= 0 && t <= 1)
                            Aday(x1 + t * dx, y1 + t * dy);
                    }
                }
            }
            return enIyi;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            // Kopuk parçalar da ekrana sığdırma ve pencereyle seçimde hesaba katılır.
            foreach (var parca in KopukParcalar)
            {
                yield return new PointF((float)parca.BaslangicX, (float)parca.BaslangicY);
                if (parca.OrnekX.Length > 2)
                    yield return new PointF((float)parca.OrnekX[parca.OrnekX.Length / 2], (float)parca.OrnekY[parca.OrnekY.Length / 2]);
                yield return new PointF((float)parca.BitisX, (float)parca.BitisY);
            }
            foreach (var parca in Parcalar)
            {
                yield return new PointF((float)parca.BaslangicX, (float)parca.BaslangicY);
                // Kurp ve spirallerin ortası da verilir ki ekrana sığdırma eğrinin taşan kısmını da kapsasın.
                if (parca.OrnekX.Length > 2)
                    yield return new PointF((float)parca.OrnekX[parca.OrnekX.Length / 2], (float)parca.OrnekY[parca.OrnekY.Length / 2]);
            }
            if (Parcalar.Count > 0)
                yield return new PointF((float)Parcalar[Parcalar.Count - 1].BitisX, (float)Parcalar[Parcalar.Count - 1].BitisY);
        }
    }
}
