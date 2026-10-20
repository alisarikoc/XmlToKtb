using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;

namespace XmlToKtb.Arayuz
{
    /// <summary>
    /// Güzergah editörünün menü ve araç çubuğu simgeleri: some, kurp, spiral ve parça komutları için temsili çizimler;
    /// genel komutlar (kes, kopyala, yapıştır, geri al...) Windows simge yazı tipinden alınır.
    /// </summary>
    public static class GuzergahSimgeleri
    {
        private static readonly Color Koyu = Color.FromArgb(32, 64, 112);
        private static readonly Color Kirmizi = Color.FromArgb(205, 55, 40);
        private static readonly Color Yesil = Color.FromArgb(40, 140, 70);
        private static readonly Color Turuncu = Color.FromArgb(230, 140, 20);
        private static readonly Color Gri = Color.FromArgb(140, 140, 140);

        private static readonly Dictionary<string, Bitmap> Onbellek = new Dictionary<string, Bitmap>();
        private static bool? _mdl2Var;

        private static readonly Dictionary<string, Action<Graphics, float>> Cizimler = new Dictionary<string, Action<Graphics, float>>
        {
            { "SomeCiz", SomeCiz }, { "SomeEkle", SomeEkle }, { "SomeSil", SomeSil }, { "DogrultudanCiz", DogrultudanCiz },
            { "ObjedenOlustur", ObjedenOlustur }, { "KurpYerlestir", KurpYerlestir }, { "KurpTeget", KurpTeget },
            { "KurpYaricap", KurpYaricap }, { "KurpUcNokta", KurpUcNokta }, { "KurpDeger", KurpDeger }, { "SpiralEkle", SpiralEkle },
            { "SpiralKurba", SpiralKurba }, { "SpiralliKurp", SpiralliKurp }, { "ParcaSil", ParcaSil }, { "SatirSil", SatirSil },
            { "KopukBagla", KopukBagla }, { "KopukSil", KopukSil }, { "Uzat", Uzat }, { "Dogru", Dogru }, { "Kurp", Kurp }, { "Spiral", Spiral }
        };

        // Windows simge yazı tipindeki (Segoe MDL2 Assets) genel simgeler.
        private static readonly Dictionary<string, KeyValuePair<char, Color>> Glifler = new Dictionary<string, KeyValuePair<char, Color>>
        {
            { "Ekle", new KeyValuePair<char, Color>('', Yesil) }, { "Cikar", new KeyValuePair<char, Color>('', Kirmizi) },
            { "Kes", new KeyValuePair<char, Color>('', Koyu) }, { "Kopyala", new KeyValuePair<char, Color>('', Koyu) },
            { "Yapistir", new KeyValuePair<char, Color>('', Koyu) }, { "Yukari", new KeyValuePair<char, Color>('', Koyu) },
            { "Asagi", new KeyValuePair<char, Color>('', Koyu) }, { "GeriAl", new KeyValuePair<char, Color>('', Koyu) }
        };

        /// <summary>Verilen komut anahtarının simgesi (boyut × boyut piksel); anahtar bilinmiyorsa null.</summary>
        public static Bitmap Al(string anahtar, int boyut = 16)
        {
            var onbellekAnahtari = anahtar + "|" + boyut;
            if (Onbellek.TryGetValue(onbellekAnahtari, out var hazir))
                return hazir;
            if (!Cizimler.ContainsKey(anahtar) && !Glifler.ContainsKey(anahtar))
                return null;

            var simge = new Bitmap(boyut, boyut);
            using (var g = Graphics.FromImage(simge))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                if (Cizimler.TryGetValue(anahtar, out var cizim))
                    cizim(g, boyut);
                else
                    GlifCiz(g, boyut, Glifler[anahtar].Key, Glifler[anahtar].Value);
            }
            Onbellek[onbellekAnahtari] = simge;
            return simge;
        }

