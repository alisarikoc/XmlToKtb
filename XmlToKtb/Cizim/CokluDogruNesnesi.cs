using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace XmlToKtb.Cizim
{
    /// <summary>Çoklu doğrunun bir köşesi. Bulge, bu köşeden sonrakine giden parçanın eğriliğidir (0: düz).</summary>
    public class CokluDogruKosesi
    {
        public double X, Y, Z;
        /// <summary>Yay parçasında tan(merkez açı / 4); artı saat yönünün tersine, eksi saat yönünde döner.</summary>
        public double Bulge;

        public CokluDogruKosesi Kopyala() => new CokluDogruKosesi { X = X, Y = Y, Z = Z, Bulge = Bulge };
    }

    /// <summary>
    /// Uç uca bağlı doğru ve yay parçalarından oluşan tek obje (AutoCAD'deki polyline). Köşelerin kendi kotu olabilir
    /// (3B çoklu doğru).
    /// </summary>
    public class CokluDogruNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Çoklu Doğru";

        [Browsable(false)]
        public List<CokluDogruKosesi> Koseler { get; } = new List<CokluDogruKosesi>();

        [DisplayName("Kapalı")]
        public bool Kapali { get; set; }

        [DisplayName("Köşe Sayısı")]
        public int KoseSayisi => Koseler.Count;

        [DisplayName("Uzunluk")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Uzunluk
        {
            get
            {
                // Düz parçada kiriş, yay parçasında r·|açı| (tam değer).
                double toplam = 0;
                for (var i = 0; i < ParcaSayisi; i++)
                {
                    ParcaUclari(i, out var a, out var b);
                    var kiris = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                    if (Math.Abs(a.Bulge) < 1e-9 || kiris < 1e-12)
                    {
                        toplam += kiris;
                        continue;
                    }
                    YayBilgisi(a, b, out _, out _, out var r, out _, out var supurme);
                    toplam += r * Math.Abs(supurme);
                }
                return toplam;
            }
        }

        /// <summary>Kapalı çoklu doğrunun alanı (açıksa 0): köşelerin çokgen alanına yay parçalarının daire dilimleri eklenir.</summary>
        [DisplayName("Alan")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Alan
        {
            get
            {
                if (!Kapali || Koseler.Count < 2)
                    return 0;
                double alan = 0;
                for (var i = 0; i < Koseler.Count; i++)
                {
                    var j = (i + 1) % Koseler.Count;
                    alan += (Koseler[i].X * Koseler[j].Y - Koseler[j].X * Koseler[i].Y) / 2;
                }
                // Saat yönünün tersine dönen (artı bulge) yay, kirişin sağına taşar: işaretli alana eklenir.
                for (var i = 0; i < ParcaSayisi; i++)
                {
                    ParcaUclari(i, out var a, out var b);
                    if (Math.Abs(a.Bulge) < 1e-9)
                        continue;
                    YayBilgisi(a, b, out _, out _, out var r, out _, out var supurme);
                    var aci = Math.Abs(supurme);
                    alan += Math.Sign(a.Bulge) * r * r / 2 * (aci - Math.Sin(aci));
                }
                return Math.Abs(alan);
            }
        }
        /// <summary>İlk köşenin kotu; değiştirilirse bütün köşelere aynı kot verilir.</summary>
        [DisplayName("Kot")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Kot
        {
            get => Koseler.Count > 0 ? Koseler[0].Z : 0;
            set
            {
                foreach (var kose in Koseler)
                    kose.Z = value;
            }
        }

        [Browsable(false)]
        public int ParcaSayisi => Koseler.Count < 2 ? 0 : Kapali ? Koseler.Count : Koseler.Count - 1;

        /// <summary>i. parçanın başlangıç ve bitiş köşesi.</summary>
        public void ParcaUclari(int i, out CokluDogruKosesi a, out CokluDogruKosesi b)
        {
            a = Koseler[i];
            b = Koseler[(i + 1) % Koseler.Count];
        }

        /// <summary>Yay parçasının merkezi, yarıçapı, başlangıç açısı ve (işaretli) süpürme açısı (radyan).</summary>
        public static void YayBilgisi(CokluDogruKosesi a, CokluDogruKosesi b, out double mx, out double my, out double r, out double baslangicAcisi, out double supurme)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, kiris = Math.Sqrt(dx * dx + dy * dy);
            supurme = 4 * Math.Atan(a.Bulge);
            r = kiris * (1 + a.Bulge * a.Bulge) / (4 * Math.Abs(a.Bulge));
            // Merkez, kirişin ortasından kirişin soluna (bulge artıysa) kayıktır.
            var kayma = kiris * (1 - a.Bulge * a.Bulge) / (4 * a.Bulge);
            mx = (a.X + b.X) / 2 - dy / kiris * kayma;
            my = (a.Y + b.Y) / 2 + dx / kiris * kayma;
            baslangicAcisi = Math.Atan2(a.Y - my, a.X - mx);
        }

        /// <summary>i. parçanın örneklenmiş noktaları ({x, y, z}); düz parçada yalnızca iki uç.</summary>
        public List<double[]> ParcaNoktalari(int i, int yayOrnekSayisi)
        {
            ParcaUclari(i, out var a, out var b);
            var sonuc = new List<double[]>();
            if (Math.Abs(a.Bulge) < 1e-9 || (Math.Abs(b.X - a.X) < 1e-12 && Math.Abs(b.Y - a.Y) < 1e-12))
            {
                sonuc.Add(new[] { a.X, a.Y, a.Z });
                sonuc.Add(new[] { b.X, b.Y, b.Z });
                return sonuc;
            }

            YayBilgisi(a, b, out var mx, out var my, out var r, out var basAci, out var supurme);
            var adim = Math.Max(2, (int)Math.Ceiling(yayOrnekSayisi * Math.Abs(supurme) / (2 * Math.PI)));
            for (var k = 0; k <= adim; k++)
            {
                var t = (double)k / adim;
                var aci = basAci + supurme * t;
                sonuc.Add(new[] { mx + r * Math.Cos(aci), my + r * Math.Sin(aci), a.Z + (b.Z - a.Z) * t });
            }
            // Uçlar tam köşe koordinatında olsun.
            sonuc[0] = new[] { a.X, a.Y, a.Z };
            sonuc[sonuc.Count - 1] = new[] { b.X, b.Y, b.Z };
            return sonuc;
        }

        /// <summary>Bütün çoklu doğrunun örneklenmiş noktaları (kapalıysa son nokta tekrar edilmez).</summary>
        public List<double[]> Noktalar(int yayOrnekSayisi = 72)
        {
            var sonuc = new List<double[]>();
            for (var i = 0; i < ParcaSayisi; i++)
            {
                var parca = ParcaNoktalari(i, yayOrnekSayisi);
                sonuc.AddRange(sonuc.Count == 0 ? parca : parca.Skip(1));
            }
            if (Kapali && sonuc.Count > 1)
                sonuc.RemoveAt(sonuc.Count - 1);
            if (sonuc.Count == 0 && Koseler.Count == 1)
                sonuc.Add(new[] { Koseler[0].X, Koseler[0].Y, Koseler[0].Z });
            return sonuc;
        }

        public CokluDogruNesnesi Kopyala()
        {
            var kopya = new CokluDogruNesnesi { Kapali = Kapali };
            kopya.Koseler.AddRange(Koseler.Select(k => k.Kopyala()));
            return kopya;
        }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            var noktalar = Noktalar();
            if (noktalar.Count < 2)
                return;
            var ekran = noktalar.Select(p => gorunum.DunyadanEkrana(p[0], p[1], panelGenislik, panelYukseklik)).ToArray();
            using (var kalem = new Pen(Secili ? Color.DodgerBlue : CizimRengi(Color.Black), Secili ? 2f : 1f))
            {
                if (Kapali)
                    grafik.DrawPolygon(kalem, ekran);
                else
                    grafik.DrawLines(kalem, ekran);
            }

            if (Secili)
            {
                using (var firca = new SolidBrush(Color.DodgerBlue))
                {
                    foreach (var kose in Koseler)
                    {
                        var p = gorunum.DunyadanEkrana(kose.X, kose.Y, panelGenislik, panelYukseklik);
                        grafik.FillRectangle(firca, p.X - 3, p.Y - 3, 6, 6);
                    }
                }
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            return EnYakinNokta(dunyaNoktasi.X, dunyaNoktasi.Y, out _, out _) <= toleransDunya;
        }

        /// <summary>Verilen noktaya çoklu doğru üzerindeki en yakın nokta ve uzaklığı.</summary>
        public double EnYakinNokta(double x, double y, out double px, out double py)
        {
            px = py = 0;
            var enYakin = double.MaxValue;
            var noktalar = Noktalar();
            if (Kapali && noktalar.Count > 1)
                noktalar.Add(noktalar[0]);
            for (var i = 0; i + 1 < noktalar.Count; i++)
            {
                double ax = noktalar[i][0], ay = noktalar[i][1], bx = noktalar[i + 1][0], by = noktalar[i + 1][1];
                double dx = bx - ax, dy = by - ay, boyKare = dx * dx + dy * dy;
                var t = boyKare < 1e-18 ? 0 : Math.Max(0, Math.Min(1, ((x - ax) * dx + (y - ay) * dy) / boyKare));
                double qx = ax + t * dx, qy = ay + t * dy, uzaklik = Math.Sqrt((x - qx) * (x - qx) + (y - qy) * (y - qy));
                if (uzaklik < enYakin)
                {
                    enYakin = uzaklik;
                    px = qx;
                    py = qy;
                }
            }
            if (noktalar.Count == 1)
            {
                px = noktalar[0][0];
                py = noktalar[0][1];
                enYakin = Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
            }
            return enYakin;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            foreach (var kose in Koseler)
                yield return new PointF((float)kose.X, (float)kose.Y);
        }

        public override PointF? DikNoktaAl(PointF referansDunyaNoktasi)
        {
            if (Koseler.Count < 2)
                return null;
            EnYakinNokta(referansDunyaNoktasi.X, referansDunyaNoktasi.Y, out var px, out var py);
            return new PointF((float)px, (float)py);
        }
    }
}
