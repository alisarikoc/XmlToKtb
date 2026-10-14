using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;

namespace XmlToKtb.Cizim
{
    public enum EtiketTarafi
    {
        Sol,
        Sag
    }

    /// <summary>Etiket yazısının güzergah eksenine göre doğrultusu.</summary>
    public enum EtiketYaziYonu
    {
        EkseneParalel,
        EkseneDik
    }

    /// <summary>Aralıklı (ör. her 100 m) kilometre etiketlerinin yazılış biçimi.</summary>
    public enum AralikEtiketBicimi
    {
        /// <summary>TCK (Karayolları) usulü: 1000'in katları tam (1+000), aradakiler yalnızca yüzler basamağıyla (1, 2, ... 9).</summary>
        Tck,
        /// <summary>Her etikette kilometrenin tamamı (1+100).</summary>
        TamKilometre
    }

    /// <summary>
    /// Bir güzergahın kilometre etiketleri (Civil 3D'deki "alignment label set" karşılığı): güzergahtan ayrı seçilen,
    /// özellikleri değiştirilebilen bir objedir. Etiketler güzergaha bağlıdır; güzergah düzenlendiğinde yerleri ve
    /// kilometreleri kendiliğinden güncellenir. Yazı boyu kağıt üzerindeki milimetre olarak verilir ve proje
    /// ölçeğiyle araziye çevrilir (1/1000'de 2 mm = 2 m).
    ///
    /// İki tür etiket vardır: geometri noktaları (başlangıç, bitiş, parça birleşimleri; tam kilometresiyle ve eksene
    /// dik yazılır) ve aralıklı etiketler (ör. her 100 m; biçimi ve yazı yönü ayrıca seçilir).
    /// </summary>
    public class GuzergahEtiketNesnesi : CizimNesnesi
    {
        /// <summary>Tek bir etiketin yeri ve yazısı.</summary>
        public struct Etiket
        {
            /// <summary>Etiketin güzergah üzerindeki noktası (çentiğin ortası).</summary>
            public double X, Y;
            public string Metin;
            /// <summary>Güzergahtan etiket tarafına doğru dikme doğrultusu (+X'ten saat yönünün tersine, derece).</summary>
            public double DikmeAcisi;
            /// <summary>Yazının dayandığı nokta: ortalı yazıda yazının ortası, diğerlerinde başladığı ya da bittiği yer.</summary>
            public double YaziX, YaziY;
            /// <summary>Yazının çizileceği açı; baş aşağı gelmeyecek şekilde seçilmiştir (derece).</summary>
            public double YaziAcisi;
            /// <summary>Yazı dayanma noktasına göre ortalanır (eksene paralel yazılar).</summary>
            public bool Ortali;
            /// <summary>Ortalı değilse: yazının sonu dayanma noktasına gelir (çevrilmiş dik yazılar).</summary>
            public bool SagaDayali;
            /// <summary>Yazının arazideki yüksekliği (m).</summary>
            public double Yukseklik;
        }

        [Browsable(false)]
        public GuzergahNesnesi Guzergah { get; }

        public GuzergahEtiketNesnesi(GuzergahNesnesi guzergah)
        {
            Guzergah = guzergah;
        }

        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Güzergah Etiketi";

        [DisplayName("Güzergah")]
        [Category("Etiket")]
        public string GuzergahAdi => Guzergah.Ad;

        [DisplayName("Yazı Yüksekliği (mm)")]
        [Description("Geometri noktalarının (başlangıç, bitiş, birleşim) yazısının kağıt üzerindeki yüksekliği. Arazideki boyu proje ölçeğiyle bulunur: 1/1000'de 2 mm = 2 m.")]
        [Category("Etiket")]
        public double YaziYuksekligiMm { get; set; } = CizimAyarlari.KmYazisiYuksekligiMm;

        [DisplayName("Renk")]
        [Description("Boş bırakılırsa etiketler tabakanın rengiyle çizilir.")]
        [Category("Etiket")]
        public Color Renk { get; set; } = Color.Empty;

