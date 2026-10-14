using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XmlToKtb.Las
{
    /// <summary>
    /// Bir LAS (LIDAR nokta bulutu) dosyasından belleğe alınan noktalar ve dosya bilgileri. Çok büyük dosyalarda
    /// noktaların tamamı değil, eşit aralıklı bir örneği yüklenir (bkz. <see cref="Adim"/>).
    /// Koordinatlar <see cref="OrijinX"/>/<see cref="OrijinY"/> noktasına göre fark olarak (float) tutulur; böylece
    /// milyonlarca nokta az bellek kaplar ve hassasiyet milimetre düzeyinde kalır.
    /// </summary>
    public class LasBulutu
    {
        public string DosyaYolu;
        public string Surum;
        public int NoktaBicimi;
        public string UretenYazilim;
        /// <summary>Dosyada tanımlı koordinat sisteminin EPSG kodu; tanımlı değilse 0.</summary>
        public int Epsg;
        /// <summary>Dosyada koordinat sistemi için yazılmış ad (varsa).</summary>
        public string KoordinatSistemiAdi;

        /// <summary>Dosyadaki toplam nokta sayısı.</summary>
        public long ToplamNokta;
        /// <summary>Yüklenen nokta sayısı.</summary>
        public int Sayi;
        /// <summary>Her kaç noktadan birinin yüklendiği (1: tamamı).</summary>
        public int Adim = 1;

        public double OrijinX, OrijinY;
        /// <summary>Orijine göre sağa (doğu) ve yukarı (kuzey) uzaklıklar ile kot.</summary>
        public float[] X, Y, Z;
        public ushort[] Yogunluk;
        public byte[] Sinif;
        /// <summary>Noktaların rengi (0xRRGGBB); dosyada renk yoksa null.</summary>
        public int[] Rgb;

        /// <summary>Yüklenen noktaların sınır değerleri (gerçek koordinat).</summary>
        public double MinX, MinY, MinZ, MaxX, MaxY, MaxZ;
        public int MinYogunluk, MaxYogunluk;
        /// <summary>Sınıf numarasına göre yüklenen nokta sayıları.</summary>
        public readonly int[] SinifSayilari = new int[256];

        /// <summary>ASPRS standart sınıf adları.</summary>
        public static string SinifAdi(int sinif)
        {
            switch (sinif)
            {
                case 0: return "Sınıflandırılmamış (oluşturuldu)";
                case 1: return "Sınıflandırılmamış";
                case 2: return "Zemin";
                case 3: return "Alçak bitki";
                case 4: return "Orta bitki";
                case 5: return "Yüksek bitki";
                case 6: return "Bina";
                case 7: return "Gürültü (alçak)";
                case 8: return "Anahtar nokta";
                case 9: return "Su";
                case 10: return "Demiryolu";
                case 11: return "Yol yüzeyi";
                case 12: return "Bindirme";
                case 13: return "Tel (koruma)";
                case 14: return "Tel (iletken)";
                case 15: return "Direk";
                case 16: return "Tel bağlantısı";
                case 17: return "Köprü";
                case 18: return "Gürültü (yüksek)";
                default: return "Sınıf " + sinif;
            }
        }

        /// <summary>
        /// LAS dosyasını okur. Dosyadaki nokta sayısı <paramref name="enFazlaNokta"/> değerini aşıyorsa noktalar eşit
        /// aralıkla örneklenir. <paramref name="ilerleme"/> 0-1 arası oranla ara ara çağrılır.
        /// LAS 1.0-1.4 ve 0-10 nokta biçimleri okunur; sıkıştırılmış LAZ dosyaları okunmaz.
        /// </summary>
        public static LasBulutu Oku(string dosyaYolu, int enFazlaNokta, Action<double> ilerleme = null)
        {
            using (var akis = new FileStream(dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            {
                var baslik = new byte[375];
                var okunan = akis.Read(baslik, 0, baslik.Length);
                if (okunan < 227 || Encoding.ASCII.GetString(baslik, 0, 4) != "LASF")
                    throw new InvalidDataException("Bu bir LAS dosyası değil (\"LASF\" imzası bulunamadı).");

                int surumBuyuk = baslik[24], surumKucuk = baslik[25];
                var bulut = new LasBulutu
                {
                    DosyaYolu = dosyaYolu,
                    Surum = surumBuyuk + "." + surumKucuk,
                    UretenYazilim = Encoding.ASCII.GetString(baslik, 58, 32).TrimEnd('\0', ' ')
                };

                long noktaBaslangici = BitConverter.ToUInt32(baslik, 96);
                int bicimBayti = baslik[104];
                int kayitBoyu = BitConverter.ToUInt16(baslik, 105);
                long noktaSayisi = BitConverter.ToUInt32(baslik, 107);
                // LAS 1.4'te nokta sayısı 64 bitlik ayrı bir alanda tutulur (eski alan sıfır olabilir).
                if (surumBuyuk == 1 && surumKucuk >= 4 && okunan >= 255)
                {
                    var buyukSayi = (long)BitConverter.ToUInt64(baslik, 247);
                    if (buyukSayi > 0)
                        noktaSayisi = buyukSayi;
                }

                // Sıkıştırılmış (LAZ) dosyalarda biçim baytının üst bitleri dolu olur.
                if ((bicimBayti & 0xC0) != 0 || Path.GetExtension(dosyaYolu).Equals(".laz", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("Sıkıştırılmış LAZ dosyaları okunamıyor. Dosyayı LAS olarak (sıkıştırmasız) kaydedip yeniden deneyin.");

                var bicim = bicimBayti & 0x3F;
                bulut.NoktaBicimi = bicim;
                if (bicim > 10)
                    throw new NotSupportedException("Desteklenmeyen LAS nokta biçimi: " + bicim);

                KoordinatSisteminiOku(akis, BitConverter.ToUInt16(baslik, 94), BitConverter.ToUInt32(baslik, 100), noktaBaslangici, bulut);

                double olcekX = BitConverter.ToDouble(baslik, 131), olcekY = BitConverter.ToDouble(baslik, 139), olcekZ = BitConverter.ToDouble(baslik, 147);
                double ofsetX = BitConverter.ToDouble(baslik, 155), ofsetY = BitConverter.ToDouble(baslik, 163), ofsetZ = BitConverter.ToDouble(baslik, 171);

                // Kayıt içindeki alanların yerleri nokta biçimine göre değişir.
                var yeniBicim = bicim >= 6;
                var sinifYeri = yeniBicim ? 16 : 15;
                var renkYeri = -1;
                switch (bicim)
                {
                    case 2: renkYeri = 20; break;
                    case 3: case 5: renkYeri = 28; break;
                    case 7: case 8: case 10: renkYeri = 30; break;
                }
                var enAzKayit = renkYeri >= 0 ? renkYeri + 6 : yeniBicim ? 30 : 20;
                if (kayitBoyu < enAzKayit)
                    throw new InvalidDataException("LAS başlığı tutarsız: nokta kayıt boyu (" + kayitBoyu + ") " + bicim + ". biçim için çok küçük.");

                // Başlıktaki sayı dosya boyutundan fazlaysa (yarım kalmış dosya) eldeki kadarı okunur.
                var sigabilen = (akis.Length - noktaBaslangici) / kayitBoyu;
                if (noktaSayisi <= 0 || noktaSayisi > sigabilen)
                    noktaSayisi = sigabilen;
                if (noktaSayisi <= 0)
                    throw new InvalidDataException("LAS dosyasında nokta yok.");

                bulut.ToplamNokta = noktaSayisi;
                bulut.Adim = (int)Math.Max(1, (noktaSayisi + enFazlaNokta - 1) / Math.Max(1, enFazlaNokta));
                var yuklenecek = (int)((noktaSayisi + bulut.Adim - 1) / bulut.Adim);

                bulut.X = new float[yuklenecek];
                bulut.Y = new float[yuklenecek];
                bulut.Z = new float[yuklenecek];
                bulut.Yogunluk = new ushort[yuklenecek];
                bulut.Sinif = new byte[yuklenecek];
                if (renkYeri >= 0)
                    bulut.Rgb = new int[yuklenecek];

                // Renkler 16 bit saklanır; bazı yazılımlar 8 bitlik değeri olduğu gibi yazar. Hangisi olduğu ancak
                // değerlere bakılarak anlaşılır: 255'ten büyük değer varsa 16 bittir.
                var kirmizi = renkYeri >= 0 ? new ushort[yuklenecek] : null;
                var yesil = renkYeri >= 0 ? new ushort[yuklenecek] : null;
                var mavi = renkYeri >= 0 ? new ushort[yuklenecek] : null;
                var enBuyukRenk = 0;

                bulut.MinX = bulut.MinY = bulut.MinZ = double.MaxValue;
                bulut.MaxX = bulut.MaxY = bulut.MaxZ = double.MinValue;
                bulut.MinYogunluk = int.MaxValue;
                bulut.MaxYogunluk = int.MinValue;

                var sayi = 0;
                void NoktayiIsle(byte[] tampon, int yer)
                {
                    var x = BitConverter.ToInt32(tampon, yer) * olcekX + ofsetX;
                    var y = BitConverter.ToInt32(tampon, yer + 4) * olcekY + ofsetY;
                    var z = BitConverter.ToInt32(tampon, yer + 8) * olcekZ + ofsetZ;
                    if (sayi == 0)
                    {
                        // Orijin ilk noktanın tam metreye yuvarlanmış konumudur.
                        bulut.OrijinX = Math.Floor(x);
                        bulut.OrijinY = Math.Floor(y);
                    }

                    bulut.X[sayi] = (float)(x - bulut.OrijinX);
                    bulut.Y[sayi] = (float)(y - bulut.OrijinY);
                    bulut.Z[sayi] = (float)z;
                    var yogunluk = BitConverter.ToUInt16(tampon, yer + 12);
                    bulut.Yogunluk[sayi] = yogunluk;
                    // Eski biçimlerde sınıf baytının üst üç biti bayraktır.
                    var sinif = yeniBicim ? tampon[yer + sinifYeri] : (byte)(tampon[yer + sinifYeri] & 0x1F);
                    bulut.Sinif[sayi] = sinif;
                    bulut.SinifSayilari[sinif]++;

                    if (renkYeri >= 0)
                    {
                        kirmizi[sayi] = BitConverter.ToUInt16(tampon, yer + renkYeri);
                        yesil[sayi] = BitConverter.ToUInt16(tampon, yer + renkYeri + 2);
                        mavi[sayi] = BitConverter.ToUInt16(tampon, yer + renkYeri + 4);
                        enBuyukRenk = Math.Max(enBuyukRenk, Math.Max(kirmizi[sayi], Math.Max(yesil[sayi], mavi[sayi])));
                    }

                    if (x < bulut.MinX) bulut.MinX = x;
                    if (x > bulut.MaxX) bulut.MaxX = x;
                    if (y < bulut.MinY) bulut.MinY = y;
                    if (y > bulut.MaxY) bulut.MaxY = y;
                    if (z < bulut.MinZ) bulut.MinZ = z;
                    if (z > bulut.MaxZ) bulut.MaxZ = z;
                    if (yogunluk < bulut.MinYogunluk) bulut.MinYogunluk = yogunluk;
                    if (yogunluk > bulut.MaxYogunluk) bulut.MaxYogunluk = yogunluk;
                    sayi++;
                }

                if (bulut.Adim <= 16)
                {
                    // Sık örneklemede dosya baştan sona bloklar halinde okunur, blok içinde her Adim'inci kayıt alınır.
                    const int bloktakiKayit = 32768;
                    var tampon = new byte[kayitBoyu * bloktakiKayit];
                    akis.Seek(noktaBaslangici, SeekOrigin.Begin);
                    long kayitNo = 0;
                    while (kayitNo < noktaSayisi && sayi < yuklenecek)
                    {
                        var istenen = (int)Math.Min(bloktakiKayit, noktaSayisi - kayitNo) * kayitBoyu;
                        var alinan = TamOku(akis, tampon, istenen);
                        var kayitSayisi = alinan / kayitBoyu;
                        if (kayitSayisi == 0)
                            break;

                        // Bloktaki ilk alınacak kayıt: Adim'in katı olan ilk kayıt numarası.
                        var ilk = (int)((bulut.Adim - kayitNo % bulut.Adim) % bulut.Adim);
                        for (var k = ilk; k < kayitSayisi && sayi < yuklenecek; k += bulut.Adim)
                            NoktayiIsle(tampon, k * kayitBoyu);

                        kayitNo += kayitSayisi;
                        ilerleme?.Invoke((double)kayitNo / noktaSayisi);
                    }
                }
                else
                {
                    // Seyrek örneklemede (çok büyük dosya) yalnızca alınacak kayıtlara atlanır.
                    var tampon = new byte[kayitBoyu];
                    for (long kayitNo = 0; kayitNo < noktaSayisi && sayi < yuklenecek; kayitNo += bulut.Adim)
                    {
                        akis.Seek(noktaBaslangici + kayitNo * kayitBoyu, SeekOrigin.Begin);
                        if (TamOku(akis, tampon, kayitBoyu) < kayitBoyu)
                            break;
                        NoktayiIsle(tampon, 0);
                        if ((sayi & 0xFFFF) == 0)
                            ilerleme?.Invoke((double)kayitNo / noktaSayisi);
                    }
                }

                bulut.Sayi = sayi;
                if (sayi == 0)
                    throw new InvalidDataException("LAS dosyasından nokta okunamadı.");

                if (renkYeri >= 0)
                {
                    var kaydirma = enBuyukRenk > 255 ? 8 : 0;
                    for (var i = 0; i < sayi; i++)
                        bulut.Rgb[i] = ((kirmizi[i] >> kaydirma) << 16) | ((yesil[i] >> kaydirma) << 8) | (mavi[i] >> kaydirma);
                    // Bütün renkler sıfırsa dosyada gerçekte renk yoktur.
                    if (enBuyukRenk == 0)
                        bulut.Rgb = null;
                }

                ilerleme?.Invoke(1.0);
                return bulut;
            }
        }

        /// <summary>
        /// Başlıktan sonraki değişken uzunluklu kayıtlardan (VLR) koordinat sistemini okur: GeoTIFF anahtarları
        /// (kayıt 34735) ya da WKT metni (kayıt 2112). Bulunamazsa <see cref="Epsg"/> sıfır kalır.
        /// </summary>
        private static void KoordinatSisteminiOku(Stream akis, int baslikBoyu, long kayitSayisi, long noktaBaslangici, LasBulutu bulut)
        {
            try
            {
                akis.Seek(baslikBoyu, SeekOrigin.Begin);
                var kayitBasligi = new byte[54];
                int projeksiyonEpsg = 0, cografiEpsg = 0, wktEpsg = 0;
                for (long k = 0; k < kayitSayisi && akis.Position + 54 <= noktaBaslangici; k++)
                {
                    if (TamOku(akis, kayitBasligi, 54) < 54)
                        break;
                    var kullanici = Encoding.ASCII.GetString(kayitBasligi, 2, 16).TrimEnd('\0', ' ');
                    int kayitNo = BitConverter.ToUInt16(kayitBasligi, 18), boy = BitConverter.ToUInt16(kayitBasligi, 20);
                    var veri = new byte[boy];
                    if (TamOku(akis, veri, boy) < boy)
                        break;
                    if (!kullanici.Equals("LASF_Projection", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (kayitNo == 34735 && boy >= 8)
                    {
                        // Anahtar dizini: 4 sayılık başlığın ardından her anahtar için (no, yer, adet, değer).
                        var anahtarSayisi = Math.Min(BitConverter.ToUInt16(veri, 6), (boy - 8) / 8);
                        for (var a = 0; a < anahtarSayisi; a++)
                        {
                            int anahtar = BitConverter.ToUInt16(veri, 8 + a * 8), yer = BitConverter.ToUInt16(veri, 10 + a * 8), deger = BitConverter.ToUInt16(veri, 14 + a * 8);
                            // 0: tanımsız, 32767: kullanıcı tanımlı (EPSG kodu yok).
                            if (yer != 0 || deger == 0 || deger == 32767)
                                continue;
                            if (anahtar == 3072)
                                projeksiyonEpsg = deger;
                            else if (anahtar == 2048)
                                cografiEpsg = deger;
                        }
                    }
                    else if (kayitNo == 34737)
                    {
                        var ad = Encoding.ASCII.GetString(veri).Trim('\0', ' ', '|');
                        if (ad.Length > 0 && bulut.KoordinatSistemiAdi == null)
                            bulut.KoordinatSistemiAdi = ad.Replace('|', ' ');
                    }
                    else if (kayitNo == 2112)
                    {
                        var wkt = Encoding.UTF8.GetString(veri).Trim('\0', ' ');
                        var adEslesmesi = Regex.Match(wkt, "\"([^\"]+)\"");
                        if (adEslesmesi.Success)
                            bulut.KoordinatSistemiAdi = adEslesmesi.Groups[1].Value;
                        // Metindeki EPSG kodlarından tanınan (desteklenen) sonuncusu, yoksa en sondaki alınır.
                        foreach (Match eslesme in Regex.Matches(wkt, "(?:AUTHORITY|ID)\\s*\\[\\s*\"EPSG\"\\s*,\\s*\"?(\\d+)\"?"))
                        {
                            var kod = int.Parse(eslesme.Groups[1].Value);
                            if (wktEpsg == 0 || KoordinatSistemi.Bul(kod) != null || KoordinatSistemi.Bul(wktEpsg) == null)
                                wktEpsg = kod;
                        }
                    }
                }

                bulut.Epsg = projeksiyonEpsg != 0 ? projeksiyonEpsg : wktEpsg != 0 ? wktEpsg : cografiEpsg;
            }
            catch (Exception)
            {
                // Koordinat sistemi kaydı bozuksa dosya yine de açılır; sistem tanımsız kabul edilir.
                bulut.Epsg = 0;
            }
        }

        /// <summary>
        /// Yüklü bütün noktaların yatay koordinatlarını verilen dönüştürücüyle çevirir; kotlar değişmez. Orijin ve
        /// sınır değerleri yeniden hesaplanır.
        /// </summary>
        public void KoordinatlariDonustur(KoordinatDonusturucu donusturucu)
        {
            double eskiOrijinX = OrijinX, eskiOrijinY = OrijinY;
            double ilkX = eskiOrijinX + X[0], ilkY = eskiOrijinY + Y[0];
            donusturucu.Donustur(ref ilkX, ref ilkY);
            double yeniOrijinX = Math.Floor(ilkX), yeniOrijinY = Math.Floor(ilkY);

            Parallel.For(0, Sayi, i =>
            {
                double x = eskiOrijinX + X[i], y = eskiOrijinY + Y[i];
                donusturucu.Donustur(ref x, ref y);
                X[i] = (float)(x - yeniOrijinX);
                Y[i] = (float)(y - yeniOrijinY);
            });

            OrijinX = yeniOrijinX;
            OrijinY = yeniOrijinY;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (var i = 0; i < Sayi; i++)
            {
                if (X[i] < minX) minX = X[i];
                if (X[i] > maxX) maxX = X[i];
                if (Y[i] < minY) minY = Y[i];
                if (Y[i] > maxY) maxY = Y[i];
            }
            MinX = OrijinX + minX; MaxX = OrijinX + maxX;
            MinY = OrijinY + minY; MaxY = OrijinY + maxY;
        }

        private static int TamOku(Stream akis, byte[] tampon, int istenen)
        {
            var toplam = 0;
            while (toplam < istenen)
            {
                var okunan = akis.Read(tampon, toplam, istenen - toplam);
                if (okunan <= 0)
                    break;
                toplam += okunan;
            }
            return toplam;
        }
    }
}
