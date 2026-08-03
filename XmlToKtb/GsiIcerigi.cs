using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace XmlToKtb
{
    /// <summary>
    /// Leica GSI8 formatındaki (16 karakterlik sabit genişlikte kelime blokları: 2 haneli Word ID + 4 haneli
    /// bilgi kodu + işaret + 8 haneli değer) nivelman kayıt dosyasını, klasik "Geri/İleri okuma-mesafe-Kot"
    /// nivelman tablosuna çevirir.
    ///
    /// Tablo, her nokta için bir satır olacak şekilde, o noktaya yapılan Geri (backsight) okuma/mesafesini VE
    /// İleri (foresight) okuma/mesafesini birlikte gösterir — bunlar genelde FARKLI istasyonlardan gelir. Bu
    /// eşleştirme İSİM ile değil, dosyadaki SIRA ile yapılır (bir istasyonun İleri hedefi, bir sonraki
    /// istasyonun Geri hedefiyle aynı fiziksel noktadır) — böylece gerçek nivelmanlarda sık kullanılan, tekrar
    /// eden genel ara nokta adları ("RS" gibi, birden fazla farklı fiziksel noktada kullanılabilir) yanlışlıkla
    /// birbirine karışmaz.
    ///
    /// Format çözümü (örnek dosya ve referans çıktıyla çapraz doğrulanmıştır):
    /// - WI 11: Nokta No (metin).
    /// - WI 32: Kayar (staff) okuma, metre, 4 ondalık.
    /// - WI 33: Yatay mesafe, metre, 4 ondalık; bilgi kodunun İLK hanesi atış türünü belirtir:
    ///          1/5 = Geri (backsight), 2/6 = İleri (foresight). Cihaz her istasyonda çift ölçüm
    ///          (Geri-İleri-İleri-Geri sırayla) yapar; bu iki tekrar ortalanır.
    /// - WI 83: Cihazın kendi hesapladığı kümülatif Kot (metre, 6 ondalık); o satırdaki (yeni belirlenen)
    ///          noktaya aittir ve bir istasyonun bittiğini işaret eder.
    /// </summary>
    public static class GsiIcerigi
    {
        private const string NoktaKodu = "11";
        private const string OkumaKodu = "32";
        private const string MesafeKodu = "33";
        private const string KotKodu = "83";

        private class Istasyon
        {
            public string Ad;
            public readonly List<double> GeriOkuma = new List<double>();
            public readonly List<double> GeriMesafe = new List<double>();
            public readonly List<double> IleriOkuma = new List<double>();
            public readonly List<double> IleriMesafe = new List<double>();
            public double Kot;
        }

        public static string Excellistesi(List<Array> gsiData)
        {
            // 1. adım: dosyayı sırayla gezip her "Kot" satırında bir istasyon kaydı kapatılır. İstasyon[i].Ad,
            // o istasyonda YENİ belirlenen noktadır; Geri* alanları bu istasyonun (bir önceki, bilinen noktaya
            // yaptığı) backsight okuma/mesafesi, İleri* alanları ise Ad'ın kendisine yapılan foresight
            // okuma/mesafesidir.
            var istasyonlar = new List<Istasyon>();
            var mevcut = new Istasyon();

            foreach (var satir in gsiData)
            {
                string noktaNo = null;
                double? okuma = null;
                double? mesafe = null;
                var subKod = '\0';
                double? kot = null;
                var kotVarMi = false;

                foreach (var ham in satir)
                {
                    var kelime = Convert.ToString(ham);
                    if (!KelimeyiCoz(kelime, out var wi, out var bilgiIlkKarakter, out var deger, out var noktaMetni))
                        continue;

                    switch (wi)
                    {
                        case NoktaKodu:
                            noktaNo = noktaMetni;
                            break;
                        case OkumaKodu:
                            okuma = deger;
                            break;
                        case MesafeKodu:
                            mesafe = deger;
                            subKod = bilgiIlkKarakter;
                            break;
                        case KotKodu:
                            kot = deger;
                            kotVarMi = true;
                            break;
                    }
                }

                if (noktaNo != null && okuma.HasValue && mesafe.HasValue)
                {
                    if (subKod == '1' || subKod == '5')
                    {
                        mevcut.GeriOkuma.Add(okuma.Value);
                        mevcut.GeriMesafe.Add(mesafe.Value);
                    }
                    else if (subKod == '2' || subKod == '6')
                    {
                        mevcut.IleriOkuma.Add(okuma.Value);
                        mevcut.IleriMesafe.Add(mesafe.Value);
                    }
                }

                if (kotVarMi && noktaNo != null)
                {
                    mevcut.Ad = noktaNo;
                    mevcut.Kot = kot.Value;
                    istasyonlar.Add(mevcut);
                    mevcut = new Istasyon();
                }
            }

            // 2. adım: ardışık istasyonları SIRAYLA birleştirerek nokta satırlarını oluştur — istasyon[i]'nin
            // İleri verisi ile istasyon[i+1]'in Geri verisi aynı fiziksel noktaya ait olduğu için birleştirilir.
            var satirlar = new StringBuilder();
            satirlar.AppendLine("Nokta No,Geri Mesafe,İleri Mesafe,Geri Okuma,İleri Okuma,Kot");

            for (var i = 0; i < istasyonlar.Count; i++)
            {
                var buradaki = istasyonlar[i];
                var geriKaynagi = i + 1 < istasyonlar.Count ? istasyonlar[i + 1] : null;

                satirlar.AppendLine(string.Join(",",
                    buradaki.Ad,
                    Bicimlendir(geriKaynagi != null ? Ortalama(geriKaynagi.GeriMesafe) : null),
                    Bicimlendir(Ortalama(buradaki.IleriMesafe)),
                    Bicimlendir(geriKaynagi != null ? Ortalama(geriKaynagi.GeriOkuma) : null),
                    Bicimlendir(Ortalama(buradaki.IleriOkuma)),
                    buradaki.Kot.ToString("F6", CultureInfo.InvariantCulture)));
            }

            return satirlar.ToString();
        }

        private static string Bicimlendir(double? deger)
        {
            return deger.HasValue ? deger.Value.ToString("F6", CultureInfo.InvariantCulture) : "0";
        }

        private static double? Ortalama(List<double> degerler)
        {
            if (degerler.Count == 0)
                return null;

            var toplam = 0.0;
            foreach (var d in degerler)
                toplam += d;
            return toplam / degerler.Count;
        }

        /// <summary>
        /// Tek bir GSI kelime bloğunu (WI + 4 haneli bilgi kodu + işaret + 8 haneli değer, toplam 15 karakter)
        /// çözer. Yalnızca ilgilendiğimiz kelimeler (11/32/33/83) için true döner; diğerleri atlanır.
        /// </summary>
        private static bool KelimeyiCoz(string kelime, out string wi, out char bilgiIlkKarakter, out double deger, out string noktaMetni)
        {
            wi = null;
            bilgiIlkKarakter = '\0';
            deger = 0;
            noktaMetni = null;

            if (string.IsNullOrEmpty(kelime) || kelime.Length < 15)
                return false;

            wi = kelime.Substring(0, 2);
            var bilgiKodu = kelime.Substring(2, 4);
            bilgiIlkKarakter = bilgiKodu.Length > 0 ? bilgiKodu[0] : '\0';
            var isaret = kelime[6];
            var hamDeger = kelime.Substring(7);

            if (wi == NoktaKodu)
            {
                noktaMetni = hamDeger.TrimStart('0');
                if (noktaMetni.Length == 0)
                    noktaMetni = "0";
                return true;
            }

            if (wi != OkumaKodu && wi != MesafeKodu && wi != KotKodu)
                return false;

            if (!long.TryParse(hamDeger, NumberStyles.None, CultureInfo.InvariantCulture, out var sayi))
                return false;

            // Mesafe (WI 33) ve Okuma (WI 32) 4 ondalık; Kot (WI 83) 6 ondalık olarak yazılır.
            var ondalikBasamak = wi == KotKodu ? 6 : 4;
            var sayisalDeger = sayi / Math.Pow(10, ondalikBasamak);
            if (isaret == '-')
                sayisalDeger = -sayisalDeger;

            deger = sayisalDeger;
            return true;
        }
    }
}