        [DisplayName("Taraf")]
        [Description("Etiketlerin, gidiş yönüne göre güzergahın hangi yanına yazılacağı.")]
        [Category("Etiket")]
        public EtiketTarafi Taraf { get; set; } = EtiketTarafi.Sol;

        [DisplayName("Ondalık Basamak")]
        [Description("Geometri noktalarının kilometresindeki ondalık basamak sayısı.")]
        [Category("Etiket")]
        public int OndalikBasamak { get; set; } = 3;

        [DisplayName("Ön Ek")]
        [Description("Tam yazılan kilometrelerin önüne eklenecek metin (ör. \"Km \").")]
        [Category("Etiket")]
        public string OnEk { get; set; } = "";

        [DisplayName("Çentik")]
        [Description("Etiket noktasında güzergahı dik kesen kısa çizgi.")]
        [Category("Etiket")]
        public bool CentikGoster { get; set; } = true;

        [DisplayName("Başlangıç ve Bitiş")]
        [Category("Geometri Noktaları")]
        public bool BaslangicVeBitis { get; set; } = true;

        [DisplayName("Birleşim Noktaları")]
        [Description("Doğru, kurp ve spirallerin birleştiği noktalar.")]
        [Category("Geometri Noktaları")]
        public bool BirlesimNoktalari { get; set; } = true;

        [DisplayName("Aralık (m)")]
        [Description("Sıfırdan büyükse bu aralığın katı olan kilometrelere de etiket yazılır (ör. 100: her 100 metrede). 0: kapalı.")]
        [Category("Aralıklı Etiketler")]
        public double Aralik { get; set; }

        [DisplayName("Biçim")]
        [Description("Tck: 1000 metrenin katları tam (1+000), aradakiler yalnızca yüzler basamağıyla (1, 2 ... 9) yazılır. TamKilometre: her etikette kilometrenin tamamı yazılır.")]
        [Category("Aralıklı Etiketler")]
        public AralikEtiketBicimi AralikBicimi { get; set; } = AralikEtiketBicimi.Tck;

        [DisplayName("Yazı Yönü")]
        [Description("Aralıklı etiketlerin yazısı eksene paralel mi dik mi olsun.")]
        [Category("Aralıklı Etiketler")]
        public EtiketYaziYonu AralikYaziYonu { get; set; } = EtiketYaziYonu.EkseneParalel;

        [DisplayName("Yazı Yüksekliği (mm)")]
        [Description("Aralıklı etiketlerin kağıt üzerindeki yazı yüksekliği (geometri noktalarının yazı boyundan bağımsızdır).")]
        [Category("Aralıklı Etiketler")]
        public double AralikYaziYuksekligiMm { get; set; } = CizimAyarlari.KmYazisiYuksekligiMm;

        [DisplayName("Tam Km Yazı Çarpanı")]
        [Description("Tck biçiminde 1000 metrenin katlarındaki yazının, diğer yazılara göre kaç kat büyük yazılacağı (1: aynı boy).")]
        [Category("Aralıklı Etiketler")]
        public double TamKmYaziCarpani { get; set; } = 1.0;

        /// <summary>Yazının arazideki yüksekliği (m).</summary>
        [Browsable(false)]
        public double YaziYuksekligi => YaziYuksekligiMm * CizimAyarlari.ProjeOlcegi / 1000.0;

        /// <summary>Yazının güzergah ekseninden uzaklığı (m).</summary>
        [Browsable(false)]
        public double YaziBoslugu => YaziYuksekligi * 0.8;

