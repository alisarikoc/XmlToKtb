namespace XmlToKtb
{
    partial class GorunumAyarlariForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Tasarımcısı Tarafından Oluşturulan Kod

        private void InitializeComponent()
        {
            this.basamakLabel = new System.Windows.Forms.Label();
            this.basamakNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.tamamButton = new System.Windows.Forms.Button();
            this.iptalButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.basamakNumericUpDown)).BeginInit();
            this.SuspendLayout();
            //
            // basamakLabel
            //
            this.basamakLabel.AutoSize = true;
            this.basamakLabel.Location = new System.Drawing.Point(12, 17);
            this.basamakLabel.Name = "basamakLabel";
            this.basamakLabel.Size = new System.Drawing.Size(160, 13);
            this.basamakLabel.TabIndex = 0;
            this.basamakLabel.Text = "Ondalık Basamak Sayısı:";
            //
            // basamakNumericUpDown
            //
            this.basamakNumericUpDown.Location = new System.Drawing.Point(178, 15);
            this.basamakNumericUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            this.basamakNumericUpDown.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.basamakNumericUpDown.Name = "basamakNumericUpDown";
            this.basamakNumericUpDown.Size = new System.Drawing.Size(60, 20);
            this.basamakNumericUpDown.TabIndex = 1;
            this.basamakNumericUpDown.Value = new decimal(new int[] { 3, 0, 0, 0 });
            //
            // tamamButton
            //
            this.tamamButton.Location = new System.Drawing.Point(82, 51);
            this.tamamButton.Name = "tamamButton";
            this.tamamButton.Size = new System.Drawing.Size(75, 27);
            this.tamamButton.TabIndex = 2;
            this.tamamButton.Text = "Tamam";
            this.tamamButton.UseVisualStyleBackColor = true;
            this.tamamButton.Click += new System.EventHandler(this.tamamButton_Click);
            //
            // iptalButton
            //
            this.iptalButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.iptalButton.Location = new System.Drawing.Point(163, 51);
            this.iptalButton.Name = "iptalButton";
            this.iptalButton.Size = new System.Drawing.Size(75, 27);
            this.iptalButton.TabIndex = 3;
            this.iptalButton.Text = "İptal";
            this.iptalButton.UseVisualStyleBackColor = true;
            //
            // GorunumAyarlariForm
            //
            this.AcceptButton = this.tamamButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.iptalButton;
            this.ClientSize = new System.Drawing.Size(250, 90);
            this.Controls.Add(this.iptalButton);
            this.Controls.Add(this.tamamButton);
            this.Controls.Add(this.basamakNumericUpDown);
            this.Controls.Add(this.basamakLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GorunumAyarlariForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Görünüm Ayarları";
            ((System.ComponentModel.ISupportInitialize)(this.basamakNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label basamakLabel;
        private System.Windows.Forms.NumericUpDown basamakNumericUpDown;
        private System.Windows.Forms.Button tamamButton;
        private System.Windows.Forms.Button iptalButton;
    }
}
