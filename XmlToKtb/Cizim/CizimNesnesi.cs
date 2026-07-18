using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Çizim ekranındaki tüm objelerin (çizgi, ileride kurp/polyline...) ortak temeli.</summary>
    public abstract class CizimNesnesi
    {
        public bool Secili { get; set; }

        public abstract void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik);

        /// <summary>Verilen dünya noktasına, verilen dünya toleransı içinde değiyor mu?</summary>
        public abstract bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya);
    }
}