        public GuzergahEtiketNesnesi Kopyala()
        {
            return new GuzergahEtiketNesnesi(Guzergah)
            {
                YaziYuksekligiMm = YaziYuksekligiMm,
                Renk = Renk,
                Taraf = Taraf,
                OndalikBasamak = OndalikBasamak,
                OnEk = OnEk,
                CentikGoster = CentikGoster,
                BaslangicVeBitis = BaslangicVeBitis,
                BirlesimNoktalari = BirlesimNoktalari,
                Aralik = Aralik,
                AralikBicimi = AralikBicimi,
                AralikYaziYonu = AralikYaziYonu,
                TamKmYaziCarpani = TamKmYaziCarpani,
                AralikYaziYuksekligiMm = AralikYaziYuksekligiMm
            };
        }

        // Kilometre 1000 metrenin (milimetre hassasiyetinde) tam katı mı?
        private static bool TamKilometreMi(double km)
        {
            return Math.Abs(km - Math.Round(km / 1000.0) * 1000.0) < 1e-3;
        }

        // Aralıklı etiketin yazısı. Tck biçiminde 1000'in katları tam, aradakiler yüzler basamağıyla yazılır; aralık
        // 100'ün katı değilse (ör. 50) ara değer ondalıkla gösterilir (150 m → 1.5).
        private string AralikMetni(double km)
        {
            if (AralikBicimi == AralikEtiketBicimi.TamKilometre || TamKilometreMi(km))
                return (OnEk ?? "") + GuzergahNesnesi.KmMetni(km, 0);

            var yuzler = Math.Round((Math.Abs(km) - Math.Floor(Math.Abs(km) / 1000.0) * 1000.0) / 100.0, 4);
            return yuzler.ToString("0.####", CultureInfo.InvariantCulture);
        }

        /// <summary>Ayarlara göre etiketlenecek noktaları, güzergahın o anki geometrisinden üretir.</summary>
        public List<Etiket> EtiketleriGetir()
        {
            var etiketler = new List<Etiket>();
            var parcalar = Guzergah.Parcalar;
            if (parcalar.Count == 0)
                return etiketler;

            // Önce geometri noktaları; aralıklı etiket aynı kilometreye denk gelirse geometri noktası öncelik alır.
            var geometriKm = new List<double>();
            void GeometriEkle(double km)
            {
                foreach (var mevcut in geometriKm)
                {
                    if (Math.Abs(mevcut - km) < 1e-3)
                        return;
                }
                geometriKm.Add(km);
            }

            if (BaslangicVeBitis)
            {
                GeometriEkle(parcalar[0].BaslangicKm);
                GeometriEkle(parcalar[parcalar.Count - 1].BitisKm);
            }
            if (BirlesimNoktalari)
            {
                for (var i = 1; i < parcalar.Count; i++)
                    GeometriEkle(parcalar[i].BaslangicKm);
            }

            var basamak = Math.Max(0, Math.Min(6, OndalikBasamak));
            foreach (var km in geometriKm)
                EtiketEkle(etiketler, km, (OnEk ?? "") + GuzergahNesnesi.KmMetni(km, basamak), EtiketYaziYonu.EkseneDik, YaziYuksekligi);

            // Aralıklı etiketlerin kendi yazı boyu vardır; Tck biçiminde tam kilometreler istenirse daha büyük yazılır.
            var aralikYuksekligi = Math.Max(0, AralikYaziYuksekligiMm) * CizimAyarlari.ProjeOlcegi / 1000.0;

            if (Aralik > 0)
            {
                double ilk = parcalar[0].BaslangicKm, son = parcalar[parcalar.Count - 1].BitisKm;
                // Çok küçük aralık binlerce etiket üretmesin.
                if ((son - ilk) / Aralik <= 20000)
                {
                    var ilkKat = (long)Math.Ceiling(ilk / Aralik - 1e-9);
                    for (var kat = ilkKat; kat * Aralik <= son + 1e-9; kat++)
                    {
                        var km = kat * Aralik;
                        var geometriNoktasi = false;
                        foreach (var mevcut in geometriKm)
                            geometriNoktasi |= Math.Abs(mevcut - km) < 1e-3;
                        if (geometriNoktasi)
                            continue;
                        var buyukYazi = AralikBicimi == AralikEtiketBicimi.Tck && TamKilometreMi(km) && TamKmYaziCarpani > 0;
                        EtiketEkle(etiketler, km, AralikMetni(km), AralikYaziYonu, aralikYuksekligi * (buyukYazi ? TamKmYaziCarpani : 1.0));
                    }
                }
            }

            return etiketler;
        }

