using System.Collections.Generic;

namespace XmlToKtb.Projeksiyon
{
    /// <summary>Türkiye'de kullanılan projeksiyon/datum sistemleri ve coğrafi (enlem/boylam) seçenekleri.</summary>
    public enum ProjeksiyonSistemi
    {
        Tanimsiz,
        Ed50,
        Itrf96,
        UtmWgs84,
        /// <summary>WGS84/ITRF96 datumunda enlem/boylam.</summary>
        Cografi,
        /// <summary>ED-50 datumunda enlem/boylam.</summary>
        CografiEd50
    }

    /// <summary>
    /// Bir dilime (merkezi meridyene) ait bilgiler; dropdown'da gösterilecek etiketle birlikte. Ölçek katsayısı
    /// 3° dilimlerde 1, 6° (UTM) dilimlerde 0.9996'dır. EPSG kodu bilinmiyorsa 0'dır.
    /// </summary>
    public struct DilimBilgisi
    {
        public string Etiket { get; }
        public double MerkeziMeridyenDerece { get; }
        public double SagaKaydirma { get; }
        public double OlcekFaktoru { get; }
        public int Epsg { get; }

        public DilimBilgisi(string etiket, double merkeziMeridyenDerece, double sagaKaydirma, double olcekFaktoru, int epsg)
        {
            Etiket = etiket;
            MerkeziMeridyenDerece = merkeziMeridyenDerece;
            SagaKaydirma = sagaKaydirma;
            OlcekFaktoru = olcekFaktoru;
            Epsg = epsg;
        }

        public override string ToString() => Etiket;
    }

    public static class ProjeksiyonBilgisi
    {
        /// <summary>
        /// ITRF96 (TUREF) dilimleri: 3° TM (EPSG:5253-5259), dilim numarası sağa değerinin başına yazılan 3°
        /// Gauss-Krüger (EPSG:5269-5275) ve 6° UTM.
        /// </summary>
        public static readonly DilimBilgisi[] Itrf96Dilimleri = DilimleriOlustur(5253, 5269, 0);

        /// <summary>
        /// ED-50 dilimleri: 3° TM (EPSG:2319-2325), dilim numarası sağa değerinin başına yazılan 3° Gauss-Krüger
        /// (EPSG:2206-2212) ve 6° UTM (EPSG:23035-23038).
        /// </summary>
        public static readonly DilimBilgisi[] Ed50Dilimleri = DilimleriOlustur(2319, 2206, 23035);

        /// <summary>WGS84 6° UTM dilimleri (Türkiye'yi kapsayan 35N-38N; EPSG:32635-32638).</summary>
        public static readonly DilimBilgisi[] Utm6DereceDilimleri = UtmDilimleriniOlustur(32635).ToArray();

        // 3° dilimler (orta meridyen 27, 30, ..., 45; dilim no 9-15), ardından 6° UTM dilimleri.
        private static DilimBilgisi[] DilimleriOlustur(int tmIlkEpsg, int dilimNoluIlkEpsg, int utmIlkEpsg)
        {
            var dilimler = new List<DilimBilgisi>();
            for (var i = 0; i < 7; i++)
            {
                var dom = 27 + 3 * i;
                dilimler.Add(new DilimBilgisi("TM" + dom + " - 3° (EPSG:" + (tmIlkEpsg + i) + ")", dom, 500_000, 1.0, tmIlkEpsg + i));
            }
            for (var i = 0; i < 7; i++)
            {
                int dom = 27 + 3 * i, dilimNo = 9 + i;
                dilimler.Add(new DilimBilgisi("TM" + dom + " - 3°, sağa değeri " + dilimNo + " önekli (EPSG:" + (dilimNoluIlkEpsg + i) + ")",
                    dom, dilimNo * 1_000_000 + 500_000, 1.0, dilimNoluIlkEpsg + i));
            }
            dilimler.AddRange(UtmDilimleriniOlustur(utmIlkEpsg));
            return dilimler.ToArray();
        }

        private static List<DilimBilgisi> UtmDilimleriniOlustur(int ilkEpsg)
        {
            var dilimler = new List<DilimBilgisi>();
            for (var i = 0; i < 4; i++)
            {
                var dom = 27 + 6 * i;
                var epsg = ilkEpsg == 0 ? 0 : ilkEpsg + i;
                dilimler.Add(new DilimBilgisi("UTM " + (35 + i) + "N - 6°, DOM " + dom + (epsg != 0 ? " (EPSG:" + epsg + ")" : ""), dom, 500_000, 0.9996, epsg));
            }
            return dilimler;
        }

        public static IReadOnlyList<DilimBilgisi> DilimleriGetir(ProjeksiyonSistemi sistem)
        {
            switch (sistem)
            {
                case ProjeksiyonSistemi.Itrf96:
                    return Itrf96Dilimleri;
                case ProjeksiyonSistemi.Ed50:
                    return Ed50Dilimleri;
                case ProjeksiyonSistemi.UtmWgs84:
                    return Utm6DereceDilimleri;
                default:
                    return new DilimBilgisi[0];
            }
        }

        /// <summary>Sistem ED-50 datumunda mı (değilse ITRF96/WGS84).</summary>
        public static bool Ed50DatumuMu(ProjeksiyonSistemi sistem)
        {
            return sistem == ProjeksiyonSistemi.Ed50 || sistem == ProjeksiyonSistemi.CografiEd50;
        }

        /// <summary>Sistem enlem/boylam (derece) mi.</summary>
        public static bool CografiMi(ProjeksiyonSistemi sistem)
        {
            return sistem == ProjeksiyonSistemi.Cografi || sistem == ProjeksiyonSistemi.CografiEd50;
        }

        public static Elipsoid ElipsoidGetir(ProjeksiyonSistemi sistem)
        {
            return Ed50DatumuMu(sistem) ? Elipsoid.Hayford1924 : Elipsoid.Wgs84;
        }

        public static string GoruntuAdiGetir(ProjeksiyonSistemi sistem)
        {
            switch (sistem)
            {
                case ProjeksiyonSistemi.Ed50: return "ED-50";
                case ProjeksiyonSistemi.Itrf96: return "ITRF96 (TUREF)";
                case ProjeksiyonSistemi.UtmWgs84: return "UTM WGS84";
                case ProjeksiyonSistemi.Cografi: return "Coğrafi WGS84 (Enlem/Boylam)";
                case ProjeksiyonSistemi.CografiEd50: return "Coğrafi ED-50 (Enlem/Boylam)";
                default: return "Tanımsız";
            }
        }

        /// <summary>Sistemin dilimi ve EPSG koduyla birlikte tam adı (durum çubuğunda gösterilir).</summary>
        public static string TamAdGetir(ProjeksiyonSistemi sistem, DilimBilgisi? dilim)
        {
            var ad = GoruntuAdiGetir(sistem);
            if (sistem == ProjeksiyonSistemi.Cografi)
                return ad + " (EPSG:4326)";
            if (sistem == ProjeksiyonSistemi.CografiEd50)
                return ad + " (EPSG:4230)";
            return dilim != null ? ad + " / " + dilim.Value.Etiket : ad;
        }

        /// <summary>Sistem bir projeksiyon dilimi (merkezi meridyen) gerektiriyor mu (Coğrafi ve Tanımsız gerektirmez).</summary>
        public static bool DilimGereklidir(ProjeksiyonSistemi sistem)
        {
            return sistem == ProjeksiyonSistemi.Ed50 || sistem == ProjeksiyonSistemi.Itrf96 || sistem == ProjeksiyonSistemi.UtmWgs84;
        }
    }
}
