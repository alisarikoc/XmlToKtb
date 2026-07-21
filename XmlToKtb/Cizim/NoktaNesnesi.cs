using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Tek bir nokta işaretleyicisi (ör. DXF POINT varlığı).</summary>
    public class NoktaNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Nokta";

        [DisplayName("X")]
        public double X { get; set; }

        [DisplayName("Y")]
        public double Y { get; set; }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            const float yaricap = 3f;
            var ekran = gorunum.DunyadanEkrana(X, Y, panelGenislik, panelYukseklik);
            using (var kalem = new Pen(Secili ? Color.DodgerBlue : Color.Black, 1.5f))
            {
                grafik.DrawLine(kalem, ekran.X - yaricap, ekran.Y, ekran.X + yaricap, ekran.Y);
                grafik.DrawLine(kalem, ekran.X, ekran.Y - yaricap, ekran.X, ekran.Y + yaricap);
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            var dx = dunyaNoktasi.X - X;
            var dy = dunyaNoktasi.Y - Y;
            return dx * dx + dy * dy <= toleransDunya * toleransDunya;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)X, (float)Y);
        }
    }
}
