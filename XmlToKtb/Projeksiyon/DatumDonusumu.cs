using System;

namespace XmlToKtb.Projeksiyon
{
    /// <summary>
    /// ED-50 (Hayford/International 1924) ile ITRF96/WGS84 (GRS80) datumları arasında 7 parametreli (Helmert,
    /// "Position Vector") dönüşüm. Türkiye geneli için yayınlanan EPSG:1784 "ED50 to WGS 84 (30)" parametreleri
    /// kullanılır: dX=-84.1m, dY=-101.8m, dZ=-129.7m, rZ=0.468", ölçek=1.05 ppm (nokta bulutu modülüyle aynı).
    /// Bu parametreler yaklaşık 2 m doğrulukta sonuç verir; jeodezik (survey-grade) hassasiyette değildir.
    /// </summary>
    public static class DatumDonusumu
    {
        private const double DX_Ed50ToWgs84 = -84.1;
        private const double DY_Ed50ToWgs84 = -101.8;
        private const double DZ_Ed50ToWgs84 = -129.7;
        private const double RZ_Ed50ToWgs84 = 0.468 * Math.PI / 180.0 / 3600.0;
        private const double Olcek_Ed50ToWgs84 = 1.0 + 1.05e-6;

        public static void GeodetikTenEcefe(double enlemRad, double boylamRad, double yukseklik, Elipsoid elipsoid,
            out double x, out double y, out double z)
        {
            var n = elipsoid.A / Math.Sqrt(1 - elipsoid.E2 * Math.Pow(Math.Sin(enlemRad), 2));
            x = (n + yukseklik) * Math.Cos(enlemRad) * Math.Cos(boylamRad);
            y = (n + yukseklik) * Math.Cos(enlemRad) * Math.Sin(boylamRad);
            z = (n * (1 - elipsoid.E2) + yukseklik) * Math.Sin(enlemRad);
        }

        public static void EcefdenGeodetige(double x, double y, double z, Elipsoid elipsoid,
            out double enlemRad, out double boylamRad, out double yukseklik)
        {
            var p = Math.Sqrt(x * x + y * y);
            boylamRad = Math.Atan2(y, x);

            var enlem = Math.Atan2(z, p * (1 - elipsoid.E2));
            for (var i = 0; i < 5; i++)
            {
                var n = elipsoid.A / Math.Sqrt(1 - elipsoid.E2 * Math.Pow(Math.Sin(enlem), 2));
                yukseklik = p / Math.Cos(enlem) - n;
                enlem = Math.Atan2(z, p * (1 - elipsoid.E2 * n / (n + yukseklik)));
            }

            var sonN = elipsoid.A / Math.Sqrt(1 - elipsoid.E2 * Math.Pow(Math.Sin(enlem), 2));
            yukseklik = p / Math.Cos(enlem) - sonN;
            enlemRad = enlem;
        }

        public static void Ed50DenWgs84e(double enlemRad, double boylamRad, out double hedefEnlemRad, out double hedefBoylamRad)
        {
            GeodetikTenEcefe(enlemRad, boylamRad, 0, Elipsoid.Hayford1924, out var x, out var y, out var z);
            EcefdenGeodetige(
                DX_Ed50ToWgs84 + Olcek_Ed50ToWgs84 * (x - RZ_Ed50ToWgs84 * y),
                DY_Ed50ToWgs84 + Olcek_Ed50ToWgs84 * (RZ_Ed50ToWgs84 * x + y),
                DZ_Ed50ToWgs84 + Olcek_Ed50ToWgs84 * z, Elipsoid.Wgs84,
                out hedefEnlemRad, out hedefBoylamRad, out _);
        }

        public static void Wgs84DenEd50e(double enlemRad, double boylamRad, out double hedefEnlemRad, out double hedefBoylamRad)
        {
            GeodetikTenEcefe(enlemRad, boylamRad, 0, Elipsoid.Wgs84, out var x, out var y, out var z);
            // İleri dönüşümün tersi: öteleme ve ölçek geri alınır, dönüklük ters yönde uygulanır.
            double ax = (x - DX_Ed50ToWgs84) / Olcek_Ed50ToWgs84, ay = (y - DY_Ed50ToWgs84) / Olcek_Ed50ToWgs84, az = (z - DZ_Ed50ToWgs84) / Olcek_Ed50ToWgs84;
            EcefdenGeodetige(ax + RZ_Ed50ToWgs84 * ay, -RZ_Ed50ToWgs84 * ax + ay, az, Elipsoid.Hayford1924,
                out hedefEnlemRad, out hedefBoylamRad, out _);
        }
    }
}
