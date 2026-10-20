using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace XmlToKtb.Cizim
{
    public enum OlcuTuru
    {
        /// <summary>Verilen doğrultudaki (Açı) izdüşüm uzunluğu: yatay (0°) ya da düşey (90°) ölçü.</summary>
        Dogrusal,
        /// <summary>İki nokta arasındaki gerçek uzunluk; ölçü çizgisi iki noktaya paraleldir.</summary>
        Hizali,
        /// <summary>A köşesinde, B ve C kolları arasındaki açı; yay D noktasından geçer.</summary>
        Acisal,
        /// <summary>A merkezli, B noktasından geçen dairenin yarıçapı.</summary>
        Yaricap,
        /// <summary>A merkezli, B noktasından geçen dairenin çapı.</summary>
        Cap
    }

    /// <summary>
    /// Ölçülendirme objesi (AutoCAD DIMENSION). Tanım noktalarıyla saklanır; çizgiler, oklar ve yazı her çizimde ölçü
    /// stilinden yeniden üretilir.
    /// <list type="bullet">
    /// <item>Doğrusal/Hizalı: A ve B ölçülen noktalar, C ölçü çizgisinin geçtiği nokta.</item>
    /// <item>Açısal: A köşe, B ve C kol noktaları, D yayın geçtiği nokta (hangi açının ölçüldüğünü de belirler).</item>
    /// <item>Yarıçap/Çap: A merkez, B daire üzerinde ölçü çizgisinin değdiği nokta.</item>
    /// </list>
    /// </summary>
    public class OlcuNesnesi : CizimNesnesi, IOlcuStilli
    {
        [DisplayName("Tip"), ReadOnly(true)]
        public override string Tip => "Ölçü";

        [DisplayName("Ölçü Türü"), ReadOnly(true)]
        public OlcuTuru Tur { get; set; }

        [Browsable(false)] public double AX { get; set; }
        [Browsable(false)] public double AY { get; set; }
        [Browsable(false)] public double BX { get; set; }
        [Browsable(false)] public double BY { get; set; }
        [Browsable(false)] public double CX { get; set; }
        [Browsable(false)] public double CY { get; set; }
        [Browsable(false)] public double DX { get; set; }
        [Browsable(false)] public double DY { get; set; }

        /// <summary>Doğrusal ölçünün doğrultusu (derece, saat yönünün tersine; 0 yatay, 90 düşey).</summary>
        [DisplayName("Ölçü Doğrultusu (derece)"), Description("Yalnızca doğrusal ölçüde: 0 yatay, 90 düşey.")]
        public double Aci { get; set; }

        /// <summary>Boşsa ölçülen değer yazılır; "&lt;&gt;" ölçülen değerin yerine geçer.</summary>
        [DisplayName("Yazı"), Description("Boşsa ölçülen değer yazılır. <> ölçülen değerin yerine geçer (ör. \"L = <> m\").")]
        public string Metin { get; set; } = "";

        [DisplayName("Ölçülen Değer"), ReadOnly(true)]
        public string OlculenMetin => DegerMetni();

        [Browsable(false)]
        public OlcuStili Stil { get; set; }

        [DisplayName("Ölçü Stili"), TypeConverter(typeof(OlcuStiliAdiConverter))]
        public string StilAdi
        {
            get => Stil?.Ad ?? "";
            set
            {
                var yeni = Stil?.Liste?.Find(s => s.Ad == value);
                if (yeni != null)
                    Stil = yeni;
            }
        }

        private OlcuStili GecerliStil => Stil ?? OlcuStili.Varsayilan;

        /// <summary>Ölçülen büyüklük: uzunluk (m) ya da açı (radyan).</summary>
        [Browsable(false)]
        public double Olcu
        {
            get
            {
                switch (Tur)
                {
                    case OlcuTuru.Dogrusal:
                    {
                        var r = Aci * Math.PI / 180;
                        return Math.Abs((BX - AX) * Math.Cos(r) + (BY - AY) * Math.Sin(r));
                    }
                    case OlcuTuru.Hizali:
                    case OlcuTuru.Yaricap:
                        return Uzaklik(AX, AY, BX, BY);
                    case OlcuTuru.Cap:
                        return 2 * Uzaklik(AX, AY, BX, BY);
                    default:
                        AciDilimi(out _, out var tarama, out _);
                        return tarama;
                }
            }
        }

        public string DegerMetni()
        {
            var stil = GecerliStil;
            switch (Tur)
            {
                case OlcuTuru.Acisal: return stil.AciMetni(Olcu);
                case OlcuTuru.Yaricap: return "R" + stil.UzunlukMetni(Olcu);
                case OlcuTuru.Cap: return "Ø" + stil.UzunlukMetni(Olcu);
                default: return stil.UzunlukMetni(Olcu);
            }
        }

        /// <summary>Ekrana yazılan metin: kullanıcı yazısı (içindeki &lt;&gt; ölçülen değerle) ya da ölçülen değer.</summary>
        public string YaziMetni()
        {
            if (string.IsNullOrEmpty(Metin))
                return DegerMetni();
            return Metin.Contains("<>") ? Metin.Replace("<>", DegerMetni()) : Metin;
        }

        public OlcuNesnesi Kopyala() => new OlcuNesnesi
        {
            Tur = Tur, AX = AX, AY = AY, BX = BX, BY = BY, CX = CX, CY = CY, DX = DX, DY = DY, Aci = Aci, Metin = Metin, Stil = Stil
        };

        private static double Uzaklik(double ax, double ay, double bx, double by) => Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));

        private static double Mod2Pi(double a)
        {
            a %= 2 * Math.PI;
            return a < 0 ? a + 2 * Math.PI : a;
        }

        // Açısal ölçünün dilimi: D noktasını içeren taraf (başlangıç açısı, saat yönünün tersine tarama, yarıçap).
        private void AciDilimi(out double baslangic, out double tarama, out double yaricap)
        {
            double a1 = Math.Atan2(BY - AY, BX - AX), a2 = Math.Atan2(CY - AY, CX - AX), ad = Math.Atan2(DY - AY, DX - AX);
            var s = Mod2Pi(a2 - a1);
            if (Mod2Pi(ad - a1) <= s)
            {
                baslangic = a1;
                tarama = s;
            }
            else
            {
                baslangic = a2;
                tarama = 2 * Math.PI - s;
            }
            yaricap = Uzaklik(AX, AY, DX, DY);
        }

        /// <summary>Açısal ölçünün DXF'e yazılacak kolları: dilim B'den başlıyorsa (B, C), C'den başlıyorsa (C, B).</summary>
        public bool AciB_denBasliyor()
        {
            double a1 = Math.Atan2(BY - AY, BX - AX), a2 = Math.Atan2(CY - AY, CX - AX), ad = Math.Atan2(DY - AY, DX - AX);
            return Mod2Pi(ad - a1) <= Mod2Pi(a2 - a1);
        }

        // ------------------------------------------------------------------ geometri

        /// <summary>Ölçünün dünya koordinatlarındaki çizim parçaları.</summary>
        public sealed class Geometri
        {
            public readonly List<double[]> Cizgiler = new List<double[]>();
            /// <summary>Ok: {uç x, uç y, yön x, yön y} (yön, okun gösterdiği taraf).</summary>
            public readonly List<double[]> Oklar = new List<double[]>();
            public double YaziX, YaziY, YaziAci, YaziYukseklik;
            public string Yazi;
            public DikeyHizalama YaziDikey = DikeyHizalama.Alt;
        }

        // Yazı ters okunmasın: açı (-90, 90] aralığına getirilir.
        private static double OkunurAci(double derece)
        {
            derece %= 360;
            if (derece < 0) derece += 360;
            if (derece > 90 && derece <= 270) derece -= 180;
            if (derece > 270) derece -= 360;
            return derece;
        }

        public Geometri GeometriOlustur()
        {
            var g = new Geometri();
            var stil = GecerliStil;
            double ok = stil.Boy(stil.OkBoyu), bosluk = stil.Boy(stil.YaziBoslugu), exo = stil.Boy(stil.UzatmaAraligi), exe = stil.Boy(stil.UzatmaTasmasi);
            double dle = stil.OkTipi == OlcuOkTipi.EgikCizgi ? stil.Boy(stil.OlcuCizgisiTasmasi) : 0;
            g.YaziYukseklik = stil.Boy(stil.YaziYuksekligi);
            g.Yazi = YaziMetni();
            var yaziGenisligi = g.Yazi.Length * g.YaziYukseklik * 0.62;

            switch (Tur)
            {
                case OlcuTuru.Dogrusal:
                case OlcuTuru.Hizali:
                {
                    double ux, uy;
                    if (Tur == OlcuTuru.Dogrusal)
                    {
                        ux = Math.Cos(Aci * Math.PI / 180);
                        uy = Math.Sin(Aci * Math.PI / 180);
                    }
                    else
                    {
                        var l = Uzaklik(AX, AY, BX, BY);
                        ux = l > 1e-12 ? (BX - AX) / l : 1;
                        uy = l > 1e-12 ? (BY - AY) / l : 0;
                    }
                    double nx = -uy, ny = ux;
                    var o1 = (CX - AX) * nx + (CY - AY) * ny;
                    var o2 = (CX - BX) * nx + (CY - BY) * ny;
                    double f1x = AX + nx * o1, f1y = AY + ny * o1, f2x = BX + nx * o2, f2y = BY + ny * o2;

                    UzatmaCizgisi(g, AX, AY, f1x, f1y, exo, exe);
                    UzatmaCizgisi(g, BX, BY, f2x, f2y, exo, exe);

                    // Ölçü çizgisi f1 → f2 yönünde; yön, ölçü doğrultusuyla aynı ya da ters olabilir.
                    var boy = Uzaklik(f1x, f1y, f2x, f2y);
                    double dx = boy > 1e-12 ? (f2x - f1x) / boy : ux, dy = boy > 1e-12 ? (f2y - f1y) / boy : uy;
                    var disarida = stil.OkTipi != OlcuOkTipi.EgikCizgi && stil.OkTipi != OlcuOkTipi.Yok && boy < 2.2 * ok;
                    double mx = (f1x + f2x) / 2, my = (f1y + f2y) / 2;
                    var yaziAci = OkunurAci(Math.Atan2(dy, dx) * 180 / Math.PI);
                    double yukariX = -Math.Sin(yaziAci * Math.PI / 180), yukariY = Math.Cos(yaziAci * Math.PI / 180);

                    if (stil.YaziKonumu == OlcuYaziKonumu.Ortada && !disarida && boy > yaziGenisligi + 2 * bosluk)
                    {
                        // Ölçü çizgisi yazının olduğu yerde kesilir.
                        var yarim = yaziGenisligi / 2 + bosluk;
                        g.Cizgiler.Add(new[] { f1x - dx * dle, f1y - dy * dle, mx - dx * yarim, my - dy * yarim });
                        g.Cizgiler.Add(new[] { mx + dx * yarim, my + dy * yarim, f2x + dx * dle, f2y + dy * dle });
                        g.YaziX = mx;
                        g.YaziY = my;
                        g.YaziDikey = DikeyHizalama.Orta;
                    }
                    else
                    {
                        g.Cizgiler.Add(new[] { f1x - dx * dle, f1y - dy * dle, f2x + dx * dle, f2y + dy * dle });
                        g.YaziX = mx + yukariX * bosluk;
                        g.YaziY = my + yukariY * bosluk;
                    }
                    g.YaziAci = yaziAci;

                    if (disarida)
                    {
                        // Ölçü kısa: oklar dışarıda, içeri bakar; ölçü çizgisi dışarı uzatılır.
                        g.Cizgiler.Add(new[] { f1x, f1y, f1x - dx * ok * 2, f1y - dy * ok * 2 });
                        g.Cizgiler.Add(new[] { f2x, f2y, f2x + dx * ok * 2, f2y + dy * ok * 2 });
                        g.Oklar.Add(new[] { f1x, f1y, dx, dy });
                        g.Oklar.Add(new[] { f2x, f2y, -dx, -dy });
                    }
                    else
                    {
                        g.Oklar.Add(new[] { f1x, f1y, -dx, -dy });
                        g.Oklar.Add(new[] { f2x, f2y, dx, dy });
                    }
                    break;
                }

                case OlcuTuru.Acisal:
                {
                    AciDilimi(out var bas, out var tarama, out var r);
                    if (r < 1e-9)
                        break;
                    var adim = Math.Max(8, (int)Math.Ceiling(tarama * 180 / Math.PI / 3));
                    double px = AX + r * Math.Cos(bas), py = AY + r * Math.Sin(bas);
                    for (var i = 1; i <= adim; i++)
                    {
                        var a = bas + tarama * i / adim;
                        double qx = AX + r * Math.Cos(a), qy = AY + r * Math.Sin(a);
                        g.Cizgiler.Add(new[] { px, py, qx, qy });
                        px = qx;
                        py = qy;
                    }
                    var son = bas + tarama;
                    // Yay kol noktasının dışındaysa kol doğrultusunda uzatma çizgisi. Dilim B'den başlıyorsa
                    // başlangıç kolu B'ye, bitiş kolu C'ye aittir; değilse tersi.
                    var bdenBasliyor = AciB_denBasliyor();
                    double bBoyu = Uzaklik(AX, AY, BX, BY), cBoyu = Uzaklik(AX, AY, CX, CY);
                    foreach (var kol in new[] { new[] { bas, bdenBasliyor ? bBoyu : cBoyu }, new[] { son, bdenBasliyor ? cBoyu : bBoyu } })
                    {
                        double a = kol[0], kolBoyu = kol[1];
                        if (r > kolBoyu + exo)
                            g.Cizgiler.Add(new[] { AX + (kolBoyu + exo) * Math.Cos(a), AY + (kolBoyu + exo) * Math.Sin(a), AX + (r + exe) * Math.Cos(a), AY + (r + exe) * Math.Sin(a) });
                    }
                    g.Oklar.Add(new[] { AX + r * Math.Cos(bas), AY + r * Math.Sin(bas), Math.Sin(bas), -Math.Cos(bas) });
                    g.Oklar.Add(new[] { AX + r * Math.Cos(son), AY + r * Math.Sin(son), -Math.Sin(son), Math.Cos(son) });
                    var orta = bas + tarama / 2;
                    g.YaziAci = OkunurAci(orta * 180 / Math.PI - 90);
                    // Yazı yayın dışına: okunur açıdaki "yukarı" dışarıya bakmıyorsa yazı yayın içine kaymasın diye dikey hiza değişir.
                    var disaYukari = Math.Cos(orta - (g.YaziAci + 90) * Math.PI / 180) > 0;
                    g.YaziX = AX + (r + bosluk) * Math.Cos(orta);
                    g.YaziY = AY + (r + bosluk) * Math.Sin(orta);
                    g.YaziDikey = disaYukari ? DikeyHizalama.Alt : DikeyHizalama.Ust;
                    break;
                }

                case OlcuTuru.Yaricap:
                case OlcuTuru.Cap:
                {
                    var r = Uzaklik(AX, AY, BX, BY);
                    if (r < 1e-9)
                        break;
                    double ux = (BX - AX) / r, uy = (BY - AY) / r;
                    var yaziAci = OkunurAci(Math.Atan2(uy, ux) * 180 / Math.PI);
                    double yukariX = -Math.Sin(yaziAci * Math.PI / 180), yukariY = Math.Cos(yaziAci * Math.PI / 180);
                    g.YaziAci = yaziAci;
                    if (Tur == OlcuTuru.Yaricap)
                    {
                        // Merkezden daireye, oradan yazı boyunca dışarı.
                        var uzanti = yaziGenisligi + 2 * bosluk;
                        g.Cizgiler.Add(new[] { AX, AY, BX + ux * uzanti, BY + uy * uzanti });
                        g.Oklar.Add(new[] { BX, BY, ux, uy });
                        var yaziMerkezi = bosluk + yaziGenisligi / 2;
                        g.YaziX = BX + ux * yaziMerkezi + yukariX * bosluk;
                        g.YaziY = BY + uy * yaziMerkezi + yukariY * bosluk;
                    }
                    else
                    {
                        double kx = 2 * AX - BX, ky = 2 * AY - BY;
                        g.Cizgiler.Add(new[] { kx, ky, BX, BY });
                        g.Oklar.Add(new[] { BX, BY, ux, uy });
                        g.Oklar.Add(new[] { kx, ky, -ux, -uy });
                        g.YaziX = AX + yukariX * bosluk;
                        g.YaziY = AY + yukariY * bosluk;
                    }
                    break;
                }
            }
            return g;
        }

        // Ölçülen noktadan ölçü çizgisindeki ayağa: noktadan exo kadar sonra başlar, ayağı exe kadar geçer.
        private static void UzatmaCizgisi(Geometri g, double px, double py, double ax, double ay, double exo, double exe)
        {
            var l = Uzaklik(px, py, ax, ay);
            if (l < 1e-12)
                return;
            double dx = (ax - px) / l, dy = (ay - py) / l;
            if (l <= exo)
                return;
            g.Cizgiler.Add(new[] { px + dx * exo, py + dy * exo, ax + dx * exe, ay + dy * exe });
        }

        // Okun çokgeni (dünya koordinatı): uç, yön ve boy.
        private static List<double[]> OkCokgeni(double[] o, double boy, OlcuOkTipi tip)
        {
            double x = o[0], y = o[1], dx = o[2], dy = o[3], nx = -dy, ny = dx;
            switch (tip)
            {
                case OlcuOkTipi.KapaliDolu:
                case OlcuOkTipi.Acik:
                {
                    var yarim = boy / 6;
                    return new List<double[]>
                    {
                        new[] { x, y },
                        new[] { x - dx * boy + nx * yarim, y - dy * boy + ny * yarim },
                        new[] { x - dx * boy - nx * yarim, y - dy * boy - ny * yarim }
                    };
                }
                case OlcuOkTipi.EgikCizgi:
                {
                    // Ölçü doğrultusuna 45° eğik kısa çizgi.
                    var yarim = boy / 2;
                    double ex = (dx + nx) / Math.Sqrt(2), ey = (dy + ny) / Math.Sqrt(2);
                    return new List<double[]> { new[] { x - ex * yarim, y - ey * yarim }, new[] { x + ex * yarim, y + ey * yarim } };
                }
                default:
                    return null;
            }
        }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            var g = GeometriOlustur();
            var stil = GecerliStil;
            var renk = Secili ? Color.DodgerBlue : CizimRengi(Color.Black);
            PointF E(double x, double y) => gorunum.DunyadanEkrana(x, y, panelGenislik, panelYukseklik);
            using (var kalem = new Pen(renk, Secili ? 2f : KalemKalinligi(1f)))
            using (var firca = new SolidBrush(renk))
            {
                foreach (var c in g.Cizgiler)
                    grafik.DrawLine(kalem, E(c[0], c[1]), E(c[2], c[3]));
                var okBoyu = stil.Boy(stil.OkBoyu);
                foreach (var o in g.Oklar)
                {
                    if (stil.OkTipi == OlcuOkTipi.Nokta)
                    {
                        var p = E(o[0], o[1]);
                        var rp = Math.Max(1.5f, (float)(okBoyu / 5 * gorunum.Olcek));
                        grafik.FillEllipse(firca, p.X - rp, p.Y - rp, 2 * rp, 2 * rp);
                        continue;
                    }
                    var cokgen = OkCokgeni(o, okBoyu, stil.OkTipi);
                    if (cokgen == null)
                        continue;
                    var ekran = cokgen.Select(p => E(p[0], p[1])).ToArray();
                    if (stil.OkTipi == OlcuOkTipi.KapaliDolu)
                        grafik.FillPolygon(firca, ekran);
                    else if (stil.OkTipi == OlcuOkTipi.Acik)
                    {
                        grafik.DrawLine(kalem, ekran[0], ekran[1]);
                        grafik.DrawLine(kalem, ekran[0], ekran[2]);
                    }
                    else
                        grafik.DrawLine(kalem, ekran[0], ekran[1]);
                }
            }
            if (!string.IsNullOrEmpty(g.Yazi))
                YaziNesnesiOlustur(g, renk).Ciz(grafik, gorunum, panelGenislik, panelYukseklik);
        }

        private static YaziNesnesi YaziNesnesiOlustur(Geometri g, Color? renk) => new YaziNesnesi
        {
            Metin = g.Yazi, KonumX = g.YaziX, KonumY = g.YaziY, Yukseklik = g.YaziYukseklik, Donus = g.YaziAci,
            HizaYatay = YatayHizalama.Orta, HizaDikey = g.YaziDikey, OzelRenk = renk
        };

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            var g = GeometriOlustur();
            double px = dunyaNoktasi.X, py = dunyaNoktasi.Y;
            foreach (var c in g.Cizgiler)
            {
                double dx = c[2] - c[0], dy = c[3] - c[1], l2 = dx * dx + dy * dy;
                var t = l2 < 1e-18 ? 0 : Math.Max(0, Math.Min(1, ((px - c[0]) * dx + (py - c[1]) * dy) / l2));
                double qx = c[0] + t * dx, qy = c[1] + t * dy;
                if ((px - qx) * (px - qx) + (py - qy) * (py - qy) <= toleransDunya * toleransDunya)
                    return true;
            }
            return !string.IsNullOrEmpty(g.Yazi) && YaziNesnesiOlustur(g, null).IsabetEttiMi(dunyaNoktasi, toleransDunya);
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)AX, (float)AY);
            yield return new PointF((float)BX, (float)BY);
            if (Tur == OlcuTuru.Acisal)
                yield return new PointF((float)CX, (float)CY);
        }

        /// <summary>Patlat: ölçüyü çizgilere, ok çokgenlerine (ya da noktalarda dairelere) ve yazıya ayırır.</summary>
        public List<CizimNesnesi> Parcala()
        {
            var g = GeometriOlustur();
            var stil = GecerliStil;
            var sonuc = new List<CizimNesnesi>();
            foreach (var c in g.Cizgiler)
                sonuc.Add(new CizgiNesnesi { BaslangicX = c[0], BaslangicY = c[1], BitisX = c[2], BitisY = c[3] });
            var okBoyu = stil.Boy(stil.OkBoyu);
            foreach (var o in g.Oklar)
            {
                if (stil.OkTipi == OlcuOkTipi.Nokta)
                {
                    sonuc.Add(new DaireNesnesi { MerkezX = o[0], MerkezY = o[1], Yaricap = okBoyu / 5 });
                    continue;
                }
                var cokgen = OkCokgeni(o, okBoyu, stil.OkTipi);
                if (cokgen == null)
                    continue;
                if (cokgen.Count == 2)
                {
                    sonuc.Add(new CizgiNesnesi { BaslangicX = cokgen[0][0], BaslangicY = cokgen[0][1], BitisX = cokgen[1][0], BitisY = cokgen[1][1] });
                    continue;
                }
                var cd = new CokluDogruNesnesi { Kapali = stil.OkTipi == OlcuOkTipi.KapaliDolu };
                if (stil.OkTipi == OlcuOkTipi.Acik)
                    cokgen = new List<double[]> { cokgen[1], cokgen[0], cokgen[2] };
                foreach (var p in cokgen)
                    cd.Koseler.Add(new CokluDogruKosesi { X = p[0], Y = p[1] });
                sonuc.Add(cd);
            }
            if (!string.IsNullOrEmpty(g.Yazi))
                sonuc.Add(YaziNesnesiOlustur(g, null));
            foreach (var parca in sonuc)
            {
                parca.Tabaka = Tabaka;
                parca.OzelRenk = OzelRenk;
                parca.CizgiKalinligi = CizgiKalinligi;
            }
            return sonuc;
        }
    }
}
