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
            // Açıklıkla bağlanmış (atlamalı) güzergah tek bir some çizgisiyle ifade edilemez.
            SomeModeliGecerli = gecerli && Parcalar.Count > 0 && !Parcalar.Any(p => p.AraBaglanti && p.Sabit);
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

        // Ek olarak, somenin gruptaki (en uzun) kurbun ortasına uzaklığını verir (kurp yoksa 0). Korunacak parça
        // verilirse (boyu elle girilen kurp) sapmayı o değil, gruptaki diğer en uzun kurp tamamlar.
        private static bool GrubuYerlestir(List<GuzergahParcasi> grup, double sapma, out double girisTeget, out double cikisTeget, out double ortaMesafe,
            GuzergahParcasi korunacak = null)
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

            var kurp = grup.Where(p => p.Tip == GuzergahParcaTipi.Kurp && !ReferenceEquals(p, korunacak)).OrderByDescending(p => p.Uzunluk).FirstOrDefault();
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
            return SomeModelindenKur(noktalar, gruplar, out hata, -1, -1);
        }

        // korunanSome/korunanParca: o somenin grubunda boyu değiştirilmeyecek kurp (sapmayı gruptaki diğer kurp tamamlar).
        private bool SomeModelindenKur(List<double[]> noktalar, List<List<GuzergahParcasi>> gruplar, out string hata, int korunanSome, int korunanParca)
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
                var korunacak = i == korunanSome && korunanParca >= 0 && korunanParca < yeniGruplar[i].Count ? yeniGruplar[i][korunanParca] : null;
                if (!GrubuYerlestir(yeniGruplar[i], AciyiSar(acilar[i + 1] - acilar[i]), out girisTegetleri[i], out cikisTegetleri[i], out _, korunacak))
                {
                    hata = (i + 1) + ". somedeki kurp grubu bu sapma açısına oturmuyor (kurpların toplam dönüşü sapmayı aşıyor ya da kurp boyu sıfırın altına düşüyor).";
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
        /// Sürüklenebilir noktalar: başlangıç, some noktaları, bitiş, basit kurpların orta noktaları (yarıçap
        /// tutamaçları) ve güzergah editörü açıkken her parçanın bitiş ucu (parça boyu tutamaçları).
        /// </summary>
        public IList<double[]> TutamaclariAl()
        {
            var tutamaclar = new List<double[]>();
            if (!TutamaclarEtkin)
                return tutamaclar;
            if (SomeModeliAl(out var noktalar, out _))
            {
                tutamaclar.AddRange(noktalar);
                foreach (var some in Someler)
                {
                    if (some.OrtaTutamacVar)
                        tutamaclar.Add(new[] { some.OrtaX, some.OrtaY });
                }
            }
            foreach (var sira in UcTutamaciOlanParcalar())
                tutamaclar.Add(new[] { Parcalar[sira].BitisX, Parcalar[sira].BitisY });
            return tutamaclar;
        }

        // Editör açıkken (güzergah vurgulu) bitiş ucundan sündürülebilen parçalar. Some modeli geçerliyse son parçanın
        // ucu zaten güzergahın bitiş tutamacıdır; değilse o da eklenir.
        private List<int> UcTutamaciOlanParcalar()
        {
            var sonuc = new List<int>();
            if (VurguluParca < 0)
                return sonuc;
            var son = SomeModeliGecerli ? Parcalar.Count - 1 : Parcalar.Count;
            for (var i = 0; i < son; i++)
                sonuc.Add(i);
            return sonuc;
        }

        /// <summary>
        /// Bir ucu ya da someyi yeni yerine taşır (kurp yarıçapları ve spiral boyları korunur); kurp ortasındaki
        /// tutamaç taşınırsa kurbun yarıçapı, parça ucundaki tutamaç taşınırsa o parçanın boyu değişir.
        /// </summary>
        public bool TutamacTasi(int tutamac, double x, double y)
        {
            if (tutamac < 0)
                return false;

            var modelVar = SomeModeliAl(out var noktalar, out var gruplar);
            var modelTutamacSayisi = modelVar ? noktalar.Count + Someler.Count(s => s.OrtaTutamacVar) : 0;
            if (tutamac >= modelTutamacSayisi)
            {
                var ucParcalari = UcTutamaciOlanParcalar();
                var sira = tutamac - modelTutamacSayisi;
                return sira < ucParcalari.Count && ParcaUcunuTasi(ucParcalari[sira], x, y);
            }

            if (tutamac < noktalar.Count)
            {
                noktalar[tutamac] = new[] { x, y };
                return SomeModelindenKur(noktalar, gruplar, out _);
            }

            // Orta tutamaçlar, tutamacı olan somelerin sırasıyla dizilidir.
            var ortaSira = tutamac - noktalar.Count;
            for (var i = 0; i < Someler.Count; i++)
            {
                if (Someler[i].OrtaTutamacVar && ortaSira-- == 0)
                    return KurpOrtasiniTasi(i, x, y, noktalar, gruplar);
            }
            return false;
        }

        // Parçanın başı sabitken verilen boydaki bitiş noktası (eğrilik baştan sona doğrusal değişir).
        private static void ParcaSonu(GuzergahParcasi parca, double boy, out double x, out double y)
        {
            double k0 = parca.BaslangicEgriligi, k1 = parca.BitisEgriligi, a0 = parca.BaslangicAcisi;
            const int adim = 64;
            var h = boy / adim;
            double toplamX = 0, toplamY = 0;
            for (var j = 0; j <= adim; j++)
            {
                var s = j * h;
                var dogrultu = boy <= 0 ? a0 : a0 + k0 * s + (k1 - k0) * s * s / (2 * boy);
                var agirlik = j == 0 || j == adim ? 1 : j % 2 == 1 ? 4 : 2;
                toplamX += agirlik * Math.Cos(dogrultu);
                toplamY += agirlik * Math.Sin(dogrultu);
            }
            x = parca.BaslangicX + toplamX * h / 3;
            y = parca.BaslangicY + toplamY * h / 3;
        }

        /// <summary>
        /// Parçanın bitiş ucunu fareye en yakın gelecek şekilde parçanın boyunu değiştirir (doğruda izdüşüm, kurpta
        /// merkez açısı, spiralde boy araması; yarıçaplar aynı kalır). Sonraki parçalar uç uca kayar.
        /// </summary>
        private bool ParcaUcunuTasi(int sira, double x, double y)
        {
            var parca = Parcalar[sira];
            double yeniBoy;
            if (parca.Tip == GuzergahParcaTipi.Dogru)
            {
                yeniBoy = (x - parca.BaslangicX) * Math.Cos(parca.BaslangicAcisi) + (y - parca.BaslangicY) * Math.Sin(parca.BaslangicAcisi);
            }
            else if (parca.Tip == GuzergahParcaTipi.Kurp)
            {
                // Merkezden bakınca başlangıçtan fareye dönüş açısı (dönüş yönünde).
                var sola = parca.Yon == DonusYonu.Sol;
                var dikme = parca.BaslangicAcisi + (sola ? Math.PI / 2 : -Math.PI / 2);
                double mx = parca.BaslangicX + parca.YaricapBaslangic * Math.Cos(dikme), my = parca.BaslangicY + parca.YaricapBaslangic * Math.Sin(dikme);
                var donus = Math.Atan2(y - my, x - mx) - Math.Atan2(parca.BaslangicY - my, parca.BaslangicX - mx);
                if (!sola) donus = -donus;
                donus %= 2 * Math.PI;
                if (donus < 0) donus += 2 * Math.PI;
                yeniBoy = donus * parca.YaricapBaslangic;
            }
            else
            {
                // Spiral: önce kaba tarama, sonra en iyi aralıkta altın oran araması.
                var ust = 3 * (parca.Uzunluk + Math.Sqrt(Math.Pow(x - parca.BaslangicX, 2) + Math.Pow(y - parca.BaslangicY, 2)));
                double Uzaklik(double boy)
                {
                    ParcaSonu(parca, boy, out var px, out var py);
                    return (px - x) * (px - x) + (py - y) * (py - y);
                }
                double enIyi = 0, enKucuk = double.MaxValue;
                const int tarama = 120;
                for (var k = 1; k <= tarama; k++)
                {
                    var boy = ust * k / tarama;
                    var u = Uzaklik(boy);
                    if (u < enKucuk) { enKucuk = u; enIyi = boy; }
                }
                double a = Math.Max(1e-3, enIyi - ust / tarama), b = enIyi + ust / tarama, oran = (Math.Sqrt(5) - 1) / 2;
                for (var k = 0; k < 60; k++)
                {
                    double c = b - oran * (b - a), d = a + oran * (b - a);
                    if (Uzaklik(c) < Uzaklik(d)) b = d; else a = c;
                }
                yeniBoy = (a + b) / 2;
            }

            if (double.IsNaN(yeniBoy) || yeniBoy < 1e-3)
                return false;
            parca.Uzunluk = yeniBoy;
            SabitleriBirak(sira + 1);
            Hesapla();
            return true;
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

        /// <summary>
        /// Bir somenin kurp grubundaki kurp ya da spiral parçasının değerini (yarıçap, kurp boyu, spiral boyu) some
        /// noktaları ve doğrultular aynı kalacak şekilde değiştirir: kurp sapmayı tamamlayacak boyu alır, teğet boyları
        /// değişir, bağlı doğrular kısalır ya da uzar. Kurp boyu verilirse o boyu verecek yarıçap aranır (spiral boyları
        /// aynı kalır). <paramref name="yeni"/> parçanın istenen değerleridir. Uygulanamazsa güzergah değişmez.
        /// </summary>
        public bool SomeParcasiniDegistir(int parcaSirasi, GuzergahParcasi yeni, bool uzunlukDegisti, out string hata)
        {
            hata = null;
            if (!SomeModeliAl(out var noktalar, out var gruplar))
            {
                hata = "Güzergah some noktalarıyla ifade edilemiyor.";
                return false;
            }
            var someSirasi = Someler.FindIndex(s => s.ParcaSiralari.Contains(parcaSirasi));
            if (someSirasi < 0)
            {
                hata = "Parça bir somenin kurp grubunda değil.";
                return false;
            }

            var grup = gruplar[someSirasi];
            var hedef = grup[Someler[someSirasi].ParcaSiralari.IndexOf(parcaSirasi)];
            var basit = BasitGrubunKurbu(grup) != null;
            double[] onceki = noktalar[someSirasi], simdiki = noktalar[someSirasi + 1], sonraki = noktalar[someSirasi + 2];
            var sapma = AciyiSar(Math.Atan2(sonraki[1] - simdiki[1], sonraki[0] - simdiki[0]) - Math.Atan2(simdiki[1] - onceki[1], simdiki[0] - onceki[0]));

            var hedefSirasi = grup.IndexOf(hedef);
            if (hedef.Tip == GuzergahParcaTipi.Kurp && uzunlukDegisti && !basit)
            {
                // Birleşik kurp: girilen boy aynen kalır, grubun giriş/çıkış doğrultuları sabit kalacak şekilde sapmayı
                // gruptaki diğer en uzun kurp tamamlar (içerdeki someler bu yüzden yer değiştirebilir).
                if (!grup.Any(p => p.Tip == GuzergahParcaTipi.Kurp && !ReferenceEquals(p, hedef)))
                {
                    hata = "Grupta boyu ayarlanabilecek başka kurp yok; giriş ve çıkış doğrultuları korunarak bu kurbun boyu değiştirilemez.";
                    return false;
                }
                hedef.Uzunluk = yeni.Uzunluk;
                return SomeModelindenKur(noktalar, gruplar, out hata, someSirasi, hedefSirasi);
            }
            if (hedef.Tip == GuzergahParcaTipi.Kurp && uzunlukDegisti)
            {
                // Sapma sabitken kurp boyu yarıçapla birlikte artar; istenen boyu veren yarıçap ikiye bölmeyle aranır.
                double alt = 0.01, ust = 1e7, bulunanBoy = double.NaN;
                for (var adim = 0; adim < 90; adim++)
                {
                    var orta = Math.Sqrt(alt * ust);
                    var deneme = grup.Select(KiriksizKopya).ToList();
                    GrupYaricapiniAyarla(deneme, orta);
                    if (!GrubuYerlestir(deneme, sapma, out _, out _))
                    {
                        alt = orta;
                        continue;
                    }
                    var boy = deneme.First(p => p.Tip == GuzergahParcaTipi.Kurp).Uzunluk;
                    if (boy < yeni.Uzunluk) alt = orta; else ust = orta;
                    bulunanBoy = boy;
                }
                if (ust >= 9e6 || double.IsNaN(bulunanBoy) || Math.Abs(bulunanBoy - yeni.Uzunluk) > 1e-3)
                {
                    hata = "Bu kurp boyu, somedeki sapma açısı ve spiral boylarıyla elde edilemiyor.";
                    return false;
                }
                GrupYaricapiniAyarla(grup, ust);
            }
            else if (hedef.Tip == GuzergahParcaTipi.Kurp)
            {
                var yaricap = yeni.YaricapBaslangic;
                if (basit)
                {
                    GrupYaricapiniAyarla(grup, yaricap);
                }
                else
                {
                    // Bileşik grupta yalnızca bu kurp ve ona doğrudan bağlanan spiral uçları değişir.
                    var j = grup.IndexOf(hedef);
                    if (j > 0 && grup[j - 1].Tip == GuzergahParcaTipi.Spiral && !double.IsInfinity(grup[j - 1].YaricapBitis))
                        grup[j - 1].YaricapBitis = yaricap;
                    if (j + 1 < grup.Count && grup[j + 1].Tip == GuzergahParcaTipi.Spiral && !double.IsInfinity(grup[j + 1].YaricapBaslangic))
                        grup[j + 1].YaricapBaslangic = yaricap;
                    hedef.YaricapBaslangic = hedef.YaricapBitis = yaricap;
                }
            }
            else
            {
                // Spiral: boy değişirse kurp boyu kendiliğinden ayarlanır. Doğruya bağlanan ucu aynı kalıp kurp tarafındaki
                // yarıçapı değişirse (basit grupta) kurbun yarıçapı da onunla birlikte değişir.
                hedef.Uzunluk = yeni.Uzunluk;
                var ayniUcSonsuz = double.IsInfinity(hedef.YaricapBaslangic) == double.IsInfinity(yeni.YaricapBaslangic)
                                   && double.IsInfinity(hedef.YaricapBitis) == double.IsInfinity(yeni.YaricapBitis)
                                   && double.IsInfinity(yeni.YaricapBaslangic) != double.IsInfinity(yeni.YaricapBitis);
                if (basit && ayniUcSonsuz)
                {
                    GrupYaricapiniAyarla(grup, double.IsInfinity(yeni.YaricapBaslangic) ? yeni.YaricapBitis : yeni.YaricapBaslangic);
                }
                else
                {
                    hedef.YaricapBaslangic = yeni.YaricapBaslangic;
                    hedef.YaricapBitis = yeni.YaricapBitis;
                }
            }

            return SomeModelindenKur(noktalar, gruplar, out hata);
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
