using System;

namespace XmlToKtb.Projeksiyon
{
    /// <summary>
    /// ED-50 (Hayford/International 1924) ile ITRF96/WGS84 (GRS80) datumları arasında yaklaşık jeosentrik
    /// öteleme (3 parametreli Molodensky) dönüşümü. NIMA TR8350.2'de yayınlanan, Avrupa bölgesi için
    /// yaygın kullanılan ortalama parametreler kullanılır: dX=-87m, dY=-98m, dZ=-121m (ED-50 → WGS84).
    /// Bu parametreler birkaç metre mertebesinde YAKLAŞIK sonuç verir; jeodezik (survey-grade) hassasiyette değildir.
    /// </summary>
    public static class DatumDonusumu
    {
        private const double DX_Ed50ToWgs84 = -87.0;
        private const double DY_Ed50ToWgs84 = -98.0;
        private const double DZ_Ed50ToWgs84 = -121.0;

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
            EcefdenGeodetige(x + DX_Ed50ToWgs84, y + DY_Ed50ToWgs84, z + DZ_Ed50ToWgs84, Elipsoid.Wgs84,
                out hedefEnlemRad, out hedefBoylamRad, out _);
        }

        public static void Wgs84DenEd50e(double enlemRad, double boylamRad, out double hedefEnlemRad, out double hedefBoylamRad)
        {
            GeodetikTenEcefe(enlemRad, boylamRad, 0, Elipsoid.Wgs84, out var x, out var y, out var z);
            EcefdenGeodetige(x - DX_Ed50ToWgs84, y - DY_Ed50ToWgs84, z - DZ_Ed50ToWgs84, Elipsoid.Hayford1924,
                out hedefEnlemRad, out hedefBoylamRad, out _);
        }
    }
}
