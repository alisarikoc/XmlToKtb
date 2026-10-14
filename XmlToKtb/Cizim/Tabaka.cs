using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Çizim tabakası (layer). Objeler bir tabakaya aittir: tabaka kapalıysa objeleri çizilmez ve seçilemez,
    /// kilitliyse çizilir ama seçilemez; kendi rengi olmayan objeler tabakanın rengiyle çizilir.
    /// </summary>
    public class Tabaka
    {
        public string Ad;
        public Color Renk = Color.Black;
        public bool Gorunur = true;
        public bool Kilitli;

        /// <summary>Tabakanın ait olduğu çizimin tabaka listesi (Özellikler panelinde tabaka seçimi için).</summary>
        public List<Tabaka> Liste;

        public override string ToString()
        {
            return Ad;
        }
    }

    /// <summary>Özellikler panelinde objenin tabakasını, çizimdeki tabakaların listesinden seçtirir.</summary>
    public class TabakaAdiConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return (context?.Instance as CizimNesnesi)?.Tabaka?.Liste != null;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var liste = (context?.Instance as CizimNesnesi)?.Tabaka?.Liste;
            return new StandardValuesCollection(liste == null ? new string[0] : liste.Select(t => t.Ad).ToArray());
        }
    }
}
