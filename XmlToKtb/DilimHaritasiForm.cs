using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace XmlToKtb
{
    /// <summary>
    /// 3° ve 6° dilimlerin il sınırlarıyla birlikte gösterildiği Türkiye haritası. Haritada bir yere tıklanınca o
    /// yerin boylamı ve düştüğü dilimin orta meridyeni (DOM) seçilir.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class DilimHaritasiForm : Form
    {
        // Haritanın kapsadığı alan (derece).
        private const double BatiBoylam = 25.0, DoguBoylam = 45.5, GuneyEnlem = 35.4, KuzeyEnlem = 42.6;

        /// <summary>Bir ilin adı, sınır halkaları (boylam, enlem) ve adının yazılacağı yer.</summary>
        private class Il
        {
            public string Ad;
            public readonly List<PointF[]> Halkalar = new List<PointF[]>();
            public PointF Merkez;
            public RectangleF Sinir;
        }

        private static List<Il> _iller;

        /// <summary>
        /// İl sınırlarını gömülü Veri\Iller.txt dosyasından okur. Her satır "Ad|boylam enlem,boylam enlem;..." biçimindedir
        /// (halkalar ';' ile ayrılır; adalar ayrı halkadır). Veri okunamazsa harita sınırsız çizilir.
        /// </summary>
        private static List<Il> Iller
        {
            get
            {
                if (_iller != null)
                    return _iller;

                var iller = new List<Il>();
                try
                {
                    var derleme = Assembly.GetExecutingAssembly();
                    var kaynakAdi = derleme.GetManifestResourceNames().FirstOrDefault(a => a.EndsWith("Iller.txt", StringComparison.OrdinalIgnoreCase));
                    if (kaynakAdi != null)
                    {
                        using (var okuyucu = new StreamReader(derleme.GetManifestResourceStream(kaynakAdi), Encoding.UTF8))
                        {
                            string satir;
                            while ((satir = okuyucu.ReadLine()) != null)
                            {
                                var parcalar = satir.Split('|');
                                if (parcalar.Length != 2 || parcalar[1].Length == 0)
                                    continue;

                                var il = new Il { Ad = parcalar[0] };
                                foreach (var halkaMetni in parcalar[1].Split(';'))
                                {
                                    var halka = halkaMetni.Split(',').Select(n =>
                                    {
                                        var xy = n.Split(' ');
                                        return new PointF(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture));
                                    }).ToArray();
                                    if (halka.Length >= 3)
                                        il.Halkalar.Add(halka);
                                }
                                if (il.Halkalar.Count == 0)
                                    continue;

                                // Ad, en büyük halkanın (ana kara parçasının) ağırlık merkezine yazılır.
                                double enBuyukAlan = -1;
                                foreach (var halka in il.Halkalar)
                                {
                                    double alan = 0, mx = 0, my = 0;
                                    for (var i = 0; i < halka.Length; i++)
                                    {
                                        PointF p = halka[i], q = halka[(i + 1) % halka.Length];
                                        var capraz = (double)p.X * q.Y - (double)q.X * p.Y;
                                        alan += capraz;
                                        mx += (p.X + q.X) * capraz;
                                        my += (p.Y + q.Y) * capraz;
                                    }
                                    if (Math.Abs(alan) > enBuyukAlan && Math.Abs(alan) > 1e-9)
                                    {
                                        enBuyukAlan = Math.Abs(alan);
                                        il.Merkez = new PointF((float)(mx / (3 * alan)), (float)(my / (3 * alan)));
                                    }
                                }

                                var tumNoktalar = il.Halkalar.SelectMany(h => h).ToList();
                                il.Sinir = RectangleF.FromLTRB(tumNoktalar.Min(p => p.X), tumNoktalar.Min(p => p.Y), tumNoktalar.Max(p => p.X), tumNoktalar.Max(p => p.Y));
                                iller.Add(il);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    iller.Clear();
                }
                return _iller = iller;
            }
        }

        // Verilen yerin (boylam, enlem) içinde bulunduğu il; hiçbirinin içinde değilse (deniz, yurt dışı) null.
        private static Il IlBul(double boylam, double enlem)
        {
            foreach (var il in Iller)
            {
                if (boylam < il.Sinir.Left || boylam > il.Sinir.Right || enlem < il.Sinir.Top || enlem > il.Sinir.Bottom)
                    continue;
                foreach (var halka in il.Halkalar)
                {
                    var icinde = false;
                    for (int i = 0, j = halka.Length - 1; i < halka.Length; j = i++)
                    {
                        if ((halka[i].Y > enlem) != (halka[j].Y > enlem) &&
                            boylam < (halka[j].X - halka[i].X) * (enlem - halka[i].Y) / (halka[j].Y - halka[i].Y) + halka[i].X)
                            icinde = !icinde;
                    }
                    if (icinde)
                        return il;
                }
            }
            return null;
        }

        private class HaritaPaneli : Panel
        {
            public HaritaPaneli()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        private readonly HaritaPaneli _harita = new HaritaPaneli { Dock = DockStyle.Fill, BackColor = Color.FromArgb(214, 232, 245), Cursor = Cursors.Cross };
        private readonly RadioButton _ucDereceRadio = new RadioButton { Text = "3° dilimler", AutoSize = true, Location = new Point(12, 9), Checked = true };
        private readonly RadioButton _altiDereceRadio = new RadioButton { Text = "6° dilimler (UTM)", AutoSize = true, Location = new Point(110, 9) };
        private readonly Label _bilgiLabel = new Label { AutoSize = true, Location = new Point(250, 11), Text = "Haritada bir yere tıklayın." };
        private readonly Button _tamamButton = new Button { Text = "Tamam", Size = new Size(75, 26), Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        private double _secilenBoylam = double.NaN, _secilenEnlem;

        /// <summary>Tıklanan yerin boylamı (derece).</summary>
        public double SecilenBoylam => _secilenBoylam;
        /// <summary>true: 6° dilim, false: 3° dilim seçildi.</summary>
        public bool AltiDerece => _altiDereceRadio.Checked;

        public DilimHaritasiForm(bool altiDerece, double? boylam)
        {
            Text = "Dilim Haritası - DOM Bul";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1180, 600);
            MinimumSize = new Size(640, 380);

            var ust = new Panel { Dock = DockStyle.Top, Height = 38, Width = ClientSize.Width };
            var iptal = new Button { Text = "İptal", Size = new Size(75, 26), DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _tamamButton.Location = new Point(ClientSize.Width - 170, 6);
            iptal.Location = new Point(ClientSize.Width - 89, 6);
            _tamamButton.DialogResult = DialogResult.OK;
            ust.Controls.AddRange(new Control[] { _ucDereceRadio, _altiDereceRadio, _bilgiLabel, _tamamButton, iptal });
            Controls.Add(_harita);
            Controls.Add(ust);
            AcceptButton = _tamamButton;
            CancelButton = iptal;

            _altiDereceRadio.Checked = altiDerece;
            if (boylam != null && boylam >= BatiBoylam && boylam <= DoguBoylam)
            {
                _secilenBoylam = boylam.Value;
                _secilenEnlem = 39;
                _tamamButton.Enabled = true;
            }

            _ucDereceRadio.CheckedChanged += (s, e) => { BilgiyiGuncelle(); _harita.Invalidate(); };
            _harita.Paint += Harita_Paint;
            _harita.MouseDown += (s, e) =>
            {
                EkrandanCografiye(e.Location, out _secilenBoylam, out _secilenEnlem);
                _tamamButton.Enabled = true;
                BilgiyiGuncelle();
                _harita.Invalidate();
            };
            _harita.MouseDoubleClick += (s, e) => DialogResult = DialogResult.OK;
            BilgiyiGuncelle();
        }

        private void BilgiyiGuncelle()
        {
            if (double.IsNaN(_secilenBoylam))
            {
                _bilgiLabel.Text = "Haritada bir yere tıklayın.";
                return;
            }

            var dom = AltiDerece ? KoordinatSistemiForm.AltiDereceDom(_secilenBoylam) : KoordinatSistemiForm.UcDereceDom(_secilenBoylam);
            var il = IlBul(_secilenBoylam, _secilenEnlem);
            _bilgiLabel.Text = (il != null ? il.Ad + "   " : "") + "Boylam " + _secilenBoylam.ToString("0.00", CultureInfo.InvariantCulture) + "°   →   DOM " + dom +
                               (AltiDerece ? "  (UTM " + ((int)Math.Floor(_secilenBoylam / 6.0) + 31) + "N)" : "  (dilim " + dom / 3 + ")");
        }

        // Harita, enlem-boylamın düz (eş aralıklı) gösterimidir; boylam aralığı orta enlemin kosinüsüyle daraltılır.
        private RectangleF HaritaAlani()
        {
            const float kenar = 14;
            var oran = (DoguBoylam - BatiBoylam) * Math.Cos(39 * Math.PI / 180) / (KuzeyEnlem - GuneyEnlem);
            float genislik = _harita.Width - 2 * kenar, yukseklik = _harita.Height - 2 * kenar;
            if (genislik / yukseklik > oran)
                genislik = (float)(yukseklik * oran);
            else
                yukseklik = (float)(genislik / oran);
            return new RectangleF((_harita.Width - genislik) / 2, (_harita.Height - yukseklik) / 2, genislik, yukseklik);
        }

        private PointF CografidenEkrana(RectangleF alan, double boylam, double enlem)
        {
            return new PointF(
                (float)(alan.Left + (boylam - BatiBoylam) / (DoguBoylam - BatiBoylam) * alan.Width),
                (float)(alan.Top + (KuzeyEnlem - enlem) / (KuzeyEnlem - GuneyEnlem) * alan.Height));
        }

        private void EkrandanCografiye(Point nokta, out double boylam, out double enlem)
        {
            var alan = HaritaAlani();
            boylam = Math.Max(BatiBoylam, Math.Min(DoguBoylam, BatiBoylam + (nokta.X - alan.Left) / alan.Width * (DoguBoylam - BatiBoylam)));
            enlem = Math.Max(GuneyEnlem, Math.Min(KuzeyEnlem, KuzeyEnlem - (nokta.Y - alan.Top) / alan.Height * (KuzeyEnlem - GuneyEnlem)));
        }

        private void Harita_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var alan = HaritaAlani();
            if (alan.Width < 10 || alan.Height < 10)
                return;

            // İllerin ekran koordinatları bir kez hesaplanır; önce kara dolgusu, dilimlerden sonra sınırlar çizilir.
            var ilHalkalari = new List<PointF[]>();
            foreach (var il in Iller)
            {
                foreach (var halka in il.Halkalar)
                    ilHalkalari.Add(halka.Select(p => CografidenEkrana(alan, p.X, p.Y)).ToArray());
            }

            using (var kara = new SolidBrush(Color.FromArgb(246, 243, 232)))
            {
                foreach (var halka in ilHalkalari)
                    g.FillPolygon(kara, halka);
            }

            // Dilimler: 3°'de DOM ± 1.5°, 6°'da DOM ± 3° genişliğinde şeritler.
            double yariGenislik = AltiDerece ? 3 : 1.5, adim = AltiDerece ? 6 : 3;
            var secilenDom = double.IsNaN(_secilenBoylam) ? double.NaN
                : AltiDerece ? KoordinatSistemiForm.AltiDereceDom(_secilenBoylam) : KoordinatSistemiForm.UcDereceDom(_secilenBoylam);
            g.SetClip(alan);
            using (var yazi = new Font(Font.FontFamily, 9f, FontStyle.Bold))
            using (var domKalemi = new Pen(Color.FromArgb(200, 60, 60), 1.2f) { DashStyle = DashStyle.Dash })
            using (var sinirKalemi = new Pen(Color.FromArgb(70, 90, 120), 1.4f))
            {
                var sira = 0;
                for (double dom = 27; dom - yariGenislik < DoguBoylam; dom += adim, sira++)
                {
                    var sol = CografidenEkrana(alan, dom - yariGenislik, KuzeyEnlem);
                    var sag = CografidenEkrana(alan, dom + yariGenislik, GuneyEnlem);
                    var serit = RectangleF.FromLTRB(sol.X, alan.Top, sag.X, alan.Bottom);
                    var secili = dom == secilenDom;
                    using (var firca = new SolidBrush(secili ? Color.FromArgb(90, 255, 200, 40) : sira % 2 == 0 ? Color.FromArgb(38, 60, 120, 200) : Color.FromArgb(38, 60, 180, 90)))
                        g.FillRectangle(firca, serit);
                    g.DrawLine(sinirKalemi, serit.Left, alan.Top, serit.Left, alan.Bottom);
                    g.DrawLine(sinirKalemi, serit.Right, alan.Top, serit.Right, alan.Bottom);

                    var orta = CografidenEkrana(alan, dom, KuzeyEnlem).X;
                    g.DrawLine(domKalemi, orta, alan.Top, orta, alan.Bottom);
                    var etiket = "DOM " + dom.ToString("0") + (AltiDerece ? "\nUTM " + ((int)Math.Floor(dom / 6.0) + 31) + "N" : "\ndilim " + (dom / 3).ToString("0"));
                    var boyut = g.MeasureString(etiket, yazi);
                    using (var bicim = new StringFormat { Alignment = StringAlignment.Center })
                        g.DrawString(etiket, yazi, Brushes.Firebrick, new RectangleF(orta - boyut.Width / 2 - 2, alan.Top + 4, boyut.Width + 4, boyut.Height), bicim);
                }
            }
            g.ResetClip();

            using (var kalem = new Pen(Color.FromArgb(95, 95, 95), 1f) { LineJoin = LineJoin.Round })
            {
                foreach (var halka in ilHalkalari)
                    g.DrawPolygon(kalem, halka);
            }

            // İl adları pencere büyüdükçe büyür; ilin ağırlık merkezine ortalanarak yazılır.
            using (var ilYazisi = new Font(Font.FontFamily, Math.Max(5.5f, Math.Min(10f, alan.Width / 150f))))
            using (var bicim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                foreach (var il in Iller)
                    g.DrawString(il.Ad, ilYazisi, Brushes.Black, CografidenEkrana(alan, il.Merkez.X, il.Merkez.Y), bicim);
            }

            using (var kucukYazi = new Font(Font.FontFamily, 7.5f))
            {
                // Alt kenarda boylam cetveli.
                for (var boylam = 26; boylam <= 45; boylam++)
                {
                    var x = CografidenEkrana(alan, boylam, GuneyEnlem).X;
                    g.DrawLine(Pens.DimGray, x, alan.Bottom - (boylam % 3 == 0 ? 8 : 4), x, alan.Bottom);
                    if (boylam % 3 == 0)
                        g.DrawString(boylam + "°", kucukYazi, Brushes.DimGray, x - 9, alan.Bottom - 21);
                }
            }
            g.DrawRectangle(Pens.Gray, alan.X, alan.Y, alan.Width, alan.Height);

            if (!double.IsNaN(_secilenBoylam))
            {
                var p = CografidenEkrana(alan, _secilenBoylam, _secilenEnlem);
                using (var kalem = new Pen(Color.Red, 2))
                {
                    g.DrawLine(kalem, p.X - 8, p.Y, p.X + 8, p.Y);
                    g.DrawLine(kalem, p.X, p.Y - 8, p.X, p.Y + 8);
                    g.DrawEllipse(kalem, p.X - 5, p.Y - 5, 10, 10);
                }
            }
        }
    }
}
