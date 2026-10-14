using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace XmlToKtb.Las
{
    public enum KoordinatDatumu
    {
        Ed50,
        Itrf96,
        Wgs84
    }

    /// <summary>Türkiye'de kullanılan, EPSG koduyla tanımlı bir koordinat sistemi (coğrafi ya da Transversal Merkator).</summary>
    public class KoordinatSistemi
    {
        public int Epsg;
        public string Ad;
        public KoordinatDatumu Datum;
        /// <summary>true: enlem/boylam (derece); false: Transversal Merkator (sağa/yukarı, metre).</summary>
        public bool Cografi;
        /// <summary>Dilim orta meridyeni (derece).</summary>
        public double OrtaMeridyen;
        /// <summary>Orta meridyendeki ölçek katsayısı (3° dilimde 1, 6° UTM'de 0.9996).</summary>
        public double Olcek = 1;

        public override string ToString()
        {
            return "EPSG:" + Epsg + " - " + Ad;
        }

        private static List<KoordinatSistemi> _liste;

        /// <summary>Desteklenen sistemler: ITRF96 (TUREF) ve ED50'nin 3° ve 6° dilimleri ile coğrafi sistemler.</summary>
        public static List<KoordinatSistemi> Liste
        {
            get
            {
                if (_liste != null)
                    return _liste;

                var liste = new List<KoordinatSistemi>();
                // 3 derecelik dilimler: orta meridyen 27, 30, ..., 45.
                for (var i = 0; i < 7; i++)
                {
                    var dom = 27 + 3 * i;
                    liste.Add(new KoordinatSistemi { Epsg = 5253 + i, Ad = "ITRF96 (TUREF) / TM" + dom + " (3°)", Datum = KoordinatDatumu.Itrf96, OrtaMeridyen = dom });
                }
                // 6 derecelik dilimler (UTM 35-38): orta meridyen 27, 33, 39, 45.
                for (var i = 0; i < 4; i++)
                {
                    var dom = 27 + 6 * i;
                    liste.Add(new KoordinatSistemi { Epsg = 32635 + i, Ad = "WGS84 (ITRF) / UTM " + (35 + i) + "N (6°, DOM " + dom + ")", Datum = KoordinatDatumu.Wgs84, OrtaMeridyen = dom, Olcek = 0.9996 });
                }
                for (var i = 0; i < 7; i++)
                {
                    var dom = 27 + 3 * i;
                    liste.Add(new KoordinatSistemi { Epsg = 2319 + i, Ad = "ED50 / TM" + dom + " (3°)", Datum = KoordinatDatumu.Ed50, OrtaMeridyen = dom });
                }
                for (var i = 0; i < 4; i++)
                {
                    var dom = 27 + 6 * i;
                    liste.Add(new KoordinatSistemi { Epsg = 23035 + i, Ad = "ED50 / UTM " + (35 + i) + "N (6°, DOM " + dom + ")", Datum = KoordinatDatumu.Ed50, OrtaMeridyen = dom, Olcek = 0.9996 });
                }
                liste.Add(new KoordinatSistemi { Epsg = 5252, Ad = "ITRF96 (TUREF) coğrafi (enlem/boylam)", Datum = KoordinatDatumu.Itrf96, Cografi = true });
                liste.Add(new KoordinatSistemi { Epsg = 4326, Ad = "WGS84 coğrafi (enlem/boylam)", Datum = KoordinatDatumu.Wgs84, Cografi = true });
                liste.Add(new KoordinatSistemi { Epsg = 4230, Ad = "ED50 coğrafi (enlem/boylam)", Datum = KoordinatDatumu.Ed50, Cografi = true });
                return _liste = liste;
            }
        }

        /// <summary>EPSG koduna göre desteklenen sistemi döner; yoksa null.</summary>
        public static KoordinatSistemi Bul(int epsg)
        {
            return Liste.FirstOrDefault(s => s.Epsg == epsg);
        }

        /// <summary>
        /// Verilen koordinatın bu sistemde Türkiye sınırları içinde anlamlı olup olmadığına bakar (yanlış sistem
        /// seçimini yakalamak için kaba bir denetim).
        /// </summary>
        public bool TurkiyeIcindeMi(double dogu, double kuzey)
        {
            if (Cografi)
                return dogu >= 25 && dogu <= 45.5 && kuzey >= 35 && kuzey <= 43;
            return dogu >= 100000 && dogu <= 900000 && kuzey >= 3850000 && kuzey <= 4750000;
        }
    }

    /// <summary>
    /// ED50 ile ITRF96/WGS84 arasındaki 7 parametreli (Helmert, "Position Vector") datum dönüşümünün parametreleri.
    /// Varsayılan değerler EPSG:1784 "ED50 to WGS 84 (30)" (Türkiye geneli, yaklaşık 2 m doğruluk) dönüşümüdür.
    /// </summary>
    public class DatumParametreleri
    {
        public double Dx = -84.1, Dy = -101.8, Dz = -129.7;
        /// <summary>Dönüklükler (yay saniyesi).</summary>
        public double Rx, Ry, Rz = 0.468;
        /// <summary>Ölçek farkı (ppm).</summary>
        public double Ppm = 1.05;
    }

    /// <summary>İki koordinat sistemi arasında nokta dönüştürür. Kurulduktan sonra birden çok iş parçacığından çağrılabilir.</summary>
    public sealed class KoordinatDonusturucu
    {
        private const double DereceRadyan = Math.PI / 180;

        private sealed class Elipsoid
        {
            public readonly double A, E2, IkinciE2, B;
            // Krüger serisinin katsayıları (Transversal Merkator; dilim içinde milimetre altı doğruluk).
            private readonly double _yayA, _n;
            private readonly double[] _alfa = new double[3], _beta = new double[3], _delta = new double[3];

            public Elipsoid(double a, double tersBasiklik)
            {
                A = a;
                var f = 1 / tersBasiklik;
                E2 = f * (2 - f);
                B = a * (1 - f);
                IkinciE2 = E2 / (1 - E2);
                double n = f / (2 - f), n2 = n * n, n3 = n2 * n;
                _n = n;
                _yayA = a / (1 + n) * (1 + n2 / 4 + n2 * n2 / 64);
                _alfa[0] = n / 2 - 2 * n2 / 3 + 5 * n3 / 16; _alfa[1] = 13 * n2 / 48 - 3 * n3 / 5; _alfa[2] = 61 * n3 / 240;
                _beta[0] = n / 2 - 2 * n2 / 3 + 37 * n3 / 96; _beta[1] = n2 / 48 + n3 / 15; _beta[2] = 17 * n3 / 480;
                _delta[0] = 2 * n - 2 * n2 / 3 - 2 * n3; _delta[1] = 7 * n2 / 3 - 8 * n3 / 5; _delta[2] = 56 * n3 / 15;
            }

            private static double Atanh(double x)
            {
                return 0.5 * Math.Log((1 + x) / (1 - x));
            }

            /// <summary>Enlem/boylamdan (radyan) Transversal Merkator sağa/yukarı değerleri.</summary>
            public void Projeksiyon(double enlem, double boylam, KoordinatSistemi sistem, out double dogu, out double kuzey)
            {
                var c = 2 * Math.Sqrt(_n) / (1 + _n);
                var sinEnlem = Math.Sin(enlem);
                var t = Math.Sinh(Atanh(sinEnlem) - c * Atanh(c * sinEnlem));
                var boylamFarki = boylam - sistem.OrtaMeridyen * DereceRadyan;
                var ksi = Math.Atan2(t, Math.Cos(boylamFarki));
                var eta = Atanh(Math.Sin(boylamFarki) / Math.Sqrt(1 + t * t));

                double toplamKsi = ksi, toplamEta = eta;
                for (var j = 1; j <= 3; j++)
                {
                    toplamKsi += _alfa[j - 1] * Math.Sin(2 * j * ksi) * Math.Cosh(2 * j * eta);
                    toplamEta += _alfa[j - 1] * Math.Cos(2 * j * ksi) * Math.Sinh(2 * j * eta);
                }
                dogu = 500000 + sistem.Olcek * _yayA * toplamEta;
                kuzey = sistem.Olcek * _yayA * toplamKsi;
            }

            /// <summary>Transversal Merkator sağa/yukarı değerlerinden enlem/boylam (radyan).</summary>
            public void TersProjeksiyon(double dogu, double kuzey, KoordinatSistemi sistem, out double enlem, out double boylam)
            {
                var ksi = kuzey / (sistem.Olcek * _yayA);
                var eta = (dogu - 500000) / (sistem.Olcek * _yayA);
                double ksi1 = ksi, eta1 = eta;
                for (var j = 1; j <= 3; j++)
                {
                    ksi1 -= _beta[j - 1] * Math.Sin(2 * j * ksi) * Math.Cosh(2 * j * eta);
                    eta1 -= _beta[j - 1] * Math.Cos(2 * j * ksi) * Math.Sinh(2 * j * eta);
                }

                var chi = Math.Asin(Math.Sin(ksi1) / Math.Cosh(eta1));
                enlem = chi;
                for (var j = 1; j <= 3; j++)
                    enlem += _delta[j - 1] * Math.Sin(2 * j * chi);
                boylam = sistem.OrtaMeridyen * DereceRadyan + Math.Atan2(Math.Sinh(eta1), Math.Cos(ksi1));
            }
        }

        private static readonly Elipsoid Hayford = new Elipsoid(6378388, 297);                 // ED50 (International 1924)
        private static readonly Elipsoid Grs80 = new Elipsoid(6378137, 298.257222101);         // ITRF96
        private static readonly Elipsoid Wgs84 = new Elipsoid(6378137, 298.257223563);

        private static Elipsoid ElipsoidAl(KoordinatDatumu datum)
        {
            return datum == KoordinatDatumu.Ed50 ? Hayford : datum == KoordinatDatumu.Itrf96 ? Grs80 : Wgs84;
        }

        private readonly KoordinatSistemi _kaynak, _hedef;
        private readonly Elipsoid _kaynakElipsoid, _hedefElipsoid;
        // 0: datum değişmez, +1: ED50 -> ITRF96/WGS84, -1: ITRF96/WGS84 -> ED50.
        private readonly int _datumYonu;
        private readonly double _dx, _dy, _dz, _rx, _ry, _rz, _olcek;

        public KoordinatDonusturucu(KoordinatSistemi kaynak, KoordinatSistemi hedef, DatumParametreleri parametreler)
        {
            _kaynak = kaynak;
            _hedef = hedef;
            _kaynakElipsoid = ElipsoidAl(kaynak.Datum);
            _hedefElipsoid = ElipsoidAl(hedef.Datum);

            // ITRF96 ile WGS84 arasındaki fark bu işin hassasiyetinin çok altındadır; aynı kabul edilir.
            bool kaynakEd50 = kaynak.Datum == KoordinatDatumu.Ed50, hedefEd50 = hedef.Datum == KoordinatDatumu.Ed50;
            _datumYonu = kaynakEd50 == hedefEd50 ? 0 : kaynakEd50 ? 1 : -1;

            const double saniyeRadyan = Math.PI / 180 / 3600;
            _dx = parametreler.Dx; _dy = parametreler.Dy; _dz = parametreler.Dz;
            _rx = parametreler.Rx * saniyeRadyan; _ry = parametreler.Ry * saniyeRadyan; _rz = parametreler.Rz * saniyeRadyan;
            _olcek = 1 + parametreler.Ppm * 1e-6;
        }

        /// <summary>
        /// Noktayı kaynak sistemden hedef sisteme çevirir. Coğrafi sistemlerde <paramref name="dogu"/> boylam,
        /// <paramref name="kuzey"/> enlemdir (derece). Yalnızca yatay konum dönüştürülür; kot değişmez.
        /// </summary>
        public void Donustur(ref double dogu, ref double kuzey)
        {
            double enlem, boylam;
            if (_kaynak.Cografi)
            {
                boylam = dogu * DereceRadyan;
                enlem = kuzey * DereceRadyan;
            }
            else
            {
                _kaynakElipsoid.TersProjeksiyon(dogu, kuzey, _kaynak, out enlem, out boylam);
            }

            if (_datumYonu != 0)
            {
                // Elipsoit yüzeyindeki nokta yer merkezli dik koordinata çevrilir, Helmert dönüşümü uygulanır ve
                // hedef elipsoide geri dönülür.
                var e = _kaynakElipsoid;
                var n = e.A / Math.Sqrt(1 - e.E2 * Math.Sin(enlem) * Math.Sin(enlem));
                double x = n * Math.Cos(enlem) * Math.Cos(boylam), y = n * Math.Cos(enlem) * Math.Sin(boylam), z = n * (1 - e.E2) * Math.Sin(enlem);

                double x2, y2, z2;
                if (_datumYonu > 0)
                {
                    x2 = _dx + _olcek * (x - _rz * y + _ry * z);
                    y2 = _dy + _olcek * (_rz * x + y - _rx * z);
                    z2 = _dz + _olcek * (-_ry * x + _rx * y + z);
                }
                else
                {
                    double ax = (x - _dx) / _olcek, ay = (y - _dy) / _olcek, az = (z - _dz) / _olcek;
                    x2 = ax + _rz * ay - _ry * az;
                    y2 = -_rz * ax + ay + _rx * az;
                    z2 = _ry * ax - _rx * ay + az;
                }

                var h = _hedefElipsoid;
                var p = Math.Sqrt(x2 * x2 + y2 * y2);
                var teta = Math.Atan2(z2 * h.A, p * h.B);
                enlem = Math.Atan2(z2 + h.IkinciE2 * h.B * Math.Pow(Math.Sin(teta), 3), p - h.E2 * h.A * Math.Pow(Math.Cos(teta), 3));
                boylam = Math.Atan2(y2, x2);
            }

            if (_hedef.Cografi)
            {
                dogu = boylam / DereceRadyan;
                kuzey = enlem / DereceRadyan;
            }
            else
            {
                _hedefElipsoid.Projeksiyon(enlem, boylam, _hedef, out dogu, out kuzey);
            }
        }
    }
}
