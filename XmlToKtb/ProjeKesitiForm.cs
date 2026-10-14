using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Common;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Güzergah dosyası ve tip kesit seçilerek, kesit editöründeki aktif (referans) dosyanın kilometrelerinde proje
    /// kesitleri üretilen pencere. Her kilometrede tip kesit, güzergahın o kilometredeki kırmızı kotu kadar ötelenir.
    /// </summary>
    /// <summary>Proje Kesiti penceresindeki seçimler; pencere kapanıp açıldığında korunmaları için editörde tutulur.</summary>
    public class ProjeKesitiAyarlari
    {
        public string GuzergahYolu;
        public List<CizimNesnesi> TipKesitNesneleri;
        public bool DeverUygula;
        public DeverTablosu DeverTablosu = new DeverTablosu();
    }

    [System.ComponentModel.DesignerCategory("Code")]
    public class ProjeKesitiForm : Form
    {
        private readonly KseDosyasi _referansDosya;
        private readonly ProjeKesitiAyarlari _ayarlar;
        private LandXML _guzergah;

        private readonly Label _guzergahLabel = new Label { Location = new Point(170, 23), Size = new Size(290, 17), AutoEllipsis = true };
        private readonly Label _tipKesitLabel = new Label { Location = new Point(170, 63), Size = new Size(290, 17), AutoEllipsis = true };
        private readonly Button _olusturButton = new Button { Text = "Proje Enkesiti Oluştur", Location = new Point(12, 104), Size = new Size(150, 30) };

        /// <summary>Seçilen güzergah dosyasının yolu (seçilmediyse null).</summary>
        public string GuzergahYolu
        {
            get => _ayarlar.GuzergahYolu;
            private set => _ayarlar.GuzergahYolu = value;
        }

        /// <summary>Tip kesit çizim ekranındaki objeler (seçilmediyse null).</summary>
        public List<CizimNesnesi> TipKesitNesneleri
        {
            get => _ayarlar.TipKesitNesneleri;
            private set => _ayarlar.TipKesitNesneleri = value;
        }

        /// <summary>Tamam ile kapatıldığında üretilen proje kesitleri.</summary>
        public KseDosyasi Sonuc { get; private set; }

        public ProjeKesitiForm(KseDosyasi referansDosya, ProjeKesitiAyarlari ayarlar)
        {
            _referansDosya = referansDosya;
            _ayarlar = ayarlar;

            Text = "Proje Kesiti Oluştur";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(472, 148);

            var guzergahButton = new Button { Text = "Güzergah Seçimi", Location = new Point(12, 16), Size = new Size(150, 30) };
            var tipKesitButton = new Button { Text = "Tip Kesit Seçimi", Location = new Point(12, 56), Size = new Size(150, 30) };
            var kapatButton = new Button { Text = "Kapat", Location = new Point(385, 104), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

            guzergahButton.Click += GuzergahButton_Click;
            tipKesitButton.Click += TipKesitButton_Click;
            _olusturButton.Click += OlusturButton_Click;

            Controls.AddRange(new Control[] { guzergahButton, _guzergahLabel, tipKesitButton, _tipKesitLabel, _olusturButton, kapatButton });
            CancelButton = kapatButton;

            // Pencere daha önce kullanıldıysa önceki seçimler geri yüklenir.
            if (GuzergahYolu != null)
            {
                try
                {
                    _guzergah = GuzergahOku(GuzergahYolu, out _);
                }
                catch
                {
                    // Dosya artık okunamıyorsa seçim boş başlar.
                    GuzergahYolu = null;
                }
            }

            GorunumuGuncelle();
        }

        private void GorunumuGuncelle()
        {
            _guzergahLabel.Text = GuzergahYolu != null ? Path.GetFileName(GuzergahYolu) : "(seçilmedi)";

            var tipKesitHazir = TipKesitNesneleri != null && TipKesitEkrani.TipKesitNoktalariniGetir(TipKesitNesneleri, out var noktalar, out _);
            _tipKesitLabel.Text = tipKesitHazir
                ? TipKesitNesneleri.OfType<CizgiNesnesi>().Count() + " çizgili tip kesit" + (_ayarlar.DeverUygula ? " (dever uygulanacak)" : "")
                : "(seçilmedi)";

            _olusturButton.Enabled = _guzergah != null && tipKesitHazir;
        }

        /// <summary>
        /// Güzergah dosyasını okur. XML bozuksa ve bozukluk yarıda kesilmiş etiket satırlarından kaynaklanıyorsa
        /// (bazı programlar satırları 255 karakterde keser) dosyayı onararak okur; onarılan satır sayısını
        /// <paramref name="onarilanSatirSayisi"/> ile bildirir.
        /// </summary>
        internal static LandXML GuzergahOku(string dosyaYolu, out int onarilanSatirSayisi)
        {
            onarilanSatirSayisi = 0;
            try
            {
                return GuzergahOku(dosyaYolu);
            }
            catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
            {
                var onarilmis = KesikEtiketleriOnar(File.ReadAllLines(dosyaYolu), out onarilanSatirSayisi);
                if (onarilanSatirSayisi == 0)
                    throw;

                using (var akis = new MemoryStream(Encoding.UTF8.GetBytes(onarilmis)))
                    return new DosyaDeserializer().XmlDeserializer<LandXML>(akis);
            }
        }

        /// <summary>
        /// Açılış etiketi satır sonunda kapanmadan kesilmiş satırları onarır: yarım kalan son özniteliği atar ve
        /// etiketi kapatır. Kesilen özniteliklerin değeri geri getirilemez; okunan modelde boş kalırlar.
        /// </summary>
        private static string KesikEtiketleriOnar(string[] satirlar, out int onarilanSatirSayisi)
        {
            onarilanSatirSayisi = 0;
            var sb = new StringBuilder();
            foreach (var hamSatir in satirlar)
            {
                var satir = hamSatir.TrimEnd();
                var icerik = satir.TrimStart();

                // XML bildirimi metni UTF-8'e çevrildiği için atılır (içindeki encoding artık geçerli olmayabilir).
                if (icerik.StartsWith("<?xml"))
                    continue;

                var kesikAcilisEtiketi = icerik.StartsWith("<") && !icerik.StartsWith("</") && !icerik.StartsWith("<!")
                                         && !icerik.StartsWith("<?") && !icerik.Contains(">");
                if (kesikAcilisEtiketi)
                {
                    var tirnakSayisi = satir.Count(c => c == '"');
                    satir = tirnakSayisi % 2 == 1
                        ? Regex.Replace(satir, "\\s+[^\\s=\"]+=\"[^\"]*$", "")   // değeri yarım kalmış öznitelik
                        : Regex.Replace(satir, "\\s+[^\\s=\"<]+=?$", "");        // adı yarım kalmış öznitelik
                    satir += ">";
                    onarilanSatirSayisi++;
                }

                sb.AppendLine(satir);
            }
            return sb.ToString();
        }

        private static LandXML GuzergahOku(string dosyaYolu)
        {
            var uzanti = Path.GetExtension(dosyaYolu);
            if (uzanti != null && uzanti.ToLower().Equals(".ktb"))
            {
                using (var ktbStreamReader = new StreamReader(dosyaYolu))
                {
                    var xmlData = KTBtoXML.GetirXmlData(ktbStreamReader);
                    return KTBtoXML.LandXmlOlustur(Path.GetFileNameWithoutExtension(dosyaYolu), xmlData);
                }
            }

            return new DosyaDeserializer().XmlDeserializer<LandXML>(dosyaYolu);
        }

        private void GuzergahButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Güzergah Dosyasını Seçin", Filter = "Güzergah (*.xml;*.ktb)|*.xml;*.ktb|XML (*.xml)|*.xml|KTB (*.ktb)|*.ktb" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                LandXML guzergah;
                int onarilanSatirSayisi;
                try
                {
                    guzergah = GuzergahOku(dlg.FileName, out onarilanSatirSayisi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Güzergah dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (guzergah?.Alignments?.Alignment == null)
                {
                    MessageBox.Show("Dosyada bir güzergah ekseni (Alignment) bulunamadı.", "Güzergah Seçimi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (guzergah.Alignments.Alignment.Profile?.ProfAlign?.Items == null)
                {
                    MessageBox.Show("Güzergah dosyasında düşey geometri (profil) yok; kilometredeki kot hesaplanamaz.", "Güzergah Seçimi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (onarilanSatirSayisi > 0)
                {
                    MessageBox.Show(
                        "Dosya bozuk: " + onarilanSatirSayisi + " satırda etiket yarıda kesilmiş (satırlar 255 karakterde kırpılmış olabilir).\n\n" +
                        "Kesilen kısımlar yok sayılarak dosya okundu. Kot hesabında kullanılan profil bilgisi okunabildi, " +
                        "ancak yatay geometrideki bazı değerler eksik olabilir.",
                        "Güzergah Seçimi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                _guzergah = guzergah;
                GuzergahYolu = dlg.FileName;
                // Yeni güzergahla birlikte dever tablosu da o güzergahtan yeniden okunur.
                _ayarlar.DeverTablosu = DeverTablosu.GuzergahtanOku(guzergah.Alignments.Alignment);
                GorunumuGuncelle();
            }
        }

        private void TipKesitButton_Click(object sender, EventArgs e)
        {
            using (var ekran = new TipKesitEkrani(TipKesitNesneleri, _ayarlar.DeverUygula, _ayarlar.DeverTablosu, GuzergahYolu))
            {
                var sonuc = ekran.ShowDialog(this);

                // Dever güzergah dosyasına yazıldıysa, ekrandan İptal ile çıkılsa bile artık geçerli olan odur.
                if (ekran.DosyayaKaydedilenDeverTablosu != null)
                    _ayarlar.DeverTablosu = ekran.DosyayaKaydedilenDeverTablosu;

                if (sonuc != DialogResult.OK)
                    return;

                TipKesitNesneleri = ekran.Nesneler;
                _ayarlar.DeverUygula = ekran.DeverUygula;
                _ayarlar.DeverTablosu = ekran.DeverTablosu;
                GorunumuGuncelle();
            }
        }

        private void OlusturButton_Click(object sender, EventArgs e)
        {
            if (!TipKesitEkrani.TipKesitNoktalariniGetir(TipKesitNesneleri, out var tipKesitNoktalari, out var hata))
            {
                MessageBox.Show(hata, "Proje Kesiti", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var alignment = _guzergah.Alignments.Alignment;
            var sonuc = new KseDosyasi();
            var kotHesaplanamayan = 0;

            foreach (var referansKesit in _referansDosya.Kesitler)
            {
                if (double.IsNaN(referansKesit.Km))
                {
                    kotHesaplanamayan++;
                    continue;
                }

                double kirmiziKot;
                try
                {
                    kirmiziKot = Convert.ToDouble(Prfhesap.Duzenleyici(alignment, Convert.ToDecimal(referansKesit.Km)));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Km " + referansKesit.Kilometre + " için kot hesaplanamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Prfhesap, kilometre profilin dışındaysa 0 döner.
                if (kirmiziKot == 0)
                {
                    kotHesaplanamayan++;
                    continue;
                }

                var kesit = new KseKesit
                {
                    No = (sonuc.Kesitler.Count + 1).ToString(),
                    Kilometre = referansKesit.Kilometre,
                    Km = referansKesit.Km,
                    Aciklama = ""
                };
                kesit.Baslik = kesit.No + "  " + kesit.Kilometre;

                double dever = 0;
                var deverYonu = "cw";
                if (_ayarlar.DeverUygula)
                    _ayarlar.DeverTablosu.DeverGetir(referansKesit.Km, out dever, out deverYonu);

                for (var i = 0; i < tipKesitNoktalari.Count; i++)
                {
                    var nokta = _ayarlar.DeverTablosu.Uygula(tipKesitNoktalari[i], dever, deverYonu);
                    kesit.Noktalar.Add(new KseNokta
                    {
                        Mesafe = Math.Round(nokta.X, 3),
                        Kot = Math.Round(kirmiziKot + nokta.Y, 3),
                        Kod = i.ToString()
                    });
                }

                kesit.CizgileriOlustur();
                sonuc.Kesitler.Add(kesit);
            }

            if (sonuc.Kesitler.Count == 0)
            {
                MessageBox.Show(
                    "Hiçbir kilometrede kot hesaplanamadı. Güzergah dosyasının profili, kesit kilometrelerini kapsamıyor olabilir.",
                    "Proje Kesiti", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (kotHesaplanamayan > 0)
            {
                MessageBox.Show(
                    sonuc.Kesitler.Count + " kilometrede proje kesiti oluşturuldu.\n" +
                    kotHesaplanamayan + " kilometrede güzergah kotu hesaplanamadığı için kesit oluşturulmadı.",
                    "Proje Kesiti", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Sonuc = sonuc;
            DialogResult = DialogResult.OK;
        }
    }
}
