using System;

namespace XmlToKtb.Projeksiyon
{
    /// <summary>Enlem/boylamdan Gauss-Krüger / UTM (Transverse Mercator) düzlem koordinatına ve tersine dönüşüm (Redfearn seri formülleri).</summary>
    public static class GaussKruger
    {
        public static void Ileri(double enlemRad, double boylamRad, double merkeziMeridyenRad, Elipsoid elipsoid,
            double olcekFaktoru, double sagaKaydirma, double yukariKaydirma, out double saga, out double yukari)
        {
            var a = elipsoid.A;
            var e2 = elipsoid.E2;
            var eIkinciUssu = elipsoid.EIkinciUssu;

            var n = a / Math.Sqrt(1 - e2 * Math.Pow(Math.Sin(enlemRad), 2));
            var t = Math.Pow(Math.Tan(enlemRad), 2);
            var c = eIkinciUssu * Math.Pow(Math.Cos(enlemRad), 2);
            var aa = (boylamRad - merkeziMeridyenRad) * Math.Cos(enlemRad);

            var m = a * ((1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256) * enlemRad
                         - (3 * e2 / 8 + 3 * e2 * e2 / 32 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(2 * enlemRad)
                         + (15 * e2 * e2 / 256 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(4 * enlemRad)
                         - (35 * e2 * e2 * e2 / 3072) * Math.Sin(6 * enlemRad));

            saga = sagaKaydirma + olcekFaktoru * n * (aa
                + (1 - t + c) * Math.Pow(aa, 3) / 6
                + (5 - 18 * t + t * t + 72 * c - 58 * eIkinciUssu) * Math.Pow(aa, 5) / 120);

            yukari = yukariKaydirma + olcekFaktoru * (m + n * Math.Tan(enlemRad) * (
                Math.Pow(aa, 2) / 2
                + (5 - t + 9 * c + 4 * c * c) * Math.Pow(aa, 4) / 24
                + (61 - 58 * t + t * t + 600 * c - 330 * eIkinciUssu) * Math.Pow(aa, 6) / 720));
        }

        public static void Ters(double saga, double yukari, double merkeziMeridyenRad, Elipsoid elipsoid,
            double olcekFaktoru, double sagaKaydirma, double yukariKaydirma, out double enlemRad, out double boylamRad)
        {
            var a = elipsoid.A;
            var e2 = elipsoid.E2;
            var eIkinciUssu = elipsoid.EIkinciUssu;
            var e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));

            var m = (yukari - yukariKaydirma) / olcekFaktoru;
            var mu = m / (a * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256));

            var enlem1 = mu
                + (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32) * Math.Sin(2 * mu)
                + (21 * e1 * e1 / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu)
                + (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu)
                + (1097 * Math.Pow(e1, 4) / 512) * Math.Sin(8 * mu);

            var n1 = a / Math.Sqrt(1 - e2 * Math.Pow(Math.Sin(enlem1), 2));
            var t1 = Math.Pow(Math.Tan(enlem1), 2);
            var c1 = eIkinciUssu * Math.Pow(Math.Cos(enlem1), 2);
            var r1 = a * (1 - e2) / Math.Pow(1 - e2 * Math.Pow(Math.Sin(enlem1), 2), 1.5);
            var d = (saga - sagaKaydirma) / (n1 * olcekFaktoru);

            enlemRad = enlem1 - (n1 * Math.Tan(enlem1) / r1) * (
                Math.Pow(d, 2) / 2
                - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * eIkinciUssu) * Math.Pow(d, 4) / 24
                + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * eIkinciUssu - 3 * c1 * c1) * Math.Pow(d, 6) / 720);

            boylamRad = merkeziMeridyenRad + (
                d
                - (1 + 2 * t1 + c1) * Math.Pow(d, 3) / 6
                + (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * eIkinciUssu + 24 * t1 * t1) * Math.Pow(d, 5) / 120
            ) / Math.Cos(enlem1);
        }
    }
}
