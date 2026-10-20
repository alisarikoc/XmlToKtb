using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;

namespace XmlToKtb.Arayuz
{
    /// <summary>
    /// Ribbon simgeleri: komut adındaki anahtar kelimeye göre seçilen temsili simge. CAD komutlarının (çizgi, daire,
    /// ölçü, kaydır, kırp, güzergah, kesit, yüzey...) simgeleri çizilir; genel komutlar (aç, kaydet, geri al...)
    /// Windows'un simge yazı tipinden (Segoe MDL2 Assets) alınır. Eşleşme yoksa baş harfli renkli kare çizilir.
    /// </summary>
    public static class RibbonSimgeleri
    {
        private static readonly Color Koyu = Color.FromArgb(32, 64, 112);
        private static readonly Color Kirmizi = Color.FromArgb(205, 55, 40);
        private static readonly Color Yesil = Color.FromArgb(40, 140, 70);
        private static readonly Color Turuncu = Color.FromArgb(230, 140, 20);

        private static readonly Dictionary<string, Bitmap> Onbellek = new Dictionary<string, Bitmap>();
        private static bool? _mdl2Var;

        // Anahtar kelimeler sırayla denenir; daha özel olanlar önce gelir.
        private static readonly List<KeyValuePair<string[], Action<Graphics, float>>> Tablo = new List<KeyValuePair<string[], Action<Graphics, float>>>
        {
            S(new[] { "çoklu doğruya dönüştür" }, Zincir),
            S(new[] { "çoklu doğru" }, CokluDogru),
            S(new[] { "2 noktadan dönüşüm", "hizala" }, Hizala),
            S(new[] { "kılavuz", "leader" }, Kilavuz),
            S(new[] { "hizalı" }, OlcuHizali),
            S(new[] { "doğrusal" }, OlcuDogrusal),
            S(new[] { "açısal" }, OlcuAcisal),
            S(new[] { "yarıçap ölçü", "yarıçap" }, OlcuYaricap),
            S(new[] { "çap" }, OlcuCap),
            S(new[] { "ölçü stil", "aktif ölçü" }, OlcuStili),
            S(new[] { "ölçülendirme" }, OlcuDogrusal),
            S(new[] { "çizgi çiz", "çizgi" }, Cizgi),
            S(new[] { "teğet" }, DaireTeget),
            S(new[] { "daire", "merkez ve yarıçap", "3 nokta", "2 nokta ve yarıçap" }, Daire),
            S(new[] { "yay" }, Yay),
            S(new[] { "nokta editör" }, Tablo_),
            S(new[] { "nokta bulutu", "(las)" }, NoktaBulutu),
            S(new[] { "nokta at", "nokta" }, Nokta),
            S(new[] { "yazı yaz" }, Yazi),
            S(new[] { "kaydır" }, Kaydir),
            S(new[] { "döndür" }, Dondur),
            S(new[] { "ölçekle" }, Olcekle),
            S(new[] { "kırp" }, Kirp),
            S(new[] { "uzat" }, Uzat),
            S(new[] { "paralel" }, Paralel),
            S(new[] { "aynala" }, Aynala),
            S(new[] { "patlat" }, Patlat),
            S(new[] { "toplu obje" }, TopluDegistir),
            S(new[] { "tümünü seç" }, TumunuSec),
            S(new[] { "seçimi kaldır" }, SecimiKaldir),
            S(new[] { "tersine" }, TersSecim),
            S(new[] { "benzer" }, Benzer),
            S(new[] { "önceki seçim" }, OncekiSecim),
            S(new[] { "çit" }, Cit),
            S(new[] { "pencere" }, Pencereler),
            S(new[] { "güzergah editör" }, GuzergahEditoru),
            S(new[] { "güzergah" }, Guzergah),
            S(new[] { "enkesit", "kesit" }, Kesit),
            S(new[] { "poligon" }, Poligon),
            S(new[] { "yüzey", "üçgen" }, Yuzey),
            S(new[] { "kopyala" }, Kopyala)
        };

