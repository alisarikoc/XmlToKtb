using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Arayuz;

namespace XmlToKtb
{
    /// <summary>
    /// Çizim ekranının ribbon menüsü. Klasik menünün yerini alır (Görünüm > Klasik Menüyü Göster ile geri açılır); bütün
    /// düğmeler klasik menüdeki komutları çalıştırır, böylece kısayollar ve menü davranışı aynı kalır.
    /// <list type="bullet">
    /// <item>Solda Dosya düğmesi (Dosya menüsü) ve hızlı erişim: Yeni, Aç, Kaydet, Geri Al, İleri Al.</item>
    /// <item>Giriş sekmesi: en sık kullanılan komutlar (Çizim, Değiştir, Ölçülendirme, Pano, Seçim, Tabakalar).</item>
    /// <item>Diğer sekmeler menülerden kurulur: her menü bir sekme, ayraçlar grup sınırı, alt menüler açılır düğme.
    /// Alt öğesi olmayan menüler (ör. Nokta Bulutu) Araçlar sekmesinde toplanır.</item>
    /// </list>
    /// </summary>
    public partial class CizimEkrani
    {
        private RibbonCubugu _ribbon;
        private ToolStripMenuItem _klasikMenuOgesi;

        // Menü gruplarının ribbon'daki adları (grubun ilk öğesine göre); listede olmayan grup sekmenin adını alır.
        private static readonly Dictionary<string, string> GrupAdlari = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Çizgi Çiz", "Çizim" }, { "Nokta At", "Nokta ve Yazı" }, { "Geri Al", "Geri Al" }, { "Panoya Kopyala", "Pano" },
            { "Kaydır", "Değiştir" }, { "Uzat", "Değiştir 2" }, { "Toplu Obje Değiştir", "Toplu İşlem" }, { "Nokta Editörü", "Nokta" },
            { "Tümünü Seç", "Seçim" }, { "Çit ile Seç", "Gelişmiş Seçim" }, { "Tek Pencere", "Pencereler" }, { "Arka Plan Rengi Seç", "Arka Plan" },
            { "Xml Dosyasından Güzergah Çiz", "Güzergah" }, { "Enkesit Oluştur", "Enkesit" }, { "Poligon Hesabı", "Hesap" },
            { "Yüzeyin Noktalarını Farklı Kaydet", "Yüzey" }, { "Giriş Ekranı", "Ekran" }, { "Görünüm Hassasiyeti", "Ayarlar" }, { "Klasik Menüyü Göster", "Menü" }
        };

        private void RibbonKur()
        {
            // Klasik menü Görünüm menüsünden geri açılabilir (ribbon'da da Görünüm sekmesinde görünür).
            var gorunum = UstMenu("Görünüm");
            if (gorunum != null)
            {
                _klasikMenuOgesi = new ToolStripMenuItem("Klasik Menüyü Göster") { CheckOnClick = true };
                _klasikMenuOgesi.CheckedChanged += (s, e) => anaMenuStrip.Visible = _klasikMenuOgesi.Checked;
                gorunum.DropDownItems.Add(new ToolStripSeparator());
                gorunum.DropDownItems.Add(_klasikMenuOgesi);
            }

            _ribbon = new RibbonCubugu();
            _ribbon.KomutCalisti += () => BeginInvoke((Action)cizimPaneli1.DegisiklikKaydet);

            var dosya = UstMenu("Dosya");
            if (dosya != null)
                _ribbon.UygulamaDugmesiEkle("Dosya", dosya);
            _ribbon.HizliErisimEkle("Yeni", Bul("Dosya", "Yeni"));
            _ribbon.HizliErisimEkle("Aç", Bul("Dosya", "Aç"));
            _ribbon.HizliErisimEkle("Kaydet", Bul("Dosya", "Kaydet"));
            _ribbon.HizliErisimEkle("Geri Al", Bul("Düzenle", "Geri Al"));
            _ribbon.HizliErisimEkle("İleri Al", Bul("Düzenle", "İleri Al"));

            GirisSekmesiniKur();

            var araclar = new List<ToolStripMenuItem>();
            foreach (var ust in anaMenuStrip.Items.OfType<ToolStripMenuItem>())
            {
                if (ust == dosya)
                    continue;
                if (ust.DropDownItems.Count == 0)
                {
                    araclar.Add(ust);
                    continue;
                }
                MenudenSekmeKur(GrupAdi(ust.Text), ust);
            }
            if (araclar.Count > 0)
            {
                var sekme = _ribbon.SekmeEkle("Araçlar");
                var grup = sekme.GrupEkle("Araçlar");
                foreach (var oge in araclar)
                    grup.DugmeEkle(oge, DugmeBoyu.Buyuk);
            }

            // Ribbon klasik menünün yerine (en üste) yerleşir; klasik menü gizlenir, kısayolları çalışmaya devam eder.
            Controls.Add(_ribbon);
            Controls.SetChildIndex(_ribbon, Controls.GetChildIndex(anaMenuStrip));
            anaMenuStrip.Visible = false;
            // Eski araç çubuğunun bütün düğmeleri gizliyse (yakalama modları OSEC menüsünde) boş şerit yer kaplamasın.
            if (!toolStrip1.Items.Cast<ToolStripItem>().Any(i => i.Available && !string.IsNullOrEmpty(i.Text)))
                toolStrip1.Visible = false;
        }

