using System;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Ana çizim ekranının Görünüm menüsündeki seçenekler: obje etiketleri ve kalın çizgi gösterimi. Bir CizimPaneli'ne
    /// verilmezse (kesit editörü, LAS ekranı...) objeler eskisi gibi etiketsiz ve varsayılan kalınlıkla çizilir.
    /// </summary>
    public class GorunumSecenekleri
    {
        public bool NoktaAdlari;
        public bool NoktaKotlari;
        public bool NoktaKodlari;
        public bool AlanAdlari;
        public bool CizgiKotlari;
        public bool KalinCizim;

        /// <summary>Etiket yazılarının büyüklüğü (punto). Ekranda sabittir; yakınlaştırıp uzaklaştırınca değişmez.</summary>
        public float EtiketBoyu = 12f;
    }

    /// <summary>
    /// O anda çizilen panelin bağlamı. Çizim tek (arayüz) iş parçacığında yapıldığından, panel objeleri çizmeden önce
    /// buraya kendi seçeneklerini ve arka plan rengini yazar; objeler etiket ve kalınlık kararlarını buradan okur.
    /// </summary>
    public static class CizimBaglami
    {
        [ThreadStatic] private static GorunumSecenekleri _secenekler;
        [ThreadStatic] private static bool _koyuArkaPlan;

        public static GorunumSecenekleri Secenekler => _secenekler;
        public static bool KoyuArkaPlan => _koyuArkaPlan;

        /// <summary>Bağlamı kurar; dönen nesne Dispose edilince önceki bağlam geri gelir.</summary>
        public static IDisposable Kur(GorunumSecenekleri secenekler, Color arkaPlan)
        {
            var eski = new Geri(_secenekler, _koyuArkaPlan);
            _secenekler = secenekler;
            _koyuArkaPlan = KoyuMu(arkaPlan);
            return eski;
        }

        public static bool KoyuMu(Color renk) => (renk.R * 299 + renk.G * 587 + renk.B * 114) / 1000 < 110;

        /// <summary>Koyu zeminde siyah (ya da çok koyu) renk beyaz çizilir (AutoCAD'deki gibi).</summary>
        public static Color ZemineUydur(Color renk)
        {
            return _koyuArkaPlan && KoyuMu(renk) && renk.GetSaturation() < 0.25f ? Color.White : renk;
        }

        /// <summary>
        /// Etiketi ekran noktasına yazar. Yazı büyüklüğü seçeneklerdeki puntodur ve ekranda sabittir (çizim ölçeğinden
        /// bağımsız). dx/dy yazının noktaya göre kaydırılmasıdır (yazı yüksekliği cinsinden); acıDerece ekranda saat
        /// yönünün tersine dönme açısıdır.
        /// </summary>
        public static void EtiketYaz(Graphics grafik, CizimGorunum gorunum, string metin, PointF ekran, Color renk,
            float dx = 0, float dy = 0, float aciDerece = 0, bool ortala = false)
        {
            if (string.IsNullOrEmpty(metin) || _secenekler == null || _secenekler.EtiketBoyu <= 0)
                return;
            var yukseklik = _secenekler.EtiketBoyu * grafik.DpiY / 72f;
            using (var yazi = new Font(FontFamily.GenericSansSerif, yukseklik, GraphicsUnit.Pixel))
            using (var firca = new SolidBrush(ZemineUydur(renk)))
            {
                var durum = grafik.Save();
                grafik.TranslateTransform(ekran.X, ekran.Y);
                if (Math.Abs(aciDerece) > 0.01f)
                    grafik.RotateTransform(-aciDerece);
                var boyut = grafik.MeasureString(metin, yazi);
                var x = dx * yukseklik - (ortala ? boyut.Width / 2 : 0);
                var y = dy * yukseklik - boyut.Height / 2;
                grafik.DrawString(metin, yazi, firca, x, y);
                grafik.Restore(durum);
            }
        }

        /// <summary>Ekrandaki iki nokta doğrultusunda, yazının ters durmaması için (-90°, 90°] aralığına getirilmiş açı.</summary>
        public static float OkunurAci(PointF a, PointF b)
        {
            var aci = (float)(Math.Atan2(-(b.Y - a.Y), b.X - a.X) * 180 / Math.PI);
            if (aci > 90) aci -= 180;
            else if (aci <= -90) aci += 180;
            return aci;
        }

        public static string KotMetni(double kot) => kot.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        private sealed class Geri : IDisposable
        {
            private readonly GorunumSecenekleri _secenekler;
            private readonly bool _koyu;

            public Geri(GorunumSecenekleri secenekler, bool koyu)
            {
                _secenekler = secenekler;
                _koyu = koyu;
            }

            public void Dispose()
            {
                CizimBaglami._secenekler = _secenekler;
                CizimBaglami._koyuArkaPlan = _koyu;
            }
        }
    }
}
