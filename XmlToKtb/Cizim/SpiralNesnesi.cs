using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Spiral (klotoid/geçiş eğrisi) dönüş yönü — LandXML'deki rot="ccw"/"cw" karşılığı.</summary>
    public enum DonusYonu
    {
        Sag,
        Sol
    }

    /// <summary>
    /// Klotoid (geçiş eğrisi) — LandXML CoordGeom Spiral karşılığı. Tek, sürekli bir obje olarak tutulur;
    /// çizim/seçim için kendi içinde örneklenir (parçalara bölünmüş ayrı çizgiler değil).
    /// </summary>
    public class SpiralNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Spiral";

        [DisplayName("Başlangıç X")]
        [Category("Başlangıç Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BaslangicX { get; set; }

        [DisplayName("Başlangıç Y")]
        [Category("Başlangıç Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BaslangicY { get; set; }

        [DisplayName("Bitiş X")]
        [Category("Bitiş Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BitisX { get; set; }

        [DisplayName("Bitiş Y")]
        [Category("Bitiş Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BitisY { get; set; }

        [DisplayName("PI X")]
        [Category("PI Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double PiX { get; set; }

        [DisplayName("PI Y")]
        [Category("PI Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double PiY { get; set; }

        [DisplayName("Başlangıç Z")]
        [Category("Başlangıç Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BaslangicZ { get; set; }

        [DisplayName("Bitiş Z")]
        [Category("Bitiş Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BitisZ { get; set; }

        [DisplayName("PI Z")]
        [Category("PI Noktası")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double PiZ { get; set; }

        [DisplayName("Başlangıç Yarıçapı")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double YaricapBaslangic { get; set; } = double.PositiveInfinity;

        [DisplayName("Bitiş Yarıçapı")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double YaricapBitis { get; set; } = double.PositiveInfinity;

        [DisplayName("Uzunluk")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Uzunluk { get; set; }

        [DisplayName("Yön")]
        public DonusYonu Yon { get; set; }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            var noktalar = OrnekNoktalariAl();
            if (noktalar.Count < 2)
                return;

            var ekranNoktalari = new PointF[noktalar.Count];
            for (var i = 0; i < noktalar.Count; i++)
                ekranNoktalari[i] = gorunum.DunyadanEkrana(noktalar[i].X, noktalar[i].Y, panelGenislik, panelYukseklik);

            using (var kalem = new Pen(Secili ? Color.DodgerBlue : CizimRengi(Color.Black), Secili ? 2f : 1f))
            {
                grafik.DrawLines(kalem, ekranNoktalari);
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            var noktalar = OrnekNoktalariAl();
            for (var i = 0; i < noktalar.Count - 1; i++)
            {
                var uzaklik = NoktaDogruParcasiUzakligi(dunyaNoktasi, noktalar[i], noktalar[i + 1]);
                if (uzaklik <= toleransDunya)
                    return true;
            }
            return false;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)BaslangicX, (float)BaslangicY);
            yield return new PointF((float)BitisX, (float)BitisY);
            yield return new PointF((float)PiX, (float)PiY);
        }

        /// <summary>DXF'e sayısal (Polyline) olarak kaydetmek gibi dış kullanımlar için dünya koordinatındaki örnek noktalar.</summary>
        public List<PointF> OrnekNoktalariGetir(int ornekSayisi = SpiralHesaplayici.VarsayilanOrnekSayisi)
        {
            return OrnekNoktalariAl(ornekSayisi);
        }

        /// <summary>
        /// Örnek noktalarla birlikte, her noktanın Z'sini Başlangıç Z'den Bitiş Z'ye doğrusal enterpolasyonla
        /// verir (spiral düzlemsel kabul edilir; gerçek uygulamalarda genelde yükseklik de doğrusala yakındır).
        /// </summary>
        public List<(PointF Nokta, double Z)> OrnekNoktalariVeZGetir(int ornekSayisi = SpiralHesaplayici.VarsayilanOrnekSayisi)
        {
            var noktalar = OrnekNoktalariAl(ornekSayisi);
            var sonuc = new List<(PointF, double)>(noktalar.Count);
            for (var i = 0; i < noktalar.Count; i++)
            {
                var t = noktalar.Count > 1 ? (double)i / (noktalar.Count - 1) : 0.0;
                var z = BaslangicZ + t * (BitisZ - BaslangicZ);
                sonuc.Add((noktalar[i], z));
            }
            return sonuc;
        }

        /// <summary>
        /// Çizim ekranının Sağa(X)/Yukarı(Y) kuralındaki noktalarımızı SpiralHesaplayici'nin beklediği ham
        /// (Kuzey,Doğu) sırasına çevirip hesaplatır, dönen noktaları tekrar çizim ekranı sırasına çevirir.
        /// </summary>
        private List<PointF> OrnekNoktalariAl(int ornekSayisi = SpiralHesaplayici.VarsayilanOrnekSayisi)
        {
            var hamNoktalar = SpiralHesaplayici.NoktalariHesapla(
                BaslangicY, BaslangicX, BitisY, BitisX, PiY, PiX,
                YaricapBaslangic, YaricapBitis, Uzunluk, Yon == DonusYonu.Sol, ornekSayisi);

            var sonuc = new List<PointF>(hamNoktalar.Count);
            foreach (var nokta in hamNoktalar)
                sonuc.Add(new PointF(nokta.Y, nokta.X));

            return sonuc;
        }

        private static double NoktaDogruParcasiUzakligi(PointF nokta, PointF uc1, PointF uc2)
        {
            var dx = uc2.X - uc1.X;
            var dy = uc2.Y - uc1.Y;
            var uzunlukKare = dx * dx + dy * dy;
            if (uzunlukKare < 1e-12)
                return Math.Sqrt(Math.Pow(nokta.X - uc1.X, 2) + Math.Pow(nokta.Y - uc1.Y, 2));

            var t = ((nokta.X - uc1.X) * dx + (nokta.Y - uc1.Y) * dy) / uzunlukKare;
            t = Math.Max(0, Math.Min(1, t));

            var enYakinX = uc1.X + t * dx;
            var enYakinY = uc1.Y + t * dy;
            return Math.Sqrt(Math.Pow(nokta.X - enYakinX, 2) + Math.Pow(nokta.Y - enYakinY, 2));
        }
    }
}
