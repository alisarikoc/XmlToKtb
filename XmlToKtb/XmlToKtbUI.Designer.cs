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
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.donusturButton = new System.Windows.Forms.Button();
            this.isimHaklariLabel = new System.Windows.Forms.Label();
            this.kaydetButton = new System.Windows.Forms.Button();
            this.bilgiLabel = new System.Windows.Forms.Label();
            this.dosyaTuruComboBox = new System.Windows.Forms.ComboBox();
            this.kayitTuruLabel = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.enkestlistesiBox = new System.Windows.Forms.CheckBox();
            this.gabariButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.tipKesitButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.enkesitButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ncnOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.LisopenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "xml";
            this.openFileDialog.Filter = "XML veya KTB (*.xml;*ktb)|*.xml;*.ktb";
            this.openFileDialog.FilterIndex = 0;
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
            // isimHaklariLabel
            // 
            this.isimHaklariLabel.AutoSize = true;
            this.isimHaklariLabel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.isimHaklariLabel.Font = new System.Drawing.Font("Arial", 10F);
            this.isimHaklariLabel.ForeColor = System.Drawing.SystemColors.Highlight;
            this.isimHaklariLabel.Location = new System.Drawing.Point(10, 206);
            this.isimHaklariLabel.Name = "isimHaklariLabel";
            this.isimHaklariLabel.Size = new System.Drawing.Size(107, 16);
            this.isimHaklariLabel.TabIndex = 1;
            this.isimHaklariLabel.Text = "© Burak Kalafat";
            // 
            // kaydetButton
            // 
            this.kaydetButton.Enabled = false;
            this.kaydetButton.Location = new System.Drawing.Point(305, 78);
            this.kaydetButton.Name = "kaydetButton";
            this.kaydetButton.Size = new System.Drawing.Size(93, 31);
            this.kaydetButton.TabIndex = 2;
            this.kaydetButton.Text = "Çıktı Kaydet";
            this.kaydetButton.UseVisualStyleBackColor = true;
            this.kaydetButton.Click += new System.EventHandler(this.kaydet_Tikla);
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
            // dosyaTuruComboBox
            // 
            this.dosyaTuruComboBox.FormattingEnabled = true;
            this.dosyaTuruComboBox.Items.AddRange(new object[] {
            "KTB",
            "ALN ve PRF"});
            this.dosyaTuruComboBox.Location = new System.Drawing.Point(305, 49);
            this.dosyaTuruComboBox.Margin = new System.Windows.Forms.Padding(2);
            this.dosyaTuruComboBox.Name = "dosyaTuruComboBox";
            this.dosyaTuruComboBox.Size = new System.Drawing.Size(92, 21);
            this.dosyaTuruComboBox.TabIndex = 6;
            this.dosyaTuruComboBox.SelectedIndexChanged += new System.EventHandler(this.dosyaTuruComboBox_SelectedIndexChanged);
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
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(428, 191);
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
            this.tabPage1.Size = new System.Drawing.Size(420, 165);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Güzergah";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.enkestlistesiBox);
            this.tabPage2.Controls.Add(this.gabariButton);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.tipKesitButton);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.enkesitButton);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(420, 165);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Kesit";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // enkestlistesiBox
            // 
            this.enkestlistesiBox.AutoSize = true;
            this.enkestlistesiBox.Location = new System.Drawing.Point(321, 18);
            this.enkestlistesiBox.Name = "enkestlistesiBox";
            this.enkestlistesiBox.Size = new System.Drawing.Size(93, 30);
            this.enkestlistesiBox.TabIndex = 11;
            this.enkestlistesiBox.Text = "Enkesit Listesi\r\nOluştur";
            this.enkestlistesiBox.UseVisualStyleBackColor = true;
            // 
            // gabariButton
            // 
            this.gabariButton.Location = new System.Drawing.Point(209, 114);
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
            this.label3.Location = new System.Drawing.Point(9, 120);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(185, 18);
            this.label3.TabIndex = 9;
            this.label3.Text = "Gabariden Enkesit Oluştur:";
            // 
            // tipKesitButton
            // 
            this.tipKesitButton.Location = new System.Drawing.Point(209, 67);
            this.tipKesitButton.Name = "tipKesitButton";
            this.tipKesitButton.Size = new System.Drawing.Size(93, 31);
            this.tipKesitButton.TabIndex = 8;
            this.tipKesitButton.Text = "Başlat";
            this.tipKesitButton.UseVisualStyleBackColor = true;
            this.tipKesitButton.Click += new System.EventHandler(this.tipKesitButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label2.Location = new System.Drawing.Point(9, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(194, 18);
            this.label2.TabIndex = 7;
            this.label2.Text = "Tip Kesitten Enkesit Oluştur:";
            // 
            // enkesitButton
            // 
            this.enkesitButton.Location = new System.Drawing.Point(212, 17);
            this.enkesitButton.Name = "enkesitButton";
            this.enkesitButton.Size = new System.Drawing.Size(93, 31);
            this.enkesitButton.TabIndex = 6;
            this.enkesitButton.Text = "Başlat";
            this.enkesitButton.UseVisualStyleBackColor = true;
            this.enkesitButton.Click += new System.EventHandler(this.enkesitButton_Click);
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
            // ncnOpenFileDialog
            // 
            this.ncnOpenFileDialog.Filter = "NCN veya ASC (*.ncn;*asc)|*.ncn;*.asc";
            // 
            // LisopenFileDialog
            // 
            this.LisopenFileDialog.Filter = "LIS (*.lis)|*.lis";
            // 
            // XmlToKtbUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 248);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.isimHaklariLabel);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(460, 275);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(460, 275);
            this.Name = "XmlToKtbUI";
            this.ShowIcon = false;
            this.Text = "XML to KTB";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
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
        private TabPage tabPage2;
        private Label label1;
        private Button enkesitButton;
        private OpenFileDialog ncnOpenFileDialog;
        private Button tipKesitButton;
        private Label label2;
        private Button gabariButton;
        private Label label3;
        private CheckBox enkestlistesiBox;
        private OpenFileDialog LisopenFileDialog;
    }
}

