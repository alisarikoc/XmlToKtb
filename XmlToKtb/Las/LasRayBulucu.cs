using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace XmlToKtb.Las
{
    /// <summary>Ray bulma ayarları (metre).</summary>
    public class LasRayAyarlari
    {
        /// <summary>Izgara hücre boyu.</summary>
        public double Hucre = 0.02;
        /// <summary>Zemin (en alçak yüzey) aranan pencerenin yarı genişliği.</summary>
        public double ZeminPenceresi = 0.30;
        /// <summary>Zeminin bu kadar üstündeki noktalar alt katman sayılır (tünel tavanı, duvarlar ve tel gibi üst objeler elenir).</summary>
        public double AltKatman = 0.60;
        /// <summary>Ray başının zeminden en az yüksekliği.</summary>
        public double EnAzRayYuksekligi = 0.07;
        /// <summary>
        /// Dar sırt şartı: ray başı bir yanındaki yüzeyden en az <see cref="YanFarki"/>, öbür yanındakinden en az
        /// <see cref="KarsiYanFarki"/> yüksekte olmalı (görülmeyen yan alçak sayılır). Duvar ve bordür gibi tek yanı
        /// yüksek ya da üstü düz kenarlar elenir.
        /// </summary>
        public double YanFarki = 0.05, KarsiYanFarki = 0.015;
        /// <summary>Sırt şartında yanlara bakılan uzaklık aralığı (ray başının yarı genişliği + pay).</summary>
        public double YanMesafesiEnAz = 0.08, YanMesafesiEnFazla = 0.14;
        /// <summary>Ray parçasının en az boyu; daha kısa sırtlar (bağlantı elemanı, cıvata, taş) elenir.</summary>
        public double EnAzBoy = 1.0;
        /// <summary>Ray parçasının ortalama en fazla genişliği (alan / boy).</summary>
        public double EnFazlaGenislik = 0.12;
        /// <summary>Ray başından aşağı doğru ray sayılan yükseklik (baş + gövde).</summary>
        public double RayYuksekligi = 0.18;
        /// <summary>
        /// Paralel eşi olan (iki raylı hat) parçalar alınır. Eşler arası (ray başı ortaları) mesafe hat açıklığı +
        /// ray başı genişliği kadardır; <see cref="EsToleransi"/> kadar sapmaya izin verilir.
        /// </summary>
        public bool EsAra = true;
        /// <summary>Hat açıklığı (ray iç yüzleri arası; normal hat 1.435 m).</summary>
        public double HatAcikligi = 1.435;
        public double RayBasiGenisligi = 0.072;
        public double EsToleransi = 0.06;
        /// <summary>Ray sınıfının numarası (ASPRS 10: Rail).</summary>
        public byte RaySinifi = 10;
    }

    /// <summary>
    /// Nokta bulutunda demiryolu raylarını bulur: ray, yanlarındaki yüzeyden yüksekte duran dar ve uzun bir sırttır
    /// (ray başı). Bulut karolara bölünür; her karoda 2 cm'lik ızgarada alt yüzey (zemin) ve sırtlar bulunur, sırt
    /// hücreleri birleştirilip uzun ve dar parçalar ray sayılır. Ray hücrelerindeki, ray başından ray yüksekliği kadar
    /// aşağıya kadarki noktalar ray sınıfına alınır. Yön, kot ve dever bilinmeden çalışır.
    /// </summary>
    public static class LasRayBulucu
    {
        private const double KaroBoyu = 24, KaroPayi = 1.0;

        /// <summary>
        /// Rayları bulur ve noktaların sınıfını ray sınıfı yapar. <paramref name="sinifGorunur"/> verilirse yalnızca
        /// görünen sınıflardaki noktalar değerlendirilir. Ray sınıfına alınan nokta sayısını verir; önceden ray
        /// sınıfında olup ray bulunmayan noktalar 1 (sınıflandırılmamış) yapılır.
        /// </summary>
        public static int Siniflandir(LasBulutu b, LasRayAyarlari ayar, bool[] sinifGorunur, Action<double> ilerleme)
        {
            var ray = Bul(b, ayar, sinifGorunur, ilerleme);
            var sayi = 0;
            for (var i = 0; i < b.Sayi; i++)
            {
                if (ray[i])
                {
                    if (b.Sinif[i] != ayar.RaySinifi)
                    {
                        b.SinifSayilari[b.Sinif[i]]--;
                        b.Sinif[i] = ayar.RaySinifi;
                        b.SinifSayilari[ayar.RaySinifi]++;
                    }
                    sayi++;
                }
                else if (b.Sinif[i] == ayar.RaySinifi)
                {
                    b.SinifSayilari[ayar.RaySinifi]--;
                    b.Sinif[i] = 1;
                    b.SinifSayilari[1]++;
                }
            }
            return sayi;
        }

        /// <summary>Ray noktalarını bulur (sınıfları değiştirmez).</summary>
        public static bool[] Bul(LasBulutu b, LasRayAyarlari ayar, bool[] sinifGorunur, Action<double> ilerleme)
        {
            var sonuc = new bool[b.Sayi];
            double minX = b.MinX - b.OrijinX, minY = b.MinY - b.OrijinY;
            int kx = Math.Max(1, (int)Math.Ceiling((b.MaxX - b.MinX) / KaroBoyu)), ky = Math.Max(1, (int)Math.Ceiling((b.MaxY - b.MinY) / KaroBoyu));

            // Noktalar karolara (paylarıyla birlikte) dağıtılır: önce sayılır, sonra yerleştirilir.
            var karoSayisi = kx * ky;
            var adet = new int[karoSayisi + 1];
            void KarolariGez(int i, Action<int> islem)
            {
                double x = b.X[i] - minX, y = b.Y[i] - minY;
                int x0 = Math.Max(0, (int)Math.Floor((x - KaroPayi) / KaroBoyu)), x1 = Math.Min(kx - 1, (int)Math.Floor((x + KaroPayi) / KaroBoyu));
                int y0 = Math.Max(0, (int)Math.Floor((y - KaroPayi) / KaroBoyu)), y1 = Math.Min(ky - 1, (int)Math.Floor((y + KaroPayi) / KaroBoyu));
                for (var ty = y0; ty <= y1; ty++)
                    for (var tx = x0; tx <= x1; tx++)
                        islem(ty * kx + tx);
            }
            bool Uygun(int i) => sinifGorunur == null || sinifGorunur[b.Sinif[i]];
            for (var i = 0; i < b.Sayi; i++)
            {
                if (Uygun(i))
                    KarolariGez(i, k => adet[k + 1]++);
            }
            for (var k = 0; k < karoSayisi; k++)
                adet[k + 1] += adet[k];
            var dizi = new int[adet[karoSayisi]];
            var yer = (int[])adet.Clone();
            for (var i = 0; i < b.Sayi; i++)
            {
                if (Uygun(i))
                    KarolariGez(i, k => dizi[yer[k]++] = i);
            }

            var biten = 0;
            Parallel.For(0, karoSayisi, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, k =>
            {
                if (adet[k + 1] - adet[k] > 100)
                {
                    double karoX = minX + (k % kx) * KaroBoyu, karoY = minY + (k / kx) * KaroBoyu;
                    KaroIsle(b, ayar, dizi, adet[k], adet[k + 1], karoX, karoY, sonuc);
                }
                var n = Interlocked.Increment(ref biten);
                ilerleme?.Invoke((double)n / karoSayisi);
            });
            return sonuc;
        }

        // Bir karonun (payıyla) noktalarında rayları bulur; yalnızca karonun kendi alanındaki noktaları işaretler.
        private static void KaroIsle(LasBulutu b, LasRayAyarlari ayar, int[] dizi, int bas, int son, double karoX, double karoY, bool[] sonuc)
        {
            var h = ayar.Hucre;
            double ox = karoX - KaroPayi, oy = karoY - KaroPayi;
            var boy = KaroBoyu + 2 * KaroPayi;
            int nx = (int)Math.Ceiling(boy / h) + 1, ny = nx, n = nx * ny;

            int Hucre(int i)
            {
                int cx = (int)((b.X[i] - ox) / h), cy = (int)((b.Y[i] - oy) / h);
                return cx < 0 || cy < 0 || cx >= nx || cy >= ny ? -1 : cy * nx + cx;
            }

            // 1) Her hücrenin en alçak noktası; 2) pencerede en alçak = zemin.
            var zmin = new float[n];
            for (var c = 0; c < n; c++)
                zmin[c] = float.NaN;
            for (var j = bas; j < son; j++)
            {
                var i = dizi[j];
                var c = Hucre(i);
                if (c < 0)
                    continue;
                if (float.IsNaN(zmin[c]) || b.Z[i] < zmin[c])
                    zmin[c] = b.Z[i];
            }
            var r = Math.Max(1, (int)Math.Round(ayar.ZeminPenceresi / h));
            var zemin = MinFiltre(MinFiltre(zmin, nx, ny, r, true), nx, ny, r, false);

            // 3) Alt katmanın üst yüzeyi: zeminin AltKatman kadar üstüne kadarki noktaların üstten %10'luk dilimi
            //    (en az ikinci en yüksek). Seyrek gürültü ve ray arabası gibi üstten geçen seyrek noktalar yüzeyi bozmaz.
            var ust = new float[n];
            var hucreBasi = new int[n + 1];
            var noktaHucresi = new int[son - bas];
            for (var j = bas; j < son; j++)
            {
                var i = dizi[j];
                var c = Hucre(i);
                if (c >= 0 && !float.IsNaN(zemin[c]) && b.Z[i] <= zemin[c] + ayar.AltKatman)
                {
                    noktaHucresi[j - bas] = c;
                    hucreBasi[c + 1]++;
                }
                else
                {
                    noktaHucresi[j - bas] = -1;
                }
            }
            for (var c = 0; c < n; c++)
                hucreBasi[c + 1] += hucreBasi[c];
            var kotlar = new float[hucreBasi[n]];
            var dolum = new int[n];
            for (var j = bas; j < son; j++)
            {
                var c = noktaHucresi[j - bas];
                if (c >= 0)
                    kotlar[hucreBasi[c] + dolum[c]++] = b.Z[dizi[j]];
            }
            for (var c = 0; c < n; c++)
            {
                var adet = hucreBasi[c + 1] - hucreBasi[c];
                if (adet == 0)
                {
                    ust[c] = float.NaN;
                    continue;
                }
                Array.Sort(kotlar, hucreBasi[c], adet);
                var k = Math.Min(adet, Math.Max(2, (int)Math.Ceiling(adet * 0.1)));
                ust[c] = kotlar[hucreBasi[c + 1] - k];
            }

            // 4) Sırt hücreleri: zeminden yeterince yüksek ve en az bir doğrultuda iki yanından da yüksek.
            int d0 = Math.Max(1, (int)Math.Round(ayar.YanMesafesiEnAz / h)), d1 = Math.Max(d0, (int)Math.Round(ayar.YanMesafesiEnFazla / h));
            var yonler = new[] { new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { 1, -1 } };
            var sirt = new bool[n];
            for (var cy = d1 + 1; cy < ny - d1 - 1; cy++)
            {
                for (var cx = d1 + 1; cx < nx - d1 - 1; cx++)
                {
                    var c = cy * nx + cx;
                    var u = ust[c];
                    if (float.IsNaN(u) || u - zemin[c] < ayar.EnAzRayYuksekligi)
                        continue;
                    foreach (var yon in yonler)
                    {
                        // Çapraz doğrultuda adımlar √2 uzun; mesafe aralığı adım sayısıyla korunur.
                        var capraz = yon[0] != 0 && yon[1] != 0;
                        int a0 = capraz ? Math.Max(1, (int)Math.Round(d0 / 1.41421356)) : d0, a1 = capraz ? Math.Max(a0, (int)Math.Round(d1 / 1.41421356)) : d1;
                        float yanA = float.MinValue, yanB = float.MinValue;
                        var veriVar = false;
                        for (var a = a0; a <= a1; a++)
                        {
                            var pa = ust[(cy + yon[1] * a) * nx + cx + yon[0] * a];
                            var pb = ust[(cy - yon[1] * a) * nx + cx - yon[0] * a];
                            if (!float.IsNaN(pa)) { yanA = Math.Max(yanA, pa); veriVar = true; }
                            if (!float.IsNaN(pb)) { yanB = Math.Max(yanB, pb); veriVar = true; }
                        }
                        // İki yanı da boş (görülmeyen) olan tek hücre sırt sayılmaz; bir yanı boşsa o yan alçak kabul edilir.
                        if (!veriVar)
                            continue;
                        double fa = u - yanA, fb = u - yanB;
                        if ((fa >= ayar.YanFarki && fb >= ayar.KarsiYanFarki) || (fb >= ayar.YanFarki && fa >= ayar.KarsiYanFarki))
                        {
                            sirt[c] = true;
                            break;
                        }
                    }
                }
            }

            // 5) Sırt hücrelerini birleştir (8 komşuluk; 1 hücrelik boşluk atlanır), uzun ve dar parçaları al.
            var etiket = new int[n];
            var parcalar = new List<Parca>();
            var kuyruk = new Queue<int>();
            for (var c = 0; c < n; c++)
            {
                if (!sirt[c] || etiket[c] != 0)
                    continue;
                var p = new Parca { No = parcalar.Count + 1 };
                parcalar.Add(p);
                etiket[c] = p.No;
                kuyruk.Enqueue(c);
                while (kuyruk.Count > 0)
                {
                    var q = kuyruk.Dequeue();
                    int qx = q % nx, qy = q / nx;
                    p.Ekle(qx * h, qy * h);
                    for (var dy = -2; dy <= 2; dy++)
                    {
                        for (var dx = -2; dx <= 2; dx++)
                        {
                            int sx = qx + dx, sy = qy + dy;
                            if (sx < 0 || sy < 0 || sx >= nx || sy >= ny)
                                continue;
                            var s = sy * nx + sx;
                            if (sirt[s] && etiket[s] == 0)
                            {
                                etiket[s] = p.No;
                                kuyruk.Enqueue(s);
                            }
                        }
                    }
                }
            }
            foreach (var p in parcalar)
                p.Hesapla(h);
            var aday = new List<Parca>();
            foreach (var p in parcalar)
            {
                if (p.Boy >= ayar.EnAzBoy && p.Sayi * h * h / p.Boy <= ayar.EnFazlaGenislik)
                    aday.Add(p);
            }

            // 6) İki raylı hat: parçanın kendisine paralel, ray başları arası kadar ötede ve boyuna örtüşen bir eşi olmalı.
            var gecerli = new bool[parcalar.Count + 1];
            foreach (var p in aday)
            {
                if (!ayar.EsAra)
                {
                    gecerli[p.No] = true;
                    continue;
                }
                foreach (var e in aday)
                {
                    if (ReferenceEquals(p, e))
                        continue;
                    var aciFarki = Math.Abs(p.Ux * e.Uy - p.Uy * e.Ux);
                    if (aciFarki > 0.09)
                        continue;
                    double dx = e.Mx - p.Mx, dy = e.My - p.My;
                    var dik = Math.Abs(-dx * p.Uy + dy * p.Ux);
                    var boyuna = dx * p.Ux + dy * p.Uy;
                    if (Math.Abs(dik - (ayar.HatAcikligi + ayar.RayBasiGenisligi)) > ayar.EsToleransi)
                        continue;
                    // Boyuna örtüşme: merkezler arası boyuna fark, yarı boylar toplamından küçük.
                    if (Math.Abs(boyuna) > (p.Boy + e.Boy) / 2)
                        continue;
                    gecerli[p.No] = true;
                    break;
                }
            }

            // 7) Ray hücreleri (1 hücre genişletilmiş) ve her hücrenin ray başı kotu (komşu ray hücrelerinin en yükseği).
            var rayUstu = new float[n];
            for (var c = 0; c < n; c++)
                rayUstu[c] = float.NaN;
            for (var c = 0; c < n; c++)
            {
                if (etiket[c] == 0 || !gecerli[etiket[c]])
                    continue;
                int cx = c % nx, cy = c / nx;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        int sx = cx + dx, sy = cy + dy;
                        if (sx < 0 || sy < 0 || sx >= nx || sy >= ny)
                            continue;
                        var s = sy * nx + sx;
                        if (float.IsNaN(rayUstu[s]) || ust[c] > rayUstu[s])
                            rayUstu[s] = ust[c];
                    }
                }
            }

            // 8) Noktalar: karonun kendi alanındaysa ve ray hücresinde ray başından RayYuksekligi aşağısına kadarsa ray.
            for (var j = bas; j < son; j++)
            {
                var i = dizi[j];
                double x = b.X[i], y = b.Y[i];
                if (x < karoX || x >= karoX + KaroBoyu || y < karoY || y >= karoY + KaroBoyu)
                    continue;
                var c = Hucre(i);
                if (c < 0 || float.IsNaN(rayUstu[c]))
                    continue;
                var z = b.Z[i];
                if (z <= rayUstu[c] + 0.02 && z >= rayUstu[c] - ayar.RayYuksekligi)
                    sonuc[i] = true;
            }
        }

        // Tek doğrultuda kayan pencere en küçüğü (NaN'lar atlanır).
        private static float[] MinFiltre(float[] giris, int nx, int ny, int r, bool yatay)
        {
            var cikis = new float[giris.Length];
            var uzunluk = yatay ? nx : ny;
            var satirSayisi = yatay ? ny : nx;
            var tampon = new float[uzunluk];
            for (var s = 0; s < satirSayisi; s++)
            {
                for (var k = 0; k < uzunluk; k++)
                    tampon[k] = giris[yatay ? s * nx + k : k * nx + s];
                for (var k = 0; k < uzunluk; k++)
                {
                    var m = float.NaN;
                    for (var t = Math.Max(0, k - r); t <= Math.Min(uzunluk - 1, k + r); t++)
                    {
                        var v = tampon[t];
                        if (!float.IsNaN(v) && (float.IsNaN(m) || v < m))
                            m = v;
                    }
                    cikis[yatay ? s * nx + k : k * nx + s] = m;
                }
            }
            return cikis;
        }

        // Birleşik sırt parçası: hücre merkezlerinden ana doğrultu, boy ve merkez.
        private sealed class Parca
        {
            public int No, Sayi;
            private double _sx, _sy, _sxx, _syy, _sxy;
            private readonly List<double> _xs = new List<double>(), _ys = new List<double>();
            public double Mx, My, Ux, Uy, Boy;

            public void Ekle(double x, double y)
            {
                Sayi++;
                _sx += x; _sy += y; _sxx += x * x; _syy += y * y; _sxy += x * y;
                _xs.Add(x);
                _ys.Add(y);
            }

            public void Hesapla(double hucre)
            {
                Mx = _sx / Sayi;
                My = _sy / Sayi;
                double cxx = _sxx / Sayi - Mx * Mx, cyy = _syy / Sayi - My * My, cxy = _sxy / Sayi - Mx * My;
                var aci = 0.5 * Math.Atan2(2 * cxy, cxx - cyy);
                Ux = Math.Cos(aci);
                Uy = Math.Sin(aci);
                double enAz = double.MaxValue, enCok = double.MinValue;
                for (var k = 0; k < _xs.Count; k++)
                {
                    var t = (_xs[k] - Mx) * Ux + (_ys[k] - My) * Uy;
                    if (t < enAz) enAz = t;
                    if (t > enCok) enCok = t;
                }
                Boy = enCok - enAz + hucre;
            }
        }
    }
}
