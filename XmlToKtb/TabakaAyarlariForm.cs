using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    /// <summary>
    /// Tabaka ayarları penceresi: çizimdeki tabakaları listeler; aktif tabaka, ad, görünürlük, kilit ve renk buradan
    /// değiştirilir, tabaka eklenir/silinir. Değişiklikler çizime hemen yansır.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class TabakaAyarlariForm : Form
    {
        private const int AktifKolon = 0, AdKolon = 1, GorunurKolon = 2, KilitliKolon = 3, RenkKolon = 4, ObjeKolon = 5;

        private readonly CizimPaneli _panel;
        private readonly DataGridView _tablo = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        private bool _dolduruluyor;

        public TabakaAyarlariForm(CizimPaneli panel)
        {
            _panel = panel;

            Text = "Tabaka Ayarları";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 420);
            MinimumSize = new Size(460, 260);
            MinimizeBox = false;
            ShowInTaskbar = false;

            _tablo.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Aktif", FillWeight = 12 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tabaka", FillWeight = 40 });
            _tablo.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Görünür", FillWeight = 14 });
            _tablo.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Kilitli", FillWeight = 12 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Renk", ReadOnly = true, FillWeight = 12 });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Obje", ReadOnly = true, FillWeight = 10 });
            foreach (DataGridViewColumn kolon in _tablo.Columns)
                kolon.SortMode = DataGridViewColumnSortMode.NotSortable;
            _tablo.Columns[ObjeKolon].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Onay kutuları tıklanır tıklanmaz işlensin (hücreden çıkmayı beklemeden).
            _tablo.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_tablo.IsCurrentCellDirty && _tablo.CurrentCell is DataGridViewCheckBoxCell)
                    _tablo.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _tablo.CellValueChanged += Tablo_CellValueChanged;
            _tablo.CellClick += Tablo_CellClick;

            var altPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6, 6, 6, 0) };
            Button Dugme(string metin, EventHandler tiklama)
            {
                var dugme = new Button { Text = metin, Size = new Size(96, 26) };
                dugme.Click += tiklama;
                altPanel.Controls.Add(dugme);
                return dugme;
            }

            Dugme("Yeni Tabaka", YeniTabaka_Click);
            Dugme("Sil", Sil_Click);
            Dugme("Tümünü Aç", (s, e) => TumununGorunurlugunuAyarla(true));
            Dugme("Tümünü Kapat", (s, e) => TumununGorunurlugunuAyarla(false));
            var kapatDugmesi = Dugme("Kapat", (s, e) => Close());
            CancelButton = kapatDugmesi;

            Controls.Add(_tablo);
            Controls.Add(altPanel);

            Doldur();
        }

        private Tabaka SatirTabakasi(int satir)
        {
            return (Tabaka)_tablo.Rows[satir].Tag;
        }

        private void Doldur()
        {
            _dolduruluyor = true;
            var seciliSatir = _tablo.CurrentCell?.RowIndex ?? 0;
            _tablo.Rows.Clear();
            foreach (var tabaka in _panel.Tabakalar)
            {
                var objeSayisi = _panel.Nesneler.Count(n => n.Tabaka == tabaka);
                var satir = _tablo.Rows[_tablo.Rows.Add(tabaka == _panel.AktifTabaka, tabaka.Ad, tabaka.Gorunur, tabaka.Kilitli, "", objeSayisi)];
                satir.Tag = tabaka;
                satir.Cells[RenkKolon].Style.BackColor = tabaka.Renk;
                satir.Cells[RenkKolon].Style.SelectionBackColor = tabaka.Renk;
            }
            if (_tablo.Rows.Count > 0)
                _tablo.CurrentCell = _tablo.Rows[Math.Min(seciliSatir, _tablo.Rows.Count - 1)].Cells[AdKolon];
            _dolduruluyor = false;
        }

        private void Uygula()
        {
            _panel.TabakalarDegisti();
        }

        private void Tablo_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_dolduruluyor || e.RowIndex < 0)
                return;

            var tabaka = SatirTabakasi(e.RowIndex);
            var deger = _tablo.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            switch (e.ColumnIndex)
            {
                case AktifKolon:
                    // Aktif tabaka tektir: işaret kaldırılamaz, başka satır işaretlenince öncekinden kalkar.
                    _panel.AktifTabaka = tabaka;
                    // Hücre olayı sürerken tablo yeniden doldurulamaz; doldurma olay bittikten sonraya bırakılır.
                    BeginInvoke((Action)Doldur);
                    break;

                case AdKolon:
                {
                    var yeniAd = Convert.ToString(deger)?.Trim();
                    if (string.IsNullOrEmpty(yeniAd) || _panel.Tabakalar.Any(t => t != tabaka && string.Equals(t.Ad, yeniAd, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Tabaka adı boş olamaz ve başka bir tabakayla aynı olamaz.", "Tabaka Ayarları", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        BeginInvoke((Action)Doldur);
                        return;
                    }
                    tabaka.Ad = yeniAd;
                    break;
                }

                case GorunurKolon:
                    tabaka.Gorunur = Convert.ToBoolean(deger);
                    break;

                case KilitliKolon:
                    tabaka.Kilitli = Convert.ToBoolean(deger);
                    break;
            }
            Uygula();
        }

        private void Tablo_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != RenkKolon)
                return;

            var tabaka = SatirTabakasi(e.RowIndex);
            using (var dlg = new ColorDialog { Color = tabaka.Renk, FullOpen = true })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                tabaka.Renk = dlg.Color;
            }
            BeginInvoke((Action)Doldur);
            Uygula();
        }

        private void YeniTabaka_Click(object sender, EventArgs e)
        {
            var sira = 1;
            while (_panel.Tabakalar.Any(t => t.Ad == "Tabaka" + sira))
                sira++;
            _panel.TabakaBulVeyaEkle("Tabaka" + sira, Color.Black, true, false);
            Doldur();
            _tablo.CurrentCell = _tablo.Rows[_tablo.Rows.Count - 1].Cells[AdKolon];
            _tablo.BeginEdit(true);
        }

        private void Sil_Click(object sender, EventArgs e)
        {
            if (_tablo.CurrentCell == null)
                return;

            var tabaka = SatirTabakasi(_tablo.CurrentCell.RowIndex);
            string engel = null;
            if (tabaka == _panel.AktifTabaka)
                engel = "Aktif tabaka silinemez. Önce başka bir tabakayı aktif yapın.";
            else if (_panel.Nesneler.Any(n => n.Tabaka == tabaka))
                engel = "Üzerinde obje olan tabaka silinemez. Önce objeleri silin ya da başka tabakaya alın.";

            if (engel != null)
            {
                MessageBox.Show(engel, "Tabaka Ayarları", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _panel.Tabakalar.Remove(tabaka);
            Doldur();
            Uygula();
        }

        private void TumununGorunurlugunuAyarla(bool gorunur)
        {
            foreach (var tabaka in _panel.Tabakalar)
                tabaka.Gorunur = gorunur;
            Doldur();
            Uygula();
        }
    }
}
