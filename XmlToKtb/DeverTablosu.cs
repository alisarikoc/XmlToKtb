using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Common;

namespace XmlToKtb
{
    /// <summary>Dever tablosunun bir satırı: bu kilometredeki dever (mm) ve kurp yönü ("cw"/"ccw").</summary>
    public class DeverSatiri
    {
        public double Km;
        public double Dever;
        public string Yon = "cw";
    }

    /// <summary>
    /// Güzergahın dever (superelevation) tablosu ve tip kesite dever uygulama hesabı. Satırlar arasında dever
    /// doğrusal değişir; tablonun dışında kalan kilometrelerde dever 0'dır.
    /// </summary>
    public class DeverTablosu
    {
        public readonly List<DeverSatiri> Satirlar = new List<DeverSatiri>();
        /// <summary>Ray ekseni açıklığı (m); dever açısı dever/ekartman oranından bulunur.</summary>
        public double Ekartman = 1.5;

        public DeverTablosu Kopyala()
        {
            var kopya = new DeverTablosu { Ekartman = Ekartman };
            kopya.Satirlar.AddRange(Satirlar.Select(s => new DeverSatiri { Km = s.Km, Dever = s.Dever, Yon = s.Yon }));
            return kopya;
        }

        /// <summary>Güzergah dosyasındaki dever (Cant) istasyonlarından tablo kurar; dosyada dever yoksa tablo boş olur.</summary>
        public static DeverTablosu GuzergahtanOku(LandXMLAlignmentsAlignment alignment)
        {
            var tablo = new DeverTablosu();
            var cant = alignment?.Cant;
            if (cant == null)
                return tablo;

            if (cant.gauge > 0)
                tablo.Ekartman = Convert.ToDouble(cant.gauge);

            if (cant.CantStation == null)
                return tablo;

            foreach (var istasyon in cant.CantStation)
            {
                tablo.Satirlar.Add(new DeverSatiri
                {
                    Km = Convert.ToDouble(istasyon.station),
                    Dever = Convert.ToDouble(istasyon.appliedCant),
                    Yon = istasyon.curvature == "ccw" ? "ccw" : "cw"
                });
            }
            return tablo;
        }

