using System;
using System.Collections.Generic;
using System.Linq;

namespace XmlToKtb.Cizim
{
    /// <summary>Objelerin geometrisini oluşturan temel parça: doğru parçası ya da yay (çift hassasiyetli).</summary>
    public class Primitif
    {
        public bool YayMi;
        // Doğru parçası (ve yayın uçları).
        public double X1, Y1, Z1, X2, Y2, Z2;
        // Yay: merkez, yarıçap, başlangıç açısı ve işaretli süpürme açısı (radyan, artı saat yönünün tersine).
        public double Mx, My, R, BasAci, Supurme;

        public static Primitif Dogru(double x1, double y1, double z1, double x2, double y2, double z2) =>
            new Primitif { X1 = x1, Y1 = y1, Z1 = z1, X2 = x2, Y2 = y2, Z2 = z2 };

        public static Primitif Yay(double mx, double my, double r, double basAci, double supurme, double z)
        {
            return new Primitif
            {
                YayMi = true, Mx = mx, My = my, R = r, BasAci = basAci, Supurme = supurme,
                X1 = mx + r * Math.Cos(basAci), Y1 = my + r * Math.Sin(basAci), Z1 = z,
                X2 = mx + r * Math.Cos(basAci + supurme), Y2 = my + r * Math.Sin(basAci + supurme), Z2 = z
            };
        }

        /// <summary>Açı (radyan) yayın süpürdüğü aralıkta mı?</summary>
        public bool AciIcindeMi(double aci)
        {
            var fark = Supurme >= 0 ? aci - BasAci : BasAci - aci;
            fark %= 2 * Math.PI;
            if (fark < 0) fark += 2 * Math.PI;
            return fark <= Math.Abs(Supurme) + 1e-9 || Math.Abs(Supurme) >= 2 * Math.PI - 1e-9;
        }

        /// <summary>Yayı ya da doğruyu örnekleyen noktalar ({x, y}).</summary>
        public List<double[]> Noktalar(int tamDaireOrnek = 96)
        {
            if (!YayMi)
                return new List<double[]> { new[] { X1, Y1 }, new[] { X2, Y2 } };
            var adim = Math.Max(2, (int)Math.Ceiling(tamDaireOrnek * Math.Abs(Supurme) / (2 * Math.PI)));
            var sonuc = new List<double[]>();
            for (var k = 0; k <= adim; k++)
            {
                var a = BasAci + Supurme * k / adim;
                sonuc.Add(new[] { Mx + R * Math.Cos(a), My + R * Math.Sin(a) });
            }
            return sonuc;
        }
    }

    /// <summary>
    /// Düzenleme komutlarının geometrisi: objeleri temel parçalara ayırma, patlatma, uzatma, paralel kopya, çoklu
    /// doğruya birleştirme ve çizgi-obje kesişimleri.
    /// </summary>
    public static class GeometriIslemleri
    {
        // ------------------------------------------------------------------ temel parçalar

        /// <summary>Objenin geometrisini doğru parçaları ve yaylar olarak verir (nokta ve yazı için boş).</summary>
        public static List<Primitif> Primitifler(CizimNesnesi nesne)
        {
            var sonuc = new List<Primitif>();
            switch (nesne)
            {
                case CizgiNesnesi c:
                    sonuc.Add(Primitif.Dogru(c.BaslangicX, c.BaslangicY, c.BaslangicZ, c.BitisX, c.BitisY, c.BitisZ));
                    break;
                case YayNesnesi a:
                {
                    var supurme = (a.BitisAcisi - a.BaslangicAcisi) % 360;
                    if (supurme <= 0) supurme += 360;
                    sonuc.Add(Primitif.Yay(a.MerkezX, a.MerkezY, a.Yaricap, a.BaslangicAcisi * Math.PI / 180, supurme * Math.PI / 180, a.MerkezZ));
                    break;
                }
                case DaireNesnesi d:
                    sonuc.Add(Primitif.Yay(d.MerkezX, d.MerkezY, d.Yaricap, 0, 2 * Math.PI, d.MerkezZ));
                    break;
                case CokluDogruNesnesi cd:
                    for (var i = 0; i < cd.ParcaSayisi; i++)
                    {
                        cd.ParcaUclari(i, out var p, out var q);
                        if (Math.Abs(p.Bulge) < 1e-9)
                        {
                            sonuc.Add(Primitif.Dogru(p.X, p.Y, p.Z, q.X, q.Y, q.Z));
                        }
                        else
                        {
                            CokluDogruNesnesi.YayBilgisi(p, q, out var mx, out var my, out var r, out var bas, out var sup);
                            var yay = Primitif.Yay(mx, my, r, bas, sup, p.Z);
                            yay.X1 = p.X; yay.Y1 = p.Y; yay.X2 = q.X; yay.Y2 = q.Y; yay.Z2 = q.Z;
                            sonuc.Add(yay);
                        }
                    }
                    break;
                case SpiralNesnesi s:
                {
                    var noktalar = s.OrnekNoktalariGetir();
                    for (var i = 0; i + 1 < noktalar.Count; i++)
                        sonuc.Add(Primitif.Dogru(noktalar[i].X, noktalar[i].Y, 0, noktalar[i + 1].X, noktalar[i + 1].Y, 0));
                    break;
                }
                case GuzergahNesnesi g:
                    // Kopuk parçalar da yakalanabilir (arayı bağlarken uçlarına yakalanmak için).
                    foreach (var parca in g.TumParcalar)
                    {
                        if (parca.Tip == GuzergahParcaTipi.Dogru)
                        {
                            sonuc.Add(Primitif.Dogru(parca.BaslangicX, parca.BaslangicY, 0, parca.BitisX, parca.BitisY, 0));
                        }
                        else if (parca.Tip == GuzergahParcaTipi.Kurp)
                        {
                            KurpMerkezi(parca, out var mx, out var my);
                            var bas = Math.Atan2(parca.BaslangicY - my, parca.BaslangicX - mx);
                            var sup = (parca.Yon == DonusYonu.Sol ? 1 : -1) * parca.Uzunluk / parca.YaricapBaslangic;
                            var yay = Primitif.Yay(mx, my, parca.YaricapBaslangic, bas, sup, 0);
                            yay.X1 = parca.BaslangicX; yay.Y1 = parca.BaslangicY; yay.X2 = parca.BitisX; yay.Y2 = parca.BitisY;
                            sonuc.Add(yay);
                        }
                        else
                        {
                            for (var i = 0; i + 1 < parca.OrnekX.Length; i++)
                                sonuc.Add(Primitif.Dogru(parca.OrnekX[i], parca.OrnekY[i], 0, parca.OrnekX[i + 1], parca.OrnekY[i + 1], 0));
                        }
                    }
                    break;
                case BlokNesnesi blok:
                    foreach (var oge in blok.Ogeler)
                        sonuc.AddRange(Primitifler(oge));
                    break;

                case OlcuNesnesi olcu:
                    // Kesişim yakalama ve çit seçimi için ölçünün çizgileri.
                    foreach (var c in olcu.GeometriOlustur().Cizgiler)
                        sonuc.Add(Primitif.Dogru(c[0], c[1], 0, c[2], c[3], 0));
                    break;

                case KilavuzNesnesi kilavuz:
                    foreach (var c in kilavuz.GeometriOlustur().Cizgiler)
                        sonuc.Add(Primitif.Dogru(c[0], c[1], 0, c[2], c[3], 0));
                    break;
            }
            return sonuc;
        }