        private static void GlifCiz(Graphics g, int boyut, char kod, Color renk)
        {
            if (_mdl2Var == null)
            {
                using (var yazilar = new InstalledFontCollection())
                    _mdl2Var = yazilar.Families.Any(f => f.Name == "Segoe MDL2 Assets");
            }
            if (_mdl2Var != true)
                return;
            using (var yazi = new Font("Segoe MDL2 Assets", boyut * 0.75f, GraphicsUnit.Pixel))
            using (var bicim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var firca = new SolidBrush(renk))
                g.DrawString(kod.ToString(), yazi, firca, new RectangleF(0, 1, boyut, boyut), bicim);
        }

        // ------------------------------------------------------------------ çizim yardımcıları (0..1 birim koordinat)

        private static Pen K(float s, Color? renk = null, float kat = 1f, DashStyle tur = DashStyle.Solid) =>
            new Pen(renk ?? Koyu, Math.Max(1f, s / 14f) * kat) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round, DashStyle = tur };

        private static PointF P(float s, double x, double y) => new PointF((float)(x * s), (float)(y * s));

        private static void Kutu(Graphics g, float s, double x, double y, Color? renk = null)
        {
            var a = Math.Max(2.5f, s * 0.16f);
            using (var f = new SolidBrush(renk ?? Kirmizi))
                g.FillRectangle(f, (float)(x * s) - a / 2, (float)(y * s) - a / 2, a, a);
        }

        // Some noktası: içi boş üçgen.
        private static void Ucgen(Graphics g, float s, double x, double y, Color? renk = null)
        {
            var a = 0.16;
            using (var k = K(s, renk ?? Kirmizi))
                g.DrawPolygon(k, new[] { P(s, x, y - a), P(s, x - a, y + a * 0.75), P(s, x + a, y + a * 0.75) });
        }

        private static void Ok(Graphics g, float s, double x, double y, double dx, double dy, Color? renk = null)
        {
            var l = Math.Sqrt(dx * dx + dy * dy);
            dx /= l; dy /= l;
            var b = 0.26;
            var pts = new[] { P(s, x, y), P(s, x - dx * b + dy * b * 0.5, y - dy * b - dx * b * 0.5), P(s, x - dx * b - dy * b * 0.5, y - dy * b + dx * b * 0.5) };
            using (var f = new SolidBrush(renk ?? Koyu))
                g.FillPolygon(f, pts);
        }

        private static void Arti(Graphics g, float s, Color renk)
        {
            using (var k = K(s, renk, 1.6f)) { g.DrawLine(k, P(s, 0.62, 0.8), P(s, 0.94, 0.8)); g.DrawLine(k, P(s, 0.78, 0.64), P(s, 0.78, 0.96)); }
        }

        private static void Carpi(Graphics g, float s)
        {
            using (var k = K(s, Kirmizi, 1.6f)) { g.DrawLine(k, P(s, 0.64, 0.66), P(s, 0.94, 0.96)); g.DrawLine(k, P(s, 0.94, 0.66), P(s, 0.64, 0.96)); }
        }

        // Some poligonu: üç doğru, iki some.
        private static void SomePoligonu(Graphics g, float s)
        {
            var p = new[] { P(s, 0.06, 0.82), P(s, 0.36, 0.3), P(s, 0.66, 0.62), P(s, 0.94, 0.12) };
            using (var k = K(s)) g.DrawLines(k, p);
        }

        // ------------------------------------------------------------------ simgeler

        private static void SomeCiz(Graphics g, float s)
        {
            SomePoligonu(g, s);
            Ucgen(g, s, 0.36, 0.34); Ucgen(g, s, 0.66, 0.62);
            Kutu(g, s, 0.06, 0.82, Koyu); Kutu(g, s, 0.94, 0.12, Koyu);
        }

        private static void SomeEkle(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLines(k, new[] { P(s, 0.06, 0.7), P(s, 0.42, 0.2), P(s, 0.94, 0.5) });
            Ucgen(g, s, 0.42, 0.24);
            Arti(g, s, Yesil);
        }

        private static void SomeSil(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLines(k, new[] { P(s, 0.06, 0.7), P(s, 0.42, 0.2), P(s, 0.94, 0.5) });
            Ucgen(g, s, 0.42, 0.24);
            Carpi(g, s);
        }

        private static void DogrultudanCiz(Graphics g, float s)
        {
            // Referans obje (gri) ve ucundan aynı doğrultuda devam eden doğru.
            using (var k = K(s, Gri)) g.DrawLine(k, P(s, 0.06, 0.94), P(s, 0.36, 0.64));
            using (var k = K(s, Kirmizi, 1f, DashStyle.Dot)) g.DrawLine(k, P(s, 0.36, 0.64), P(s, 0.96, 0.04));
            using (var k = K(s, Koyu, 1.3f)) g.DrawLine(k, P(s, 0.36, 0.64), P(s, 0.74, 0.26));
            Ok(g, s, 0.84, 0.16, 1, -1);
            Kutu(g, s, 0.36, 0.64);
        }

        private static void ObjedenOlustur(Graphics g, float s)
        {
            using (var k = K(s, Gri)) { g.DrawLine(k, P(s, 0.06, 0.9), P(s, 0.4, 0.9)); g.DrawArc(k, s * 0.2f, s * 0.42f, s * 0.48f, s * 0.48f, 270, 90); }
            using (var k = K(s, Koyu, 1.2f)) g.DrawLine(k, P(s, 0.3, 0.32), P(s, 0.72, 0.32));
            Ok(g, s, 0.92, 0.32, 1, 0, Yesil);
            Kutu(g, s, 0.06, 0.9, Koyu);
        }

        private static void Dogru(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.1, 0.9), P(s, 0.9, 0.1));
            Kutu(g, s, 0.1, 0.9); Kutu(g, s, 0.9, 0.1);
        }

        private static void Kurp(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawArc(k, s * 0.1f, s * 0.2f, s * 0.8f, s * 1.2f, 190, 160);
            Kutu(g, s, 0.12, 0.66); Kutu(g, s, 0.88, 0.66);
        }

        private static void Spiral(Graphics g, float s)
        {
            // Eğriliği giderek artan eğri.
            using (var k = K(s)) g.DrawBezier(k, P(s, 0.06, 0.86), P(s, 0.55, 0.86), P(s, 0.82, 0.6), P(s, 0.9, 0.12));
            Kutu(g, s, 0.06, 0.86); Kutu(g, s, 0.9, 0.12);
        }

        // İki teğet doğru, somede kurp.
        private static void KurpYerlestir(Graphics g, float s)
        {
            using (var k = K(s, Gri, 1f, DashStyle.Dash)) { g.DrawLine(k, P(s, 0.06, 0.92), P(s, 0.5, 0.2)); g.DrawLine(k, P(s, 0.5, 0.2), P(s, 0.94, 0.92)); }
            using (var k = K(s, Koyu, 1.3f)) g.DrawArc(k, s * 0.2f, s * 0.42f, s * 0.6f, s * 0.6f, 215, 110);
            Ucgen(g, s, 0.5, 0.2);
        }

        private static void KurpTeget(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.04, 0.86), P(s, 0.4, 0.86));
            using (var k = K(s, Koyu, 1.3f)) g.DrawArc(k, s * 0.04f, s * 0.14f, s * 0.72f, s * 0.72f, 90, -90);
            Kutu(g, s, 0.76, 0.5);
        }

        private static void KurpYaricap(Graphics g, float s)
        {
            KurpTeget(g, s);
            using (var yazi = new Font("Segoe UI", s * 0.5f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var f = new SolidBrush(Kirmizi))
                g.DrawString("R", yazi, f, s * 0.04f, -s * 0.04f);
        }

        private static void KurpUcNokta(Graphics g, float s)
        {
            using (var k = K(s, Koyu, 1.3f)) g.DrawArc(k, s * 0.1f, s * 0.2f, s * 0.8f, s * 1.2f, 190, 160);
            Kutu(g, s, 0.12, 0.66); Kutu(g, s, 0.5, 0.2); Kutu(g, s, 0.88, 0.66);
        }

        private static void KurpDeger(Graphics g, float s)
        {
            using (var k = K(s, Koyu, 1.3f)) g.DrawArc(k, s * 0.04f, s * 0.3f, s * 0.7f, s * 1.0f, 190, 150);
            using (var k = K(s, Kirmizi)) { g.DrawLine(k, P(s, 0.6, 0.72), P(s, 0.96, 0.72)); g.DrawLine(k, P(s, 0.6, 0.88), P(s, 0.96, 0.88)); }
        }

        private static void SpiralEkle(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.04, 0.86), P(s, 0.3, 0.86));
            using (var k = K(s, Turuncu, 1.3f)) g.DrawBezier(k, P(s, 0.3, 0.86), P(s, 0.62, 0.86), P(s, 0.8, 0.62), P(s, 0.84, 0.2));
            Kutu(g, s, 0.84, 0.2);
        }

        private static void SpiralKurba(Graphics g, float s)
        {
            using (var k = K(s, Koyu, 1.3f)) g.DrawArc(k, s * 0.22f, s * 0.2f, s * 0.56f, s * 0.6f, 215, 110);
            using (var k = K(s, Turuncu, 1.3f))
            {
                g.DrawBezier(k, P(s, 0.02, 0.86), P(s, 0.16, 0.7), P(s, 0.22, 0.5), P(s, 0.29, 0.33));
                g.DrawBezier(k, P(s, 0.71, 0.33), P(s, 0.78, 0.5), P(s, 0.84, 0.7), P(s, 0.98, 0.86));
            }
        }

        private static void SpiralliKurp(Graphics g, float s)
        {
            using (var k = K(s, Gri, 1f, DashStyle.Dash)) { g.DrawLine(k, P(s, 0.06, 0.94), P(s, 0.5, 0.16)); g.DrawLine(k, P(s, 0.5, 0.16), P(s, 0.94, 0.94)); }
            using (var k = K(s, Koyu, 1.3f)) g.DrawArc(k, s * 0.3f, s * 0.42f, s * 0.4f, s * 0.4f, 225, 90);
            using (var k = K(s, Turuncu, 1.3f))
            {
                g.DrawLine(k, P(s, 0.2, 0.7), P(s, 0.36, 0.48));
                g.DrawLine(k, P(s, 0.64, 0.48), P(s, 0.8, 0.7));
            }
            Ucgen(g, s, 0.5, 0.2);
        }

        private static void ParcaSil(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.04, 0.5), P(s, 0.3, 0.5));
            using (var k = K(s, Kirmizi, 1.3f, DashStyle.Dot)) g.DrawLine(k, P(s, 0.3, 0.5), P(s, 0.62, 0.5));
            using (var k = K(s)) g.DrawArc(k, s * 0.5f, s * 0.04f, s * 0.9f, s * 0.92f, 180, 50);
            Carpi(g, s);
        }

        private static void SatirSil(Graphics g, float s)
        {
            using (var k = K(s, Gri))
                for (var i = 0; i < 4; i++)
                    g.DrawLine(k, P(s, 0.06, 0.12 + i * 0.2), P(s, 0.7, 0.12 + i * 0.2));
            using (var f = new SolidBrush(Color.FromArgb(90, Kirmizi)))
                g.FillRectangle(f, s * 0.04f, s * 0.24f, s * 0.68f, s * 0.16f);
            Carpi(g, s);
        }

        private static void KopukBagla(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.04, 0.78), P(s, 0.36, 0.54));
            using (var k = K(s, Gri, 1f, DashStyle.Dash)) g.DrawLine(k, P(s, 0.64, 0.38), P(s, 0.96, 0.14));
            using (var k = K(s, Yesil, 1.4f)) g.DrawLine(k, P(s, 0.36, 0.54), P(s, 0.64, 0.38));
            Kutu(g, s, 0.36, 0.54, Yesil); Kutu(g, s, 0.64, 0.38, Yesil);
        }

        private static void KopukSil(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.04, 0.6), P(s, 0.3, 0.42));
            using (var k = K(s, Gri, 1f, DashStyle.Dash)) g.DrawLine(k, P(s, 0.46, 0.32), P(s, 0.94, 0.04));
            Carpi(g, s);
        }

        private static void Uzat(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.04, 0.5), P(s, 0.5, 0.5));
            using (var k = K(s, Kirmizi, 1f, DashStyle.Dot)) g.DrawLine(k, P(s, 0.5, 0.5), P(s, 0.8, 0.5));
            Ok(g, s, 0.98, 0.5, 1, 0, Kirmizi);
            Kutu(g, s, 0.5, 0.5, Koyu);
        }
    }
}