        // Windows simge yazı tipinden alınan genel simgeler.
        private static readonly List<KeyValuePair<string[], char>> Glifler = new List<KeyValuePair<string[], char>>
        {
            G(new[] { "geri al" }, ''), G(new[] { "ileri al" }, ''),
            G(new[] { "panoya kes", "kes" }, ''), G(new[] { "yapıştır" }, ''),
            G(new[] { "yeni" }, ''), G(new[] { "aç" }, ''), G(new[] { "kaydet" }, ''), G(new[] { "kapat" }, ''),
            G(new[] { "ekle" }, ''), G(new[] { "içe aktar" }, ''), G(new[] { "dışa aktar" }, ''),
            G(new[] { "sil" }, ''),
            G(new[] { "google", "kml" }, ''), G(new[] { "json" }, ''),
            G(new[] { "bul" }, ''), G(new[] { "hızlı seçim" }, ''),
            G(new[] { "arka plan", "renk" }, ''), G(new[] { "etiket" }, ''), G(new[] { "kalın" }, ''),
            G(new[] { "tabaka" }, ''), G(new[] { "giriş ekranı", "ekran" }, ''),
            G(new[] { "hassasiyet", "ayar" }, ''), G(new[] { "hesap" }, ''), G(new[] { "klasik" }, '')
        };

        private static KeyValuePair<string[], Action<Graphics, float>> S(string[] k, Action<Graphics, float> a) => new KeyValuePair<string[], Action<Graphics, float>>(k, a);
        private static KeyValuePair<string[], char> G(string[] k, char c) => new KeyValuePair<string[], char>(k, c);

        /// <summary>Komut adına göre simge (boyut × boyut piksel).</summary>
        public static Bitmap Al(string komut, int boyut)
        {
            var anahtar = (komut ?? "").ToLowerInvariant() + "|" + boyut;
            if (Onbellek.TryGetValue(anahtar, out var hazir))
                return hazir;
            var metin = (komut ?? "").ToLower(new System.Globalization.CultureInfo("tr-TR"));
            var simge = new Bitmap(boyut, boyut);
            using (var g = Graphics.FromImage(simge))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                // Genel komut kelimeleri (tam kelime) CAD kelimelerinden önce denenir: "Panoya Kopyala" pano simgesi alır.
                var glif = Glifler.FirstOrDefault(x => x.Key.Any(k => KelimeVar(metin, k)));
                var cizim = Tablo.FirstOrDefault(x => x.Key.Any(k => metin.Contains(k)));
                if (metin.StartsWith("panoya kopyala") || metin.StartsWith("referans noktası") || metin.StartsWith("baz noktası"))
                    GlifCiz(g, boyut, '', metin.StartsWith("panoya") ? Koyu : Turuncu);
                else if (glif.Key != null && (cizim.Key == null || GlifOncelikli(metin)))
                    GlifCiz(g, boyut, glif.Value, Koyu);
                else if (cizim.Key != null)
                    cizim.Value(g, boyut);
                else
                    BasHarf(g, boyut, komut);
            }
            Onbellek[anahtar] = simge;
            return simge;
        }

        // Metin bu kelimeyle başlıyorsa genel simge seçilir ("Kaydet", "Aç...", "Geri Al").
        private static bool GlifOncelikli(string metin) =>
            Glifler.Any(x => x.Key.Any(k => metin.StartsWith(k)));

        private static bool KelimeVar(string metin, string kelime)
        {
            var i = metin.IndexOf(kelime, StringComparison.Ordinal);
            while (i >= 0)
            {
                var basta = i == 0 || !char.IsLetter(metin[i - 1]);
                var sonda = i + kelime.Length >= metin.Length || !char.IsLetter(metin[i + kelime.Length]);
                if (basta && sonda)
                    return true;
                i = metin.IndexOf(kelime, i + 1, StringComparison.Ordinal);
            }
            return false;
        }