        private static void KurpMerkezi(GuzergahParcasi parca, out double mx, out double my)
        {
            var dikme = parca.BaslangicAcisi + (parca.Yon == DonusYonu.Sol ? Math.PI / 2 : -Math.PI / 2);
            mx = parca.BaslangicX + parca.YaricapBaslangic * Math.Cos(dikme);
            my = parca.BaslangicY + parca.YaricapBaslangic * Math.Sin(dikme);
        }

        // ------------------------------------------------------------------ kesişimler

        /// <summary>
        /// A noktasından B'ye giden (sonsuz) doğrunun parçayı kestiği yerlerin doğru üzerindeki oranları
        /// (A'da 0, B'de 1).
        /// </summary>
        public static List<double> DogruKesisimleri(double ax, double ay, double bx, double by, Primitif p)
        {
            var sonuc = new List<double>();
            double dx = bx - ax, dy = by - ay;
            if (!p.YayMi)
            {
                double qx = p.X2 - p.X1, qy = p.Y2 - p.Y1;
                var payda = dx * qy - dy * qx;
                if (Math.Abs(payda) < 1e-15)
                    return sonuc;
                var t = ((p.X1 - ax) * qy - (p.Y1 - ay) * qx) / payda;
                var u = ((p.X1 - ax) * dy - (p.Y1 - ay) * dx) / payda;
                if (u >= -1e-9 && u <= 1 + 1e-9)
                    sonuc.Add(t);
                return sonuc;
            }

            double fx = ax - p.Mx, fy = ay - p.My;
            double a = dx * dx + dy * dy, b = 2 * (fx * dx + fy * dy), c = fx * fx + fy * fy - p.R * p.R;
            var d = b * b - 4 * a * c;
            if (a < 1e-18 || d < 0)
                return sonuc;
            var kok = Math.Sqrt(d);
            foreach (var t in new[] { (-b - kok) / (2 * a), (-b + kok) / (2 * a) })
            {
                double x = ax + t * dx, y = ay + t * dy;
                if (p.AciIcindeMi(Math.Atan2(y - p.My, x - p.Mx)))
                    sonuc.Add(t);
            }
            return sonuc;
        }

        /// <summary>İki temel parçanın (doğru parçası / yay) kesişim noktaları ({x, y}).</summary>
        public static List<double[]> PrimitifKesisimleri(Primitif a, Primitif b)
        {
            var sonuc = new List<double[]>();
            if (!a.YayMi)
            {
                // Doğru parçası: diğer parçayla kesişimlerin doğru üzerindeki oranları 0-1 arasında olmalı.
                foreach (var t in DogruKesisimleri(a.X1, a.Y1, a.X2, a.Y2, b))
                {
                    if (t >= -1e-9 && t <= 1 + 1e-9)
                        sonuc.Add(new[] { a.X1 + t * (a.X2 - a.X1), a.Y1 + t * (a.Y2 - a.Y1) });
                }
                return sonuc;
            }
            if (!b.YayMi)
                return PrimitifKesisimleri(b, a);

            // İki çember: kesişim noktaları iki yayın da açı aralığında olmalı.
            double dx = b.Mx - a.Mx, dy = b.My - a.My, d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1e-12 || d > a.R + b.R + 1e-9 || d < Math.Abs(a.R - b.R) - 1e-9)
                return sonuc;
            var l = (a.R * a.R - b.R * b.R + d * d) / (2 * d);
            var h = Math.Sqrt(Math.Max(0, a.R * a.R - l * l));
            double px = a.Mx + l * dx / d, py = a.My + l * dy / d;
            foreach (var isaret in h < 1e-12 ? new[] { 0.0 } : new[] { 1.0, -1.0 })
            {
                double x = px - isaret * h * dy / d, y = py + isaret * h * dx / d;
                if (a.AciIcindeMi(Math.Atan2(y - a.My, x - a.Mx)) && b.AciIcindeMi(Math.Atan2(y - b.My, x - b.Mx)))
                    sonuc.Add(new[] { x, y });
            }
            return sonuc;
        }

