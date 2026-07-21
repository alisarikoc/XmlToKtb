using System;
using System.Collections.Generic;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Merkez+yarıçap dışındaki daire inşa yöntemlerinin (3 nokta, 2 nokta+yarıçap, teğet TTR/TTT) geometrisi.</summary>
    public static class DaireInsaHesaplari
    {
        private const double Epsilon = 1e-9;

        /// <summary>Bir teğet hedefini (Çizgi/Yay/Daire) tekdüze temsil eder: ya sonsuz doğru (ax+by+c=0) ya da daire (merkez,yarıçap).</summary>
        private struct HedefEgri
        {
            public bool Cizgi;
            public double A, B, C;
            public double Cx, Cy, R;
        }

        public static DaireNesnesi UcNoktadanGecen(PointF p1, PointF p2, PointF p3)
        {
            double x1 = p1.X, y1 = p1.Y, x2 = p2.X, y2 = p2.Y, x3 = p3.X, y3 = p3.Y;
            var d = 2 * (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));
            if (Math.Abs(d) < Epsilon)
                return null;

            var ux = ((x1 * x1 + y1 * y1) * (y2 - y3) + (x2 * x2 + y2 * y2) * (y3 - y1) + (x3 * x3 + y3 * y3) * (y1 - y2)) / d;
            var uy = ((x1 * x1 + y1 * y1) * (x3 - x2) + (x2 * x2 + y2 * y2) * (x1 - x3) + (x3 * x3 + y3 * y3) * (x2 - x1)) / d;
            var yaricap = Math.Sqrt((ux - x1) * (ux - x1) + (uy - y1) * (uy - y1));
            return new DaireNesnesi { MerkezX = ux, MerkezY = uy, Yaricap = yaricap };
        }

        public static List<DaireNesnesi> IkiNoktaVeYaricap(PointF p1, PointF p2, double yaricap)
        {
            var sonuc = new List<DaireNesnesi>();
            if (yaricap <= 0)
                return sonuc;

            var mx = (p1.X + p2.X) / 2.0;
            var my = (p1.Y + p2.Y) / 2.0;
            var dx = (double)p2.X - p1.X;
            var dy = (double)p2.Y - p1.Y;
            var uzunluk = Math.Sqrt(dx * dx + dy * dy);
            if (uzunluk < Epsilon)
                return sonuc;

            var yariMesafe = uzunluk / 2.0;
            if (yaricap < yariMesafe - Epsilon)
                return sonuc;

            var hKare = yaricap * yaricap - yariMesafe * yariMesafe;
            var h = hKare > 0 ? Math.Sqrt(hKare) : 0;
            var nx = -dy / uzunluk;
            var ny = dx / uzunluk;

            sonuc.Add(new DaireNesnesi { MerkezX = mx + h * nx, MerkezY = my + h * ny, Yaricap = yaricap });
            if (h > Epsilon)
                sonuc.Add(new DaireNesnesi { MerkezX = mx - h * nx, MerkezY = my - h * ny, Yaricap = yaricap });

            return sonuc;
        }

        /// <summary>Verilen iki objeye teğet, verilen yarıçapta olası tüm daireleri döndürür (TTR).</summary>
        public static List<DaireNesnesi> TegetIkiObjeYaricap(CizimNesnesi obje1, CizimNesnesi obje2, double yaricap)
        {
            var sonuc = new List<DaireNesnesi>();
            if (yaricap <= 0)
                return sonuc;

            var h1 = HedefOlustur(obje1);
            var h2 = HedefOlustur(obje2);
            if (h1 == null || h2 == null)
                return sonuc;

            var lokus1 = OffsetLokuslari(h1.Value, yaricap);
            var lokus2 = OffsetLokuslari(h2.Value, yaricap);

            foreach (var l1 in lokus1)
            foreach (var l2 in lokus2)
            foreach (var merkez in LokusKesisimi(l1, l2))
                EkleBenzersiz(sonuc, new DaireNesnesi { MerkezX = merkez.X, MerkezY = merkez.Y, Yaricap = yaricap });

            return sonuc;
        }

        /// <summary>Verilen üç objeye teğet olası tüm daireleri döndürür (TTT / Apollonius problemi, sayısal çözüm).</summary>
        public static List<DaireNesnesi> TegetUcObje(CizimNesnesi obje1, CizimNesnesi obje2, CizimNesnesi obje3, PointF baslangicTahmini)
        {
            var sonuc = new List<DaireNesnesi>();
            var h1 = HedefOlustur(obje1);
            var h2 = HedefOlustur(obje2);
            var h3 = HedefOlustur(obje3);
            if (h1 == null || h2 == null || h3 == null)
                return sonuc;

            var hedefler = new[] { h1.Value, h2.Value, h3.Value };

            for (var v1 = 0; v1 < 2; v1++)
            for (var v2 = 0; v2 < 2; v2++)
            for (var v3 = 0; v3 < 2; v3++)
            {
                var varyant = new[] { v1, v2, v3 };
                var cozum = NewtonIleCoz(hedefler, varyant, baslangicTahmini);
                if (cozum != null && !GirdiObjelerdenBiriyleAyni(cozum, hedefler))
                    EkleBenzersiz(sonuc, cozum);
            }

            return sonuc;
        }

        /// <summary>Sayısal çözücü bazen dejenere olarak girdi objelerinden birinin aynısını (kendisiyle teğet) bulabiliyor; bunlar gerçek çözüm değildir.</summary>
        private static bool GirdiObjelerdenBiriyleAyni(DaireNesnesi aday, HedefEgri[] hedefler)
        {
            foreach (var h in hedefler)
            {
                if (h.Cizgi)
                    continue;

                var dx = h.Cx - aday.MerkezX;
                var dy = h.Cy - aday.MerkezY;
                if (dx * dx + dy * dy < 1e-6 && Math.Abs(h.R - aday.Yaricap) < 1e-6)
                    return true;
            }
            return false;
        }

        private static HedefEgri? HedefOlustur(CizimNesnesi nesne)
        {
            if (nesne is CizgiNesnesi c)
            {
                var dx = c.BitisX - c.BaslangicX;
                var dy = c.BitisY - c.BaslangicY;
                var uzunluk = Math.Sqrt(dx * dx + dy * dy);
                if (uzunluk < Epsilon)
                    return null;

                var a = dy / uzunluk;
                var b = -dx / uzunluk;
                var cc = -(a * c.BaslangicX + b * c.BaslangicY);
                return new HedefEgri { Cizgi = true, A = a, B = b, C = cc };
            }

            if (nesne is DaireNesnesi d)
                return new HedefEgri { Cizgi = false, Cx = d.MerkezX, Cy = d.MerkezY, R = d.Yaricap };

            if (nesne is YayNesnesi y)
                return new HedefEgri { Cizgi = false, Cx = y.MerkezX, Cy = y.MerkezY, R = y.Yaricap };

            return null;
        }

        private static List<HedefEgri> OffsetLokuslari(HedefEgri h, double r)
        {
            var sonuc = new List<HedefEgri>();
            if (h.Cizgi)
            {
                sonuc.Add(new HedefEgri { Cizgi = true, A = h.A, B = h.B, C = h.C - r });
                sonuc.Add(new HedefEgri { Cizgi = true, A = h.A, B = h.B, C = h.C + r });
            }
            else
            {
                sonuc.Add(new HedefEgri { Cizgi = false, Cx = h.Cx, Cy = h.Cy, R = h.R + r });
                sonuc.Add(new HedefEgri { Cizgi = false, Cx = h.Cx, Cy = h.Cy, R = Math.Abs(h.R - r) });
            }
            return sonuc;
        }

        private static List<PointF> LokusKesisimi(HedefEgri l1, HedefEgri l2)
        {
            if (l1.Cizgi && l2.Cizgi) return CizgiCizgiKesisimi(l1, l2);
            if (l1.Cizgi) return CizgiDaireKesisimi(l1, l2);
            if (l2.Cizgi) return CizgiDaireKesisimi(l2, l1);
            return DaireDaireKesisimi(l1, l2);
        }

        private static List<PointF> CizgiCizgiKesisimi(HedefEgri l1, HedefEgri l2)
        {
            var det = l1.A * l2.B - l2.A * l1.B;
            if (Math.Abs(det) < Epsilon)
                return new List<PointF>();

            var x = (l1.B * l2.C - l2.B * l1.C) / det;
            var y = (l2.A * l1.C - l1.A * l2.C) / det;
            return new List<PointF> { new PointF((float)x, (float)y) };
        }

        private static List<PointF> CizgiDaireKesisimi(HedefEgri cizgi, HedefEgri daire)
        {
            var d = cizgi.A * daire.Cx + cizgi.B * daire.Cy + cizgi.C;
            var ayakX = daire.Cx - d * cizgi.A;
            var ayakY = daire.Cy - d * cizgi.B;
            var rKare = daire.R * daire.R - d * d;
            if (rKare < -Epsilon)
                return new List<PointF>();
            if (rKare < 0) rKare = 0;

            var yariUzunluk = Math.Sqrt(rKare);
            var dirX = -cizgi.B;
            var dirY = cizgi.A;

            var sonuc = new List<PointF> { new PointF((float)(ayakX + yariUzunluk * dirX), (float)(ayakY + yariUzunluk * dirY)) };
            if (yariUzunluk > Epsilon)
                sonuc.Add(new PointF((float)(ayakX - yariUzunluk * dirX), (float)(ayakY - yariUzunluk * dirY)));
            return sonuc;
        }

        private static List<PointF> DaireDaireKesisimi(HedefEgri d1, HedefEgri d2)
        {
            var dx = d2.Cx - d1.Cx;
            var dy = d2.Cy - d1.Cy;
            var mesafe = Math.Sqrt(dx * dx + dy * dy);
            if (mesafe < Epsilon || mesafe > d1.R + d2.R + Epsilon || mesafe < Math.Abs(d1.R - d2.R) - Epsilon)
                return new List<PointF>();

            var a = (d1.R * d1.R - d2.R * d2.R + mesafe * mesafe) / (2 * mesafe);
            var hKare = d1.R * d1.R - a * a;
            var h = hKare > 0 ? Math.Sqrt(hKare) : 0;

            var ortaX = d1.Cx + a * dx / mesafe;
            var ortaY = d1.Cy + a * dy / mesafe;

            var sonuc = new List<PointF> { new PointF((float)(ortaX + h * dy / mesafe), (float)(ortaY - h * dx / mesafe)) };
            if (h > Epsilon)
                sonuc.Add(new PointF((float)(ortaX - h * dy / mesafe), (float)(ortaY + h * dx / mesafe)));
            return sonuc;
        }

        private static void EkleBenzersiz(List<DaireNesnesi> liste, DaireNesnesi aday)
        {
            foreach (var mevcut in liste)
            {
                var dx = mevcut.MerkezX - aday.MerkezX;
                var dy = mevcut.MerkezY - aday.MerkezY;
                if (dx * dx + dy * dy < 1e-6 && Math.Abs(mevcut.Yaricap - aday.Yaricap) < 1e-6)
                    return;
            }
            liste.Add(aday);
        }

        private static DaireNesnesi NewtonIleCoz(HedefEgri[] hedefler, int[] varyant, PointF baslangicTahmini)
        {
            double x = baslangicTahmini.X;
            double y = baslangicTahmini.Y;
            double r = 0;
            for (var i = 0; i < 3; i++)
                r += MesafeTahmini(hedefler[i], x, y);
            r = Math.Max(r / 3.0, 1e-3);

            for (var adim = 0; adim < 80; adim++)
            {
                var f = new double[3];
                for (var i = 0; i < 3; i++)
                    f[i] = Kalinti(hedefler[i], varyant[i], x, y, r);

                var maxKalinti = Math.Max(Math.Abs(f[0]), Math.Max(Math.Abs(f[1]), Math.Abs(f[2])));
                if (maxKalinti < 1e-7)
                    break;

                const double h = 1e-5;
                var jakobiyen = new double[3, 3];
                for (var i = 0; i < 3; i++)
                {
                    jakobiyen[i, 0] = (Kalinti(hedefler[i], varyant[i], x + h, y, r) - f[i]) / h;
                    jakobiyen[i, 1] = (Kalinti(hedefler[i], varyant[i], x, y + h, r) - f[i]) / h;
                    jakobiyen[i, 2] = (Kalinti(hedefler[i], varyant[i], x, y, r + h) - f[i]) / h;
                }

                if (!DogrusalSistemCoz(jakobiyen, f, out var delta))
                    return null;

                x -= delta[0];
                y -= delta[1];
                r -= delta[2];

                if (r <= 0)
                    r = 1e-3;
                if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(r))
                    return null;
            }

            var sonKalinti = Math.Max(Math.Abs(Kalinti(hedefler[0], varyant[0], x, y, r)),
                Math.Max(Math.Abs(Kalinti(hedefler[1], varyant[1], x, y, r)), Math.Abs(Kalinti(hedefler[2], varyant[2], x, y, r))));

            if (sonKalinti > 1e-4 || r <= 1e-6 || double.IsNaN(x) || double.IsNaN(y))
                return null;

            return new DaireNesnesi { MerkezX = x, MerkezY = y, Yaricap = r };
        }

        private static double MesafeTahmini(HedefEgri h, double x, double y)
        {
            return h.Cizgi ? Math.Abs(h.A * x + h.B * y + h.C) : Math.Sqrt((x - h.Cx) * (x - h.Cx) + (y - h.Cy) * (y - h.Cy));
        }

        private static double Kalinti(HedefEgri h, int varyant, double x, double y, double r)
        {
            if (h.Cizgi)
            {
                var d = h.A * x + h.B * y + h.C;
                return d - (varyant == 0 ? r : -r);
            }

            var mesafe = Math.Sqrt((x - h.Cx) * (x - h.Cx) + (y - h.Cy) * (y - h.Cy));
            var hedefMesafe = varyant == 0 ? r + h.R : Math.Abs(r - h.R);
            return mesafe - hedefMesafe;
        }

        private static bool DogrusalSistemCoz(double[,] jakobiyen, double[] f, out double[] delta)
        {
            var a = (double[,])jakobiyen.Clone();
            var b = (double[])f.Clone();
            delta = new double[3];

            for (var p = 0; p < 3; p++)
            {
                var maxSatir = p;
                for (var i = p + 1; i < 3; i++)
                    if (Math.Abs(a[i, p]) > Math.Abs(a[maxSatir, p])) maxSatir = i;

                if (Math.Abs(a[maxSatir, p]) < 1e-12)
                    return false;

                if (maxSatir != p)
                {
                    for (var k = 0; k < 3; k++)
                    {
                        var t = a[p, k]; a[p, k] = a[maxSatir, k]; a[maxSatir, k] = t;
                    }
                    var tb = b[p]; b[p] = b[maxSatir]; b[maxSatir] = tb;
                }

                for (var i = p + 1; i < 3; i++)
                {
                    var oran = a[i, p] / a[p, p];
                    for (var k = p; k < 3; k++)
                        a[i, k] -= oran * a[p, k];
                    b[i] -= oran * b[p];
                }
            }

            for (var i = 2; i >= 0; i--)
            {
                var toplam = b[i];
                for (var k = i + 1; k < 3; k++)
                    toplam -= a[i, k] * delta[k];
                delta[i] = toplam / a[i, i];
            }

            return true;
        }
    }
}
