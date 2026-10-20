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

        /// <summary>Nokta numarası/adı. DXF gibi nokta numarası taşımayan kaynaklardan gelen noktalarda "0"dır.</summary>
        [DisplayName("Nokta No")]
        public string NoktaAdi { get; set; } = "0";

        [DisplayName("Kod")]
        public string Kod { get; set; } = "";

        [DisplayName("Pafta")]
        public string Pafta { get; set; } = "";

        [DisplayName("X")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double X { get; set; }

        [DisplayName("Y")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Y { get; set; }

        [DisplayName("Z")]
        [TypeConverter(typeof(OndalikDegerConverter))]
        public double Z { get; set; }

        public override void Ciz(Graphics grafik, CizimGorunum gorunum, int panelGenislik, int panelYukseklik)
        {
            const float yaricap = 3f;
            var ekran = gorunum.DunyadanEkrana(X, Y, panelGenislik, panelYukseklik);
            var renk = Secili ? Color.DodgerBlue : CizimRengi(Color.Black);
            using (var kalem = new Pen(renk, 1.5f))
            {
                grafik.DrawLine(kalem, ekran.X - yaricap, ekran.Y, ekran.X + yaricap, ekran.Y);
                grafik.DrawLine(kalem, ekran.X, ekran.Y - yaricap, ekran.X, ekran.Y + yaricap);
            }

            // Etiketler noktanın sağında alt alta: ad, kot, kod (yalnızca açık olanlar).
            var secenekler = CizimBaglami.Secenekler;
            if (secenekler == null)
                return;
            var satir = 0;
            void Yaz(string metin)
            {
                if (string.IsNullOrWhiteSpace(metin))
                    return;
                CizimBaglami.EtiketYaz(grafik, gorunum, metin, ekran, renk, 0.4f, -0.6f + 1.15f * satir);
                satir++;
            }
            if (secenekler.NoktaAdlari)
                Yaz(NoktaAdi);
            if (secenekler.NoktaKotlari)
                Yaz(CizimBaglami.KotMetni(Z));
            if (secenekler.NoktaKodlari)
                Yaz(Kod);
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
