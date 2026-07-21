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
        Cizgi
    }

    /// <summary>Çizim ekranında aynı anda birden fazlası aktif olabilen nokta yakalama (object snap) türleri.</summary>
    [Flags]
    public enum YakalamaModu
    {
        Yok = 0,
        Nokta = 1,
        Uc = 2,
        Orta = 4,
        Dik = 8
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
                AracDegisti?.Invoke(this, _aktifArac);
                Invalidate();
            }
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

        public CizimPaneli()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
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
                if (AktifArac == CizimAraci.Cizgi)
                    AktifArac = CizimAraci.Sec;
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
            else
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

                foreach (var nesne in Nesneler)
                    nesne.Secili = ReferenceEquals(nesne, bulunan);

                SecimDegisti?.Invoke(this, bulunan);
                Invalidate();
            }
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

            var dunyaNoktasi = DunyaNoktasiHesaplaVeYakala(e.Location);
            FareKonumuDegisti?.Invoke(this, dunyaNoktasi);

            if ((AktifArac == CizimAraci.Cizgi && _beklemedekiBaslangic != null) || _aktifYakalamaModu != YakalamaModu.Yok)
                Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Middle)
                _panSurukleniyor = false;
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
