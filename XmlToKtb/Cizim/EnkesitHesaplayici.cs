using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace XmlToKtb.Cizim
{
    /// <summary>Enkesit kotlarının nereden alınacağı.</summary>
    public enum EnkesitKaynagi
    {
        /// <summary>Üçgen model (yüzey) objesi: kesit çizgisinin üçgen kenarlarını kestiği yerler.</summary>
        Yuzey,
        /// <summary>Seçilen tabakalardaki kotlu çizgi, yay ve daireler (eş yükseklik eğrileri, 3B çizgiler).</summary>
        TabakaObjeleri,
        /// <summary>Seçilen tabakalardaki noktalar: kesit çizgisine en yakın ilerideki ve gerideki noktalar izdüşürülür.</summary>
        Noktalar
    }

    public class EnkesitAyarlari
    {
        public GuzergahNesnesi Guzergah;
        public double BaslangicKm, BitisKm;
        /// <summary>Kesitler arası kilometre aralığı (m).</summary>
        public double Aralik = 20;
        /// <summary>Aralıklı kesitler aralığın katlarına (ör. 0+020, 0+040) oturtulur; değilse başlangıç kilometresinden sayılır.</summary>
        public bool AraligaYuvarla = true;
        /// <summary>Güzergah geometri noktalarında (doğru, kurp, spiral birleşimleri) da kesit alınır.</summary>
        public bool GeometriNoktalari = true;
        /// <summary>Başlangıç ve bitiş kilometrelerinde de kesit alınır.</summary>
        public bool BaslangicVeBitis = true;
        /// <summary>Ayrıca kesit alınacak kilometreler.</summary>
        public List<double> EkKilometreler = new List<double>();
        /// <summary>Eksenin soluna ve sağına kesit genişliği (m).</summary>
        public double SolGenislik = 30, SagGenislik = 30;

        public EnkesitKaynagi Kaynak = EnkesitKaynagi.Yuzey;
        public YuzeyNesnesi Yuzey;
        /// <summary>Tabaka objeleri ve noktalar kaynağında kullanılacak tabakalar (null: hepsi).</summary>
        public HashSet<Tabaka> Tabakalar;
        /// <summary>Noktalardan kesitte kesit çizgisinin önünde ve arkasında aranacak uzaklık (m).</summary>
        public double AramaMesafesi = 5;
        /// <summary>Noktalardan kesitte her bu aralıkta en yakın ilerideki ve gerideki nokta alınır (m).</summary>
        public double NoktaAraligi = 0.10;
        /// <summary>Eksende (mesafe 0) nokta yoksa iki yanındaki noktalardan enterpolasyonla eklenir.</summary>
        public bool EksenNoktasiEkle = true;
        /// <summary>Kesit boyunca bundan daha yakın noktalar birleştirilir (m; 0: kapalı).</summary>
        public double BirlestirmeHassasiyeti = 0.01;
    }

    /// <summary>Bir enkesitin plandaki yeri: eksen noktası, sol ve sağ uçları.</summary>
    public class EnkesitYeri
    {
        public double Km;
        public double EksenX, EksenY, SolX, SolY, SagX, SagY;
        /// <summary>Gidiş doğrultusu (+X'ten saat yönünün tersine, radyan).</summary>
        public double Aci;
    }

    /// <summary>
    /// Güzergaha dik enkesitleri çizimdeki objelerden üretir. Mesafeler eksenin solunda eksi, sağında artıdır
    /// (gidiş yönüne göre); kotlar objelerin Z değerleridir.
    /// </summary>
    public static class EnkesitHesaplayici
    {
        /// <summary>Ayarlara göre kesit alınacak kilometreler (küçükten büyüğe, 1 mm'den yakın olanlar tek sayılır).</summary>
        public static List<double> KilometreleriBelirle(EnkesitAyarlari ayar)
        {
            var g = ayar.Guzergah;
            double bas = Math.Max(ayar.BaslangicKm, g.BaslangicKm), son = Math.Min(ayar.BitisKm, g.BitisKm);
            var kmler = new List<double>();
            if (son < bas)
                return kmler;

            if (ayar.Aralik > 0)
            {
                var ilk = ayar.AraligaYuvarla ? Math.Ceiling(bas / ayar.Aralik - 1e-9) * ayar.Aralik : bas;
                for (var km = ilk; km <= son + 1e-9 && kmler.Count < 1000000; km += ayar.Aralik)
                    kmler.Add(Math.Round(km, 6));
            }
            if (ayar.BaslangicVeBitis)
            {
                kmler.Add(bas);
                kmler.Add(son);
            }
            if (ayar.GeometriNoktalari)
            {
                foreach (var parca in g.Parcalar)
                {
                    kmler.Add(parca.BaslangicKm);
                    kmler.Add(parca.BitisKm);
                }
            }
            kmler.AddRange(ayar.EkKilometreler);

            kmler = kmler.Where(km => km >= bas - 1e-6 && km <= son + 1e-6).OrderBy(km => km).ToList();
            var tekil = new List<double>();
            foreach (var km in kmler)
            {
                if (tekil.Count == 0 || km - tekil[tekil.Count - 1] > 1e-3)
                    tekil.Add(km);
            }
            return tekil;
        }

        /// <summary>
        /// Enkesitleri üretir. Kotu bulunamayan (kaynakta en az iki nokta çıkmayan) kilometreler atlanır ve
        /// <paramref name="bosKesit"/> ile sayılır; kesitlerin plandaki yerleri <paramref name="yerler"/> ile verilir.
        /// </summary>
        public static KseDosyasi Olustur(EnkesitAyarlari ayar, IList<CizimNesnesi> nesneler, Action<double> ilerleme, out int bosKesit, out List<EnkesitYeri> yerler)
        {
            bosKesit = 0;
            yerler = new List<EnkesitYeri>();
            var dosya = new KseDosyasi();
            var kmler = KilometreleriBelirle(ayar);

            bool TabakaUygun(CizimNesnesi n) => ayar.Tabakalar == null || (n.Tabaka != null && ayar.Tabakalar.Contains(n.Tabaka));
            var yuzeyIndeksi = ayar.Kaynak == EnkesitKaynagi.Yuzey && ayar.Yuzey != null ? new UcgenIndeksi(ayar.Yuzey) : null;
            // Çoklu doğru ve bloklar parçalarıyla (çizgi/yay) kesiştirilir.
            var cizgiler = ayar.Kaynak == EnkesitKaynagi.TabakaObjeleri
                ? nesneler.Where(n => !(n is NoktaNesnesi) && TabakaUygun(n))
                    .SelectMany(n => n is CokluDogruNesnesi || n is BlokNesnesi ? (IEnumerable<CizimNesnesi>)GeometriIslemleri.Patlat(n, false) : new[] { n }).ToList()
                : null;
            var noktalar = ayar.Kaynak == EnkesitKaynagi.Noktalar ? nesneler.OfType<NoktaNesnesi>().Where(TabakaUygun).ToList() : null;

            for (var k = 0; k < kmler.Count; k++)
            {
                ilerleme?.Invoke((double)k / kmler.Count);
                var km = kmler[k];
                if (!ayar.Guzergah.KonumAl(km, out var x, out var y, out var aci))
                {
                    bosKesit++;
                    continue;
                }

                // Gidişe göre sağ taraf (sin a, -cos a); kesit soldan sağa -SolGenislik .. +SagGenislik.
                double sx = Math.Sin(aci), sy = -Math.Cos(aci);
                var yer = new EnkesitYeri
                {
                    Km = km, Aci = aci, EksenX = x, EksenY = y,
                    SolX = x - sx * ayar.SolGenislik, SolY = y - sy * ayar.SolGenislik,
                    SagX = x + sx * ayar.SagGenislik, SagY = y + sy * ayar.SagGenislik
                };

                List<double[]> ham;
                switch (ayar.Kaynak)
                {
                    case EnkesitKaynagi.Yuzey:
                        ham = yuzeyIndeksi != null ? yuzeyIndeksi.Kes(yer, ayar.SolGenislik, ayar.SagGenislik) : new List<double[]>();
                        break;
                    case EnkesitKaynagi.TabakaObjeleri:
                        ham = ObjeleriKes(cizgiler, yer, ayar.SolGenislik, ayar.SagGenislik);
                        break;
                    default:
                        ham = NoktalardanKesit(noktalar, yer, ayar);
                        break;
                }

                var kesitNoktalari = Duzenle(ham, ayar);
                if (kesitNoktalari.Count < 2)
                {
                    bosKesit++;
                    continue;
                }

                yerler.Add(yer);
                var no = (dosya.Kesitler.Count + 1).ToString(CultureInfo.InvariantCulture);
                var kmMetni = km.ToString("0.000", CultureInfo.InvariantCulture);
                dosya.Kesitler.Add(new KseKesit
                {
                    No = no, Kilometre = kmMetni, Km = km, Aciklama = "", Baslik = no + "  " + kmMetni,
                    Noktalar = kesitNoktalari.Select(p => new KseNokta { Mesafe = Math.Round(p[0], 3), Kot = Math.Round(p[1], 3), Kod = "" }).ToList()
                });
            }

            ilerleme?.Invoke(1);
            return dosya;
        }

        // Sıralar, üst üste düşenleri ayıklar, istenirse eksen noktasını ekler ve sık noktaları birleştirir.
        private static List<double[]> Duzenle(List<double[]> ham, EnkesitAyarlari ayar)
        {
            var sirali = ham.Where(p => !double.IsNaN(p[1])).OrderBy(p => p[0]).ToList();
            var tekil = new List<double[]>();
            foreach (var p in sirali)
            {
                if (tekil.Count > 0 && p[0] - tekil[tekil.Count - 1][0] < 1e-3)
                    continue;
                tekil.Add(p);
            }

            if (ayar.EksenNoktasiEkle && tekil.Count >= 2 && !tekil.Any(p => Math.Abs(p[0]) < 1e-3))
            {
                var sag = tekil.FindIndex(p => p[0] > 0);
                if (sag > 0)
                {
                    double[] a = tekil[sag - 1], b = tekil[sag];
                    var t = -a[0] / (b[0] - a[0]);
                    tekil.Insert(sag, new[] { 0.0, a[1] + t * (b[1] - a[1]) });
                }
            }

            if (ayar.BirlestirmeHassasiyeti <= 0 || tekil.Count <= 2)
                return tekil;

            double Uzaklik(double[] a, double[] b) => Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]));
            var sonuc = new List<double[]> { tekil[0] };
            for (var i = 1; i < tekil.Count - 1; i++)
            {
                // Eksen noktası birleştirmede atılmaz.
                if (Math.Abs(tekil[i][0]) < 1e-3 || Uzaklik(tekil[i], sonuc[sonuc.Count - 1]) >= ayar.BirlestirmeHassasiyeti)
                    sonuc.Add(tekil[i]);
            }
            var son = tekil[tekil.Count - 1];
            if (sonuc.Count > 1 && Math.Abs(sonuc[sonuc.Count - 1][0]) >= 1e-3 && Uzaklik(son, sonuc[sonuc.Count - 1]) < ayar.BirlestirmeHassasiyeti)
                sonuc.RemoveAt(sonuc.Count - 1);
            sonuc.Add(son);
            return sonuc;
        }

        // İki doğru parçasının kesişimi: kesit çizgisi üzerindeki oran (t) ve diğer parça üzerindeki oran (u).
        private static bool ParcalarKesisir(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy, out double t, out double u)
        {
            t = u = 0;
            double rx = bx - ax, ry = by - ay, qx = dx - cx, qy = dy - cy;
            var payda = rx * qy - ry * qx;
            if (Math.Abs(payda) < 1e-12)
                return false;
            t = ((cx - ax) * qy - (cy - ay) * qx) / payda;
            u = ((cx - ax) * ry - (cy - ay) * rx) / payda;
            return t >= -1e-9 && t <= 1 + 1e-9 && u >= -1e-9 && u <= 1 + 1e-9;
        }

        // Kotlu çizgi, yay ve dairelerin kesit çizgisini kestiği yerler.
        private static List<double[]> ObjeleriKes(List<CizimNesnesi> objeler, EnkesitYeri yer, double sol, double sag)
        {
            var sonuc = new List<double[]>();
            var boy = sol + sag;
            foreach (var obje in objeler)
            {
                if (obje is CizgiNesnesi cizgi)
                {
                    if (ParcalarKesisir(yer.SolX, yer.SolY, yer.SagX, yer.SagY, cizgi.BaslangicX, cizgi.BaslangicY, cizgi.BitisX, cizgi.BitisY, out var t, out var u))
                        sonuc.Add(new[] { -sol + t * boy, cizgi.BaslangicZ + u * (cizgi.BitisZ - cizgi.BaslangicZ) });
                }
                else if (obje is DaireNesnesi daire)
                {
                    foreach (var t in CemberKesisimleri(yer, daire.MerkezX, daire.MerkezY, daire.Yaricap))
                        sonuc.Add(new[] { -sol + t * boy, daire.MerkezZ });
                }
                else if (obje is YayNesnesi yay)
                {
                    foreach (var t in CemberKesisimleri(yer, yay.MerkezX, yay.MerkezY, yay.Yaricap))
                    {
                        double px = yer.SolX + t * (yer.SagX - yer.SolX), py = yer.SolY + t * (yer.SagY - yer.SolY);
                        var aci = Math.Atan2(py - yay.MerkezY, px - yay.MerkezX) * 180 / Math.PI;
                        double yayBoyu = ((yay.BitisAcisi - yay.BaslangicAcisi) % 360 + 360) % 360, nokta = ((aci - yay.BaslangicAcisi) % 360 + 360) % 360;
                        if (yayBoyu == 0 || nokta <= yayBoyu)
                            sonuc.Add(new[] { -sol + t * boy, yay.MerkezZ });
                    }
                }
            }
            return sonuc;
        }

        // Kesit çizgisinin bir çemberi kestiği oranlar (0-1 arası).
        private static IEnumerable<double> CemberKesisimleri(EnkesitYeri yer, double mx, double my, double r)
        {
            double dx = yer.SagX - yer.SolX, dy = yer.SagY - yer.SolY, fx = yer.SolX - mx, fy = yer.SolY - my;
            double a = dx * dx + dy * dy, b = 2 * (fx * dx + fy * dy), c = fx * fx + fy * fy - r * r;
            var d = b * b - 4 * a * c;
            if (a < 1e-12 || d < 0)
                yield break;
            var kok = Math.Sqrt(d);
            foreach (var t in new[] { (-b - kok) / (2 * a), (-b + kok) / (2 * a) })
            {
                if (t >= 0 && t <= 1)
                    yield return t;
            }
        }

        // NetCAD mantığı: her nokta aralığında kesit çizgisinin ilerisindeki ve gerisindeki en yakın nokta.
        private static List<double[]> NoktalardanKesit(List<NoktaNesnesi> noktalar, EnkesitYeri yer, EnkesitAyarlari ayar)
        {
            double tx = Math.Cos(yer.Aci), ty = Math.Sin(yer.Aci), sx = Math.Sin(yer.Aci), sy = -Math.Cos(yer.Aci);
            var aralik = Math.Max(1e-3, ayar.NoktaAraligi);
            var ileri = new Dictionary<long, NoktaNesnesi>();
            var geri = new Dictionary<long, NoktaNesnesi>();
            var ilerilik = new Dictionary<NoktaNesnesi, double>();
            foreach (var nokta in noktalar)
            {
                double dx = nokta.X - yer.EksenX, dy = nokta.Y - yer.EksenY;
                var ilerisi = dx * tx + dy * ty;
                var mesafe = dx * sx + dy * sy;
                if (Math.Abs(ilerisi) > ayar.AramaMesafesi || mesafe < -ayar.SolGenislik || mesafe > ayar.SagGenislik)
                    continue;
                var dilim = (long)Math.Floor(mesafe / aralik);
                var hedef = ilerisi >= 0 ? ileri : geri;
                ilerilik[nokta] = Math.Abs(ilerisi);
                if (!hedef.TryGetValue(dilim, out var mevcut) || Math.Abs(ilerisi) < ilerilik[mevcut])
                    hedef[dilim] = nokta;
            }

            return ileri.Values.Concat(geri.Values)
                .Select(n => new[] { (n.X - yer.EksenX) * sx + (n.Y - yer.EksenY) * sy, n.Z })
                .ToList();
        }

        /// <summary>Yüzeyin üçgenlerini ızgaraya dağıtan, kesit çizgisinin kestiği üçgenleri hızlı bulan dizin.</summary>
        private class UcgenIndeksi
        {
            private readonly YuzeyNesnesi _yuzey;
            private readonly double _minX, _minY, _hucre;
            private readonly int _nx, _ny;
            private readonly List<int>[] _hucreler;
            private readonly int[] _damga;
            private int _damgaNo;

            public UcgenIndeksi(YuzeyNesnesi yuzey)
            {
                _yuzey = yuzey;
                var ucgenler = yuzey.Ucgenler;
                var ucgenSayisi = ucgenler.Count / 3;
                double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
                var canli = 0;
                for (var u = 0; u < ucgenSayisi; u++)
                {
                    if (ucgenler[u * 3] < 0)
                        continue;
                    canli++;
                    for (var k = 0; k < 3; k++)
                    {
                        var n = ucgenler[u * 3 + k];
                        minX = Math.Min(minX, yuzey.X[n]); maxX = Math.Max(maxX, yuzey.X[n]);
                        minY = Math.Min(minY, yuzey.Y[n]); maxY = Math.Max(maxY, yuzey.Y[n]);
                    }
                }
                if (canli == 0)
                {
                    _hucreler = new List<int>[0];
                    _damga = new int[0];
                    return;
                }

                // Hücre başına ortalama birkaç üçgen düşecek boyut.
                _hucre = Math.Max(0.5, Math.Sqrt(Math.Max(1e-6, (maxX - minX) * (maxY - minY)) / canli) * 2);
                _minX = minX;
                _minY = minY;
                _nx = Math.Max(1, (int)((maxX - minX) / _hucre) + 1);
                _ny = Math.Max(1, (int)((maxY - minY) / _hucre) + 1);
                _hucreler = new List<int>[_nx * _ny];
                _damga = new int[ucgenSayisi];
                for (var u = 0; u < ucgenSayisi; u++)
                {
                    if (ucgenler[u * 3] < 0)
                        continue;
                    double ux0 = double.MaxValue, uy0 = double.MaxValue, ux1 = double.MinValue, uy1 = double.MinValue;
                    for (var k = 0; k < 3; k++)
                    {
                        var n = ucgenler[u * 3 + k];
                        ux0 = Math.Min(ux0, yuzey.X[n]); ux1 = Math.Max(ux1, yuzey.X[n]);
                        uy0 = Math.Min(uy0, yuzey.Y[n]); uy1 = Math.Max(uy1, yuzey.Y[n]);
                    }
                    int cx0 = Hx(ux0), cx1 = Hx(ux1), cy0 = Hy(uy0), cy1 = Hy(uy1);
                    for (var cy = cy0; cy <= cy1; cy++)
                    {
                        for (var cx = cx0; cx <= cx1; cx++)
                        {
                            var c = cy * _nx + cx;
                            (_hucreler[c] ?? (_hucreler[c] = new List<int>())).Add(u);
                        }
                    }
                }
            }

            private int Hx(double x) => Math.Max(0, Math.Min(_nx - 1, (int)((x - _minX) / _hucre)));
            private int Hy(double y) => Math.Max(0, Math.Min(_ny - 1, (int)((y - _minY) / _hucre)));

            /// <summary>
            /// Kesit çizgisinin üçgen kenarlarını kestiği noktalar ile kesit uçlarının üçgen içindeyse kotları
            /// ({mesafe, kot}).
            /// </summary>
            public List<double[]> Kes(EnkesitYeri yer, double sol, double sag)
            {
                var sonuc = new List<double[]>();
                if (_hucreler.Length == 0)
                    return sonuc;

                _damgaNo++;
                var boy = sol + sag;
                var adimSayisi = (int)Math.Ceiling(boy / (_hucre / 2)) + 1;
                List<double> X = _yuzey.X, Y = _yuzey.Y, Z = _yuzey.Z;
                var ucgenler = _yuzey.Ucgenler;
                for (var a = 0; a <= adimSayisi; a++)
                {
                    var t = Math.Min(1.0, (double)a / adimSayisi);
                    int ox = Hx(yer.SolX + t * (yer.SagX - yer.SolX)), oy = Hy(yer.SolY + t * (yer.SagY - yer.SolY));
                    for (var cy = Math.Max(0, oy - 1); cy <= Math.Min(_ny - 1, oy + 1); cy++)
                    {
                        for (var cx = Math.Max(0, ox - 1); cx <= Math.Min(_nx - 1, ox + 1); cx++)
                        {
                            var liste = _hucreler[cy * _nx + cx];
                            if (liste == null)
                                continue;
                            foreach (var u in liste)
                            {
                                if (_damga[u] == _damgaNo)
                                    continue;
                                _damga[u] = _damgaNo;
                                int n0 = ucgenler[u * 3], n1 = ucgenler[u * 3 + 1], n2 = ucgenler[u * 3 + 2];
                                if (n0 < 0)
                                    continue;

                                // Kenar kesişimleri.
                                var kenarlar = new[] { new[] { n0, n1 }, new[] { n1, n2 }, new[] { n2, n0 } };
                                foreach (var kenar in kenarlar)
                                {
                                    int p = kenar[0], q = kenar[1];
                                    if (ParcalarKesisir(yer.SolX, yer.SolY, yer.SagX, yer.SagY, X[p], Y[p], X[q], Y[q], out var kt, out var ku))
                                        sonuc.Add(new[] { -sol + kt * boy, Z[p] + ku * (Z[q] - Z[p]) });
                                }

                                // Kesit uçları üçgenin içindeyse kotları düzlemden hesaplanır.
                                foreach (var uc in new[] { new[] { yer.SolX, yer.SolY, -sol }, new[] { yer.SagX, yer.SagY, sag } })
                                {
                                    var kot = UcgendeKot(uc[0], uc[1], X[n0], Y[n0], Z[n0], X[n1], Y[n1], Z[n1], X[n2], Y[n2], Z[n2]);
                                    if (!double.IsNaN(kot))
                                        sonuc.Add(new[] { uc[2], kot });
                                }
                            }
                        }
                    }
                }
                return sonuc;
            }

            // Nokta üçgenin içindeyse (kenarlar dahil) üçgen düzlemindeki kotu; değilse NaN.
            private static double UcgendeKot(double x, double y, double x0, double y0, double z0, double x1, double y1, double z1, double x2, double y2, double z2)
            {
                var d = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
                if (Math.Abs(d) < 1e-12)
                    return double.NaN;
                var a = ((y1 - y2) * (x - x2) + (x2 - x1) * (y - y2)) / d;
                var b = ((y2 - y0) * (x - x2) + (x0 - x2) * (y - y2)) / d;
                var c = 1 - a - b;
                if (a < -1e-9 || b < -1e-9 || c < -1e-9)
                    return double.NaN;
                return a * z0 + b * z1 + c * z2;
            }
        }
    }
}
