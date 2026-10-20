using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XmlToKtb
{
    /// <summary>
    /// Silsile sekmesi: Excel'den yapıştırılan ham silsile ölçüleri (DN, BN, Durum, Tarih, Yatay açı, Düşey açı, Eğik
    /// mesafe, Yatay mesafe) açılara çevrilir, irdelenir ve karneye yazılır.
    /// <list type="bullet">
    /// <item>Her istasyonda iki hedef (geri ve ileri) vardır. Aynı durumda (I ya da II) art arda gelen iki farklı hedefin
    /// okuması bir açı ölçüsü oluşturur: açı = ileri doğrultu − geri doğrultu (II. durumda iki doğrultu da 200 g kaydığı
    /// için fark aynıdır). Eşi olmayan okuma kullanılmaz.</item>
    /// <item>Kesin açı, kullanılan açı ölçülerinin ortalamasıdır; her ölçünün kesin açıdan farkı (cc) yazılır. Farkı büyük
    /// ölçü hesaptan çıkarılabilir (Kullan işareti ya da Delete); ortalama ve farklar yeniden hesaplanır.</item>
    /// <item>Geri/ileri: tabloda sonradan durulan nokta olarak geçen hedef ileri, daha önce durulan nokta olarak geçen
    /// hedef geri sayılır; belirlenemezse ilk bakılan hedef geridir. İstasyon bazında değiştirilebilir.</item>
    /// <item>Karneye Yaz, her istasyon için tek silsile yazar: geri doğrultu 0, ileri doğrultu kesin açı; kenarlar hedef
    /// başına I. ve II. durum yatay mesafelerinin ortalamasıdır (yatay mesafe yoksa eğik mesafe ve düşey açıdan bulunur).</item>
    /// </list>
    /// </summary>
    public partial class PoligonHesabiForm
    {
        // Ham silsile tablosu kolonları
        private const int SDn = 0, SBn = 1, SDurum = 2, STarih = 3, SYatay = 4, SDusey = 5, SEgik = 6, SYatayMesafe = 7;
        // Açı ölçüleri tablosu kolonları
        private const int ADurulan = 0, ASira = 1, ADurum = 2, AGeri = 3, AGeriDog = 4, AIleri = 5, AIleriDog = 6, AAci = 7, AFark = 8, AKullan = 9;

        private readonly DataGridView _silsileHam = Tablo();
        private readonly DataGridView _silsileAcilar = Tablo();
        private readonly DataGridView _silsileOzet = Tablo();
        private readonly Label _silsileDurum = new Label { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6, 4, 0, 0), ForeColor = Color.DarkBlue };
        // Hesaptan çıkarılan açı ölçüleri (durulan + sıra) ve geri/ileri yönü elle değiştirilen istasyonlar.
        private readonly HashSet<string> _silsileHaric = new HashSet<string>();
        private readonly HashSet<string> _silsileTers = new HashSet<string>();
        private List<SilsileIstasyonu> _silsileIstasyonlari = new List<SilsileIstasyonu>();
        private bool _silsileGuncelleniyor;

        private sealed class SilsileOkuma
        {
            public string Dn, Bn;
            public bool Ikinci;
            public double Yatay;
            public double? Mesafe;
        }

        private sealed class SilsileAcisi
        {
            public string Dn;
            public int Sira;
            public bool Ikinci;
            public double GeriDog, IleriDog, Aci;
            public bool Kullan;
            public string Anahtar => Dn + "\u0001" + Sira;
        }

        private sealed class SilsileIstasyonu
        {
            public string Dn, Geri, Ileri, Uyari;
            public readonly List<SilsileAcisi> Acilar = new List<SilsileAcisi>();
            public double? KesinAci, OrtalamaHataCc;
            public double? GeriMesafe1, GeriMesafe2, IleriMesafe1, IleriMesafe2;
            public int KullanilanSayisi => Acilar.Count(a => a.Kullan);
        }

        private TabPage SilsileSayfasiniOlustur()
        {
            foreach (var baslik in new[] { "DN (Durulan)", "BN (Bakılan)", "Durum", "Tarih", "Yatay Açı (g)", "Düşey Açı (g)", "Eğik Mesafe (m)", "Yatay Mesafe (m)" })
                KolonEkle(_silsileHam, baslik, false, baslik == "Tarih" ? 14 : 9);

            KolonEkle(_silsileAcilar, "Durulan", true, 9);
            KolonEkle(_silsileAcilar, "Silsile", true, 6);
            KolonEkle(_silsileAcilar, "Durum", true, 6);
            KolonEkle(_silsileAcilar, "Geri Nokta", true, 9);
            KolonEkle(_silsileAcilar, "Geri Doğrultu (g)", true, 10);
            KolonEkle(_silsileAcilar, "İleri Nokta", true, 9);
            KolonEkle(_silsileAcilar, "İleri Doğrultu (g)", true, 10);
            KolonEkle(_silsileAcilar, "Açı (g)", true, 10);
            KolonEkle(_silsileAcilar, "Fark (cc)", true, 8);
            _silsileAcilar.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Kullan", FillWeight = 6, SortMode = DataGridViewColumnSortMode.NotSortable });
            _silsileAcilar.AllowUserToAddRows = false;
            _silsileAcilar.AllowUserToDeleteRows = false;
            _silsileAcilar.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            foreach (var baslik in new[] { "Durulan", "Geri", "İleri", "Ölçü", "Kesin Açı (g)", "m (cc)", "Geri Kenar (m)", "İleri Kenar (m)", "Not" })
                KolonEkle(_silsileOzet, baslik, true, baslik == "Kesin Açı (g)" || baslik.EndsWith("(m)") ? 11 : baslik == "Not" ? 14 : 7);
            _silsileOzet.Columns[3].ToolTipText = "kullanılan / toplam açı ölçüsü";
            _silsileOzet.Columns[5].ToolTipText = "Kesin açının ortalama hatası: √([vv] / (n − 1))";
            _silsileOzet.AllowUserToAddRows = false;
            _silsileOzet.AllowUserToDeleteRows = false;
            _silsileOzet.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            var arac = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            arac.Items.Add(new ToolStripButton("Excel'den Yapıştır", null, (s, e) => SilsileYapistir()) { ToolTipText = "Panodaki tabloyu (başlık satırı olabilir) ham ölçü tablosunun yerine yapıştırır" });
            arac.Items.Add(new ToolStripButton("Satır Sil", null, (s, e) => SatirlariSil(_silsileHam)));
            arac.Items.Add(new ToolStripButton("Temizle", null, (s, e) =>
            {
                SilsileTemizle();
                SilsileHesapla();
            }));
            arac.Items.Add(new ToolStripSeparator());
            arac.Items.Add(new ToolStripButton("Seçili Ölçüleri Çıkar", null, (s, e) => SeciliAcilariKullan(false)) { ToolTipText = "Açı ölçüleri tablosunda seçili ölçüleri hesaptan çıkarır (Delete)" });
            arac.Items.Add(new ToolStripButton("Geri Al (Kullan)", null, (s, e) => SeciliAcilariKullan(true)) { ToolTipText = "Seçili ölçüleri yeniden hesaba katar" });
            arac.Items.Add(new ToolStripButton("Geri / İleri Değiştir", null, (s, e) => SeciliIstasyonlariTersCevir()) { ToolTipText = "Özet tablosunda seçili istasyonun geri ve ileri noktasını değiştirir" });
            arac.Items.Add(new ToolStripSeparator());
            var karneyeYaz = new ToolStripButton("Karneye Yaz", null, (s, e) => SilsileKarneyeYaz()) { ToolTipText = "Kesin açıları ve kenarları karneye yazar", Font = new Font(arac.Font, FontStyle.Bold) };
            arac.Items.Add(karneyeYaz);

            var altBolum = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            altBolum.Panel1.Controls.Add(_silsileAcilar);
            altBolum.Panel1.Controls.Add(new Label { Text = "Açı Ölçüleri (farkı büyük olanı Kullan işaretini kaldırarak ya da Delete ile hesaptan çıkarın)", Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 3, 0, 0), Font = new Font(Font, FontStyle.Bold) });
            altBolum.Panel2.Controls.Add(_silsileOzet);
            altBolum.Panel2.Controls.Add(new Label { Text = "İstasyonlar (karneye yazılacak kesin açılar ve kenarlar)", Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 3, 0, 0), Font = new Font(Font, FontStyle.Bold) });
            var bolum = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            bolum.Panel1.Controls.Add(_silsileHam);
            bolum.Panel1.Controls.Add(new Label { Text = "Ham Silsile Ölçüleri (Excel'den yapıştırın; düzeltilebilir)", Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 3, 0, 0), Font = new Font(Font, FontStyle.Bold) });
            bolum.Panel2.Controls.Add(altBolum);
            bolum.HandleCreated += (s, e) =>
            {
                bolum.SplitterDistance = Math.Max(80, bolum.Height * 2 / 5);
                altBolum.SplitterDistance = Math.Max(200, altBolum.Width * 52 / 100);
            };

            var sayfa = new TabPage("Silsile");
            sayfa.Controls.Add(bolum);
            sayfa.Controls.Add(_silsileDurum);
            sayfa.Controls.Add(arac);

            _silsileHam.CellValueChanged += (s, e) => { if (!_guncelleniyor) SilsileHesapla(); };
            _silsileHam.RowsRemoved += (s, e) => { if (!_guncelleniyor) SilsileHesapla(); };
            _silsileAcilar.DataError += (s, e) => e.ThrowException = false;
            _silsileOzet.DataError += (s, e) => e.ThrowException = false;
            // Kullan işareti tıklanınca hemen işlenir.
            _silsileAcilar.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_silsileAcilar.IsCurrentCellDirty)
                    _silsileAcilar.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _silsileAcilar.CellValueChanged += (s, e) =>
            {
                if (_silsileGuncelleniyor || e.RowIndex < 0 || e.ColumnIndex != AKullan || !(_silsileAcilar.Rows[e.RowIndex].Tag is SilsileAcisi aci))
                    return;
                var kullan = Convert.ToBoolean(_silsileAcilar.Rows[e.RowIndex].Cells[AKullan].Value);
                if (kullan) _silsileHaric.Remove(aci.Anahtar);
                else _silsileHaric.Add(aci.Anahtar);
                BeginInvoke((Action)SilsileHesapla);
            };
            _silsileAcilar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    SeciliAcilariKullan(false);
                    e.Handled = true;
                }
            };
            SilsileHesapla();
            return sayfa;
        }

        private void SilsileTemizle()
        {
            _silsileHam.Rows.Clear();
            _silsileHaric.Clear();
            _silsileTers.Clear();
        }

        /// <summary>Panodaki tabloyu ham ölçü tablosunun yerine yapıştırır (başlık satırı hesapta kendiliğinden atlanır).</summary>
        private void SilsileYapistir()
        {
            var metin = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(metin))
            {
                MessageBox.Show(this, "Panoda tablo yok. Excel'de tabloyu seçip kopyalayın (Ctrl+C).", "Silsile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_silsileHam.Rows.Count > 1 && MessageBox.Show(this, "Mevcut ham ölçüler silinip panodaki tablo yapıştırılsın mı?", "Silsile",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _guncelleniyor = true;
            try
            {
                SilsileTemizle();
                foreach (var satir in metin.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'))
                {
                    var hucreler = satir.Split('\t').Select(h => h.Trim()).ToList();
                    // Tarih kolonu olmayan (7 kolonlu) tabloda Durum'dan sonra Yatay açı gelir.
                    if (hucreler.Count == 7)
                        hucreler.Insert(STarih, "");
                    while (hucreler.Count < 8)
                        hucreler.Add("");
                    // Başlık satırı (yatay açısı sayı olmayan, durumu I/II olmayan satır) alınmaz.
                    if (SilsileSayi(hucreler[SYatay]) == null && SilsileDurumu(hucreler[SDurum]) == null)
                        continue;
                    _silsileHam.Rows.Add(hucreler.Take(8).Cast<object>().ToArray());
                }
            }
            finally
            {
                _guncelleniyor = false;
            }
            SilsileHesapla();
        }

        private static double? SilsileSayi(string metin)
        {
            metin = (metin ?? "").Trim();
            return metin.Length > 0 && NcnDosyasi.SayiOku(metin, out var deger) ? deger : (double?)null;
        }

        // I / II (ya da 1 / 2) durum; tanınmazsa null.
        private static bool? SilsileDurumu(string metin)
        {
            switch ((metin ?? "").Trim().ToUpperInvariant())
            {
                case "I": case "1": case "I.": return false;
                case "II": case "2": case "II.": return true;
                default: return null;
            }
        }

        private List<SilsileOkuma> SilsileOkumalari()
        {
            var okumalar = new List<SilsileOkuma>();
            var sonDn = "";
            foreach (DataGridViewRow satir in _silsileHam.Rows)
            {
                if (satir.IsNewRow)
                    continue;
                var dn = Metin(satir, SDn);
                if (dn.Length == 0)
                    dn = sonDn; // karne gibi yalnızca ilk satırda yazılmış durulan nokta
                var bn = Metin(satir, SBn);
                var durum = SilsileDurumu(Metin(satir, SDurum));
                var yatay = SilsileSayi(Metin(satir, SYatay));
                // Başlık ya da eksik satırlar atlanır.
                if (dn.Length == 0 || bn.Length == 0 || durum == null || yatay == null)
                    continue;
                sonDn = dn;
                var mesafe = SilsileSayi(Metin(satir, SYatayMesafe));
                if (mesafe == null)
                {
                    var egik = SilsileSayi(Metin(satir, SEgik));
                    var dusey = SilsileSayi(Metin(satir, SDusey));
                    if (egik != null && dusey != null)
                    {
                        var z = dusey.Value > 200 ? 400 - dusey.Value : dusey.Value;
                        mesafe = egik.Value * Math.Sin(z * Math.PI / 200);
                    }
                }
                okumalar.Add(new SilsileOkuma { Dn = dn, Bn = bn, Ikinci = durum.Value, Yatay = yatay.Value, Mesafe = mesafe });
            }
            return okumalar;
        }

        /// <summary>Ham ölçülerden açı ölçülerini, kesin açıları ve kenarları hesaplayıp iki tabloyu yeniden doldurur.</summary>
        private void SilsileHesapla()
        {
            var okumalar = SilsileOkumalari();
            var istasyonSirasi = okumalar.Select(o => o.Dn).Distinct().ToList();
            var istasyonlar = new List<SilsileIstasyonu>();

            for (var k = 0; k < istasyonSirasi.Count; k++)
            {
                var dn = istasyonSirasi[k];
                var ist = new SilsileIstasyonu { Dn = dn };
                istasyonlar.Add(ist);
                var kendi = okumalar.Where(o => o.Dn == dn).ToList();
                var hedefler = kendi.Select(o => o.Bn).Distinct().ToList();
                if (hedefler.Count != 2)
                {
                    ist.Uyari = hedefler.Count < 2 ? "Tek hedef var; açı oluşmaz." : "İkiden fazla hedef (" + string.Join(", ", hedefler) + "); poligon istasyonu iki hedefli olmalıdır.";
                    continue;
                }

                // Geri/ileri: sonradan durulan nokta olan hedef ileri, önceden durulan nokta olan hedef geridir.
                var sonrakiler = istasyonSirasi.Skip(k + 1).ToList();
                var oncekiler = istasyonSirasi.Take(k).ToList();
                string geri = hedefler[0], ileri = hedefler[1];
                if (sonrakiler.Contains(hedefler[0]) && !sonrakiler.Contains(hedefler[1]))
                {
                    geri = hedefler[1];
                    ileri = hedefler[0];
                }
                else if (oncekiler.Contains(hedefler[1]) && !oncekiler.Contains(hedefler[0]))
                {
                    geri = hedefler[1];
                    ileri = hedefler[0];
                }
                if (_silsileTers.Contains(dn))
                {
                    var t = geri;
                    geri = ileri;
                    ileri = t;
                }
                ist.Geri = geri;
                ist.Ileri = ileri;

                // Aynı durumda art arda gelen iki farklı hedefin okuması bir açı ölçüsüdür.
                var bekleyen = new SilsileOkuma[2];
                var sira = 0;
                foreach (var o in kendi)
                {
                    var d = o.Ikinci ? 1 : 0;
                    var onceki = bekleyen[d];
                    if (onceki == null || onceki.Bn == o.Bn)
                    {
                        bekleyen[d] = o;
                        continue;
                    }
                    bekleyen[d] = null;
                    var geriOkuma = onceki.Bn == geri ? onceki : o;
                    var ileriOkuma = onceki.Bn == geri ? o : onceki;
                    var aci = new SilsileAcisi
                    {
                        Dn = dn,
                        Sira = ++sira,
                        Ikinci = o.Ikinci,
                        GeriDog = geriOkuma.Yatay,
                        IleriDog = ileriOkuma.Yatay,
                        Aci = Mod400(ileriOkuma.Yatay - geriOkuma.Yatay)
                    };
                    aci.Kullan = !_silsileHaric.Contains(aci.Anahtar);
                    ist.Acilar.Add(aci);
                }

                // Kesin açı: kullanılan ölçülerin ortalaması (400 g sınırında doğru olsun diye ilk ölçüye göre farklarla).
                var kullanilan = ist.Acilar.Where(a => a.Kullan).ToList();
                if (kullanilan.Count > 0)
                {
                    var taban = kullanilan[0].Aci;
                    var kesin = Mod400(taban + kullanilan.Average(a => Fark(a.Aci - taban)));
                    ist.KesinAci = kesin;
                    if (kullanilan.Count > 1)
                        ist.OrtalamaHataCc = Math.Sqrt(kullanilan.Sum(a => Math.Pow(Fark(a.Aci - kesin) * 10000, 2)) / (kullanilan.Count - 1));
                }
                else if (ist.Acilar.Count == 0)
                {
                    ist.Uyari = "Eşleşen açı ölçüsü yok (aynı durumda iki hedef art arda okunmamış).";
                }
                else
                {
                    ist.Uyari = "Bütün ölçüler hesaptan çıkarıldı.";
                }

                double? Ortalama(string hedef, bool ikinci)
                {
                    var liste = kendi.Where(o => o.Bn == hedef && o.Ikinci == ikinci && o.Mesafe != null).Select(o => o.Mesafe.Value).ToList();
                    return liste.Count > 0 ? liste.Average() : (double?)null;
                }
                ist.GeriMesafe1 = Ortalama(geri, false);
                ist.GeriMesafe2 = Ortalama(geri, true);
                ist.IleriMesafe1 = Ortalama(ileri, false);
                ist.IleriMesafe2 = Ortalama(ileri, true);
            }
            _silsileIstasyonlari = istasyonlar;
            SilsileTablolariniDoldur();
        }

        private static double? IkiOrtalama(double? a, double? b) => a != null && b != null ? (a + b) / 2 : a ?? b;

        private void SilsileTablolariniDoldur()
        {
            var ilkSatir = _silsileAcilar.FirstDisplayedScrollingRowIndex;
            var seciliAnahtarlar = new HashSet<string>(_silsileAcilar.SelectedRows.Cast<DataGridViewRow>().Select(r => (r.Tag as SilsileAcisi)?.Anahtar).Where(a => a != null));
            var seciliIstasyon = _silsileOzet.SelectedRows.Count > 0 ? (_silsileOzet.SelectedRows[0].Tag as SilsileIstasyonu)?.Dn : null;
            var c = CultureInfo.InvariantCulture;

            _silsileGuncelleniyor = true;
            try
            {
                _silsileAcilar.Rows.Clear();
                _silsileOzet.Rows.Clear();
                var acilarArkaPlan = new[] { Color.White, Color.FromArgb(242, 246, 252) };
                for (var k = 0; k < _silsileIstasyonlari.Count; k++)
                {
                    var ist = _silsileIstasyonlari[k];
                    // Kullanılan ölçüler içinde kesin açıdan en çok sapan, kırmızı yazılır.
                    var enBuyuk = ist.KesinAci == null ? null : ist.Acilar.Where(a => a.Kullan).OrderByDescending(a => Math.Abs(Fark(a.Aci - ist.KesinAci.Value))).FirstOrDefault();
                    foreach (var aci in ist.Acilar)
                    {
                        var fark = ist.KesinAci == null ? "" : (Fark(aci.Aci - ist.KesinAci.Value) * 10000).ToString("+0.0;-0.0;0", c);
                        var satir = _silsileAcilar.Rows[_silsileAcilar.Rows.Add(ist.Dn, aci.Sira, aci.Ikinci ? "II" : "I", ist.Geri, G(aci.GeriDog), ist.Ileri, G(aci.IleriDog), G(aci.Aci), fark, aci.Kullan)];
                        satir.Tag = aci;
                        satir.DefaultCellStyle.BackColor = acilarArkaPlan[k % 2];
                        satir.Cells[AKullan].ReadOnly = false;
                        if (!aci.Kullan)
                        {
                            satir.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                            satir.DefaultCellStyle.Font = new Font(_silsileAcilar.Font, FontStyle.Strikeout);
                        }
                        else if (aci == enBuyuk && ist.KullanilanSayisi > 1)
                        {
                            satir.Cells[AFark].Style.ForeColor = Color.Firebrick;
                            satir.Cells[AFark].Style.Font = _kalinYazi;
                        }
                        satir.Selected = seciliAnahtarlar.Contains(aci.Anahtar);
                    }

                    var ozet = _silsileOzet.Rows[_silsileOzet.Rows.Add(ist.Dn, ist.Geri, ist.Ileri,
                        ist.KullanilanSayisi + " / " + ist.Acilar.Count,
                        ist.KesinAci == null ? "" : G(ist.KesinAci.Value),
                        ist.OrtalamaHataCc == null ? "" : ist.OrtalamaHataCc.Value.ToString("±0.0", c),
                        IkiOrtalama(ist.GeriMesafe1, ist.GeriMesafe2) is double gm ? M(gm) : "",
                        IkiOrtalama(ist.IleriMesafe1, ist.IleriMesafe2) is double im ? M(im) : "",
                        ist.Uyari ?? (_silsileTers.Contains(ist.Dn) ? "geri/ileri elle değiştirildi" : ""))];
                    ozet.Tag = ist;
                    if (ist.KesinAci != null)
                        ozet.Cells[4].Style.Font = _kalinYazi;
                    if (ist.Uyari != null)
                        ozet.Cells[8].Style.ForeColor = Color.Firebrick;
                    ozet.Selected = ist.Dn == seciliIstasyon;
                }
            }
            finally
            {
                _silsileGuncelleniyor = false;
            }
            if (ilkSatir >= 0 && ilkSatir < _silsileAcilar.Rows.Count)
                _silsileAcilar.FirstDisplayedScrollingRowIndex = ilkSatir;

            var hazir = _silsileIstasyonlari.Count(i => i.KesinAci != null);
            _silsileDurum.Text = _silsileIstasyonlari.Count == 0
                ? "Excel'deki silsile tablosunu kopyalayıp \"Excel'den Yapıştır\" ile buraya alın. Kolonlar: DN, BN, Durum (I/II), Tarih, Yatay açı, Düşey açı, Eğik mesafe, Yatay mesafe."
                : _silsileIstasyonlari.Count + " istasyon, " + _silsileIstasyonlari.Sum(i => i.Acilar.Count) + " açı ölçüsü (" + _silsileIstasyonlari.Sum(i => i.Acilar.Count(a => !a.Kullan)) + " çıkarıldı). " +
                  hazir + " istasyonun kesin açısı hazır; irdelemeden sonra \"Karneye Yaz\" ile karneye aktarın.";
        }

        private void SeciliAcilariKullan(bool kullan)
        {
            var secili = _silsileAcilar.SelectedRows.Cast<DataGridViewRow>()
                .Concat(_silsileAcilar.SelectedCells.Cast<DataGridViewCell>().Select(h => h.OwningRow))
                .Select(r => r.Tag as SilsileAcisi).Where(a => a != null).Distinct().ToList();
            if (secili.Count == 0)
                return;
            foreach (var aci in secili)
            {
                if (kullan) _silsileHaric.Remove(aci.Anahtar);
                else _silsileHaric.Add(aci.Anahtar);
            }
            SilsileHesapla();
        }

        private void SeciliIstasyonlariTersCevir()
        {
            var secili = _silsileOzet.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Tag as SilsileIstasyonu).Where(i => i != null).ToList();
            if (secili.Count == 0)
            {
                MessageBox.Show(this, "İstasyonlar tablosunda geri/ileri noktası değiştirilecek istasyonu seçin.", "Silsile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var ist in secili)
            {
                if (!_silsileTers.Remove(ist.Dn))
                    _silsileTers.Add(ist.Dn);
                // Açıların yönü değiştiği için çıkarılan ölçü seçimleri bu istasyonda geçerliliğini korur (sıra aynı).
            }
            SilsileHesapla();
        }

        /// <summary>
        /// Kesin açıları ve kenarları karneye yazar: her istasyon için geri satırı (I = 0, II = 200) ve ileri satırı
        /// (I = kesin açı, II = kesin açı + 200); mesafeler hedefin I. ve II. durum yatay mesafe ortalamalarıdır.
        /// </summary>
        private void SilsileKarneyeYaz()
        {
            var hazir = _silsileIstasyonlari.Where(i => i.KesinAci != null).ToList();
            if (hazir.Count == 0)
            {
                MessageBox.Show(this, "Karneye yazılacak kesin açı yok. Önce silsile ölçülerini yapıştırın.", "Karneye Yaz", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var eksikler = _silsileIstasyonlari.Where(i => i.KesinAci == null).Select(i => i.Dn).ToList();
            var soru = "Karnedeki mevcut ölçüler silinip " + hazir.Count + " istasyonun kesin açısı ve kenarları yazılsın mı?" +
                       (eksikler.Count > 0 ? "\n\nKesin açısı olmayan istasyonlar yazılmayacak: " + string.Join(", ", eksikler) : "");
            if (_karne.Rows.Count > 1 || eksikler.Count > 0)
            {
                if (MessageBox.Show(this, soru, "Karneye Yaz", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            string Mesafe(double? d) => d == null ? "" : M(d.Value);
            _guncelleniyor = true;
            try
            {
                _karne.Rows.Clear();
                foreach (var ist in hazir)
                {
                    _karne.Rows.Add(ist.Dn, ist.Geri, G(0), G(200), Mesafe(ist.GeriMesafe1), Mesafe(ist.GeriMesafe2));
                    _karne.Rows.Add("", ist.Ileri, G(ist.KesinAci.Value), G(Mod400(ist.KesinAci.Value + 200)), Mesafe(ist.IleriMesafe1), Mesafe(ist.IleriMesafe2));
                }
            }
            finally
            {
                _guncelleniyor = false;
            }
            KarneDegisti();
            _sekmeler.SelectedTab = _karne.Parent as TabPage ?? _sekmeler.SelectedTab;
            _karneDurum.Text = hazir.Count + " istasyon silsileden karneye yazıldı. " + _karneDurum.Text;
        }

        // ------------------------------------------------------------------ .pol

        private void SilsileMetniniYaz(StringBuilder sb)
        {
            sb.AppendLine("[SILSILE]");
            foreach (DataGridViewRow satir in _silsileHam.Rows)
            {
                if (!satir.IsNewRow)
                    sb.AppendLine(string.Join("\t", Enumerable.Range(0, 8).Select(k => Temiz(satir.Cells[k].Value))));
            }
            sb.AppendLine("[SILSILE_SECIM]");
            foreach (var anahtar in _silsileHaric.OrderBy(a => a, StringComparer.Ordinal))
            {
                var p = anahtar.Split('\u0001');
                sb.AppendLine("Haric\t" + Temiz(p[0]) + "\t" + p[1]);
            }
            foreach (var dn in _silsileTers.OrderBy(a => a, StringComparer.Ordinal))
                sb.AppendLine("Ters\t" + Temiz(dn));
        }

        private void SilsileSatiriniYukle(string bolum, string[] p)
        {
            string Al(int i) => i < p.Length ? p[i] : "";
            if (bolum == "[SILSILE]")
                _silsileHam.Rows.Add(Enumerable.Range(0, 8).Select(i => (object)Al(i)).ToArray());
            else if (Al(0) == "Haric")
                _silsileHaric.Add(Al(1) + "\u0001" + Al(2));
            else if (Al(0) == "Ters")
                _silsileTers.Add(Al(1));
        }
    }
}
