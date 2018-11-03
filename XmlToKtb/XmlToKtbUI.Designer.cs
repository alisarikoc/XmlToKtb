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
            this.xmlOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.donusturButton = new System.Windows.Forms.Button();
            this.isimHaklariLabel = new System.Windows.Forms.Label();
            this.kaydetButton = new System.Windows.Forms.Button();
            this.bilgiLabel = new System.Windows.Forms.Label();
            this.dosyaTuruComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // xmlOpenFileDialog
            // 
            this.xmlOpenFileDialog.DefaultExt = "xml";
            this.xmlOpenFileDialog.Filter = "XML Files (*.xml)|*.xml";
            this.xmlOpenFileDialog.FilterIndex = 0;
            // 
            // donusturButton
            // 
            this.donusturButton.Location = new System.Drawing.Point(439, 151);
            this.donusturButton.Margin = new System.Windows.Forms.Padding(4);
            this.donusturButton.Name = "donusturButton";
            this.donusturButton.Size = new System.Drawing.Size(124, 38);
            this.donusturButton.TabIndex = 0;
            this.donusturButton.Text = "KTB Dönüştür";
            this.donusturButton.UseVisualStyleBackColor = true;
            this.donusturButton.Click += new System.EventHandler(this.xmlDonustur_Tikla);
            // 
            // isimHaklariLabel
            // 
            this.isimHaklariLabel.AutoSize = true;
            this.isimHaklariLabel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.isimHaklariLabel.Font = new System.Drawing.Font("Arial", 10F);
            this.isimHaklariLabel.ForeColor = System.Drawing.SystemColors.Highlight;
            this.isimHaklariLabel.Location = new System.Drawing.Point(16, 263);
            this.isimHaklariLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.isimHaklariLabel.Name = "isimHaklariLabel";
            this.isimHaklariLabel.Size = new System.Drawing.Size(125, 19);
            this.isimHaklariLabel.TabIndex = 1;
            this.isimHaklariLabel.Text = "© Burak Kalafat";
            // 
            // kaydetButton
            // 
            this.kaydetButton.Enabled = false;
            this.kaydetButton.Location = new System.Drawing.Point(439, 210);
            this.kaydetButton.Margin = new System.Windows.Forms.Padding(4);
            this.kaydetButton.Name = "kaydetButton";
            this.kaydetButton.Size = new System.Drawing.Size(124, 38);
            this.kaydetButton.TabIndex = 2;
            this.kaydetButton.Text = "KTB Kaydet";
            this.kaydetButton.UseVisualStyleBackColor = true;
            this.kaydetButton.Click += new System.EventHandler(this.kaydet_Tikla);
            // 
            // bilgiLabel
            // 
            this.bilgiLabel.AutoSize = true;
            this.bilgiLabel.Location = new System.Drawing.Point(16, 11);
            this.bilgiLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.bilgiLabel.Name = "bilgiLabel";
            this.bilgiLabel.Size = new System.Drawing.Size(182, 17);
            this.bilgiLabel.TabIndex = 3;
            this.bilgiLabel.Text = "Lütfen Xml Dosyası Seçiniz.";
            // 
            // dosyaTuruComboBox
            // 
            this.dosyaTuruComboBox.FormattingEnabled = true;
            this.dosyaTuruComboBox.Items.AddRange(new object[] {
            "KTB",
            "ALN ve PRF"});
            this.dosyaTuruComboBox.Location = new System.Drawing.Point(19, 47);
            this.dosyaTuruComboBox.Name = "dosyaTuruComboBox";
            this.dosyaTuruComboBox.Size = new System.Drawing.Size(121, 24);
            this.dosyaTuruComboBox.TabIndex = 6;
            this.dosyaTuruComboBox.SelectedIndexChanged += new System.EventHandler(this.dosyaTuruComboBox_SelectedIndexChanged);
            // 
            // XmlToKtbUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 282);
            this.Controls.Add(this.dosyaTuruComboBox);
            this.Controls.Add(this.bilgiLabel);
            this.Controls.Add(this.kaydetButton);
            this.Controls.Add(this.isimHaklariLabel);
            this.Controls.Add(this.donusturButton);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(594, 329);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(594, 329);
            this.Name = "XmlToKtbUI";
            this.ShowIcon = false;
            this.Text = "XML to KTB";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog xmlOpenFileDialog;
        private System.Windows.Forms.Button donusturButton;
        private System.Windows.Forms.Label isimHaklariLabel;
        private System.Windows.Forms.Button kaydetButton;
        private System.Windows.Forms.Label bilgiLabel;
        private System.Windows.Forms.ComboBox dosyaTuruComboBox;
    }
}