        public bool AyniMi(DeverTablosu diger)
        {
            if (diger == null || diger.Satirlar.Count != Satirlar.Count)
                return false;

            for (var i = 0; i < Satirlar.Count; i++)
            {
                if (Satirlar[i].Km != diger.Satirlar[i].Km || Satirlar[i].Dever != diger.Satirlar[i].Dever || Satirlar[i].Yon != diger.Satirlar[i].Yon)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tabloyu LandXML güzergah dosyasındaki dever (Cant/CantStation) istasyonlarının yerine yazar. Dosyanın geri
        /// kalanı olduğu gibi korunur. Kilometresi değişmeyen istasyonların diğer öznitelikleri (hız vb.) de korunur.
        /// </summary>
        public void XmlDosyasinaKaydet(string dosyaYolu)
        {
            var belge = new XmlDocument { PreserveWhitespace = true };
            belge.Load(dosyaYolu);

            var alignment = belge.SelectSingleNode("//*[local-name()='Alignments']/*[local-name()='Alignment']") as XmlElement;
            if (alignment == null)
                throw new InvalidOperationException("Dosyada bir güzergah ekseni (Alignment) bulunamadı.");

            var cant = alignment.SelectSingleNode("*[local-name()='Cant']") as XmlElement;
            if (cant == null)
            {
                cant = belge.CreateElement("Cant", alignment.NamespaceURI);
                cant.SetAttribute("name", alignment.GetAttribute("name"));
                cant.SetAttribute("gauge", Ekartman.ToString("R", CultureInfo.InvariantCulture));
                cant.SetAttribute("rotationPoint", "insideRail");
                alignment.AppendChild(belge.CreateWhitespace("\t"));
                alignment.AppendChild(cant);
                alignment.AppendChild(belge.CreateWhitespace(Environment.NewLine + "\t\t"));
            }

            var eskiIstasyonlar = new List<XmlElement>();
            var digerElemanlar = new List<XmlElement>();
            foreach (XmlNode cocuk in cant.ChildNodes)
            {
                if (!(cocuk is XmlElement eleman))
                    continue;
                if (eleman.LocalName == "CantStation")
                    eskiIstasyonlar.Add(eleman);
                else
                    digerElemanlar.Add(eleman);
            }

            while (cant.HasChildNodes)
                cant.RemoveChild(cant.FirstChild);

            var girinti = Environment.NewLine + "\t\t\t\t";
            foreach (var satir in Satirlar)
            {
                // Kilometresi aynı kalan istasyon yeniden kullanılır ki hız gibi diğer öznitelikleri kaybolmasın.
                var istasyon = eskiIstasyonlar.FirstOrDefault(e =>
                    double.TryParse(e.GetAttribute("station"), NumberStyles.Float, CultureInfo.InvariantCulture, out var km)
                    && Math.Abs(km - satir.Km) < 1e-6);
                if (istasyon != null)
                {
                    eskiIstasyonlar.Remove(istasyon);
                }
                else
                {
                    istasyon = belge.CreateElement("CantStation", cant.NamespaceURI);
                    istasyon.SetAttribute("station", satir.Km.ToString("R", CultureInfo.InvariantCulture));
                }

                istasyon.SetAttribute("appliedCant", satir.Dever.ToString("R", CultureInfo.InvariantCulture));
                istasyon.SetAttribute("curvature", satir.Yon);
                cant.AppendChild(belge.CreateWhitespace(girinti));
                cant.AppendChild(istasyon);
            }

            foreach (var eleman in digerElemanlar)
            {
                cant.AppendChild(belge.CreateWhitespace(girinti));
                cant.AppendChild(eleman);
            }
            cant.AppendChild(belge.CreateWhitespace(Environment.NewLine + "\t\t\t"));

            belge.Save(dosyaYolu);
        }

        /// <summary>
        /// Verilen kilometredeki deveri (mm) ve yönünü verir. İki satır arasında dever doğrusal enterpole edilir, yön
        /// aralığın başındaki satırdan alınır (KSEIcerigi.GetirDeverVeYon ile aynı kural).
        /// </summary>
        public void DeverGetir(double km, out double dever, out string yon)
        {
            dever = 0;
            yon = "cw";

            for (var i = 0; i < Satirlar.Count - 1; i++)
            {
                var bas = Satirlar[i];
                var son = Satirlar[i + 1];
                if (km < bas.Km || km >= son.Km)
                    continue;

                dever = son.Km > bas.Km ? bas.Dever + (son.Dever - bas.Dever) / (son.Km - bas.Km) * (km - bas.Km) : bas.Dever;
                yon = bas.Yon;
                return;
            }
        }

        /// <summary>
        /// Tip kesit noktasına dever uygular: nokta referans noktası (0,0) etrafında dever açısı kadar döndürülür
        /// ("cw" saat yönünde, "ccw" tersine) ve eksen deverin yarısı kadar yükseltilir (KSEIcerigi.GabariCiz ile
        /// aynı kural: dönme iç ray üzerinden olduğu için eksen dever/2 yükselir).
        /// </summary>
        public PointD Uygula(PointD nokta, double deverMm, string yon)
        {
            if (deverMm == 0)
                return nokta;

            var aci = Math.Atan(deverMm / 1000.0 / Ekartman);
            if (yon == "ccw")
                aci = -aci;

            var x = nokta.X * Math.Cos(aci) + nokta.Y * Math.Sin(aci);
            var y = -nokta.X * Math.Sin(aci) + nokta.Y * Math.Cos(aci);
            return new PointD(x, y + Math.Abs(deverMm) / 2000.0);
        }
    }

    /// <summary>Dever tablosunun listelendiği ve elle değiştirilebildiği pencere.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class DeverListesiForm : Form
    {
        private readonly DataGridView _tablo = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersWidth = 30,
            SelectionMode = DataGridViewSelectionMode.CellSelect
        };

        /// <summary>Tamam ile kapatıldığında tablodaki (kilometreye göre sıralı) satırlar.</summary>
        public List<DeverSatiri> Satirlar { get; private set; }

        public DeverListesiForm(DeverTablosu deverTablosu, string baslik = "Dever Listesi")
        {
            Text = baslik;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 480);
            MinimumSize = new Size(360, 300);
            MinimizeBox = false;
            ShowInTaskbar = false;

            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kilometre", SortMode = DataGridViewColumnSortMode.NotSortable });
            _tablo.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dever (mm)", SortMode = DataGridViewColumnSortMode.NotSortable });
            var yonKolonu = new DataGridViewComboBoxColumn { HeaderText = "Yön", SortMode = DataGridViewColumnSortMode.NotSortable };
            yonKolonu.Items.AddRange("cw", "ccw");
            _tablo.Columns.Add(yonKolonu);

            foreach (var satir in deverTablosu.Satirlar)
            {
                _tablo.Rows.Add(
                    satir.Km.ToString("0.######", CultureInfo.InvariantCulture),
                    satir.Dever.ToString("0.###", CultureInfo.InvariantCulture),
                    satir.Yon);
            }

            var bilgi = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(6, 4, 6, 0),
                Text = deverTablosu.Satirlar.Count > 0
                    ? "Satırlar arasında dever doğrusal değişir. Yön: cw = sağa, ccw = sola dönen kurp."
                    : "Güzergahta dever tanımlı değil (ya da güzergah seçilmedi). Satırları elle girebilirsiniz."
            };

