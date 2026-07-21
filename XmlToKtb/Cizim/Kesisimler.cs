using System;
using System.Collections.Generic;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Çizgi/yay/daire kesişim hesapları ve "Kes" (kırpma/trim) komutunun geometrisi.</summary>
    public static class Kesisimler
    {
        public static List<PointF> Bul(CizimNesnesi a, CizimNesnesi b)
        {
            if (a is CizgiNesnesi ca && b is CizgiNesnesi cb) return DogruDogru(ca, cb);
            if (a is CizgiNesnesi ca2 && b is YayNesnesi yb) return DogruYay(ca2, yb);
            if (a is YayNesnesi ya && b is CizgiNesnesi cb2) return DogruYay(cb2, ya);
            if (a is YayNesnesi ya2 && b is YayNesnesi yb2) return YayYay(ya2, yb2);
            if (a is CizgiNesnesi ca3 && b is DaireNesnesi db) return DogruDaire(ca3, db);
            if (a is DaireNesnesi da && b is CizgiNesnesi cb3) return DogruDaire(cb3, da);
            if (a is DaireNesnesi da2 && b is DaireNesnesi db2) return DaireDaire(da2, db2);
            if (a is DaireNesnesi da3 && b is YayNesnesi yb3) return DaireYay(da3, yb3);
            if (a is YayNesnesi ya3 && b is DaireNesnesi db3) return DaireYay(db3, ya3);
            return new List<PointF>();
        }

        /// <summary>Hedef objeyi (Çizgi/Yay/Daire), diğer tüm objelerle kesişimlerine göre tıklanan noktadan itibaren kırpar.
        /// Sonuç, hedefin yerine konacak parça(lar)dır; kesişim yoksa null döner (kırpılacak bir şey yok).</summary>
        public static List<CizimNesnesi> Kirp(CizimNesnesi hedef, PointF tiklamaNoktasi, IEnumerable<CizimNesnesi> tumNesneler)
        {
            if (hedef is CizgiNesnesi cizgi)
                return KirpCizgi(cizgi, tiklamaNoktasi, tumNesneler);
            if (hedef is YayNesnesi yay)
                return KirpYay(yay, tiklamaNoktasi, tumNesneler);
            if (hedef is DaireNesnesi daire)
                return KirpDaire(daire, tiklamaNoktasi, tumNesneler);
            return null;
        }

        private static List<CizimNesnesi> KirpCizgi(CizgiNesnesi hedef, PointF tiklamaNoktasi, IEnumerable<CizimNesnesi> tumNesneler)
        {
            var x1 = hedef.BaslangicX;
            var y1 = hedef.BaslangicY;
            var dx = hedef.BitisX - x1;
            var dy = hedef.BitisY - y1;
            var uzunlukKare = dx * dx + dy * dy;
            if (uzunlukKare < 1e-12)
                return null;

            var tListesi = new List<double>();
            foreach (var diger in tumNesneler)
            {
                if (ReferenceEquals(diger, hedef))
                    continue;

                foreach (var nokta in Bul(hedef, diger))
                {
                    var t = ((nokta.X - x1) * dx + (nokta.Y - y1) * dy) / uzunlukKare;
                    if (t > 1e-6 && t < 1 - 1e-6)
                        tListesi.Add(t);
                }
            }

            if (tListesi.Count == 0)
                return null;

            var tiklamaT = ((tiklamaNoktasi.X - x1) * dx + (tiklamaNoktasi.Y - y1) * dy) / uzunlukKare;
            tiklamaT = Math.Max(0, Math.Min(1, tiklamaT));

            var altSinir = 0.0;
            var ustSinir = 1.0;
            foreach (var t in tListesi)
            {
                if (t <= tiklamaT && t > altSinir) altSinir = t;
                if (t >= tiklamaT && t < ustSinir) ustSinir = t;
            }

            var sonuc = new List<CizimNesnesi>();
            if (altSinir > 1e-6)
                sonuc.Add(new CizgiNesnesi { BaslangicX = x1, BaslangicY = y1, BitisX = x1 + altSinir * dx, BitisY = y1 + altSinir * dy });
            if (ustSinir < 1 - 1e-6)
                sonuc.Add(new CizgiNesnesi { BaslangicX = x1 + ustSinir * dx, BaslangicY = y1 + ustSinir * dy, BitisX = hedef.BitisX, BitisY = hedef.BitisY });

            return sonuc;
        }

        private static List<CizimNesnesi> KirpYay(YayNesnesi hedef, PointF tiklamaNoktasi, IEnumerable<CizimNesnesi> tumNesneler)
        {
            var supurme = NormalizeSupurme(hedef.BitisAcisi - hedef.BaslangicAcisi);

            double AciParametresi(double x, double y)
            {
                var aci = NormalizeAci(Math.Atan2(y - hedef.MerkezY, x - hedef.MerkezX) * 180.0 / Math.PI);
                return NormalizeAci(aci - hedef.BaslangicAcisi);
            }

            var aListesi = new List<double>();
            foreach (var diger in tumNesneler)
            {
                if (ReferenceEquals(diger, hedef))
                    continue;

                foreach (var nokta in Bul(hedef, diger))
                {
                    var a = AciParametresi(nokta.X, nokta.Y);
                    if (a > 1e-4 && a < supurme - 1e-4)
                        aListesi.Add(a);
                }
            }

            if (aListesi.Count == 0)
                return null;

            var tiklamaA = Math.Max(0, Math.Min(supurme, AciParametresi(tiklamaNoktasi.X, tiklamaNoktasi.Y)));

            var altSinir = 0.0;
            var ustSinir = supurme;
            foreach (var a in aListesi)
            {
                if (a <= tiklamaA && a > altSinir) altSinir = a;
                if (a >= tiklamaA && a < ustSinir) ustSinir = a;
            }

            var sonuc = new List<CizimNesnesi>();
            if (altSinir > 1e-4)
                sonuc.Add(new YayNesnesi { MerkezX = hedef.MerkezX, MerkezY = hedef.MerkezY, Yaricap = hedef.Yaricap, BaslangicAcisi = hedef.BaslangicAcisi, BitisAcisi = hedef.BaslangicAcisi + altSinir });
            if (ustSinir < supurme - 1e-4)
                sonuc.Add(new YayNesnesi { MerkezX = hedef.MerkezX, MerkezY = hedef.MerkezY, Yaricap = hedef.Yaricap, BaslangicAcisi = hedef.BaslangicAcisi + ustSinir, BitisAcisi = hedef.BitisAcisi });

            return sonuc;
        }

        /// <summary>Tam bir daireyi, diğer objelerle kesişim noktalarına göre parçalara ayırıp tıklanan parçayı siler.
        /// En az 2 farklı kesişim noktası olmadan (tek nokta veya hiç) anlamlı bir kırpma yapılamaz.</summary>
        private static List<CizimNesnesi> KirpDaire(DaireNesnesi hedef, PointF tiklamaNoktasi, IEnumerable<CizimNesnesi> tumNesneler)
        {
            double AciHesapla(double x, double y)
            {
                return NormalizeAci(Math.Atan2(y - hedef.MerkezY, x - hedef.MerkezX) * 180.0 / Math.PI);
            }

            var acilar = new List<double>();
            foreach (var diger in tumNesneler)
            {
                if (ReferenceEquals(diger, hedef))
                    continue;

                foreach (var nokta in Bul(hedef, diger))
                {
                    var aci = AciHesapla(nokta.X, nokta.Y);
                    if (!acilar.Exists(mevcut => AcisalFarkKucukMu(mevcut, aci)))
                        acilar.Add(aci);
                }
            }

            if (acilar.Count < 2)
                return null;

            acilar.Sort();

            var tiklamaAci = AciHesapla(tiklamaNoktasi.X, tiklamaNoktasi.Y);
            var n = acilar.Count;
            var silinecekIndex = 0;
            for (var i = 0; i < n; i++)
            {
                var bas = acilar[i];
                var bit = acilar[(i + 1) % n];
                var supurme = NormalizeSupurme(bit - bas);
                var goreli = NormalizeAci(tiklamaAci - bas);
                if (goreli <= supurme + 1e-6)
                {
                    silinecekIndex = i;
                    break;
                }
            }

            var sonuc = new List<CizimNesnesi>();
            for (var i = 0; i < n; i++)
            {
                if (i == silinecekIndex)
                    continue;

                var bas = acilar[i];
                var bit = acilar[(i + 1) % n];
                sonuc.Add(new YayNesnesi { MerkezX = hedef.MerkezX, MerkezY = hedef.MerkezY, Yaricap = hedef.Yaricap, BaslangicAcisi = bas, BitisAcisi = bit });
            }

            return sonuc;
        }

        private static bool AcisalFarkKucukMu(double aci1, double aci2)
        {
            var fark = NormalizeAci(aci1 - aci2);
            return fark < 1e-6 || fark > 360 - 1e-6;
        }

        private static List<PointF> DogruDogru(CizgiNesnesi a, CizgiNesnesi b)
        {
            double x1 = a.BaslangicX, y1 = a.BaslangicY, x2 = a.BitisX, y2 = a.BitisY;
            double x3 = b.BaslangicX, y3 = b.BaslangicY, x4 = b.BitisX, y4 = b.BitisY;

            var payda = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(payda) < 1e-9)
                return new List<PointF>();

            var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / payda;
            var u = ((x1 - x3) * (y1 - y2) - (y1 - y3) * (x1 - x2)) / payda;

            if (t < -1e-9 || t > 1 + 1e-9 || u < -1e-9 || u > 1 + 1e-9)
                return new List<PointF>();

            var ix = x1 + t * (x2 - x1);
            var iy = y1 + t * (y2 - y1);
            return new List<PointF> { new PointF((float)ix, (float)iy) };
        }

        private static List<PointF> DogruYay(CizgiNesnesi dogru, YayNesnesi yay)
        {
            return DogruDaireGenel(dogru, yay.MerkezX, yay.MerkezY, yay.Yaricap, (x, y) => AciAraligindaMi(yay, x, y));
        }

        private static List<PointF> DogruDaire(CizgiNesnesi dogru, DaireNesnesi daire)
        {
            return DogruDaireGenel(dogru, daire.MerkezX, daire.MerkezY, daire.Yaricap, null);
        }

        private static List<PointF> YayYay(YayNesnesi a, YayNesnesi b)
        {
            return DaireDaireGenel(
                a.MerkezX, a.MerkezY, a.Yaricap, (x, y) => AciAraligindaMi(a, x, y),
                b.MerkezX, b.MerkezY, b.Yaricap, (x, y) => AciAraligindaMi(b, x, y));
        }

        private static List<PointF> DaireDaire(DaireNesnesi a, DaireNesnesi b)
        {
            return DaireDaireGenel(a.MerkezX, a.MerkezY, a.Yaricap, null, b.MerkezX, b.MerkezY, b.Yaricap, null);
        }

        private static List<PointF> DaireYay(DaireNesnesi daire, YayNesnesi yay)
        {
            return DaireDaireGenel(
                daire.MerkezX, daire.MerkezY, daire.Yaricap, null,
                yay.MerkezX, yay.MerkezY, yay.Yaricap, (x, y) => AciAraligindaMi(yay, x, y));
        }

        private static List<PointF> DogruDaireGenel(CizgiNesnesi dogru, double merkezX, double merkezY, double yaricap, Func<double, double, bool> aciFiltre)
        {
            var sonuc = new List<PointF>();
            var x1 = dogru.BaslangicX;
            var y1 = dogru.BaslangicY;
            var dx = dogru.BitisX - x1;
            var dy = dogru.BitisY - y1;

            var fx = x1 - merkezX;
            var fy = y1 - merkezY;

            var a = dx * dx + dy * dy;
            var b = 2 * (fx * dx + fy * dy);
            var c = fx * fx + fy * fy - yaricap * yaricap;

            var disc = b * b - 4 * a * c;
            if (disc < 0 || a < 1e-12)
                return sonuc;

            var sqrtDisc = Math.Sqrt(disc);
            var t1 = (-b - sqrtDisc) / (2 * a);
            var t2 = (-b + sqrtDisc) / (2 * a);

            foreach (var t in new[] { t1, t2 })
            {
                if (t < -1e-9 || t > 1 + 1e-9)
                    continue;

                var px = x1 + t * dx;
                var py = y1 + t * dy;
                if (aciFiltre == null || aciFiltre(px, py))
                    sonuc.Add(new PointF((float)px, (float)py));
            }

            return sonuc;
        }

        private static List<PointF> DaireDaireGenel(
            double cx1, double cy1, double r1, Func<double, double, bool> filtre1,
            double cx2, double cy2, double r2, Func<double, double, bool> filtre2)
        {
            var sonuc = new List<PointF>();
            var dx = cx2 - cx1;
            var dy = cy2 - cy1;
            var d = Math.Sqrt(dx * dx + dy * dy);

            if (d < 1e-9 || d > r1 + r2 + 1e-9 || d < Math.Abs(r1 - r2) - 1e-9)
                return sonuc;

            var a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
            var hKare = r1 * r1 - a * a;
            var h = hKare > 0 ? Math.Sqrt(hKare) : 0;

            var ortaX = cx1 + a * dx / d;
            var ortaY = cy1 + a * dy / d;

            var p1x = ortaX + h * dy / d;
            var p1y = ortaY - h * dx / d;
            var p2x = ortaX - h * dy / d;
            var p2y = ortaY + h * dx / d;

            if ((filtre1 == null || filtre1(p1x, p1y)) && (filtre2 == null || filtre2(p1x, p1y)))
                sonuc.Add(new PointF((float)p1x, (float)p1y));

            if (h > 1e-9 && (filtre1 == null || filtre1(p2x, p2y)) && (filtre2 == null || filtre2(p2x, p2y)))
                sonuc.Add(new PointF((float)p2x, (float)p2y));

            return sonuc;
        }

        private static bool AciAraligindaMi(YayNesnesi yay, double x, double y)
        {
            var aci = NormalizeAci(Math.Atan2(y - yay.MerkezY, x - yay.MerkezX) * 180.0 / Math.PI);
            var supurme = NormalizeSupurme(yay.BitisAcisi - yay.BaslangicAcisi);
            var goreli = NormalizeAci(aci - yay.BaslangicAcisi);
            return goreli <= supurme + 1e-6;
        }

        private static double NormalizeAci(double aciDerece)
        {
            var sonuc = aciDerece % 360.0;
            if (sonuc < 0) sonuc += 360.0;
            return sonuc;
        }

        private static double NormalizeSupurme(double supurmeDerece)
        {
            var sonuc = NormalizeAci(supurmeDerece);
            return sonuc <= 0 ? sonuc + 360.0 : sonuc;
        }
    }
}
