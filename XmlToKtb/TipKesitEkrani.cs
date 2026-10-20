using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Proje kesiti için tip kesitin çizildiği/DXF'ten alındığı çizim ekranı. (0,0) noktası tip kesitin referans
    /// noktasıdır: X eksene mesafe, Y güzergah (kırmızı) kotuna göre yükseklik farkıdır.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class TipKesitEkrani : Form
    {
        private readonly CizimPaneli _cizimPaneli = new CizimPaneli { Dock = DockStyle.Fill, BackColor = Color.White };
        private readonly PropertyGrid _ozellikGrid = new PropertyGrid { Dock = DockStyle.Fill, ToolbarVisible = false };
        private readonly ToolStripStatusLabel _konumDurumLabel = new ToolStripStatusLabel("X: 0.000   Y: 0.000");
        private readonly List<ToolStripButton> _yakalamaButonlari = new List<ToolStripButton>();
        private ToolStripMenuItem _geriAlMenuItem;
        private ToolStripMenuItem _ilerleMenuItem;

        private ToolStripMenuItem _deverUygulaMenuItem;

        /// <summary>Tamam ile kapatıldığında ekrandaki objeler.</summary>
        public List<CizimNesnesi> Nesneler { get; private set; }

        /// <summary>Proje kesiti oluşturulurken tip kesite dever uygulansın mı?</summary>
        public bool DeverUygula => _deverUygulaMenuItem.Checked;

        /// <summary>Güzergahtan gelen, Dever Listesi'nde elle değiştirilebilen dever tablosu.</summary>
        public DeverTablosu DeverTablosu { get; private set; }

        private readonly string _guzergahYolu;

        /// <summary>
        /// Dever listesi güzergah dosyasına kaydedildiyse dosyaya yazılan tablo (kaydedilmediyse null). Ekrandan
        /// İptal ile çıkılsa bile dosya değiştiği için çağıran taraf bu tabloyu esas almalıdır.
        /// </summary>
        public DeverTablosu DosyayaKaydedilenDeverTablosu { get; private set; }

        public TipKesitEkrani(IEnumerable<CizimNesnesi> mevcutNesneler, bool deverUygula, DeverTablosu deverTablosu, string guzergahYolu)
        {
            _guzergahYolu = guzergahYolu;
            // İptal ile çıkıldığında asıl tablo değişmesin diye kopya üzerinde çalışılır.
            DeverTablosu = (deverTablosu ?? new DeverTablosu()).Kopyala();

            Text = "Tip Kesit";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1100, 650);
            MinimumSize = new Size(700, 450);
            ShowInTaskbar = false;

            var ozelliklerPaneli = new Panel { Dock = DockStyle.Left, Width = 260 };
            ozelliklerPaneli.Controls.Add(_ozellikGrid);
            ozelliklerPaneli.Controls.Add(new Label
            {
                Text = "Özellikler",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(6, 4, 0, 4),
                Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold)
            });

            var tamamButton = new Button { Text = "Tamam", Size = new Size(90, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var iptalButton = new Button { Text = "İptal", Size = new Size(90, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            tamamButton.Location = new Point(altPanel.Width - 196, 7);
            iptalButton.Location = new Point(altPanel.Width - 100, 7);
            altPanel.Controls.Add(new Label
            {
                Text = "Kırmızı artı (0,0) tip kesitin referans noktasıdır. Tip kesit uç uca bağlı tek parça çizgilerden oluşmalıdır.",
                Location = new Point(8, 13),
                AutoSize = true
            });
            altPanel.Controls.Add(tamamButton);
            altPanel.Controls.Add(iptalButton);
            tamamButton.Click += TamamButton_Click;
            CancelButton = iptalButton;

            var durumStrip = new StatusStrip();
            durumStrip.Items.Add(_konumDurumLabel);
            var komutDurumLabel = new ToolStripStatusLabel { ForeColor = Color.Firebrick };
            durumStrip.Items.Add(komutDurumLabel);

            var menu = MenuOlustur();
            var aracCubugu = YakalamaCubuguOlustur();

            Controls.Add(_cizimPaneli);
            Controls.Add(ozelliklerPaneli);
            Controls.Add(altPanel);
            Controls.Add(durumStrip);
            Controls.Add(aracCubugu);
            Controls.Add(menu);
            MainMenuStrip = menu;

            _cizimPaneli.SecimDegisti += (s, nesne) => _ozellikGrid.SelectedObject = nesne;
            _cizimPaneli.FareKonumuDegisti += (s, konum) => _konumDurumLabel.Text = $"X: {konum.X:0.000}   Y: {konum.Y:0.000}";
            _cizimPaneli.NesnelerGuncellendi += (s, e) => _ozellikGrid.Refresh();
            _cizimPaneli.GecmisDegisti += (s, e) => GeriAlMenusunuGuncelle();
            _cizimPaneli.UstCizim += (s, e) => DeverOnizlemesiCiz(e.Graphics);
            _deverUygulaMenuItem.Checked = deverUygula;
            _ozellikGrid.PropertyValueChanged += (s, e) =>
                _cizimPaneli.OzellikDegisti(_ozellikGrid.SelectedObject, e.ChangedItem.PropertyDescriptor?.Name, e.OldValue);

            // Tip kesit de kesit gibi tek parça çizgi dizisidir: uçlar tutamaçla sündürülür, ortak uçlar birlikte taşınır.
            _cizimPaneli.TutamacSuruklemeAcik = true;
            _cizimPaneli.OrtakUclariBirlikteTasi = true;
            _cizimPaneli.YeniNoktalariMilimetreyeYuvarla = true;
            _cizimPaneli.ReferansNoktasiGoster = true;
            _cizimPaneli.GeriAlAcik = true;

            // Sağ tık menüsü (son komutlar, pano, hızlı seçim, bul/değiştir...) ve Boşluk ile komut tekrarı.
            new PanelKomutMenusu(_cizimPaneli, this)
            {
                Degisti = () => _ozellikGrid.Refresh(),
                Ipucu = metin => komutDurumLabel.Text = string.IsNullOrEmpty(metin) ? "" : "   " + metin
            };

            if (mevcutNesneler != null)
                _cizimPaneli.Nesneler.AddRange(mevcutNesneler.Select(NesneDonusumleri.Klonla));
            _cizimPaneli.GecmisiTemizle();
            GeriAlMenusunuGuncelle();

            // Uç yakalama açık başlar; referans noktası da bu modda yakalanır.
            _yakalamaButonlari[1].Checked = true;

            Shown += (s, e) =>
            {
                if (_cizimPaneli.Nesneler.Count > 0)
                    _cizimPaneli.TumObjeleriGoster();
                else
                    _cizimPaneli.Gorunum.Olcek = 40; // boş ekranda birkaç metrelik bir alan görünsün
                _cizimPaneli.Invalidate();
            };
        }

        private MenuStrip MenuOlustur()
        {
            var menu = new MenuStrip();

            var dosya = new ToolStripMenuItem("Dosya");
            dosya.DropDownItems.Add("Aç (DXF)", null, DosyaAc_Click);
            dosya.DropDownItems.Add("Kaydet (DXF)", null, DosyaKaydet_Click);

            var cizim = new ToolStripMenuItem("Çizim");
            cizim.DropDownItems.Add("Çizgi Çiz", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Cizgi);
            var daire = new ToolStripMenuItem("Daire Çiz");
            daire.DropDownItems.Add("Merkez ve Yarıçap", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.DaireMerkezYaricap);
            daire.DropDownItems.Add("3 Nokta", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Daire3Nokta);
            daire.DropDownItems.Add("2 Nokta ve Yarıçap", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Daire2NoktaYaricap);
            cizim.DropDownItems.Add(daire);

            var duzenle = new ToolStripMenuItem("Düzenle");
            _geriAlMenuItem = new ToolStripMenuItem("Geri Al", null, (s, e) => _cizimPaneli.GeriAl()) { ShortcutKeys = Keys.Control | Keys.Z };
            _ilerleMenuItem = new ToolStripMenuItem("İlerle", null, (s, e) => _cizimPaneli.Ilerle()) { ShortcutKeys = Keys.Control | Keys.Y };
            duzenle.DropDownItems.Add(_geriAlMenuItem);
            duzenle.DropDownItems.Add(_ilerleMenuItem);
            duzenle.DropDownItems.Add(new ToolStripSeparator());
            duzenle.DropDownItems.Add("Kaydır", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Kaydir);
            duzenle.DropDownItems.Add("Döndür", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Dondur);
            duzenle.DropDownItems.Add("Kes", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Kes);
            duzenle.DropDownItems.Add("Kopyala", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Kopyala);
            duzenle.DropDownItems.Add("Ölçekle", null, (s, e) => _cizimPaneli.AktifArac = CizimAraci.Olcekle);

            var dever = new ToolStripMenuItem("Dever");
            _deverUygulaMenuItem = new ToolStripMenuItem("Dever Uygula") { CheckOnClick = true };
            _deverUygulaMenuItem.CheckedChanged += (s, e) => _cizimPaneli.Invalidate();
            dever.DropDownItems.Add(_deverUygulaMenuItem);
            dever.DropDownItems.Add("Dever Listesi...", null, DeverListesi_Click);

            menu.Items.Add(dosya);
            menu.Items.Add(cizim);
            menu.Items.Add(duzenle);
            menu.Items.Add(dever);
            return menu;
        }

        private void DeverListesi_Click(object sender, EventArgs e)
        {
            using (var dlg = new DeverListesiForm(DeverTablosu))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                var onceki = DeverTablosu.Kopyala();
                DeverTablosu.Satirlar.Clear();
                DeverTablosu.Satirlar.AddRange(dlg.Satirlar);
                _cizimPaneli.Invalidate();

                if (!DeverTablosu.AyniMi(onceki))
                    DeveriGuzergahDosyasinaKaydetmeyiSor();
            }
        }

        /// <summary>Dever listesi elle değiştirildiğinde, değişikliğin güzergah dosyasına yazılmasını kullanıcıya sorar.</summary>
        private void DeveriGuzergahDosyasinaKaydetmeyiSor()
        {
            if (_guzergahYolu == null)
                return;

            var dosyaAdi = System.IO.Path.GetFileName(_guzergahYolu);
            var uzanti = System.IO.Path.GetExtension(_guzergahYolu);
            if (uzanti == null || !uzanti.ToLower().Equals(".xml"))
            {
                MessageBox.Show(
                    "Dever listesi değişti. Değişiklik proje kesitlerine uygulanacak, ancak " + dosyaAdi + " dosyasına kaydedilemiyor: " +
                    "dever yalnızca XML güzergah dosyalarına yazılabilir.",
                    "Dever Listesi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cevap = MessageBox.Show(
                "Dever listesi değişti. Değişiklik " + dosyaAdi + " güzergah dosyasına kaydedilsin mi?\n\n" +
                "Evet: dosyadaki dever istasyonları bu listeyle değiştirilir (eski dosya " + dosyaAdi + ".bak olarak saklanır).\n" +
                "Hayır: değişiklik yalnızca bu oturumdaki proje kesitlerine uygulanır.",
                "Dever Listesi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (cevap != DialogResult.Yes)
                return;

            try
            {
                System.IO.File.Copy(_guzergahYolu, _guzergahYolu + ".bak", true);
                DeverTablosu.XmlDosyasinaKaydet(_guzergahYolu);
                DosyayaKaydedilenDeverTablosu = DeverTablosu.Kopyala();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dever güzergah dosyasına kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// "Dever Uygula" tikliyken, tip kesitin dever tablosundaki en büyük sağ (cw) ve en büyük sol (ccw) devere göre
        /// alacağı hali kesikli çizgiyle gösterir. Bu çizimler sanaldır; seçilemez ve tip kesite dahil değildir.
        /// </summary>
        private void DeverOnizlemesiCiz(Graphics grafik)
        {
            if (!_deverUygulaMenuItem.Checked)
                return;

            var enBuyukCw = DeverTablosu.Satirlar.Where(s => s.Yon != "ccw").Select(s => s.Dever).DefaultIfEmpty(0).Max();
            var enBuyukCcw = DeverTablosu.Satirlar.Where(s => s.Yon == "ccw").Select(s => s.Dever).DefaultIfEmpty(0).Max();

            DeverliKesitCiz(grafik, enBuyukCw, "cw", Color.DarkOrange);
            DeverliKesitCiz(grafik, enBuyukCcw, "ccw", Color.MediumPurple);
        }

        private void DeverliKesitCiz(Graphics grafik, double deverMm, string yon, Color renk)
        {
            if (deverMm <= 0)
                return;

            PointF? etiketYeri = null;
            using (var kalem = new Pen(renk, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                foreach (var cizgi in _cizimPaneli.Nesneler.OfType<CizgiNesnesi>())
                {
                    var bas = DeverTablosu.Uygula(new PointD(cizgi.BaslangicX, cizgi.BaslangicY), deverMm, yon);
                    var bit = DeverTablosu.Uygula(new PointD(cizgi.BitisX, cizgi.BitisY), deverMm, yon);
                    var basEkran = _cizimPaneli.Gorunum.DunyadanEkrana(bas.X, bas.Y, _cizimPaneli.Width, _cizimPaneli.Height);
                    var bitEkran = _cizimPaneli.Gorunum.DunyadanEkrana(bit.X, bit.Y, _cizimPaneli.Width, _cizimPaneli.Height);
                    grafik.DrawLine(kalem, basEkran, bitEkran);

                    // Etiket, deverin yükselttiği taraftaki en uç noktaya konur ki iki önizleme üst üste gelmesin.
                    foreach (var ekran in new[] { basEkran, bitEkran })
                    {
                        var dahaUcta = etiketYeri == null || (yon == "ccw" ? ekran.X > etiketYeri.Value.X : ekran.X < etiketYeri.Value.X);
                        if (dahaUcta)
                            etiketYeri = ekran;
                    }
                }
            }

            if (etiketYeri == null)
                return;

            using (var firca = new SolidBrush(renk))
            {
                var metin = "Dever " + deverMm.ToString("0.#") + " mm (" + yon + ")";
                var boyut = grafik.MeasureString(metin, Font);
                var x = yon == "ccw" ? etiketYeri.Value.X + 6 : etiketYeri.Value.X - boyut.Width - 6;
                // Tip kesit ekranı doldurduğunda etiket kenardan taşmasın.
                x = Math.Max(4, Math.Min(x, _cizimPaneli.Width - boyut.Width - 4));
                grafik.DrawString(metin, Font, firca, x, etiketYeri.Value.Y - boyut.Height - 2);
            }
        }

        private ToolStrip YakalamaCubuguOlustur()
        {
            var cubuk = new ToolStrip();
            cubuk.Items.Add(new ToolStripSeparator { Alignment = ToolStripItemAlignment.Right });

            // Sıra YakalamaModunuGuncelle ile eşleşir: Nokta, Uç, Orta, Dik, Merkez, Kesişim.
            var tanimlar = new[]
            {
                new[] { "Nokta", "Nokta Yakala" },
                new[] { "Uç", "Doğru Ucu Yakala" },
                new[] { "Orta", "Doğru Ortası Yakala" },
                new[] { "Dik", "Objeye Dik Yakala" },
                new[] { "Merkez", "Daire/Yay Merkezi Yakala" },
                new[] { "Kesişim", "Kesişim Yakala" }
            };

            foreach (var tanim in tanimlar)
            {
                var buton = new ToolStripButton(tanim[0])
                {
                    Alignment = ToolStripItemAlignment.Right,
                    CheckOnClick = true,
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    ToolTipText = tanim[1]
                };
                buton.CheckedChanged += (s, e) => YakalamaModunuGuncelle();
                _yakalamaButonlari.Add(buton);
                cubuk.Items.Add(buton);
            }

            return cubuk;
        }

        private void YakalamaModunuGuncelle()
        {
            var mod = YakalamaModu.Yok;
            if (_yakalamaButonlari[0].Checked) mod |= YakalamaModu.Nokta;
            if (_yakalamaButonlari[1].Checked) mod |= YakalamaModu.Uc;
            if (_yakalamaButonlari[2].Checked) mod |= YakalamaModu.Orta;
            if (_yakalamaButonlari[3].Checked) mod |= YakalamaModu.Dik;
            if (_yakalamaButonlari[4].Checked) mod |= YakalamaModu.Merkez;
            if (_yakalamaButonlari[5].Checked) mod |= YakalamaModu.Kesisim;
            _cizimPaneli.AktifYakalamaModu = mod;
        }

        private void GeriAlMenusunuGuncelle()
        {
            _geriAlMenuItem.Enabled = _cizimPaneli.GeriAlinabilir;
            _ilerleMenuItem.Enabled = _cizimPaneli.Ilerlenebilir;
        }

        private void DosyaAc_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "DXF Dosyası Seçin", Filter = "DXF (*.dxf)|*.dxf" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                netDxf.DxfDocument dxfBelge;
                try
                {
                    dxfBelge = netDxf.DxfDocument.Load(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (dxfBelge == null)
                {
                    MessageBox.Show("DXF dosyası okunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _cizimPaneli.Nesneler.AddRange(DxfIceAktarici.Ayikla(dxfBelge));
                _cizimPaneli.DegisiklikKaydet();
                _cizimPaneli.TumObjeleriGoster();
                _cizimPaneli.Invalidate();
            }
        }

        private void DosyaKaydet_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog { Title = "DXF Olarak Kaydet", Filter = "DXF (*.dxf)|*.dxf", FileName = "tipkesit.dxf" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    DxfDisaAktarici.Kaydet(_cizimPaneli.Nesneler, dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void TamamButton_Click(object sender, EventArgs e)
        {
            if (!TipKesitNoktalariniGetir(_cizimPaneli.Nesneler, out _, out var hata))
            {
                MessageBox.Show(hata, "Tip Kesit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cizimPaneli.Invalidate();
            Nesneler = _cizimPaneli.Nesneler.ToList();
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Objelerdeki çizgileri soldan sağa tek parça olarak sıralayıp tip kesit noktalarını (X = mesafe, Y = kot farkı)
        /// verir. Çizgi yoksa ya da çizgiler tek parça değilse false döner ve nedenini <paramref name="hata"/> ile bildirir.
        /// Çizgi dışındaki objeler (daire vb.) yardımcı çizim sayılır, tip kesite girmez.
        /// </summary>
        public static bool TipKesitNoktalariniGetir(IEnumerable<CizimNesnesi> nesneler, out List<PointD> noktalar, out string hata)
        {
            noktalar = new List<PointD>();
            hata = null;

            var cizgiler = nesneler.OfType<CizgiNesnesi>().ToList();
            if (cizgiler.Count == 0)
            {
                hata = "Tip kesitte hiç çizgi yok. Tip kesiti çizin ya da DXF dosyasından açın.";
                return false;
            }

            if (!KseKesit.TekParcaOlarakSirala(cizgiler, out var sirali) || sirali.Count == 0)
            {
                hata = "Tip kesit çizgileri tek parça değil (kopuk, dallanan ya da kapalı çizgi var).\n\n" +
                       "Tip kesit soldan başlayıp sağda biten, uç uca bağlı tek bir çizgi dizisi olmalıdır.";
                return false;
            }

            noktalar.Add(new PointD(sirali[0].BaslangicX, sirali[0].BaslangicY));
            foreach (var cizgi in sirali)
                noktalar.Add(new PointD(cizgi.BitisX, cizgi.BitisY));
            return true;
        }
    }

    /// <summary>Çift hassasiyetli 2B nokta.</summary>
    public struct PointD
    {
        public readonly double X;
        public readonly double Y;

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
