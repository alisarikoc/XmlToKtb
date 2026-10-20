using System;
using System.Drawing;
using System.Windows.Forms;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Çok pencereli görünümde aktif olmayan pencere: asıl çizim panelinin objelerini kendi pan/zoom'uyla gösterir.
    /// Tıklanınca Aktiflestirildi olayı tetiklenir; ana ekran bu pencereyle asıl paneli yer değiştirir.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class GorunumPenceresi : Control
    {
        public CizimPaneli Kaynak { get; }
        public CizimGorunum Gorunum { get; } = new CizimGorunum();

        public event EventHandler Aktiflestirildi;

        public GorunumPenceresi(CizimPaneli kaynak)
        {
            Kaynak = kaynak;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Kaynak.BackColor);
            Kaynak.ArkaPlaniVeNesneleriCiz(e.Graphics, Gorunum, Width, Height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Aktiflestirildi?.Invoke(this, EventArgs.Empty);
        }
    }
}
