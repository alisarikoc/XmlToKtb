namespace XmlToKtb.Cizim
{
    /// <summary>Çizim ekranının genel ayarları.</summary>
    public static class CizimAyarlari
    {
        /// <summary>
        /// Proje ölçeği (1/ProjeOlcegi). Kağıttaki boyutu sabit olması gereken yazılar bu ölçeğe göre boyutlanır:
        /// kağıtta h mm olan yazının arazideki yüksekliği h * ProjeOlcegi / 1000 metredir.
        /// </summary>
        public static double ProjeOlcegi { get; set; } = 1000.0;

        /// <summary>Güzergah kilometre yazılarının kağıttaki yüksekliği (mm): 1/1000'de 2 m, 1/500'de 1 m eder.</summary>
        public const double KmYazisiYuksekligiMm = 2.0;
    }
}
