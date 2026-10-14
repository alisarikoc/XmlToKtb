using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace XmlToKtb.Cizim
{
    /// <summary>KSE dosyasındaki bir kesit noktası ("0 mesafe kot kod" satırı).</summary>
    public class KseNokta
    {
        public double Mesafe;
        public double Kot;
        public string Kod;
    }

    /// <summary>KSE dosyasındaki tek bir enkesit ("1 no km ..." satırı ve onu izleyen noktalar).</summary>
    public class KseKesit
    {
        /// <summary>Başlık satırının "1"den sonraki kısmı (kesit no, km, varsa kırmızı kot vb.); aynen geri yazılır.</summary>
        public string Baslik;
        public string No;
        public string Kilometre;
        /// <summary>Kilometrenin sayısal değeri (okunamadıysa NaN); dosyalar arası kesit eşleştirmede kullanılır.</summary>
        public double Km = double.NaN;
        public string Aciklama;
        public List<KseNokta> Noktalar = new List<KseNokta>();
        /// <summary>Kesitin çizim ekranındaki karşılığı; ardışık noktaları birleştiren çizgiler.</summary>
        public readonly List<CizgiNesnesi> Cizgiler = new List<CizgiNesnesi>();

        /// <summary>Noktalardan, ardışık noktaları birleştiren çizim çizgilerini kurar.</summary>
        public void CizgileriOlustur()
        {
            Cizgiler.Clear();
            for (var i = 0; i < Noktalar.Count - 1; i++)
            {
                Cizgiler.Add(new CizgiNesnesi
                {
                    BaslangicX = Noktalar[i].Mesafe,
                    BaslangicY = Noktalar[i].Kot,
                    BitisX = Noktalar[i + 1].Mesafe,
                    BitisY = Noktalar[i + 1].Kot
                });
            }
        }

        /// <summary>
        /// Kesit çizim ekranından kaldırılmadan/kaydedilmeden önce çağrılır: ekrandaki çizgileri soldan sağa tek
        /// parça (polyline) olarak sıralar ve kesitin noktalarını bu sıradan yeniden kurar. Çizgiler tek parça
        /// değilse (kopuk, dallanan ya da kapalı) hiçbir şeyi değiştirmeden false döner.
        /// </summary>
        public bool CizimdenGuncelle(IEnumerable<CizimNesnesi> cizimdekiNesneler)
        {
            var cizimdekiCizgiler = cizimdekiNesneler.OfType<CizgiNesnesi>().ToList();

            // Tek noktalı kesitin çizgisi olmaz; ona dokunulmadıysa okunduğu gibi kalır.
            if (Cizgiler.Count == 0 && cizimdekiCizgiler.Count == 0)
                return true;

            // Önce kesitin mevcut sırası, ardından yeni çizilenler: değişmemiş kesitin nokta sırası korunur.
            var cizimdekiKume = new HashSet<CizgiNesnesi>(cizimdekiCizgiler);
            var adaylar = Cizgiler.Where(cizimdekiKume.Contains).ToList();
            var eskiKume = new HashSet<CizgiNesnesi>(adaylar);
            adaylar.AddRange(cizimdekiCizgiler.Where(c => !eskiKume.Contains(c)));

            if (!TekParcaOlarakSirala(adaylar, out var sirali))
                return false;

            Cizgiler.Clear();
            Cizgiler.AddRange(sirali);

            var noktalar = new List<KseNokta>();
            for (var i = 0; i < Cizgiler.Count; i++)
            {
                if (i == 0)
                    noktalar.Add(new KseNokta { Mesafe = Cizgiler[i].BaslangicX, Kot = Cizgiler[i].BaslangicY });
                noktalar.Add(new KseNokta { Mesafe = Cizgiler[i].BitisX, Kot = Cizgiler[i].BitisY });
            }

            // Nokta sayısı değişmediyse kodlar sırayla korunur. Değiştiyse (ör. nokta silindi ya da eklendi) kodlar,
            // koordinatı aynı kalan noktalara taşınır; yeni noktaların kodu boş kalır.
            if (noktalar.Count == Noktalar.Count)
            {
                for (var i = 0; i < noktalar.Count; i++)
                    noktalar[i].Kod = Noktalar[i].Kod;
            }
            else
            {
                foreach (var nokta in noktalar)
                {
                    var eski = Noktalar.FirstOrDefault(n => UclarAyniMi(n.Mesafe, n.Kot, nokta.Mesafe, nokta.Kot));
                    if (eski != null)
                        nokta.Kod = eski.Kod;
                }
            }

            Noktalar = noktalar;
            return true;
        }

        /// <summary>
        /// Çizgileri uç uca tek bir zincir halinde sıralar; gerekirse çizgilerin yönünü çevirir ve birleşen uçları
        /// birebir aynı koordinata getirir. Zincir sol uçtan (küçük mesafe) başlar. Tek parça değilse false döner.
        /// </summary>
        public static bool TekParcaOlarakSirala(List<CizgiNesnesi> cizgiler, out List<CizgiNesnesi> sirali)
        {
            sirali = new List<CizgiNesnesi>();
            if (cizgiler.Count == 0)
                return true;

            var zatenSirali = true;
            for (var i = 1; i < cizgiler.Count && zatenSirali; i++)
                zatenSirali = UclarAyniMi(cizgiler[i - 1].BitisX, cizgiler[i - 1].BitisY, cizgiler[i].BaslangicX, cizgiler[i].BaslangicY);

            if (zatenSirali)
            {
                sirali.AddRange(cizgiler);
            }
            else
            {
                // Uç noktaları düğüm, çizgileri kenar kabul eden grafikte tek bir açık yol aranır.
                var dugumler = new List<double[]>();
                int DugumNo(double x, double y)
                {
                    for (var k = 0; k < dugumler.Count; k++)
                    {
                        if (UclarAyniMi(dugumler[k][0], dugumler[k][1], x, y))
                            return k;
                    }
                    dugumler.Add(new[] { x, y });
                    return dugumler.Count - 1;
                }

                var kenarlar = new List<int[]>();
                var kenarCizgileri = new List<CizgiNesnesi>();
                foreach (var cizgi in cizgiler)
                {
                    var a = DugumNo(cizgi.BaslangicX, cizgi.BaslangicY);
                    var b = DugumNo(cizgi.BitisX, cizgi.BitisY);
                    if (a == b)
                        continue; // sıfır boylu çizgi zincire katkı vermez
                    kenarlar.Add(new[] { a, b });
                    kenarCizgileri.Add(cizgi);
                }

                var derece = new int[dugumler.Count];
                foreach (var kenar in kenarlar)
                {
                    derece[kenar[0]]++;
                    derece[kenar[1]]++;
                }

                var uclar = new List<int>();
                for (var k = 0; k < derece.Length; k++)
                {
                    if (derece[k] == 0 || derece[k] > 2)
                        return false;
                    if (derece[k] == 1)
                        uclar.Add(k);
                }

                if (uclar.Count != 2)
                    return false;

                var guncel = dugumler[uclar[1]][0] < dugumler[uclar[0]][0] ? uclar[1] : uclar[0];
                var kullanildi = new bool[kenarlar.Count];
                while (true)
                {
                    var bulunan = -1;
                    for (var k = 0; k < kenarlar.Count; k++)
                    {
                        if (!kullanildi[k] && (kenarlar[k][0] == guncel || kenarlar[k][1] == guncel))
                        {
                            bulunan = k;
                            break;
                        }
                    }

                    if (bulunan < 0)
                        break;

                    kullanildi[bulunan] = true;
                    var cizgi = kenarCizgileri[bulunan];
                    if (kenarlar[bulunan][0] != guncel)
                    {
                        YonCevir(cizgi);
                        guncel = kenarlar[bulunan][0];
                    }
                    else
                    {
                        guncel = kenarlar[bulunan][1];
                    }
                    sirali.Add(cizgi);
                }

                // Kullanılmayan kenar kaldıysa ayrı (kapalı) bir parça daha var demektir.
                if (sirali.Count != kenarlar.Count)
                    return false;
            }

            for (var i = 1; i < sirali.Count; i++)
            {
                sirali[i].BaslangicX = sirali[i - 1].BitisX;
                sirali[i].BaslangicY = sirali[i - 1].BitisY;
            }

            return true;
        }

        private static bool UclarAyniMi(double x1, double y1, double x2, double y2)
        {
            return Math.Abs(x1 - x2) <= CizimPaneli.UcEsitlikToleransi && Math.Abs(y1 - y2) <= CizimPaneli.UcEsitlikToleransi;
        }

        private static void YonCevir(CizgiNesnesi cizgi)
        {
            var x = cizgi.BaslangicX;
            var y = cizgi.BaslangicY;
            var z = cizgi.BaslangicZ;
            cizgi.BaslangicX = cizgi.BitisX;
            cizgi.BaslangicY = cizgi.BitisY;
            cizgi.BaslangicZ = cizgi.BitisZ;
            cizgi.BitisX = x;
            cizgi.BitisY = y;
            cizgi.BitisZ = z;
        }
    }

    /// <summary>
    /// NetCAD enkesit (KSE) dosyasını okur/yazar. Çizimde X = eksene mesafe, Y = kot olarak kullanılır.
    /// </summary>
    public class KseDosyasi
    {
        /// <summary>Kesitlerden önce gelen satırlar (ör. "2  @=COLOR=SECTION=DISPLAY=..."); aynen geri yazılır.</summary>
        public readonly List<string> OnSatirlar = new List<string>();
        public readonly List<KseKesit> Kesitler = new List<KseKesit>();

        /// <summary>Verilen kilometredeki (1 mm toleransla) ilk kesiti döner; yoksa null.</summary>
        public KseKesit KmdenBul(double km)
        {
            return Kesitler.FirstOrDefault(k => Math.Abs(k.Km - km) <= 1e-3);
        }

        public static KseDosyasi Oku(string dosyaYolu)
        {
            var dosya = new KseDosyasi();
            KseKesit guncelKesit = null;

            foreach (var hamSatir in File.ReadAllLines(dosyaYolu, Encoding.Default))
            {
                var satir = hamSatir.Trim();
                if (satir.Length == 0)
                    continue;

                var parcalar = satir.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parcalar[0] == "1")
                {
                    guncelKesit = new KseKesit
                    {
                        Baslik = string.Join("  ", parcalar.Skip(1)),
                        No = parcalar.Length > 1 ? parcalar[1] : "",
                        Kilometre = parcalar.Length > 2 ? parcalar[2] : "",
                        Aciklama = string.Join(" ", parcalar.Skip(3))
                    };
                    if (double.TryParse(guncelKesit.Kilometre, NumberStyles.Float, CultureInfo.InvariantCulture, out var km))
                        guncelKesit.Km = km;
                    dosya.Kesitler.Add(guncelKesit);
                }
                else if (parcalar[0] == "0" && guncelKesit != null && parcalar.Length >= 3
                         && double.TryParse(parcalar[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var mesafe)
                         && double.TryParse(parcalar[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var kot))
                {
                    guncelKesit.Noktalar.Add(new KseNokta { Mesafe = mesafe, Kot = kot, Kod = string.Join("  ", parcalar.Skip(3)) });
                }
                else if (guncelKesit == null)
                {
                    dosya.OnSatirlar.Add(satir);
                }
            }

            foreach (var kesit in dosya.Kesitler)
                kesit.CizgileriOlustur();

            return dosya;
        }

        /// <summary>Kesitleri güncel noktalarıyla KSE metnine çevirir. Noktası kalmamış kesit yazılmaz.</summary>
        public string Yaz()
        {
            var sb = new StringBuilder();
            foreach (var satir in OnSatirlar)
                sb.AppendLine(satir);

            foreach (var kesit in Kesitler)
            {
                if (kesit.Noktalar.Count == 0)
                    continue;

                sb.AppendLine("1  " + kesit.Baslik);
                foreach (var nokta in kesit.Noktalar)
                {
                    var satir = "0  " + nokta.Mesafe.ToString("R", CultureInfo.InvariantCulture) + "  " + nokta.Kot.ToString("R", CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(nokta.Kod))
                        satir += "  " + nokta.Kod;
                    sb.AppendLine(satir);
                }
            }

            return sb.ToString();
        }
    }
}
