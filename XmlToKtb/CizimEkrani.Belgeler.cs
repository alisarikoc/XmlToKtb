using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;
using XmlToKtb.Projeksiyon;

namespace XmlToKtb
{
    /// <summary>
    /// Çok belgeli çizim ekranı: her açılan dosya (ya da Yeni) ayrı bir belgedir ve en üstte sekme olarak görünür;
    /// sekmeye tıklanınca o belgeye geçilir.
    /// <para>Çizim paneli tektir (bütün komutlar ona bağlıdır). Belgeye geçerken mevcut belgenin objeleri, tabakaları,
    /// görünümü, geri al geçmişi ve koordinat sistemi saklanır, geçilen belgeninkiler panele yüklenir. Pano formda
    /// tutulduğu için belgeler arasında kes/kopyala/yapıştır çalışır.</para>
    /// </summary>
    public partial class CizimEkrani
    {
        private sealed class CizimBelgesi
        {
            public string Ad;
            public string DosyaYolu;
            public bool Degisti;
            public CizimPaneli.BelgeDurumu Durum;
            public ProjeksiyonSistemi Sistem = ProjeksiyonSistemi.Tanimsiz;
            public DilimBilgisi? Dilim;
        }

        private readonly List<CizimBelgesi> _belgeler = new List<CizimBelgesi>();
        private CizimBelgesi _aktifBelge;
        private TabControl _belgeSekmeleri;
        private int _yeniSayaci;
        private bool _belgeDegistiriliyor;

        private void BelgeleriKur()
        {
            _belgeSekmeleri = new TabControl { Dock = DockStyle.Top, Height = 24, Padding = new System.Drawing.Point(10, 3), ShowToolTips = true };
            Controls.Add(_belgeSekmeleri);
            // Menü ve araç çubuğunun altında, çizim alanının (ve Özellikler panelinin) üstünde dursun.
            Controls.SetChildIndex(_belgeSekmeleri, Controls.GetChildIndex(toolStrip1));
            _belgeSekmeleri.SelectedIndexChanged += (s, e) =>
            {
                if (!_belgeDegistiriliyor && _belgeSekmeleri.SelectedTab?.Tag is CizimBelgesi belge)
                    BelgeyeGec(belge);
            };
            var sekmeMenusu = new ContextMenuStrip();
            TabPage menuSekmesi = null;
            sekmeMenusu.Items.Add("Kapat", null, (s, e) => { if (menuSekmesi?.Tag is CizimBelgesi b) BelgeyiKapat(b); });
            sekmeMenusu.Items.Add("Diğerlerini Kapat", null, (s, e) =>
            {
                if (!(menuSekmesi?.Tag is CizimBelgesi b))
                    return;
                foreach (var diger in _belgeler.Where(x => x != b).ToList())
                    if (!BelgeyiKapat(diger))
                        break;
            });
            _belgeSekmeleri.MouseUp += (s, e) =>
            {
                var sekme = TiklananSekme(e.Location);
                if (sekme == null)
                    return;
                if (e.Button == MouseButtons.Right)
                {
                    menuSekmesi = sekme;
                    sekmeMenusu.Show(_belgeSekmeleri, e.Location);
                }
                else if (e.Button == MouseButtons.Middle && sekme.Tag is CizimBelgesi b)
                {
                    BelgeyiKapat(b);
                }
            };

            // Dosya menüsü: Yeni (en üstte), Aç (yeni sekmede), Ekle (aktif belgeye), Kaydet, Kapat.
            dosyaAcMenuItem.Text = "Aç...";
            dosyaAcMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            dosyaAcMenuItem.ToolTipText = "Dosyayı (DXF/DWG) yeni bir sekmede açar";
            dosyaKaydetMenuItem.Text = "Kaydet (DXF)...";
            dosyaKaydetMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            var yeni = new ToolStripMenuItem("Yeni", null, (s, e) => YeniBelge()) { ShortcutKeys = Keys.Control | Keys.N, ToolTipText = "Boş bir çizim sekmesi açar" };
            var ekle = new ToolStripMenuItem("Ekle...", null, (s, e) => DosyaAc(false)) { ToolTipText = "Dosyadaki (DXF/DWG) objeleri aktif çizime ekler" };
            var kapat = new ToolStripMenuItem("Kapat", null, (s, e) => { if (_aktifBelge != null) BelgeyiKapat(_aktifBelge); }) { ShortcutKeys = Keys.Control | Keys.F4 };
            dosyaMenuItem.DropDownItems.Insert(0, yeni);
            dosyaMenuItem.DropDownItems.Insert(dosyaMenuItem.DropDownItems.IndexOf(dosyaAcMenuItem) + 1, ekle);
            dosyaMenuItem.DropDownItems.Insert(dosyaMenuItem.DropDownItems.IndexOf(dosyaKaydetMenuItem) + 1, kapat);
            dosyaMenuItem.DropDownItems.Insert(dosyaMenuItem.DropDownItems.IndexOf(kapat) + 1, new ToolStripSeparator());

            // Aktif belgede kaydedilecek bir değişiklik olunca sekmede "*" görünür.
            cizimPaneli1.GecmisDegisti += (s, e) =>
            {
                if (_belgeDegistiriliyor || _aktifBelge == null || _aktifBelge.Degisti || !cizimPaneli1.GeriAlinabilir)
                    return;
                _aktifBelge.Degisti = true;
                SekmeMetniniGuncelle(_aktifBelge);
            };

            FormClosing += (s, e) =>
            {
                if (e.Cancel)
                    return;
                BelgeDurumunuSakla();
                var degisenler = _belgeler.Where(b => b.Degisti && b.Durum.NesneSayisi > 0).Select(b => b.Ad).ToList();
                if (degisenler.Count == 0)
                    return;
                var cevap = MessageBox.Show(this, "Kaydedilmemiş değişiklikleri olan çizimler var:\n\n" + string.Join("\n", degisenler) + "\n\nYine de kapatılsın mı?",
                    "Çizim Ekranı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (cevap != DialogResult.Yes)
                    e.Cancel = true;
            };

            // Ekranın ilk (boş) çizimi Yeni1'dir.
            YeniBelge();
        }

        private TabPage TiklananSekme(System.Drawing.Point konum)
        {
            for (var i = 0; i < _belgeSekmeleri.TabCount; i++)
                if (_belgeSekmeleri.GetTabRect(i).Contains(konum))
                    return _belgeSekmeleri.TabPages[i];
            return null;
        }

        private TabPage BelgeSekmesi(CizimBelgesi belge) => _belgeSekmeleri.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Tag == belge);

