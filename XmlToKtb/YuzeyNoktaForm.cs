using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace XmlToKtb
{
    /// <summary>Yüzey noktasının numarasının ve Y, X, Z değerlerinin girildiği/düzeltildiği pencere.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class YuzeyNoktaForm : Form
    {
        private readonly TextBox _noTextBox;
        private readonly TextBox _yTextBox;
        private readonly TextBox _xTextBox;
        private readonly TextBox _zTextBox;

        public string No { get; private set; }
        /// <summary>Sağa değer (NCN'deki Y).</summary>
        public double Y { get; private set; }
        /// <summary>Yukarı değer (NCN'deki X).</summary>
        public double X { get; private set; }
        public double Z { get; private set; }

        public YuzeyNoktaForm(string baslik, string no, double y, double x, double z)
        {
            Text = baslik;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(280, 170);

            _noTextBox = SatirEkle("Nokta No:", no, 12);
            _yTextBox = SatirEkle("Y (sağa):", y.ToString("0.###", CultureInfo.InvariantCulture), 42);
            _xTextBox = SatirEkle("X (yukarı):", x.ToString("0.###", CultureInfo.InvariantCulture), 72);
            _zTextBox = SatirEkle("Z (kot):", z.ToString("0.###", CultureInfo.InvariantCulture), 102);

            var tamamButton = new Button { Text = "Tamam", Location = new Point(112, 136), Size = new Size(75, 26) };
            var iptalButton = new Button { Text = "İptal", Location = new Point(193, 136), Size = new Size(75, 26), DialogResult = DialogResult.Cancel };
            tamamButton.Click += TamamButton_Click;
            Controls.Add(tamamButton);
            Controls.Add(iptalButton);
            AcceptButton = tamamButton;
            CancelButton = iptalButton;

            // Çoğunlukla yalnızca kot değiştirilir; pencere açıldığında imleç orada başlar.
            Shown += (s, e) =>
            {
                _zTextBox.Focus();
                _zTextBox.SelectAll();
            };
        }

        private TextBox SatirEkle(string etiket, string deger, int ust)
        {
            Controls.Add(new Label { Text = etiket, Location = new Point(12, ust + 3), AutoSize = true });
            var kutu = new TextBox { Text = deger, Location = new Point(100, ust), Size = new Size(168, 20) };
            Controls.Add(kutu);
            return kutu;
        }

        private static bool SayiOku(string metin, out double sayi)
        {
            return double.TryParse(metin.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out sayi);
        }

        private void TamamButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_noTextBox.Text) || !SayiOku(_yTextBox.Text, out var y) || !SayiOku(_xTextBox.Text, out var x) || !SayiOku(_zTextBox.Text, out var z))
            {
                MessageBox.Show("Nokta numarası boş olamaz; Y, X ve Z geçerli birer sayı olmalıdır.", "Geçersiz Değer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            No = _noTextBox.Text.Trim();
            Y = y;
            X = x;
            Z = z;
            DialogResult = DialogResult.OK;
        }
    }
}
