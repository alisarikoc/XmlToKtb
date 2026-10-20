using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Çizim ekranının sağ tık menüsü. Ekranda çalışan komut ya da bekleyen seçim yokken sağ tıklanınca açılır (komut
    /// seçim/nokta beklerken sağ tık seçimi onaylamaya ya da nokta almayı bitirmeye devam eder):
    /// seçili güzergahın komutları, son 10 komut, pano komutları, hızlı seçim, bul/değiştir, geri al/ileri al ve seçim.
    /// Son komutu tekrarlamak Boşluk (ya da Enter) tuşuyladır.
    /// </summary>
    public partial class CizimEkrani
    {
        private readonly ContextMenuStrip _sagTikMenusu = new ContextMenuStrip { ShowImageMargin = true };
        private bool _sagTikUygun;

        private void SagTikMenusunuKur()
        {
            // Tıklandığı andaki durum, panel kendi işini (seçim onayı vb.) yapmadan önce alınır.
            cizimPaneli1.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    _sagTikUygun = EkranBosBekliyor();
            };
            cizimPaneli1.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || !_sagTikUygun)
                    return;
                _sagTikUygun = false;
                SagTikMenusunuDoldur();
                _sagTikMenusu.Show(cizimPaneli1, e.Location);
            };
        }

        private void SagTikMenusunuDoldur()
        {
            var menu = _sagTikMenusu;
            menu.Items.Clear();
            var seciliSayisi = cizimPaneli1.Nesneler.Count(n => n.Secili);

            ToolStripMenuItem Oge(string metin, char simge, string kisayol, Action eylem, bool etkin = true)
            {
                var oge = new ToolStripMenuItem(metin, Simge(simge), (s, e) => eylem()) { Enabled = etkin };
                if (kisayol != null)
                    oge.ShortcutKeyDisplayString = kisayol;
                return oge;
            }

            // Seçili güzergah varsa onun komutları en üstte.
            if (SeciliGuzergah() != null)
            {
                menu.Items.Add(new ToolStripMenuItem("Güzergah", Simge('')) { DropDown = _guzergahMenusu });
                menu.Items.Add(new ToolStripSeparator());
            }

            // Son Komutlar: son kullanılan 10 komut; tıklanınca çalışır. İlki Boşluk ile de tekrarlanır.
            var son = new ToolStripMenuItem("Son Komutlar", Simge('')) { Enabled = _sonKomutlar.Count > 0 };
            for (var i = 0; i < _sonKomutlar.Count; i++)
            {
                var komut = _sonKomutlar[i];
                var oge = new ToolStripMenuItem(komut.Key, i == 0 ? Simge('') : null, (s, e) => komut.Value());
                if (i == 0)
                {
                    oge.ShortcutKeyDisplayString = "Boşluk";
                    oge.Font = new Font(oge.Font, FontStyle.Bold);
                }
                son.DropDownItems.Add(oge);
            }
            menu.Items.Add(son);

            // Pano.
            var pano = new ToolStripMenuItem("Pano", Simge(''));
            pano.DropDownItems.Add(Oge("Kes", '', "Ctrl+X", () => KomutBaslat(DuzenKomutu.PanoyaKes)));
            pano.DropDownItems.Add(Oge("Kopyala", '', "Ctrl+C", () => KomutBaslat(DuzenKomutu.PanoyaKopyala)));
            pano.DropDownItems.Add(Oge("Baz Noktasıyla Kopyala", '', "Ctrl+Shift+C", () => KomutBaslat(DuzenKomutu.ReferansKopyala)));
            pano.DropDownItems.Add(new ToolStripSeparator());
            pano.DropDownItems.Add(Oge("Yapıştır", '', "Ctrl+V", () => KomutBaslat(DuzenKomutu.Yapistir), _pano.Count > 0));
            pano.DropDownItems.Add(Oge("Orijinal Yerine Yapıştır", '', "Ctrl+Shift+V", KoordinatliYapistir, _pano.Count > 0));
            menu.Items.Add(pano);
            menu.Items.Add(new ToolStripSeparator());

            var ozellikler = Oge("Özellikler", '', null, OzellikleriAcKapat);
            ozellikler.Checked = OzelliklerAcik;
            menu.Items.Add(ozellikler);

            menu.Items.Add(Oge("Hızlı Seçim...", '', null, HizliSecim));
            menu.Items.Add(Oge("Bul / Değiştir...", '', "Ctrl+F", BulDegistir));
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(Oge("Geri Al", '', "Ctrl+Z", GeriAl, cizimPaneli1.GeriAlinabilir));
            menu.Items.Add(Oge("İleri Al", '', "Ctrl+Y", IleriAl, cizimPaneli1.Ilerlenebilir));
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(Oge("Tümünü Seç", '', "Ctrl+A", () => SecimiUygula(n => true, SecimBirlesimi.Yeni)));
            if (seciliSayisi > 0)
            {
                menu.Items.Add(Oge("Seçimi Kaldır (" + seciliSayisi + ")", '', "Esc", () => SecimiUygula(n => false, SecimBirlesimi.Yeni)));
                menu.Items.Add(Oge("Seçilenleri Sil", '', "Del", () =>
                {
                    cizimPaneli1.SeciliNesneleriSil();
                    cizimPaneli1.DegisiklikKaydet();
                }));
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Oge("Tümünü Göster", '', null, () =>
            {
                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }));
        }

        private static Bitmap Simge(char kod) => PanelKomutMenusu.Simge(kod);
    }
}
