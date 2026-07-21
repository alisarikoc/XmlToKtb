namespace XmlToKtb.Projeksiyon
{
    /// <summary>Referans elipsoit parametreleri (büyük yarı eksen ve basıklık).</summary>
    public struct Elipsoid
    {
        public double A { get; }
        public double F { get; }
        public double E2 => F * (2 - F);
        public double EIkinciUssu => E2 / (1 - E2);

        public Elipsoid(double a, double f)
        {
            A = a;
            F = f;
        }

        /// <summary>WGS84 / GRS80 — pratik mühendislik hassasiyetinde birbirinin aynısı kabul edilir (ITRF96 de bu elipsoidi kullanır).</summary>
        public static readonly Elipsoid Wgs84 = new Elipsoid(6378137.0, 1.0 / 298.257223563);

        /// <summary>International 1924 (Hayford) — ED-50 datumunun elipsoidi.</summary>
        public static readonly Elipsoid Hayford1924 = new Elipsoid(6378388.0, 1.0 / 297.0);
    }
}
