using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Güzergahın bir some (PI) noktası: iki doğrultunun kesiştiği yer ve o köşeyi dönen parçalar (kurp, varsa giriş
    /// ve çıkış spiralleri). Grubu boş olan some, kurpsuz bir kırık noktadır.
    /// </summary>
    public class GuzergahSomesi
    {
        public double X, Y;
        /// <summary>Someyi dönen parçaların (doğru olmayan) sıralı kopyaları.</summary>
        public List<GuzergahParcasi> Grup = new List<GuzergahParcasi>();
        /// <summary>Grubun güzergahtaki başlangıç ve bitiş noktaları (teğet noktaları).</summary>
        public double GirisX, GirisY, CikisX, CikisY;
        /// <summary>Grubun parçalarının güzergahtaki sıraları.</summary>
        public List<int> ParcaSiralari = new List<int>();
        /// <summary>
        /// Grup tek kurplu basit bir grupsa (kurp ve varsa doğruya bağlanan giriş/çıkış spiralleri) kurbun ortasında
        /// yarıçapı değiştiren bir tutamaç bulunur.
        /// </summary>
        public bool OrtaTutamacVar;
        public double OrtaX, OrtaY;
    }

    /// <summary>
    /// Güzergahın some (PI) modeli: güzergah başlangıç noktası, some noktaları ve bitiş noktasıyla tanımlanan bir
    /// kırık çizgi ve her somedeki kurp grubu olarak da görülebilir. Bu görünüm, güzergahı ekranda uçlarından ve
    /// somelerinden sürükleyerek düzenlemeyi sağlar: bir nokta taşındığında kurpların yarıçapı ve spirallerin boyu
    /// aynı kalır, yalnızca kurp boyları ve doğruların uzunlukları yeni sapma açılarına göre yeniden hesaplanır.
    /// </summary>
    public partial class GuzergahNesnesi
    {
        /// <summary>Güzergahın some noktaları (her Hesapla çağrısında yenilenir).</summary>
        [Browsable(false)]
        public List<GuzergahSomesi> Someler { get; private set; } = new List<GuzergahSomesi>();

        /// <summary>
        /// Güzergah some modeliyle ifade edilebiliyor mu? Doğrultuları kesişmeyen (paralel ya da 180°'den fazla dönen)
        /// bir kurp grubu varsa edilemez; o zaman ekrandan sürükleyerek düzenleme kapalıdır.
        /// </summary>
        [Browsable(false)]
        public bool SomeModeliGecerli { get; private set; }

        /// <summary>Tutamaçlar güzergah seçiliyken ya da editörü açıkken gösterilir ve sürüklenebilir.</summary>
        [Browsable(false)]
        public bool TutamaclarEtkin => Secili || VurguluParca >= 0;

        private static double AciyiSar(double aci)
        {
            while (aci > Math.PI) aci -= 2 * Math.PI;
            while (aci <= -Math.PI) aci += 2 * Math.PI;
            return aci;
        }

        // Grup şablonları kırıklık taşımaz; kırıklık some modelinde ayrı (kurpsuz) bir somedir.
        private static GuzergahParcasi KiriksizKopya(GuzergahParcasi parca)
        {
            var kopya = parca.Kopyala();
            kopya.KirikAcisi = 0;
            return kopya;
        }

        // Grup basitse (tek kurp; diğer parçalar o kurbu doğruya bağlayan spiraller; hepsi aynı yönde) kurbunu verir.
        private static GuzergahParcasi BasitGrubunKurbu(List<GuzergahParcasi> grup)
        {
            var kurplar = grup.Where(p => p.Tip == GuzergahParcaTipi.Kurp).ToList();
            if (kurplar.Count != 1 || grup.Any(p => p.Yon != grup[0].Yon))
                return null;

            var yaricap = kurplar[0].YaricapBaslangic;
            foreach (var parca in grup)
            {
                if (parca.Tip != GuzergahParcaTipi.Spiral)
                    continue;
                double sonlu = double.IsInfinity(parca.YaricapBaslangic) ? parca.YaricapBitis : parca.YaricapBaslangic;
                var birUcuSonsuz = double.IsInfinity(parca.YaricapBaslangic) != double.IsInfinity(parca.YaricapBitis);
                if (!birUcuSonsuz || Math.Abs(sonlu - yaricap) > 1e-6 * yaricap)
                    return null;
            }
            return kurplar[0];
        }

        // Basit grubun yarıçapını değiştirir: kurp ve ona bağlanan spiral uçları yeni yarıçapı alır.
        private static void GrupYaricapiniAyarla(List<GuzergahParcasi> grup, double yaricap)
        {
            foreach (var parca in grup)
            {
                if (parca.Tip == GuzergahParcaTipi.Kurp)
                {
                    parca.YaricapBaslangic = parca.YaricapBitis = yaricap;
                }
                else if (parca.Tip == GuzergahParcaTipi.Spiral)
                {
                    if (!double.IsInfinity(parca.YaricapBaslangic)) parca.YaricapBaslangic = yaricap;
                    if (!double.IsInfinity(parca.YaricapBitis)) parca.YaricapBitis = yaricap;
                }
            }
        }

        private static double Donus(GuzergahParcasi parca)
        {
            return (parca.BaslangicEgriligi + parca.BitisEgriligi) / 2 * parca.Uzunluk;
        }

        // Parçalardan some noktalarını çıkarır. Doğrular grupları ayırır; eğriliğin sıfırdan geçtiği birleşimler de
        // (ör. çıkış spiralinden hemen giriş spiraline geçilen ters kurplar) sıfır boylu bir doğru gibi grubu böler.
        private void SomeleriGuncelle()
        {
            var someler = new List<GuzergahSomesi>();
            var gecerli = true;
            var acikGrup = new List<GuzergahParcasi>();

            // Art arda gelen parçaların somesini bulur; doğrultuları kesişmiyorsa null döner.
            GuzergahSomesi SomeHesapla(List<GuzergahParcasi> grup)
            {
                GuzergahParcasi ilk = grup[0], son = grup[grup.Count - 1];
                var sapma = grup.Sum(Donus);
                double dx = son.BitisX - ilk.BaslangicX, dy = son.BitisY - ilk.BaslangicY;
                double c = Math.Cos(ilk.BaslangicAcisi), s = Math.Sin(ilk.BaslangicAcisi);
                // Grubun bitiş noktası, girişteki doğrultuya göre (ileri, sola) koordinatlarda.
                double ileri = dx * c + dy * s, sola = -dx * s + dy * c;
                var sinSapma = Math.Sin(sapma);
                if (Math.Abs(sapma) >= Math.PI - 1e-3 || Math.Abs(sinSapma) < 1e-9)
                    return null;

                // Giriş doğrultusu ile çıkış doğrultusunun kesişimi: girişten t1, çıkıştan t2 uzakta.
                double t1 = ileri - sola * Math.Cos(sapma) / sinSapma, t2 = sola / sinSapma;
                if (t1 < -1e-6 || t2 < -1e-6)
                    return null;

                var some = new GuzergahSomesi
                {
                    X = ilk.BaslangicX + t1 * c,
                    Y = ilk.BaslangicY + t1 * s,
                    Grup = grup.Select(KiriksizKopya).ToList(),
                    GirisX = ilk.BaslangicX, GirisY = ilk.BaslangicY, CikisX = son.BitisX, CikisY = son.BitisY,
                    ParcaSiralari = grup.Select(p => Parcalar.IndexOf(p)).ToList()
                };
                var kurp = BasitGrubunKurbu(grup);
                if (kurp != null && KonumAl(kurp.BaslangicKm + kurp.Uzunluk / 2, out var ortaX, out var ortaY, out _))
                {
                    some.OrtaTutamacVar = true;
                    some.OrtaX = ortaX;
                    some.OrtaY = ortaY;
                }
                return some;
            }

            void GrubuKapat()
            {
                if (acikGrup.Count == 0)
                    return;

                var some = SomeHesapla(acikGrup);
                if (some != null)
                {
                    someler.Add(some);
                }
                else
                {
                    // Bir bütün olarak somesi olmayan grup (ör. doğrudan birbirine bağlanan ters kurplar) parça parça
                    // ele alınır: her parçanın kendi somesi olur, aralarındaki doğru sıfır boyludur.
                    foreach (var parca in acikGrup)
                    {
                        var tekli = SomeHesapla(new List<GuzergahParcasi> { parca });
                        if (tekli == null)
                            gecerli = false;
                        else
                            someler.Add(tekli);
                    }
                }
                acikGrup.Clear();
            }

            GuzergahParcasi onceki = null;
            foreach (var parca in Parcalar)
            {
                if (onceki != null && !parca.Sabit && Math.Abs(parca.KirikAcisi) > 1e-9)
                {
                    // Kırık başlayan parça: önündeki grup kapanır ve başlangıcı kurpsuz bir some olur.
                    GrubuKapat();
                    someler.Add(new GuzergahSomesi { X = parca.BaslangicX, Y = parca.BaslangicY, GirisX = parca.BaslangicX, GirisY = parca.BaslangicY, CikisX = parca.BaslangicX, CikisY = parca.BaslangicY });
                    if (parca.Tip != GuzergahParcaTipi.Dogru)
                        acikGrup.Add(parca);
                }
                else if (parca.Tip == GuzergahParcaTipi.Dogru)
                {
                    if (acikGrup.Count > 0)
                    {
                        GrubuKapat();
                    }
                    else if (onceki != null && onceki.Tip == GuzergahParcaTipi.Dogru && Math.Abs(AciyiSar(parca.BaslangicAcisi - onceki.BitisAcisi)) > 1e-7)
                    {
                        // Art arda gelen, doğrultusu farklı iki doğru: kurpsuz kırık nokta.
                        someler.Add(new GuzergahSomesi { X = parca.BaslangicX, Y = parca.BaslangicY, GirisX = parca.BaslangicX, GirisY = parca.BaslangicY, CikisX = parca.BaslangicX, CikisY = parca.BaslangicY });
                    }
                }
                else
                {
                    if (acikGrup.Count > 0 && onceki.BitisEgriligi == 0 && parca.BaslangicEgriligi == 0)
                        GrubuKapat();
                    acikGrup.Add(parca);
                }
                onceki = parca;
            }
            GrubuKapat();

            Someler = someler;
            SomeModeliGecerli = gecerli && Parcalar.Count > 0;
        }

        /// <summary>
        /// Güzergahın some modelini verir: noktalar (başlangıç, someler, bitiş; her biri {x, y}) ve her somenin kurp
        /// grubu. Güzergah boşsa ya da some modeliyle ifade edilemiyorsa false döner.
        /// </summary>
        public bool SomeModeliAl(out List<double[]> noktalar, out List<List<GuzergahParcasi>> gruplar)
        {
            noktalar = new List<double[]>();
            gruplar = new List<List<GuzergahParcasi>>();
            if (!SomeModeliGecerli || Parcalar.Count == 0)
                return false;

            noktalar.Add(new[] { Parcalar[0].BaslangicX, Parcalar[0].BaslangicY });
            foreach (var some in Someler)
            {
                noktalar.Add(new[] { some.X, some.Y });
                gruplar.Add(some.Grup.Select(p => p.Kopyala()).ToList());
            }
            noktalar.Add(new[] { Parcalar[Parcalar.Count - 1].BitisX, Parcalar[Parcalar.Count - 1].BitisY });
            return true;
        }

        /// <summary>
        /// Bir kurp grubunu verilen sapma açısına (radyan; sola artı) oturtur: yarıçaplar ve spiral boyları aynı kalır,
        /// gruptaki en uzun kurbun boyu sapmayı tamamlayacak şekilde değiştirilir. Somenin giriş ve çıkış teğet
        /// boylarını verir. Sığmıyorsa (ör. spiraller tek başına sapmadan fazla dönüyorsa) false döner.
        /// </summary>
        public static bool GrubuYerlestir(List<GuzergahParcasi> grup, double sapma, out double girisTeget, out double cikisTeget)
        {
            return GrubuYerlestir(grup, sapma, out girisTeget, out cikisTeget, out _);
        }

        // Ek olarak, somenin gruptaki (en uzun) kurbun ortasına uzaklığını verir (kurp yoksa 0).
        private static bool GrubuYerlestir(List<GuzergahParcasi> grup, double sapma, out double girisTeget, out double cikisTeget, out double ortaMesafe)
        {
            girisTeget = cikisTeget = ortaMesafe = 0;
            if (grup.Count == 0)
                return true;
            if (Math.Abs(sapma) < 1e-6 || Math.Abs(sapma) > Math.PI - 1e-3)
                return false;

            // Hepsi aynı yöne dönen (basit) grup, sapmanın yönüne çevrilir; böylece some karşı tarafa geçince kurp da döner.
            if (grup.All(p => p.Yon == grup[0].Yon))
            {
                foreach (var parca in grup)
                    parca.Yon = sapma > 0 ? DonusYonu.Sol : DonusYonu.Sag;
            }

            var kurp = grup.Where(p => p.Tip == GuzergahParcaTipi.Kurp).OrderByDescending(p => p.Uzunluk).FirstOrDefault();
            if (kurp != null)
            {
                var digerleri = grup.Where(p => !ReferenceEquals(p, kurp)).Sum(Donus);
                var uzunluk = (sapma - digerleri) / kurp.BaslangicEgriligi;
                if (double.IsNaN(uzunluk) || double.IsInfinity(uzunluk) || uzunluk < 1e-4)
                    return false;
                kurp.Uzunluk = uzunluk;
            }
            else if (Math.Abs(grup.Sum(Donus) - sapma) > 1e-9)
            {
                // Kurpsuz (yalnızca spirallerden oluşan) grubun sapması değiştirilemez.
                return false;
            }

            // Grup, başlangıcı orijinde ve doğrultusu +X olacak şekilde serilir; bitiş noktasından teğet boyları bulunur.
            var gecici = new GuzergahNesnesi();
            gecici.Parcalar.AddRange(grup.Select(p => p.Kopyala()));
            gecici.Hesapla();
            var son = gecici.Parcalar[gecici.Parcalar.Count - 1];
            var sinSapma = Math.Sin(sapma);
            girisTeget = son.BitisX - son.BitisY * Math.Cos(sapma) / sinSapma;
            cikisTeget = son.BitisY / sinSapma;
            if (kurp != null)
            {
                var serilenKurp = gecici.Parcalar[grup.IndexOf(kurp)];
                if (gecici.KonumAl(serilenKurp.BaslangicKm + serilenKurp.Uzunluk / 2, out var ox, out var oy, out _))
                    ortaMesafe = Math.Sqrt((ox - girisTeget) * (ox - girisTeget) + oy * oy);
            }
            return girisTeget >= -1e-9 && cikisTeget >= -1e-9;
        }

        /// <summary>
        /// Güzergahı some modelinden yeniden kurar: noktalar arasındaki doğrultulara göre her gruba sapması verilir,
        /// aradaki doğruların boyu hesaplanır. Kurplar sığmıyorsa (teğet boyları noktalar arasına sığmıyorsa) güzergahı
        /// değiştirmeden false döner.
        /// </summary>
        public bool SomeModelindenKur(List<double[]> noktalar, List<List<GuzergahParcasi>> gruplar, out string hata)
        {
            hata = null;
            var somesayisi = noktalar.Count - 2;
            if (somesayisi < 0 || gruplar.Count != somesayisi)
            {
                hata = "Güzergah için en az iki nokta gerekir.";
                return false;
            }

            var acilar = new double[noktalar.Count - 1];
            var mesafeler = new double[noktalar.Count - 1];
            for (var i = 0; i + 1 < noktalar.Count; i++)
            {
                double dx = noktalar[i + 1][0] - noktalar[i][0], dy = noktalar[i + 1][1] - noktalar[i][1];
                mesafeler[i] = Math.Sqrt(dx * dx + dy * dy);
                if (mesafeler[i] < 1e-6)
                {
                    hata = "Art arda iki nokta üst üste gelemez.";
                    return false;
                }
                acilar[i] = Math.Atan2(dy, dx);
            }

            var yeniGruplar = gruplar.Select(g => g.Select(KiriksizKopya).ToList()).ToList();
            double[] girisTegetleri = new double[somesayisi], cikisTegetleri = new double[somesayisi];
            for (var i = 0; i < somesayisi; i++)
            {
                if (!GrubuYerlestir(yeniGruplar[i], AciyiSar(acilar[i + 1] - acilar[i]), out girisTegetleri[i], out cikisTegetleri[i]))
                {
                    hata = (i + 1) + ". somedeki kurp bu sapma açısına oturmuyor.";
                    return false;
                }
            }

            // Kurpsuz somedeki sapma, o someden sonra gelen ilk parçanın başındaki kırıklık olarak saklanır.
            var parcalar = new List<GuzergahParcasi>();
            var bekleyenKirik = 0.0;
            void ParcaEkle(GuzergahParcasi parca)
            {
                parca.KirikAcisi = parcalar.Count == 0 ? 0 : bekleyenKirik;
                bekleyenKirik = 0;
                parcalar.Add(parca);
            }

            for (var i = 0; i < mesafeler.Length; i++)
            {
                var dogruBoyu = mesafeler[i] - (i > 0 ? cikisTegetleri[i - 1] : 0) - (i < somesayisi ? girisTegetleri[i] : 0);
                if (dogruBoyu < -1e-6)
                {
                    hata = "Kurplar noktalar arasına sığmıyor (teğet boyları doğrunun boyunu aşıyor).";
                    return false;
                }
                if (dogruBoyu > 1e-6)
                    ParcaEkle(new GuzergahParcasi { Tip = GuzergahParcaTipi.Dogru, Uzunluk = dogruBoyu });
                if (i >= somesayisi)
                    continue;

                if (yeniGruplar[i].Count == 0)
                    bekleyenKirik += AciyiSar(acilar[i + 1] - acilar[i]);
                foreach (var parca in yeniGruplar[i])
                    ParcaEkle(parca);
            }
            if (parcalar.Count == 0)
            {
                hata = "Güzergahta parça kalmıyor.";
                return false;
            }

            BaslangicX = noktalar[0][0];
            BaslangicY = noktalar[0][1];
            BaslangicAcisi = acilar[0];
            Parcalar.Clear();
            Parcalar.AddRange(parcalar);
            Hesapla();
            return true;
        }

        // ------------------------------------------------------------------ tutamaçlar

        /// <summary>
        /// Sürüklenebilir noktalar: başlangıç, some noktaları, bitiş ve ardından basit kurpların orta noktaları
        /// (yarıçap tutamaçları).
        /// </summary>
        public IList<double[]> TutamaclariAl()
        {
            if (!TutamaclarEtkin || !SomeModeliAl(out var noktalar, out _))
                return new List<double[]>();
            foreach (var some in Someler)
            {
                if (some.OrtaTutamacVar)
                    noktalar.Add(new[] { some.OrtaX, some.OrtaY });
            }
            return noktalar;
        }

        /// <summary>
        /// Bir ucu ya da someyi yeni yerine taşır (kurp yarıçapları ve spiral boyları korunur); kurp ortasındaki
        /// tutamaç taşınırsa kurbun yarıçapı değişir.
        /// </summary>
        public bool TutamacTasi(int tutamac, double x, double y)
        {
            if (!SomeModeliAl(out var noktalar, out var gruplar) || tutamac < 0)
                return false;

            if (tutamac < noktalar.Count)
            {
                noktalar[tutamac] = new[] { x, y };
                return SomeModelindenKur(noktalar, gruplar, out _);
            }

            // Orta tutamaçlar, tutamacı olan somelerin sırasıyla dizilidir.
            var sira = tutamac - noktalar.Count;
            for (var i = 0; i < Someler.Count; i++)
            {
                if (Someler[i].OrtaTutamacVar && sira-- == 0)
                    return KurpOrtasiniTasi(i, x, y, noktalar, gruplar);
            }
            return false;
        }

        // Kurbun ortası fareye getirilecek şekilde yarıçapı bulur. Somenin kurp ortasına uzaklığı yarıçapla birlikte
        // büyüdüğü için yarıçap ikiye bölme yöntemiyle aranır; spiral boyları aynı kalır.
        private bool KurpOrtasiniTasi(int someSirasi, double x, double y, List<double[]> noktalar, List<List<GuzergahParcasi>> gruplar)
        {
            var some = Someler[someSirasi];
            // Hedef uzaklık: farenin, someden kurp ortasına giden doğrultu üzerindeki izdüşümü.
            double yonX = some.OrtaX - some.X, yonY = some.OrtaY - some.Y, yonBoyu = Math.Sqrt(yonX * yonX + yonY * yonY);
            if (yonBoyu < 1e-9)
                return false;
            var hedef = ((x - some.X) * yonX + (y - some.Y) * yonY) / yonBoyu;
            if (hedef <= 1e-6)
                return false;

            double[] onceki = noktalar[someSirasi], simdiki = noktalar[someSirasi + 1], sonraki = noktalar[someSirasi + 2];
            var sapma = AciyiSar(Math.Atan2(sonraki[1] - simdiki[1], sonraki[0] - simdiki[0]) - Math.Atan2(simdiki[1] - onceki[1], simdiki[0] - onceki[0]));

            double alt = 0.01, ust = 1e7;
            for (var adim = 0; adim < 70; adim++)
            {
                var orta = Math.Sqrt(alt * ust);
                var deneme = gruplar[someSirasi].Select(KiriksizKopya).ToList();
                GrupYaricapiniAyarla(deneme, orta);
                // Sığmayan (spiralleri sapmadan fazla dönen) yarıçap fazla küçüktür.
                if (!GrubuYerlestir(deneme, sapma, out _, out _, out var mesafe) || mesafe < hedef)
                    alt = orta;
                else
                    ust = orta;
            }
            if (ust >= 9e6)
                return false;

            GrupYaricapiniAyarla(gruplar[someSirasi], Math.Round(ust, 3));
            return SomeModelindenKur(noktalar, gruplar, out _);
        }

        // Some noktalarını ve somelere giden teğet doğrultularını (sanal, kesikli) çizer.
        private void SomeleriCiz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            if (!SomeModeliGecerli || Parcalar.Count == 0)
                return;

            using (var kesikli = new Pen(Color.FromArgb(200, 120, 120, 120), 1f) { DashStyle = DashStyle.Dash })
            using (var someKalemi = new Pen(Color.OrangeRed, 1.6f))
            using (var ucFircasi = new SolidBrush(Color.DodgerBlue))
            {
                foreach (var some in Someler)
                {
                    var p = gorunum.DunyadanEkrana(some.X, some.Y, panelGenislik, panelYukseklik);
                    if (some.Grup.Count > 0)
                    {
                        grafik.DrawLine(kesikli, gorunum.DunyadanEkrana(some.GirisX, some.GirisY, panelGenislik, panelYukseklik), p);
                        grafik.DrawLine(kesikli, p, gorunum.DunyadanEkrana(some.CikisX, some.CikisY, panelGenislik, panelYukseklik));
                    }
                    // Some noktası: içi boş üçgen.
                    grafik.DrawPolygon(someKalemi, new[] { new PointF(p.X, p.Y - 7), new PointF(p.X - 6, p.Y + 5), new PointF(p.X + 6, p.Y + 5) });
                    // Kurp ortasındaki yarıçap tutamacı: içi dolu daire.
                    if (some.OrtaTutamacVar)
                    {
                        var orta = gorunum.DunyadanEkrana(some.OrtaX, some.OrtaY, panelGenislik, panelYukseklik);
                        grafik.FillEllipse(Brushes.OrangeRed, orta.X - 5, orta.Y - 5, 10, 10);
                        grafik.DrawEllipse(Pens.White, orta.X - 5, orta.Y - 5, 10, 10);
                    }
                }

                var ilk = gorunum.DunyadanEkrana(Parcalar[0].BaslangicX, Parcalar[0].BaslangicY, panelGenislik, panelYukseklik);
                var son = gorunum.DunyadanEkrana(Parcalar[Parcalar.Count - 1].BitisX, Parcalar[Parcalar.Count - 1].BitisY, panelGenislik, panelYukseklik);
                grafik.FillRectangle(ucFircasi, ilk.X - 4, ilk.Y - 4, 8, 8);
                grafik.FillRectangle(ucFircasi, son.X - 4, son.Y - 4, 8, 8);
            }
        }
    }
}
