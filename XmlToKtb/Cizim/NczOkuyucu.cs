using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// NetCAD çizim dosyası (.NCZ) okuyucu. Format belgelenmemiştir; aşağıdaki düzen NetCAD 8.5 dosyalarının, aynı
    /// çizimin NetCAD'den alınmış DXF karşılığıyla karşılaştırılmasıyla çıkarılmıştır.
    /// <para>Dosya bir parça (chunk) dizisidir: [1 bayt tür][int32 uzunluk][içerik]. Önemli parçalar: 0x01 başlık,
    /// 0x19 NetCAD sürümü, 0x06 tabaka tablosu, 0x17 objelerin başladığı işaret, 0x15 obje, 0x16 ek öznitelikli obje.
    /// 0x17'den önceki 0x15 parçaları blok tanımlarıdır ve alınmaz.</para>
    /// <para>Obje içeriği: [0] 0, [1] tip, [2] tabaka sırası, [3] X (kuzey, double), [11] Y (doğu, double), [19] Z (float),
    /// [45] sınır kutusu (4 double), [81] tipe özel alanlar. 0x16'da 81'de öznitelik metni (uzunluk önekli) ve 6 bayt
    /// vardır, tipe özel alanlar ondan sonra başlar.</para>
    /// <para>Tipler: 0x01 nokta (ad), 0x02 çizgi (bitiş X, Y double, Z float), 0x05 yazı (yükseklik, açı radyan, 3 bayt,
    /// metin), 0x06 sembol, 0x07 3B çoklu doğru (köşe başına X, Y, Z double), 0x09 çoklu doğru (ilk köşe başlıkta,
    /// diğerleri göreli float farklar, köşe başına 9 bayt), 0x0C üçgen (2. ve 3. köşe). Diğer tipler atlanır.</para>
    /// Koordinatlar çizim ekranı kuralına çevrilir: ekran X = doğu (NCZ Y), ekran Y = kuzey (NCZ X).
    /// </summary>
    public static class NczOkuyucu
    {
        public sealed class Sonuc
        {
            public readonly List<CizimNesnesi> Nesneler = new List<CizimNesnesi>();
            /// <summary>Okunamayan (desteklenmeyen) obje tipleri ve sayıları.</summary>
            public readonly SortedDictionary<int, int> Atlanan = new SortedDictionary<int, int>();
            public string Surum = "";
            public int AtlananToplam => Atlanan.Values.Sum();
        }

        private static readonly Encoding Kodlama = TurkceKodlama();

        private static Encoding TurkceKodlama()
        {
            try
            {
                return Encoding.GetEncoding(1254);
            }
            catch (Exception)
            {
                return Encoding.Default;
            }
        }

        /// <summary>
        /// NCZ dosyasını okur. tabakaBul, NCZ tabakasının (ad, renk) çizimdeki karşılığını döner; null verilirse objeler
        /// tabakasız kalır.
        /// </summary>
        public static Sonuc Oku(string dosya, Func<string, Color, Tabaka> tabakaBul)
        {
            var b = File.ReadAllBytes(dosya);
            if (b.Length < 5 || b[0] != 0x01)
                throw new InvalidDataException("Bu bir NetCAD çizim dosyası (NCZ) değil.");

            // Parça yapısı baştan sona tutmalı.
            var parcalar = new List<int>();
            var p = 0;
            while (p < b.Length)
            {
                if (p + 5 > b.Length)
                    throw new InvalidDataException("NCZ biçimi tanınmadı (dosya sonu yarım parça).");
                var n = BitConverter.ToInt32(b, p + 1);
                if (n < 0 || p + 5 + n > b.Length)
                    throw new InvalidDataException("NCZ biçimi tanınmadı (geçersiz parça uzunluğu, konum " + p + "). Dosya bu programın tanıdığı NetCAD sürümünden farklı olabilir.");
                parcalar.Add(p);
                p += 5 + n;
            }

            var sonuc = new Sonuc();
            var tabakalar = new List<Tabaka>();
            var objelerBasladi = false;
            foreach (var parca in parcalar)
            {
                var tur = b[parca];
                var o = parca + 5;
                var n = BitConverter.ToInt32(b, parca + 1);
                switch (tur)
                {
                    case 0x19:
                        if (n > 1)
                            sonuc.Surum = Kodlama.GetString(b, o + 1, Math.Min(b[o], n - 1));
                        break;
                    case 0x06:
                        tabakalar = TabakalariOku(b, o, n, tabakaBul);
                        break;
                    case 0x17:
                        objelerBasladi = true;
                        break;
                    case 0x15:
                    case 0x16:
                        if (!objelerBasladi || n < 81)
                            break;
                        CizimNesnesi nesne = null;
                        try
                        {
                            nesne = ObjeOku(b, o, n, tur == 0x16);
                        }
                        catch (ArgumentException)
                        {
                            // Beklenen alanlar kaydın dışına taşıyor: desteklenmeyen düzen.
                        }
                        if (nesne == null)
                        {
                            var tip = b[o + 1];
                            sonuc.Atlanan[tip] = sonuc.Atlanan.TryGetValue(tip, out var sayi) ? sayi + 1 : 1;
                            break;
                        }
                        var sira = b[o + 2];
                        if (sira < tabakalar.Count)
                            nesne.Tabaka = tabakalar[sira];
                        sonuc.Nesneler.Add(nesne);
                        break;
                }
            }
            return sonuc;
        }

        // Tabaka tablosu: 13 bayt başlık (11-12: tabaka sayısı), sonra tabaka başına 29 bayt:
        // [ad uzunluğu][20 baytlık ad alanı][renk (ACI) int16][int16][4 bayt bayrak].
        private static List<Tabaka> TabakalariOku(byte[] b, int o, int n, Func<string, Color, Tabaka> tabakaBul)
        {
            var liste = new List<Tabaka>();
            if (n < 13)
                return liste;
            var sayi = BitConverter.ToInt16(b, o + 11);
            for (var i = 0; i < sayi; i++)
            {
                var k = o + 13 + i * 29;
                if (k + 29 > o + n)
                    break;
                var uzunluk = Math.Min((int)b[k + 4], 20);
                var ad = Kodlama.GetString(b, k + 5, uzunluk).Trim();
                if (ad.Length == 0 || ad == "0")
                    ad = "0";
                liste.Add(tabakaBul?.Invoke(ad, NetcadRengi(BitConverter.ToInt16(b, k + 25))));
            }
            return liste;
        }

        // NetCAD renk numarasının karşılığı. Değerler, NetCAD'in aynı çizimi DXF'e aktarırken verdiği renklerdir
        // (0-15 NetCAD'in temel paleti); tabloda olmayan numaralar için AutoCAD (ACI) rengi kullanılır.
        private static readonly Dictionary<int, Color> NetcadPaleti = new Dictionary<int, Color>
        {
            { 0, Color.Black }, { 1, Color.FromArgb(0, 0, 255) }, { 2, Color.FromArgb(0, 255, 0) }, { 3, Color.FromArgb(0, 255, 255) },
            { 4, Color.FromArgb(255, 0, 0) }, { 5, Color.FromArgb(255, 127, 0) }, { 6, Color.FromArgb(128, 64, 0) }, { 7, Color.FromArgb(255, 191, 0) },
            { 8, Color.FromArgb(192, 192, 192) }, { 9, Color.FromArgb(0, 127, 255) }, { 10, Color.FromArgb(255, 0, 255) }, { 11, Color.FromArgb(127, 255, 255) },
            { 12, Color.FromArgb(255, 127, 255) }, { 13, Color.FromArgb(255, 127, 127) }, { 14, Color.FromArgb(255, 255, 0) }, { 15, Color.Black },
            { 19, Color.FromArgb(0, 165, 165) }, { 36, Color.FromArgb(165, 0, 0) }, { 78, Color.FromArgb(76, 76, 0) }, { 122, Color.FromArgb(165, 0, 82) },
            { 128, Color.FromArgb(128, 128, 128) }, { 142, Color.FromArgb(255, 255, 127) }, { 147, Color.FromArgb(153, 153, 153) },
            { 172, Color.FromArgb(204, 204, 204) }, { 179, Color.FromArgb(192, 192, 192) }, { 187, Color.FromArgb(192, 192, 192) },
            { 251, Color.FromArgb(127, 191, 255) }
        };

        private static Color NetcadRengi(int numara)
        {
            if (NetcadPaleti.TryGetValue(numara, out var renk))
                return renk;
            return numara > 0 && numara < 256 ? DxfIceAktarici.RenkCevir(new netDxf.AciColor((short)numara)) : Color.Black;
        }

        private static double D(byte[] b, int i, int son)
        {
            if (i + 8 > son)
                throw new ArgumentException("kayıt dışı");
            return BitConverter.ToDouble(b, i);
        }

        private static double F(byte[] b, int i, int son)
        {
            if (i + 4 > son)
                throw new ArgumentException("kayıt dışı");
            return BitConverter.ToSingle(b, i);
        }

        private static string Metin(byte[] b, int i, int son, out int sonraki)
        {
            if (i >= son)
                throw new ArgumentException("kayıt dışı");
            var uzunluk = b[i];
            if (i + 1 + uzunluk > son)
                throw new ArgumentException("kayıt dışı");
            sonraki = i + 1 + uzunluk;
            return Kodlama.GetString(b, i + 1, uzunluk);
        }

        private static CizimNesnesi ObjeOku(byte[] b, int o, int n, bool genis)
        {
            var son = o + n;
            var tip = b[o + 1];
            double kuzey = D(b, o + 3, son), dogu = D(b, o + 11, son), z = F(b, o + 19, son);
            var k = o + 81;
            // 0x16: NetCAD 7.x'te 81'de 11 sıfır bayt, "@" ile başlayan öznitelik metni ve 6 bayt vardır; tipe özel
            // alanlar ondan sonra başlar. NetCAD 8.x'te ek veri kaydın sonundadır, alanlar yerindedir.
            if (genis && k + 13 < son && Enumerable.Range(k, 11).All(i => b[i] == 0) && b[k + 11] > 0 && b[k + 12] == (byte)'@')
            {
                Metin(b, k + 11, son, out var sonraki);
                k = sonraki + 6;
            }
            // Çizgi ve üçgen köşeleri kaydın sınır kutusunda olmalı (kutu float hassasiyetinde yuvarlanmıştır).
            double minK = D(b, o + 45, son), minD = D(b, o + 53, son), maxK = D(b, o + 61, son), maxD = D(b, o + 69, son);
            bool KutudaMi(double kz, double dg) => kz >= minK - 1 && kz <= maxK + 1 && dg >= minD - 1 && dg <= maxD + 1;

            switch (tip)
            {
                case 0x01:
                {
                    var ad = k < son ? Metin(b, k, son, out _).Trim() : "";
                    return new NoktaNesnesi { X = dogu, Y = kuzey, Z = z, NoktaAdi = ad.Length > 0 ? ad : "0" };
                }
                case 0x06:
                    return new NoktaNesnesi { X = dogu, Y = kuzey, Z = z, NoktaAdi = "0", Kod = "Sembol" };
                case 0x02:
                {
                    double bitisK = D(b, k, son), bitisD = D(b, k + 8, son);
                    if (!KutudaMi(kuzey, dogu) || !KutudaMi(bitisK, bitisD))
                        return null;
                    return new CizgiNesnesi
                    {
                        BaslangicX = dogu, BaslangicY = kuzey, BaslangicZ = z,
                        BitisX = bitisD, BitisY = bitisK, BitisZ = F(b, k + 16, son)
                    };
                }
                case 0x05:
                {
                    var yukseklik = F(b, k, son);
                    var aciRadyan = F(b, k + 4, son);
                    var metin = Metin(b, k + 11, son, out _).TrimEnd();
                    if (metin.Trim().Length == 0)
                        return null;
                    return new YaziNesnesi
                    {
                        KonumX = dogu, KonumY = kuzey, KonumZ = z, Metin = metin,
                        Yukseklik = yukseklik > 0 ? yukseklik : 1.5,
                        Donus = aciRadyan * 180 / Math.PI,
                        HizaYatay = YatayHizalama.Sol, HizaDikey = DikeyHizalama.Alt
                    };
                }
                case 0x07:
                    return CokluDogru3BOku(b, o, son, k);
                case 0x09:
                {
                    // İlk köşe başlıkta; diğerleri ona göre göreli (kuzey, doğu) float farklar, köşe başına 9 bayt.
                    var bas = k + 45;
                    if (bas > son || (son - bas) % 9 != 0)
                        return null;
                    var cd = new CokluDogruNesnesi();
                    cd.Koseler.Add(new CokluDogruKosesi { X = dogu, Y = kuzey, Z = z });
                    for (var v = bas; v + 9 <= son; v += 9)
                        cd.Koseler.Add(new CokluDogruKosesi { X = dogu + F(b, v + 4, son), Y = kuzey + F(b, v, son), Z = z });
                    return KapaliyiAyarla(cd);
                }
                case 0x0C:
                {
                    if (!KutudaMi(D(b, k, son), D(b, k + 8, son)) || !KutudaMi(D(b, k + 20, son), D(b, k + 28, son)))
                        return null;
                    var cd = new CokluDogruNesnesi { Kapali = true };
                    cd.Koseler.Add(new CokluDogruKosesi { X = dogu, Y = kuzey, Z = z });
                    cd.Koseler.Add(new CokluDogruKosesi { X = D(b, k + 8, son), Y = D(b, k, son), Z = F(b, k + 16, son) });
                    cd.Koseler.Add(new CokluDogruKosesi { X = D(b, k + 28, son), Y = D(b, k + 20, son), Z = F(b, k + 36, son) });
                    return cd;
                }
                default:
                    return null;
            }
        }

        /// <summary>
        /// 3B çoklu doğru: köşeler kaydın sonunda, köşe başına 24 bayt (kuzey, doğu, Z double). Köşelerden önceki
        /// alanların uzunluğu sürüme göre değiştiği için başlangıç, kalan uzunluğu 24'ün katı olan ve sınır kutusunun
        /// içine düşen ilk konum olarak bulunur.
        /// </summary>
        private static CizimNesnesi CokluDogru3BOku(byte[] b, int o, int son, int k)
        {
            double minK = D(b, o + 45, son), minD = D(b, o + 53, son), maxK = D(b, o + 61, son), maxD = D(b, o + 69, son);
            const double pay = 1.0; // sınır kutusu float hassasiyetinde yuvarlanmıştır
            for (var bas = k; bas + 24 <= son; bas++)
            {
                if ((son - bas) % 24 != 0)
                    continue;
                double kz = BitConverter.ToDouble(b, bas), dg = BitConverter.ToDouble(b, bas + 8);
                if (kz < minK - pay || kz > maxK + pay || dg < minD - pay || dg > maxD + pay)
                    continue;
                var cd = new CokluDogruNesnesi();
                for (var v = bas; v + 24 <= son; v += 24)
                    cd.Koseler.Add(new CokluDogruKosesi { X = BitConverter.ToDouble(b, v + 8), Y = BitConverter.ToDouble(b, v), Z = BitConverter.ToDouble(b, v + 16) });
                return KapaliyiAyarla(cd);
            }
            return null;
        }

        // Son köşe ilk köşeyle aynıysa çoklu doğru kapalı sayılır ve tekrarlanan köşe atılır.
        private static CokluDogruNesnesi KapaliyiAyarla(CokluDogruNesnesi cd)
        {
            if (cd.Koseler.Count < 2)
                return cd.Koseler.Count == 1 ? cd : null;
            var ilk = cd.Koseler[0];
            var sonKose = cd.Koseler[cd.Koseler.Count - 1];
            if (cd.Koseler.Count > 3 && Math.Abs(ilk.X - sonKose.X) < 1e-6 && Math.Abs(ilk.Y - sonKose.Y) < 1e-6)
            {
                cd.Koseler.RemoveAt(cd.Koseler.Count - 1);
                cd.Kapali = true;
            }
            return cd;
        }
    }
}
