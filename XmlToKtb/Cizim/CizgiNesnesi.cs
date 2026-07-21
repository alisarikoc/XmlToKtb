using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>İki nokta arasında çizilen bir çizgi objesi.</summary>
    public class CizgiNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Çizgi";

        [DisplayName("Başlangıç X")]
        [Category("Başlangıç Noktası")]
        public double BaslangicX { get; set; }

        [DisplayName("Başlangıç Y")]
        [Category("Başlangıç Noktası")]
        public double BaslangicY { get; set; }

        [DisplayName("Bitiş X")]
        [Category("Bitiş Noktası")]
        public double BitisX { get; set; }

        [DisplayName("Bitiş Y")]
        [Category("Bitiş Noktası")]
        public double BitisY { get; set; }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            var bas = gorunum.DunyadanEkrana(BaslangicX, BaslangicY, panelGenislik, panelYukseklik);
            var bit = gorunum.DunyadanEkrana(BitisX, BitisY, panelGenislik, panelYukseklik);

            using (var kalem = new Pen(Secili ? Color.DodgerBlue : Color.Black, Secili ? 2.5f : 1.5f))
            {
                grafik.DrawLine(kalem, bas, bit);
            }

            if (Secili)
            {
                DugumNoktasiCiz(grafik, bas);
                DugumNoktasiCiz(grafik, bit);
            }
        }

        private static void DugumNoktasiCiz(Graphics grafik, PointF ekranNoktasi)
        {
            const float yaricap = 4f;
            var dikdortgen = new RectangleF(ekranNoktasi.X - yaricap, ekranNoktasi.Y - yaricap, yaricap * 2, yaricap * 2);
            grafik.FillRectangle(Brushes.White, dikdortgen);
            using (var kalem = new Pen(Color.DodgerBlue, 1.5f))
            {
                grafik.DrawRectangle(kalem, dikdortgen.X, dikdortgen.Y, dikdortgen.Width, dikdortgen.Height);
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            var uzaklik = NoktaDogruParcasiUzakligi(dunyaNoktasi.X, dunyaNoktasi.Y, BaslangicX, BaslangicY, BitisX, BitisY);
            return uzaklik <= toleransDunya;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)BaslangicX, (float)BaslangicY);
            yield return new PointF((float)BitisX, (float)BitisY);
        }

        public override PointF? OrtaNoktaAl()
        {
            return new PointF((float)((BaslangicX + BitisX) / 2.0), (float)((BaslangicY + BitisY) / 2.0));
        }

        public override PointF? DikNoktaAl(PointF referansDunyaNoktasi)
        {
            var dx = BitisX - BaslangicX;
            var dy = BitisY - BaslangicY;
            var uzunlukKare = dx * dx + dy * dy;
            if (uzunlukKare < 1e-12)
                return null;

            var t = ((referansDunyaNoktasi.X - BaslangicX) * dx + (referansDunyaNoktasi.Y - BaslangicY) * dy) / uzunlukKare;
            if (t < 0 || t > 1)
                return null;

            var ayakX = BaslangicX + t * dx;
            var ayakY = BaslangicY + t * dy;
            return new PointF((float)ayakX, (float)ayakY);
        }

        private static double NoktaDogruParcasiUzakligi(double px, double py, double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var uzunlukKare = dx * dx + dy * dy;
            if (uzunlukKare < 1e-12)
                return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));

            var t = ((px - x1) * dx + (py - y1) * dy) / uzunlukKare;
            t = Math.Max(0, Math.Min(1, t));

            var enYakinX = x1 + t * dx;
            var enYakinY = y1 + t * dy;
            return Math.Sqrt((px - enYakinX) * (px - enYakinX) + (py - enYakinY) * (py - enYakinY));
        }
    }
}
