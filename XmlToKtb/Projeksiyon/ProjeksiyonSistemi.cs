using System.Collections.Generic;

namespace XmlToKtb.Projeksiyon
{
    /// <summary>Türkiye'de kullanılan projeksiyon/datum sistemleri ve coğrafi (enlem/boylam) seçeneği.</summary>
    public enum ProjeksiyonSistemi
    {
        Tanimsiz,
        Ed50,
        Itrf96,
        UtmWgs84,
        Cografi
    }

    /// <summary>Bir dilime (merkezi meridyene) ait bilgiler; dropdown'da gösterilecek etiketle birlikte.</summary>
    public struct DilimBilgisi
    {
        public string Etiket { get; }
        public double MerkeziMeridyenDerece { get; }
        public double SagaKaydirma { get; }

        public DilimBilgisi(string etiket, double merkeziMeridyenDerece, double sagaKaydirma)
        {
            Etiket = etiket;
            MerkeziMeridyenDerece = merkeziMeridyenDerece;
            SagaKaydirma = sagaKaydirma;
        }

        public override string ToString() => Etiket;
    }

    public static class ProjeksiyonBilgisi
    {
        /// <summary>ITRF96 (TUREF) Gauss-Krüger 3° dilimleri; standart false easting (500.000), dilim numarası sadece etikette.</summary>
        public static readonly DilimBilgisi[] Itrf96Dilimleri =
        {
            new DilimBilgisi("27° (Dilim 9)", 27, 500_000),
            new DilimBilgisi("30° (Dilim 10)", 30, 500_000),
            new DilimBilgisi("33° (Dilim 11)", 33, 500_000),
            new DilimBilgisi("36° (Dilim 12)", 36, 500_000),
            new DilimBilgisi("39° (Dilim 13)", 39, 500_000),
            new DilimBilgisi("42° (Dilim 14)", 42, 500_000),
            new DilimBilgisi("45° (Dilim 15)", 45, 500_000)
        };

        /// <summary>ED-50 / UTM WGS84 için 6° UTM dilimleri (Türkiye'yi kapsayan 35N-38N).</summary>
        public static readonly DilimBilgisi[] Utm6DereceDilimleri =
        {
            new DilimBilgisi("27° (UTM 35N)", 27, 500_000),
            new DilimBilgisi("33° (UTM 36N)", 33, 500_000),
            new DilimBilgisi("39° (UTM 37N)", 39, 500_000),
            new DilimBilgisi("45° (UTM 38N)", 45, 500_000)
        };

        public static IReadOnlyList<DilimBilgisi> DilimleriGetir(ProjeksiyonSistemi sistem)
        {
            switch (sistem)
            {
                case ProjeksiyonSistemi.Itrf96:
                    return Itrf96Dilimleri;
                case ProjeksiyonSistemi.Ed50:
                case ProjeksiyonSistemi.UtmWgs84:
                    return Utm6DereceDilimleri;
                default:
                    return new DilimBilgisi[0];
            }
        }

        public static Elipsoid ElipsoidGetir(ProjeksiyonSistemi sistem)
        {
            return sistem == ProjeksiyonSistemi.Ed50 ? Elipsoid.Hayford1924 : Elipsoid.Wgs84;
        }

        public static double OlcekFaktoruGetir(ProjeksiyonSistemi sistem)
        {
            return sistem == ProjeksiyonSistemi.Itrf96 ? 1.0 : 0.9996;
        }

        public static string GoruntuAdiGetir(ProjeksiyonSistemi sistem)
        {
            switch (sistem)
            {
                case ProjeksiyonSistemi.Ed50: return "ED-50";
                case ProjeksiyonSistemi.Itrf96: return "ITRF96 (TUREF)";
                case ProjeksiyonSistemi.UtmWgs84: return "UTM WGS84";
                case ProjeksiyonSistemi.Cografi: return "Coğrafi (Enlem/Boylam)";
                default: return "Tanımsız";
            }
        }

        /// <summary>Sistem bir projeksiyon dilimi (merkezi meridyen) gerektiriyor mu (Coğrafi ve Tanımsız gerektirmez).</summary>
        public static bool DilimGereklidir(ProjeksiyonSistemi sistem)
        {
            return sistem == ProjeksiyonSistemi.Ed50 || sistem == ProjeksiyonSistemi.Itrf96 || sistem == ProjeksiyonSistemi.UtmWgs84;
        }
    }
}