        /// <summary>İki doğru parçası kesişiyor mu (uçlar dahil)?</summary>
        public static bool ParcalarKesisir(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
        {
            double rx = bx - ax, ry = by - ay, qx = dx - cx, qy = dy - cy;
            var payda = rx * qy - ry * qx;
            if (Math.Abs(payda) < 1e-15)
                return false;
            var t = ((cx - ax) * qy - (cy - ay) * qx) / payda;
            var u = ((cx - ax) * ry - (cy - ay) * rx) / payda;
            return t >= -1e-9 && t <= 1 + 1e-9 && u >= -1e-9 && u <= 1 + 1e-9;
        }

        /// <summary>Obje, verilen kırık çizgiyi (çit) kesiyor mu? Nokta ve yazılar çite tolerans kadar yakınsa sayılır.</summary>
        public static bool CitiKesiyorMu(CizimNesnesi nesne, List<double[]> cit, double tolerans)
        {
            double? nx = null, ny = null;
            if (nesne is NoktaNesnesi n) { nx = n.X; ny = n.Y; }
            else if (nesne is YaziNesnesi y) { nx = y.KonumX; ny = y.KonumY; }
            if (nx != null)
            {
                for (var i = 0; i + 1 < cit.Count; i++)
                {
                    if (NoktaParcaUzakligi(nx.Value, ny.Value, cit[i][0], cit[i][1], cit[i + 1][0], cit[i + 1][1]) <= tolerans)
                        return true;
                }
                return false;
            }

            foreach (var p in Primitifler(nesne))
            {
                var noktalar = p.Noktalar();
                for (var k = 0; k + 1 < noktalar.Count; k++)
                {
                    for (var i = 0; i + 1 < cit.Count; i++)
                    {
                        if (ParcalarKesisir(noktalar[k][0], noktalar[k][1], noktalar[k + 1][0], noktalar[k + 1][1], cit[i][0], cit[i][1], cit[i + 1][0], cit[i + 1][1]))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Objenin herhangi bir parçası verilen kutuya giriyor mu?</summary>
        public static bool KutuyuKesiyorMu(CizimNesnesi nesne, double minX, double minY, double maxX, double maxY)
        {
            bool Icinde(double x, double y) => x >= minX && x <= maxX && y >= minY && y <= maxY;
            foreach (var p in Primitifler(nesne))
            {
                var noktalar = p.Noktalar();
                for (var k = 0; k + 1 < noktalar.Count; k++)
                {
                    double x1 = noktalar[k][0], y1 = noktalar[k][1], x2 = noktalar[k + 1][0], y2 = noktalar[k + 1][1];
                    if (Icinde(x1, y1) || Icinde(x2, y2)
                        || ParcalarKesisir(x1, y1, x2, y2, minX, minY, maxX, minY) || ParcalarKesisir(x1, y1, x2, y2, maxX, minY, maxX, maxY)
                        || ParcalarKesisir(x1, y1, x2, y2, maxX, maxY, minX, maxY) || ParcalarKesisir(x1, y1, x2, y2, minX, maxY, minX, minY))
                        return true;
                }
            }
            return false;
        }

        public static double NoktaParcaUzakligi(double x, double y, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax, dy = by - ay, boyKare = dx * dx + dy * dy;
            var t = boyKare < 1e-18 ? 0 : Math.Max(0, Math.Min(1, ((x - ax) * dx + (y - ay) * dy) / boyKare));
            double qx = ax + t * dx - x, qy = ay + t * dy - y;
            return Math.Sqrt(qx * qx + qy * qy);
        }

        // ------------------------------------------------------------------ patlat

        /// <summary>
        /// Objeyi bir önceki objelerine ayırır: çoklu doğru doğru ve yaylara (istenirse köşelere nokta), blok içindeki
        /// objelere, güzergah doğru/yay/spirallere, kilometre etiketleri çentik ve yazılara, yüzey üçgen kenarlarına
        /// (3B çizgi) ve noktalarına. Patlatılamayan objede null döner.
        /// </summary>
        public static List<CizimNesnesi> Patlat(CizimNesnesi nesne, bool koseNoktalari)
        {
            List<CizimNesnesi> sonuc;
            switch (nesne)
            {
                case CokluDogruNesnesi cd:
                    sonuc = new List<CizimNesnesi>();
                    for (var i = 0; i < cd.ParcaSayisi; i++)
                    {
                        cd.ParcaUclari(i, out var p, out var q);
                        if (Math.Abs(p.Bulge) < 1e-9)
                        {
                            sonuc.Add(new CizgiNesnesi { BaslangicX = p.X, BaslangicY = p.Y, BaslangicZ = p.Z, BitisX = q.X, BitisY = q.Y, BitisZ = q.Z });
                        }
                        else
                        {
                            CokluDogruNesnesi.YayBilgisi(p, q, out var mx, out var my, out var r, out var bas, out var sup);
                            double basD = bas * 180 / Math.PI, bitD = (bas + sup) * 180 / Math.PI;
                            sonuc.Add(new YayNesnesi { MerkezX = mx, MerkezY = my, MerkezZ = p.Z, Yaricap = r, BaslangicAcisi = sup > 0 ? basD : bitD, BitisAcisi = sup > 0 ? bitD : basD });
                        }
                    }
                    if (koseNoktalari)
                    {
                        var no = 1;
                        foreach (var kose in cd.Koseler)
                            sonuc.Add(new NoktaNesnesi { X = kose.X, Y = kose.Y, Z = kose.Z, NoktaAdi = (no++).ToString() });
                    }
                    break;

                case BlokNesnesi blok:
                    sonuc = blok.Ogeler.Select(NesneDonusumleri.Klonla).ToList();
                    break;

                case OlcuNesnesi olcu:
                    sonuc = olcu.Parcala();
                    break;

                case KilavuzNesnesi kilavuz:
                    sonuc = kilavuz.Parcala();
                    break;

                case GuzergahNesnesi g:
                    sonuc = new List<CizimNesnesi>();
                    foreach (var parca in g.TumParcalar)
                    {
                        if (parca.Tip == GuzergahParcaTipi.Dogru)
                        {
                            sonuc.Add(new CizgiNesnesi { BaslangicX = parca.BaslangicX, BaslangicY = parca.BaslangicY, BitisX = parca.BitisX, BitisY = parca.BitisY });
                        }
                        else if (parca.Tip == GuzergahParcaTipi.Kurp)
                        {
                            KurpMerkezi(parca, out var mx, out var my);
                            double bas = Math.Atan2(parca.BaslangicY - my, parca.BaslangicX - mx) * 180 / Math.PI;
                            double bit = Math.Atan2(parca.BitisY - my, parca.BitisX - mx) * 180 / Math.PI;
                            var sola = parca.Yon == DonusYonu.Sol;
                            sonuc.Add(new YayNesnesi { MerkezX = mx, MerkezY = my, Yaricap = parca.YaricapBaslangic, BaslangicAcisi = sola ? bas : bit, BitisAcisi = sola ? bit : bas });
                        }
                        else
                        {
                            // Spiralin some noktası: başlangıç ve bitiş teğetlerinin kesişimi.
                            double c0 = Math.Cos(parca.BaslangicAcisi), s0 = Math.Sin(parca.BaslangicAcisi), c1 = Math.Cos(parca.BitisAcisi), s1 = Math.Sin(parca.BitisAcisi);
                            var payda = c0 * s1 - s0 * c1;
                            double t = Math.Abs(payda) < 1e-12 ? parca.Uzunluk / 2 : ((parca.BitisX - parca.BaslangicX) * s1 - (parca.BitisY - parca.BaslangicY) * c1) / payda;
                            sonuc.Add(new SpiralNesnesi
                            {
                                BaslangicX = parca.BaslangicX, BaslangicY = parca.BaslangicY, BitisX = parca.BitisX, BitisY = parca.BitisY,
                                PiX = parca.BaslangicX + t * c0, PiY = parca.BaslangicY + t * s0,
                                YaricapBaslangic = parca.YaricapBaslangic, YaricapBitis = parca.YaricapBitis, Uzunluk = parca.Uzunluk, Yon = parca.Yon
                            });
                        }
                    }
                    break;

                case GuzergahEtiketNesnesi etiketSeti:
                    sonuc = new List<CizimNesnesi>();
                    foreach (var etiket in etiketSeti.EtiketleriGetir())
                    {
                        var dikme = etiket.DikmeAcisi * Math.PI / 180;
                        double dx = Math.Cos(dikme), dy = Math.Sin(dikme), yarim = etiketSeti.YaziYuksekligi / 2;
                        if (etiketSeti.CentikGoster)
                            sonuc.Add(new CizgiNesnesi { BaslangicX = etiket.X - yarim * dx, BaslangicY = etiket.Y - yarim * dy, BitisX = etiket.X + yarim * dx, BitisY = etiket.Y + yarim * dy });
                        sonuc.Add(new YaziNesnesi
                        {
                            Metin = etiket.Metin, KonumX = etiket.YaziX, KonumY = etiket.YaziY, Yukseklik = etiket.Yukseklik, Donus = etiket.YaziAcisi,
                            HizaYatay = etiket.Ortali ? YatayHizalama.Orta : etiket.SagaDayali ? YatayHizalama.Sag : YatayHizalama.Sol,
                            HizaDikey = DikeyHizalama.Orta,
                            OzelRenk = etiketSeti.Renk.IsEmpty ? (System.Drawing.Color?)null : etiketSeti.Renk
                        });
                    }
                    break;

                case YuzeyNesnesi yuzey:
                {
                    sonuc = new List<CizimNesnesi>();
                    var kenarlar = new HashSet<long>();
                    for (var u = 0; u + 2 < yuzey.Ucgenler.Count; u += 3)
                    {
                        if (yuzey.Ucgenler[u] < 0)
                            continue;
                        for (var k = 0; k < 3; k++)
                        {
                            int a = yuzey.Ucgenler[u + k], b = yuzey.Ucgenler[u + (k + 1) % 3];
                            if (!kenarlar.Add(((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b)))
                                continue;
                            sonuc.Add(new CizgiNesnesi { BaslangicX = yuzey.X[a], BaslangicY = yuzey.Y[a], BaslangicZ = yuzey.Z[a], BitisX = yuzey.X[b], BitisY = yuzey.Y[b], BitisZ = yuzey.Z[b] });
                        }
                    }
                    for (var i = 0; i < yuzey.X.Count; i++)
                    {
                        if (yuzey.Nolar[i] != null)
                            sonuc.Add(new NoktaNesnesi { X = yuzey.X[i], Y = yuzey.Y[i], Z = yuzey.Z[i], NoktaAdi = yuzey.Nolar[i] });
                    }
                    break;
                }

                default:
                    return null;
            }

            // Parçalar ana objenin tabakasında ve renginde kalır (kendi tabakası/rengi olanlar hariç).
            foreach (var parca in sonuc)
            {
                if (parca.Tabaka == null)
                    parca.Tabaka = nesne.Tabaka;
                if (parca.OzelRenk == null)
                    parca.OzelRenk = nesne.OzelRenk;
                if (parca.CizgiKalinligi <= 0)
                    parca.CizgiKalinligi = nesne.CizgiKalinligi;
            }
            return sonuc;
        }

        // ------------------------------------------------------------------ uzat

        /// <summary>
        /// Çizgiyi (ya da açık çoklu doğrunun düz uç parçasını) sınır objesine kadar uzatır: sınıra daha yakın olan
        /// uç, uzantısının sınırı kestiği ilk yere taşınır. Uzatılamıyorsa false döner.
        /// </summary>
        public static bool Uzat(CizimNesnesi nesne, List<Primitif> sinir)
        {
            if (nesne is CizgiNesnesi c)
            {
                if (!UzatmaBul(c.BaslangicX, c.BaslangicY, c.BitisX, c.BitisY, sinir, out var basaUzat, out var t))
                    return false;
                double x = c.BaslangicX + t * (c.BitisX - c.BaslangicX), y = c.BaslangicY + t * (c.BitisY - c.BaslangicY);
                if (basaUzat)
                {
                    c.BaslangicZ += t * (c.BitisZ - c.BaslangicZ);
                    c.BaslangicX = x; c.BaslangicY = y;
                }
                else
                {
                    c.BitisZ = c.BaslangicZ + t * (c.BitisZ - c.BaslangicZ);
                    c.BitisX = x; c.BitisY = y;
                }
                return true;
            }

            if (nesne is CokluDogruNesnesi cd && !cd.Kapali && cd.Koseler.Count >= 2)
            {
                // İki uç parça ayrı ayrı denenir; sınıra en az uzatmayla ulaşan uç seçilir.
                CokluDogruKosesi bas0 = cd.Koseler[0], bas1 = cd.Koseler[1], son0 = cd.Koseler[cd.Koseler.Count - 1], son1 = cd.Koseler[cd.Koseler.Count - 2];
                double basMesafe = double.MaxValue, sonMesafe = double.MaxValue, basT = 0, sonT = 0;
                if (Math.Abs(bas0.Bulge) < 1e-9 && UzatmaBul(bas1.X, bas1.Y, bas0.X, bas0.Y, sinir, out var basaDogru, out var t1) && !basaDogru)
                {
                    basT = t1;
                    basMesafe = (t1 - 1) * Math.Sqrt(Math.Pow(bas0.X - bas1.X, 2) + Math.Pow(bas0.Y - bas1.Y, 2));
                }
                if (Math.Abs(son1.Bulge) < 1e-9 && UzatmaBul(son1.X, son1.Y, son0.X, son0.Y, sinir, out var sonaDogru, out var t2) && !sonaDogru)
                {
                    sonT = t2;
                    sonMesafe = (t2 - 1) * Math.Sqrt(Math.Pow(son0.X - son1.X, 2) + Math.Pow(son0.Y - son1.Y, 2));
                }
                if (basMesafe == double.MaxValue && sonMesafe == double.MaxValue)
                    return false;
                if (basMesafe <= sonMesafe)
                {
                    double nx = bas1.X + basT * (bas0.X - bas1.X), ny = bas1.Y + basT * (bas0.Y - bas1.Y);
                    bas0.X = nx; bas0.Y = ny;
                }
                else
                {
                    double nx = son1.X + sonT * (son0.X - son1.X), ny = son1.Y + sonT * (son0.Y - son1.Y);
                    son0.X = nx; son0.Y = ny;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Objeyi sınıra kadar uzatır; tıklanan yere yakın olan uç uzatılır (o uç sınıra ulaşamıyorsa öbür uç denenir).
        /// Çizgide ve açık çoklu doğrunun düz uç parçalarında çalışır.
        /// </summary>
        public static bool Uzat(CizimNesnesi nesne, List<Primitif> sinir, double tikX, double tikY)
        {
            double Uzaklik(double x, double y) => (x - tikX) * (x - tikX) + (y - tikY) * (y - tikY);

            if (nesne is CizgiNesnesi c)
            {
                UcAdaylari(c.BaslangicX, c.BaslangicY, c.BitisX, c.BitisY, sinir, out var tBas, out var tSon);
                var basYakin = Uzaklik(c.BaslangicX, c.BaslangicY) <= Uzaklik(c.BitisX, c.BitisY);
                bool basaUzat;
                if (tBas != null && (basYakin || tSon == null)) basaUzat = true;
                else if (tSon != null) basaUzat = false;
                else return false;

                var t = basaUzat ? tBas.Value : tSon.Value;
                double x = c.BaslangicX + t * (c.BitisX - c.BaslangicX), y = c.BaslangicY + t * (c.BitisY - c.BaslangicY);
                var z = c.BaslangicZ + t * (c.BitisZ - c.BaslangicZ);
                if (basaUzat) { c.BaslangicX = x; c.BaslangicY = y; c.BaslangicZ = z; }
                else { c.BitisX = x; c.BitisY = y; c.BitisZ = z; }
                return true;
            }

            if (nesne is CokluDogruNesnesi cd && !cd.Kapali && cd.Koseler.Count >= 2)
            {
                CokluDogruKosesi bas0 = cd.Koseler[0], bas1 = cd.Koseler[1], son0 = cd.Koseler[cd.Koseler.Count - 1], son1 = cd.Koseler[cd.Koseler.Count - 2];
                double? basT = null, sonT = null;
                if (Math.Abs(bas0.Bulge) < 1e-9)
                {
                    UcAdaylari(bas1.X, bas1.Y, bas0.X, bas0.Y, sinir, out _, out basT);
                }
                if (Math.Abs(son1.Bulge) < 1e-9)
                {
                    UcAdaylari(son1.X, son1.Y, son0.X, son0.Y, sinir, out _, out sonT);
                }
                var basYakin = Uzaklik(bas0.X, bas0.Y) <= Uzaklik(son0.X, son0.Y);
                if (basT != null && (basYakin || sonT == null))
                {
                    double nx = bas1.X + basT.Value * (bas0.X - bas1.X), ny = bas1.Y + basT.Value * (bas0.Y - bas1.Y);
                    bas0.X = nx; bas0.Y = ny;
                    return true;
                }
                if (sonT != null)
                {
                    double nx = son1.X + sonT.Value * (son0.X - son1.X), ny = son1.Y + sonT.Value * (son0.Y - son1.Y);
                    son0.X = nx; son0.Y = ny;
                    return true;
                }
            }
            return false;
        }

        // A-B doğrusunun sınırı A'nın gerisinde (t < 0) ve B'nin ilerisinde (t > 1) kestiği en yakın yerler.
        private static void UcAdaylari(double ax, double ay, double bx, double by, List<Primitif> sinir, out double? tBas, out double? tSon)
        {
            tBas = tSon = null;
            foreach (var p in sinir)
            {
                foreach (var k in DogruKesisimleri(ax, ay, bx, by, p))
                {
                    if (k < -1e-9 && (tBas == null || k > tBas)) tBas = k;
                    if (k > 1 + 1e-9 && (tSon == null || k < tSon)) tSon = k;
                }
            }
        }

        // A-B doğrusunun sınırla kesiştiği, parçanın dışındaki en yakın yer: basaUzat true ise A tarafında (t < 0),
        // değilse B tarafında (t > 1). İki taraf da mümkünse uzatma mesafesi kısa olan seçilir.
        private static bool UzatmaBul(double ax, double ay, double bx, double by, List<Primitif> sinir, out bool basaUzat, out double t)
        {
            basaUzat = false;
            t = 0;
            double? enIyiBas = null, enIyiSon = null;
            foreach (var p in sinir)
            {
                foreach (var k in DogruKesisimleri(ax, ay, bx, by, p))
                {
                    if (k < -1e-9 && (enIyiBas == null || k > enIyiBas)) enIyiBas = k;
                    if (k > 1 + 1e-9 && (enIyiSon == null || k < enIyiSon)) enIyiSon = k;
                }
            }
            if (enIyiBas == null && enIyiSon == null)
                return false;
            if (enIyiSon == null || (enIyiBas != null && -enIyiBas.Value < enIyiSon.Value - 1))
            {
                basaUzat = true;
                t = enIyiBas.Value;
            }
            else
            {
                t = enIyiSon.Value;
            }
            return true;
        }

        // ------------------------------------------------------------------ paralel

        /// <summary>
        /// Objenin tıklanan tarafına, verilen mesafede paralel kopyasını üretir (çizgi, daire, yay, çoklu doğru,
        /// güzergah). Güzergahın paraleli çoklu doğru olur. Üretilemiyorsa null döner.
        /// </summary>
        public static CizimNesnesi Paralel(CizimNesnesi nesne, double mesafe, double tarafX, double tarafY)
        {
            CizimNesnesi sonuc = null;
            switch (nesne)
            {
                case CizgiNesnesi c:
                {
                    double dx = c.BitisX - c.BaslangicX, dy = c.BitisY - c.BaslangicY, boy = Math.Sqrt(dx * dx + dy * dy);
                    if (boy < 1e-12)
                        return null;
                    var taraf = Math.Sign(dx * (tarafY - c.BaslangicY) - dy * (tarafX - c.BaslangicX));
                    if (taraf == 0)
                        return null;
                    double ox = -dy / boy * mesafe * taraf, oy = dx / boy * mesafe * taraf;
                    sonuc = new CizgiNesnesi
                    {
                        BaslangicX = c.BaslangicX + ox, BaslangicY = c.BaslangicY + oy, BaslangicZ = c.BaslangicZ,
                        BitisX = c.BitisX + ox, BitisY = c.BitisY + oy, BitisZ = c.BitisZ
                    };
                    break;
                }
                case DaireNesnesi d:
                {
                    var disarida = Math.Sqrt(Math.Pow(tarafX - d.MerkezX, 2) + Math.Pow(tarafY - d.MerkezY, 2)) > d.Yaricap;
                    var r = d.Yaricap + (disarida ? mesafe : -mesafe);
                    if (r <= 1e-9)
                        return null;
                    sonuc = new DaireNesnesi { MerkezX = d.MerkezX, MerkezY = d.MerkezY, MerkezZ = d.MerkezZ, Yaricap = r };
                    break;
                }
                case YayNesnesi a:
                {
                    var disarida = Math.Sqrt(Math.Pow(tarafX - a.MerkezX, 2) + Math.Pow(tarafY - a.MerkezY, 2)) > a.Yaricap;
                    var r = a.Yaricap + (disarida ? mesafe : -mesafe);
                    if (r <= 1e-9)
                        return null;
                    sonuc = new YayNesnesi { MerkezX = a.MerkezX, MerkezY = a.MerkezY, MerkezZ = a.MerkezZ, Yaricap = r, BaslangicAcisi = a.BaslangicAcisi, BitisAcisi = a.BitisAcisi };
                    break;
                }
                case CokluDogruNesnesi cd:
                    sonuc = CokluDogruParaleli(cd, mesafe, tarafX, tarafY);
                    break;
                case GuzergahNesnesi g:
                {
                    var cd = GuzergahiCokluDogruyaCevir(g);
                    sonuc = cd != null ? CokluDogruParaleli(cd, mesafe, tarafX, tarafY) : null;
                    break;
                }
            }

            if (sonuc != null)
            {
                sonuc.Tabaka = nesne.Tabaka;
                sonuc.OzelRenk = nesne.OzelRenk;
                sonuc.CizgiKalinligi = nesne.CizgiKalinligi;
            }
            return sonuc;
        }

        /// <summary>Güzergahı çoklu doğruya çevirir: doğru ve kurplar tam, spiraller kısa doğru parçalarıyla.</summary>
        public static CokluDogruNesnesi GuzergahiCokluDogruyaCevir(GuzergahNesnesi g)
        {
            if (g.Parcalar.Count == 0)
                return null;
            var cd = new CokluDogruNesnesi();
            foreach (var parca in g.Parcalar)
            {
                if (parca.Tip == GuzergahParcaTipi.Spiral)
                {
                    for (var i = 0; i + 1 < parca.OrnekX.Length; i++)
                        cd.Koseler.Add(new CokluDogruKosesi { X = parca.OrnekX[i], Y = parca.OrnekY[i] });
                }
                else
                {
                    var bulge = parca.Tip == GuzergahParcaTipi.Kurp ? (parca.Yon == DonusYonu.Sol ? 1 : -1) * Math.Tan(parca.Uzunluk / parca.YaricapBaslangic / 4) : 0;
                    cd.Koseler.Add(new CokluDogruKosesi { X = parca.BaslangicX, Y = parca.BaslangicY, Bulge = bulge });
                }
            }
            var sonParca = g.Parcalar[g.Parcalar.Count - 1];
            cd.Koseler.Add(new CokluDogruKosesi { X = sonParca.BitisX, Y = sonParca.BitisY });
            return cd;
        }

        private static CokluDogruNesnesi CokluDogruParaleli(CokluDogruNesnesi cd, double mesafe, double tarafX, double tarafY)
        {
            var parcaSayisi = cd.ParcaSayisi;
            if (parcaSayisi == 0)
                return null;

            // Taraf, tıklanan yere en yakın parçanın gidiş yönüne göre belirlenir (sol: +1, sağ: -1).
            var enYakin = 0;
            var enKucuk = double.MaxValue;
            for (var i = 0; i < parcaSayisi; i++)
            {
                cd.ParcaUclari(i, out var p, out var q);
                var noktalar = cd.ParcaNoktalari(i, 64);
                for (var k = 0; k + 1 < noktalar.Count; k++)
                {
                    var u = NoktaParcaUzakligi(tarafX, tarafY, noktalar[k][0], noktalar[k][1], noktalar[k + 1][0], noktalar[k + 1][1]);
                    if (u < enKucuk)
                    {
                        enKucuk = u;
                        enYakin = i;
                    }
                }
            }
            double taraf;
            {
                var noktalar = cd.ParcaNoktalari(enYakin, 64);
                var k = 0;
                var enK = double.MaxValue;
                for (var j = 0; j + 1 < noktalar.Count; j++)
                {
                    var u = NoktaParcaUzakligi(tarafX, tarafY, noktalar[j][0], noktalar[j][1], noktalar[j + 1][0], noktalar[j + 1][1]);
                    if (u < enK) { enK = u; k = j; }
                }
                double dx = noktalar[k + 1][0] - noktalar[k][0], dy = noktalar[k + 1][1] - noktalar[k][1];
                taraf = Math.Sign(dx * (tarafY - noktalar[k][1]) - dy * (tarafX - noktalar[k][0]));
                if (taraf == 0)
                    return null;
            }
            var kayma = mesafe * taraf;

            // Her parçanın kaydırılmış başlangıç ve bitişi; düz parçalar aynı doğrultuda, yaylar aynı merkezli.
            var baslar = new double[parcaSayisi][];
            var bitisler = new double[parcaSayisi][];
            var bulgeler = new double[parcaSayisi];
            for (var i = 0; i < parcaSayisi; i++)
            {
                cd.ParcaUclari(i, out var p, out var q);
                bulgeler[i] = p.Bulge;
                if (Math.Abs(p.Bulge) < 1e-9)
                {
                    double dx = q.X - p.X, dy = q.Y - p.Y, boy = Math.Sqrt(dx * dx + dy * dy);
                    if (boy < 1e-12)
                        return null;
                    double ox = -dy / boy * kayma, oy = dx / boy * kayma;
                    baslar[i] = new[] { p.X + ox, p.Y + oy, p.Z };
                    bitisler[i] = new[] { q.X + ox, q.Y + oy, q.Z };
                }
                else
                {
                    CokluDogruNesnesi.YayBilgisi(p, q, out var mx, out var my, out var r, out var bas, out var sup);
                    // Saat yönünün tersine dönen yayda gidişin solu merkez tarafıdır.
                    var yeniR = r - kayma * Math.Sign(sup);
                    if (yeniR <= 1e-9)
                        return null;
                    baslar[i] = new[] { mx + yeniR * Math.Cos(bas), my + yeniR * Math.Sin(bas), p.Z };
                    bitisler[i] = new[] { mx + yeniR * Math.Cos(bas + sup), my + yeniR * Math.Sin(bas + sup), q.Z };
                }
            }

            // Birleşimler: iki düz parça birbirine uzatılır (köşe); yay içeren birleşimlerde uçlar teğetse zaten çakışır,
            // çakışmıyorsa araya düz bir parça girer.
            var sonuc = new CokluDogruNesnesi { Kapali = cd.Kapali };
            void KoseEkle(double[] nokta, double bulge) => sonuc.Koseler.Add(new CokluDogruKosesi { X = nokta[0], Y = nokta[1], Z = nokta[2], Bulge = bulge });

            double[] Birlesim(int i, int j, out double[] ara)
            {
                ara = null;
                if (Math.Abs(bulgeler[i]) < 1e-9 && Math.Abs(bulgeler[j]) < 1e-9)
                {
                    double ax = baslar[i][0], ay = baslar[i][1], bx = bitisler[i][0], by = bitisler[i][1];
                    double cx = baslar[j][0], cy = baslar[j][1], ex = bitisler[j][0], ey = bitisler[j][1];
                    double rx = bx - ax, ry = by - ay, qx = ex - cx, qy = ey - cy, payda = rx * qy - ry * qx;
                    if (Math.Abs(payda) > 1e-12)
                    {
                        var t = ((cx - ax) * qy - (cy - ay) * qx) / payda;
                        return new[] { ax + t * rx, ay + t * ry, bitisler[i][2] };
                    }
                    return bitisler[i];
                }
                if (Math.Abs(bitisler[i][0] - baslar[j][0]) < 1e-6 && Math.Abs(bitisler[i][1] - baslar[j][1]) < 1e-6)
                    return bitisler[i];
                ara = baslar[j];
                return bitisler[i];
            }

            if (cd.Kapali)
            {
                // Her parçanın başı, bir önceki parçayla birleşim noktasıdır.
                for (var j = 0; j < parcaSayisi; j++)
                {
                    var b = Birlesim((j - 1 + parcaSayisi) % parcaSayisi, j, out var ara);
                    if (ara != null)
                    {
                        KoseEkle(b, 0);
                        KoseEkle(ara, bulgeler[j]);
                    }
                    else
                    {
                        KoseEkle(b, bulgeler[j]);
                    }
                }
            }
            else
            {
                KoseEkle(baslar[0], bulgeler[0]);
                for (var i = 0; i < parcaSayisi - 1; i++)
                {
                    var b = Birlesim(i, i + 1, out var ara);
                    if (ara != null)
                    {
                        KoseEkle(b, 0);
                        KoseEkle(ara, bulgeler[i + 1]);
                    }
                    else
                    {
                        KoseEkle(b, bulgeler[i + 1]);
                    }
                }
                KoseEkle(bitisler[parcaSayisi - 1], 0);
            }
            return sonuc;
        }

        // ------------------------------------------------------------------ çoklu doğruya birleştirme

        /// <summary>
        /// Uç uca bağlı çizgi, yay ve açık çoklu doğruları zincirler halinde çoklu doğrulara birleştirir (uçlar 1 mm
        /// içinde çakışmalı). Birleşen objeler <paramref name="kullanilanlar"/> ile verilir; tek parçalık zincirler de
        /// çoklu doğruya çevrilir.
        /// </summary>
        public static List<CokluDogruNesnesi> CokluDogruyaBirlestir(List<CizimNesnesi> nesneler, out List<CizimNesnesi> kullanilanlar)
        {
            const double tolerans = 1e-3;
            kullanilanlar = new List<CizimNesnesi>();
            var parcalar = new List<KeyValuePair<CizimNesnesi, List<CokluDogruKosesi>>>();
            foreach (var nesne in nesneler)
            {
                var koseler = KoseListesi(nesne);
                if (koseler != null && koseler.Count >= 2)
                    parcalar.Add(new KeyValuePair<CizimNesnesi, List<CokluDogruKosesi>>(nesne, koseler));
            }

            bool Ayni(CokluDogruKosesi a, CokluDogruKosesi b) => Math.Abs(a.X - b.X) <= tolerans && Math.Abs(a.Y - b.Y) <= tolerans;

            var kullanildi = new bool[parcalar.Count];
            var sonuc = new List<CokluDogruNesnesi>();
            for (var s = 0; s < parcalar.Count; s++)
            {
                if (kullanildi[s])
                    continue;
                kullanildi[s] = true;
                var zincir = parcalar[s].Value.Select(k => k.Kopyala()).ToList();
                var ilkNesne = parcalar[s].Key;
                kullanilanlar.Add(ilkNesne);

                var buldu = true;
                while (buldu)
                {
                    buldu = false;
                    for (var i = 0; i < parcalar.Count; i++)
                    {
                        if (kullanildi[i])
                            continue;
                        var aday = parcalar[i].Value;
                        List<CokluDogruKosesi> eklenecek = null;
                        var sonaEkle = true;
                        if (Ayni(zincir[zincir.Count - 1], aday[0])) eklenecek = aday;
                        else if (Ayni(zincir[zincir.Count - 1], aday[aday.Count - 1])) eklenecek = TersCevir(aday);
                        else if (Ayni(zincir[0], aday[aday.Count - 1])) { eklenecek = aday; sonaEkle = false; }
                        else if (Ayni(zincir[0], aday[0])) { eklenecek = TersCevir(aday); sonaEkle = false; }
                        if (eklenecek == null)
                            continue;

                        var kopya = eklenecek.Select(k => k.Kopyala()).ToList();
                        if (sonaEkle)
                        {
                            // Birleşim köşesinin bulge'ı eklenen parçanın ilk parçasınındır.
                            zincir[zincir.Count - 1].Bulge = kopya[0].Bulge;
                            zincir.AddRange(kopya.Skip(1));
                        }
                        else
                        {
                            kopya.RemoveAt(kopya.Count - 1);
                            zincir.InsertRange(0, kopya);
                        }
                        kullanildi[i] = true;
                        kullanilanlar.Add(parcalar[i].Key);
                        buldu = true;
                    }
                }

                var cd = new CokluDogruNesnesi { Tabaka = ilkNesne.Tabaka, OzelRenk = ilkNesne.OzelRenk };
                if (zincir.Count >= 3 && Ayni(zincir[0], zincir[zincir.Count - 1]))
                {
                    zincir.RemoveAt(zincir.Count - 1);
                    cd.Kapali = true;
                }
                zincir[zincir.Count - 1].Bulge = cd.Kapali ? zincir[zincir.Count - 1].Bulge : 0;
                cd.Koseler.AddRange(zincir);
                sonuc.Add(cd);
            }
            return sonuc;
        }

        /// <summary>Çoklu doğrunun yönünü ters çevirir (köşeler tersine dizilir, yay parçalarının bulge'ı işaret değiştirir).</summary>
        public static void CokluDogruyuTersCevir(CokluDogruNesnesi cd)
        {
            var koseler = cd.Koseler.ToList();
            if (cd.Kapali && koseler.Count > 1)
            {
                // Kapalıda kapanış parçası da yer değiştirir: ilk köşe başa sabitlenip geri kalanı ters çevrilir.
                koseler.Add(koseler[0].Kopyala());
                var ters = TersCevir(koseler);
                ters.RemoveAt(ters.Count - 1);
                cd.Koseler.Clear();
                cd.Koseler.AddRange(ters);
                return;
            }
            cd.Koseler.Clear();
            cd.Koseler.AddRange(TersCevir(koseler));
        }

        /// <summary>
        /// Çoklu doğruyu güzergaha çevirir: düz parçalar doğru, yay parçaları kurp olur. Teğet olmayan birleşimler
        /// güzergahta kırıklık (kurpsuz some) olarak kalır. Güzergahın önceki parçaları silinir.
        /// </summary>
        public static void CokluDogruyuGuzergahaCevir(CokluDogruNesnesi cd, GuzergahNesnesi g)
        {
            var koseler = cd.Koseler.ToList();
            if (cd.Kapali && koseler.Count > 1)
                koseler.Add(new CokluDogruKosesi { X = koseler[0].X, Y = koseler[0].Y, Z = koseler[0].Z });

            var parcalar = new List<GuzergahParcasi>();
            double? oncekiBitisYonu = null;
            for (var i = 0; i + 1 < koseler.Count; i++)
            {
                CokluDogruKosesi a = koseler[i], b = koseler[i + 1];
                double dx = b.X - a.X, dy = b.Y - a.Y, kiris = Math.Sqrt(dx * dx + dy * dy);
                if (kiris < 1e-9)
                    continue;
                var kirisYonu = Math.Atan2(dy, dx);
                GuzergahParcasi parca;
                double baslangicYonu, bitisYonu;
                if (Math.Abs(a.Bulge) < 1e-9)
                {
                    parca = new GuzergahParcasi { Tip = GuzergahParcaTipi.Dogru, Uzunluk = kiris };
                    baslangicYonu = bitisYonu = kirisYonu;
                }
                else
                {
                    var aci = 4 * Math.Atan(a.Bulge);
                    var r = kiris * (1 + a.Bulge * a.Bulge) / (4 * Math.Abs(a.Bulge));
                    parca = new GuzergahParcasi
                    {
                        Tip = GuzergahParcaTipi.Kurp, Uzunluk = r * Math.Abs(aci), YaricapBaslangic = r, YaricapBitis = r,
                        Yon = aci > 0 ? DonusYonu.Sol : DonusYonu.Sag
                    };
                    baslangicYonu = kirisYonu - aci / 2;
                    bitisYonu = kirisYonu + aci / 2;
                }

                if (oncekiBitisYonu == null)
                {
                    g.BaslangicX = a.X;
                    g.BaslangicY = a.Y;
                    g.BaslangicAcisi = baslangicYonu;
                }
                else
                {
                    var kirik = baslangicYonu - oncekiBitisYonu.Value;
                    while (kirik > Math.PI) kirik -= 2 * Math.PI;
                    while (kirik <= -Math.PI) kirik += 2 * Math.PI;
                    parca.KirikAcisi = Math.Abs(kirik) < 1e-9 ? 0 : kirik;
                }
                oncekiBitisYonu = bitisYonu;
                parcalar.Add(parca);
            }

            g.Parcalar.Clear();
            g.Parcalar.AddRange(parcalar);
            g.Hesapla();
        }

        // Objenin çoklu doğru köşeleri (yön: çizgide baştan sona, yayda saat yönünün tersine).
        private static List<CokluDogruKosesi> KoseListesi(CizimNesnesi nesne)
        {
            switch (nesne)
            {
                case CizgiNesnesi c:
                    return new List<CokluDogruKosesi>
                    {
                        new CokluDogruKosesi { X = c.BaslangicX, Y = c.BaslangicY, Z = c.BaslangicZ },
                        new CokluDogruKosesi { X = c.BitisX, Y = c.BitisY, Z = c.BitisZ }
                    };
                case YayNesnesi a:
                {
                    var p = Primitifler(a)[0];
                    return new List<CokluDogruKosesi>
                    {
                        new CokluDogruKosesi { X = p.X1, Y = p.Y1, Z = a.MerkezZ, Bulge = Math.Tan(p.Supurme / 4) },
                        new CokluDogruKosesi { X = p.X2, Y = p.Y2, Z = a.MerkezZ }
                    };
                }
                case CokluDogruNesnesi cd when !cd.Kapali:
                    return cd.Koseler.Select(k => k.Kopyala()).ToList();
                default:
                    return null;
            }
        }

        // Köşe dizisini ters çevirir; her parçanın bulge'ı yön değiştirdiği için işaret değiştirir.
        private static List<CokluDogruKosesi> TersCevir(List<CokluDogruKosesi> koseler)
        {
            var sonuc = new List<CokluDogruKosesi>();
            for (var i = koseler.Count - 1; i >= 0; i--)
            {
                var kose = koseler[i].Kopyala();
                kose.Bulge = i > 0 ? -koseler[i - 1].Bulge : 0;
                sonuc.Add(kose);
            }
            return sonuc;
        }
    }
}
