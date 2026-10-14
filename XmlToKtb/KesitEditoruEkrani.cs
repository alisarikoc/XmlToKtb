using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    public partial class KesitEditoruEkrani : Form
    {
        /// <summary>Editörde açık bir KSE dosyası ve Dosyalar listesindeki durumu.</summary>
        private class AcikDosya
        {
            public KseDosyasi Dosya;
            public string Yol;
            public bool Gorunur = true;
            public Color Renk;
            /// <summary>Proje menüsünden üretilen (henüz diske yazılmamış) proje kesiti dosyası mı?</summary>
            public bool ProjeKesitiMi;
            public string Ad => Path.GetFileName(Yol);
        }

        /// <summary>O an çizim ekranında duran bir kesit ve ait olduğu dosya.</summary>
        private class GosterilenKesit
        {
            public AcikDosya Dosya;
            public KseKesit Kesit;
        }

        private static readonly Color[] DosyaRenkleri =
        {
            Color.Black, Color.Red, Color.Blue, Color.Green, Color.DarkOrange, Color.Purple, Color.Teal, Color.Brown
        };

        private readonly List<AcikDosya> _dosyalar = new List<AcikDosya>();
        /// <summary>Kesitler listesindeki kilometreleri veren dosya; yeni çizilen çizgiler de bu dosyanın kesitine eklenir.</summary>
        private AcikDosya _referans;
        /// <summary>Referans dosyanın Kesitler listesinde seçili olan kesiti.</summary>
        private KseKesit _referansKesit;
        private readonly List<GosterilenKesit> _gosterilenler = new List<GosterilenKesit>();
        private readonly Dictionary<CizgiNesnesi, KseKesit> _cizgiSahibi = new Dictionary<CizgiNesnesi, KseKesit>();

        // Proje Kesiti penceresindeki son seçimler; pencere yeniden açıldığında geri yüklenir.
        private readonly ProjeKesitiAyarlari _projeAyarlari = new ProjeKesitiAyarlari();

        public KesitEditoruEkrani()
        {
            InitializeComponent();
            cizimPaneli1.SecimDegisti += CizimPaneli1_SecimDegisti;
            cizimPaneli1.FareKonumuDegisti += CizimPaneli1_FareKonumuDegisti;
            cizimPaneli1.NesnelerGuncellendi += (s, e) => ozellikGrid.Refresh();

            // Kesit çizgileri uç uca tek parça olduğundan uçlar tutamaçla sündürülür ve ortak uçlar birlikte taşınır.
            cizimPaneli1.TutamacSuruklemeAcik = true;
            cizimPaneli1.OrtakUclariBirlikteTasi = true;
            cizimPaneli1.OrtakUcGrubu = CizgininKesitiniGetir;
            cizimPaneli1.YeniNoktalariMilimetreyeYuvarla = true;
            cizimPaneli1.GeriAlAcik = true;
            cizimPaneli1.GecmisDegisti += (s, e) => GeriAlMenusunuGuncelle();
            GeriAlMenusunuGuncelle();
            ucYakalaButton.Checked = true;

            // Proje menüsünden sonra: kesitlerin noktaları üzerinde toplu işlemler.
            var kesitIslemleriMenuItem = new ToolStripMenuItem("Kesit İşlemleri");
            kesitIslemleriMenuItem.DropDownItems.Add("Nokta Sil (ekrandan)", null, (s, e) => NoktaSilmeyiBaslat());
            kesitIslemleriMenuItem.DropDownItems.Add("Kesit Boyunca Sık Noktaları Temizle...", null, (s, e) => SikNoktalariTemizle());
            cizimPaneli1.NoktaTiklandi += NoktaSilTiklandi;
            cizimPaneli1.NoktaAlmaBitti += (s, e) =>
            {
                _noktaSiliniyor = false;
                konumDurumLabel.Text = "";
            };
            anaMenuStrip.Items.Insert(anaMenuStrip.Items.IndexOf(projeMenuItem) + 1, kesitIslemleriMenuItem);
        }

        /// <summary>
        /// Kesit boyunca birbirine verilen hassasiyetten (mesafe-kot düzleminde) daha yakın olan noktaları atar: kesitin
        /// ilk ve son noktası her zaman korunur, aradaki her nokta son tutulan noktaya en az hassasiyet kadar uzaksa
        /// tutulur. Ekrandaki kesite ya da referans dosyanın bütün kesitlerine uygulanır.
        /// </summary>
        private void SikNoktalariTemizle()
        {
            if (_referans == null)
            {
                MessageBox.Show("Önce bir KSE dosyası açın.", "Sık Noktaları Temizle", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!GosterilenleriGuncelle())
                return;

            double hassasiyet;
            bool hepsi;
            using (var dlg = new Form
            {
                Text = "Sık Noktaları Temizle",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(360, 150)
            })
            {
                dlg.Controls.Add(new Label { Text = "Hassasiyet (m):", Location = new Point(12, 17), AutoSize = true });
                var kutu = new TextBox { Text = "0.10", Location = new Point(150, 14), Size = new Size(90, 20) };
                var ekrandaki = new RadioButton { Text = "Yalnızca ekrandaki kesit (Km " + (_referansKesit?.Kilometre ?? "-") + ")", Location = new Point(12, 46), AutoSize = true, Enabled = _referansKesit != null };
                var tumu = new RadioButton { Text = _referans.Ad + " dosyasının bütün kesitleri", Location = new Point(12, 70), AutoSize = true, Checked = true };
                var tamam = new Button { Text = "Tamam", Location = new Point(192, 112), Size = new Size(75, 26), DialogResult = DialogResult.OK };
                var iptal = new Button { Text = "İptal", Location = new Point(273, 112), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { kutu, ekrandaki, tumu, tamam, iptal });
                dlg.AcceptButton = tamam;
                dlg.CancelButton = iptal;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                if (!NcnDosyasi.SayiOku(kutu.Text, out hassasiyet) || hassasiyet <= 0)
                {
                    MessageBox.Show("Hassasiyet sıfırdan büyük bir sayı olmalıdır.", "Sık Noktaları Temizle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                hepsi = tumu.Checked;
            }

            var kesitler = hepsi ? _referans.Dosya.Kesitler : new List<KseKesit> { _referansKesit };
            int onceki = 0, sonraki = 0, degisenKesit = 0;
            foreach (var kesit in kesitler)
            {
                onceki += kesit.Noktalar.Count;
                var temiz = SikNoktalariAyikla(kesit.Noktalar, hassasiyet);
                sonraki += temiz.Count;
                if (temiz.Count == kesit.Noktalar.Count)
                    continue;
                kesit.Noktalar = temiz;
                kesit.CizgileriOlustur();
                degisenKesit++;
            }

            if (_referansKesit != null)
                KesitiCiz(_referansKesit, false);
            MessageBox.Show(
                kesitler.Count + " kesit tarandı, " + degisenKesit + " kesitte " + (onceki - sonraki) + " nokta silindi (" + onceki + " → " + sonraki + ").\n\n" +
                "Bu işlem geri alınamaz; sonucu beğenmezseniz dosyayı kaydetmeden kapatıp yeniden açın.",
                "Sık Noktaları Temizle", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Nokta Sil komutu açık mı (çizim paneli nokta bekliyor).
        private bool _noktaSiliniyor;

        private void NoktaSilmeyiBaslat()
        {
            if (_referansKesit == null)
            {
                MessageBox.Show("Önce bir kesit açın.", "Nokta Sil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _noktaSiliniyor = true;
            cizimPaneli1.NoktaAlmayaBasla(true);
            cizimPaneli1.Focus();
            konumDurumLabel.Text = "Nokta Sil: silinecek kesit noktasını tıklayın. Bitirmek için sağ tık ya da Esc.";
        }

        /// <summary>
        /// Tıklanan kesit noktasını siler: o noktada birleşen iki çizgiden biri atılır, diğeri ilerideki noktaya
        /// uzatılır; böylece gerideki ve ilerideki nokta birbirine bağlanır. Kesitin uç noktası silinirse uçtaki çizgi
        /// atılır. İşlem Geri Al ile geri alınabilir.
        /// </summary>
        private void NoktaSilTiklandi(double x, double y)
        {
            if (!_noktaSiliniyor)
                return;

            // Tıklanan yere ekranda en yakın çizgi ucu (en çok 10 piksel).
            var tolerans = 10 / cizimPaneli1.Gorunum.Olcek;
            CizgiNesnesi bulunan = null;
            double px = 0, py = 0, enYakin = tolerans * tolerans;
            foreach (var cizgi in cizimPaneli1.Nesneler.OfType<CizgiNesnesi>())
            {
                foreach (var uc in new[] { new[] { cizgi.BaslangicX, cizgi.BaslangicY }, new[] { cizgi.BitisX, cizgi.BitisY } })
                {
                    var uzaklik = (uc[0] - x) * (uc[0] - x) + (uc[1] - y) * (uc[1] - y);
                    if (uzaklik > enYakin)
                        continue;
                    enYakin = uzaklik;
                    bulunan = cizgi;
                    px = uc[0];
                    py = uc[1];
                }
            }
            if (bulunan == null)
                return;

            bool AyniNokta(double ax, double ay) => Math.Abs(ax - px) <= CizimPaneli.UcEsitlikToleransi && Math.Abs(ay - py) <= CizimPaneli.UcEsitlikToleransi;

            // Aynı kesitte bu noktaya dokunan çizgiler.
            var kesit = CizgininKesitiniGetir(bulunan);
            var dokunanlar = cizimPaneli1.Nesneler.OfType<CizgiNesnesi>()
                .Where(c => Equals(CizgininKesitiniGetir(c), kesit) && (AyniNokta(c.BaslangicX, c.BaslangicY) || AyniNokta(c.BitisX, c.BitisY)))
                .ToList();

            if (dokunanlar.Count == 2)
            {
                CizgiNesnesi kalan = dokunanlar[0], atilan = dokunanlar[1];
                // Atılan çizginin öbür ucu (ilerideki ya da gerideki nokta).
                var digerBaslangic = !AyniNokta(atilan.BaslangicX, atilan.BaslangicY);
                double ox = digerBaslangic ? atilan.BaslangicX : atilan.BitisX, oy = digerBaslangic ? atilan.BaslangicY : atilan.BitisY;
                if (AyniNokta(kalan.BaslangicX, kalan.BaslangicY))
                {
                    kalan.BaslangicX = ox;
                    kalan.BaslangicY = oy;
                }
                else
                {
                    kalan.BitisX = ox;
                    kalan.BitisY = oy;
                }
                cizimPaneli1.Nesneler.Remove(atilan);
                _cizgiSahibi.Remove(atilan);
            }
            else if (dokunanlar.Count == 1)
            {
                cizimPaneli1.Nesneler.Remove(dokunanlar[0]);
                _cizgiSahibi.Remove(dokunanlar[0]);
            }
            else
            {
                MessageBox.Show("Bu noktada " + dokunanlar.Count + " çizgi birleşiyor; kesit tek parça olmadığı için nokta silinemedi.", "Nokta Sil", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ozellikGrid.SelectedObject = null;
            cizimPaneli1.DegisiklikKaydet();
            cizimPaneli1.Invalidate();
        }

        private static List<KseNokta> SikNoktalariAyikla(List<KseNokta> noktalar, double hassasiyet)
        {
            if (noktalar.Count <= 2)
                return new List<KseNokta>(noktalar);

            double Uzaklik(KseNokta a, KseNokta b)
            {
                double dm = a.Mesafe - b.Mesafe, dk = a.Kot - b.Kot;
                return Math.Sqrt(dm * dm + dk * dk);
            }

            var sonuc = new List<KseNokta> { noktalar[0] };
            for (var i = 1; i < noktalar.Count - 1; i++)
            {
                if (Uzaklik(noktalar[i], sonuc[sonuc.Count - 1]) >= hassasiyet)
                    sonuc.Add(noktalar[i]);
            }

            // Son nokta her zaman kalır; ondan önce tutulan nokta ona çok yakınsa (ve ilk nokta değilse) atılır.
            var son = noktalar[noktalar.Count - 1];
            if (sonuc.Count > 1 && Uzaklik(son, sonuc[sonuc.Count - 1]) < hassasiyet)
                sonuc.RemoveAt(sonuc.Count - 1);
            sonuc.Add(son);
            return sonuc;
        }

        private void GeriAlMenusunuGuncelle()
        {
            geriAlMenuItem.Enabled = cizimPaneli1.GeriAlinabilir;
            ilerleMenuItem.Enabled = cizimPaneli1.Ilerlenebilir;
        }

        private void geriAlMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.GeriAl();
        }

        private void ilerleMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.Ilerle();
        }

        private void yakalamaButonu_CheckedChanged(object sender, EventArgs e)
        {
            var mod = YakalamaModu.Yok;
            if (noktaYakalaButton.Checked) mod |= YakalamaModu.Nokta;
            if (ucYakalaButton.Checked) mod |= YakalamaModu.Uc;
            if (ortaYakalaButton.Checked) mod |= YakalamaModu.Orta;
            if (dikYakalaButton.Checked) mod |= YakalamaModu.Dik;
            if (merkezYakalaButton.Checked) mod |= YakalamaModu.Merkez;
            cizimPaneli1.AktifYakalamaModu = mod;
        }

        /// <summary>Çizginin ait olduğu kesit; henüz bir kesite işlenmemiş (yeni çizilmiş) çizgi referans dosyanın kesitine aittir.</summary>
        private object CizgininKesitiniGetir(CizgiNesnesi cizgi)
        {
            if (_cizgiSahibi.TryGetValue(cizgi, out var kesit))
                return kesit;
            return _gosterilenler.FirstOrDefault(g => g.Dosya == _referans)?.Kesit;
        }

        /// <summary>
        /// Ekrandaki kesitlerin çizgilerini kesit modellerine işler. Bir kesitin çizgileri tek parça değilse
        /// kullanıcıyı uyarır ve false döner; bu durumda kesitten çıkılmamalı/kaydedilmemelidir.
        /// </summary>
        private bool GosterilenleriGuncelle()
        {
            var paneldekiCizgiler = cizimPaneli1.Nesneler.OfType<CizgiNesnesi>().ToList();
            var sahipliCizgiler = new HashSet<CizgiNesnesi>(_gosterilenler.SelectMany(g => g.Kesit.Cizgiler));

            foreach (var gosterilen in _gosterilenler)
            {
                // Hiçbir kesite ait olmayan (yeni çizilmiş) çizgiler referans dosyanın kesitine eklenir.
                var kendiCizgileri = new HashSet<CizgiNesnesi>(gosterilen.Kesit.Cizgiler);
                var yenileriAlir = gosterilen.Dosya == _referans;
                var cizgiler = paneldekiCizgiler
                    .Where(c => kendiCizgileri.Contains(c) || (yenileriAlir && !sahipliCizgiler.Contains(c)))
                    .Cast<CizimNesnesi>().ToList();

                if (!gosterilen.Kesit.CizimdenGuncelle(cizgiler))
                {
                    MessageBox.Show(
                        gosterilen.Dosya.Ad + " dosyasındaki kesitin çizgileri tek parça değil (kopuk, dallanan ya da kapalı çizgi var).\n\n" +
                        "Kesit soldan başlayıp sağda biten, uç uca bağlı tek bir çizgi dizisi olmalıdır. " +
                        "Lütfen çizgileri uçlarından birleştirip tekrar deneyin.",
                        "Kesit " + gosterilen.Kesit.No + " - Km " + gosterilen.Kesit.Kilometre, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                foreach (var cizgi in gosterilen.Kesit.Cizgiler)
                {
                    cizgi.Renk = gosterilen.Dosya.Renk;
                    _cizgiSahibi[cizgi] = gosterilen.Kesit;
                }
            }

            // Sıralama sırasında yapılan yön çevirme/uç birleştirme kullanıcı düzeltmesi değildir; geri al adımı sayılmaz.
            cizimPaneli1.SonDurumuGuncelle();
            cizimPaneli1.Invalidate();
            return true;
        }

        /// <summary>
        /// Referans dosyanın verilen kesitini ve tikli diğer dosyaların aynı kilometredeki kesitlerini çizer.
        /// Çağırmadan önce ekrandaki değişiklikler GosterilenleriGuncelle ile işlenmiş olmalıdır.
        /// </summary>
        private void KesitiCiz(KseKesit referansKesit, bool ekranaSigdir)
        {
            _referansKesit = referansKesit;
            _gosterilenler.Clear();
            _cizgiSahibi.Clear();

            cizimPaneli1.AktifArac = CizimAraci.Sec;
            cizimPaneli1.Nesneler.Clear();
            ozellikGrid.SelectedObject = null;

            foreach (var dosya in _dosyalar)
            {
                if (!dosya.Gorunur)
                    continue;

                var kesit = dosya == _referans ? referansKesit : dosya.Dosya.KmdenBul(referansKesit.Km);
                if (kesit == null)
                    continue;

                _gosterilenler.Add(new GosterilenKesit { Dosya = dosya, Kesit = kesit });
                foreach (var cizgi in kesit.Cizgiler)
                {
                    cizgi.Secili = false;
                    cizgi.Renk = dosya.Renk;
                    _cizgiSahibi[cizgi] = kesit;
                }
                cizimPaneli1.Nesneler.AddRange(kesit.Cizgiler);
            }

            // Geri al geçmişi ekrandaki kesite aittir; ekrana başka kesit gelince sıfırlanır.
            cizimPaneli1.GecmisiTemizle();

            if (ekranaSigdir)
                cizimPaneli1.TumObjeleriGoster();
            cizimPaneli1.Invalidate();
        }

        /// <summary>
        /// Verilen dosyayı kilometre referansı yapar: Kesitler listesi bu dosyanın kesitleriyle dolar ve (varsa) aynı
        /// kilometredeki kesit çizilir. Çağırmadan önce ekrandaki değişiklikler işlenmiş olmalıdır.
        /// </summary>
        private void ReferansAyarla(AcikDosya dosya)
        {
            var oncekiKm = _referansKesit != null ? _referansKesit.Km : double.NaN;
            _referans = dosya;
            Text = "Kesit Editörü - Referans: " + dosya.Ad;

            foreach (ListViewItem satir in dosyaListView.Items)
            {
                var referansMi = satir.Tag == dosya;
                satir.SubItems[1].Text = referansMi ? "Referans" : "";
                satir.Font = new Font(dosyaListView.Font, referansMi ? FontStyle.Bold : FontStyle.Regular);
            }

            var secilecek = dosya.Dosya.KmdenBul(oncekiKm) ?? dosya.Dosya.Kesitler[0];
            // Seçim olayı aynı kesiti yeniden çizmeye kalkmasın diye kesit, liste doldurulmadan önce atanır.
            _referansKesit = secilecek;

            kesitListView.BeginUpdate();
            kesitListView.Items.Clear();
            foreach (var kesit in dosya.Dosya.Kesitler)
            {
                var satir = new ListViewItem(new[] { kesit.No, kesit.Kilometre, kesit.Aciklama }) { Tag = kesit };
                kesitListView.Items.Add(satir);
                if (kesit == secilecek)
                {
                    satir.Selected = true;
                    satir.Focused = true;
                }
            }
            kesitListView.EndUpdate();
            if (kesitListView.SelectedItems.Count > 0)
                kesitListView.SelectedItems[0].EnsureVisible();

            KesitiCiz(secilecek, true);
        }

        private void CizimPaneli1_SecimDegisti(object sender, CizimNesnesi secilenNesne)
        {
            ozellikGrid.SelectedObject = secilenNesne;
        }

        private void ozellikGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            // Bir ucun koordinatı elle değiştirildiyse, eski noktayı paylaşan komşu çizginin ucu da taşınır.
            cizimPaneli1.OzellikDegisti(ozellikGrid.SelectedObject, e.ChangedItem.PropertyDescriptor?.Name, e.OldValue);
        }

        private void CizimPaneli1_FareKonumuDegisti(object sender, System.Drawing.PointF dunyaKonumu)
        {
            konumDurumLabel.Text = $"X: {dunyaKonumu.X:0.000}   Y: {dunyaKonumu.Y:0.000}";
        }

        private void dosyaAcMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "KSE Dosyası Seçin", Filter = "KSE (*.kse)|*.kse", Multiselect = true })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;
                DosyalariAc(dlg.FileNames);
            }
        }

        /// <summary>Verilen KSE dosyalarını açar; son açılan kilometre referansı olur.</summary>
        public void DosyalariAc(IEnumerable<string> dosyaYollari)
        {
            {
                // Referans değişeceği için ekrandaki kesitin değişiklikleri önce modele işlenir.
                if (!GosterilenleriGuncelle())
                    return;

                AcikDosya sonEklenen = null;
                foreach (var dosyaYolu in dosyaYollari)
                {
                    var zatenAcik = _dosyalar.FirstOrDefault(d => string.Equals(d.Yol, dosyaYolu, StringComparison.OrdinalIgnoreCase));
                    if (zatenAcik != null)
                    {
                        sonEklenen = zatenAcik;
                        continue;
                    }

                    KseDosyasi kseDosyasi;
                    try
                    {
                        kseDosyasi = KseDosyasi.Oku(dosyaYolu);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Path.GetFileName(dosyaYolu) + " okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }

                    if (kseDosyasi.Kesitler.Count == 0)
                    {
                        MessageBox.Show(Path.GetFileName(dosyaYolu) + " dosyasında kesit bulunamadı.", "Kesit Editörü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    var dosya = new AcikDosya { Dosya = kseDosyasi, Yol = dosyaYolu, Renk = DosyaRenkleri[_dosyalar.Count % DosyaRenkleri.Length] };
                    _dosyalar.Add(dosya);
                    dosyaListView.Items.Add(new ListViewItem(new[] { dosya.Ad, "" })
                    {
                        Tag = dosya,
                        Checked = true,
                        ForeColor = dosya.Renk,
                        ToolTipText = dosyaYolu
                    });
                    sonEklenen = dosya;
                }

                // Son yüklenen dosya kilometre referansı olur.
                if (sonEklenen != null)
                    ReferansAyarla(sonEklenen);
            }
        }

        private void dosyaListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            var dosya = (AcikDosya)e.Item.Tag;
            if (dosya == null || dosya.Gorunur == e.Item.Checked)
                return;

            // Ekrandaki değişiklikler işlenemiyorsa (kopuk kesit) görünürlük değiştirilmez, tik geri alınır.
            if (!GosterilenleriGuncelle())
            {
                e.Item.Checked = dosya.Gorunur;
                return;
            }

            dosya.Gorunur = e.Item.Checked;
            if (_referansKesit != null)
                KesitiCiz(_referansKesit, false);
        }

        private void dosyaMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = dosyaListView.SelectedItems.Count == 0;
        }

        private void referansYapMenuItem_Click(object sender, EventArgs e)
        {
            if (dosyaListView.SelectedItems.Count == 0)
                return;

            var dosya = (AcikDosya)dosyaListView.SelectedItems[0].Tag;
            if (dosya == _referans || !GosterilenleriGuncelle())
                return;

            ReferansAyarla(dosya);
        }

        private void projeKesitiOlusturMenuItem_Click(object sender, EventArgs e)
        {
            if (_referans == null)
            {
                MessageBox.Show(
                    "Proje kesiti, açık olan referans KSE dosyasının kilometrelerinde oluşturulur. Önce bir KSE dosyası açın.",
                    "Proje Kesiti", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!GosterilenleriGuncelle())
                return;

            KseDosyasi projeKesitleri;
            using (var dlg = new ProjeKesitiForm(_referans.Dosya, _projeAyarlari))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                projeKesitleri = dlg.Sonuc;
            }

            // Önceki proje kesiti varsa yenisiyle değiştirilir.
            var eskiProje = _dosyalar.FirstOrDefault(d => d.ProjeKesitiMi);
            var eskiProjeReferanstiMi = eskiProje != null && eskiProje == _referans;
            if (eskiProje != null)
            {
                _dosyalar.Remove(eskiProje);
                foreach (ListViewItem satir in dosyaListView.Items)
                {
                    if (satir.Tag != eskiProje)
                        continue;
                    dosyaListView.Items.Remove(satir);
                    break;
                }
            }

            var klasor = Path.GetDirectoryName(eskiProjeReferanstiMi ? eskiProje.Yol : _referans.Yol) ?? "";
            var proje = new AcikDosya
            {
                Dosya = projeKesitleri,
                Yol = Path.Combine(klasor, "Proje Kesiti.kse"),
                Renk = Color.Red,
                ProjeKesitiMi = true
            };
            _dosyalar.Add(proje);
            dosyaListView.Items.Add(new ListViewItem(new[] { proje.Ad, "" })
            {
                Tag = proje,
                Checked = true,
                ForeColor = proje.Renk,
                ToolTipText = "Proje kesiti (henüz kaydedilmedi)"
            });

            if (eskiProjeReferanstiMi)
                ReferansAyarla(proje);
            else
                KesitiCiz(_referansKesit, false);
        }

        private void dosyaOzelliklerMenuItem_Click(object sender, EventArgs e)
        {
            if (dosyaListView.SelectedItems.Count == 0)
                return;

            var satir = dosyaListView.SelectedItems[0];
            var dosya = (AcikDosya)satir.Tag;

            using (var dlg = new Form
            {
                Text = "Özellikler - " + dosya.Ad,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(380, 150)
            })
            {
                var dosyaLabel = new Label { Text = "Dosya:", Location = new Point(12, 15), AutoSize = true };
                var yolLabel = new Label { Text = dosya.Yol, Location = new Point(90, 15), Size = new Size(278, 30), AutoEllipsis = true };
                var kesitLabel = new Label { Text = "Kesit sayısı:", Location = new Point(12, 50), AutoSize = true };
                var kesitSayisiLabel = new Label { Text = dosya.Dosya.Kesitler.Count.ToString(), Location = new Point(90, 50), AutoSize = true };
                var renkLabel = new Label { Text = "Renk:", Location = new Point(12, 82), AutoSize = true };
                var renkButton = new Button
                {
                    Location = new Point(90, 77),
                    Size = new Size(80, 23),
                    BackColor = dosya.Renk,
                    FlatStyle = FlatStyle.Flat
                };
                var tamamButton = new Button { Text = "Tamam", Location = new Point(212, 115), Size = new Size(75, 23), DialogResult = DialogResult.OK };
                var iptalButton = new Button { Text = "İptal", Location = new Point(293, 115), Size = new Size(75, 23), DialogResult = DialogResult.Cancel };

                renkButton.Click += (s, a) =>
                {
                    using (var renkDlg = new ColorDialog { Color = renkButton.BackColor, FullOpen = true })
                    {
                        if (renkDlg.ShowDialog(dlg) == DialogResult.OK)
                            renkButton.BackColor = renkDlg.Color;
                    }
                };

                dlg.Controls.AddRange(new Control[] { dosyaLabel, yolLabel, kesitLabel, kesitSayisiLabel, renkLabel, renkButton, tamamButton, iptalButton });
                dlg.AcceptButton = tamamButton;
                dlg.CancelButton = iptalButton;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                dosya.Renk = renkButton.BackColor;
            }

            satir.ForeColor = dosya.Renk;
            foreach (var gosterilen in _gosterilenler.Where(g => g.Dosya == dosya))
            {
                foreach (var cizgi in gosterilen.Kesit.Cizgiler)
                    cizgi.Renk = dosya.Renk;
            }
            cizimPaneli1.Invalidate();
        }

        private void dosyaKapatMenuItem_Click(object sender, EventArgs e)
        {
            if (dosyaListView.SelectedItems.Count == 0)
                return;

            var satir = dosyaListView.SelectedItems[0];
            var dosya = (AcikDosya)satir.Tag;

            var cevap = MessageBox.Show(
                dosya.Ad + " kapatılsın mı?\n\nBu dosyada kaydedilmemiş değişiklikler varsa kaybolur.",
                "Dosyayı Kapat", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (cevap != DialogResult.Yes)
                return;

            // Kapatılan dosyanın kesiti kopuk olsa da sorun değil; onu ekrandan düşüp kalanları modele işleriz.
            // Çizgileri önce ekrandan kaldırılır ki referans kesite "yeni çizgi" olarak eklenmesinler.
            foreach (var gosterilen in _gosterilenler.Where(g => g.Dosya == dosya))
                cizimPaneli1.Nesneler.RemoveAll(n => n is CizgiNesnesi c && gosterilen.Kesit.Cizgiler.Contains(c));
            _gosterilenler.RemoveAll(g => g.Dosya == dosya);
            if (!GosterilenleriGuncelle())
            {
                // Kalan dosyalardan biri kopuk: kapatma iptal edilir, ekran eski haline döner.
                if (_referansKesit != null)
                    KesitiCiz(_referansKesit, false);
                return;
            }

            _dosyalar.Remove(dosya);
            dosyaListView.Items.Remove(satir);

            if (_dosyalar.Count == 0)
            {
                _referans = null;
                _referansKesit = null;
                _gosterilenler.Clear();
                _cizgiSahibi.Clear();
                kesitListView.Items.Clear();
                cizimPaneli1.AktifArac = CizimAraci.Sec;
                cizimPaneli1.Nesneler.Clear();
                cizimPaneli1.GecmisiTemizle();
                cizimPaneli1.Invalidate();
                ozellikGrid.SelectedObject = null;
                Text = "Kesit Editörü";
            }
            else if (dosya == _referans)
            {
                // Referans kapatıldıysa en son açılan dosya referans olur.
                ReferansAyarla(_dosyalar[_dosyalar.Count - 1]);
            }
            else
            {
                KesitiCiz(_referansKesit, false);
            }
        }

        private void kesitListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (kesitListView.SelectedItems.Count == 0)
                return;

            var kesit = (KseKesit)kesitListView.SelectedItems[0].Tag;
            if (kesit == _referansKesit)
                return;

            // Ekrandaki kesitte yapılan değişiklikler, kesit ekrandan kaldırılmadan önce modele işlenir.
            // Çizgiler tek parça değilse kesitten çıkılmaz; listedeki seçim eski kesite geri alınır.
            if (!GosterilenleriGuncelle())
            {
                BeginInvoke((Action)(() =>
                {
                    foreach (ListViewItem satir in kesitListView.Items)
                    {
                        if (satir.Tag != _referansKesit)
                            continue;
                        satir.Selected = true;
                        satir.Focused = true;
                        break;
                    }
                }));
                return;
            }

            KesitiCiz(kesit, true);
        }

        private void dosyaKaydetMenuItem_Click(object sender, EventArgs e)
        {
            if (_referans == null)
            {
                MessageBox.Show("Kaydetmek için önce bir KSE dosyası açın.", "Kesit Editörü", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!GosterilenleriGuncelle())
                return;

            // Referans dosya kaydedilir.
            using (var dlg = new SaveFileDialog
            {
                Title = "KSE Olarak Kaydet - " + _referans.Ad,
                Filter = "KSE (*.kse)|*.kse",
                FileName = _referans.Ad,
                InitialDirectory = Path.GetDirectoryName(_referans.Yol)
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    File.WriteAllText(dlg.FileName, _referans.Dosya.Yaz(), Encoding.Default);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("KSE dosyası kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var kaydedilmeyenSayisi = cizimPaneli1.Nesneler.Count(n => !(n is CizgiNesnesi));
                if (kaydedilmeyenSayisi > 0)
                {
                    MessageBox.Show(
                        "KSE dosyasına yalnızca çizgiler yazılır; ekrandaki " + kaydedilmeyenSayisi + " obje kaydedilmedi.",
                        "Kesit Editörü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void cizgiCizMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Cizgi;
        }

        private void daireMerkezYaricapMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.DaireMerkezYaricap;
        }

        private void daire3NoktaMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Daire3Nokta;
        }

        private void kaydirMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kaydir;
        }

        private void dondurMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Dondur;
        }

        private void kesMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kes;
        }

        private void kopyalaMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kopyala;
        }

        private void olcekleMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Olcekle;
        }
    }
}
