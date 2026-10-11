using System;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Kaydır/Kopyala/Döndür/Ölçekle komutları için CizimNesnesi'leri klonlama ve dönüştürme yardımcıları.</summary>
    public static class NesneDonusumleri
    {
        public static CizimNesnesi Klonla(CizimNesnesi nesne)
        {
            switch (nesne)
            {
                case CizgiNesnesi c:
                    return new CizgiNesnesi
                    {
                        BaslangicX = c.BaslangicX, BaslangicY = c.BaslangicY, BaslangicZ = c.BaslangicZ,
                        BitisX = c.BitisX, BitisY = c.BitisY, BitisZ = c.BitisZ
                    };
                case YaziNesnesi y:
                    return new YaziNesnesi
                    {
                        KonumX = y.KonumX,
                        KonumY = y.KonumY,
                        KonumZ = y.KonumZ,
                        Metin = y.Metin,
                        Yukseklik = y.Yukseklik,
                        Donus = y.Donus,
                        HizaYatay = y.HizaYatay,
                        HizaDikey = y.HizaDikey
                    };
                case NoktaNesnesi n:
                    return new NoktaNesnesi { X = n.X, Y = n.Y, Z = n.Z };
                case DaireNesnesi d:
                    return new DaireNesnesi { MerkezX = d.MerkezX, MerkezY = d.MerkezY, MerkezZ = d.MerkezZ, Yaricap = d.Yaricap };
                case YayNesnesi a:
                    return new YayNesnesi
                    {
                        MerkezX = a.MerkezX,
                        MerkezY = a.MerkezY,
                        MerkezZ = a.MerkezZ,
                        Yaricap = a.Yaricap,
                        BaslangicAcisi = a.BaslangicAcisi,
                        BitisAcisi = a.BitisAcisi
                    };
                case SpiralNesnesi s:
                    return new SpiralNesnesi
                    {
                        BaslangicX = s.BaslangicX,
                        BaslangicY = s.BaslangicY,
                        BaslangicZ = s.BaslangicZ,
                        BitisX = s.BitisX,
                        BitisY = s.BitisY,
                        BitisZ = s.BitisZ,
                        PiX = s.PiX,
                        PiY = s.PiY,
                        PiZ = s.PiZ,
                        YaricapBaslangic = s.YaricapBaslangic,
                        YaricapBitis = s.YaricapBitis,
                        Uzunluk = s.Uzunluk,
                        Yon = s.Yon
                    };
                default:
                    return nesne;
            }
        }

        public static void Tasi(CizimNesnesi nesne, double dx, double dy)
        {
            switch (nesne)
            {
                case CizgiNesnesi c:
                    c.BaslangicX += dx; c.BaslangicY += dy;
                    c.BitisX += dx; c.BitisY += dy;
                    break;
                case YaziNesnesi y:
                    y.KonumX += dx; y.KonumY += dy;
                    break;
                case NoktaNesnesi n:
                    n.X += dx; n.Y += dy;
                    break;
                case DaireNesnesi d:
                    d.MerkezX += dx; d.MerkezY += dy;
                    break;
                case YayNesnesi a:
                    a.MerkezX += dx; a.MerkezY += dy;
                    break;
                case SpiralNesnesi s:
                    s.BaslangicX += dx; s.BaslangicY += dy;
                    s.BitisX += dx; s.BitisY += dy;
                    s.PiX += dx; s.PiY += dy;
                    break;
            }
        }

        public static void Dondur(CizimNesnesi nesne, PointF merkez, double aciDerece)
        {
            var rad = aciDerece * Math.PI / 180.0;
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);

            (double X, double Y) DonduruluenNokta(double x, double y)
            {
                var dx = x - merkez.X;
                var dy = y - merkez.Y;
                return (merkez.X + dx * cos - dy * sin, merkez.Y + dx * sin + dy * cos);
            }

            switch (nesne)
            {
                case CizgiNesnesi c:
                {
                    var bas = DonduruluenNokta(c.BaslangicX, c.BaslangicY);
                    var bit = DonduruluenNokta(c.BitisX, c.BitisY);
                    c.BaslangicX = bas.X; c.BaslangicY = bas.Y;
                    c.BitisX = bit.X; c.BitisY = bit.Y;
                    break;
                }
                case YaziNesnesi y:
                {
                    var konum = DonduruluenNokta(y.KonumX, y.KonumY);
                    y.KonumX = konum.X; y.KonumY = konum.Y;
                    y.Donus += aciDerece;
                    break;
                }
                case NoktaNesnesi n:
                {
                    var nokta = DonduruluenNokta(n.X, n.Y);
                    n.X = nokta.X; n.Y = nokta.Y;
                    break;
                }
                case DaireNesnesi d:
                {
                    var merkezYeni = DonduruluenNokta(d.MerkezX, d.MerkezY);
                    d.MerkezX = merkezYeni.X; d.MerkezY = merkezYeni.Y;
                    break;
                }
                case YayNesnesi a:
                {
                    var merkezYeni = DonduruluenNokta(a.MerkezX, a.MerkezY);
                    a.MerkezX = merkezYeni.X; a.MerkezY = merkezYeni.Y;
                    a.BaslangicAcisi += aciDerece;
                    a.BitisAcisi += aciDerece;
                    break;
                }
                case SpiralNesnesi s:
                {
                    var bas = DonduruluenNokta(s.BaslangicX, s.BaslangicY);
                    var bit = DonduruluenNokta(s.BitisX, s.BitisY);
                    var pi = DonduruluenNokta(s.PiX, s.PiY);
                    s.BaslangicX = bas.X; s.BaslangicY = bas.Y;
                    s.BitisX = bit.X; s.BitisY = bit.Y;
                    s.PiX = pi.X; s.PiY = pi.Y;
                    break;
                }
            }
        }

        public static void Olcekle(CizimNesnesi nesne, PointF merkez, double carpan)
        {
            (double X, double Y) OlceklenmisNokta(double x, double y)
            {
                return (merkez.X + (x - merkez.X) * carpan, merkez.Y + (y - merkez.Y) * carpan);
            }

            switch (nesne)
            {
                case CizgiNesnesi c:
                {
                    var bas = OlceklenmisNokta(c.BaslangicX, c.BaslangicY);
                    var bit = OlceklenmisNokta(c.BitisX, c.BitisY);
                    c.BaslangicX = bas.X; c.BaslangicY = bas.Y;
                    c.BitisX = bit.X; c.BitisY = bit.Y;
                    break;
                }
                case YaziNesnesi y:
                {
                    var konum = OlceklenmisNokta(y.KonumX, y.KonumY);
                    y.KonumX = konum.X; y.KonumY = konum.Y;
                    y.Yukseklik *= Math.Abs(carpan);
                    break;
                }
                case NoktaNesnesi n:
                {
                    var nokta = OlceklenmisNokta(n.X, n.Y);
                    n.X = nokta.X; n.Y = nokta.Y;
                    break;
                }
                case DaireNesnesi d:
                {
                    var merkezYeni = OlceklenmisNokta(d.MerkezX, d.MerkezY);
                    d.MerkezX = merkezYeni.X; d.MerkezY = merkezYeni.Y;
                    d.Yaricap *= Math.Abs(carpan);
                    break;
                }
                case YayNesnesi a:
                {
                    var merkezYeni = OlceklenmisNokta(a.MerkezX, a.MerkezY);
                    a.MerkezX = merkezYeni.X; a.MerkezY = merkezYeni.Y;
                    a.Yaricap *= Math.Abs(carpan);
                    break;
                }
                case SpiralNesnesi s:
                {
                    var bas = OlceklenmisNokta(s.BaslangicX, s.BaslangicY);
                    var bit = OlceklenmisNokta(s.BitisX, s.BitisY);
                    var pi = OlceklenmisNokta(s.PiX, s.PiY);
                    s.BaslangicX = bas.X; s.BaslangicY = bas.Y;
                    s.BitisX = bit.X; s.BitisY = bit.Y;
                    s.PiX = pi.X; s.PiY = pi.Y;
                    var mutlakCarpan = Math.Abs(carpan);
                    s.YaricapBaslangic *= mutlakCarpan;
                    s.YaricapBitis *= mutlakCarpan;
                    s.Uzunluk *= mutlakCarpan;
                    break;
                }
            }
        }
    }
}
