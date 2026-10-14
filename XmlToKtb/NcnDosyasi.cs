using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>Nokta editöründeki bir satır: NCN düzeninde Y sağa, X yukarı değerdir.</summary>
    public class NoktaKaydi
    {
        public string Ad = "0";
        public double Y;
        public double X;
        public double Z;
        public string Kod = "";
        public string Pafta = "";
        public string Tabaka = "";

        /// <summary>Satırın çizimdeki karşılığı; dosyadan yüklenen ya da yeni eklenen satırda null'dır.</summary>
        public NoktaNesnesi Kaynak;
    }

    /// <summary>
    /// NetCAD nokta dosyasını (NCN) okur/yazar. Satır biçimi: nokta adı, Y, X, Z, 0 ve tırnak içinde üç alan;
    /// tırnaklı alanlardan ilki (10 karakter) kod, üçüncüsü (6 karakter) pafta olarak kullanılır.
    /// </summary>
    public static class NcnDosyasi
    {
        private static readonly Regex TirnakliAlan = new Regex("\"([^\"]*)\"", RegexOptions.Compiled);

        public static bool SayiOku(string metin, out double sayi)
        {
            return double.TryParse((metin ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out sayi);
        }

        public static List<NoktaKaydi> Oku(string dosyaYolu)
        {
            var noktalar = new List<NoktaKaydi>();
            foreach (var satir in File.ReadLines(dosyaYolu, Encoding.Default))
            {
                // Tırnaklı alanlar boşluk içerebildiği için önce onlar ayrılır, kalan kısım boşluklardan bölünür.
                var tirnakBasi = satir.IndexOf('"');
                var sayisalKisim = tirnakBasi >= 0 ? satir.Substring(0, tirnakBasi) : satir;
                var parcalar = sayisalKisim.Split(new[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (parcalar.Length < 3 || !SayiOku(parcalar[1], out var y) || !SayiOku(parcalar[2], out var x))
                    continue;

                var nokta = new NoktaKaydi { Ad = parcalar[0], Y = y, X = x };
                if (parcalar.Length >= 4)
                    SayiOku(parcalar[3], out nokta.Z);

                var alanlar = TirnakliAlan.Matches(satir);
                if (alanlar.Count >= 1)
                    nokta.Kod = alanlar[0].Groups[1].Value.Trim();
                if (alanlar.Count >= 3)
                    nokta.Pafta = alanlar[2].Groups[1].Value.Trim();
                noktalar.Add(nokta);
            }
            return noktalar;
        }

        public static void Yaz(IEnumerable<NoktaKaydi> noktalar, string dosyaYolu)
        {
            using (var yazici = new StreamWriter(dosyaYolu, false, Encoding.Default))
            {
                foreach (var nokta in noktalar)
                {
                    yazici.WriteLine(
                        (nokta.Ad ?? "").PadLeft(10) + Sayi(nokta.Y).PadLeft(13) + Sayi(nokta.X).PadLeft(14) + Sayi(nokta.Z).PadLeft(13) +
                        " 0 \"" + Alan(nokta.Kod, 10) + "\" \"  \" \"" + Alan(nokta.Pafta, 6) + "\"");
                }
            }
        }

        private static string Sayi(double deger)
        {
            return deger.ToString("0.000", CultureInfo.InvariantCulture);
        }

        // Tırnak işareti alanı bozacağı için çıkarılır; kısa metin boşlukla tamamlanır.
        private static string Alan(string metin, int genislik)
        {
            return (metin ?? "").Replace("\"", "").PadRight(genislik);
        }
    }
}
