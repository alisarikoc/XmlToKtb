using System;
using System.Windows.Forms;

namespace XmlToKtb
{
    /// <summary>Özellikler panelindeki sayısal değerlerin gösterim (ondalık basamak) hassasiyetini ayarlamak için diyalog.</summary>
    public partial class GorunumAyarlariForm : Form
    {
        public int OndalikBasamakSayisi => (int)basamakNumericUpDown.Value;

        public GorunumAyarlariForm(int baslangicDegeri)
        {
            InitializeComponent();
            basamakNumericUpDown.Value = Math.Max(basamakNumericUpDown.Minimum, Math.Min(basamakNumericUpDown.Maximum, baslangicDegeri));
        }

        private void tamamButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