        private void SekmeMetniniGuncelle(CizimBelgesi belge)
        {
            var sekme = BelgeSekmesi(belge);
            if (sekme == null)
                return;
            sekme.Text = belge.Ad + (belge.Degisti ? " *" : "");
            sekme.ToolTipText = belge.DosyaYolu ?? belge.Ad + " (kaydedilmedi)";
            if (belge == _aktifBelge)
                Text = "Çizim Ekranı - " + belge.Ad;
        }

        /// <summary>Boş bir belge (Yeni1, Yeni2...) açar ve ona geçer.</summary>
        private CizimBelgesi YeniBelge(string ad = null, string dosyaYolu = null)
        {
            var belge = new CizimBelgesi { Ad = ad ?? "Yeni" + (++_yeniSayaci), DosyaYolu = dosyaYolu, Durum = new CizimPaneli.BelgeDurumu() };
            _belgeler.Add(belge);
            _belgeDegistiriliyor = true;
            try
            {
                _belgeSekmeleri.TabPages.Add(new TabPage { Tag = belge });
            }
            finally
            {
                _belgeDegistiriliyor = false;
            }
            SekmeMetniniGuncelle(belge);
            BelgeyeGec(belge);
            return belge;
        }

        // Aktif belgenin panel ve ekrandaki durumunu belgeye yazar.
        private void BelgeDurumunuSakla()
        {
            if (_aktifBelge == null)
                return;
            _aktifBelge.Durum = cizimPaneli1.BelgeDurumunuAl();
            _aktifBelge.Sistem = _aktifSistem;
            _aktifBelge.Dilim = _aktifDilim;
        }

        /// <summary>Belgeye geçer: yarım kalan komut kapatılır, mevcut belge saklanır, geçilen belge panele yüklenir.</summary>
        private void BelgeyeGec(CizimBelgesi belge)
        {
            if (belge == _aktifBelge)
                return;
            KomutuBitir();
            _belgeDegistiriliyor = true;
            try
            {
                BelgeDurumunuSakla();
                _aktifBelge = belge;
                cizimPaneli1.BelgeDurumunuYukle(belge.Durum);
                _aktifSistem = belge.Sistem;
                _aktifDilim = belge.Dilim;
                koordinatSistemiLabel.Text = ProjeksiyonBilgisi.TamAdGetir(_aktifSistem, _aktifDilim);
                var sekme = BelgeSekmesi(belge);
                if (sekme != null && _belgeSekmeleri.SelectedTab != sekme)
                    _belgeSekmeleri.SelectedTab = sekme;
            }
            finally
            {
                _belgeDegistiriliyor = false;
            }
            ozellikGrid.SelectedObject = null;
            SekmeMetniniGuncelle(belge);
            TumPencereleriYenile();
            cizimPaneli1.Focus();
        }

