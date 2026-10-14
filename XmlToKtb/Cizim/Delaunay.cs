using System;
using System.Collections.Generic;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Noktalardan Delaunay üçgenlemesi (her noktayı en yakın komşularına bağlayan, ince uzun üçgenlerden kaçınan
    /// üçgen ağı) üretir. Noktalar X'e göre sıralanıp tek tek eklenir (Bowyer-Watson, süpürme iyileştirmeli).
    /// </summary>
    public static class Delaunay
    {
        private struct Ucgen
        {
            public int A, B, C;
            // Çevrel çemberin merkezi ve yarıçapının karesi.
            public double Mx, My, R2;
        }

        /// <summary>
        /// Üçgenleri, her biri için art arda üç nokta indeksi olarak döner. Aynı konumdaki (yatayda çakışan)
        /// noktalardan yalnızca ilki üçgenlemeye girer.
        /// </summary>
        public static int[] Ucgenle(double[] x, double[] y)
        {
            var n = x.Length;
            if (n < 3)
                return new int[0];

            // Büyük koordinatlarda (ör. 4 400 000) sayısal hassasiyet kaybolmasın diye noktalar merkeze ötelenir.
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (var i = 0; i < n; i++)
            {
                if (x[i] < minX) minX = x[i];
                if (x[i] > maxX) maxX = x[i];
                if (y[i] < minY) minY = y[i];
                if (y[i] > maxY) maxY = y[i];
            }
            double ortaX = (minX + maxX) / 2, ortaY = (minY + maxY) / 2;
            var boyut = Math.Max(Math.Max(maxX - minX, maxY - minY), 1e-9);

            // Sondaki üç eleman, bütün noktaları içine alan süper üçgenin köşeleridir.
            var px = new double[n + 3];
            var py = new double[n + 3];
            for (var i = 0; i < n; i++)
            {
                px[i] = x[i] - ortaX;
                py[i] = y[i] - ortaY;
            }
            px[n] = -20 * boyut; py[n] = -boyut;
            px[n + 1] = 0; py[n + 1] = 20 * boyut;
            px[n + 2] = 20 * boyut; py[n + 2] = -boyut;

            var sira = new int[n];
            for (var i = 0; i < n; i++)
                sira[i] = i;
            Array.Sort(sira, (a, b) =>
            {
                var c = px[a].CompareTo(px[b]);
                return c != 0 ? c : py[a].CompareTo(py[b]);
            });

            var acik = new List<Ucgen> { UcgenOlustur(px, py, n, n + 1, n + 2) };
            var kapali = new List<Ucgen>();
            var kenarlar = new List<int>();
            var cakismaToleransi = boyut * 1e-12;

            var onceki = -1;
            foreach (var p in sira)
            {
                if (onceki >= 0 && Math.Abs(px[p] - px[onceki]) <= cakismaToleransi && Math.Abs(py[p] - py[onceki]) <= cakismaToleransi)
                    continue;
                onceki = p;

                kenarlar.Clear();
                for (var j = acik.Count - 1; j >= 0; j--)
                {
                    var u = acik[j];
                    var dx = px[p] - u.Mx;

                    // Noktalar X sırasıyla geldiğinden, çemberi tamamen solda kalan üçgene artık hiçbir nokta giremez.
                    if (dx > 0 && dx * dx > u.R2)
                    {
                        kapali.Add(u);
                        acik[j] = acik[acik.Count - 1];
                        acik.RemoveAt(acik.Count - 1);
                        continue;
                    }

                    var dy = py[p] - u.My;
                    if (dx * dx + dy * dy > u.R2)
                        continue;

                    kenarlar.Add(u.A); kenarlar.Add(u.B);
                    kenarlar.Add(u.B); kenarlar.Add(u.C);
                    kenarlar.Add(u.C); kenarlar.Add(u.A);
                    acik[j] = acik[acik.Count - 1];
                    acik.RemoveAt(acik.Count - 1);
                }

                // İki silinen üçgenin paylaştığı kenarlar oyuğun içinde kalır; yalnızca oyuğun sınır kenarları yeni
                // noktaya bağlanır.
                for (var i = 0; i < kenarlar.Count; i += 2)
                {
                    if (kenarlar[i] < 0)
                        continue;

                    var ortak = false;
                    for (var k = i + 2; k < kenarlar.Count; k += 2)
                    {
                        if (kenarlar[k] < 0)
                            continue;
                        if ((kenarlar[i] == kenarlar[k] && kenarlar[i + 1] == kenarlar[k + 1])
                            || (kenarlar[i] == kenarlar[k + 1] && kenarlar[i + 1] == kenarlar[k]))
                        {
                            kenarlar[k] = -1;
                            ortak = true;
                        }
                    }

                    if (!ortak)
                        acik.Add(UcgenOlustur(px, py, kenarlar[i], kenarlar[i + 1], p));
                }
            }

            kapali.AddRange(acik);

            var sonuc = new List<int>(kapali.Count * 3);
            foreach (var u in kapali)
            {
                // Süper üçgenin köşesine değen üçgenler gerçek yüzeye ait değildir.
                if (u.A >= n || u.B >= n || u.C >= n)
                    continue;
                sonuc.Add(u.A);
                sonuc.Add(u.B);
                sonuc.Add(u.C);
            }
            return sonuc.ToArray();
        }

        private static Ucgen UcgenOlustur(double[] px, double[] py, int a, int b, int c)
        {
            var u = new Ucgen { A = a, B = b, C = c };
            double ax = px[a], ay = py[a], bx = px[b], by = py[b], cx = px[c], cy = py[c];
            var d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));

            if (Math.Abs(d) < 1e-300)
            {
                // Doğrusal (yozlaşmış) üçgen: çemberi sonsuz kabul edilir ki sonraki nokta onu silip yeniden kursun.
                u.Mx = ax;
                u.My = ay;
                u.R2 = double.MaxValue;
                return u;
            }

            double a2 = ax * ax + ay * ay, b2 = bx * bx + by * by, c2 = cx * cx + cy * cy;
            u.Mx = (a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / d;
            u.My = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d;
            u.R2 = (ax - u.Mx) * (ax - u.Mx) + (ay - u.My) * (ay - u.My);
            return u;
        }
    }
}