            var tamamButton = new Button { Text = "Tamam", Size = new Size(80, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var iptalButton = new Button { Text = "İptal", Size = new Size(80, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            tamamButton.Location = new Point(altPanel.Width - 176, 7);
            iptalButton.Location = new Point(altPanel.Width - 90, 7);
            altPanel.Controls.Add(tamamButton);
            altPanel.Controls.Add(iptalButton);
            tamamButton.Click += TamamButton_Click;
            CancelButton = iptalButton;

            Controls.Add(_tablo);
            Controls.Add(bilgi);
            Controls.Add(altPanel);
        }

        private static bool SayiOku(object hucreDegeri, out double sayi)
        {
            var metin = Convert.ToString(hucreDegeri)?.Trim().Replace(',', '.');
            return double.TryParse(metin, NumberStyles.Float, CultureInfo.InvariantCulture, out sayi);
        }

        private void TamamButton_Click(object sender, EventArgs e)
        {
            _tablo.EndEdit();

            var satirlar = new List<DeverSatiri>();
            foreach (DataGridViewRow satir in _tablo.Rows)
            {
                if (satir.IsNewRow)
                    continue;

                var bosSatir = string.IsNullOrWhiteSpace(Convert.ToString(satir.Cells[0].Value))
                               && string.IsNullOrWhiteSpace(Convert.ToString(satir.Cells[1].Value));
                if (bosSatir)
                    continue;

                if (!SayiOku(satir.Cells[0].Value, out var km) || !SayiOku(satir.Cells[1].Value, out var dever) || dever < 0)
                {
                    _tablo.CurrentCell = satir.Cells[0];
                    MessageBox.Show(
                        (satir.Index + 1) + ". satırda kilometre ya da dever geçerli bir sayı değil. Dever negatif olamaz; yönü \"Yön\" sütunundan seçin.",
                        "Dever Listesi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                satirlar.Add(new DeverSatiri { Km = km, Dever = dever, Yon = Convert.ToString(satir.Cells[2].Value) == "ccw" ? "ccw" : "cw" });
            }

            // Aynı kilometredeki satırların (yön değişim noktaları) girilen sırası korunur.
            Satirlar = satirlar.OrderBy(s => s.Km).ToList();
            DialogResult = DialogResult.OK;
        }
    }
}
