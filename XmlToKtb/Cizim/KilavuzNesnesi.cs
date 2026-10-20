using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Kılavuz çizgi (AutoCAD LEADER / MLEADER): ok ucundan başlayıp köşelerden geçen çizgi, sonunda yatay iniş çizgisi
    /// ve çok satırlı açıklama yazısı. Ok tipi, ok boyu, yazı yüksekliği ve boşluk ölçü stilinden alınır.
    /// </summary>
    public class KilavuzNesnesi : CizimNesnesi, IOlcuStilli
    {
        [DisplayName("Tip"), ReadOnly(true)]
        public override string Tip => "Kılavuz Çizgi";

        /// <summary>Köşeler ({x, y}): ilki okun ucu, sonuncusu iniş çizgisinin başladığı nokta.</summary>
        [Browsable(false)]
        public List<double[]> Koseler { get; } = new List<double[]>();

        [DisplayName("Yazı"), Description("Açıklama yazısı; birden çok satır olabilir."),
         Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", typeof(UITypeEditor))]
        public string Metin { get; set; } = "";

        [DisplayName("İniş Çizgisi"), Description("Son köşeden yazıya doğru yatay çizgi (AutoCAD landing).")]
        public bool Inis { get; set; } = true;

        [DisplayName("Köşe Sayısı"), ReadOnly(true)]
        public int KoseSayisi => Koseler.Count;

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

        public KilavuzNesnesi Kopyala()
        {
            var kopya = new KilavuzNesnesi { Metin = Metin, Inis = Inis, Stil = Stil };
            kopya.Koseler.AddRange(Koseler.Select(k => new[] { k[0], k[1] }));
            return kopya;
        }

        /// <summary>Çizim parçaları: çizgiler, ok ({uç x, uç y, yön x, yön y}) ve satır satır yazılar.</summary>
        public sealed class Geometri
        {
            public readonly List<double[]> Cizgiler = new List<double[]>();
            public double[] Ok;
            public readonly List<YaziNesnesi> Yazilar = new List<YaziNesnesi>();
        }

        /// <summary>İniş çizgisinin uzunluğu (çizim birimi): iki ok boyu, en az bir yazı yüksekliği.</summary>
        [Browsable(false)]
        public double InisBoyu => Math.Max(GecerliStil.Boy(GecerliStil.OkBoyu) * 2, GecerliStil.Boy(GecerliStil.YaziYuksekligi));

        /// <summary>İniş çizgisinin yönü: son parça sağa gidiyorsa +1, sola gidiyorsa -1.</summary>
        [Browsable(false)]
        public int InisYonu => Koseler.Count < 2 || Koseler[Koseler.Count - 1][0] >= Koseler[Koseler.Count - 2][0] ? 1 : -1;

        public Geometri GeometriOlustur()
        {
            var g = new Geometri();
            if (Koseler.Count < 2)
                return g;
            var stil = GecerliStil;
            for (var i = 0; i + 1 < Koseler.Count; i++)
                g.Cizgiler.Add(new[] { Koseler[i][0], Koseler[i][1], Koseler[i + 1][0], Koseler[i + 1][1] });
            double dx = Koseler[0][0] - Koseler[1][0], dy = Koseler[0][1] - Koseler[1][1], l = Math.Sqrt(dx * dx + dy * dy);
            if (l > 1e-12)
                g.Ok = new[] { Koseler[0][0], Koseler[0][1], dx / l, dy / l };

            // İniş çizgisi son parçanın yatay yönünde; yazı onun ucunda, satırların ortası iniş hizasında.
            var son = Koseler[Koseler.Count - 1];
            var once = Koseler[Koseler.Count - 2];
            var yon = son[0] >= once[0] ? 1 : -1;
            var h = stil.Boy(stil.YaziYuksekligi);
            var bosluk = stil.Boy(stil.YaziBoslugu);
            double x = son[0], y = son[1];
            if (Inis)
            {
                var inis = InisBoyu;
                g.Cizgiler.Add(new[] { x, y, x + yon * inis, y });
                x += yon * inis;
            }
            var satirlar = (Metin ?? "").Replace("\r\n", "\n").Split('\n');
            if (satirlar.Length == 1 && satirlar[0].Length == 0)
                return g;
            var satirAraligi = h * 1.5;
            var ustSatirY = y + (satirlar.Length - 1) * satirAraligi / 2;
            for (var i = 0; i < satirlar.Length; i++)
            {
                if (satirlar[i].Length == 0)
                    continue;
                g.Yazilar.Add(new YaziNesnesi
                {
                    Metin = satirlar[i], KonumX = x + yon * bosluk, KonumY = ustSatirY - i * satirAraligi, Yukseklik = h,
                    HizaYatay = yon > 0 ? YatayHizalama.Sol : YatayHizalama.Sag, HizaDikey = DikeyHizalama.Orta
                });
            }
            return g;
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
                if (g.Ok != null)
                    OkCiz(grafik, gorunum, g.Ok, stil, kalem, firca, E);
            }
            foreach (var yazi in g.Yazilar)
            {
                yazi.OzelRenk = renk;
                yazi.Ciz(grafik, gorunum, panelGenislik, panelYukseklik);
            }
            if (Secili)
            {
                using (var firca = new SolidBrush(Color.DodgerBlue))
                    foreach (var k in Koseler)
                    {
                        var p = E(k[0], k[1]);
                        grafik.FillRectangle(firca, p.X - 3, p.Y - 3, 6, 6);
                    }
            }
        }

        private static void OkCiz(Graphics grafik, CizimGorunum gorunum, double[] o, OlcuStili stil, Pen kalem, Brush firca, Func<double, double, PointF> E)
        {
            var boy = stil.Boy(stil.OkBoyu);
            double x = o[0], y = o[1], dx = o[2], dy = o[3], nx = -dy, ny = dx;
            switch (stil.OkTipi)
            {
                case OlcuOkTipi.Nokta:
                {
                    var p = E(x, y);
                    var r = Math.Max(1.5f, (float)(boy / 5 * gorunum.Olcek));
                    grafik.FillEllipse(firca, p.X - r, p.Y - r, 2 * r, 2 * r);
                    break;
                }
                case OlcuOkTipi.EgikCizgi:
                {
                    double ex = (dx + nx) / Math.Sqrt(2), ey = (dy + ny) / Math.Sqrt(2);
                    grafik.DrawLine(kalem, E(x - ex * boy / 2, y - ey * boy / 2), E(x + ex * boy / 2, y + ey * boy / 2));
                    break;
                }
                case OlcuOkTipi.Yok:
                    break;
                default:
                {
                    var yarim = boy / 6;
                    var ucgen = new[] { E(x, y), E(x - dx * boy + nx * yarim, y - dy * boy + ny * yarim), E(x - dx * boy - nx * yarim, y - dy * boy - ny * yarim) };
                    if (stil.OkTipi == OlcuOkTipi.KapaliDolu)
                        grafik.FillPolygon(firca, ucgen);
                    else
                    {
                        grafik.DrawLine(kalem, ucgen[0], ucgen[1]);
                        grafik.DrawLine(kalem, ucgen[0], ucgen[2]);
                    }
                    break;
                }
            }
        }

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
            return g.Yazilar.Any(y => y.IsabetEttiMi(dunyaNoktasi, toleransDunya));
        }

        public override IEnumerable<PointF> UcNoktalariAl() => Koseler.Select(k => new PointF((float)k[0], (float)k[1]));

        /// <summary>Patlat: çizgilere, oka (kapalı ok çokgen) ve yazılara ayırır.</summary>
        public List<CizimNesnesi> Parcala()
        {
            var g = GeometriOlustur();
            var stil = GecerliStil;
            var sonuc = new List<CizimNesnesi>();
            foreach (var c in g.Cizgiler)
                sonuc.Add(new CizgiNesnesi { BaslangicX = c[0], BaslangicY = c[1], BitisX = c[2], BitisY = c[3] });
            if (g.Ok != null && (stil.OkTipi == OlcuOkTipi.KapaliDolu || stil.OkTipi == OlcuOkTipi.Acik))
            {
                double boy = stil.Boy(stil.OkBoyu), x = g.Ok[0], y = g.Ok[1], dx = g.Ok[2], dy = g.Ok[3], yarim = boy / 6;
                var ok = new CokluDogruNesnesi { Kapali = stil.OkTipi == OlcuOkTipi.KapaliDolu };
                ok.Koseler.Add(new CokluDogruKosesi { X = x - dx * boy - dy * yarim, Y = y - dy * boy + dx * yarim });
                ok.Koseler.Add(new CokluDogruKosesi { X = x, Y = y });
                ok.Koseler.Add(new CokluDogruKosesi { X = x - dx * boy + dy * yarim, Y = y - dy * boy - dx * yarim });
                sonuc.Add(ok);
            }
            sonuc.AddRange(g.Yazilar);
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