        private static void GlifCiz(Graphics g, int boyut, char kod, Color renk)
        {
            if (_mdl2Var == null)
            {
                using (var yazilar = new InstalledFontCollection())
                    _mdl2Var = yazilar.Families.Any(f => f.Name == "Segoe MDL2 Assets");
            }
            if (_mdl2Var != true)
            {
                BasHarf(g, boyut, kod.ToString());
                return;
            }
            using (var yazi = new Font("Segoe MDL2 Assets", boyut * 0.72f, GraphicsUnit.Pixel))
            using (var bicim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var firca = new SolidBrush(renk))
                g.DrawString(kod.ToString(), yazi, firca, new RectangleF(0, 1, boyut, boyut), bicim);
        }

        private static void BasHarf(Graphics g, int boyut, string metin)
        {
            var temiz = new string((metin ?? "?").Where(char.IsLetterOrDigit).ToArray());
            var harf = temiz.Length > 0 ? temiz.Substring(0, 1).ToUpper() : "?";
            var renkler = new[] { Koyu, Yesil, Turuncu, Color.FromArgb(120, 70, 160), Color.FromArgb(20, 130, 150) };
            var renk = renkler[Math.Abs((metin ?? "").GetHashCode()) % renkler.Length];
            var r = new RectangleF(boyut * 0.1f, boyut * 0.1f, boyut * 0.8f, boyut * 0.8f);
            using (var yol = Yuvarlak(r, boyut * 0.18f))
            using (var firca = new SolidBrush(renk))
                g.FillPath(firca, yol);
            using (var yazi = new Font("Segoe UI", boyut * 0.45f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var bicim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(harf, yazi, Brushes.White, r, bicim);
        }

        private static GraphicsPath Yuvarlak(RectangleF r, float c)
        {
            var yol = new GraphicsPath();
            yol.AddArc(r.X, r.Y, c * 2, c * 2, 180, 90);
            yol.AddArc(r.Right - c * 2, r.Y, c * 2, c * 2, 270, 90);
            yol.AddArc(r.Right - c * 2, r.Bottom - c * 2, c * 2, c * 2, 0, 90);
            yol.AddArc(r.X, r.Bottom - c * 2, c * 2, c * 2, 90, 90);
            yol.CloseFigure();
            return yol;
        }

        // ------------------------------------------------------------------ çizim yardımcıları (0..1 birim koordinat)

        private static Pen K(float s, Color? renk = null, float kat = 1f, DashStyle tur = DashStyle.Solid) => new Pen(renk ?? Koyu, Math.Max(1f, s / 16f) * kat) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round, DashStyle = tur };
        private static PointF P(float s, double x, double y) => new PointF((float)(x * s), (float)(y * s));

        private static void Kutu(Graphics g, float s, double x, double y, Color? renk = null)
        {
            var a = Math.Max(2f, s * 0.09f);
            using (var f = new SolidBrush(renk ?? Kirmizi))
                g.FillRectangle(f, (float)(x * s) - a / 2, (float)(y * s) - a / 2, a, a);
        }

        private static void Ok(Graphics g, float s, double x, double y, double dx, double dy, Color? renk = null)
        {
            var l = Math.Sqrt(dx * dx + dy * dy);
            dx /= l; dy /= l;
            var b = 0.13;
            var pts = new[] { P(s, x, y), P(s, x - dx * b + dy * b * 0.45, y - dy * b - dx * b * 0.45), P(s, x - dx * b - dy * b * 0.45, y - dy * b + dx * b * 0.45) };
            using (var f = new SolidBrush(renk ?? Koyu))
                g.FillPolygon(f, pts);
        }

        private static void Cizgi(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.15, 0.85), P(s, 0.85, 0.15));
            Kutu(g, s, 0.15, 0.85); Kutu(g, s, 0.85, 0.15);
        }

        private static void CokluDogru(Graphics g, float s)
        {
            var p = new[] { P(s, 0.1, 0.8), P(s, 0.32, 0.3), P(s, 0.6, 0.68), P(s, 0.9, 0.2) };
            using (var k = K(s)) g.DrawLines(k, p);
            foreach (var q in p) Kutu(g, s, q.X / s, q.Y / s);
        }

        private static void Zincir(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawLine(k, P(s, 0.1, 0.75), P(s, 0.45, 0.4)); g.DrawLine(k, P(s, 0.55, 0.6), P(s, 0.9, 0.25)); }
            using (var k = K(s, Kirmizi)) g.DrawArc(k, s * 0.38f, s * 0.38f, s * 0.24f, s * 0.24f, 0, 360);
        }

        private static void Daire(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawEllipse(k, s * 0.14f, s * 0.14f, s * 0.72f, s * 0.72f);
            using (var k = K(s, Kirmizi)) { g.DrawLine(k, P(s, 0.42, 0.5), P(s, 0.58, 0.5)); g.DrawLine(k, P(s, 0.5, 0.42), P(s, 0.5, 0.58)); }
        }

        private static void DaireTeget(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawEllipse(k, s * 0.25f, s * 0.2f, s * 0.5f, s * 0.5f); g.DrawLine(k, P(s, 0.05, 0.7), P(s, 0.95, 0.7)); }
            Kutu(g, s, 0.5, 0.7);
        }

        private static void Yay(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawArc(k, s * 0.12f, s * 0.25f, s * 0.76f, s * 0.9f, 200, 140);
            Kutu(g, s, 0.16, 0.55); Kutu(g, s, 0.84, 0.55); Kutu(g, s, 0.5, 0.25);
        }

        private static void Nokta(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawLine(k, P(s, 0.2, 0.5), P(s, 0.8, 0.5)); g.DrawLine(k, P(s, 0.5, 0.2), P(s, 0.5, 0.8)); }
            using (var k = K(s, Kirmizi)) g.DrawEllipse(k, s * 0.38f, s * 0.38f, s * 0.24f, s * 0.24f);
        }

        private static void NoktaBulutu(Graphics g, float s)
        {
            var r = new Random(7);
            using (var f = new SolidBrush(Koyu))
                for (var i = 0; i < 40; i++)
                {
                    double x = 0.1 + r.NextDouble() * 0.8, y = 0.25 + r.NextDouble() * 0.55 - Math.Sin(x * 3) * 0.12;
                    var a = Math.Max(1.5f, s * 0.05f);
                    g.FillEllipse(i % 5 == 0 ? Brushes.ForestGreen : f, (float)(x * s), (float)(y * s), a, a);
                }
        }

        private static void Yazi(Graphics g, float s)
        {
            using (var yazi = new Font("Times New Roman", s * 0.7f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var f = new SolidBrush(Koyu))
            using (var bicim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString("A", yazi, f, new RectangleF(0, 0, s, s * 0.95f), bicim);
            using (var k = K(s, Kirmizi)) g.DrawLine(k, P(s, 0.15, 0.9), P(s, 0.85, 0.9));
        }

        private static void OlcuDogrusal(Graphics g, float s) => OlcuCiz(g, s, Koyu, "");

        private static void OlcuStili(Graphics g, float s)
        {
            OlcuCiz(g, s, Yesil, "");
            using (var f = new SolidBrush(Turuncu)) g.FillEllipse(f, s * 0.58f, s * 0.58f, s * 0.36f, s * 0.36f);
            using (var k = K(s, Color.White)) g.DrawEllipse(k, s * 0.69f, s * 0.69f, s * 0.14f, s * 0.14f);
        }

        private static void OlcuCiz(Graphics g, float s, Color renk, string yazi)
        {
            using (var k = K(s, renk))
            {
                g.DrawLine(k, P(s, 0.15, 0.85), P(s, 0.15, 0.45));
                g.DrawLine(k, P(s, 0.85, 0.85), P(s, 0.85, 0.45));
                g.DrawLine(k, P(s, 0.15, 0.55), P(s, 0.85, 0.55));
            }
            Ok(g, s, 0.15, 0.55, -1, 0, renk); Ok(g, s, 0.85, 0.55, 1, 0, renk);
            using (var k = K(s, Kirmizi, 1.6f)) g.DrawLine(k, P(s, 0.35, 0.35), P(s, 0.65, 0.35));
        }

        private static void OlcuHizali(Graphics g, float s)
        {
            using (var k = K(s))
            {
                g.DrawLine(k, P(s, 0.1, 0.75), P(s, 0.7, 0.15));
                g.DrawLine(k, P(s, 0.2, 0.9), P(s, 0.08, 0.78));
                g.DrawLine(k, P(s, 0.85, 0.3), P(s, 0.72, 0.18));
            }
            Ok(g, s, 0.1, 0.75, -1, 1); Ok(g, s, 0.7, 0.15, 1, -1);
            using (var k = K(s, Kirmizi, 1.6f)) g.DrawLine(k, P(s, 0.28, 0.42), P(s, 0.45, 0.25));
        }

        private static void OlcuAcisal(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawLine(k, P(s, 0.12, 0.85), P(s, 0.92, 0.85)); g.DrawLine(k, P(s, 0.12, 0.85), P(s, 0.7, 0.15)); }
            using (var k = K(s, Kirmizi)) g.DrawArc(k, s * -0.43f, s * 0.3f, s * 1.1f, s * 1.1f, -50, 50);
            Ok(g, s, 0.67, 0.85, 0, 1, Kirmizi);
        }

        private static void OlcuYaricap(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawEllipse(k, s * 0.1f, s * 0.15f, s * 0.7f, s * 0.7f);
            using (var k = K(s, Kirmizi)) g.DrawLine(k, P(s, 0.45, 0.5), P(s, 0.92, 0.12));
            Ok(g, s, 0.7, 0.29, 0.8, -0.64, Kirmizi);
        }

        private static void Kilavuz(Graphics g, float s)
        {
            using (var k = K(s, Kirmizi))
                g.DrawLines(k, new[] { P(s, 0.12, 0.88), P(s, 0.42, 0.42), P(s, 0.56, 0.42) });
            Ok(g, s, 0.12, 0.88, -0.55, 0.85, Kirmizi);
            using (var k = K(s, Koyu, 1.4f))
            {
                g.DrawLine(k, P(s, 0.62, 0.3), P(s, 0.92, 0.3));
                g.DrawLine(k, P(s, 0.62, 0.48), P(s, 0.84, 0.48));
            }
        }

        private static void OlcuCap(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawEllipse(k, s * 0.12f, s * 0.12f, s * 0.76f, s * 0.76f);
            using (var k = K(s, Kirmizi)) g.DrawLine(k, P(s, 0.23, 0.77), P(s, 0.77, 0.23));
            Ok(g, s, 0.77, 0.23, 1, -1, Kirmizi); Ok(g, s, 0.23, 0.77, -1, 1, Kirmizi);
        }

        private static void Kaydir(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawLine(k, P(s, 0.12, 0.5), P(s, 0.88, 0.5)); g.DrawLine(k, P(s, 0.5, 0.12), P(s, 0.5, 0.88)); }
            Ok(g, s, 0.92, 0.5, 1, 0); Ok(g, s, 0.08, 0.5, -1, 0); Ok(g, s, 0.5, 0.08, 0, -1); Ok(g, s, 0.5, 0.92, 0, 1);
        }

        private static void Kopyala(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawRectangle(k, s * 0.1f, s * 0.38f, s * 0.4f, s * 0.4f);
            using (var k = K(s, Kirmizi, 1f, DashStyle.Dash)) g.DrawRectangle(k, s * 0.48f, s * 0.15f, s * 0.4f, s * 0.4f);
            Ok(g, s, 0.62, 0.42, 1, -1, Kirmizi);
        }

        private static void Dondur(Graphics g, float s)
        {
            using (var k = K(s, null, 1.3f)) g.DrawArc(k, s * 0.17f, s * 0.17f, s * 0.66f, s * 0.66f, -60, 290);
            Ok(g, s, 0.66, 0.22, 0.6, 0.8);
            Kutu(g, s, 0.5, 0.5);
        }

        private static void Olcekle(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawRectangle(k, s * 0.1f, s * 0.1f, s * 0.8f, s * 0.8f);
            using (var f = new SolidBrush(Color.FromArgb(70, Kirmizi))) g.FillRectangle(f, s * 0.1f, s * 0.55f, s * 0.35f, s * 0.35f);
            using (var k = K(s, Kirmizi)) g.DrawLine(k, P(s, 0.35, 0.65), P(s, 0.75, 0.25));
            Ok(g, s, 0.78, 0.22, 1, -1, Kirmizi);
        }

        private static void Kirp(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawLine(k, P(s, 0.5, 0.1), P(s, 0.5, 0.9)); g.DrawLine(k, P(s, 0.1, 0.5), P(s, 0.5, 0.5)); }
            using (var k = K(s, Kirmizi, 1f, DashStyle.Dash)) g.DrawLine(k, P(s, 0.5, 0.5), P(s, 0.92, 0.5));
            using (var k = K(s, Kirmizi)) { g.DrawLine(k, P(s, 0.66, 0.36), P(s, 0.82, 0.64)); g.DrawLine(k, P(s, 0.82, 0.36), P(s, 0.66, 0.64)); }
        }

        private static void Uzat(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawLine(k, P(s, 0.82, 0.1), P(s, 0.82, 0.9)); g.DrawLine(k, P(s, 0.1, 0.5), P(s, 0.42, 0.5)); }
            using (var k = K(s, Kirmizi, 1f, DashStyle.Dash)) g.DrawLine(k, P(s, 0.42, 0.5), P(s, 0.76, 0.5));
            Ok(g, s, 0.8, 0.5, 1, 0, Kirmizi);
        }

        private static void Paralel(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawBezier(k, P(s, 0.1, 0.65), P(s, 0.35, 0.35), P(s, 0.6, 0.9), P(s, 0.9, 0.5));
            using (var k = K(s, Kirmizi)) g.DrawBezier(k, P(s, 0.1, 0.4), P(s, 0.35, 0.1), P(s, 0.6, 0.65), P(s, 0.9, 0.25));
        }

        private static void Aynala(Graphics g, float s)
        {
            using (var k = K(s, Yesil, 1f, DashStyle.DashDot)) g.DrawLine(k, P(s, 0.5, 0.05), P(s, 0.5, 0.95));
            using (var f = new SolidBrush(Koyu)) g.FillPolygon(f, new[] { P(s, 0.42, 0.25), P(s, 0.42, 0.8), P(s, 0.1, 0.8) });
            using (var k = K(s, Kirmizi, 1f, DashStyle.Dash)) g.DrawPolygon(k, new[] { P(s, 0.58, 0.25), P(s, 0.58, 0.8), P(s, 0.9, 0.8) });
        }

        private static void Patlat(Graphics g, float s)
        {
            using (var k = K(s))
            {
                g.DrawLine(k, P(s, 0.15, 0.3), P(s, 0.42, 0.3)); g.DrawLine(k, P(s, 0.58, 0.3), P(s, 0.85, 0.3));
                g.DrawLine(k, P(s, 0.15, 0.7), P(s, 0.42, 0.7)); g.DrawLine(k, P(s, 0.58, 0.7), P(s, 0.85, 0.7));
            }
            Ok(g, s, 0.1, 0.1, -1, -1, Kirmizi); Ok(g, s, 0.9, 0.1, 1, -1, Kirmizi); Ok(g, s, 0.1, 0.9, -1, 1, Kirmizi); Ok(g, s, 0.9, 0.9, 1, 1, Kirmizi);
        }

        private static void Hizala(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawLine(k, P(s, 0.1, 0.85), P(s, 0.45, 0.5));
            using (var k = K(s, Kirmizi)) g.DrawLine(k, P(s, 0.5, 0.25), P(s, 0.9, 0.25));
            using (var k = K(s, Turuncu, 1f, DashStyle.Dash)) { g.DrawLine(k, P(s, 0.1, 0.85), P(s, 0.5, 0.25)); g.DrawLine(k, P(s, 0.45, 0.5), P(s, 0.9, 0.25)); }
            Kutu(g, s, 0.1, 0.85); Kutu(g, s, 0.45, 0.5); Kutu(g, s, 0.5, 0.25, Yesil); Kutu(g, s, 0.9, 0.25, Yesil);
        }

        private static void TopluDegistir(Graphics g, float s)
        {
            using (var k = K(s)) for (var i = 0; i < 4; i++) g.DrawLine(k, P(s, 0.1, 0.2 + i * 0.2), P(s, 0.6, 0.2 + i * 0.2));
            using (var k = K(s, Turuncu, 2.2f)) g.DrawLine(k, P(s, 0.55, 0.85), P(s, 0.9, 0.35));
        }

        private static void Tablo_(Graphics g, float s)
        {
            using (var k = K(s))
            {
                g.DrawRectangle(k, s * 0.1f, s * 0.15f, s * 0.8f, s * 0.7f);
                for (var i = 1; i < 4; i++) g.DrawLine(k, P(s, 0.1, 0.15 + i * 0.175), P(s, 0.9, 0.15 + i * 0.175));
                g.DrawLine(k, P(s, 0.4, 0.15), P(s, 0.4, 0.85));
            }
            using (var f = new SolidBrush(Color.FromArgb(90, Kirmizi))) g.FillRectangle(f, s * 0.1f, s * 0.15f, s * 0.8f, s * 0.175f);
        }

        private static void Secim(Graphics g, float s, Color renk)
        {
            using (var k = K(s, renk, 1f, DashStyle.Dash)) g.DrawRectangle(k, s * 0.12f, s * 0.12f, s * 0.76f, s * 0.76f);
        }

        private static void TumunuSec(Graphics g, float s)
        {
            Secim(g, s, Koyu);
            using (var f = new SolidBrush(Color.FromArgb(80, 30, 120, 220))) g.FillRectangle(f, s * 0.12f, s * 0.12f, s * 0.76f, s * 0.76f);
        }

        private static void SecimiKaldir(Graphics g, float s)
        {
            Secim(g, s, Koyu);
            using (var k = K(s, Kirmizi, 1.4f)) { g.DrawLine(k, P(s, 0.3, 0.3), P(s, 0.7, 0.7)); g.DrawLine(k, P(s, 0.7, 0.3), P(s, 0.3, 0.7)); }
        }

        private static void TersSecim(Graphics g, float s)
        {
            Secim(g, s, Koyu);
            using (var f = new SolidBrush(Color.FromArgb(110, 30, 120, 220))) g.FillRectangle(f, s * 0.12f, s * 0.12f, s * 0.38f, s * 0.76f);
        }

        private static void Benzer(Graphics g, float s)
        {
            using (var k = K(s)) { g.DrawEllipse(k, s * 0.08f, s * 0.3f, s * 0.38f, s * 0.38f); g.DrawEllipse(k, s * 0.54f, s * 0.3f, s * 0.38f, s * 0.38f); }
            using (var f = new SolidBrush(Color.FromArgb(90, 30, 120, 220))) { g.FillEllipse(f, s * 0.08f, s * 0.3f, s * 0.38f, s * 0.38f); g.FillEllipse(f, s * 0.54f, s * 0.3f, s * 0.38f, s * 0.38f); }
        }

        private static void OncekiSecim(Graphics g, float s)
        {
            Secim(g, s, Koyu);
            using (var k = K(s, Kirmizi, 1.3f)) g.DrawArc(k, s * 0.3f, s * 0.3f, s * 0.4f, s * 0.4f, 180, 270);
            Ok(g, s, 0.3, 0.5, 0, 1, Kirmizi);
        }

        private static void Cit(Graphics g, float s)
        {
            using (var k = K(s, Kirmizi)) g.DrawLines(k, new[] { P(s, 0.08, 0.7), P(s, 0.3, 0.3), P(s, 0.52, 0.7), P(s, 0.74, 0.3), P(s, 0.92, 0.6) });
            using (var k = K(s)) { g.DrawLine(k, P(s, 0.15, 0.15), P(s, 0.15, 0.85)); g.DrawLine(k, P(s, 0.62, 0.15), P(s, 0.62, 0.85)); }
        }

        private static void Pencereler(Graphics g, float s)
        {
            using (var k = K(s))
            {
                g.DrawRectangle(k, s * 0.1f, s * 0.15f, s * 0.8f, s * 0.7f);
                g.DrawLine(k, P(s, 0.5, 0.15), P(s, 0.5, 0.85));
                g.DrawLine(k, P(s, 0.5, 0.5), P(s, 0.9, 0.5));
            }
            using (var f = new SolidBrush(Color.FromArgb(70, 30, 120, 220))) g.FillRectangle(f, s * 0.1f, s * 0.15f, s * 0.4f, s * 0.7f);
        }

        private static void Guzergah(Graphics g, float s)
        {
            using (var k = K(s, Color.FromArgb(120, 120, 120), 3.2f)) g.DrawBezier(k, P(s, 0.05, 0.85), P(s, 0.45, 0.85), P(s, 0.5, 0.15), P(s, 0.95, 0.15));
            using (var k = K(s, Color.White, 1f, DashStyle.Dash)) g.DrawBezier(k, P(s, 0.05, 0.85), P(s, 0.45, 0.85), P(s, 0.5, 0.15), P(s, 0.95, 0.15));
            Kutu(g, s, 0.05, 0.85); Kutu(g, s, 0.95, 0.15);
        }

        private static void GuzergahEditoru(Graphics g, float s)
        {
            using (var k = K(s)) g.DrawBezier(k, P(s, 0.05, 0.85), P(s, 0.4, 0.85), P(s, 0.45, 0.25), P(s, 0.8, 0.25));
            using (var k = K(s, Kirmizi, 1f, DashStyle.Dash)) { g.DrawLine(k, P(s, 0.05, 0.85), P(s, 0.45, 0.85)); g.DrawLine(k, P(s, 0.45, 0.85), P(s, 0.8, 0.25)); }
            using (var f = new SolidBrush(Kirmizi)) g.FillPolygon(f, new[] { P(s, 0.45, 0.78), P(s, 0.39, 0.9), P(s, 0.51, 0.9) });
        }

        private static void Kesit(Graphics g, float s)
        {
            using (var k = K(s, Color.FromArgb(140, 90, 40))) g.DrawLines(k, new[] { P(s, 0.05, 0.4), P(s, 0.25, 0.55), P(s, 0.45, 0.5), P(s, 0.7, 0.65), P(s, 0.95, 0.45) });
            using (var k = K(s, null, 1.4f)) g.DrawLines(k, new[] { P(s, 0.12, 0.8), P(s, 0.3, 0.6), P(s, 0.7, 0.6), P(s, 0.88, 0.8) });
            using (var k = K(s, Kirmizi, 1f, DashStyle.DashDot)) g.DrawLine(k, P(s, 0.5, 0.1), P(s, 0.5, 0.95));
        }

        private static void Poligon(Graphics g, float s)
        {
            var p = new[] { P(s, 0.1, 0.75), P(s, 0.35, 0.35), P(s, 0.62, 0.62), P(s, 0.9, 0.25) };
            using (var k = K(s)) g.DrawLines(k, p);
            using (var f = new SolidBrush(Kirmizi))
                foreach (var q in p)
                    g.FillPolygon(f, new[] { new PointF(q.X, q.Y - s * 0.09f), new PointF(q.X - s * 0.08f, q.Y + s * 0.06f), new PointF(q.X + s * 0.08f, q.Y + s * 0.06f) });
        }

        private static void Yuzey(Graphics g, float s)
        {
            var p = new[] { P(s, 0.1, 0.8), P(s, 0.45, 0.85), P(s, 0.9, 0.7), P(s, 0.25, 0.4), P(s, 0.65, 0.35), P(s, 0.5, 0.1) };
            using (var f = new SolidBrush(Color.FromArgb(60, 40, 140, 70))) g.FillPolygon(f, new[] { p[0], p[1], p[2], p[4], p[5], p[3] });
            using (var k = K(s, Yesil))
            {
                foreach (var t in new[] { new[] { 0, 1, 3 }, new[] { 1, 4, 3 }, new[] { 1, 2, 4 }, new[] { 3, 4, 5 } })
                    g.DrawPolygon(k, new[] { p[t[0]], p[t[1]], p[t[2]] });
            }
        }
    }
}
