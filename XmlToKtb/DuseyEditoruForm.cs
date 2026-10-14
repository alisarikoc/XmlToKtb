using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Güzergahın düşey geometrisinin (some noktaları ve düşey kurplar) düzenlendiği pencere. Üstte some tablosu,
    /// altta profilin önizlemesi bulunur; her değişiklik önizlemeye hemen yansır. Tamam ile güzergaha yazılır.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class DuseyEditoruForm : Form
    {
        private const int SiraKolon = 0, KmKolon = 1, KotKolon = 2, TipKolon = 3, BoyKolon = 4, YaricapKolon = 5, GirisKolon = 6, CikisKolon = 7;
        private static readonly string[] TipAdlari = { "Yok", "Parabol", "Daire" };

        private readonly GuzergahNesnesi _guzergah;
        private readonly List<DuseyNokta> _noktalar;
        private readonly List<List<DuseyNokta>> _geriAlListesi = new List<List<DuseyNokta>>();
        private bool _dolduruluyor;
        private ToolStripButton _geriAlDugmesi;
        private readonly Label _ozetLabel = new Label { AutoSize = true, Location = new Point(10, 13) };
        private readonly Panel _onizleme = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.White };

        private readonly DataGridView _tablo = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersWidth = 28,
            SelectionMode = DataGridViewSelectionMode.RowHeaderSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                // Fare tekerleği olayını alabilmesi için panel odaklanabilir olmalıdır.
                SetStyle(ControlStyles.Selectable, true);
                TabStop = true;
            }
        }

        public DuseyEditoruForm(GuzergahNesnesi guzergah)
        {
            _guzergah = guzergah;
            _noktalar = guzergah.Dusey.Select(n => n.Kopyala()).ToList();

            Text = "Düşey Editörü - " + guzergah.Ad;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 600);
            MinimumSize = new Size(700, 420);
            MinimizeBox = false;
            ShowInTaskbar = false;

            var araclar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            ToolStripButton Dugme(string metin, string ipucu, EventHandler tiklama)
            {
                var dugme = new ToolStripButton(metin) { DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = ipucu };
                dugme.Click += tiklama;
                araclar.Items.Add(dugme);
                return dugme;
            }
            Dugme("Ekle", "Seçili someden sonra yeni some ekler", (s, e) => Ekle());
            Dugme("Çıkar", "Seçili someleri siler", (s, e) => Cikar());
            Dugme("Sırala", "Someleri kilometreye göre sıralar", (s, e) => Sirala());
            araclar.Items.Add(new ToolStripSeparator());
            _geriAlDugmesi = Dugme("Geri Al", "Son değişikliği geri alır", (s, e) => GeriAl());

            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sıra", ReadOnly = true, FillWeight = 6 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kilometre", FillWeight = 14 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kot", FillWeight = 12 });
            var tipKolonu = new DataGridViewComboBoxColumn { HeaderText = "Düşey Kurp", FillWeight = 12, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            tipKolonu.Items.AddRange(TipAdlari);
            _tablo.Columns.Add(tipKolonu);
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kurp Boyu (L)", FillWeight = 13 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Yarıçap (R)", FillWeight = 13 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giriş Eğimi (%)", FillWeight = 13 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Çıkış Eğimi (%)", FillWeight = 13 });
            foreach (DataGridViewColumn kolon in _tablo.Columns)
            {
                kolon.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (kolon.ReadOnly)
                    kolon.DefaultCellStyle.BackColor = Color.FromArgb(244, 244, 244);
                if (!(kolon is DataGridViewComboBoxColumn))
                    kolon.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            _tablo.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_tablo.IsCurrentCellDirty && _tablo.CurrentCell is DataGridViewComboBoxCell)
                    _tablo.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _tablo.CellValueChanged += Tablo_CellValueChanged;
            _tablo.DataError += (s, e) => e.ThrowException = false;
            _tablo.CurrentCellChanged += (s, e) => _onizleme.Invalidate();
            _onizleme.Paint += Onizleme_Paint;
            OnizlemeOlaylariniBagla();

            var bolme = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            bolme.Panel1.Controls.Add(_tablo);
            bolme.Panel2.Controls.Add(_onizleme);
            // Bölme yeri ancak pencere boyutlandıktan sonra verilebilir: üstte tablo, altta biraz daha küçük önizleme.
            Load += (s, e) => bolme.SplitterDistance = Math.Max(bolme.Panel1MinSize, bolme.Height * 55 / 100);

            var tamamButton = new Button { Text = "Tamam", Size = new Size(90, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var vazgecButton = new Button { Text = "Vazgeç", Size = new Size(90, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            tamamButton.Location = new Point(altPanel.Width - 196, 7);
            vazgecButton.Location = new Point(altPanel.Width - 100, 7);
            altPanel.Controls.Add(_ozetLabel);
            altPanel.Controls.Add(tamamButton);
            altPanel.Controls.Add(vazgecButton);
            tamamButton.Click += TamamButton_Click;
            CancelButton = vazgecButton;

            Controls.Add(bolme);
            Controls.Add(altPanel);
            Controls.Add(araclar);

            Doldur(0);
        }

        private static string Sayi(double deger, string bicim = "0.000")
        {
            return deger.ToString(bicim, CultureInfo.InvariantCulture);
        }

        // Dairesel kurpta tasarım değeri yarıçaptır: komşu kot ya da kilometreler değişince kurp boyu yarıçaptan
        // yeniden hesaplanır (L = R x eğim farkı). Uçtaki somelerde kurp olmaz.
        private void KurplariDuzelt()
        {
            for (var i = 0; i < _noktalar.Count; i++)
            {
                var nokta = _noktalar[i];
                if (i == 0 || i == _noktalar.Count - 1)
                {
                    nokta.Tip = DuseyKurpTipi.Yok;
                    nokta.Uzunluk = 0;
                    nokta.Yaricap = 0;
                }
                else if (nokta.Tip == DuseyKurpTipi.Daire && nokta.Yaricap > 0)
                {
                    nokta.Uzunluk = nokta.Yaricap * Math.Abs(DuseyGeometri.EgimFarki(_noktalar, i));
                }
                else if (nokta.Tip == DuseyKurpTipi.Yok)
                {
                    nokta.Uzunluk = 0;
                    nokta.Yaricap = 0;
                }
            }
        }

        private void Doldur(int secilecekSatir)
        {
            KurplariDuzelt();
            _dolduruluyor = true;
            _tablo.Rows.Clear();
            for (var i = 0; i < _noktalar.Count; i++)
            {
                _tablo.Rows.Add();
                SatiriYaz(i);
            }
            if (_tablo.Rows.Count > 0)
            {
                _tablo.ClearSelection();
                _tablo.CurrentCell = _tablo.Rows[Math.Max(0, Math.Min(secilecekSatir, _tablo.Rows.Count - 1))].Cells[KmKolon];
            }
            _dolduruluyor = false;
            DurumuGuncelle();
        }

        private void SatiriYaz(int i)
        {
            var nokta = _noktalar[i];
            var satir = _tablo.Rows[i];
            var uc = i == 0 || i == _noktalar.Count - 1;
            var kurpVar = !uc && nokta.Tip != DuseyKurpTipi.Yok;

            satir.Cells[SiraKolon].Value = (i + 1).ToString();
            satir.Cells[KmKolon].Value = Sayi(nokta.Km);
            satir.Cells[KotKolon].Value = Sayi(nokta.Kot);
            satir.Cells[TipKolon].Value = TipAdlari[(int)nokta.Tip];
            satir.Cells[BoyKolon].Value = kurpVar ? Sayi(nokta.Uzunluk) : "";
            satir.Cells[YaricapKolon].Value = kurpVar && nokta.Tip == DuseyKurpTipi.Daire ? Sayi(nokta.Yaricap) : "";
            satir.Cells[GirisKolon].Value = i > 0 ? Sayi(DuseyGeometri.Egim(_noktalar, i - 1) * 100, "0.0000") : "";
            satir.Cells[CikisKolon].Value = i < _noktalar.Count - 1 ? Sayi(DuseyGeometri.Egim(_noktalar, i) * 100, "0.0000") : "";

            // Güzergahın ilk ve son somesinde düşey kurp olmaz; parabolde yarıçap girilmez.
            satir.Cells[TipKolon].ReadOnly = uc;
            satir.Cells[BoyKolon].ReadOnly = !kurpVar;
            satir.Cells[YaricapKolon].ReadOnly = !kurpVar || nokta.Tip != DuseyKurpTipi.Daire;
            // İlk somenin giriş, son somenin çıkış eğimi yoktur.
            satir.Cells[GirisKolon].ReadOnly = i == 0;
            satir.Cells[CikisKolon].ReadOnly = i == _noktalar.Count - 1;
            satir.Cells[GirisKolon].Style.BackColor = i == 0 ? Color.FromArgb(244, 244, 244) : Color.Empty;
            satir.Cells[CikisKolon].Style.BackColor = i == _noktalar.Count - 1 ? Color.FromArgb(244, 244, 244) : Color.Empty;
        }

        private void DegerlerDegisti()
        {
            KurplariDuzelt();
            _dolduruluyor = true;
            for (var i = 0; i < _noktalar.Count && i < _tablo.Rows.Count; i++)
                SatiriYaz(i);
            _dolduruluyor = false;
            DurumuGuncelle();
        }

        private void DurumuGuncelle()
        {
            _ozetLabel.Text = _noktalar.Count == 0
                ? "Düşey geometri yok. \"Ekle\" ile some noktası ekleyin."
                : _noktalar.Count + " some   |   Km " + Sayi(_noktalar.Min(n => n.Km)) + " - " + Sayi(_noktalar.Max(n => n.Km)) +
                  "   |   Kot " + Sayi(_noktalar.Min(n => n.Kot)) + " - " + Sayi(_noktalar.Max(n => n.Kot));
            _geriAlDugmesi.Enabled = _geriAlListesi.Count > 0;
            _onizleme.Invalidate();
        }

        private void GeriAlKaydet()
        {
            _geriAlListesi.Add(_noktalar.Select(n => n.Kopyala()).ToList());
            if (_geriAlListesi.Count > 100)
                _geriAlListesi.RemoveAt(0);
        }

        private void GeriAl()
        {
            if (_geriAlListesi.Count == 0)
                return;
            _tablo.CancelEdit();
            var satir = _tablo.CurrentCell?.RowIndex ?? 0;
            _noktalar.Clear();
            _noktalar.AddRange(_geriAlListesi[_geriAlListesi.Count - 1]);
            _geriAlListesi.RemoveAt(_geriAlListesi.Count - 1);
            Doldur(satir);
        }

        private void Tablo_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_dolduruluyor || e.RowIndex < 0 || e.RowIndex >= _noktalar.Count)
                return;

            var nokta = _noktalar[e.RowIndex];
            var metin = Convert.ToString(_tablo.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? "";
            var onceki = _noktalar.Select(n => n.Kopyala()).ToList();
            string hata = null;

            switch (e.ColumnIndex)
            {
                case KmKolon:
                    if (NcnDosyasi.SayiOku(metin, out var km)) nokta.Km = km;
                    else hata = "Kilometre geçerli bir sayı olmalıdır.";
                    break;

                case KotKolon:
                    if (NcnDosyasi.SayiOku(metin, out var kot)) nokta.Kot = kot;
                    else hata = "Kot geçerli bir sayı olmalıdır.";
                    break;

                case TipKolon:
                {
                    var tip = (DuseyKurpTipi)Math.Max(0, Array.IndexOf(TipAdlari, metin));
                    if (tip == nokta.Tip)
                        return;
                    nokta.Tip = tip;
                    // Kurp yeni eklendiyse başlangıç değerleri verilir.
                    if (tip == DuseyKurpTipi.Parabol && nokta.Uzunluk <= 0)
                        nokta.Uzunluk = 100;
                    if (tip == DuseyKurpTipi.Daire && nokta.Yaricap <= 0)
                    {
                        var egimFarki = Math.Abs(DuseyGeometri.EgimFarki(_noktalar, e.RowIndex));
                        nokta.Yaricap = nokta.Uzunluk > 0 && egimFarki > 1e-9 ? nokta.Uzunluk / egimFarki : 5000;
                    }
                    break;
                }

                case BoyKolon:
                    if (NcnDosyasi.SayiOku(metin, out var boy) && boy > 0)
                    {
                        nokta.Uzunluk = boy;
                        // Dairesel kurpta boy değişince yarıçap ona göre bulunur (R = L / eğim farkı).
                        var egimFarki = Math.Abs(DuseyGeometri.EgimFarki(_noktalar, e.RowIndex));
                        if (nokta.Tip == DuseyKurpTipi.Daire && egimFarki > 1e-9)
                            nokta.Yaricap = boy / egimFarki;
                    }
                    else
                    {
                        hata = "Kurp boyu sıfırdan büyük bir sayı olmalıdır.";
                    }
                    break;

                case YaricapKolon:
                    if (NcnDosyasi.SayiOku(metin, out var yaricap) && yaricap > 0) nokta.Yaricap = yaricap;
                    else hata = "Yarıçap sıfırdan büyük bir sayı olmalıdır.";
                    break;

                case GirisKolon:
                    // Giriş eğimi değişince bu some yerinde kalır; önceki somenin kotu yeni eğime göre hesaplanır.
                    if (e.RowIndex == 0)
                        return;
                    if (NcnDosyasi.SayiOku(metin, out var girisEgimi))
                    {
                        var onceki2 = _noktalar[e.RowIndex - 1];
                        onceki2.Kot = nokta.Kot - girisEgimi / 100.0 * (nokta.Km - onceki2.Km);
                    }
                    else
                    {
                        hata = "Eğim yüzde olarak geçerli bir sayı olmalıdır (ör. 1.25 ya da -0.3).";
                    }
                    break;

                case CikisKolon:
                    // Çıkış eğimi değişince bu some yerinde kalır; sonraki somenin kotu yeni eğime göre hesaplanır.
                    if (e.RowIndex >= _noktalar.Count - 1)
                        return;
                    if (NcnDosyasi.SayiOku(metin, out var cikisEgimi))
                    {
                        var sonraki = _noktalar[e.RowIndex + 1];
                        sonraki.Kot = nokta.Kot + cikisEgimi / 100.0 * (sonraki.Km - nokta.Km);
                    }
                    else
                    {
                        hata = "Eğim yüzde olarak geçerli bir sayı olmalıdır (ör. 1.25 ya da -0.3).";
                    }
                    break;

                default:
                    return;
            }

            if (hata != null)
            {
                MessageBox.Show(hata, "Düşey Editörü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BeginInvoke((Action)DegerlerDegisti);
                return;
            }

            _geriAlListesi.Add(onceki);
            // Hücre olayı sürerken satırlar yeniden yazılamaz; yazma olay bittikten sonraya bırakılır.
            BeginInvoke((Action)DegerlerDegisti);
        }

        private List<int> SeciliSatirlar()
        {
            var satirlar = _tablo.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).ToList();
            if (satirlar.Count == 0 && _tablo.CurrentCell != null)
                satirlar.Add(_tablo.CurrentCell.RowIndex);
            satirlar.Sort();
            return satirlar;
        }

        private void Ekle()
        {
            _tablo.EndEdit();
            GeriAlKaydet();

            // Yeni some, seçili some ile sonraki arasının ortasına (kotu aradaki eğime göre) eklenir.
            var satirlar = SeciliSatirlar();
            var onceki = satirlar.Count > 0 ? satirlar[satirlar.Count - 1] : _noktalar.Count - 1;
            var yeni = new DuseyNokta();
            if (_noktalar.Count == 0)
            {
                yeni.Km = _guzergah.BaslangicKm;
            }
            else if (onceki >= 0 && onceki + 1 < _noktalar.Count)
            {
                yeni.Km = (_noktalar[onceki].Km + _noktalar[onceki + 1].Km) / 2;
                yeni.Kot = (_noktalar[onceki].Kot + _noktalar[onceki + 1].Kot) / 2;
            }
            else
            {
                // Sona ekleme: güzergahın sonu daha ilerideyse oraya, değilse son someden 100 m ileriye.
                var son = _noktalar[_noktalar.Count - 1];
                yeni.Km = _guzergah.BitisKm > son.Km + 1e-6 ? _guzergah.BitisKm : son.Km + 100;
                yeni.Kot = son.Kot;
            }

            _noktalar.Insert(Math.Max(0, onceki + 1), yeni);
            Doldur(onceki + 1);
        }

        private void Cikar()
        {
            _tablo.EndEdit();
            var satirlar = SeciliSatirlar();
            if (satirlar.Count == 0)
                return;

            GeriAlKaydet();
            for (var k = satirlar.Count - 1; k >= 0; k--)
                _noktalar.RemoveAt(satirlar[k]);
            Doldur(satirlar[0]);
        }

        private void Sirala()
        {
            _tablo.EndEdit();
            GeriAlKaydet();
            var sirali = _noktalar.OrderBy(n => n.Km).ToList();
            _noktalar.Clear();
            _noktalar.AddRange(sirali);
            Doldur(0);
        }

        private void TamamButton_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();
            KurplariDuzelt();

            for (var i = 1; i < _noktalar.Count; i++)
            {
                if (_noktalar[i].Km > _noktalar[i - 1].Km)
                    continue;
                MessageBox.Show(
                    "Somelerin kilometreleri artan sırada olmalıdır (" + (i + 1) + ". some öncekinden geride ya da aynı kilometrede). " +
                    "\"Sırala\" ile düzeltebilirsiniz.",
                    "Düşey Editörü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _guzergah.Dusey.Clear();
            _guzergah.Dusey.AddRange(_noktalar.Select(n => n.Kopyala()));
            DialogResult = DialogResult.OK;
        }

        // ------------------------------------------------------------------ profil önizlemesi

        // Önizlemede görünen kilometre ve kot aralığı. Fare tekerleğiyle yakınlaştırılır, sürükleyerek kaydırılır;
        // ayarlanmamışsa (ya da çift tıklanınca) profilin tamamı gösterilir.
        private bool _gorunumAyarli;
        private double _gKmMin, _gKmMax, _gKotMin, _gKotMax;
        private bool _surukleniyor;
        private Point _surukBaslangic;
        private double _surukKmMin, _surukKmMax, _surukKotMin, _surukKotMax;

        private Rectangle OnizlemeAlani()
        {
            const int kenar = 44;
            return new Rectangle(kenar + 16, 14, _onizleme.Width - kenar - 32, _onizleme.Height - 14 - 30);
        }

        private void GorunumuSigdir(List<DuseyNokta> sirali)
        {
            _gKmMin = sirali[0].Km;
            _gKmMax = sirali[sirali.Count - 1].Km;
            if (_gKmMax - _gKmMin < 1e-6)
                _gKmMax = _gKmMin + 1;
            // Kot ekseni, düz profilde bile okunabilsin diye en az 1 m açılır ve biraz pay bırakılır.
            double kotMin = sirali.Min(n => n.Kot), kotMax = sirali.Max(n => n.Kot);
            var kotPay = Math.Max((kotMax - kotMin) * 0.15, 0.5);
            _gKotMin = kotMin - kotPay;
            _gKotMax = kotMax + kotPay;
            _gorunumAyarli = true;
        }

        private void OnizlemeOlaylariniBagla()
        {
            // Tekerlek olayı odaktaki kontrole gider; fare önizlemenin üzerine gelince (hücre düzenlenmiyorsa) odak alınır.
            _onizleme.MouseEnter += (s, e) =>
            {
                if (!_tablo.IsCurrentCellInEditMode)
                    _onizleme.Focus();
            };

            _onizleme.MouseWheel += (s, e) =>
            {
                var alan = OnizlemeAlani();
                if (!_gorunumAyarli || alan.Width < 40 || alan.Height < 40)
                    return;

                // İmlecin altındaki kilometre ve kot yerinde kalacak şekilde yakınlaştırılır. Ctrl basılıyken yalnızca
                // kot ekseni, Shift basılıyken yalnızca kilometre ekseni yakınlaşır.
                var carpan = e.Delta > 0 ? 1 / 1.25 : 1.25;
                var oranX = Math.Max(0, Math.Min(1, (e.X - alan.Left) / (double)alan.Width));
                var oranY = Math.Max(0, Math.Min(1, (alan.Bottom - e.Y) / (double)alan.Height));
                var km = _gKmMin + oranX * (_gKmMax - _gKmMin);
                var kot = _gKotMin + oranY * (_gKotMax - _gKotMin);

                if (ModifierKeys != Keys.Control)
                {
                    var genislik = Math.Max((_gKmMax - _gKmMin) * carpan, 0.5);
                    _gKmMin = km - oranX * genislik;
                    _gKmMax = _gKmMin + genislik;
                }
                if (ModifierKeys != Keys.Shift)
                {
                    var yukseklik = Math.Max((_gKotMax - _gKotMin) * carpan, 0.01);
                    _gKotMin = kot - oranY * yukseklik;
                    _gKotMax = _gKotMin + yukseklik;
                }
                _onizleme.Invalidate();
            };

            _onizleme.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle)
                    return;
                if (e.Clicks >= 2)
                {
                    // Çift tıklama profilin tamamını yeniden gösterir.
                    _gorunumAyarli = false;
                    _onizleme.Invalidate();
                    return;
                }
                _surukleniyor = true;
                _surukBaslangic = e.Location;
                _surukKmMin = _gKmMin; _surukKmMax = _gKmMax; _surukKotMin = _gKotMin; _surukKotMax = _gKotMax;
            };
            _onizleme.MouseMove += (s, e) =>
            {
                var alan = OnizlemeAlani();
                if (!_surukleniyor || alan.Width < 40 || alan.Height < 40)
                    return;
                var kmFarki = (e.X - _surukBaslangic.X) / (double)alan.Width * (_surukKmMax - _surukKmMin);
                var kotFarki = (e.Y - _surukBaslangic.Y) / (double)alan.Height * (_surukKotMax - _surukKotMin);
                _gKmMin = _surukKmMin - kmFarki; _gKmMax = _surukKmMax - kmFarki;
                _gKotMin = _surukKotMin + kotFarki; _gKotMax = _surukKotMax + kotFarki;
                _onizleme.Invalidate();
            };
            _onizleme.MouseUp += (s, e) => _surukleniyor = false;
        }

        private void Onizleme_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var alan = OnizlemeAlani();

            var sirali = _noktalar.OrderBy(n => n.Km).ToList();
            if (sirali.Count < 2 || alan.Width < 40 || alan.Height < 40)
            {
                TextRenderer.DrawText(g, "Profil önizlemesi için en az iki some gerekir.", Font, new Point(12, 12), SystemColors.GrayText);
                return;
            }

            if (!_gorunumAyarli)
                GorunumuSigdir(sirali);
            double kmMin = _gKmMin, kmMax = _gKmMax, kotMin = _gKotMin, kotMax = _gKotMax;

            PointF Ekran(double km, double kot)
            {
                return new PointF(
                    (float)(alan.Left + (km - kmMin) / (kmMax - kmMin) * alan.Width),
                    (float)(alan.Bottom - (kot - kotMin) / (kotMax - kotMin) * alan.Height));
            }

            using (var cerceve = new Pen(Color.Silver))
                g.DrawRectangle(cerceve, alan);
            TextRenderer.DrawText(g, Sayi(kotMax, "0.00"), Font, new Point(2, alan.Top - 6), SystemColors.GrayText);
            TextRenderer.DrawText(g, Sayi(kotMin, "0.00"), Font, new Point(2, alan.Bottom - 8), SystemColors.GrayText);
            TextRenderer.DrawText(g, "Km " + Sayi(kmMin), Font, new Point(alan.Left, alan.Bottom + 6), SystemColors.GrayText);
            var sonYazi = "Km " + Sayi(kmMax);
            TextRenderer.DrawText(g, sonYazi, Font, new Point(alan.Right - TextRenderer.MeasureText(sonYazi, Font).Width, alan.Bottom + 6), SystemColors.GrayText);
            var ipucu = "Tekerlek: yakınlaştır (Ctrl: yalnız kot, Shift: yalnız km)  |  Sürükle: kaydır  |  Çift tık: tümünü göster";
            TextRenderer.DrawText(g, ipucu, Font, new Point(alan.Left + (alan.Width - TextRenderer.MeasureText(ipucu, Font).Width) / 2, alan.Bottom + 6), SystemColors.GrayText);

            // Çizim, yakınlaştırıldığında çerçevenin dışına taşmasın.
            var eskiKirpma = g.Clip;
            g.SetClip(alan);

            // Teğet çokgeni (someler arası doğrular) kesikli, kurplu profil düz kırmızı çizilir.
            using (var tegetKalemi = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dash })
                g.DrawLines(tegetKalemi, sirali.Select(n => Ekran(n.Km, n.Kot)).ToArray());

            var profil = new List<PointF>();
            var adim = Math.Max(alan.Width, 2);
            for (var i = 0; i <= adim; i++)
            {
                var km = kmMin + (kmMax - kmMin) * i / adim;
                var kot = DuseyGeometri.KotAl(sirali, km);
                if (kot != null)
                    profil.Add(Ekran(km, kot.Value));
            }
            if (profil.Count >= 2)
            {
                using (var profilKalemi = new Pen(Color.Red, 2f))
                    g.DrawLines(profilKalemi, profil.ToArray());
            }

            // Some noktaları; tabloda üzerinde durulan some vurgulanır.
            var secili = _tablo.CurrentCell != null && _tablo.CurrentCell.RowIndex < _noktalar.Count ? _noktalar[_tablo.CurrentCell.RowIndex] : null;
            foreach (var nokta in sirali)
            {
                var p = Ekran(nokta.Km, nokta.Kot);
                var vurgulu = ReferenceEquals(nokta, secili);
                var boyut = vurgulu ? 9f : 5f;
                using (var firca = new SolidBrush(vurgulu ? Color.OrangeRed : Color.DimGray))
                    g.FillEllipse(firca, p.X - boyut / 2, p.Y - boyut / 2, boyut, boyut);
            }

            // Düşey kurpların giriş (G) ve çıkış (Ç) noktaları: profil üzerinde işaret ve kilometre/kot etiketi.
            // Sıkışık yerlerde etiketler üst üste binmesin diye sığmayan etiket yazılmaz (yakınlaşınca görünür).
            var yazilanlar = new List<RectangleF>();
            using (var isaretKalemi = new Pen(Color.RoyalBlue, 1.5f))
            using (var yaziFircasi = new SolidBrush(Color.RoyalBlue))
            using (var kucukFont = new Font(Font.FontFamily, 7.5f))
            {
                for (var i = 1; i < sirali.Count - 1; i++)
                {
                    var some = sirali[i];
                    if (some.Tip == DuseyKurpTipi.Yok || some.Uzunluk <= 0)
                        continue;

                    foreach (var giris in new[] { true, false })
                    {
                        var km = some.Km + (giris ? -1 : 1) * some.Uzunluk / 2;
                        var kot = DuseyGeometri.KotAl(sirali, km);
                        if (kot == null)
                            continue;

                        var p = Ekran(km, kot.Value);
                        if (p.X < alan.Left - 2 || p.X > alan.Right + 2 || p.Y < alan.Top - 2 || p.Y > alan.Bottom + 2)
                            continue;

                        g.DrawLine(isaretKalemi, p.X, p.Y - 6, p.X, p.Y + 6);

                        var metin = (giris ? "G " : "Ç ") + GuzergahNesnesi.KmMetni(km, 3) + "\n" + Sayi(kot.Value);
                        var boyut = g.MeasureString(metin, kucukFont);
                        // Giriş etiketi işaretin soluna, çıkış etiketi sağına; profilin üzerine yazılır.
                        var kutu = new RectangleF(giris ? p.X - boyut.Width - 2 : p.X + 2, p.Y - boyut.Height - 8, boyut.Width, boyut.Height);
                        if (kutu.Top < alan.Top)
                            kutu.Y = p.Y + 8;
                        if (yazilanlar.Any(y => y.IntersectsWith(kutu)))
                            continue;
                        yazilanlar.Add(kutu);
                        g.DrawString(metin, kucukFont, yaziFircasi, kutu.Location);
                    }
                }
            }

            g.Clip = eskiKirpma;
        }
    }
}
