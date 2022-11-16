using System.ComponentModel;
using System.Windows.Forms;

namespace XmlToKtb
{
    partial class XmlToKtbUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.TabPage tabPage2;
            this.enkestlistesiBox = new System.Windows.Forms.CheckBox();
            this.gabariButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.tipKesitButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.daire_enkesitButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.bilgiLabel = new System.Windows.Forms.Label();
            this.kayitTuruLabel = new System.Windows.Forms.Label();
            this.dosyaTuruComboBox = new System.Windows.Forms.ComboBox();
            this.donusturButton = new System.Windows.Forms.Button();
            this.kaydetButton = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabllobutton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.Tcddtabllobutton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.isimHaklariLabel = new System.Windows.Forms.Label();
            this.ncnOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.LisopenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.button2 = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();

            this.dxf_button = new System.Windows.Forms.Button();
            this.dxf_label = new System.Windows.Forms.Label();
            tabPage2 = new System.Windows.Forms.TabPage();
            tabPage2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(this.enkestlistesiBox);
            tabPage2.Controls.Add(this.gabariButton);
            tabPage2.Controls.Add(this.label3);
            tabPage2.Controls.Add(this.button1);
            tabPage2.Controls.Add(this.tipKesitButton);
            tabPage2.Controls.Add(this.label4);
            tabPage2.Controls.Add(this.label2);
            tabPage2.Controls.Add(this.daire_enkesitButton);
            tabPage2.Controls.Add(this.label1);
            tabPage2.Location = new System.Drawing.Point(4, 22);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(3);
            tabPage2.Size = new System.Drawing.Size(440, 207);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Kesit";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // enkestlistesiBox
            // 
            this.enkestlistesiBox.AutoSize = true;
            this.enkestlistesiBox.Location = new System.Drawing.Point(344, 17);
            this.enkestlistesiBox.Name = "enkestlistesiBox";
            this.enkestlistesiBox.Size = new System.Drawing.Size(93, 30);
            this.enkestlistesiBox.TabIndex = 11;
            this.enkestlistesiBox.Text = "Enkesit Listesi\r\nOluştur";
            this.enkestlistesiBox.UseVisualStyleBackColor = true;
            // 
            // gabariButton
            // 
            this.gabariButton.Location = new System.Drawing.Point(245, 159);
            this.gabariButton.Name = "gabariButton";
            this.gabariButton.Size = new System.Drawing.Size(93, 31);
            this.gabariButton.TabIndex = 10;
            this.gabariButton.Text = "Başlat";
            this.gabariButton.UseVisualStyleBackColor = true;
            this.gabariButton.Click += new System.EventHandler(this.gabariButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label3.Location = new System.Drawing.Point(12, 165);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(185, 18);
            this.label3.TabIndex = 9;
            this.label3.Text = "Gabariden Enkesit Oluştur:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(245, 16);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(93, 31);
            this.button1.TabIndex = 8;
            this.button1.Text = "Başlat";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.enkesitButton_Click);
            // 
            // tipKesitButton
            // 
            this.tipKesitButton.Location = new System.Drawing.Point(245, 112);
            this.tipKesitButton.Name = "tipKesitButton";
            this.tipKesitButton.Size = new System.Drawing.Size(93, 31);
            this.tipKesitButton.TabIndex = 8;
            this.tipKesitButton.Text = "Başlat";
            this.tipKesitButton.UseVisualStyleBackColor = true;
            this.tipKesitButton.Click += new System.EventHandler(this.tipKesitButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label4.Location = new System.Drawing.Point(12, 74);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(222, 18);
            this.label4.TabIndex = 7;
            this.label4.Text = "Noktal. Dairesel Enkesit Oluştur:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label2.Location = new System.Drawing.Point(12, 118);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(194, 18);
            this.label2.TabIndex = 7;
            this.label2.Text = "Tip Kesitten Enkesit Oluştur:";
            // 
            // daire_enkesitButton
            // 
            this.daire_enkesitButton.Location = new System.Drawing.Point(245, 69);
            this.daire_enkesitButton.Name = "daire_enkesitButton";
            this.daire_enkesitButton.Size = new System.Drawing.Size(93, 31);
            this.daire_enkesitButton.TabIndex = 6;
            this.daire_enkesitButton.Text = "Başlat";
            this.daire_enkesitButton.UseVisualStyleBackColor = true;
            this.daire_enkesitButton.Click += new System.EventHandler(this.daire_enkesitButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label1.Location = new System.Drawing.Point(9, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(197, 18);
            this.label1.TabIndex = 4;
            this.label1.Text = "Noktalardan Enkesit Oluştur:";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(448, 233);
            this.tabControl1.TabIndex = 8;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.bilgiLabel);
            this.tabPage1.Controls.Add(this.kayitTuruLabel);
            this.tabPage1.Controls.Add(this.dosyaTuruComboBox);
            this.tabPage1.Controls.Add(this.donusturButton);
            this.tabPage1.Controls.Add(this.kaydetButton);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(440, 207);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Güzergah";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // bilgiLabel
            // 
            this.bilgiLabel.AutoSize = true;
            this.bilgiLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.bilgiLabel.Location = new System.Drawing.Point(3, 3);
            this.bilgiLabel.Name = "bilgiLabel";
            this.bilgiLabel.Size = new System.Drawing.Size(151, 18);
            this.bilgiLabel.TabIndex = 3;
            this.bilgiLabel.Text = "Lütfen Dosya Seçiniz.";
            // 
            // kayitTuruLabel
            // 
            this.kayitTuruLabel.AutoSize = true;
            this.kayitTuruLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.kayitTuruLabel.Location = new System.Drawing.Point(190, 52);
            this.kayitTuruLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.kayitTuruLabel.Name = "kayitTuruLabel";
            this.kayitTuruLabel.Size = new System.Drawing.Size(106, 13);
            this.kayitTuruLabel.TabIndex = 7;
            this.kayitTuruLabel.Text = "Kaydedilecek Tür";
            this.kayitTuruLabel.Visible = false;
            // 
            // dosyaTuruComboBox
            // 
            this.dosyaTuruComboBox.FormattingEnabled = true;
            this.dosyaTuruComboBox.Items.AddRange(new object[] {
            "KTB",
            "ALN ve PRF",
            "TBL"});
            this.dosyaTuruComboBox.Location = new System.Drawing.Point(305, 49);
            this.dosyaTuruComboBox.Margin = new System.Windows.Forms.Padding(2);
            this.dosyaTuruComboBox.Name = "dosyaTuruComboBox";
            this.dosyaTuruComboBox.Size = new System.Drawing.Size(92, 21);
            this.dosyaTuruComboBox.TabIndex = 6;
            this.dosyaTuruComboBox.SelectedIndexChanged += new System.EventHandler(this.dosyaTuruComboBox_SelectedIndexChanged);
            // 
            // donusturButton
            // 
            this.donusturButton.Location = new System.Drawing.Point(6, 24);
            this.donusturButton.Name = "donusturButton";
            this.donusturButton.Size = new System.Drawing.Size(93, 31);
            this.donusturButton.TabIndex = 0;
            this.donusturButton.Text = "Dosya Seç";
            this.donusturButton.UseVisualStyleBackColor = true;
            this.donusturButton.Click += new System.EventHandler(this.xmlDonustur_Tikla);
            // 
            // kaydetButton
            // 
            this.kaydetButton.Enabled = false;
            this.kaydetButton.Location = new System.Drawing.Point(305, 78);
            this.kaydetButton.Name = "kaydetButton";
            this.kaydetButton.Size = new System.Drawing.Size(93, 31);
            this.kaydetButton.TabIndex = 2;
            this.kaydetButton.Text = "Dönüştür";
            this.kaydetButton.UseVisualStyleBackColor = true;
            this.kaydetButton.Click += new System.EventHandler(this.kaydet_Tikla);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.button2);
            this.tabPage3.Controls.Add(this.tabllobutton);
            this.tabPage3.Controls.Add(this.label7);
            this.tabPage3.Controls.Add(this.label5);
            this.tabPage3.Controls.Add(this.Tcddtabllobutton);
            this.tabPage3.Controls.Add(this.label6);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(440, 207);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Tablolar";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabllobutton
            // 
            this.tabllobutton.Location = new System.Drawing.Point(245, 72);
            this.tabllobutton.Name = "tabllobutton";
            this.tabllobutton.Size = new System.Drawing.Size(93, 31);
            this.tabllobutton.TabIndex = 10;
            this.tabllobutton.Text = "Tablo Olustur";
            this.tabllobutton.UseVisualStyleBackColor = true;
            this.tabllobutton.Click += new System.EventHandler(this.guzerhah_tablo_click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label5.Location = new System.Drawing.Point(50, 77);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(138, 18);
            this.label5.TabIndex = 9;
            this.label5.Text = "TCDD Hat El Kitabı:";
            // 
            // Tcddtabllobutton
            // 
            this.Tcddtabllobutton.Location = new System.Drawing.Point(245, 23);
            this.Tcddtabllobutton.Name = "Tcddtabllobutton";
            this.Tcddtabllobutton.Size = new System.Drawing.Size(93, 31);
            this.Tcddtabllobutton.TabIndex = 10;
            this.Tcddtabllobutton.Text = "Tablo Olustur";
            this.Tcddtabllobutton.UseVisualStyleBackColor = true;
            this.Tcddtabllobutton.Click += new System.EventHandler(this.tcddteknik_tablo_click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label6.Location = new System.Drawing.Point(50, 28);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(159, 18);
            this.label6.TabIndex = 9;
            this.label6.Text = "TCDD Teknik Tablosu:";
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "xml";
            this.openFileDialog.Filter = "XML veya KTB (*.xml;*ktb)|*.xml;*.ktb";
            this.openFileDialog.FilterIndex = 0;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.dxf_button);
            this.tabPage4.Controls.Add(this.dxf_label);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(440, 207);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Dxf";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // isimHaklariLabel
            // 
            this.isimHaklariLabel.AutoSize = true;
            this.isimHaklariLabel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.isimHaklariLabel.Font = new System.Drawing.Font("Arial", 10F);
            this.isimHaklariLabel.ForeColor = System.Drawing.SystemColors.Highlight;
            this.isimHaklariLabel.Location = new System.Drawing.Point(9, 248);
            this.isimHaklariLabel.Name = "isimHaklariLabel";
            this.isimHaklariLabel.Size = new System.Drawing.Size(195, 16);
            this.isimHaklariLabel.TabIndex = 1;
            this.isimHaklariLabel.Text = "© Burak Kalafat ve Ali Sarıkoç";
            // 
            // ncnOpenFileDialog
            // 
            this.ncnOpenFileDialog.Filter = "NCN veya ASC (*.ncn;*asc)|*.ncn;*.asc";
            // 
            // LisopenFileDialog
            // 
            this.LisopenFileDialog.Filter = "TBL (*.tbl)|*.tbl";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(245, 126);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(93, 31);
            this.button2.TabIndex = 10;
            this.button2.Text = "XML Olustur";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.elKitabiXML_click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label7.Location = new System.Drawing.Point(50, 131);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(175, 18);
            this.label7.TabIndex = 9;
            this.label7.Text = "El Kitabı İçin XML oluştur:";
            // 
            // dxf_button
            // 
            this.dxf_button.Location = new System.Drawing.Point(245, 126);
            this.dxf_button.Name = "dxf_button";
            this.dxf_button.Size = new System.Drawing.Size(93, 31);
            this.dxf_button.TabIndex = 10;
            this.dxf_button.Text = "XML Olustur";
            this.dxf_button.UseVisualStyleBackColor = true;
            this.dxf_button.Click += new System.EventHandler(this.dxftoXML_click);
            // 
            // dxf_label
            // 
            this.dxf_label.AutoSize = true;
            this.dxf_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.dxf_label.Location = new System.Drawing.Point(50, 131);
            this.dxf_label.Name = "dxf_label";
            this.dxf_label.Size = new System.Drawing.Size(175, 18);
            this.dxf_label.TabIndex = 9;
            this.dxf_label.Text = "Dxf den XML oluştur:";
            // 
            // XmlToKtbUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 261);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.isimHaklariLabel);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(480, 300);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(480, 300);
            this.Name = "XmlToKtbUI";
            this.ShowIcon = false;
            this.Text = "XML to KTB";
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private OpenFileDialog openFileDialog;
        private Button donusturButton;
        private Label isimHaklariLabel;
        private Button kaydetButton;
        private Label bilgiLabel;
        private ComboBox dosyaTuruComboBox;
        private Label kayitTuruLabel;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private Label label1;
        private Button daire_enkesitButton;
        private OpenFileDialog ncnOpenFileDialog;
        private Button tipKesitButton;
        private Label label2;
        private Button gabariButton;

        public Button tabllobutton;
        public Button Tcddtabllobutton;

        private Label label3;

        public Label label5;
        public Label label6;

        private CheckBox enkestlistesiBox;
        private OpenFileDialog LisopenFileDialog;
        private Label label4;
        private Button button1;
        private TabPage tabPage3;
        public Button button2;
        public Label label7;


        private TabPage tabPage4;

        public Button dxf_button;
        public Label dxf_label;
    }
}

