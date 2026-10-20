using System;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Benzerlik dönüşümü (öteleme, dönme, ölçek ve istenirse aynalama): p' = Hedef + Ölçek · Döndür(Açı) · Ayna(p − Kaynak).
    /// Aynalamada eksen, Kaynak noktasından AynaAcisi doğrultusunda geçer. Bütün obje tiplerine aynı biçimde uygulanır;
    /// açılar ve yarıçaplar da buna göre düzeltilir. Çift hassasiyetle çalışır.
    /// </summary>
    public sealed class Donusum
    {
        public double KaynakX, KaynakY, HedefX, HedefY;
        /// <summary>Dönme açısı (radyan, saat yönünün tersine).</summary>
        public double Aci;
        public double Olcek = 1;
        public bool Aynali;
        /// <summary>Ayna ekseninin doğrultusu (radyan).</summary>
        public double AynaAcisi;

        public static Donusum Oteleme(double dx, double dy) => new Donusum { HedefX = dx, HedefY = dy };

        public static Donusum Dondurme(double mx, double my, double aciRadyan) =>
            new Donusum { KaynakX = mx, KaynakY = my, HedefX = mx, HedefY = my, Aci = aciRadyan };

        public static Donusum Olcekleme(double mx, double my, double olcek) =>
            new Donusum { KaynakX = mx, KaynakY = my, HedefX = mx, HedefY = my, Olcek = olcek };

        /// <summary>İki noktadan geçen eksene göre aynalama.</summary>
        public static Donusum Aynalama(double x1, double y1, double x2, double y2) =>
            new Donusum { KaynakX = x1, KaynakY = y1, HedefX = x1, HedefY = y1, Aynali = true, AynaAcisi = Math.Atan2(y2 - y1, x2 - x1) };

        /// <summary>
        /// İki noktadan hizalama (AutoCAD ALIGN): 1. kaynak noktası 1. hedefe taşınır, 1-2 kaynak doğrultusu 1-2 hedef
        /// doğrultusuna döndürülür; istenirse 1-2 uzunlukları eşitlenecek şekilde ölçeklenir.
        /// </summary>
        public static Donusum Hizalama(double kx1, double ky1, double hx1, double hy1, double kx2, double ky2, double hx2, double hy2, bool olceklensin)
        {
            double kaynakBoyu = Math.Sqrt((kx2 - kx1) * (kx2 - kx1) + (ky2 - ky1) * (ky2 - ky1));
            double hedefBoyu = Math.Sqrt((hx2 - hx1) * (hx2 - hx1) + (hy2 - hy1) * (hy2 - hy1));
            return new Donusum
            {
                KaynakX = kx1, KaynakY = ky1, HedefX = hx1, HedefY = hy1,
                Aci = Math.Atan2(hy2 - hy1, hx2 - hx1) - Math.Atan2(ky2 - ky1, kx2 - kx1),
                Olcek = olceklensin && kaynakBoyu > 1e-12 ? hedefBoyu / kaynakBoyu : 1
            };
        }

        public void Nokta(double x, double y, out double nx, out double ny)
        {
            double vx = x - KaynakX, vy = y - KaynakY;
            if (Aynali)
            {
                double c2 = Math.Cos(2 * AynaAcisi), s2 = Math.Sin(2 * AynaAcisi);
                var ax = vx * c2 + vy * s2;
                vy = vx * s2 - vy * c2;
                vx = ax;
            }
            double c = Math.Cos(Aci), s = Math.Sin(Aci);
            nx = HedefX + Olcek * (vx * c - vy * s);
            ny = HedefY + Olcek * (vx * s + vy * c);
        }

        /// <summary>Bir doğrultunun (radyan) dönüşmüş hali.</summary>
        public double Yon(double radyan) => (Aynali ? 2 * AynaAcisi - radyan : radyan) + Aci;

        public double YonDerece(double derece) => Yon(derece * Math.PI / 180) * 180 / Math.PI;

        /// <summary>Dönüşümü objeye uygular (objenin kendisi değişir).</summary>
        public void Uygula(CizimNesnesi nesne)
        {
            double x, y;
            switch (nesne)
            {
                case CizgiNesnesi c:
                    Nokta(c.BaslangicX, c.BaslangicY, out x, out y);
                    c.BaslangicX = x; c.BaslangicY = y;
                    Nokta(c.BitisX, c.BitisY, out x, out y);
                    c.BitisX = x; c.BitisY = y;
                    break;

                case NoktaNesnesi n:
                    Nokta(n.X, n.Y, out x, out y);
                    n.X = x; n.Y = y;
                    break;

                case YaziNesnesi yazi:
                {
                    Nokta(yazi.KonumX, yazi.KonumY, out x, out y);
                    yazi.KonumX = x; yazi.KonumY = y;
                    yazi.Yukseklik *= Math.Abs(Olcek);
                    var donus = AciNormal(YonDerece(yazi.Donus));
                    // Aynalanan yazı ters okunmasın: baş aşağı geliyorsa 180° çevrilir ve yatay hizası değişir.
                    if (Aynali && donus > 90 && donus <= 270)
                    {
                        donus = AciNormal(donus + 180);
                        if (yazi.HizaYatay == YatayHizalama.Sol) yazi.HizaYatay = YatayHizalama.Sag;
                        else if (yazi.HizaYatay == YatayHizalama.Sag) yazi.HizaYatay = YatayHizalama.Sol;
                    }
                    yazi.Donus = donus;
                    break;
                }

                case DaireNesnesi d:
                    Nokta(d.MerkezX, d.MerkezY, out x, out y);
                    d.MerkezX = x; d.MerkezY = y;
                    d.Yaricap *= Math.Abs(Olcek);
                    break;

                case YayNesnesi a:
                {
                    Nokta(a.MerkezX, a.MerkezY, out x, out y);
                    a.MerkezX = x; a.MerkezY = y;
                    a.Yaricap *= Math.Abs(Olcek);
                    double bas = YonDerece(a.BaslangicAcisi), bit = YonDerece(a.BitisAcisi);
                    // Aynalamada yayın dönüş yönü tersine döner; saat yönünün tersine çizim için uçlar yer değiştirir.
                    a.BaslangicAcisi = AciNormal(Aynali ? bit : bas);
                    a.BitisAcisi = AciNormal(Aynali ? bas : bit);
                    break;
                }

                case SpiralNesnesi sp:
                    Nokta(sp.BaslangicX, sp.BaslangicY, out x, out y);
                    sp.BaslangicX = x; sp.BaslangicY = y;
                    Nokta(sp.BitisX, sp.BitisY, out x, out y);
                    sp.BitisX = x; sp.BitisY = y;
                    Nokta(sp.PiX, sp.PiY, out x, out y);
                    sp.PiX = x; sp.PiY = y;
                    sp.YaricapBaslangic *= Math.Abs(Olcek);
                    sp.YaricapBitis *= Math.Abs(Olcek);
                    sp.Uzunluk *= Math.Abs(Olcek);
                    if (Aynali)
                        sp.Yon = sp.Yon == DonusYonu.Sol ? DonusYonu.Sag : DonusYonu.Sol;
                    break;

                case CokluDogruNesnesi cd:
                    foreach (var kose in cd.Koseler)
                    {
                        Nokta(kose.X, kose.Y, out x, out y);
                        kose.X = x; kose.Y = y;
                        if (Aynali)
                            kose.Bulge = -kose.Bulge;
                    }
                    break;

                case OlcuNesnesi olcu:
                    // Tanım noktaları dönüşür; doğrusal ölçünün doğrultusu da döner. Ölçülen değer yeni geometriden
                    // hesaplanır (ölçeklenen ölçü yeni uzunluğu gösterir).
                    Nokta(olcu.AX, olcu.AY, out x, out y);
                    olcu.AX = x; olcu.AY = y;
                    Nokta(olcu.BX, olcu.BY, out x, out y);
                    olcu.BX = x; olcu.BY = y;
                    Nokta(olcu.CX, olcu.CY, out x, out y);
                    olcu.CX = x; olcu.CY = y;
                    Nokta(olcu.DX, olcu.DY, out x, out y);
                    olcu.DX = x; olcu.DY = y;
                    olcu.Aci = AciNormal(YonDerece(olcu.Aci));
                    break;

                case KilavuzNesnesi kilavuz:
                    // Köşeler dönüşür; iniş çizgisi ve yazı her zaman yatay kalır (AutoCAD'deki gibi).
                    foreach (var k in kilavuz.Koseler)
                    {
                        Nokta(k[0], k[1], out x, out y);
                        k[0] = x; k[1] = y;
                    }
                    break;

                case BlokNesnesi blok:
                    Nokta(blok.KonumX, blok.KonumY, out x, out y);
                    blok.KonumX = x; blok.KonumY = y;
                    foreach (var oge in blok.Ogeler)
                        Uygula(oge);
                    break;

                case YuzeyNesnesi yuzey:
                    for (var i = 0; i < yuzey.X.Count; i++)
                    {
                        Nokta(yuzey.X[i], yuzey.Y[i], out x, out y);
                        yuzey.X[i] = x; yuzey.Y[i] = y;
                    }
                    yuzey.KoordinatlarDegisti();
                    break;

                case GuzergahNesnesi g:
                    Nokta(g.BaslangicX, g.BaslangicY, out x, out y);
                    g.BaslangicX = x; g.BaslangicY = y;
                    g.BaslangicAcisi = Yon(g.BaslangicAcisi);
                    foreach (var parca in g.TumParcalar)
                    {
                        parca.Uzunluk *= Math.Abs(Olcek);
                        if (!double.IsInfinity(parca.YaricapBaslangic)) parca.YaricapBaslangic *= Math.Abs(Olcek);
                        if (!double.IsInfinity(parca.YaricapBitis)) parca.YaricapBitis *= Math.Abs(Olcek);
                        if (Aynali)
                        {
                            parca.Yon = parca.Yon == DonusYonu.Sol ? DonusYonu.Sag : DonusYonu.Sol;
                            parca.KirikAcisi = -parca.KirikAcisi;
                        }
                        Nokta(parca.SabitX, parca.SabitY, out x, out y);
                        parca.SabitX = x; parca.SabitY = y;
                        parca.SabitAci = Yon(parca.SabitAci);
                    }
                    g.Hesapla();
                    break;
            }
        }

        private static double AciNormal(double derece)
        {
            derece %= 360;
            return derece < 0 ? derece + 360 : derece;
        }
    }
}
