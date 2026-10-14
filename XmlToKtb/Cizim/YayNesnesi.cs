using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace XmlToKtb.Cizim
{
    /// <summary>Merkez, yarıçap ve başlangıç/bitiş açılarıyla tanımlı yay (ör. DXF ARC varlığı).
    /// Açılar, DXF standardına uygun şekilde +X ekseninden saat yönünün tersine (CCW), derece cinsindendir.</summary>
    public class YayNesnesi : CizimNesnesi
    {
        [DisplayName("Tip")]
        [ReadOnly(true)]
        public override string Tip => "Yay";

        [DisplayName("Merkez X")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double MerkezX { get; set; }

        [DisplayName("Merkez Y")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double MerkezY { get; set; }

        [DisplayName("Yarıçap")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Yaricap { get; set; }

        [DisplayName("Başlangıç Açısı")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BaslangicAcisi { get; set; }

        [DisplayName("Bitiş Açısı")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double BitisAcisi { get; set; }

        /// <summary>Başlangıç açısından bitiş açısına saat yönünün tersine ölçülen yay boyu.</summary>
        [DisplayName("Uzunluk")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Uzunluk
        {
            get
            {
                var aci = (BitisAcisi - BaslangicAcisi) % 360.0;
                if (aci <= 0)
                    aci += 360.0;
                return Yaricap * aci * Math.PI / 180.0;
            }
        }

        [DisplayName("Merkez Z")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double MerkezZ { get; set; }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            var ekranMerkez = gorunum.DunyadanEkrana(MerkezX, MerkezY, panelGenislik, panelYukseklik);
            var pikselYaricap = (float)(Yaricap * gorunum.Olcek);
            if (pikselYaricap < 0.5f)
                return;

            var dunyaSupurme = NormalizeSupurme(BitisAcisi - BaslangicAcisi);
            var gdiBaslangic = -BaslangicAcisi;
            var gdiSupurme = -dunyaSupurme;

            using (var kalem = new Pen(Secili ? Color.DodgerBlue : CizimRengi(Color.Black), Secili ? 2f : 1f))
            {
                grafik.DrawArc(kalem, ekranMerkez.X - pikselYaricap, ekranMerkez.Y - pikselYaricap,
                    pikselYaricap * 2, pikselYaricap * 2, (float)gdiBaslangic, (float)gdiSupurme);
            }
        }

        public override bool IsabetEttiMi(PointF dunyaNoktasi, double toleransDunya)
        {
            var dx = dunyaNoktasi.X - MerkezX;
            var dy = dunyaNoktasi.Y - MerkezY;
            var mesafe = Math.Sqrt(dx * dx + dy * dy);
            if (Math.Abs(mesafe - Yaricap) > toleransDunya)
                return false;

            var aci = NormalizeAci(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            var supurme = NormalizeSupurme(BitisAcisi - BaslangicAcisi);
            var goreliAci = NormalizeAci(aci - BaslangicAcisi);
            return goreliAci <= supurme;
        }

        public override IEnumerable<PointF> UcNoktalariAl()
        {
            yield return new PointF((float)MerkezX, (float)MerkezY);
            yield return AcidaNokta(BaslangicAcisi);
            yield return AcidaNokta(BitisAcisi);
            yield return AcidaNokta((BaslangicAcisi + BitisAcisi) / 2);
        }

        public override PointF? MerkezNoktaAl()
        {
            return new PointF((float)MerkezX, (float)MerkezY);
        }

        private PointF AcidaNokta(double aciDerece)
        {
            var rad = aciDerece * Math.PI / 180.0;
            return new PointF((float)(MerkezX + Yaricap * Math.Cos(rad)), (float)(MerkezY + Yaricap * Math.Sin(rad)));
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
