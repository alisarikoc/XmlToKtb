using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Yüzeylerin üzerine çizilen eş yükseklik eğrilerinin ayarları. Bütün yüzeyler için ortaktır; ana (major) eğriler
    /// ile ara (minor) eğrilerin kot aralığı ve rengi ayrı ayrı verilir.
    /// </summary>
    public static class EsYukseklikAyarlari
    {
        public static bool MajorGoster { get; private set; } = true;
        public static double MajorAralik { get; private set; } = 10.0;
        public static Color MajorRenk { get; private set; } = Color.Red;

        public static bool MinorGoster { get; private set; } = true;
        public static double MinorAralik { get; private set; } = 2.0;
        public static Color MinorRenk { get; private set; } = Color.Gray;

        /// <summary>Ayarlar her değiştiğinde artar; yüzeyler çizim önbelleklerinin güncelliğini bununla denetler.</summary>
        public static int Surum { get; private set; }

        public static void MajorAyarla(bool goster, double aralik, Color renk)
        {
            MajorGoster = goster;
            MajorAralik = aralik;
            MajorRenk = renk;
            Surum++;
        }

        public static void MinorAyarla(bool goster, double aralik, Color renk)
        {
            MinorGoster = goster;
            MinorAralik = aralik;
            MinorRenk = renk;
            Surum++;
        }
    }
}
