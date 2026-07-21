using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Çizim ekranındaki tüm objelerin (çizgi, ileride kurp/polyline...) ortak temeli.</summary>
    public abstract class CizimNesnesi
    {
        [Browsable(false)]
        public bool Secili { get; set; }

        /// <summary>Properties panelinde gösterilecek, kullanıcıya yönelik obje tipi adı (ör. "Çizgi", "Yazı").</summary>
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public abstract string Tip { get; }

        public abstract void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik);

        /// <summary>Verilen dünya noktasına, verilen dünya toleransı içinde değiyor mu?</summary>
        public abstract bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya);

        /// <summary>Nokta/Uç yakalama için objenin dünya koordinatındaki uç noktaları.</summary>
        public virtual IEnumerable<PointF> UcNoktalariAl()
        {
            yield break;
        }

        /// <summary>Orta nokta yakalama için objenin dünya koordinatındaki orta noktası (yoksa null).</summary>
        public virtual PointF? OrtaNoktaAl()
        {
            return null;
        }

        /// <summary>Dik yakalama için, verilen referans noktasından objeye indirilen dikmenin ayağı (yoksa null).</summary>
        public virtual PointF? DikNoktaAl(PointF referansDunyaNoktasi)
        {
            return null;
        }

        /// <summary>Merkez yakalama için objenin merkez noktası (Daire/Yay için; yoksa null).</summary>
        public virtual PointF? MerkezNoktaAl()
        {
            return null;
        }
    }
}
