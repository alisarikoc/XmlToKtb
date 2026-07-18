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

    /// <summary>AutoCAD benzeri pan/zoom yapılabilen, çizim yapılabilen koordinat düzlemi.</summary>
    public class CizimPaneli : Panel
    {
        private const float SecimToleransPiksel = 5f;
        private const double ZoomCarpani = 1.15;

        public readonly List<CizimNesnesi> Nesneler = new List<CizimNesnesi>();
        public readonly CizimGorunum Gorunum = new CizimGorunum();

        private CizimAraci _aktifArac = CizimAraci.Sec;
        private PointF? _beklemedekiBaslangic;
        private PointF _sonFareEkranKonumu;
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button == MouseButtons.Middle)
            {
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

            var dunyaNoktasi = Gorunum.EkrandanDunyaya(e.Location, Width, Height);

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

            var dunyaNoktasi = Gorunum.EkrandanDunyaya(e.Location, Width, Height);
            FareKonumuDegisti?.Invoke(this, dunyaNoktasi);

            if (_panSurukleniyor)
            {
                var deltaX = e.Location.X - _panBaslangicEkran.X;
                var deltaY = e.Location.Y - _panBaslangicEkran.Y;
                Gorunum.OfsetX = _panBaslangicOfsetX - deltaX / Gorunum.Olcek;
                Gorunum.OfsetY = _panBaslangicOfsetY + deltaY / Gorunum.Olcek;
                Invalidate();
                return;
            }

            if (AktifArac == CizimAraci.Cizgi && _beklemedekiBaslangic != null)
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
    }
}
