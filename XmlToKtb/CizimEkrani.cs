using System;
using System.Windows.Forms;
using XmlToKtb.Cizim;

namespace XmlToKtb
{
    public partial class CizimEkrani : Form
    {
        public CizimEkrani()
        {
            InitializeComponent();
            cizimPaneli1.SecimDegisti += CizimPaneli1_SecimDegisti;
            cizimPaneli1.FareKonumuDegisti += CizimPaneli1_FareKonumuDegisti;
            cizimPaneli1.AracDegisti += CizimPaneli1_AracDegisti;
        }

        private void CizimPaneli1_SecimDegisti(object sender, CizimNesnesi secilenNesne)
        {
            ozellikGrid.SelectedObject = secilenNesne;
        }

        private void CizimPaneli1_FareKonumuDegisti(object sender, System.Drawing.PointF dunyaKonumu)
        {
            konumDurumLabel.Text = $"X: {dunyaKonumu.X:0.000}   Y: {dunyaKonumu.Y:0.000}";
        }

        private void CizimPaneli1_AracDegisti(object sender, CizimAraci araci)
        {
            secAraciButton.Checked = araci == CizimAraci.Sec;
            cizgiAraciButton.Checked = araci == CizimAraci.Cizgi;
        }

        private void secAraciButton_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Sec;
        }

        private void cizgiAraciButton_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Cizgi;
        }

        private void ozellikGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            cizimPaneli1.SeciliNesneGuncellendi();
        }
    }
}
