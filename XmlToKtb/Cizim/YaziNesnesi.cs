using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    public enum YatayHizalama
    {
        Sol,
        Orta,
        Sag
    }

    public enum DikeyHizalama
    {
        Ust,
        Orta,
        Alt
    }

    /// <summary>Belirli bir konumda gösterilen metin etiketi (ör. JSON/DXF'ten gelen ada/parsel etiketleri).
    /// KonumX/KonumY, DXF'teki gibi Hiza'ya göre metnin belirttiği yakalama (baz) noktasını temsil eder.</summary>
    public class YaziNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Yazı";

        [DisplayName("Metin")]
        public string Metin { get; set; } = "";

        [DisplayName("Konum X")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double KonumX { get; set; }

        [DisplayName("Konum Y")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double KonumY { get; set; }

        [DisplayName("Konum Z")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double KonumZ { get; set; }

        [DisplayName("Yükseklik")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Yukseklik { get; set; } = 1.5;

        [DisplayName("Döndürme")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Donus { get; set; }

        [DisplayName("Yatay Hiza")]
        [Category("Hizalama")]
        public YatayHizalama HizaYatay { get; set; } = YatayHizalama.Sol;

        [DisplayName("Dikey Hiza")]
        [Category("Hizalama")]
        public DikeyHizalama HizaDikey { get; set; } = DikeyHizalama.Ust;

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            if (string.IsNullOrEmpty(Metin))
                return;

            var ekranNoktasi = gorunum.DunyadanEkrana(KonumX, KonumY, panelGenislik, panelYukseklik);
            var pikselYukseklik = (float)(Yukseklik * gorunum.Olcek);
            if (pikselYukseklik < 1f)
                return;

            var eskiDurum = grafik.Save();
            try
            {
                // Dünya açıları +X ekseninden saat yönünün tersine (CCW) ölçülür; ekranda Y ekseni ters
                // olduğundan GDI+'ın saat yönü pozitif döndürmesi için açı negatiflenir.
                grafik.TranslateTransform(ekranNoktasi.X, ekranNoktasi.Y);
                grafik.RotateTransform((float)-Donus);

                using (var font = new Font(FontFamily.GenericSansSerif, pikselYukseklik, GraphicsUnit.Pixel))
                {
                    var olcum = grafik.MeasureString(Metin, font);
                    var yaziNoktasi = new PointF((float)-YatayOfset(olcum.Width), (float)-DikeyOfset(olcum.Height));
                    using (var firca = new SolidBrush(Secili ? Color.DodgerBlue : CizimRengi(Color.Black)))
                        grafik.DrawString(Metin, font, firca, yaziNoktasi);
                }
            }
            finally
            {
                grafik.Restore(eskiDurum);
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            if (string.IsNullOrEmpty(Metin))
                return false;

            // Noktayı, yazının döndürülmemiş yerel eksenine çevir (ters döndürme).
            var rad = Donus * Math.PI / 180.0;
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);
            var dx = dunyaNoktasi.X - KonumX;
            var dy = dunyaNoktasi.Y - KonumY;
            var yerelX = dx * cos + dy * sin;
            var yerelY = -dx * sin + dy * cos;

            // Genişlik, ortalama karakter eni ~0.6 x yükseklik kabul edilerek tahmin edilir (Graphics olmadan ölçülemiyor).
            // yerelY dünya yönünde (yukarı +) olduğundan, ekranda aşağı doğru çizilen kutunun dikey sınırları ters çevrilir.
            var genislikTahmini = Math.Max(Metin.Length * Yukseklik * 0.6, Yukseklik);
            var solOfset = YatayOfset(genislikTahmini);
            var ustOfset = DikeyOfset(Yukseklik);

            var minX = -solOfset - toleransDunya;
            var maxX = genislikTahmini - solOfset + toleransDunya;
            var minY = ustOfset - Yukseklik - toleransDunya;
            var maxY = ustOfset + toleransDunya;

            return yerelX >= minX && yerelX <= maxX && yerelY >= minY && yerelY <= maxY;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)KonumX, (float)KonumY);
        }

        private double YatayOfset(double genislik)
        {
            switch (HizaYatay)
            {
                case YatayHizalama.Orta: return genislik / 2.0;
                case YatayHizalama.Sag: return genislik;
                default: return 0.0;
            }
        }

        private double DikeyOfset(double yukseklik)
        {
            switch (HizaDikey)
            {
                case DikeyHizalama.Orta: return yukseklik / 2.0;
                case DikeyHizalama.Alt: return yukseklik;
                default: return 0.0;
            }
        }
    }
}