        private void EtiketEkle(List<Etiket> etiketler, double km, string metin, EtiketYaziYonu yon, double yukseklik)
        {
            if (yukseklik <= 0 || !Guzergah.KonumAl(km, out var x, out var y, out var gidisAcisi))
                return;

            var dikmeRad = gidisAcisi + (Taraf == EtiketTarafi.Sol ? Math.PI / 2 : -Math.PI / 2);
            var etiket = new Etiket { X = x, Y = y, Metin = metin, DikmeAcisi = AciyiDuzelt(dikmeRad * 180.0 / Math.PI), Yukseklik = yukseklik };

            if (yon == EtiketYaziYonu.EkseneDik)
            {
                // Yazı dikme doğrultusunda, eksenden bir boşluk bırakılarak yazılır. Baş aşağı gelecekse çevrilir
                // ve sonu eksene bakacak şekilde hizalanır.
                var ters = etiket.DikmeAcisi > 90.0 && etiket.DikmeAcisi < 270.0;
                etiket.YaziX = x + YaziBoslugu * Math.Cos(dikmeRad);
                etiket.YaziY = y + YaziBoslugu * Math.Sin(dikmeRad);
                etiket.YaziAcisi = ters ? etiket.DikmeAcisi - 180.0 : etiket.DikmeAcisi;
                etiket.SagaDayali = ters;
            }
            else
            {
                // Yazı eksene paralel, çentiğin hizasında ortalanmış olarak eksenin yanına yazılır.
                var uzaklik = YaziBoslugu + yukseklik / 2;
                var gidisDerece = AciyiDuzelt(gidisAcisi * 180.0 / Math.PI);
                etiket.YaziX = x + uzaklik * Math.Cos(dikmeRad);
                etiket.YaziY = y + uzaklik * Math.Sin(dikmeRad);
                etiket.YaziAcisi = gidisDerece > 90.0 && gidisDerece < 270.0 ? gidisDerece - 180.0 : gidisDerece;
                etiket.Ortali = true;
            }

            etiketler.Add(etiket);
        }

        private static double AciyiDuzelt(double derece)
        {
            derece %= 360.0;
            return derece < 0 ? derece + 360.0 : derece;
        }


        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            // Çentik boyu geometri noktalarının yazı boyu kadardır.
            var pikselYukseklik = (float)Math.Min(YaziYuksekligi * gorunum.Olcek, 400.0);

