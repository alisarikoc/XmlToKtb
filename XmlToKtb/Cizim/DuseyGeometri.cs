using System;
using System.Collections.Generic;

namespace XmlToKtb.Cizim
{
    /// <summary>Düşey some noktasındaki düşey kurbun türü.</summary>
    public enum DuseyKurpTipi
    {
        Yok,
        Parabol,
        Daire
    }

    /// <summary>
    /// Güzergahın düşey geometrisindeki bir some noktası (PVI): kilometresi, kotu ve o noktadaki düşey kurp.
    /// İki some arasında eğim sabittir; some noktasında, verilmişse, eğimler düşey kurpla birleştirilir.
    /// </summary>
    public class DuseyNokta
    {
        public double Km;
        public double Kot;
        public DuseyKurpTipi Tip = DuseyKurpTipi.Yok;
        /// <summary>Düşey kurbun yataydaki boyu (m). Kurp, some noktasının iki yanına yarı yarıya uzanır.</summary>
        public double Uzunluk;
        /// <summary>Dairesel düşey kurbun yarıçapı (m). Parabolde kullanılmaz.</summary>
        public double Yaricap;

        public DuseyNokta Kopyala()
        {
            return new DuseyNokta { Km = Km, Kot = Kot, Tip = Tip, Uzunluk = Uzunluk, Yaricap = Yaricap };
        }
    }

    /// <summary>Düşey geometri hesapları: eğimler ve verilen kilometredeki proje (kırmızı) kotu.</summary>
    public static class DuseyGeometri
    {
        /// <summary>i. some ile (i+1). some arasındaki eğim (m/m). Geçersiz aralıkta 0.</summary>
        public static double Egim(IList<DuseyNokta> noktalar, int i)
        {
            if (i < 0 || i + 1 >= noktalar.Count)
                return 0;
            var mesafe = noktalar[i + 1].Km - noktalar[i].Km;
            return Math.Abs(mesafe) < 1e-9 ? 0 : (noktalar[i + 1].Kot - noktalar[i].Kot) / mesafe;
        }

        /// <summary>i. somedeki eğim farkı (çıkış eğimi - giriş eğimi). Uçtaki somelerde 0.</summary>
        public static double EgimFarki(IList<DuseyNokta> noktalar, int i)
        {
            return i <= 0 || i >= noktalar.Count - 1 ? 0 : Egim(noktalar, i) - Egim(noktalar, i - 1);
        }

        // Uçtaki somelerde kurp olmaz; aradakilerde verilen kurp boyu geçerlidir.
        private static double KurpBoyu(IList<DuseyNokta> noktalar, int i)
        {
            return i <= 0 || i >= noktalar.Count - 1 || noktalar[i].Tip == DuseyKurpTipi.Yok ? 0 : Math.Max(0, noktalar[i].Uzunluk);
        }

        /// <summary>
        /// Verilen kilometredeki proje kotu; kilometre düşey geometrinin dışındaysa null. Düşey kurp içinde kot,
        /// teğetten kurp ucuna olan uzaklığın karesiyle orantılı farkla hesaplanır (parabol; dairesel kurp için de
        /// pratikte aynı sonucu verir).
        /// </summary>
        public static double? KotAl(IList<DuseyNokta> noktalar, double km)
        {
            if (noktalar.Count < 2 || km < noktalar[0].Km - 1e-9 || km > noktalar[noktalar.Count - 1].Km + 1e-9)
                return null;

            for (var i = 0; i + 1 < noktalar.Count; i++)
            {
                if (km > noktalar[i + 1].Km && i + 2 < noktalar.Count)
                    continue;

                var egim = Egim(noktalar, i);
                var kot = noktalar[i].Kot + egim * (km - noktalar[i].Km);

                // Aralığın başındaki somenin kurbu: çıkış teğeti üzerindeyiz, kurbun bitiş ucuna uzaklık alınır.
                var basBoy = KurpBoyu(noktalar, i);
                if (basBoy > 0)
                {
                    var uzaklik = noktalar[i].Km + basBoy / 2 - km;
                    if (uzaklik > 0)
                        kot += EgimFarki(noktalar, i) / (2 * basBoy) * uzaklik * uzaklik;
                }

                // Aralığın sonundaki somenin kurbu: giriş teğeti üzerindeyiz, kurbun başlangıç ucuna uzaklık alınır.
                var sonBoy = KurpBoyu(noktalar, i + 1);
                if (sonBoy > 0)
                {
                    var uzaklik = km - (noktalar[i + 1].Km - sonBoy / 2);
                    if (uzaklik > 0)
                        kot += EgimFarki(noktalar, i + 1) / (2 * sonBoy) * uzaklik * uzaklik;
                }

                return kot;
            }
            return null;
        }
    }
}
