using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Blok (DXF'teki INSERT): birlikte seçilip taşınan objeler topluluğu. Alt objeler dünya koordinatında tutulur;
    /// Patlat komutu onları ayrı objeler olarak çizime bırakır.
    /// </summary>
    public class BlokNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Blok";

        [DisplayName("Blok Adı")]
        public string Ad { get; set; } = "";

        [DisplayName("Ekleme Noktası X")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        [ReadOnly(true)]
        public double KonumX { get; set; }

        [DisplayName("Ekleme Noktası Y")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        [ReadOnly(true)]
        public double KonumY { get; set; }

        [Browsable(false)]
        public List<CizimNesnesi> Ogeler { get; } = new List<CizimNesnesi>();

        [DisplayName("Obje Sayısı")]
        public int ObjeSayisi => Ogeler.Count;

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            foreach (var oge in Ogeler)
            {
                // Seçili blok, bütün objeleri seçiliymiş gibi çizilir; renksiz ("0" tabakalı) objeler bloğun rengini alır.
                var eskiSecili = oge.Secili;
                var eskiTabaka = oge.Tabaka;
                oge.Secili = Secili;
                if (oge.Tabaka == null)
                    oge.Tabaka = Tabaka;
                oge.Ciz(grafik, gorunum, panelGenislik, panelYukseklik);
                oge.Secili = eskiSecili;
                oge.Tabaka = eskiTabaka;
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            return Ogeler.Any(o => o.IsabetEttiMi(dunyaNoktasi, toleransDunya));
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)KonumX, (float)KonumY);
            foreach (var oge in Ogeler)
            foreach (var nokta in oge.UcNoktalariAl())
                yield return nokta;
        }

        public BlokNesnesi Kopyala()
        {
            var kopya = new BlokNesnesi { Ad = Ad, KonumX = KonumX, KonumY = KonumY };
            kopya.Ogeler.AddRange(Ogeler.Select(NesneDonusumleri.Klonla));
            return kopya;
        }
    }
}
