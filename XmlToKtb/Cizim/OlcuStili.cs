using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace XmlToKtb.Cizim
{
    public enum OlcuOkTipi
    {
        [Description("Kapalı dolu ok")] KapaliDolu,
        [Description("Eğik çizgi")] EgikCizgi,
        [Description("Nokta")] Nokta,
        [Description("Açık ok")] Acik,
        [Description("Yok")] Yok
    }

    public enum OlcuYaziKonumu
    {
        [Description("Ölçü çizgisinin üstünde")] Ustte,
        [Description("Ölçü çizgisinin ortasında")] Ortada
    }

    public enum OlcuAciBirimi
    {
        Grad,
        Derece
    }

    /// <summary>
    /// Ölçü stili (AutoCAD DIMSTYLE). Boyutlar kağıt birimindedir (mm); çizimdeki boyları Genel Ölçek (DIMSCALE) ile
    /// çarpılarak bulunur: 1/1000 ölçekte Genel Ölçek 1 iken 2.5 yazı yüksekliği arazide 2.5 m olur.
    /// </summary>
    public class OlcuStili
    {
        [Category("Genel"), DisplayName("Ad")]
        public string Ad { get; set; } = "Standart";

        [Category("Genel"), DisplayName("Genel Ölçek (DIMSCALE)"), Description("Bütün boyutlar bu sayıyla çarpılır. 1/1000 ölçekli haritada 1, 1/500'de 0.5 kullanın.")]
        public double GenelOlcek { get; set; } = 1;

        [Category("Yazı"), DisplayName("Yazı Yüksekliği")]
        public double YaziYuksekligi { get; set; } = 2.5;

        [Category("Yazı"), DisplayName("Yazı Boşluğu (DIMGAP)"), Description("Yazı ile ölçü çizgisi arasındaki boşluk.")]
        public double YaziBoslugu { get; set; } = 0.625;

        [Category("Yazı"), DisplayName("Yazı Konumu")]
        public OlcuYaziKonumu YaziKonumu { get; set; } = OlcuYaziKonumu.Ustte;

        [Category("Birimler"), DisplayName("Ondalık Basamak")]
        public int OndalikBasamak { get; set; } = 2;

        [Category("Birimler"), DisplayName("Uzunluk Çarpanı (DIMLFAC)"), Description("Ölçülen uzunluk bu sayıyla çarpılarak yazılır (ör. m → cm için 100).")]
        public double UzunlukCarpani { get; set; } = 1;

        [Category("Birimler"), DisplayName("Ön Ek")]
        public string OnEk { get; set; } = "";

        [Category("Birimler"), DisplayName("Son Ek")]
        public string SonEk { get; set; } = "";

        [Category("Birimler"), DisplayName("Ondalık Ayracı")]
        public char OndalikAyraci { get; set; } = '.';

        [Category("Birimler"), DisplayName("Açı Birimi")]
        public OlcuAciBirimi AciBirimi { get; set; } = OlcuAciBirimi.Grad;

        [Category("Birimler"), DisplayName("Açı Ondalık Basamak")]
        public int AciOndalikBasamak { get; set; } = 4;

        [Category("Çizgiler ve Oklar"), DisplayName("Ok Tipi")]
        public OlcuOkTipi OkTipi { get; set; } = OlcuOkTipi.KapaliDolu;

        [Category("Çizgiler ve Oklar"), DisplayName("Ok Boyu")]
        public double OkBoyu { get; set; } = 2.5;

        [Category("Çizgiler ve Oklar"), DisplayName("Uzatma Çizgisi Aralığı (DIMEXO)"), Description("Ölçülen noktayla uzatma çizgisinin başı arasındaki boşluk.")]
        public double UzatmaAraligi { get; set; } = 0.625;

        [Category("Çizgiler ve Oklar"), DisplayName("Uzatma Çizgisi Taşması (DIMEXE)"), Description("Uzatma çizgisinin ölçü çizgisini geçen kısmı.")]
        public double UzatmaTasmasi { get; set; } = 1.25;

        [Category("Çizgiler ve Oklar"), DisplayName("Ölçü Çizgisi Taşması (DIMDLE)"), Description("Eğik çizgi ok tipinde ölçü çizgisinin uzatma çizgilerini geçen kısmı.")]
        public double OlcuCizgisiTasmasi { get; set; }

        /// <summary>Stilin ait olduğu çizimin stil listesi (Özellikler panelinde stil seçimi için).</summary>
        [Browsable(false)]
        public List<OlcuStili> Liste { get; set; }

        public override string ToString() => Ad;

        public OlcuStili Kopyala(string ad)
        {
            var kopya = (OlcuStili)MemberwiseClone();
            kopya.Ad = ad;
            return kopya;
        }

        /// <summary>Çizimdeki boy: kağıt boyutu × genel ölçek.</summary>
        public double Boy(double kagitBoyu) => kagitBoyu * (GenelOlcek > 0 ? GenelOlcek : 1);

        public string UzunlukMetni(double uzunluk)
        {
            var deger = (uzunluk * UzunlukCarpani).ToString("F" + Math.Max(0, Math.Min(8, OndalikBasamak)), CultureInfo.InvariantCulture);
            if (OndalikAyraci != '.')
                deger = deger.Replace('.', OndalikAyraci);
            return OnEk + deger + SonEk;
        }

        public string AciMetni(double radyan)
        {
            var deger = AciBirimi == OlcuAciBirimi.Grad ? radyan * 200 / Math.PI : radyan * 180 / Math.PI;
            var metin = deger.ToString("F" + Math.Max(0, Math.Min(8, AciOndalikBasamak)), CultureInfo.InvariantCulture);
            if (OndalikAyraci != '.')
                metin = metin.Replace('.', OndalikAyraci);
            return metin + (AciBirimi == OlcuAciBirimi.Grad ? "g" : "°");
        }

        /// <summary>
        /// Yeni çizimin hazır stilleri: Standart (kapalı oklar) ve Harita (eğik çizgi, yazı çizgi üstünde). Genel ölçek
        /// proje ölçeğinden gelir (1/1000 → 1).
        /// </summary>
        public static List<OlcuStili> HazirStiller(double projeOlcegi)
        {
            var olcek = projeOlcegi > 0 ? projeOlcegi / 1000.0 : 1;
            return new List<OlcuStili>
            {
                new OlcuStili { Ad = "Standart", GenelOlcek = olcek },
                new OlcuStili
                {
                    Ad = "Harita", GenelOlcek = olcek, YaziYuksekligi = 2, OkTipi = OlcuOkTipi.EgikCizgi, OkBoyu = 1.5,
                    OlcuCizgisiTasmasi = 1, UzatmaAraligi = 0.5, UzatmaTasmasi = 1, YaziBoslugu = 0.5
                }
            };
        }

        /// <summary>Stili olmayan (ya da stili silinmiş) ölçünün kullandığı stil.</summary>
        public static readonly OlcuStili Varsayilan = new OlcuStili();
    }

    /// <summary>Ölçü stili kullanan obje (ölçü, kılavuz çizgi).</summary>
    public interface IOlcuStilli
    {
        OlcuStili Stil { get; set; }
    }

    /// <summary>Özellikler panelinde objenin ölçü stilini, çizimdeki ölçü stilleri listesinden seçtirir.</summary>
    public class OlcuStiliAdiConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => (context?.Instance as IOlcuStilli)?.Stil?.Liste != null;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var liste = (context?.Instance as IOlcuStilli)?.Stil?.Liste;
            return new StandardValuesCollection(liste == null ? new string[0] : liste.Select(s => s.Ad).ToArray());
        }
    }
}
