using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        DaireTegetUcObje
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
        Merkez = 16
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

        public CizimAraci AktifArac
        {
            get => _aktifArac;
            set
            {
                if (_aktifArac == value)
                    return;
                _aktifArac = value;
                _beklemedekiBaslangic = null;
                _duzenlenecekNesneler.Clear();
                _duzenlemeAsamasi = DuzenlemeAsamasi.ObjeSeciliyor;
                _daireNoktalar.Clear();
                _daireObjeleri.Clear();
                _daireAdaylari = null;
                _daireAsamasi = DaireAsamasi.GirdiBekleniyor;
                Cursor = value == CizimAraci.Sec ? _secAraciImleci : Cursors.Default;

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

        private readonly Cursor _secAraciImleci;

        public CizimPaneli()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            TabStop = true;
            _secAraciImleci = KareImleciOlustur(MilimetreyiPikselYap(2.0));
            Cursor = _secAraciImleci;
            KeyDown += CizimPaneli_KeyDown;
        }

        private void CizimPaneli_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && AktifArac == CizimAraci.Sec)
            {
                SeciliNesneleriSil();
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
                Invalidate();
            }
            else
            {
                foreach (var nesne in _duzenlenecekNesneler)
                    nesne.Secili = false;
                foreach (var nesne in _daireObjeleri)
                    nesne.Secili = false;
                AktifArac = CizimAraci.Sec;
            }
        }

        private void SeciliNesneleriSil()
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

            SecimBildir();
            Invalidate();
        }

        /// <summary>Seçili obje sayısına göre Properties panelini günceller (tek obje seçiliyse onu, aksi halde boşu bildirir).</summary>
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

        private static Cursor KareImleciOlustur(int boyutPiksel)
        {
            using (var bitmap = new Bitmap(boyutPiksel, boyutPiksel))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    using (var kalem = new Pen(Color.Black, 1f))
                        g.DrawRectangle(kalem, 0, 0, boyutPiksel - 1, boyutPiksel - 1);
                }

                var hIcon = bitmap.GetHicon();
                return new Cursor(hIcon);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            GridCiz(e.Graphics);

            foreach (var nesne in Nesneler)
                nesne.Ciz(e.Graphics, Gorunum, Width, Height);

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

        private static void YakalamaIsaretiCiz(Graphics grafik, PointF ekranNoktasi, YakalamaModu turu)
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
            else
            {
                using (var kalem = new Pen(Color.Magenta, 2f))
                    grafik.DrawRectangle(kalem, ekranNoktasi.X - yaricap, ekranNoktasi.Y - yaricap, yaricap * 2, yaricap * 2);
            }
        }

        private void GridCiz(Graphics grafik)
        {
            if (Width <= 0 || Height <= 0)
                return;

            var solUst = Gorunum.EkrandanDunyaya(new PointF(0, 0), Width, Height);
            var sagAlt = Gorunum.EkrandanDunyaya(new PointF(Width, Height), Width, Height);
            var dunyaMinX = Math.Min(solUst.X, sagAlt.X);
            var dunyaMaxX = Math.Max(solUst.X, sagAlt.X);
            var dunyaMinY = Math.Min(solUst.Y, sagAlt.Y);
            var dunyaMaxY = Math.Max(solUst.Y, sagAlt.Y);

            var adim = UygunAdimHesapla(100.0 / Gorunum.Olcek);

            using (var izgaraKalemi = new Pen(Color.Gainsboro, 1f))
            using (var eksenKalemi = new Pen(Color.Silver, 1.5f))
            using (var yaziFirca = new SolidBrush(Color.Gray))
            using (var yaziFontu = new Font(FontFamily.GenericSansSerif, 7.5f))
            {
                var baslangicX = Math.Floor(dunyaMinX / adim) * adim;
                for (var x = baslangicX; x <= dunyaMaxX; x += adim)
                {
                    var ekran = Gorunum.DunyadanEkrana(x, 0, Width, Height);
                    grafik.DrawLine(izgaraKalemi, ekran.X, 0, ekran.X, Height);
                    grafik.DrawString(x.ToString("0.##"), yaziFontu, yaziFirca, ekran.X + 2, 2);
                }

                var baslangicY = Math.Floor(dunyaMinY / adim) * adim;
                for (var y = baslangicY; y <= dunyaMaxY; y += adim)
                {
                    var ekran = Gorunum.DunyadanEkrana(0, y, Width, Height);
                    grafik.DrawLine(izgaraKalemi, 0, ekran.Y, Width, ekran.Y);
                    grafik.DrawString(y.ToString("0.##"), yaziFontu, yaziFirca, 2, ekran.Y + 2);
                }

                if (dunyaMinX <= 0 && dunyaMaxX >= 0)
                {
                    var ekran = Gorunum.DunyadanEkrana(0, 0, Width, Height);
                    grafik.DrawLine(eksenKalemi, ekran.X, 0, ekran.X, Height);
                }
                if (dunyaMinY <= 0 && dunyaMaxY >= 0)
                {
                    var ekran = Gorunum.DunyadanEkrana(0, 0, Width, Height);
                    grafik.DrawLine(eksenKalemi, 0, ekran.Y, Width, ekran.Y);
                }
            }
        }

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

            public YakalamaAdayi(PointF nokta, YakalamaModu tur)
            {
                Nokta = nokta;
                Tur = tur;
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

            foreach (var nesne in Nesneler)
            {
                var adaylar = new List<YakalamaAdayi>();

                if ((_aktifYakalamaModu & (YakalamaModu.Nokta | YakalamaModu.Uc)) != 0)
                {
                    var ucTuru = (_aktifYakalamaModu & YakalamaModu.Uc) != 0 ? YakalamaModu.Uc : YakalamaModu.Nokta;
                    foreach (var uc in nesne.UcNoktalariAl())
                        adaylar.Add(new YakalamaAdayi(uc, ucTuru));
                }

                if ((_aktifYakalamaModu & YakalamaModu.Orta) != 0)
                {
                    var orta = nesne.OrtaNoktaAl();
                    if (orta != null)
                        adaylar.Add(new YakalamaAdayi(orta.Value, YakalamaModu.Orta));
                }

                if ((_aktifYakalamaModu & YakalamaModu.Dik) != 0 && _beklemedekiBaslangic != null)
                {
                    var dik = nesne.DikNoktaAl(_beklemedekiBaslangic.Value);
                    if (dik != null)
                        adaylar.Add(new YakalamaAdayi(dik.Value, YakalamaModu.Dik));
                }

                if ((_aktifYakalamaModu & YakalamaModu.Merkez) != 0)
                {
                    var merkez = nesne.MerkezNoktaAl();
                    if (merkez != null)
                        adaylar.Add(new YakalamaAdayi(merkez.Value, YakalamaModu.Merkez));
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
                if (AktifArac == CizimAraci.Cizgi || AktifArac == CizimAraci.Kes)
                {
                    AktifArac = CizimAraci.Sec;
                }
                else if (DuzenlemeAraciMi(AktifArac))
                {
                    if (_duzenlemeAsamasi == DuzenlemeAsamasi.ObjeSeciliyor && _duzenlenecekNesneler.Count > 0)
                    {
                        _duzenlemeAsamasi = DuzenlemeAsamasi.TabanNoktasiBekleniyor;
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
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            var dunyaNoktasi = DunyaNoktasiHesaplaVeYakala(e.Location);

            if (AktifArac == CizimAraci.Cizgi)
            {
                if (_beklemedekiBaslangic == null)
                {
                    _beklemedekiBaslangic = dunyaNoktasi;
                }
                else
                {
                    var yeniCizgi = new CizgiNesnesi
                    {
                        BaslangicX = _beklemedekiBaslangic.Value.X,
                        BaslangicY = _beklemedekiBaslangic.Value.Y,
                        BitisX = dunyaNoktasi.X,
                        BitisY = dunyaNoktasi.Y
                    };
                    Nesneler.Add(yeniCizgi);
                    _beklemedekiBaslangic = dunyaNoktasi;
                }
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
        }

        private void SecAraciMouseDown(PointF dunyaNoktasi, Point ekranNoktasi)
        {
            var toleransDunya = SecimToleransPiksel / Gorunum.Olcek;
            CizimNesnesi bulunan = null;
            for (var i = Nesneler.Count - 1; i >= 0; i--)
            {
                if (Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
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
                if ((Nesneler[i] is CizgiNesnesi || Nesneler[i] is YayNesnesi || Nesneler[i] is DaireNesnesi) && Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
                {
                    hedef = Nesneler[i];
                    break;
                }
            }

            if (hedef == null)
                return;

            var parcalar = Kesisimler.Kirp(hedef, dunyaNoktasi, Nesneler);
            if (parcalar == null)
                return;

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
                        if (Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
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
                if (DaireTegetObjesiOlabilirMi(Nesneler[i]) && Nesneler[i].IsabetEttiMi(dunyaNoktasi, toleransDunya))
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
