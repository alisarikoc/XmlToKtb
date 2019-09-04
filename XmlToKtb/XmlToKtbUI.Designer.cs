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
            this.donusturButton.Location = new System.Drawing.Point(35, 56);
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
            this.kaydetButton.Location = new System.Drawing.Point(329, 188);
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
            this.bilgiLabel.Location = new System.Drawing.Point(32, 35);
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
            this.dosyaTuruComboBox.Location = new System.Drawing.Point(329, 159);
            this.dosyaTuruComboBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dosyaTuruComboBox.Name = "dosyaTuruComboBox";
            this.dosyaTuruComboBox.Size = new System.Drawing.Size(92, 21);
            this.dosyaTuruComboBox.TabIndex = 6;
            this.dosyaTuruComboBox.Visible = true;
            this.dosyaTuruComboBox.SelectedIndexChanged += new System.EventHandler(this.dosyaTuruComboBox_SelectedIndexChanged);
            // 
            // kayitTuruLabel
            // 
            this.kayitTuruLabel.AutoSize = true;
            this.kayitTuruLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.kayitTuruLabel.Location = new System.Drawing.Point(214, 162);
            this.kayitTuruLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.kayitTuruLabel.Name = "kayitTuruLabel";
            this.kayitTuruLabel.Size = new System.Drawing.Size(106, 13);
            this.kayitTuruLabel.TabIndex = 7;
            this.kayitTuruLabel.Text = "Kaydedilecek Tür";
            this.kayitTuruLabel.Visible = false;
            // 
            // XmlToKtbUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 236);
            this.Controls.Add(this.kayitTuruLabel);
            this.Controls.Add(this.dosyaTuruComboBox);
            this.Controls.Add(this.bilgiLabel);
            this.Controls.Add(this.kaydetButton);
            this.Controls.Add(this.isimHaklariLabel);
            this.Controls.Add(this.donusturButton);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(450, 275);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(450, 275);
            this.Name = "XmlToKtbUI";
            this.ShowIcon = false;
            this.Text = "XML to KTB";
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
    }
}

