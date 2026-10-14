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
            kopya.Dusey.AddRange(Dusey.Select(n => n.Kopyala()));
            kopya.Dever = Dever.Kopyala();
            kopya.Hesapla();
            return kopya;
        }

        /// <summary>Verilen sıradan itibaren parçaların dosyaya sabitlenmiş başlangıçlarını kaldırır; o parçalar uç uca hesaplanır.</summary>
        public void SabitleriBirak(int ilkSira)
        {
            for (var i = Math.Max(0, ilkSira); i < Parcalar.Count; i++)
                Parcalar[i].Sabit = false;
        }

        /// <summary>
        /// Parçaların konumlarını başlangıç noktasından itibaren sırayla yeniden hesaplar. Parçalar ya da başlangıç
        /// değerleri değiştikten sonra çağrılmalıdır.
        /// </summary>
        public void Hesapla()
        {
            double x = BaslangicX, y = BaslangicY, aci = BaslangicAcisi, km = BaslangicKm;
            foreach (var parca in Parcalar)
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

            SomeleriGuncelle();
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
            return ParcaBul(dunyaNoktasi.X, dunyaNoktasi.Y, toleransDunya) >= 0;
        }

        /// <summary>Verilen konuma tolerans içinde en yakın parçanın sırası; yoksa -1.</summary>
        public int ParcaBul(double x, double y, double tolerans)
        {
            var enYakin = -1;
            var enYakinKare = tolerans * tolerans;
            for (var i = 0; i < Parcalar.Count; i++)
            {
                var parca = Parcalar[i];
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

        public override IEnumerable<PointF> UcNoktalariAl()
        {
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
