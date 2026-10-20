using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace XmlToKtb.Cizim
{
    public enum CizimAraci
    {
        Sec,
        Cizgi,
        Kaydir,
        Kopyala,
        Dondur,
        Olcekle,
        Kes,
        DaireMerkezYaricap,
        Daire3Nokta,
        Daire2NoktaYaricap,
        DaireTegetIkiObje,
        DaireTegetUcObje,
        YuzeyCizgiEkle,
        YuzeyCizgiSil,
        YuzeyUcgenDondur,
        YuzeyNoktaEkle,
        YuzeyNoktaSil,
        YuzeyNoktaDuzelt,
        YuzeyNoktaTasi,
        /// <summary>Başka bir pencere (ör. güzergah editörü) ekrandan nokta bekliyor; tıklamalar NoktaTiklandi ile bildirilir.</summary>
        NoktaAl
    }

    /// <summary>
    /// Seçiliyken tutamaçlarından (ör. güzergahın uçları ve some noktaları) fareyle sürüklenebilen obje.
    /// </summary>
    public interface ITutamacli
    {
        /// <summary>Sürüklenebilir tutamaçların dünya koordinatları ({x, y}); tutamaç yoksa boş liste.</summary>
        IList<double[]> TutamaclariAl();
        /// <summary>Tutamacı verilen yere taşır; yeni yer geçerli bir geometri vermiyorsa hiçbir şeyi değiştirmeden false döner.</summary>
        bool TutamacTasi(int tutamac, double x, double y);
    }

    public enum TutamacAsamasi
    {
        Basladi,
        Surukleniyor,
        Bitti
    }

    /// <summary>Kaydır/Kopyala/Döndür/Ölçekle komutlarının çok adımlı akışındaki aşama.</summary>
    public enum DuzenlemeAsamasi
    {
        ObjeSeciliyor,
        TabanNoktasiBekleniyor,
        IkinciNoktaBekleniyor
    }

    /// <summary>Daire çizim komutlarının çok adımlı akışındaki aşama.</summary>
    public enum DaireAsamasi
    {
        GirdiBekleniyor,
        CozumSeciliyor
    }

    /// <summary>Çizim ekranında aynı anda birden fazlası aktif olabilen nokta yakalama (object snap) türleri.</summary>
    [Flags]
    public enum YakalamaModu
    {
        Yok = 0,
        Nokta = 1,
        Uc = 2,
        Orta = 4,
        Dik = 8,
        Merkez = 16,
        /// <summary>İki objenin (çizgi, yay, daire, çoklu doğru, güzergah...) kesiştiği nokta.</summary>
        Kesisim = 32
    }

    /// <summary>AutoCAD benzeri pan/zoom yapılabilen, çizim yapılabilen koordinat düzlemi.</summary>
    public class CizimPaneli : Panel
    {
        private const float SecimToleransPiksel = 5f;
        private const float YakalamaToleransPiksel = 10f;
        private const double ZoomCarpani = 1.15;

        public readonly List<CizimNesnesi> Nesneler = new List<CizimNesnesi>();
        public readonly CizimGorunum Gorunum = new CizimGorunum();

        private CizimAraci _aktifArac = CizimAraci.Sec;
        private YakalamaModu _aktifYakalamaModu = YakalamaModu.Yok;
        private PointF? _beklemedekiBaslangic;
        private PointF _sonFareEkranKonumu;
        private PointF? _yakalananDunyaNoktasi;
        private YakalamaModu _yakalananTuru;
        // Kesişime ya da orta noktaya yakalanıldığında o noktanın çift hassasiyetli koordinatı (diğerlerinde null).
        private double[] _yakalananTam;
        private bool _panSurukleniyor;
        private Point _panBaslangicEkran;
        private double _panBaslangicOfsetX;
        private double _panBaslangicOfsetY;

        private DuzenlemeAsamasi _duzenlemeAsamasi;
        private readonly List<CizimNesnesi> _duzenlenecekNesneler = new List<CizimNesnesi>();
        private PointF _tabanNoktasi;

        private DaireAsamasi _daireAsamasi;
        private readonly List<PointF> _daireNoktalar = new List<PointF>();
        private readonly List<CizimNesnesi> _daireObjeleri = new List<CizimNesnesi>();
        private List<DaireNesnesi> _daireAdaylari;

        private bool _secimKutusuBekleniyor;
        private bool _secimKutusuSurukleniyor;
        private Point _secimKutusuBaslangicEkran;

        public event EventHandler<CizimNesnesi> SecimDegisti;
        public event EventHandler<PointF> FareKonumuDegisti;
        public event EventHandler<CizimAraci> AracDegisti;
        /// <summary>Bir objenin koordinatı fareyle (uç tutamacı sürüklenerek) değiştirildiğinde tetiklenir.</summary>
        public event EventHandler NesnelerGuncellendi;
        /// <summary>Seç aracındayken Enter'a basıldığında ya da sağ tıklandığında tetiklenir: kullanıcı seçimini bitirdi.</summary>
        public event EventHandler SecimOnaylandi;
        /// <summary>Seç aracındayken Esc'ye basıldığında (seçim temizlenirken) tetiklenir.</summary>
        public event EventHandler SecimIptalEdildi;
        /// <summary>Objeler çizildikten sonra tetiklenir; ekranı kullanan form çizime ait olmayan (sanal) şekiller ekleyebilir.</summary>
        public event PaintEventHandler UstCizim;

        /// <summary>NoktaAl aracı açıkken ekrana sol tıklandığında, tıklanan yerin (yakalama uygulanmış) koordinatıyla tetiklenir.</summary>
        public event Action<double, double> NoktaTiklandi;
        /// <summary>NoktaAl aracından sağ tık ya da Esc ile çıkıldığında tetiklenir.</summary>
        public event EventHandler NoktaAlmaBitti;
        /// <summary>Tutamaçlı bir obje (ör. güzergah) tutamacından sürüklenirken, başlarken ve bırakılınca tetiklenir.</summary>
        public event Action<CizimNesnesi, TutamacAsamasi> TutamacSuruklendi;

        private CizimNesnesi _suruklenenNesne;
        private int _suruklenenTutamac = -1;

        // Fare konumunun çift hassasiyetli dünya koordinatı; bir noktaya yakalanmışsa o noktanın koordinatı.
        private void HassasNoktaAl(Point ekranNoktasi, out double x, out double y)
        {
            var yakalanan = DunyaNoktasiHesaplaVeYakala(ekranNoktasi);
            if (_yakalananDunyaNoktasi != null)
                TamKoordinatAl(yakalanan, out x, out y);
            else
                Gorunum.EkrandanDunyayaHassas(ekranNoktasi, Width, Height, out x, out y);
        }

        // Tıklanan yer seçili (etkin) bir tutamaçlı objenin tutamacıysa sürüklemeyi başlatır.
        private bool NesneTutamaciBaslat(Point ekranNoktasi)
        {
            for (var i = Nesneler.Count - 1; i >= 0; i--)
            {
                if (!(Nesneler[i] is ITutamacli tutamacli) || !Secilebilir(Nesneler[i]))
                    continue;

                var tutamaclar = tutamacli.TutamaclariAl();
                for (var t = 0; t < tutamaclar.Count; t++)
                {
                    if (!TutamacaIsabetEttiMi(tutamaclar[t][0], tutamaclar[t][1], ekranNoktasi))
                        continue;
                    _suruklenenNesne = Nesneler[i];
                    _suruklenenTutamac = t;
                    TutamacSuruklendi?.Invoke(_suruklenenNesne, TutamacAsamasi.Basladi);
                    return true;
                }
            }
            return false;
        }

        private const float TutamacToleransPiksel = 6f;
        /// <summary>İki uç noktanın "aynı nokta" sayılacağı dünya mesafesi.</summary>
        public const double UcEsitlikToleransi = 1e-3;

        /// <summary>Açıksa, Seç aracında seçili çizginin uç tutamacı sürüklenerek çizgi sündürülebilir.</summary>
        public bool TutamacSuruklemeAcik { get; set; }

        /// <summary>Açıksa, bir çizginin ucu taşındığında aynı noktadaki diğer çizgi uçları da birlikte taşınır.</summary>
        public bool OrtakUclariBirlikteTasi { get; set; }

        /// <summary>
        /// Verilirse, ortak uçlar yalnızca aynı gruptaki (ör. aynı kesite ait) çizgiler arasında birlikte taşınır;
        /// üst üste çizilen farklı grupların çakışan uçları birbirini etkilemez.
        /// </summary>
        public Func<CizgiNesnesi, object> OrtakUcGrubu { get; set; }

        private bool AyniUcGrubundaMi(CizgiNesnesi a, CizgiNesnesi b)
        {
            return OrtakUcGrubu == null || Equals(OrtakUcGrubu(a), OrtakUcGrubu(b));
        }

        /// <summary>Açıksa, fareyle verilen (bir uca yakalanmamış) yeni noktalar milimetreye yuvarlanır.</summary>
        public bool YeniNoktalariMilimetreyeYuvarla { get; set; }

        /// <summary>Açıksa (0,0) noktası renkli bir artı işaretiyle gösterilir ve Nokta/Uç yakalamada aday olur.</summary>
        public bool ReferansNoktasiGoster { get; set; }

        /// <summary>Açıksa çizimdeki değişiklikler (ekleme, silme, çizgi koordinatı) geri al/ilerle için kaydedilir.</summary>
        public bool GeriAlAcik { get; set; }

        /// <summary>Geri al/ilerle geçmişi değiştiğinde (ör. menü düğmelerini güncellemek için) tetiklenir.</summary>
        public event EventHandler GecmisDegisti;

        public bool GeriAlinabilir => _geriAlYigini.Count > 0;
        public bool Ilerlenebilir => _ilerleYigini.Count > 0;

        /// <summary>Geri alınabilecek en fazla adım sayısı (0: sınırsız). Sınır aşılınca en eski adım silinir.</summary>
        public int GeriAlSiniri { get; set; }

        /// <summary>
        /// Çizimin bir andaki hali için bir objenin kaydı: obje, tabakası, rengi, koordinat ve boyutları (Degerler),
        /// metinleri ve (güzergah/yüzey gibi karmaşık objelerde) bir kopyası.
        /// </summary>
        internal class DurumKaydi
        {
            public CizimNesnesi Nesne;
            public Tabaka Tabaka;
            public Color? Renk;
            public double[] Degerler;
            public string[] Metinler;
            public object Kopya;
        }

        private List<DurumKaydi> _sonDurum = new List<DurumKaydi>();
        // En sondaki eleman en yeni adımdır.
        private readonly List<List<DurumKaydi>> _geriAlYigini = new List<List<DurumKaydi>>();
        private readonly List<List<DurumKaydi>> _ilerleYigini = new List<List<DurumKaydi>>();

        // Objenin geri al için saklanan sayısal değerleri.
        private static double[] DegerleriAl(CizimNesnesi nesne)
        {
            switch (nesne)
            {
                case CizgiNesnesi c: return new[] { c.BaslangicX, c.BaslangicY, c.BaslangicZ, c.BitisX, c.BitisY, c.BitisZ };
                case NoktaNesnesi n: return new[] { n.X, n.Y, n.Z };
                case DaireNesnesi d: return new[] { d.MerkezX, d.MerkezY, d.MerkezZ, d.Yaricap };
                case YayNesnesi a: return new[] { a.MerkezX, a.MerkezY, a.MerkezZ, a.Yaricap, a.BaslangicAcisi, a.BitisAcisi };
                case YaziNesnesi y: return new[] { y.KonumX, y.KonumY, y.KonumZ, y.Yukseklik, y.Donus, (double)y.HizaYatay, (double)y.HizaDikey };
                case SpiralNesnesi s:
                    return new[] { s.BaslangicX, s.BaslangicY, s.BaslangicZ, s.BitisX, s.BitisY, s.BitisZ, s.PiX, s.PiY, s.PiZ, s.YaricapBaslangic, s.YaricapBitis, s.Uzunluk, (double)s.Yon };
                case CokluDogruNesnesi cd:
                {
                    var degerler = new List<double> { cd.Kapali ? 1 : 0 };
                    foreach (var k in cd.Koseler)
                    {
                        degerler.Add(k.X); degerler.Add(k.Y); degerler.Add(k.Z); degerler.Add(k.Bulge);
                    }
                    return degerler.ToArray();
                }
                case BlokNesnesi b:
                {
                    var degerler = new List<double> { b.KonumX, b.KonumY };
                    foreach (var oge in b.Ogeler)
                    {
                        var alt = DegerleriAl(oge) ?? new double[0];
                        degerler.Add(alt.Length);
                        degerler.AddRange(alt);
                    }
                    return degerler.ToArray();
                }
                case GuzergahNesnesi g:
                {
                    // Karşılaştırma için güzergahın tanımı; geri yükleme kopyadan yapılır.
                    var degerler = new List<double> { g.BaslangicKm, g.BaslangicX, g.BaslangicY, g.BaslangicAcisi, g.Dusey.Count, g.Dever.Satirlar.Count };
                    degerler.Add(g.KopukParcalar.Count);
                    foreach (var p in g.TumParcalar)
                        degerler.AddRange(new[] { (double)p.Tip, p.Uzunluk, p.YaricapBaslangic, p.YaricapBitis, (double)p.Yon, p.KirikAcisi, p.Sabit ? 1 : 0, p.SabitX, p.SabitY, p.SabitAci });
                    foreach (var d in g.Dusey)
                        degerler.AddRange(new[] { d.Km, d.Kot, (double)d.Tip, d.Uzunluk, d.Yaricap });
                    return degerler.ToArray();
                }
                case YuzeyNesnesi yuzey:
                    return new double[] { yuzey.Surum, yuzey.X.Count };
                case OlcuNesnesi o:
                    return new[] { (double)o.Tur, o.AX, o.AY, o.BX, o.BY, o.CX, o.CY, o.DX, o.DY, o.Aci };
                case KilavuzNesnesi kl:
                {
                    var degerler = new List<double> { kl.Inis ? 1 : 0 };
                    foreach (var k in kl.Koseler)
                    {
                        degerler.Add(k[0]);
                        degerler.Add(k[1]);
                    }
                    return degerler.ToArray();
                }
                default:
                    return null;
            }
        }

        private static void DegerleriYukle(CizimNesnesi nesne, double[] d)
        {
            if (d == null)
                return;
            switch (nesne)
            {
                case CizgiNesnesi c:
                    c.BaslangicX = d[0]; c.BaslangicY = d[1]; c.BaslangicZ = d[2]; c.BitisX = d[3]; c.BitisY = d[4]; c.BitisZ = d[5];
                    break;
                case NoktaNesnesi n:
                    n.X = d[0]; n.Y = d[1]; n.Z = d[2];
                    break;
                case DaireNesnesi dd:
                    dd.MerkezX = d[0]; dd.MerkezY = d[1]; dd.MerkezZ = d[2]; dd.Yaricap = d[3];
                    break;
                case YayNesnesi a:
                    a.MerkezX = d[0]; a.MerkezY = d[1]; a.MerkezZ = d[2]; a.Yaricap = d[3]; a.BaslangicAcisi = d[4]; a.BitisAcisi = d[5];
                    break;
                case YaziNesnesi y:
                    y.KonumX = d[0]; y.KonumY = d[1]; y.KonumZ = d[2]; y.Yukseklik = d[3]; y.Donus = d[4];
                    y.HizaYatay = (YatayHizalama)(int)d[5]; y.HizaDikey = (DikeyHizalama)(int)d[6];
                    break;
                case SpiralNesnesi s:
                    s.BaslangicX = d[0]; s.BaslangicY = d[1]; s.BaslangicZ = d[2]; s.BitisX = d[3]; s.BitisY = d[4]; s.BitisZ = d[5];
                    s.PiX = d[6]; s.PiY = d[7]; s.PiZ = d[8]; s.YaricapBaslangic = d[9]; s.YaricapBitis = d[10]; s.Uzunluk = d[11]; s.Yon = (DonusYonu)(int)d[12];
                    break;
                case CokluDogruNesnesi cd:
                    cd.Kapali = d[0] != 0;
                    cd.Koseler.Clear();
                    for (var i = 1; i + 3 < d.Length; i += 4)
                        cd.Koseler.Add(new CokluDogruKosesi { X = d[i], Y = d[i + 1], Z = d[i + 2], Bulge = d[i + 3] });
                    break;
                case KilavuzNesnesi kl:
                    kl.Inis = d[0] != 0;
                    kl.Koseler.Clear();
                    for (var i = 1; i + 1 < d.Length; i += 2)
                        kl.Koseler.Add(new[] { d[i], d[i + 1] });
                    break;
                case OlcuNesnesi o:
                    o.Tur = (OlcuTuru)(int)d[0];
                    o.AX = d[1]; o.AY = d[2]; o.BX = d[3]; o.BY = d[4]; o.CX = d[5]; o.CY = d[6]; o.DX = d[7]; o.DY = d[8]; o.Aci = d[9];
                    break;
                case BlokNesnesi b:
                {
                    b.KonumX = d[0]; b.KonumY = d[1];
                    var yer = 2;
                    foreach (var oge in b.Ogeler)
                    {
                        if (yer >= d.Length)
                            break;
                        var uzunluk = (int)d[yer++];
                        var alt = new double[uzunluk];
                        Array.Copy(d, yer, alt, 0, uzunluk);
                        yer += uzunluk;
                        DegerleriYukle(oge, uzunluk > 0 ? alt : null);
                    }
                    break;
                }
            }
        }

        private static string[] MetinleriAl(CizimNesnesi nesne)
        {
            switch (nesne)
            {
                case YaziNesnesi y: return new[] { y.Metin };
                case NoktaNesnesi n: return new[] { n.NoktaAdi, n.Kod, n.Pafta };
                case GuzergahNesnesi g: return new[] { g.Ad };
                case OlcuNesnesi o: return new[] { o.Metin, o.StilAdi };
                case KilavuzNesnesi kl: return new[] { kl.Metin, kl.StilAdi };
                case BlokNesnesi b: return b.Ogeler.OfType<YaziNesnesi>().Select(y => y.Metin).ToArray();
                default: return null;
            }
        }

        private static void MetinleriYukle(CizimNesnesi nesne, string[] m)
        {
            if (m == null)
                return;
            switch (nesne)
            {
                case YaziNesnesi y: y.Metin = m[0]; break;
                case NoktaNesnesi n: n.NoktaAdi = m[0]; n.Kod = m[1]; n.Pafta = m[2]; break;
                case OlcuNesnesi o: o.Metin = m[0]; o.StilAdi = m[1]; break;
                case KilavuzNesnesi kl: kl.Metin = m[0]; kl.StilAdi = m[1]; break;
                case BlokNesnesi b:
                {
                    var yazilar = b.Ogeler.OfType<YaziNesnesi>().ToList();
                    for (var i = 0; i < yazilar.Count && i < m.Length; i++)
                        yazilar[i].Metin = m[i];
                    break;
                }
            }
        }

        private List<DurumKaydi> DurumAl()
        {
            // Yüzey koordinatları yalnızca değiştiğinde kopyalanır; değişmemişse önceki kaydın kopyası kullanılır.
            var oncekiler = new Dictionary<CizimNesnesi, DurumKaydi>();
            foreach (var kayit in _sonDurum)
                oncekiler[kayit.Nesne] = kayit;

            var durum = new List<DurumKaydi>(Nesneler.Count);
            foreach (var nesne in Nesneler)
            {
                var kayit = new DurumKaydi { Nesne = nesne, Tabaka = nesne.Tabaka, Renk = nesne.OzelRenk, Degerler = DegerleriAl(nesne), Metinler = MetinleriAl(nesne) };
                if (nesne is GuzergahNesnesi g)
                {
                    kayit.Kopya = oncekiler.TryGetValue(nesne, out var eski) && DegerlerAyniMi(eski, kayit) ? eski.Kopya : g.Kopyala();
                }
                else if (nesne is YuzeyNesnesi yuzey)
                {
                    kayit.Kopya = oncekiler.TryGetValue(nesne, out var eski) && DegerlerAyniMi(eski, kayit)
                        ? eski.Kopya
                        : new[] { new List<double>(yuzey.X), new List<double>(yuzey.Y), new List<double>(yuzey.Z) };
                }
                durum.Add(kayit);
            }
            return durum;
        }

        private static bool DegerlerAyniMi(DurumKaydi a, DurumKaydi b)
        {
            if (!ReferenceEquals(a.Tabaka, b.Tabaka) || a.Renk != b.Renk)
                return false;
            if ((a.Degerler == null) != (b.Degerler == null) || (a.Degerler != null && (a.Degerler.Length != b.Degerler.Length || !a.Degerler.SequenceEqual(b.Degerler))))
                return false;
            if ((a.Metinler == null) != (b.Metinler == null) || (a.Metinler != null && !a.Metinler.SequenceEqual(b.Metinler)))
                return false;
            return true;
        }

        private static bool DurumlarAyniMi(List<DurumKaydi> a, List<DurumKaydi> b)
        {
            if (a.Count != b.Count)
                return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!ReferenceEquals(a[i].Nesne, b[i].Nesne) || !DegerlerAyniMi(a[i], b[i]))
                    return false;
            }
            return true;
        }

        private void DurumYukle(List<DurumKaydi> durum)
        {
            // Yarım kalmış çizim/düzenleme adımı eski objelere bağlı kalmasın diye araç sıfırlanır.
            if (AktifArac == CizimAraci.Sec)
            {
                _tutamacSurukleniyor = false;
                _suruklenenUclar.Clear();
            }
            else
            {
                AktifArac = CizimAraci.Sec;
            }

            Nesneler.Clear();
            foreach (var kayit in durum)
            {
                var nesne = kayit.Nesne;
                nesne.Secili = false;
                nesne.Tabaka = kayit.Tabaka;
                nesne.OzelRenk = kayit.Renk;
                if (nesne is GuzergahNesnesi g && kayit.Kopya is GuzergahNesnesi kopya)
                {
                    g.DurumunuYukle(kopya);
                }
                else if (nesne is YuzeyNesnesi yuzey && kayit.Kopya is List<double>[] koordinatlar)
                {
                    // Yüzeyde yalnızca koordinatlar (taşıma, döndürme, ölçek) geri alınır; üçgen düzenlemelerinin
                    // kendi geri alı vardır.
                    if (koordinatlar[0].Count == yuzey.X.Count)
                    {
                        for (var i = 0; i < yuzey.X.Count; i++)
                        {
                            yuzey.X[i] = koordinatlar[0][i];
                            yuzey.Y[i] = koordinatlar[1][i];
                            yuzey.Z[i] = koordinatlar[2][i];
                        }
                        yuzey.KoordinatlarDegisti();
                    }
                }
                else
                {
                    DegerleriYukle(nesne, kayit.Degerler);
                }
                MetinleriYukle(nesne, kayit.Metinler);
                Nesneler.Add(nesne);
            }

            // Yükleme yüzey sayaçlarını değiştirebildiği için son durum yeniden alınır.
            _sonDurum = durum;
            _sonDurum = DurumAl();
            _yakalananDunyaNoktasi = null;
            SecimBildir();
            NesnelerGuncellendi?.Invoke(this, EventArgs.Empty);
            GecmisDegisti?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        /// <summary>
        /// Çizim son kayıttan beri değiştiyse önceki hali geri al geçmişine ekler. Çizimi değiştiren her işlemden
        /// sonra çağrılır; değişiklik yoksa bir şey yapmaz.
        /// </summary>
        public void DegisiklikKaydet()
        {
            if (!GeriAlAcik)
                return;

            // Tabakası belli olmayan yeni objeler aktif tabakaya şimdi girer; çizim sırasında girselerdi bu, geri al
            // geçmişinde görünmez ayrı bir adım olurdu (ilk Geri Al hiçbir şey yapmıyor gibi görünürdü).
            if (TabakalarAcik)
                foreach (var nesne in Nesneler)
                    if (nesne.Tabaka == null)
                        nesne.Tabaka = AktifTabaka;

            var durum = DurumAl();
            if (DurumlarAyniMi(durum, _sonDurum))
                return;

            _geriAlYigini.Add(_sonDurum);
            if (GeriAlSiniri > 0 && _geriAlYigini.Count > GeriAlSiniri)
                _geriAlYigini.RemoveAt(0);
            _ilerleYigini.Clear();
            _sonDurum = durum;
            GecmisDegisti?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Kullanıcıya ait olmayan (programın yaptığı) bir düzeltmeyi geri al adımı saymadan son durum olarak kabul eder.</summary>
        public void SonDurumuGuncelle()
        {
            if (GeriAlAcik)
                _sonDurum = DurumAl();
        }

        /// <summary>Geçmişi siler; çizimin o anki hali başlangıç kabul edilir (ör. ekrana başka bir çizim yüklendiğinde).</summary>
        public void GecmisiTemizle()
        {
            _geriAlYigini.Clear();
            _ilerleYigini.Clear();
            _sonDurum = DurumAl();
            GecmisDegisti?.Invoke(this, EventArgs.Empty);
        }

        public void GeriAl()
        {
            if (_geriAlYigini.Count == 0)
                return;

            // Geri almadan önce yapılmış ama kaydedilmemiş değişiklik varsa önce o kaydedilir.
            DegisiklikKaydet();
            var onceki = _geriAlYigini[_geriAlYigini.Count - 1];
            _geriAlYigini.RemoveAt(_geriAlYigini.Count - 1);
            _ilerleYigini.Add(_sonDurum);
            DurumYukle(onceki);
        }

        public void Ilerle()
        {
            if (_ilerleYigini.Count == 0)
                return;

            var sonraki = _ilerleYigini[_ilerleYigini.Count - 1];
            _ilerleYigini.RemoveAt(_ilerleYigini.Count - 1);
            _geriAlYigini.Add(_sonDurum);
            if (GeriAlSiniri > 0 && _geriAlYigini.Count > GeriAlSiniri)
                _geriAlYigini.RemoveAt(0);
            DurumYukle(sonraki);
        }
        /// <summary>
        /// Bir çizim belgesinin panelden çıkarılmış hali: objeleri, tabakaları, görünümü ve geri al geçmişi. Çok
        /// belgeli ekranda sekmeler arasında geçerken panel tek kalır; belgeler bununla saklanıp yüklenir.
        /// </summary>
        public sealed class BelgeDurumu
        {
            internal readonly List<CizimNesnesi> Nesneler = new List<CizimNesnesi>();
            internal readonly List<Tabaka> Tabakalar = new List<Tabaka>();
            internal Tabaka AktifTabaka;
            internal double Olcek = 1, OfsetX, OfsetY;
            internal List<DurumKaydi> SonDurum;
            internal readonly List<List<DurumKaydi>> GeriAl = new List<List<DurumKaydi>>();
            internal readonly List<List<DurumKaydi>> Ilerle = new List<List<DurumKaydi>>();
            internal readonly List<OlcuStili> OlcuStilleri = new List<OlcuStili>();
            internal OlcuStili AktifOlcuStili;

            /// <summary>Belgedeki obje sayısı (panelde açık değilken).</summary>
            public int NesneSayisi => Nesneler.Count;
        }

        /// <summary>Paneldeki çizimi belge durumu olarak çıkarır (panel değişmez; ardından başka bir belge yüklenir).</summary>
        public BelgeDurumu BelgeDurumunuAl()
        {
            var d = new BelgeDurumu
            {
                AktifTabaka = _aktifTabaka,
                Olcek = Gorunum.Olcek,
                OfsetX = Gorunum.OfsetX,
                OfsetY = Gorunum.OfsetY,
                SonDurum = _sonDurum
            };
            d.Nesneler.AddRange(Nesneler);
            d.Tabakalar.AddRange(Tabakalar);
            d.GeriAl.AddRange(_geriAlYigini);
            d.Ilerle.AddRange(_ilerleYigini);
            d.OlcuStilleri.AddRange(OlcuStilleri);
            d.AktifOlcuStili = _aktifOlcuStili;
            return d;
        }

        /// <summary>
        /// Belgeyi panele yükler: yarım kalan araç kapatılır, objeler, tabakalar, görünüm ve geri al geçmişi belgenin
        /// olur. Boş (yeni) bir belge için new BelgeDurumu() verilir.
        /// </summary>
        public void BelgeDurumunuYukle(BelgeDurumu d)
        {
            AktifArac = CizimAraci.Sec;
            _tutamacSurukleniyor = false;
            _suruklenenUclar.Clear();
            DuzenlenenYuzey = null;
            _yakalananDunyaNoktasi = null;

            Nesneler.Clear();
            Nesneler.AddRange(d.Nesneler);
            Tabakalar.Clear();
            Tabakalar.AddRange(d.Tabakalar);
            foreach (var tabaka in Tabakalar)
                tabaka.Liste = Tabakalar;
            _aktifTabaka = d.AktifTabaka;
            OlcuStilleri.Clear();
            OlcuStilleri.AddRange(d.OlcuStilleri);
            foreach (var stil in OlcuStilleri)
                stil.Liste = OlcuStilleri;
            _aktifOlcuStili = d.AktifOlcuStili;
            Gorunum.Olcek = d.Olcek;
            Gorunum.OfsetX = d.OfsetX;
            Gorunum.OfsetY = d.OfsetY;

            _geriAlYigini.Clear();
            _geriAlYigini.AddRange(d.GeriAl);
            _ilerleYigini.Clear();
            _ilerleYigini.AddRange(d.Ilerle);
            _sonDurum = d.SonDurum ?? DurumAl();

            SecimBildir();
            NesnelerGuncellendi?.Invoke(this, EventArgs.Empty);
            GecmisDegisti?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        /// <summary>Açıksa objeler tabakalara ayrılır: yeni objeler aktif tabakaya girer, kapalı tabakalar çizilmez.</summary>
        public bool TabakalarAcik { get; set; }

        public readonly List<Tabaka> Tabakalar = new List<Tabaka>();

        private Tabaka _aktifTabaka;

        /// <summary>Yeni çizilen objelerin gireceği tabaka. Hiç tabaka yoksa "0" tabakası oluşturulur.</summary>
        public Tabaka AktifTabaka
        {
            get
            {
                if (_aktifTabaka == null || !Tabakalar.Contains(_aktifTabaka))
                    _aktifTabaka = Tabakalar.Count > 0 ? Tabakalar[0] : TabakaBulVeyaEkle("0", Color.Black, true, false);
                return _aktifTabaka;
            }
            set => _aktifTabaka = value;
        }

        /// <summary>Çizimin ölçü stilleri (AutoCAD DIMSTYLE). Boşsa ilk kullanımda hazır stiller eklenir.</summary>
        public readonly List<OlcuStili> OlcuStilleri = new List<OlcuStili>();

        private OlcuStili _aktifOlcuStili;

        /// <summary>Yeni ölçülerin kullandığı stil. Hiç stil yoksa hazır stiller (Standart, Harita) eklenir.</summary>
        public OlcuStili AktifOlcuStili
        {
            get
            {
                if (OlcuStilleri.Count == 0)
                {
                    foreach (var stil in OlcuStili.HazirStiller(CizimAyarlari.ProjeOlcegi))
                    {
                        stil.Liste = OlcuStilleri;
                        OlcuStilleri.Add(stil);
                    }
                }
                if (_aktifOlcuStili == null || !OlcuStilleri.Contains(_aktifOlcuStili))
                    _aktifOlcuStili = OlcuStilleri[0];
                return _aktifOlcuStili;
            }
            set => _aktifOlcuStili = value;
        }

        /// <summary>
        /// Verilen stilin çizimdeki karşılığı: aynı adda stil varsa o (ayarlarına dokunmadan), yoksa stilin bir kopyası
        /// eklenir. Başka çizimden yapıştırılan ya da dosyadan okunan ölçüler için.
        /// </summary>
        public OlcuStili OlcuStiliBulVeyaEkle(OlcuStili kaynak)
        {
            if (kaynak == null)
                return null;
            var stil = OlcuStilleri.Find(s => string.Equals(s.Ad, kaynak.Ad, StringComparison.OrdinalIgnoreCase));
            if (stil != null)
                return stil;
            stil = OlcuStilleri.Contains(kaynak) ? kaynak : kaynak.Kopyala(kaynak.Ad);
            stil.Liste = OlcuStilleri;
            OlcuStilleri.Add(stil);
            return stil;
        }

        /// <summary>Verilen adda tabaka varsa onu döner (ayarlarına dokunmadan); yoksa verilen ayarlarla ekler.</summary>
        public Tabaka TabakaBulVeyaEkle(string ad, Color renk, bool gorunur, bool kilitli)
        {
            var tabaka = Tabakalar.Find(t => string.Equals(t.Ad, ad, StringComparison.OrdinalIgnoreCase));
            if (tabaka != null)
                return tabaka;

            tabaka = new Tabaka { Ad = ad, Renk = renk, Gorunur = gorunur, Kilitli = kilitli, Liste = Tabakalar };
            Tabakalar.Add(tabaka);
            return tabaka;
        }

        /// <summary>Tabaka ayarları (görünürlük, kilit, renk) değiştikten sonra çağrılır: artık seçilemeyen objelerin seçimi kalkar, çizim yenilenir.</summary>
        public void TabakalarDegisti()
        {
            var secimDegisti = false;
            foreach (var nesne in Nesneler)
            {
                if (!nesne.Secili || Secilebilir(nesne))
                    continue;
                nesne.Secili = false;
                secimDegisti = true;
            }
            if (secimDegisti)
                SecimBildir();
            Invalidate();
        }

        private static bool Gorunur(CizimNesnesi nesne)
        {
            return nesne.Tabaka == null || nesne.Tabaka.Gorunur;
        }

        // Kapalı ya da kilitli tabakadaki obje fareyle seçilemez ve düzenlenemez.
        private static bool Secilebilir(CizimNesnesi nesne)
        {
            return nesne.Tabaka == null || (nesne.Tabaka.Gorunur && !nesne.Tabaka.Kilitli);
        }

        /// <summary>Yüzey düzenleme araçlarının (çizgi/nokta ekle-sil vb.) üzerinde çalıştığı yüzey.</summary>
        public YuzeyNesnesi DuzenlenenYuzey { get; set; }

        private const float YuzeySecimToleransPiksel = 8f;
        // İki tıklamalı yüzey araçlarında (Çizgi Ekle, Nokta Taşı) ilk tıklamada seçilen noktanın indeksi; yoksa -1.
        private int _yuzeyIlkNokta = -1;

        public static bool YuzeyAraciMi(CizimAraci arac)
        {
            return arac >= CizimAraci.YuzeyCizgiEkle && arac <= CizimAraci.YuzeyNoktaTasi;
        }

        private bool _tutamacSurukleniyor;
        // Sürüklenen uçlar: çizgi ve "başlangıç ucu mu" bilgisi.
        private readonly List<KeyValuePair<CizgiNesnesi, bool>> _suruklenenUclar = new List<KeyValuePair<CizgiNesnesi, bool>>();
        private double _beklemedekiBaslangicX;
        private double _beklemedekiBaslangicY;

        public CizimAraci AktifArac
        {
            get => _aktifArac;
            set
            {
                if (_aktifArac == value)
                    return;
                _aktifArac = value;
                _beklemedekiBaslangic = null;
                _yuzeyIlkNokta = -1;
                _duzenlenecekNesneler.Clear();
                _duzenlemeAsamasi = DuzenlemeAsamasi.ObjeSeciliyor;
                _daireNoktalar.Clear();
                _daireObjeleri.Clear();
                _daireAdaylari = null;
                _daireAsamasi = DaireAsamasi.GirdiBekleniyor;

                if (DuzenlemeAraciMi(value))
                {
                    foreach (var nesne in Nesneler)
                    {
                        if (nesne.Secili)
                            _duzenlenecekNesneler.Add(nesne);
                    }
                    if (_duzenlenecekNesneler.Count > 0)
                        _duzenlemeAsamasi = DuzenlemeAsamasi.TabanNoktasiBekleniyor;
                }

                ImleciGuncelle();
                AracDegisti?.Invoke(this, _aktifArac);
                Invalidate();
            }
        }

        private static bool DuzenlemeAraciMi(CizimAraci arac)
        {
            return arac == CizimAraci.Kaydir || arac == CizimAraci.Kopyala || arac == CizimAraci.Dondur || arac == CizimAraci.Olcekle;
        }

        private static bool DaireAraciMi(CizimAraci arac)
        {
            return arac == CizimAraci.DaireMerkezYaricap || arac == CizimAraci.Daire3Nokta
                || arac == CizimAraci.Daire2NoktaYaricap || arac == CizimAraci.DaireTegetIkiObje
                || arac == CizimAraci.DaireTegetUcObje;
        }

        private static bool DaireTegetObjesiOlabilirMi(CizimNesnesi nesne)
        {
            return nesne is CizgiNesnesi || nesne is YayNesnesi || nesne is DaireNesnesi;
        }

        /// <summary>Aktif nokta yakalama türlerinin bit birleşimi. Birden fazlası aynı anda açık olabilir.</summary>
        public YakalamaModu AktifYakalamaModu
        {
            get => _aktifYakalamaModu;
            set
            {
                if (_aktifYakalamaModu == value)
                    return;
                _aktifYakalamaModu = value;
                _yakalananDunyaNoktasi = null;
                Invalidate();
            }
        }

        // Seçim imleci (kare): açık zeminde siyah, koyu zeminde beyaz.
        private readonly Cursor _kareImlecSiyah, _kareImlecBeyaz;
        private Cursor SecimImleci => CizimBaglami.KoyuMu(BackColor) ? _kareImlecBeyaz : _kareImlecSiyah;

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            // Kare imleç gösteriliyorsa zemine uygun renktekiyle değişir.
            if (_kareImlecSiyah != null)
                ImleciGuncelle();
        }

        // NoktaAl aracında obje mi seçiliyor (kare imleç) yoksa nokta mı veriliyor (artı imleç)?
        private bool _noktaAlNesneSecer;
        private bool _secimBekleniyor;

        /// <summary>
        /// Bir komut, devam etmek için obje seçimi bekliyor mu? Bekliyorsa Seç aracında da imleç kare olur
        /// (boş ekrandaki ok yerine).
        /// </summary>
        public bool SecimBekleniyor
        {
            get => _secimBekleniyor;
            set
            {
                _secimBekleniyor = value;
                ImleciGuncelle();
            }
        }

        /// <summary>
        /// İmleç: boş ekranda (Seç aracı) ok; obje seçilmesi beklenirken (yüzey çizgi sil/üçgen döndür, kes, teğet
        /// daire objeleri, kaydır/kopyala/döndür/ölçekle için obje seçimi, obje seçen dış komutlar) kare; nokta
        /// verilirken artı.
        /// </summary>
        private void ImleciGuncelle()
        {
            var arac = _aktifArac;
            var secim = arac == CizimAraci.YuzeyCizgiSil || arac == CizimAraci.YuzeyUcgenDondur || arac == CizimAraci.Kes
                        || arac == CizimAraci.DaireTegetIkiObje || arac == CizimAraci.DaireTegetUcObje
                        || (DuzenlemeAraciMi(arac) && _duzenlemeAsamasi == DuzenlemeAsamasi.ObjeSeciliyor)
                        || (arac == CizimAraci.NoktaAl && _noktaAlNesneSecer);
            secim |= arac == CizimAraci.Sec && _secimBekleniyor;
            Cursor = secim ? SecimImleci : arac == CizimAraci.Sec ? Cursors.Default : Cursors.Cross;
        }

        /// <summary>
        /// Dış bir komut için NoktaAl aracını açar. Komut ekrandan obje seçiyorsa imleç kare, nokta istiyorsa artı olur.
        /// </summary>
        public void NoktaAlmayaBasla(bool nesneSecilecek)
        {
            _noktaAlNesneSecer = nesneSecilecek;
            AktifArac = CizimAraci.NoktaAl;
            ImleciGuncelle();
        }
        public CizimPaneli()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            TabStop = true;
            _kareImlecSiyah = KareImleciOlustur(MilimetreyiPikselYap(2.0), Color.Black);
            _kareImlecBeyaz = KareImleciOlustur(MilimetreyiPikselYap(2.0), Color.White);
            Cursor = Cursors.Default;
            KeyDown += CizimPaneli_KeyDown;
        }

        // Enter normalde formun varsayılan düğmesine gider; seçim onayı için panele gelmesi sağlanır.
        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Enter || base.IsInputKey(keyData);
        }

        private void CizimPaneli_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && AktifArac == CizimAraci.Sec)
            {
                SeciliNesneleriSil();
                DegisiklikKaydet();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && AktifArac == CizimAraci.Sec)
            {
                SecimOnaylandi?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                EscUygula();
                e.Handled = true;
            }
        }

        private void EscUygula()
        {
            if (AktifArac == CizimAraci.Sec)
            {
                var degisiklikVar = false;
                foreach (var nesne in Nesneler)
                {
                    if (!nesne.Secili)
                        continue;
                    nesne.Secili = false;
                    degisiklikVar = true;
                }

                _secimKutusuBekleniyor = false;
                _secimKutusuSurukleniyor = false;

                if (degisiklikVar)
                    SecimBildir();
                SecimIptalEdildi?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
            else
            {
                foreach (var nesne in _duzenlenecekNesneler)
                    nesne.Secili = false;
                foreach (var nesne in _daireObjeleri)
                    nesne.Secili = false;
                var noktaAliniyordu = AktifArac == CizimAraci.NoktaAl;
                AktifArac = CizimAraci.Sec;
                if (noktaAliniyordu)
                    NoktaAlmaBitti?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SeciliNesneleriSil()
        {
            var silinecekler = new List<CizimNesnesi>();
            foreach (var nesne in Nesneler)
            {
                if (nesne.Secili)
                    silinecekler.Add(nesne);
            }

            if (silinecekler.Count == 0)
                return;

            foreach (var nesne in silinecekler)
                Nesneler.Remove(nesne);

            // Güzergahı silinen kilometre etiketleri de silinir (bağlı oldukları güzergah olmadan anlamsızdırlar).
            Nesneler.RemoveAll(n => n is GuzergahEtiketNesnesi etiket && !Nesneler.Contains(etiket.Guzergah));

            SecimBildir();
            Invalidate();
        }

        /// <summary>Seçili obje sayısına göre Properties panelini günceller (tek obje seçiliyse onu, aksi halde boşu bildirir).</summary>
        /// <summary>Seçim dışarıdan (ör. seçim menüsü komutlarıyla) değiştirildiğinde SecimDegisti olayını tetikler.</summary>
        public void SecimDegistiBildir()
        {
            SecimBildir();
        }

        private void SecimBildir()
        {
            CizimNesnesi tekObje = null;
            var sayac = 0;
            foreach (var nesne in Nesneler)
            {
                if (!nesne.Secili)
                    continue;
                sayac++;
                tekObje = nesne;
            }

            SecimDegisti?.Invoke(this, sayac == 1 ? tekObje : null);
        }

        private int MilimetreyiPikselYap(double milimetre)
        {
            var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
            return Math.Max(1, (int)Math.Round(milimetre / 25.4 * dpi));
        }

        private static Cursor KareImleciOlustur(int boyutPiksel, Color renk)
        {
            using (var bitmap = new Bitmap(boyutPiksel, boyutPiksel))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    using (var kalem = new Pen(renk, 1f))
                        g.DrawRectangle(kalem, 0, 0, boyutPiksel - 1, boyutPiksel - 1);
                }

                var hIcon = bitmap.GetHicon();
                return new Cursor(hIcon);
            }
        }

        /// <summary>Etiket ve kalın çizgi seçenekleri (Görünüm menüsü); null ise objeler etiketsiz, varsayılan kalınlıkta çizilir.</summary>
        public GorunumSecenekleri Secenekler { get; set; }

        /// <summary>
        /// Izgarayı ve görünür objeleri verilen görünümle çizer. Ana çizim, çok pencereli görünümdeki diğer pencereler de
        /// bunu kullanır (aynı objeler, ayrı pan/zoom).
        /// </summary>
        public void ArkaPlaniVeNesneleriCiz(Graphics grafik, CizimGorunum gorunum, int genislik, int yukseklik)
        {
            grafik.SmoothingMode = SmoothingMode.AntiAlias;
            using (CizimBaglami.Kur(Secenekler, BackColor))
            {
                GridCiz(grafik, gorunum, genislik, yukseklik);
                foreach (var nesne in Nesneler)
                {
                    // Tabakası belli olmayan (yeni çizilmiş, kopyalanmış...) obje aktif tabakaya girer.
                    if (TabakalarAcik && nesne.Tabaka == null)
                        nesne.Tabaka = AktifTabaka;
                    if (Gorunur(nesne))
                        nesne.Ciz(grafik, gorunum, genislik, yukseklik);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ArkaPlaniVeNesneleriCiz(e.Graphics, Gorunum, Width, Height);

            if (ReferansNoktasiGoster)
                ReferansNoktasiCiz(e.Graphics);

            if (YuzeyAraciMi(AktifArac) && _yuzeyIlkNokta >= 0 && DuzenlenenYuzey != null && _yuzeyIlkNokta < DuzenlenenYuzey.X.Count)
            {
                var ilk = Gorunum.DunyadanEkrana(DuzenlenenYuzey.X[_yuzeyIlkNokta], DuzenlenenYuzey.Y[_yuzeyIlkNokta], Width, Height);
                using (var kalem = new Pen(Color.Red, 1.5f) { DashStyle = DashStyle.Dash })
                    e.Graphics.DrawLine(kalem, ilk, _sonFareEkranKonumu);
                using (var kalem = new Pen(Color.Red, 2f))
                    e.Graphics.DrawRectangle(kalem, ilk.X - 5, ilk.Y - 5, 10, 10);
            }

            if (AktifArac == CizimAraci.Cizgi && _beklemedekiBaslangic != null)
            {
                var bas = Gorunum.DunyadanEkrana(_beklemedekiBaslangic.Value.X, _beklemedekiBaslangic.Value.Y, Width, Height);
                using (var kalem = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.DrawLine(kalem, bas, _sonFareEkranKonumu);
                }
            }

            if (_yakalananDunyaNoktasi != null)
            {
                var yakalamaEkran = Gorunum.DunyadanEkrana(_yakalananDunyaNoktasi.Value.X, _yakalananDunyaNoktasi.Value.Y, Width, Height);
                YakalamaIsaretiCiz(e.Graphics, yakalamaEkran, _yakalananTuru);
            }

            if (DuzenlemeAraciMi(AktifArac) && _duzenlemeAsamasi == DuzenlemeAsamasi.IkinciNoktaBekleniyor)
            {
                var fareDunya = Gorunum.EkrandanDunyaya(_sonFareEkranKonumu, Width, Height);
                OnizlemeCiz(e.Graphics, fareDunya);
            }

            if (DaireAraciMi(AktifArac))
                DaireOnizlemeCiz(e.Graphics);

            if (_secimKutusuSurukleniyor)
                SecimKutusuCiz(e.Graphics);

            UstCizim?.Invoke(this, e);
        }

        private void ReferansNoktasiCiz(Graphics grafik)
        {
            const float kol = 12f;
            var merkez = Gorunum.DunyadanEkrana(0, 0, Width, Height);
            using (var kalem = new Pen(Color.Red, 2f))
            {
                grafik.DrawLine(kalem, merkez.X - kol, merkez.Y, merkez.X + kol, merkez.Y);
                grafik.DrawLine(kalem, merkez.X, merkez.Y - kol, merkez.X, merkez.Y + kol);
                grafik.DrawEllipse(kalem, merkez.X - kol / 2, merkez.Y - kol / 2, kol, kol);
            }
        }

        private void SecimKutusuCiz(Graphics grafik)
        {
            var x = Math.Min(_secimKutusuBaslangicEkran.X, _sonFareEkranKonumu.X);
            var y = Math.Min(_secimKutusuBaslangicEkran.Y, _sonFareEkranKonumu.Y);
            var genislik = Math.Abs(_sonFareEkranKonumu.X - _secimKutusuBaslangicEkran.X);
            var yukseklik = Math.Abs(_sonFareEkranKonumu.Y - _secimKutusuBaslangicEkran.Y);
            var gecisSecimi = _sonFareEkranKonumu.X < _secimKutusuBaslangicEkran.X;
            var renk = gecisSecimi ? Color.LimeGreen : Color.DodgerBlue;

            using (var dolgu = new SolidBrush(Color.FromArgb(40, renk)))
            using (var kalem = new Pen(renk, 1f) { DashStyle = gecisSecimi ? DashStyle.Dash : DashStyle.Solid })
            {
                grafik.FillRectangle(dolgu, x, y, genislik, yukseklik);
                grafik.DrawRectangle(kalem, x, y, genislik, yukseklik);
            }
        }

        private void DaireOnizlemeCiz(Graphics grafik)
        {
            if (_daireAsamasi == DaireAsamasi.CozumSeciliyor && _daireAdaylari != null)
            {
                using (var kalem = new Pen(Color.DarkOrange, 1.5f) { DashStyle = DashStyle.Dash })
                {
                    foreach (var aday in _daireAdaylari)
                    {
                        var ekranMerkez = Gorunum.DunyadanEkrana(aday.MerkezX, aday.MerkezY, Width, Height);
                        var pikselYaricap = (float)(aday.Yaricap * Gorunum.Olcek);
                        grafik.DrawEllipse(kalem, ekranMerkez.X - pikselYaricap, ekranMerkez.Y - pikselYaricap, pikselYaricap * 2, pikselYaricap * 2);
                    }
                }
                return;
            }

            if (_daireNoktalar.Count == 0)
                return;

            using (var kalem = new Pen(Color.DarkOrange, 1.5f))
            {
                foreach (var nokta in _daireNoktalar)
                {
                    var ekran = Gorunum.DunyadanEkrana(nokta.X, nokta.Y, Width, Height);
                    grafik.DrawLine(kalem, ekran.X - 4, ekran.Y, ekran.X + 4, ekran.Y);
                    grafik.DrawLine(kalem, ekran.X, ekran.Y - 4, ekran.X, ekran.Y + 4);
                }
            }

            var sonNokta = _daireNoktalar[_daireNoktalar.Count - 1];
            var sonEkran = Gorunum.DunyadanEkrana(sonNokta.X, sonNokta.Y, Width, Height);
            using (var kalemCizgi = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dash })
                grafik.DrawLine(kalemCizgi, sonEkran, _sonFareEkranKonumu);
        }

        private void OnizlemeCiz(Graphics grafik, PointF fareDunya)
        {
            var tabanEkran = Gorunum.DunyadanEkrana(_tabanNoktasi.X, _tabanNoktasi.Y, Width, Height);
            using (var kalem = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dash })
            {
                grafik.DrawLine(kalem, tabanEkran, _sonFareEkranKonumu);
            }

            foreach (var orijinal in _duzenlenecekNesneler)
            {
                var onizleme = NesneDonusumleri.Klonla(orijinal);

                switch (AktifArac)
                {
                    case CizimAraci.Kaydir:
                    case CizimAraci.Kopyala:
                        NesneDonusumleri.Tasi(onizleme, fareDunya.X - _tabanNoktasi.X, fareDunya.Y - _tabanNoktasi.Y);
                        break;

                    case CizimAraci.Dondur:
                    {
                        var aciDerece = Math.Atan2(fareDunya.Y - _tabanNoktasi.Y, fareDunya.X - _tabanNoktasi.X) * 180.0 / Math.PI;
                        NesneDonusumleri.Dondur(onizleme, _tabanNoktasi, aciDerece);
                        break;
                    }
                }

                onizleme.Secili = true;
                onizleme.Ciz(grafik, Gorunum, Width, Height);
            }
        }

        /// <summary>Yakalama türünün ekrandaki işaretini çizer (menülerdeki simgeler de bununla çizilir).</summary>
        public static void YakalamaIsaretiCiz(Graphics grafik, PointF ekranNoktasi, YakalamaModu turu)
        {
            const float yaricap = 6f;

            if (turu == YakalamaModu.Orta)
            {
                var ucgen = new[]
                {
                    new PointF(ekranNoktasi.X, ekranNoktasi.Y - yaricap),
                    new PointF(ekranNoktasi.X - yaricap, ekranNoktasi.Y + yaricap),
                    new PointF(ekranNoktasi.X + yaricap, ekranNoktasi.Y + yaricap)
                };
                using (var kalem = new Pen(Color.DarkCyan, 2f))
                    grafik.DrawPolygon(kalem, ucgen);
            }
            else if (turu == YakalamaModu.Dik)
            {
                using (var kalem = new Pen(Color.OrangeRed, 2f))
                {
                    grafik.DrawLine(kalem, ekranNoktasi.X - yaricap, ekranNoktasi.Y, ekranNoktasi.X, ekranNoktasi.Y);
                    grafik.DrawLine(kalem, ekranNoktasi.X, ekranNoktasi.Y, ekranNoktasi.X, ekranNoktasi.Y - yaricap);
                    grafik.DrawRectangle(kalem, ekranNoktasi.X - yaricap * 0.4f, ekranNoktasi.Y - yaricap * 0.4f, yaricap * 0.4f, yaricap * 0.4f);
                }
            }
            else if (turu == YakalamaModu.Merkez)
            {
                using (var kalem = new Pen(Color.ForestGreen, 2f))
                {
                    grafik.DrawEllipse(kalem, ekranNoktasi.X - yaricap, ekranNoktasi.Y - yaricap, yaricap * 2, yaricap * 2);
                    grafik.DrawLine(kalem, ekranNoktasi.X - 2f, ekranNoktasi.Y, ekranNoktasi.X + 2f, ekranNoktasi.Y);
                    grafik.DrawLine(kalem, ekranNoktasi.X, ekranNoktasi.Y - 2f, ekranNoktasi.X, ekranNoktasi.Y + 2f);
                }
            }
            else if (turu == YakalamaModu.Nokta)
            {
                // Nokta: içinde çarpı olan daire (AutoCAD'deki düğüm işareti).
                using (var kalem = new Pen(Color.DarkViolet, 2f))
                {
                    var k = yaricap * 0.65f;
                    grafik.DrawEllipse(kalem, ekranNoktasi.X - yaricap, ekranNoktasi.Y - yaricap, yaricap * 2, yaricap * 2);
                    grafik.DrawLine(kalem, ekranNoktasi.X - k, ekranNoktasi.Y - k, ekranNoktasi.X + k, ekranNoktasi.Y + k);
                    grafik.DrawLine(kalem, ekranNoktasi.X - k, ekranNoktasi.Y + k, ekranNoktasi.X + k, ekranNoktasi.Y - k);
                }
            }
            else if (turu == YakalamaModu.Kesisim)
            {
                // Kesişim: çarpı.
                using (var kalem = new Pen(Color.DarkOrange, 2.2f))
                {
                    grafik.DrawLine(kalem, ekranNoktasi.X - yaricap, ekranNoktasi.Y - yaricap, ekranNoktasi.X + yaricap, ekranNoktasi.Y + yaricap);
                    grafik.DrawLine(kalem, ekranNoktasi.X - yaricap, ekranNoktasi.Y + yaricap, ekranNoktasi.X + yaricap, ekranNoktasi.Y - yaricap);
                }
            }
            else
            {
                using (var kalem = new Pen(Color.Magenta, 2f))
                    grafik.DrawRectangle(kalem, ekranNoktasi.X - yaricap, ekranNoktasi.Y - yaricap, yaricap * 2, yaricap * 2);
            }
        }

        /// <summary>Yakalama türünün 16x16 simgesi.</summary>
        public static Bitmap YakalamaSimgesi(YakalamaModu turu)
        {
            var simge = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(simge))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (turu == YakalamaModu.Dik)
                {
                    // Ekrandaki dik işareti küçük olduğu için simgede daha belirgin bir dik açı çizilir.
                    using (var kalem = new Pen(Color.OrangeRed, 2f))
                    {
                        g.DrawLine(kalem, 1, 14, 15, 14);
                        g.DrawLine(kalem, 6, 14, 6, 1);
                        g.DrawRectangle(kalem, 6, 9, 5, 5);
                    }
                }
                else
                {
                    YakalamaIsaretiCiz(g, new PointF(8, 8), turu);
                }
            }
            return simge;
        }

        private void GridCiz(Graphics grafik, CizimGorunum gorunum, int genislik, int yukseklik)
        {
            if (genislik <= 0 || yukseklik <= 0)
                return;

            var solUst = gorunum.EkrandanDunyaya(new PointF(0, 0), genislik, yukseklik);
            var sagAlt = gorunum.EkrandanDunyaya(new PointF(genislik, yukseklik), genislik, yukseklik);
            var dunyaMinX = Math.Min(solUst.X, sagAlt.X);
            var dunyaMaxX = Math.Max(solUst.X, sagAlt.X);
            var dunyaMinY = Math.Min(solUst.Y, sagAlt.Y);
            var dunyaMaxY = Math.Max(solUst.Y, sagAlt.Y);

            var adim = UygunAdimHesapla(100.0 / gorunum.Olcek);

            // Izgara rengi arka plana göre: zeminden biraz açık/koyu.
            var koyu = CizimBaglami.KoyuMu(BackColor);
            Color Kaydir(int miktar) => Color.FromArgb(Sinirla(BackColor.R + miktar), Sinirla(BackColor.G + miktar), Sinirla(BackColor.B + miktar));
            using (var izgaraKalemi = new Pen(Kaydir(koyu ? 35 : -35), 1f))
            using (var eksenKalemi = new Pen(Kaydir(koyu ? 70 : -63), 1.5f))
            using (var yaziFirca = new SolidBrush(Kaydir(koyu ? 110 : -127)))
            using (var yaziFontu = new Font(FontFamily.GenericSansSerif, 7.5f))
            {
                var baslangicX = Math.Floor(dunyaMinX / adim) * adim;
                for (var x = baslangicX; x <= dunyaMaxX; x += adim)
                {
                    var ekran = gorunum.DunyadanEkrana(x, 0, genislik, yukseklik);
                    grafik.DrawLine(izgaraKalemi, ekran.X, 0, ekran.X, yukseklik);
                    grafik.DrawString(x.ToString("0.##"), yaziFontu, yaziFirca, ekran.X + 2, 2);
                }

                var baslangicY = Math.Floor(dunyaMinY / adim) * adim;
                for (var y = baslangicY; y <= dunyaMaxY; y += adim)
                {
                    var ekran = gorunum.DunyadanEkrana(0, y, genislik, yukseklik);
                    grafik.DrawLine(izgaraKalemi, 0, ekran.Y, genislik, ekran.Y);
                    grafik.DrawString(y.ToString("0.##"), yaziFontu, yaziFirca, 2, ekran.Y + 2);
                }

                if (dunyaMinX <= 0 && dunyaMaxX >= 0)
                {
                    var ekran = gorunum.DunyadanEkrana(0, 0, genislik, yukseklik);
                    grafik.DrawLine(eksenKalemi, ekran.X, 0, ekran.X, yukseklik);
                }
                if (dunyaMinY <= 0 && dunyaMaxY >= 0)
                {
                    var ekran = gorunum.DunyadanEkrana(0, 0, genislik, yukseklik);
                    grafik.DrawLine(eksenKalemi, 0, ekran.Y, genislik, ekran.Y);
                }
            }
        }

        private static int Sinirla(int deger) => Math.Max(0, Math.Min(255, deger));

        private static double UygunAdimHesapla(double hamAdim)
        {
            if (hamAdim <= 0)
                return 1;
            var kuvvet = Math.Pow(10, Math.Floor(Math.Log10(hamAdim)));
            var kalan = hamAdim / kuvvet;
            double katsayi;
            if (kalan < 1.5) katsayi = 1;
            else if (kalan < 3.5) katsayi = 2;
            else if (kalan < 7.5) katsayi = 5;
            else katsayi = 10;
            return katsayi * kuvvet;
        }

        private struct YakalamaAdayi
        {
            public PointF Nokta;
            public YakalamaModu Tur;
            public double[] Tam;

            public YakalamaAdayi(PointF nokta, YakalamaModu tur)
            {
                Nokta = nokta;
                Tur = tur;
                Tam = null;
            }
        }

        /// <summary>Aktif yakalama modlarına göre verilen ekran noktasına en yakın yakalama adayını (varsa) bulur.</summary>
        private PointF? YakalamaUygula(PointF ekranNoktasi, out YakalamaModu bulunanTur)
        {
            bulunanTur = YakalamaModu.Yok;
            if (_aktifYakalamaModu == YakalamaModu.Yok)
                return null;

            PointF? enYakinDunya = null;
            var enYakinMesafeKare = double.MaxValue;
            _yakalananTam = null;

            foreach (var nesne in Nesneler)
            {
                // Kapalı tabakadaki objeye yakalanılmaz.
                if (!Gorunur(nesne))
                    continue;

                // Sürüklenen çizgi kendi ucuna yakalanmasın.
                if (_tutamacSurukleniyor && _suruklenenUclar.Any(u => ReferenceEquals(u.Key, nesne)))
                    continue;
                if (ReferenceEquals(nesne, _suruklenenNesne))
                    continue;

                // Yüzeyin yüz binlerce noktası olabilir; hepsini aday listesine koymak yerine en yakın nokta doğrudan sorulur.
                if (nesne is YuzeyNesnesi yuzey)
                {
                    if ((_aktifYakalamaModu & (YakalamaModu.Nokta | YakalamaModu.Uc)) == 0)
                        continue;

                    Gorunum.EkrandanDunyayaHassas(ekranNoktasi, Width, Height, out var fareX, out var fareY);
                    var yakinNokta = yuzey.EnYakinNokta(fareX, fareY, YakalamaToleransPiksel / Gorunum.Olcek);
                    if (yakinNokta < 0)
                        continue;

                    var noktaEkran = Gorunum.DunyadanEkrana(yuzey.X[yakinNokta], yuzey.Y[yakinNokta], Width, Height);
                    var farkX = noktaEkran.X - ekranNoktasi.X;
                    var farkY = noktaEkran.Y - ekranNoktasi.Y;
                    var uzaklikKare = (double)farkX * farkX + (double)farkY * farkY;
                    if (uzaklikKare < enYakinMesafeKare)
                    {
                        enYakinMesafeKare = uzaklikKare;
                        enYakinDunya = new PointF((float)yuzey.X[yakinNokta], (float)yuzey.Y[yakinNokta]);
                        bulunanTur = YakalamaModu.Nokta;
                        _yakalananTam = new[] { yuzey.X[yakinNokta], yuzey.Y[yakinNokta] };
                    }
                    continue;
                }

                var adaylar = new List<YakalamaAdayi>();

                if ((_aktifYakalamaModu & (YakalamaModu.Nokta | YakalamaModu.Uc)) != 0)
                {
                    var ucTuru = (_aktifYakalamaModu & YakalamaModu.Uc) != 0 ? YakalamaModu.Uc : YakalamaModu.Nokta;
                    if (nesne is GuzergahNesnesi guzergahUclari)
                    {
                        // Güzergahta parça uçları (kopuk parçalar dahil) tam koordinatla verilir.
                        foreach (var uc in guzergahUclari.YakalamaUclari())
                            adaylar.Add(new YakalamaAdayi(new PointF((float)uc[0], (float)uc[1]), ucTuru) { Tam = uc });
                    }
                    else
                    {
                        foreach (var uc in nesne.UcNoktalariAl())
                            adaylar.Add(new YakalamaAdayi(uc, ucTuru));
                    }
                }

                if ((_aktifYakalamaModu & YakalamaModu.Orta) != 0)
                {
                    // Çok parçalı objelerde her parçanın ortası adaydır; koordinat çift hassasiyetle saklanır.
                    foreach (var orta in nesne.OrtaNoktalari())
                        adaylar.Add(new YakalamaAdayi(new PointF((float)orta[0], (float)orta[1]), YakalamaModu.Orta) { Tam = orta });
                }

                if ((_aktifYakalamaModu & YakalamaModu.Dik) != 0 && _beklemedekiBaslangic != null)
                {
                    if (nesne is GuzergahNesnesi guzergahDik)
                    {
                        var tam = guzergahDik.DikNoktaTam(_beklemedekiBaslangic.Value.X, _beklemedekiBaslangic.Value.Y);
                        if (tam != null)
                            adaylar.Add(new YakalamaAdayi(new PointF((float)tam[0], (float)tam[1]), YakalamaModu.Dik) { Tam = tam });
                    }
                    else
                    {
                        var dik = nesne.DikNoktaAl(_beklemedekiBaslangic.Value);
                        if (dik != null)
                            adaylar.Add(new YakalamaAdayi(dik.Value, YakalamaModu.Dik));
                    }
                }

                if ((_aktifYakalamaModu & YakalamaModu.Merkez) != 0)
                {
                    var merkez = nesne.MerkezNoktaAl();
                    if (merkez != null)
                        adaylar.Add(new YakalamaAdayi(merkez.Value, YakalamaModu.Merkez));
                    // Güzergahta (kopuk parçalar dahil) her kurbun merkezi.
                    if (nesne is GuzergahNesnesi guzergahMerkez)
                    {
                        foreach (var m in guzergahMerkez.KurpMerkezleri())
                            adaylar.Add(new YakalamaAdayi(new PointF((float)m[0], (float)m[1]), YakalamaModu.Merkez) { Tam = m });
                    }
                }

                foreach (var aday in adaylar)
                {
                    var adayEkran = Gorunum.DunyadanEkrana(aday.Nokta.X, aday.Nokta.Y, Width, Height);
                    var dx = adayEkran.X - ekranNoktasi.X;
                    var dy = adayEkran.Y - ekranNoktasi.Y;
                    var mesafeKare = (double)dx * dx + (double)dy * dy;
                    if (mesafeKare > YakalamaToleransPiksel * YakalamaToleransPiksel || mesafeKare >= enYakinMesafeKare)
                        continue;

                    enYakinMesafeKare = mesafeKare;
                    enYakinDunya = aday.Nokta;
                    bulunanTur = aday.Tur;
                    _yakalananTam = aday.Tam;
                }
            }

            if ((_aktifYakalamaModu & YakalamaModu.Kesisim) != 0)
            {
                // Farenin yakınındaki objelerin, yine farenin yakınındaki parçaları birbiriyle kesiştirilir.
                Gorunum.EkrandanDunyayaHassas(ekranNoktasi, Width, Height, out var fareX, out var fareY);
                var tolerans = YakalamaToleransPiksel / Gorunum.Olcek;
                var fare = new PointF((float)fareX, (float)fareY);
                var yakinParcalar = new List<KeyValuePair<CizimNesnesi, Primitif>>();
                foreach (var nesne in Nesneler)
                {
                    if (!Gorunur(nesne) || nesne is YuzeyNesnesi || nesne is NoktaNesnesi || nesne is YaziNesnesi || ReferenceEquals(nesne, _suruklenenNesne)
                        || (_tutamacSurukleniyor && _suruklenenUclar.Any(u => ReferenceEquals(u.Key, nesne))) || !nesne.IsabetEttiMi(fare, tolerans))
                        continue;
                    foreach (var parca in GeometriIslemleri.Primitifler(nesne))
                    {
                        var yakin = parca.YayMi
                            ? Math.Abs(Math.Sqrt((fareX - parca.Mx) * (fareX - parca.Mx) + (fareY - parca.My) * (fareY - parca.My)) - parca.R) <= tolerans
                            : GeometriIslemleri.NoktaParcaUzakligi(fareX, fareY, parca.X1, parca.Y1, parca.X2, parca.Y2) <= tolerans;
                        if (yakin)
                            yakinParcalar.Add(new KeyValuePair<CizimNesnesi, Primitif>(nesne, parca));
                    }
                    if (yakinParcalar.Count > 200)
                        break;
                }

                for (var i = 0; i < yakinParcalar.Count; i++)
                {
                    for (var j = i + 1; j < yakinParcalar.Count; j++)
                    {
                        // Aynı objenin ardışık parçalarının ortak ucu kesişim sayılmaz (o, uç yakalamanın işidir).
                        if (ReferenceEquals(yakinParcalar[i].Key, yakinParcalar[j].Key))
                            continue;
                        foreach (var nokta in GeometriIslemleri.PrimitifKesisimleri(yakinParcalar[i].Value, yakinParcalar[j].Value))
                        {
                            var ekran = Gorunum.DunyadanEkrana(nokta[0], nokta[1], Width, Height);
                            double dx = ekran.X - ekranNoktasi.X, dy = ekran.Y - ekranNoktasi.Y, mesafeKare = dx * dx + dy * dy;
                            if (mesafeKare > YakalamaToleransPiksel * YakalamaToleransPiksel || mesafeKare >= enYakinMesafeKare)
                                continue;
                            enYakinMesafeKare = mesafeKare;
                            enYakinDunya = new PointF((float)nokta[0], (float)nokta[1]);
                            bulunanTur = YakalamaModu.Kesisim;
                            _yakalananTam = nokta;
                        }
                    }
                }
            }

            if (ReferansNoktasiGoster && (_aktifYakalamaModu & (YakalamaModu.Nokta | YakalamaModu.Uc)) != 0)
            {
                var referansEkran = Gorunum.DunyadanEkrana(0, 0, Width, Height);
                var dx = referansEkran.X - ekranNoktasi.X;
                var dy = referansEkran.Y - ekranNoktasi.Y;
                var mesafeKare = (double)dx * dx + (double)dy * dy;
                if (mesafeKare <= YakalamaToleransPiksel * YakalamaToleransPiksel && mesafeKare < enYakinMesafeKare)
                {
                    enYakinDunya = new PointF(0, 0);
                    bulunanTur = YakalamaModu.Nokta;
                    _yakalananTam = new double[] { 0, 0 };
                }
            }

            return enYakinDunya;
        }

        /// <summary>Ekran noktasını dünya koordinatına çevirir; aktif yakalama modları varsa ve tolerans içinde bir aday bulunursa ona kilitlenir.</summary>
        private PointF DunyaNoktasiHesaplaVeYakala(Point ekranNoktasi)
        {
            var yakalanan = YakalamaUygula(ekranNoktasi, out var yakalamaTuru);
            if (yakalanan != null)
            {
                _yakalananDunyaNoktasi = yakalanan;
                _yakalananTuru = yakalamaTuru;
                return yakalanan.Value;
            }

            _yakalananDunyaNoktasi = null;
            return Gorunum.EkrandanDunyaya(ekranNoktasi, Width, Height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button == MouseButtons.Middle)
            {
                if (e.Clicks >= 2)
                {
                    TumObjeleriGoster();
                    return;
                }

                _panSurukleniyor = true;
                _panBaslangicEkran = e.Location;
                _panBaslangicOfsetX = Gorunum.OfsetX;
                _panBaslangicOfsetY = Gorunum.OfsetY;
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                if (AktifArac == CizimAraci.NoktaAl)
                {
                    AktifArac = CizimAraci.Sec;
                    NoktaAlmaBitti?.Invoke(this, EventArgs.Empty);
                }
                else if (AktifArac == CizimAraci.Cizgi || AktifArac == CizimAraci.Kes)
                {
                    AktifArac = CizimAraci.Sec;
                }
                else if (YuzeyAraciMi(AktifArac))
                {
                    // İlk nokta seçilmişse sağ tık yalnızca onu bırakır; seçilmemişse araçtan çıkılır.
                    if (_yuzeyIlkNokta >= 0)
                    {
                        _yuzeyIlkNokta = -1;
                        Invalidate();
                    }
                    else
                    {
                        AktifArac = CizimAraci.Sec;
                    }
                }
                else if (DuzenlemeAraciMi(AktifArac))
                {
                    if (_duzenlemeAsamasi == DuzenlemeAsamasi.ObjeSeciliyor && _duzenlenecekNesneler.Count > 0)
                    {
                        _duzenlemeAsamasi = DuzenlemeAsamasi.TabanNoktasiBekleniyor;
                        ImleciGuncelle();
                        Invalidate();
                    }
                    else
                    {
                        foreach (var nesne in _duzenlenecekNesneler)
                            nesne.Secili = false;
                        AktifArac = CizimAraci.Sec;
                    }
                }
                else if (DaireAraciMi(AktifArac))
                {
                    foreach (var nesne in _daireObjeleri)
                        nesne.Secili = false;
                    AktifArac = CizimAraci.Sec;
                }
                else if (AktifArac == CizimAraci.Sec)
                {
                    SecimOnaylandi?.Invoke(this, EventArgs.Empty);
                }
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            if (AktifArac == CizimAraci.NoktaAl)
            {
                HassasNoktaAl(e.Location, out var tikX, out var tikY);
                NoktaTiklandi?.Invoke(tikX, tikY);
                Invalidate();
                return;
            }

            if (AktifArac == CizimAraci.YuzeyCizgiSil)
            {
                // Çizgi Sil'de tek tıklama mı pencere mi olduğu fare bırakılınca belli olur (bkz. OnMouseUp).
                _secimKutusuBekleniyor = true;
                _secimKutusuSurukleniyor = false;
                _secimKutusuBaslangicEkran = e.Location;
                return;
            }

            if (YuzeyAraciMi(AktifArac))
            {
                YuzeyDuzenleTikla(e.Location);
                return;
            }

            var dunyaNoktasi = DunyaNoktasiHesaplaVeYakala(e.Location);

            if (AktifArac == CizimAraci.Cizgi)
            {
                TamKoordinatAl(dunyaNoktasi, out var tamX, out var tamY);
                if (_beklemedekiBaslangic != null)
                {
                    var yeniCizgi = new CizgiNesnesi
                    {
                        BaslangicX = _beklemedekiBaslangicX,
                        BaslangicY = _beklemedekiBaslangicY,
                        BitisX = tamX,
                        BitisY = tamY
                    };
                    Nesneler.Add(yeniCizgi);
                }
                _beklemedekiBaslangic = dunyaNoktasi;
                _beklemedekiBaslangicX = tamX;
                _beklemedekiBaslangicY = tamY;
                Invalidate();
            }
            else if (AktifArac == CizimAraci.Kes)
            {
                KesUygula(dunyaNoktasi);
            }
            else if (DuzenlemeAraciMi(AktifArac))
            {
                DuzenlemeTikla(dunyaNoktasi);
            }
            else if (DaireAraciMi(AktifArac))
            {
                DaireTikla(dunyaNoktasi);
            }
            else
            {
                SecAraciMouseDown(dunyaNoktasi, e.Location);
            }

            DegisiklikKaydet();
        }

        /// <summary>
        /// Fareden gelen (float hassasiyetli) dünya noktasını çift hassasiyetli koordinata çevirir. Nokta bir çizgi
        /// ucuna yakalanmışsa o ucun tam koordinatı döner; böylece birleşen uçlar birebir aynı değeri taşır.
        /// </summary>
        private void TamKoordinatAl(PointF dunyaNoktasi, out double x, out double y)
        {
            if (_yakalananDunyaNoktasi != null && _yakalananTam != null)
            {
                x = _yakalananTam[0];
                y = _yakalananTam[1];
                return;
            }

            if (_yakalananDunyaNoktasi != null)
            {
                foreach (var nesne in Nesneler)
                {
                    if (!(nesne is CizgiNesnesi cizgi))
                        continue;

                    if ((float)cizgi.BaslangicX == dunyaNoktasi.X && (float)cizgi.BaslangicY == dunyaNoktasi.Y)
                    {
                        x = cizgi.BaslangicX;
                        y = cizgi.BaslangicY;
                        return;
                    }

                    if ((float)cizgi.BitisX == dunyaNoktasi.X && (float)cizgi.BitisY == dunyaNoktasi.Y)
                    {
                        x = cizgi.BitisX;
                        y = cizgi.BitisY;
                        return;
                    }
                }
            }

            x = dunyaNoktasi.X;
            y = dunyaNoktasi.Y;
            if (YeniNoktalariMilimetreyeYuvarla)
            {
                x = Math.Round(x, 3);
                y = Math.Round(y, 3);
            }
        }

        private static bool UclarAyniMi(double x1, double y1, double x2, double y2)
        {
            return Math.Abs(x1 - x2) <= UcEsitlikToleransi && Math.Abs(y1 - y2) <= UcEsitlikToleransi;
        }

        /// <summary>Tıklanan yer seçili bir çizginin uç tutamacıysa o ucu (ve istenirse aynı noktadaki diğer uçları) sürüklemeye başlar.</summary>
        private bool TutamacSuruklemeBaslat(Point ekranNoktasi)
        {
            for (var i = Nesneler.Count - 1; i >= 0; i--)
            {
                if (!Nesneler[i].Secili || !(Nesneler[i] is CizgiNesnesi cizgi))
                    continue;

                bool baslangicMi;
                if (TutamacaIsabetEttiMi(cizgi.BaslangicX, cizgi.BaslangicY, ekranNoktasi))
                    baslangicMi = true;
                else if (TutamacaIsabetEttiMi(cizgi.BitisX, cizgi.BitisY, ekranNoktasi))
                    baslangicMi = false;
                else
                    continue;

                var ucX = baslangicMi ? cizgi.BaslangicX : cizgi.BitisX;
                var ucY = baslangicMi ? cizgi.BaslangicY : cizgi.BitisY;

                _suruklenenUclar.Clear();
                _suruklenenUclar.Add(new KeyValuePair<CizgiNesnesi, bool>(cizgi, baslangicMi));
                if (OrtakUclariBirlikteTasi)
                {
                    foreach (var nesne in Nesneler)
                    {
                        if (!(nesne is CizgiNesnesi diger) || ReferenceEquals(diger, cizgi) || !AyniUcGrubundaMi(diger, cizgi))
                            continue;
                        if (UclarAyniMi(diger.BaslangicX, diger.BaslangicY, ucX, ucY))
                            _suruklenenUclar.Add(new KeyValuePair<CizgiNesnesi, bool>(diger, true));
                        if (UclarAyniMi(diger.BitisX, diger.BitisY, ucX, ucY))
                            _suruklenenUclar.Add(new KeyValuePair<CizgiNesnesi, bool>(diger, false));
                    }
                }

                _tutamacSurukleniyor = true;
                return true;
            }

            return false;
        }

        private bool TutamacaIsabetEttiMi(double dunyaX, double dunyaY, Point ekranNoktasi)
        {
            var ekran = Gorunum.DunyadanEkrana(dunyaX, dunyaY, Width, Height);
            return Math.Abs(ekran.X - ekranNoktasi.X) <= TutamacToleransPiksel && Math.Abs(ekran.Y - ekranNoktasi.Y) <= TutamacToleransPiksel;
        }

        /// <summary>
        /// Bir çizginin ucu (ör. Özellikler panelinden) değiştirildikten sonra, eski noktayı paylaşan diğer çizgi
        /// uçlarını yeni noktaya taşır.
        /// </summary>
        public void OrtakUclariGuncelle(CizgiNesnesi kaynak, double eskiX, double eskiY, double yeniX, double yeniY)
        {
            foreach (var nesne in Nesneler)
            {
                if (!(nesne is CizgiNesnesi diger) || ReferenceEquals(diger, kaynak) || !AyniUcGrubundaMi(diger, kaynak))
                    continue;

                if (UclarAyniMi(diger.BaslangicX, diger.BaslangicY, eskiX, eskiY))
                {
                    diger.BaslangicX = yeniX;
                    diger.BaslangicY = yeniY;
                }

                if (UclarAyniMi(diger.BitisX, diger.BitisY, eskiX, eskiY))
                {
                    diger.BitisX = yeniX;
                    diger.BitisY = yeniY;
                }
            }

            Invalidate();
        }

        /// <summary>
        /// Özellikler panelinden bir obje değeri değiştirildikten sonra çağrılır: değişen bir çizgi ucuysa (ve ortak
        /// uç taşıma açıksa) eski noktayı paylaşan komşu çizgi ucunu da taşır, değişikliği geri al geçmişine yazar.
        /// </summary>
        public void OzellikDegisti(object nesne, string ozellikAdi, object eskiDeger)
        {
            if (OrtakUclariBirlikteTasi && nesne is CizgiNesnesi cizgi && eskiDeger is double eski)
            {
                switch (ozellikAdi)
                {
                    case nameof(CizgiNesnesi.BaslangicX):
                        OrtakUclariGuncelle(cizgi, eski, cizgi.BaslangicY, cizgi.BaslangicX, cizgi.BaslangicY);
                        break;
                    case nameof(CizgiNesnesi.BaslangicY):
                        OrtakUclariGuncelle(cizgi, cizgi.BaslangicX, eski, cizgi.BaslangicX, cizgi.BaslangicY);
                        break;
                    case nameof(CizgiNesnesi.BitisX):
                        OrtakUclariGuncelle(cizgi, eski, cizgi.BitisY, cizgi.BitisX, cizgi.BitisY);
                        break;
                    case nameof(CizgiNesnesi.BitisY):
                        OrtakUclariGuncelle(cizgi, cizgi.BitisX, eski, cizgi.BitisX, cizgi.BitisY);
                        break;
                }
            }

            DegisiklikKaydet();
            Invalidate();
        }

        /// <summary>
        /// Çizgi Sil komutunda sürüklenen pencereyle çizgileri toplu siler. Seç aracındaki gibi: soldan sağa çekilen
        /// pencere yalnızca tamamı içinde kalan çizgileri, sağdan sola çekilen pencere değdiği bütün çizgileri seçer.
        /// </summary>
        private void YuzeyPencereyleCizgiSil(Point baslangicEkran, Point bitisEkran)
        {
            var yuzey = DuzenlenenYuzey;
            if (yuzey == null || !Nesneler.Contains(yuzey))
            {
                AktifArac = CizimAraci.Sec;
                return;
            }

            Gorunum.EkrandanDunyayaHassas(baslangicEkran, Width, Height, out var x1, out var y1);
            Gorunum.EkrandanDunyayaHassas(bitisEkran, Width, Height, out var x2, out var y2);
            var kesenlerDahil = bitisEkran.X < baslangicEkran.X;

            if (yuzey.CizgileriSil(Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2), kesenlerDahil) > 0)
                NesnelerGuncellendi?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        /// <summary>Aktif yüzey düzenleme aracını, tıklanan ekran noktasında DuzenlenenYuzey üzerinde uygular.</summary>
        private void YuzeyDuzenleTikla(Point ekranNoktasi)
        {
            var yuzey = DuzenlenenYuzey;
            if (yuzey == null || !Nesneler.Contains(yuzey))
            {
                AktifArac = CizimAraci.Sec;
                return;
            }

            Gorunum.EkrandanDunyayaHassas(ekranNoktasi, Width, Height, out var x, out var y);
            var tolerans = YuzeySecimToleransPiksel / Gorunum.Olcek;
            string hata = null;
            var degisti = false;

            switch (AktifArac)
            {
                case CizimAraci.YuzeyCizgiEkle:
                {
                    var nokta = yuzey.EnYakinNokta(x, y, tolerans);
                    if (nokta < 0)
                        break;
                    if (_yuzeyIlkNokta < 0)
                    {
                        _yuzeyIlkNokta = nokta;
                    }
                    else if (nokta != _yuzeyIlkNokta)
                    {
                        degisti = yuzey.CizgiEkle(_yuzeyIlkNokta, nokta, out hata);
                        _yuzeyIlkNokta = -1;
                    }
                    break;
                }

                case CizimAraci.YuzeyCizgiSil:
                    if (yuzey.EnYakinKenar(x, y, tolerans, out var silA, out var silB))
                        degisti = yuzey.CizgiSil(silA, silB);
                    break;

                case CizimAraci.YuzeyUcgenDondur:
                    if (yuzey.EnYakinKenar(x, y, tolerans, out var donA, out var donB))
                        degisti = yuzey.UcgenDondur(donA, donB, out hata);
                    break;

                case CizimAraci.YuzeyNoktaEkle:
                    using (var dlg = new YuzeyNoktaForm("Nokta Ekle", yuzey.SonrakiNoktaNo(), x, y, yuzey.KotHesapla(x, y)))
                    {
                        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                        {
                            yuzey.NoktaEkle(dlg.No, dlg.Y, dlg.X, dlg.Z);
                            degisti = true;
                        }
                    }
                    break;

                case CizimAraci.YuzeyNoktaSil:
                {
                    var nokta = yuzey.EnYakinNokta(x, y, tolerans);
                    if (nokta >= 0)
                    {
                        yuzey.NoktaSil(nokta);
                        degisti = true;
                    }
                    break;
                }

                case CizimAraci.YuzeyNoktaDuzelt:
                {
                    var nokta = yuzey.EnYakinNokta(x, y, tolerans);
                    if (nokta < 0)
                        break;
                    using (var dlg = new YuzeyNoktaForm("Nokta Düzelt", yuzey.Nolar[nokta], yuzey.X[nokta], yuzey.Y[nokta], yuzey.Z[nokta]))
                    {
                        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                        {
                            yuzey.NoktaDuzelt(nokta, dlg.No, dlg.Y, dlg.X, dlg.Z);
                            degisti = true;
                        }
                    }
                    break;
                }

                case CizimAraci.YuzeyNoktaTasi:
                    if (_yuzeyIlkNokta < 0)
                    {
                        _yuzeyIlkNokta = yuzey.EnYakinNokta(x, y, tolerans);
                    }
                    else
                    {
                        yuzey.NoktaTasi(_yuzeyIlkNokta, x, y);
                        _yuzeyIlkNokta = -1;
                        degisti = true;
                    }
                    break;
            }

            if (hata != null)
                MessageBox.Show(hata, "Yüzey Düzenle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (degisti)
                NesnelerGuncellendi?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        private void SecAraciMouseDown(PointF dunyaNoktasi, Point ekranNoktasi)
        {
            if (TutamacSuruklemeAcik && TutamacSuruklemeBaslat(ekranNoktasi))
                return;
            if (NesneTutamaciBaslat(ekranNoktasi))
                return;

            var toleransDunya = SecimToleransPiksel / Gorunum.Olcek;
            CizimNesnesi bulunan = null;
            for (var i = Nesneler.Count - 1; i >= 0; i--)
            {
                if (Secilebilir(Nesneler[i]) && Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
                {
                    bulunan = Nesneler[i];
                    break;
                }
            }

            if (bulunan != null)
            {
                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    bulunan.Secili = !bulunan.Secili;
                }
                else
                {
                    foreach (var nesne in Nesneler)
                        nesne.Secili = ReferenceEquals(nesne, bulunan);
                }

                SecimBildir();
                Invalidate();
                return;
            }

            // Boş alana tıklandı: pencere/kesişme seçimi sürüklemesi olası. Kesin karar MouseUp'ta veriliyor.
            _secimKutusuBekleniyor = true;
            _secimKutusuSurukleniyor = false;
            _secimKutusuBaslangicEkran = ekranNoktasi;
        }

        private void KesUygula(PointF dunyaNoktasi)
        {
            var toleransDunya = SecimToleransPiksel / Gorunum.Olcek;
            CizimNesnesi hedef = null;
            for (var i = Nesneler.Count - 1; i >= 0; i--)
            {
                if ((Nesneler[i] is CizgiNesnesi || Nesneler[i] is YayNesnesi || Nesneler[i] is DaireNesnesi) && Secilebilir(Nesneler[i]) && Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
                {
                    hedef = Nesneler[i];
                    break;
                }
            }

            if (hedef == null)
                return;

            // Seçili objeler varsa yalnızca onlar kesici sayılır; seçim yoksa bütün objeler kesicidir.
            var kesiciler = Nesneler.Where(n => n.Secili && !ReferenceEquals(n, hedef) && Gorunur(n)).ToList();
            var parcalar = Kesisimler.Kirp(hedef, dunyaNoktasi, kesiciler.Count > 0 ? kesiciler : Nesneler);
            if (parcalar == null)
                return;

            // Kesilen objenin parçaları onun tabakasında ve renginde kalır.
            foreach (var parca in parcalar)
            {
                parca.Tabaka = hedef.Tabaka;
                parca.OzelRenk = hedef.OzelRenk;
            }

            Nesneler.Remove(hedef);
            Nesneler.AddRange(parcalar);
            Invalidate();
        }

        private void DuzenlemeTikla(PointF dunyaNoktasi)
        {
            switch (_duzenlemeAsamasi)
            {
                case DuzenlemeAsamasi.ObjeSeciliyor:
                {
                    var toleransDunya = SecimToleransPiksel / Gorunum.Olcek;
                    CizimNesnesi bulunan = null;
                    for (var i = Nesneler.Count - 1; i >= 0; i--)
                    {
                        if (Secilebilir(Nesneler[i]) && Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
                        {
                            bulunan = Nesneler[i];
                            break;
                        }
                    }

                    if (bulunan != null)
                    {
                        if (_duzenlenecekNesneler.Contains(bulunan))
                        {
                            _duzenlenecekNesneler.Remove(bulunan);
                            bulunan.Secili = false;
                        }
                        else
                        {
                            _duzenlenecekNesneler.Add(bulunan);
                            bulunan.Secili = true;
                        }
                    }
                    Invalidate();
                    break;
                }

                case DuzenlemeAsamasi.TabanNoktasiBekleniyor:
                    _tabanNoktasi = dunyaNoktasi;
                    if (AktifArac == CizimAraci.Olcekle)
                        OlcekleUygulaDiyalogIle();
                    else
                    {
                        _duzenlemeAsamasi = DuzenlemeAsamasi.IkinciNoktaBekleniyor;
                        Invalidate();
                    }
                    break;

                case DuzenlemeAsamasi.IkinciNoktaBekleniyor:
                    DuzenlemeUygula(dunyaNoktasi);
                    foreach (var nesne in _duzenlenecekNesneler)
                        nesne.Secili = false;
                    AktifArac = CizimAraci.Sec;
                    Invalidate();
                    break;
            }
        }

        private void OlcekleUygulaDiyalogIle()
        {
            using (var dlg = new OlcekFaktoruForm())
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    foreach (var nesne in _duzenlenecekNesneler)
                        NesneDonusumleri.Olcekle(nesne, _tabanNoktasi, dlg.Deger);
                }

                foreach (var nesne in _duzenlenecekNesneler)
                    nesne.Secili = false;
                AktifArac = CizimAraci.Sec;
                Invalidate();
            }
        }

        private void DuzenlemeUygula(PointF ikinciNokta)
        {
            switch (AktifArac)
            {
                case CizimAraci.Kaydir:
                {
                    var dx = ikinciNokta.X - _tabanNoktasi.X;
                    var dy = ikinciNokta.Y - _tabanNoktasi.Y;
                    foreach (var nesne in _duzenlenecekNesneler)
                        NesneDonusumleri.Tasi(nesne, dx, dy);
                    break;
                }

                case CizimAraci.Kopyala:
                {
                    var dx = ikinciNokta.X - _tabanNoktasi.X;
                    var dy = ikinciNokta.Y - _tabanNoktasi.Y;
                    foreach (var nesne in _duzenlenecekNesneler)
                    {
                        var kopya = NesneDonusumleri.Klonla(nesne);
                        NesneDonusumleri.Tasi(kopya, dx, dy);
                        Nesneler.Add(kopya);
                    }
                    break;
                }

                case CizimAraci.Dondur:
                {
                    var aciDerece = Math.Atan2(ikinciNokta.Y - _tabanNoktasi.Y, ikinciNokta.X - _tabanNoktasi.X) * 180.0 / Math.PI;
                    foreach (var nesne in _duzenlenecekNesneler)
                        NesneDonusumleri.Dondur(nesne, _tabanNoktasi, aciDerece);
                    break;
                }
            }
        }

        private void DaireTikla(PointF dunyaNoktasi)
        {
            if (_daireAsamasi == DaireAsamasi.CozumSeciliyor)
            {
                DaireCozumSec(dunyaNoktasi);
                return;
            }

            switch (AktifArac)
            {
                case CizimAraci.DaireMerkezYaricap:
                    DaireMerkezYaricapTikla(dunyaNoktasi);
                    break;
                case CizimAraci.Daire3Nokta:
                    Daire3NoktaTikla(dunyaNoktasi);
                    break;
                case CizimAraci.Daire2NoktaYaricap:
                    Daire2NoktaYaricapTikla(dunyaNoktasi);
                    break;
                case CizimAraci.DaireTegetIkiObje:
                    DaireTegetObjeTikla(dunyaNoktasi, 2);
                    break;
                case CizimAraci.DaireTegetUcObje:
                    DaireTegetObjeTikla(dunyaNoktasi, 3);
                    break;
            }
        }

        private void DaireMerkezYaricapTikla(PointF merkez)
        {
            using (var dlg = new OlcekFaktoruForm("Daire Çiz", "Yarıçap:", 1.0))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    Nesneler.Add(new DaireNesnesi { MerkezX = merkez.X, MerkezY = merkez.Y, Yaricap = dlg.Deger });
            }

            AktifArac = CizimAraci.Sec;
            Invalidate();
        }

        private void Daire3NoktaTikla(PointF nokta)
        {
            _daireNoktalar.Add(nokta);
            if (_daireNoktalar.Count < 3)
            {
                Invalidate();
                return;
            }

            var daire = DaireInsaHesaplari.UcNoktadanGecen(_daireNoktalar[0], _daireNoktalar[1], _daireNoktalar[2]);
            if (daire != null)
                Nesneler.Add(daire);
            else
                MessageBox.Show("Seçilen 3 nokta aynı doğru üzerinde, daire oluşturulamadı.", "Daire Çiz", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            AktifArac = CizimAraci.Sec;
            Invalidate();
        }

        private void Daire2NoktaYaricapTikla(PointF nokta)
        {
            _daireNoktalar.Add(nokta);
            if (_daireNoktalar.Count < 2)
            {
                Invalidate();
                return;
            }

            using (var dlg = new OlcekFaktoruForm("Daire Çiz", "Yarıçap:", 1.0))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                {
                    AktifArac = CizimAraci.Sec;
                    return;
                }

                var adaylar = DaireInsaHesaplari.IkiNoktaVeYaricap(_daireNoktalar[0], _daireNoktalar[1], dlg.Deger);
                DaireAdaylariIsle(adaylar);
            }
        }

        private void DaireTegetObjeTikla(PointF dunyaNoktasi, int gerekenObjeSayisi)
        {
            var toleransDunya = SecimToleransPiksel / Gorunum.Olcek;
            CizimNesnesi bulunan = null;
            for (var i = Nesneler.Count - 1; i >= 0; i--)
            {
                if (DaireTegetObjesiOlabilirMi(Nesneler[i]) && Secilebilir(Nesneler[i]) && Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
                {
                    bulunan = Nesneler[i];
                    break;
                }
            }

            if (bulunan == null || _daireObjeleri.Contains(bulunan))
                return;

            _daireObjeleri.Add(bulunan);
            bulunan.Secili = true;
            _daireNoktalar.Add(dunyaNoktasi);

            if (_daireObjeleri.Count < gerekenObjeSayisi)
            {
                Invalidate();
                return;
            }

            List<DaireNesnesi> adaylar;
            if (gerekenObjeSayisi == 2)
            {
                using (var dlg = new OlcekFaktoruForm("Daire Çiz", "Yarıçap:", 1.0))
                {
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                    {
                        foreach (var nesne in _daireObjeleri)
                            nesne.Secili = false;
                        AktifArac = CizimAraci.Sec;
                        return;
                    }

                    adaylar = DaireInsaHesaplari.TegetIkiObjeYaricap(_daireObjeleri[0], _daireObjeleri[1], dlg.Deger);
                }
            }
            else
            {
                var baslangicTahmini = new PointF(
                    (_daireNoktalar[0].X + _daireNoktalar[1].X + _daireNoktalar[2].X) / 3f,
                    (_daireNoktalar[0].Y + _daireNoktalar[1].Y + _daireNoktalar[2].Y) / 3f);
                adaylar = DaireInsaHesaplari.TegetUcObje(_daireObjeleri[0], _daireObjeleri[1], _daireObjeleri[2], baslangicTahmini);
            }

            foreach (var nesne in _daireObjeleri)
                nesne.Secili = false;

            DaireAdaylariIsle(adaylar);
        }

        private void DaireAdaylariIsle(List<DaireNesnesi> adaylar)
        {
            if (adaylar == null || adaylar.Count == 0)
            {
                MessageBox.Show("Verilen koşullara uyan bir daire bulunamadı.", "Daire Çiz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AktifArac = CizimAraci.Sec;
                return;
            }

            if (adaylar.Count == 1)
            {
                Nesneler.Add(adaylar[0]);
                AktifArac = CizimAraci.Sec;
                Invalidate();
                return;
            }

            _daireAdaylari = adaylar;
            _daireAsamasi = DaireAsamasi.CozumSeciliyor;
            Invalidate();
        }

        private void DaireCozumSec(PointF dunyaNoktasi)
        {
            var toleransDunya = SecimToleransPiksel / Gorunum.Olcek;
            DaireNesnesi enYakin = null;
            var enYakinFark = double.MaxValue;

            foreach (var aday in _daireAdaylari)
            {
                var dx = dunyaNoktasi.X - aday.MerkezX;
                var dy = dunyaNoktasi.Y - aday.MerkezY;
                var mesafe = Math.Sqrt(dx * dx + dy * dy);
                var fark = Math.Abs(mesafe - aday.Yaricap);
                if (fark > toleransDunya || fark >= enYakinFark)
                    continue;

                enYakinFark = fark;
                enYakin = aday;
            }

            if (enYakin != null)
                Nesneler.Add(enYakin);

            AktifArac = CizimAraci.Sec;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _sonFareEkranKonumu = e.Location;

            if (_panSurukleniyor)
            {
                var dunyaPanNoktasi = Gorunum.EkrandanDunyaya(e.Location, Width, Height);
                FareKonumuDegisti?.Invoke(this, dunyaPanNoktasi);

                var deltaX = e.Location.X - _panBaslangicEkran.X;
                var deltaY = e.Location.Y - _panBaslangicEkran.Y;
                Gorunum.OfsetX = _panBaslangicOfsetX - deltaX / Gorunum.Olcek;
                Gorunum.OfsetY = _panBaslangicOfsetY + deltaY / Gorunum.Olcek;
                Invalidate();
                return;
            }

            if (_suruklenenNesne != null)
            {
                HassasNoktaAl(e.Location, out var tutamacX, out var tutamacY);
                FareKonumuDegisti?.Invoke(this, new PointF((float)tutamacX, (float)tutamacY));
                if (((ITutamacli)_suruklenenNesne).TutamacTasi(_suruklenenTutamac, tutamacX, tutamacY))
                    TutamacSuruklendi?.Invoke(_suruklenenNesne, TutamacAsamasi.Surukleniyor);
                Invalidate();
                return;
            }

            if (_tutamacSurukleniyor)
            {
                var tutamacNoktasi = DunyaNoktasiHesaplaVeYakala(e.Location);
                FareKonumuDegisti?.Invoke(this, tutamacNoktasi);
                TamKoordinatAl(tutamacNoktasi, out var yeniX, out var yeniY);
                foreach (var uc in _suruklenenUclar)
                {
                    if (uc.Value)
                    {
                        uc.Key.BaslangicX = yeniX;
                        uc.Key.BaslangicY = yeniY;
                    }
                    else
                    {
                        uc.Key.BitisX = yeniX;
                        uc.Key.BitisY = yeniY;
                    }
                }
                Invalidate();
                return;
            }

            if (_secimKutusuBekleniyor)
            {
                var dx = e.Location.X - _secimKutusuBaslangicEkran.X;
                var dy = e.Location.Y - _secimKutusuBaslangicEkran.Y;
                if (!_secimKutusuSurukleniyor && (Math.Abs(dx) > 3 || Math.Abs(dy) > 3))
                    _secimKutusuSurukleniyor = true;

                if (_secimKutusuSurukleniyor)
                    Invalidate();
            }

            var dunyaNoktasi = DunyaNoktasiHesaplaVeYakala(e.Location);
            FareKonumuDegisti?.Invoke(this, dunyaNoktasi);

            if (YuzeyAraciMi(AktifArac) && _yuzeyIlkNokta >= 0)
                Invalidate();

            if ((AktifArac == CizimAraci.Cizgi && _beklemedekiBaslangic != null) || _aktifYakalamaModu != YakalamaModu.Yok
                || (DuzenlemeAraciMi(AktifArac) && _duzenlemeAsamasi == DuzenlemeAsamasi.IkinciNoktaBekleniyor)
                || (DaireAraciMi(AktifArac) && _daireAsamasi == DaireAsamasi.GirdiBekleniyor && _daireNoktalar.Count > 0))
                Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Middle)
                _panSurukleniyor = false;

            if (e.Button == MouseButtons.Left && _suruklenenNesne != null)
            {
                var birakilan = _suruklenenNesne;
                _suruklenenNesne = null;
                _suruklenenTutamac = -1;
                _yakalananDunyaNoktasi = null;
                DegisiklikKaydet();
                TutamacSuruklendi?.Invoke(birakilan, TutamacAsamasi.Bitti);
                NesnelerGuncellendi?.Invoke(this, EventArgs.Empty);
                Invalidate();
                return;
            }

            if (e.Button == MouseButtons.Left && _tutamacSurukleniyor)
            {
                _tutamacSurukleniyor = false;
                _suruklenenUclar.Clear();
                _yakalananDunyaNoktasi = null;
                DegisiklikKaydet();
                NesnelerGuncellendi?.Invoke(this, EventArgs.Empty);
                Invalidate();
                return;
            }

            if (e.Button == MouseButtons.Left && _secimKutusuBekleniyor && AktifArac == CizimAraci.YuzeyCizgiSil)
            {
                var pencereyle = _secimKutusuSurukleniyor;
                _secimKutusuBekleniyor = false;
                _secimKutusuSurukleniyor = false;
                if (pencereyle)
                    YuzeyPencereyleCizgiSil(_secimKutusuBaslangicEkran, e.Location);
                else
                    YuzeyDuzenleTikla(e.Location);
                return;
            }

            if (e.Button == MouseButtons.Left && _secimKutusuBekleniyor)
            {
                if (_secimKutusuSurukleniyor)
                {
                    SecimKutusuTamamla(e.Location);
                }
                else if (!ModifierKeys.HasFlag(Keys.Shift))
                {
                    foreach (var nesne in Nesneler)
                        nesne.Secili = false;
                    SecimBildir();
                    Invalidate();
                }

                _secimKutusuBekleniyor = false;
                _secimKutusuSurukleniyor = false;
            }
        }

        private void SecimKutusuTamamla(Point ekranBitisNoktasi)
        {
            var dunyaBaslangic = Gorunum.EkrandanDunyaya(_secimKutusuBaslangicEkran, Width, Height);
            var dunyaBitis = Gorunum.EkrandanDunyaya(ekranBitisNoktasi, Width, Height);

            var minX = Math.Min(dunyaBaslangic.X, dunyaBitis.X);
            var maxX = Math.Max(dunyaBaslangic.X, dunyaBitis.X);
            var minY = Math.Min(dunyaBaslangic.Y, dunyaBitis.Y);
            var maxY = Math.Max(dunyaBaslangic.Y, dunyaBitis.Y);
            var kutu = RectangleF.FromLTRB(minX, minY, maxX, maxY);

            var gecisSecimi = ekranBitisNoktasi.X < _secimKutusuBaslangicEkran.X;
            var shiftBasili = ModifierKeys.HasFlag(Keys.Shift);

            if (!shiftBasili)
            {
                foreach (var nesne in Nesneler)
                    nesne.Secili = false;
            }

            foreach (var nesne in Nesneler)
            {
                if (!Secilebilir(nesne))
                    continue;

                var uygun = gecisSecimi ? NesneKutuyuKesiyorMu(nesne, kutu) : TumUcNoktalarKutuIcindeMi(nesne, kutu);
                if (!uygun)
                    continue;

                nesne.Secili = shiftBasili ? !nesne.Secili : true;
            }

            SecimBildir();
            Invalidate();
        }

        private static bool TumUcNoktalarKutuIcindeMi(CizimNesnesi nesne, RectangleF kutu)
        {
            var herhangiNoktaVar = false;
            foreach (var nokta in nesne.UcNoktalariAl())
            {
                herhangiNoktaVar = true;
                if (!kutu.Contains(nokta))
                    return false;
            }
            return herhangiNoktaVar;
        }

        private static bool NesneKutuyuKesiyorMu(CizimNesnesi nesne, RectangleF kutu)
        {
            foreach (var nokta in nesne.UcNoktalariAl())
            {
                if (kutu.Contains(nokta))
                    return true;
            }

            switch (nesne)
            {
                case CizgiNesnesi cizgi:
                    return DogruParcasiKutuyuKesiyorMu(cizgi.BaslangicX, cizgi.BaslangicY, cizgi.BitisX, cizgi.BitisY, kutu);
                case DaireNesnesi daire:
                    return DaireKutuyuKesiyorMu(daire.MerkezX, daire.MerkezY, daire.Yaricap, kutu);
                case YayNesnesi yay:
                    return DaireKutuyuKesiyorMu(yay.MerkezX, yay.MerkezY, yay.Yaricap, kutu);
                case SpiralNesnesi spiral:
                    return SpiralKutuyuKesiyorMu(spiral, kutu);
                case CokluDogruNesnesi _:
                case BlokNesnesi _:
                case GuzergahNesnesi _:
                    return GeometriIslemleri.KutuyuKesiyorMu(nesne, kutu.Left, kutu.Top, kutu.Right, kutu.Bottom);
                default:
                    return false;
            }
        }

        private static bool SpiralKutuyuKesiyorMu(SpiralNesnesi spiral, RectangleF kutu)
        {
            var noktalar = spiral.OrnekNoktalariGetir();
            for (var i = 0; i < noktalar.Count - 1; i++)
            {
                if (DogruParcasiKutuyuKesiyorMu(noktalar[i].X, noktalar[i].Y, noktalar[i + 1].X, noktalar[i + 1].Y, kutu))
                    return true;
            }
            return false;
        }

        private static bool DogruParcasiKutuyuKesiyorMu(double x1, double y1, double x2, double y2, RectangleF kutu)
        {
            return DogruDoruKesisiyorMu(x1, y1, x2, y2, kutu.Left, kutu.Top, kutu.Right, kutu.Top)
                || DogruDoruKesisiyorMu(x1, y1, x2, y2, kutu.Right, kutu.Top, kutu.Right, kutu.Bottom)
                || DogruDoruKesisiyorMu(x1, y1, x2, y2, kutu.Right, kutu.Bottom, kutu.Left, kutu.Bottom)
                || DogruDoruKesisiyorMu(x1, y1, x2, y2, kutu.Left, kutu.Bottom, kutu.Left, kutu.Top);
        }

        private static bool DogruDoruKesisiyorMu(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            var payda = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(payda) < 1e-9)
                return false;

            var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / payda;
            var u = ((x1 - x3) * (y1 - y2) - (y1 - y3) * (x1 - x2)) / payda;
            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }

        private static bool DaireKutuyuKesiyorMu(double merkezX, double merkezY, double yaricap, RectangleF kutu)
        {
            var enYakinX = Math.Max(kutu.Left, Math.Min(merkezX, kutu.Right));
            var enYakinY = Math.Max(kutu.Top, Math.Min(merkezY, kutu.Bottom));
            var dx = merkezX - enYakinX;
            var dy = merkezY - enYakinY;
            return dx * dx + dy * dy <= yaricap * yaricap;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var carpan = e.Delta > 0 ? ZoomCarpani : 1.0 / ZoomCarpani;
            Gorunum.ImlecUzerindeYakinlastir(e.Location, Width, Height, carpan);
            Invalidate();
        }

        /// <summary>Seçili objeyi (varsa) properties panelinden güncelleme sonrası yeniden çizmek için.</summary>
        public void SeciliNesneGuncellendi()
        {
            Invalidate();
        }

        /// <summary>Görünümü, çizimdeki bütün objeleri kapsayacak şekilde ortalar ve yakınlaştırır (zoom to fit).</summary>
        public void TumObjeleriGoster()
        {
            if (Nesneler.Count == 0 || Width <= 0 || Height <= 0)
                return;

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            var noktaBulundu = false;

            foreach (var nesne in Nesneler)
            {
                // Kapalı tabakalar ekrana sığdırmada hesaba katılmaz.
                if (!Gorunur(nesne))
                    continue;

                foreach (var nokta in nesne.UcNoktalariAl())
                {
                    noktaBulundu = true;
                    if (nokta.X < minX) minX = nokta.X;
                    if (nokta.X > maxX) maxX = nokta.X;
                    if (nokta.Y < minY) minY = nokta.Y;
                    if (nokta.Y > maxY) maxY = nokta.Y;
                }
            }

            if (!noktaBulundu)
                return;

            const double kenarBosluguCarpani = 1.2;
            const double minimumDunyaBoyutu = 1.0;
            var genislikDunya = Math.Max(maxX - minX, minimumDunyaBoyutu);
            var yukseklikDunya = Math.Max(maxY - minY, minimumDunyaBoyutu);

            Gorunum.OfsetX = (minX + maxX) / 2.0;
            Gorunum.OfsetY = (minY + maxY) / 2.0;

            var olcek = Math.Min(Width / (genislikDunya * kenarBosluguCarpani), Height / (yukseklikDunya * kenarBosluguCarpani));
            Gorunum.Olcek = Math.Max(CizimGorunum.MinOlcek, Math.Min(CizimGorunum.MaxOlcek, olcek));

            Invalidate();
        }
    }
}