            var renk = Secili ? Color.DodgerBlue : Renk.IsEmpty ? CizimRengi(Color.Black) : Renk;
            var fontlar = new Dictionary<double, Font>();
            try
            {
                using (var kalem = new Pen(renk, 1.5f))
                using (var firca = new SolidBrush(renk))
                {
                    foreach (var etiket in EtiketleriGetir())
                    {
                        var ekran = gorunum.DunyadanEkrana(etiket.X, etiket.Y, panelGenislik, panelYukseklik);
                        if (ekran.X < -2000 || ekran.X > panelGenislik + 2000 || ekran.Y < -2000 || ekran.Y > panelYukseklik + 2000)
                            continue;

                        // Okunamayacak kadar küçülen yazı (ve çentiği) çizilmez; aşırı yakınlaştırmada yazı boyu sınırlanır.
                        var yaziPikseli = Math.Min(etiket.Yukseklik * gorunum.Olcek, 400.0);
                        if (yaziPikseli < 4.0)
                            continue;

                        if (CentikGoster)
                        {
                            // Çentik: güzergahı dik kesen, yazı boyu kadar kısa bir çizgi (ekranda Y ekseni terstir).
                            var dikme = etiket.DikmeAcisi * Math.PI / 180.0;
                            float dx = (float)(Math.Cos(dikme) * pikselYukseklik / 2), dy = (float)(-Math.Sin(dikme) * pikselYukseklik / 2);
                            grafik.DrawLine(kalem, ekran.X - dx, ekran.Y - dy, ekran.X + dx, ekran.Y + dy);
                        }

                        if (!fontlar.TryGetValue(yaziPikseli, out var font))
                            fontlar[yaziPikseli] = font = new Font(FontFamily.GenericSansSerif, (float)yaziPikseli, GraphicsUnit.Pixel);

                        var yaziEkran = gorunum.DunyadanEkrana(etiket.YaziX, etiket.YaziY, panelGenislik, panelYukseklik);
                        var durum = grafik.Save();
                        try
                        {
                            // Dünya açıları saat yönünün tersine ölçülür; ekranda Y ekseni ters olduğu için açı eksi alınır.
                            grafik.TranslateTransform(yaziEkran.X, yaziEkran.Y);
                            grafik.RotateTransform((float)-etiket.YaziAcisi);

                            var olcum = grafik.MeasureString(etiket.Metin, font);
                            var yaziX = etiket.Ortali ? -olcum.Width / 2 : etiket.SagaDayali ? -olcum.Width : 0;
                            grafik.DrawString(etiket.Metin, font, firca, yaziX, -olcum.Height / 2);
                        }
                        finally
                        {
                            grafik.Restore(durum);
                        }
                    }
                }
            }
            finally
            {
                foreach (var font in fontlar.Values)
                    font.Dispose();
            }
        }

        // Yazının yaklaşık boyu: karakter başına yüksekliğin 0,6 katı genişlik.
        private double YaziUzunlugu(Etiket etiket)
        {
            return etiket.Metin.Length * etiket.Yukseklik * 0.6;
        }

        // Yazının kapladığı şeridin iki ucu (yazı doğrultusunda).
        private void YaziUclari(Etiket etiket, out double x1, out double y1, out double x2, out double y2)
        {
            var rad = etiket.YaziAcisi * Math.PI / 180.0;
            var uzunluk = YaziUzunlugu(etiket);
            var bas = etiket.Ortali ? -uzunluk / 2 : etiket.SagaDayali ? -uzunluk : 0;
            x1 = etiket.YaziX + bas * Math.Cos(rad);
            y1 = etiket.YaziY + bas * Math.Sin(rad);
            x2 = x1 + uzunluk * Math.Cos(rad);
            y2 = y1 + uzunluk * Math.Sin(rad);
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            // Etiket seti, etiketlerden birinin yazısının üzerine tıklanınca seçilir.
            foreach (var etiket in EtiketleriGetir())
            {
                YaziUclari(etiket, out var x1, out var y1, out var x2, out var y2);
                double dx = x2 - x1, dy = y2 - y1;
                var uzunlukKare = dx * dx + dy * dy;
                var t = uzunlukKare < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((dunyaNoktasi.X - x1) * dx + (dunyaNoktasi.Y - y1) * dy) / uzunlukKare));
                double ex = x1 + t * dx - dunyaNoktasi.X, ey = y1 + t * dy - dunyaNoktasi.Y;
                var sinir = etiket.Yukseklik / 2 + toleransDunya;
                if (ex * ex + ey * ey <= sinir * sinir)
                    return true;
            }
            return false;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            // Pencereyle seçimde ve ekrana sığdırmada etiketin noktası ve yazının iki ucu kullanılır.
            foreach (var etiket in EtiketleriGetir())
            {
                YaziUclari(etiket, out var x1, out var y1, out var x2, out var y2);
                yield return new PointF((float)etiket.X, (float)etiket.Y);
                yield return new PointF((float)x1, (float)y1);
                yield return new PointF((float)x2, (float)y2);
            }
        }
    }
}
