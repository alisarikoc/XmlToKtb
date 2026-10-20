using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace XmlToKtb
{
    /// <summary>
    /// Özellikler paneli: açılışta kapalıdır; sağ tık menüsünden (ya da Görünüm menüsünden) açılır. Ekranın herhangi bir
    /// yerinde yüzen, taşınabilir, boyutlandırılabilir ve kapatılabilir bir pencerede durur. Pencere çizim ekranının sol
    /// kenarına bırakılınca eski yerine (solda sabit panel) oturur; sabit paneldeyken başlığından sürüklenince yeniden
    /// yüzen pencere olur, başlıktaki ✕ ile kapanır.
    /// </summary>
    public partial class CizimEkrani
    {
        private const int YapismaMesafesi = 40;

        private Form _ozellikPenceresi;
        private bool _ozellikYapisik;
        private int _ozellikGenisligi;
        private Point _baslikBasilanYer;
        private bool _baslikBasili;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private void OzellikleriKur()
        {
            _ozellikGenisligi = ozelliklerPaneli.Width;
            ozelliklerPaneli.Visible = false;

            // Sabit paneldeyken başlıkta kapatma düğmesi; başlıktan sürükleyince panel yüzen pencere olur.
            var kapat = new Label
            {
                Text = "✕", Dock = DockStyle.Right, Width = 24, TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9f)
            };
            new ToolTip().SetToolTip(kapat, "Özellikleri kapat");
            kapat.Click += (s, e) => OzellikleriKapat();
            kapat.MouseEnter += (s, e) => kapat.ForeColor = Color.Firebrick;
            kapat.MouseLeave += (s, e) => kapat.ForeColor = SystemColors.ControlText;
            ozelliklerBaslikLabel.Controls.Add(kapat);
            ozelliklerBaslikLabel.Cursor = Cursors.SizeAll;
            new ToolTip().SetToolTip(ozelliklerBaslikLabel, "Sürükleyerek ayrı pencere yapın");
            ozelliklerBaslikLabel.MouseDown += (s, e) =>
            {
                _baslikBasili = e.Button == MouseButtons.Left;
                _baslikBasilanYer = e.Location;
            };
            ozelliklerBaslikLabel.MouseUp += (s, e) => _baslikBasili = false;
            ozelliklerBaslikLabel.MouseMove += (s, e) =>
            {
                if (!_baslikBasili || !_ozellikYapisik)
                    return;
                var surukleme = SystemInformation.DragSize;
                if (Math.Abs(e.X - _baslikBasilanYer.X) < surukleme.Width && Math.Abs(e.Y - _baslikBasilanYer.Y) < surukleme.Height)
                    return;
                _baslikBasili = false;
                OzellikleriSurukleyerekAyir(_baslikBasilanYer);
            };
        }

        /// <summary>Özellikler açık mı (yüzen pencerede ya da solda sabit).</summary>
        private bool OzelliklerAcik => _ozellikYapisik ? ozelliklerPaneli.Visible : _ozellikPenceresi?.Visible == true;

        /// <summary>Özellikleri açar: son kullanılan yerde (ilk açılışta çizim alanının solunda yüzen pencere).</summary>
        private void OzellikleriGoster()
        {
            if (_ozellikYapisik)
            {
                ozelliklerPaneli.Visible = true;
                return;
            }
            var pencere = OzellikPenceresiniAl();
            if (ozelliklerPaneli.Parent != pencere)
                PaneliPencereyeTasi(pencere);
            if (!pencere.Visible)
                pencere.Show(this);
            pencere.Activate();
        }

        private void OzellikleriKapat()
        {
            if (_ozellikYapisik)
                ozelliklerPaneli.Visible = false;
            else
                _ozellikPenceresi?.Hide();
            cizimPaneli1.Focus();
        }

        private void OzellikleriAcKapat()
        {
            if (OzelliklerAcik)
                OzellikleriKapat();
            else
                OzellikleriGoster();
        }

        private Form OzellikPenceresiniAl()
        {
            if (_ozellikPenceresi != null && !_ozellikPenceresi.IsDisposed)
                return _ozellikPenceresi;
            var alan = RectangleToScreen(ClientRectangle);
            var pencere = new Form
            {
                Text = "Özellikler", FormBorderStyle = FormBorderStyle.SizableToolWindow, ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual, MinimumSize = new Size(200, 200),
                Size = new Size(Math.Max(280, _ozellikGenisligi + 20), Math.Min(620, Math.Max(300, alan.Height - 200))),
                Location = new Point(alan.Left + 80, alan.Top + 160)
            };
            // Kapatma pencereyi yok etmez, gizler; ana ekran kapanırken (sahip kapanışı) normal kapanır.
            pencere.FormClosing += (s, e) =>
            {
                if (e.CloseReason != CloseReason.UserClosing)
                    return;
                e.Cancel = true;
                OzellikleriKapat();
            };
            // Taşırken sol kenar yapışma bölgesindeyse saydamlaşarak oturacağını gösterir; bırakınca oturur.
            pencere.Move += (s, e) => pencere.Opacity = pencere.Visible && YapismaBolgesinde(pencere) ? 0.6 : 1.0;
            pencere.ResizeEnd += (s, e) =>
            {
                pencere.Opacity = 1.0;
                if (YapismaBolgesinde(pencere))
                    OzellikleriYapistir();
            };
            _ozellikPenceresi = pencere;
            return pencere;
        }

        // Pencerenin sol kenarı çizim ekranının sol kenarına yakın ve dikeyde ekranla örtüşüyor mu?
        private bool YapismaBolgesinde(Form pencere)
        {
            var alan = RectangleToScreen(ClientRectangle);
            return Math.Abs(pencere.Left - alan.Left) <= YapismaMesafesi
                   && pencere.Bottom > alan.Top && pencere.Top < alan.Bottom;
        }

        private void PaneliPencereyeTasi(Form pencere)
        {
            SuspendLayout();
            if (ozelliklerPaneli.Parent == this)
                _ozellikGenisligi = ozelliklerPaneli.Width;
            ozelliklerBaslikLabel.Visible = false;
            ozelliklerPaneli.Parent = pencere;
            ozelliklerPaneli.Dock = DockStyle.Fill;
            ozelliklerPaneli.Visible = true;
            _ozellikYapisik = false;
            ResumeLayout(true);
        }

        /// <summary>Yüzen pencereyi kapatıp paneli eski yerine (çizim alanının solunda, menülerin altında) oturtur.</summary>
        private void OzellikleriYapistir()
        {
            SuspendLayout();
            _ozellikPenceresi?.Hide();
            ozelliklerPaneli.Parent = this;
            ozelliklerPaneli.Dock = DockStyle.Left;
            ozelliklerPaneli.Width = _ozellikGenisligi;
            ozelliklerBaslikLabel.Visible = true;
            ozelliklerPaneli.Visible = true;
            // Doldurma (Fill) kontrolünün hemen arkasında olmalı ki menü, ribbon ve sekmelerin altında kalsın.
            var dolgu = Controls.Cast<Control>().FirstOrDefault(c => c.Dock == DockStyle.Fill && c != ozelliklerPaneli);
            if (dolgu != null)
                Controls.SetChildIndex(ozelliklerPaneli, Controls.GetChildIndex(dolgu) + 1);
            _ozellikYapisik = true;
            ResumeLayout(true);
        }

        // Sabit panelin başlığından sürüklenince: panel yüzen pencereye geçer ve pencere fareyle taşınmaya devam eder.
        private void OzellikleriSurukleyerekAyir(Point basilanYer)
        {
            var ekranYeri = Cursor.Position;
            var boyut = ozelliklerPaneli.Size;
            var pencere = OzellikPenceresiniAl();
            pencere.Size = new Size(Math.Max(pencere.MinimumSize.Width, boyut.Width + 16), Math.Max(pencere.MinimumSize.Height, Math.Min(boyut.Height, 620)));
            var baslikYuksekligi = SystemInformation.ToolWindowCaptionHeight;
            pencere.Location = new Point(ekranYeri.X - Math.Min(basilanYer.X, pencere.Width - 40), ekranYeri.Y - baslikYuksekligi / 2);
            PaneliPencereyeTasi(pencere);
            if (!pencere.Visible)
                pencere.Show(this);
            // Pencereyi başlığından tutulmuş gibi taşımaya başla (Windows'un taşıma döngüsü; bırakınca ResizeEnd gelir).
            ReleaseCapture();
            ozelliklerBaslikLabel.Capture = false;
            const int WM_NCLBUTTONDOWN = 0xA1, HTCAPTION = 2;
            SendMessage(pencere.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }
}
