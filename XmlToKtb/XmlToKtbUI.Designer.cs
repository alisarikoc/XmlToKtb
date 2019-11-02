namespace XmlToKtb
{
    partial class XmlToKtbUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.enkesitButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ncnOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.tipKesitButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
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
            this.tabControl1.Size = new System.Drawing.Size(418, 191);
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
            this.tabPage1.Size = new System.Drawing.Size(410, 165);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Güzergah";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.tipKesitButton);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.enkesitButton);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(410, 165);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Kesit";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // enkesitButton
            // 
            this.enkesitButton.Location = new System.Drawing.Point(209, 16);
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
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(197, 18);
            this.label1.TabIndex = 4;
            this.label1.Text = "Noktalardan Enkesit Oluştur:";
            // 
            // ncnOpenFileDialog
            // 
            this.ncnOpenFileDialog.Filter = "NCN veya ASC (*.ncn;*asc)|*.ncn;*.asc";
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
            this.label2.Location = new System.Drawing.Point(6, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(194, 18);
            this.label2.TabIndex = 7;
            this.label2.Text = "Tip Kesitten Enkesit Oluştur:";
            // 
            // XmlToKtbUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(442, 248);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.isimHaklariLabel);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(450, 275);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(450, 275);
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

        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button donusturButton;
        private System.Windows.Forms.Label isimHaklariLabel;
        private System.Windows.Forms.Button kaydetButton;
        private System.Windows.Forms.Label bilgiLabel;
        private System.Windows.Forms.ComboBox dosyaTuruComboBox;
        private System.Windows.Forms.Label kayitTuruLabel;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button enkesitButton;
        private System.Windows.Forms.OpenFileDialog ncnOpenFileDialog;
        private System.Windows.Forms.Button tipKesitButton;
        private System.Windows.Forms.Label label2;
    }
}

