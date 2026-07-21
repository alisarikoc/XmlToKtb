using System;
using System.Globalization;
using System.Windows.Forms;

namespace XmlToKtb
{
    /// <summary>Tek bir pozitif sayı istenen genel amaçlı giriş diyaloğu (ölçek çarpanı, yarıçap vb.).</summary>
    public partial class OlcekFaktoruForm : Form
    {
        public double Deger { get; private set; } = 1.0;

        public OlcekFaktoruForm() : this("Ölçekle", "Ölçek Çarpanı:", 1.0)
        {
        }

        public OlcekFaktoruForm(string baslik, string etiket, double baslangicDegeri)
        {
            InitializeComponent();
            Text = baslik;
            carpanLabel.Text = etiket;
            carpanTextBox.Text = baslangicDegeri.ToString(CultureInfo.InvariantCulture);
            carpanTextBox.SelectAll();
        }

        private void tamamButton_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(carpanTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var deger)
                && !double.TryParse(carpanTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out deger))
            {
                MessageBox.Show("Lütfen geçerli bir sayı girin (ör. 0.5 veya 2).", "Geçersiz Değer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (deger <= 0)
            {
                MessageBox.Show("Değer sıfırdan büyük olmalıdır.", "Geçersiz Değer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Deger = deger;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
