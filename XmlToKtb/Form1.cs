using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XmlToKtb
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public DataTable goster(DataTable tablo)
        {

            dataGridView1.DataSource = tablo;
            this.ShowDialog();

            if (DvrKaydet.DialogResult != DialogResult.OK)
            {
                return tablo;
            }

           


            return tablo;
        }
        private List<Array> DeverdosyasiYukle()
        {
            OpenFileDialog file = new OpenFileDialog();
            file.DefaultExt = "dvr";
            file.Title = "Dever için DVR Dosyası Seçiniz..(Boşlokla Ayrılmış)";
            file.Filter = "DVR Dosyası |*.dvr";
            if (file.ShowDialog() == DialogResult.OK)
            {
                file.FilterIndex = 0;
            }
            StreamReader DvrIcerigi = new StreamReader(file.FileName);
            List<Array> kmSatirlar = new List<Array>();

            while (!DvrIcerigi.EndOfStream)
            {
                char[] separator = { ' ', '\t' };
                var veriSatir = DvrIcerigi.ReadLine();
                if (veriSatir != null)
                {
                    var veriKelimeleri = veriSatir.Split(separator, (StringSplitOptions) 1);
                    kmSatirlar.Add(veriKelimeleri);
                }
            }
            return kmSatirlar;
        }

        public DataTable DvrYukle_Click(DataTable table)
        {
            
            dataGridView1.DataSource = table;
            return table;
        }

        private void DvrYukle_Click(object sender, EventArgs e)
        {
            var deverler = DeverdosyasiYukle();
            var table=(DataTable) dataGridView1.DataSource;
            table.Clear();
            if (deverler != null)
            {
                foreach (var satir in deverler)
                {
                    DataRow drow = table.NewRow();
                    drow["Kilometre"] = Convert.ToDecimal(satir.GetValue(0));
                    drow["Uygulama Dever"] = Convert.ToDecimal(satir.GetValue(1));
                    string yon = Convert.ToString(satir.GetValue(2));
                    if (yon == "SAG" || yon == "SAĞ") yon = "cw";
                    else
                    {
                        yon = "ccw";
                    }

                    drow["Yön"] = yon;
                    table.Rows.Add(drow);
                }
            }
        }
    }
}