        private static string GrupAdi(string metin) => RibbonGrubu.KisaMetin(metin);

        /// <summary>
        /// Klasik menü gizliyken Windows menü kısayollarını (Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+F4...) işlemez; kısayollar
        /// burada menü öğelerine yönlendirilir.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_ribbon != null && !anaMenuStrip.Visible)
            {
                var oge = KisayolOgesi(anaMenuStrip.Items, keyData);
                if (oge != null && oge.Enabled)
                {
                    oge.PerformClick();
                    BeginInvoke((Action)cizimPaneli1.DegisiklikKaydet);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static ToolStripMenuItem KisayolOgesi(ToolStripItemCollection ogeler, Keys tus)
        {
            foreach (var m in ogeler.OfType<ToolStripMenuItem>())
            {
                if (m.ShortcutKeys == tus && m.Available)
                    return m;
                var alt = KisayolOgesi(m.DropDownItems, tus);
                if (alt != null)
                    return alt;
            }
            return null;
        }

        private ToolStripMenuItem UstMenu(string ad) =>
            anaMenuStrip.Items.OfType<ToolStripMenuItem>().FirstOrDefault(m => string.Equals(RibbonGrubu.KisaMetin(m.Text), ad, StringComparison.OrdinalIgnoreCase));

        /// <summary>Menü yolundaki öğe (her adımda kısa adı eşit ya da onunla başlayan ilk öğe); bulunamazsa null.</summary>
        private ToolStripMenuItem Bul(params string[] yol)
        {
            var menu = UstMenu(yol[0]);
            foreach (var ad in yol.Skip(1))
            {
                if (menu == null)
                    return null;
                var ogeler = menu.DropDownItems.OfType<ToolStripMenuItem>().ToList();
                menu = ogeler.FirstOrDefault(m => string.Equals(RibbonGrubu.KisaMetin(m.Text), ad, StringComparison.OrdinalIgnoreCase))
                       ?? ogeler.FirstOrDefault(m => RibbonGrubu.KisaMetin(m.Text).StartsWith(ad, StringComparison.OrdinalIgnoreCase));
            }
            return menu;
        }

        private void GirisSekmesiniKur()
        {
            var sekme = _ribbon.SekmeEkle("Giriş");
            foreach (var ad in new[] { "Çizim", "Düzenle", "Ayarlar" })
            {
                var m = UstMenu(ad);
                if (m != null)
                    sekme.GuncellenecekMenuler.Add(m);
            }

            var cizim = sekme.GrupEkle("Çizim");
            cizim.DugmeEkle(Bul("Çizim", "Çizgi Çiz"), DugmeBoyu.Buyuk, "Çizgi");
            cizim.DugmeEkle(Bul("Çizim", "Çoklu Doğru"), DugmeBoyu.Buyuk, "Çoklu Doğru");
            cizim.DugmeEkle(Bul("Çizim", "Daire Çiz"), DugmeBoyu.Buyuk, "Daire");
            cizim.DugmeEkle(Bul("Çizim", "Yay Çiz"), DugmeBoyu.Kucuk, "Yay");
            cizim.DugmeEkle(Bul("Çizim", "Nokta At"), DugmeBoyu.Kucuk);
            cizim.DugmeEkle(Bul("Çizim", "Yazı Yaz"), DugmeBoyu.Kucuk);

            var degistir = sekme.GrupEkle("Değiştir");
            foreach (var ad in new[] { "Kaydır", "Döndür", "Kırp", "Kopyala", "Ölçekle", "Uzat", "Aynala", "Paralel", "Patlat", "Sil" })
                degistir.DugmeEkle(Bul("Düzenle", ad), DugmeBoyu.Kucuk, ad);

            var olcu = sekme.GrupEkle("Ölçülendirme");
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Doğrusal"), DugmeBoyu.Buyuk, "Doğrusal");
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Hizalı"), DugmeBoyu.Kucuk);
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Açısal"), DugmeBoyu.Kucuk);
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Yarıçap"), DugmeBoyu.Kucuk);
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Çap"), DugmeBoyu.Kucuk);
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Kılavuz Çizgi"), DugmeBoyu.Buyuk, "Kılavuz");
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Ölçü Stilleri"), DugmeBoyu.Kucuk, "Stiller");
            olcu.DugmeEkle(Bul("Çizim", "Ölçülendirme", "Aktif Ölçü Stili"), DugmeBoyu.Kucuk, "Aktif Stil");

            var pano = sekme.GrupEkle("Pano");
            pano.DugmeEkle(Bul("Düzenle", "Yapıştır"), DugmeBoyu.Buyuk);
            pano.DugmeEkle(Bul("Düzenle", "Panoya Kes"), DugmeBoyu.Kucuk, "Kes");
            pano.DugmeEkle(Bul("Düzenle", "Panoya Kopyala"), DugmeBoyu.Kucuk, "Kopyala");
            pano.DugmeEkle(Bul("Düzenle", "Referans Noktası ile Kopyala"), DugmeBoyu.Kucuk, "Baz Noktasıyla");
            pano.DugmeEkle(Bul("Düzenle", "Koordinatlı Yerine Yapıştır"), DugmeBoyu.Kucuk, "Yerine Yapıştır");

            var secim = sekme.GrupEkle("Seçim");
            secim.DugmeEkle(Bul("Seçim", "Tümünü Seç"), DugmeBoyu.Kucuk);
            secim.DugmeEkle(Bul("Seçim", "Hızlı Seçim"), DugmeBoyu.Kucuk);
            secim.DugmeEkle(Bul("Düzenle", "Bul / Değiştir"), DugmeBoyu.Kucuk);

            var tabaka = sekme.GrupEkle("Tabakalar");
            tabaka.DugmeEkle(Bul("Ayarlar", "Tabaka Ayarları", "Tabaka Ayarları Penceresi"), DugmeBoyu.Buyuk, "Tabaka Ayarları");
            tabaka.DugmeEkle(Bul("Ayarlar", "Tabaka Ayarları", "Tıklanan Objenin Tabakasını Kapat"), DugmeBoyu.Kucuk, "Tabakayı Kapat");
            tabaka.DugmeEkle(Bul("Ayarlar", "Tabaka Ayarları", "Bütün Tabakaları Aç"), DugmeBoyu.Kucuk, "Hepsini Aç");
        }

        /// <summary>
        /// Menüden sekme kurar: ayraçlar arası her öğe kümesi bir gruptur. Tek alt menüden oluşan küme, o alt menünün
        /// öğeleriyle açılmış bir grup olur. En çok 3 öğeli grupta büyük, daha kalabalıkta küçük düğmeler kullanılır.
        /// </summary>
        private void MenudenSekmeKur(string sekmeAdi, ToolStripMenuItem menu)
        {
            var sekme = _ribbon.SekmeEkle(sekmeAdi);
            sekme.GuncellenecekMenuler.Add(menu);
            var kumeler = new List<List<ToolStripMenuItem>> { new List<ToolStripMenuItem>() };
            foreach (ToolStripItem oge in menu.DropDownItems)
            {
                if (oge is ToolStripSeparator)
                {
                    if (kumeler[kumeler.Count - 1].Count > 0)
                        kumeler.Add(new List<ToolStripMenuItem>());
                    continue;
                }
                // Uzun, pasif açıklama satırları düğme olmaz.
                if (oge is ToolStripMenuItem m && (m.Enabled || m.Text.Length < 60))
                    kumeler[kumeler.Count - 1].Add(m);
            }
            foreach (var kume in kumeler.Where(k => k.Count > 0))
            {
                if (kume.Count == 1 && kume[0].DropDownItems.Count > 0 && kume[0].DropDownItems.Count <= 12)
                {
                    // Tek alt menü: grup adı alt menünün adı, öğeleri grubun düğmeleri.
                    var alt = kume[0];
                    sekme.GuncellenecekMenuler.Add(alt);
                    var altGrup = sekme.GrupEkle(GrupAdi(alt.Text));
                    var altOgeler = alt.DropDownItems.OfType<ToolStripMenuItem>().Where(m => m.Enabled || m.Text.Length < 60).ToList();
                    foreach (var oge in altOgeler)
                        altGrup.DugmeEkle(oge, altOgeler.Count <= 3 ? DugmeBoyu.Buyuk : DugmeBoyu.Kucuk);
                    continue;
                }
                var ilk = GrupAdi(kume[0].Text);
                var grup = sekme.GrupEkle(GrupAdlari.TryGetValue(ilk, out var ad) ? ad : sekmeAdi);
                foreach (var oge in kume)
                    grup.DugmeEkle(oge, kume.Count <= 3 ? DugmeBoyu.Buyuk : DugmeBoyu.Kucuk);
            }
        }
    }
}