        /// <summary>
        /// Belgeyi kapatır; kaydedilmemiş değişiklik varsa kaydedilsin mi diye sorar (İptal'de false döner). Son belge
        /// kapanınca boş bir Yeni belge açılır.
        /// </summary>
        private bool BelgeyiKapat(CizimBelgesi belge)
        {
            if (belge != _aktifBelge)
                BelgeyeGec(belge);
            if (belge.Degisti && cizimPaneli1.Nesneler.Count > 0)
            {
                var cevap = MessageBox.Show(this, belge.Ad + " çiziminde kaydedilmemiş değişiklikler var. Kaydedilsin mi?", "Kapat",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (cevap == DialogResult.Cancel || (cevap == DialogResult.Yes && !AktifBelgeyiKaydet()))
                    return false;
            }

            var sira = _belgeler.IndexOf(belge);
            _belgeler.Remove(belge);
            _belgeDegistiriliyor = true;
            try
            {
                var sekme = BelgeSekmesi(belge);
                if (sekme != null)
                    _belgeSekmeleri.TabPages.Remove(sekme);
            }
            finally
            {
                _belgeDegistiriliyor = false;
            }
            // Kapanan belge saklanmasın diye aktif belge bırakılır, sonra komşu belgeye geçilir.
            _aktifBelge = null;
            if (_belgeler.Count == 0)
                YeniBelge();
            else
                BelgeyeGec(_belgeler[Math.Min(sira, _belgeler.Count - 1)]);
            return true;
        }

        /// <summary>
        /// DXF/DWG dosyası seçtirip okur. yeniSekme true ise dosya adında yeni bir belgede açılır (Aç); false ise
        /// objeleri aktif belgeye eklenir (Ekle, geri alınabilir).
        /// </summary>
        private void DosyaAc(bool yeniSekme)
        {
            using (var dlg = new OpenFileDialog
            {
                Title = yeniSekme ? "Çizim Dosyası Aç" : "Aktif Çizime Dosya Ekle",
                Filter = "Çizim dosyaları (*.dxf;*.dwg;*.ncz)|*.dxf;*.dwg;*.ncz|DXF (*.dxf)|*.dxf|AutoCAD DWG (*.dwg)|*.dwg|NetCAD (*.ncz)|*.ncz"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                List<CizimNesnesi> nesneler;
                string bilgi = null;
                if (string.Equals(Path.GetExtension(dlg.FileName), ".ncz", StringComparison.OrdinalIgnoreCase))
                {
                    // NCZ tabakaları önce geçici tabakalara okunur; belge hazır olunca çizimin tabakalarına bağlanır.
                    NczOkuyucu.Sonuc ncz;
                    var oncekiImlec = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        ncz = NczOkuyucu.Oku(dlg.FileName, (ad, renk) => new Tabaka { Ad = ad, Renk = renk });
                    }
                    catch (Exception ex)
                    {
                        Cursor.Current = oncekiImlec;
                        MessageBox.Show(this, "NetCAD dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    Cursor.Current = oncekiImlec;
                    if (yeniSekme)
                        YeniBelge(Path.GetFileName(dlg.FileName), dlg.FileName);
                    var esler = new Dictionary<Tabaka, Tabaka>();
                    foreach (var nesne in ncz.Nesneler)
                    {
                        var gecici = nesne.Tabaka;
                        if (gecici == null)
                            continue;
                        if (!esler.TryGetValue(gecici, out var tabaka))
                            esler[gecici] = tabaka = cizimPaneli1.TabakaBulVeyaEkle(gecici.Ad, gecici.Renk, true, false);
                        nesne.Tabaka = tabaka;
                    }
                    nesneler = ncz.Nesneler;
                    bilgi = "NetCAD " + ncz.Surum + " dosyası açıldı: " + nesneler.Count + " obje" +
                            (ncz.AtlananToplam > 0
                                ? "; desteklenmeyen " + ncz.AtlananToplam + " obje atlandı (tip " + string.Join(", ", ncz.Atlanan.Select(a => "0x" + a.Key.ToString("X2") + "×" + a.Value)) + ")."
                                : ".");
                }
                else
                {
                    var dxfBelge = CizimDosyasiOku(dlg.FileName);
                    if (dxfBelge == null)
                        return;

                    if (yeniSekme)
                        YeniBelge(Path.GetFileName(dlg.FileName), dlg.FileName);

                    // Objeler DXF'teki tabakalarına yerleştirilir; çizimde aynı adda tabaka varsa o kullanılır. Çoklu
                    // doğrular ve bloklar tek obje olarak kalır (Düzenle > Patlat ile ayrılır).
                    nesneler = DxfIceAktarici.Ayikla(dxfBelge, katman => cizimPaneli1.TabakaBulVeyaEkle(
                        katman.Name, DxfIceAktarici.RenkCevir(katman.Color), katman.IsVisible && !katman.IsFrozen, katman.IsLocked), true,
                        cizimPaneli1.OlcuStiliBulVeyaEkle);
                }
                cizimPaneli1.Nesneler.AddRange(nesneler);
                if (bilgi != null)
                    KomutIpucu(bilgi);

                if (yeniSekme)
                {
                    // Açılan dosya belgenin başlangıç hali: geri alınacak bir şey yok, değişmemiş sayılır.
                    cizimPaneli1.TumObjeleriGoster();
                    cizimPaneli1.GecmisiTemizle();
                    _aktifBelge.Degisti = false;
                    SekmeMetniniGuncelle(_aktifBelge);
                }
                else
                {
                    if (cizimPaneli1.Nesneler.Count == nesneler.Count)
                        cizimPaneli1.TumObjeleriGoster();
                    cizimPaneli1.DegisiklikKaydet();
                }
                cizimPaneli1.Invalidate();
            }
        }

        /// <summary>Aktif belgeyi DXF olarak kaydeder; belgenin adı ve yolu kaydedilen dosya olur. İptal ya da hatada false.</summary>
        private bool AktifBelgeyiKaydet()
        {
            var varsayilan = _aktifBelge?.DosyaYolu != null && string.Equals(Path.GetExtension(_aktifBelge.DosyaYolu), ".dxf", StringComparison.OrdinalIgnoreCase)
                ? _aktifBelge.DosyaYolu
                : Path.ChangeExtension(_aktifBelge?.DosyaYolu ?? (_aktifBelge?.Ad ?? "cizim"), ".dxf");
            using (var dlg = new SaveFileDialog { Title = "DXF Olarak Kaydet", Filter = "DXF (*.dxf)|*.dxf", FileName = Path.GetFileName(varsayilan) })
            {
                var klasor = Path.GetDirectoryName(varsayilan);
                if (!string.IsNullOrEmpty(klasor) && Directory.Exists(klasor))
                    dlg.InitialDirectory = klasor;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;
                try
                {
                    DxfDisaAktarici.Kaydet(cizimPaneli1.Nesneler, dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                if (_aktifBelge != null)
                {
                    _aktifBelge.DosyaYolu = dlg.FileName;
                    _aktifBelge.Ad = Path.GetFileName(dlg.FileName);
                    _aktifBelge.Degisti = false;
                    SekmeMetniniGuncelle(_aktifBelge);
                }
                return true;
            }
        }

        /// <summary>
        /// Pano kopyaları başka bir belgeden gelmiş olabilir: kopyaların tabakaları aktif belgede aynı adlı tabakaya
        /// bağlanır (yoksa aynı ayarlarla oluşturulur). Blok içindeki objelerin tabakaları da eşlenir.
        /// </summary>
        private void TabakalariAktifBelgeyeBagla(IEnumerable<CizimNesnesi> nesneler)
        {
            foreach (var nesne in nesneler)
            {
                var tabaka = nesne.Tabaka;
                if (tabaka != null && !cizimPaneli1.Tabakalar.Contains(tabaka))
                {
                    var yeni = cizimPaneli1.TabakaBulVeyaEkle(tabaka.Ad, tabaka.Renk, tabaka.Gorunur, tabaka.Kilitli);
                    if (yeni.CizgiKalinligi <= 0)
                        yeni.CizgiKalinligi = tabaka.CizgiKalinligi;
                    nesne.Tabaka = yeni;
                }
                if (nesne is BlokNesnesi blok)
                    TabakalariAktifBelgeyeBagla(blok.Ogeler);
                // Ölçünün stili de aktif belgede aynı adlı stile bağlanır (yoksa kopyası eklenir).
                if (nesne is IOlcuStilli stilli && stilli.Stil != null && !cizimPaneli1.OlcuStilleri.Contains(stilli.Stil))
                    stilli.Stil = cizimPaneli1.OlcuStiliBulVeyaEkle(stilli.Stil);
            }
        }
    }
}
