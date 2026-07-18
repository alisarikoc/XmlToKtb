using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Ekran (piksel) ile dünya (gerçek koordinat) arasındaki pan/zoom dönüşümünü tutar.</summary>
    public class CizimGorunum
    {
        public double Olcek { get; set; } = 1.0; // piksel / dünya birimi
        public double OfsetX { get; set; } // panel ortasına denk gelen dünya X'i
        public double OfsetY { get; set; } // panel ortasına denk gelen dünya Y'si

        public const double MinOlcek = 0.0000001;
        public const double MaxOlcek = 1000000.0;

        public PointF DunyadanEkrana(double dunyaX, double dunyaY, int panelGenislik, int panelYukseklik)
        {
            var ekranX = panelGenislik / 2.0 + (dunyaX - OfsetX) * Olcek;
            var ekranY = panelYukseklik / 2.0 - (dunyaY - OfsetY) * Olcek;
            return new PointF((float)ekranX, (float)ekranY);
        }

        public PointF EkrandanDunyaya(PointF ekranNoktasi, int panelGenislik, int panelYukseklik)
        {
            var dunyaX = OfsetX + (ekranNoktasi.X - panelGenislik / 2.0) / Olcek;
            var dunyaY = OfsetY - (ekranNoktasi.Y - panelYukseklik / 2.0) / Olcek;
            return new PointF((float)dunyaX, (float)dunyaY);
        }

        public void ImlecUzerindeYakinlastir(PointF ekranNoktasi, int panelGenislik, int panelYukseklik, double carpan)
        {
            var dunyaNoktasi = EkrandanDunyaya(ekranNoktasi, panelGenislik, panelYukseklik);
            var yeniOlcek = Olcek * carpan;
            if (yeniOlcek < MinOlcek || yeniOlcek > MaxOlcek)
                return;
            Olcek = yeniOlcek;
            OfsetX = dunyaNoktasi.X - (ekranNoktasi.X - panelGenislik / 2.0) / Olcek;
            OfsetY = dunyaNoktasi.Y + (ekranNoktasi.Y - panelYukseklik / 2.0) / Olcek;
        }
    }
}
