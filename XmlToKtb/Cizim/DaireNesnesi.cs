using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Merkez ve yarıçapla tanımlı daire (ör. DXF CIRCLE varlığı).</summary>
    public class DaireNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Daire";

        [DisplayName("Merkez X")]
        public double MerkezX { get; set; }

        [DisplayName("Merkez Y")]
        public double MerkezY { get; set; }

        [DisplayName("Yarıçap")]
        public double Yaricap { get; set; }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            var ekranMerkez = gorunum.DunyadanEkrana(MerkezX, MerkezY, panelGenislik, panelYukseklik);
            var pikselYaricap = (float)(Yaricap * gorunum.Olcek);
            if (pikselYaricap < 0.5f)
                return;

            using (var kalem = new Pen(Secili ? Color.DodgerBlue : Color.Black, Secili ? 2f : 1f))
            {
                grafik.DrawEllipse(kalem, ekranMerkez.X - pikselYaricap, ekranMerkez.Y - pikselYaricap, pikselYaricap * 2, pikselYaricap * 2);
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            var dx = dunyaNoktasi.X - MerkezX;
            var dy = dunyaNoktasi.Y - MerkezY;
            var mesafe = Math.Sqrt(dx * dx + dy * dy);
            return Math.Abs(mesafe - Yaricap) <= toleransDunya;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)(MerkezX - Yaricap), (float)MerkezY);
            yield return new PointF((float)(MerkezX + Yaricap), (float)MerkezY);
            yield return new PointF((float)MerkezX, (float)(MerkezY - Yaricap));
            yield return new PointF((float)MerkezX, (float)(MerkezY + Yaricap));
        }

        public override PointF? MerkezNoktaAl()
        {
            return new PointF((float)MerkezX, (float)MerkezY);
        }
    }
}
