using System;
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

        /// <summary>Objenin tabakası. Tabaka kullanmayan çizim ekranlarında null'dır.</summary>
        [Browsable(false)]
        public Tabaka Tabaka { get; set; }

        /// <summary>Objenin kendine ait rengi; null ise obje tabakasının rengiyle çizilir.</summary>
        [Browsable(false)]
        public Color? OzelRenk { get; set; }

        /// <summary>Özellikler panelinde objenin tabakasını gösterir ve çizimdeki başka bir tabakaya almayı sağlar.</summary>
        [DisplayName("Tabaka")]
        [TypeConverter(typeof(TabakaAdiConverter))]
        public string TabakaAdi
        {
            get => Tabaka?.Ad ?? "";
            set
            {
                var yeni = Tabaka?.Liste?.Find(t => t.Ad == value);
                if (yeni != null)
                    Tabaka = yeni;
            }
        }

        /// <summary>
        /// Objenin çizgi kalınlığı (mm, DXF lineweight); 0 ise tabakanın kalınlığı kullanılır. Görünüm menüsünde
        /// "Kalın Obje Çizimleri" açıkken ekranda bu kalınlıkta çizilir.
        /// </summary>
        [DisplayName("Çizgi Kalınlığı (mm)")]
        public double CizgiKalinligi { get; set; }

        /// <summary>Seçili olmayan objenin çizileceği renk: kendi rengi, yoksa tabakasının rengi, o da yoksa verilen varsayılan.</summary>
        protected Color CizimRengi(Color varsayilan)
        {
            return CizimBaglami.ZemineUydur(OzelRenk ?? Tabaka?.Renk ?? varsayilan);
        }

        /// <summary>
        /// Seçili olmayan objenin kalem kalınlığı (piksel): "Kalın Obje Çizimleri" açıksa ve objenin (ya da tabakasının)
        /// kalınlığı varsa o kalınlık (96 dpi ekranda mm), yoksa verilen varsayılan.
        /// </summary>
        protected float KalemKalinligi(float varsayilan)
        {
            var mm = CizgiKalinligi > 0 ? CizgiKalinligi : Tabaka?.CizgiKalinligi ?? 0;
            if (mm <= 0 || CizimBaglami.Secenekler == null || !CizimBaglami.Secenekler.KalinCizim)
                return varsayilan;
            return Math.Max(varsayilan, (float)(mm / 25.4 * 96));
        }

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

        /// <summary>
        /// Orta nokta yakalama için objenin orta noktaları, çift hassasiyetle ({x, y}). Çok parçalı objelerde (çoklu
        /// doğru, güzergah, blok) her parçanın ortası verilir. Varsayılan: OrtaNoktaAl.
        /// </summary>
        public virtual IEnumerable<double[]> OrtaNoktalari()
        {
            var orta = OrtaNoktaAl();
            if (orta != null)
                yield return new double[] { orta.Value.X, orta.Value.Y };
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
