namespace XmlToKtb
{
    partial class OlcekFaktoruForm
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
            this.carpanLabel = new System.Windows.Forms.Label();
            this.carpanTextBox = new System.Windows.Forms.TextBox();
            this.tamamButton = new System.Windows.Forms.Button();
            this.iptalButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // carpanLabel
            //
            this.carpanLabel.AutoSize = true;
            this.carpanLabel.Location = new System.Drawing.Point(12, 15);
            this.carpanLabel.Name = "carpanLabel";
            this.carpanLabel.Size = new System.Drawing.Size(102, 13);
            this.carpanLabel.TabIndex = 0;
            this.carpanLabel.Text = "Ölçek Çarpanı:";
            //
            // carpanTextBox
            //
            this.carpanTextBox.Location = new System.Drawing.Point(120, 12);
            this.carpanTextBox.Name = "carpanTextBox";
            this.carpanTextBox.Size = new System.Drawing.Size(100, 20);
            this.carpanTextBox.TabIndex = 1;
            this.carpanTextBox.Text = "1";
            //
            // tamamButton
            //
            this.tamamButton.Location = new System.Drawing.Point(64, 48);
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
            this.iptalButton.Location = new System.Drawing.Point(145, 48);
            this.iptalButton.Name = "iptalButton";
            this.iptalButton.Size = new System.Drawing.Size(75, 27);
            this.iptalButton.TabIndex = 3;
            this.iptalButton.Text = "İptal";
            this.iptalButton.UseVisualStyleBackColor = true;
            //
            // OlcekFaktoruForm
            //
            this.AcceptButton = this.tamamButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.iptalButton;
            this.ClientSize = new System.Drawing.Size(234, 87);
            this.Controls.Add(this.iptalButton);
            this.Controls.Add(this.tamamButton);
            this.Controls.Add(this.carpanTextBox);
            this.Controls.Add(this.carpanLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OlcekFaktoruForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Ölçekle";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label carpanLabel;
        private System.Windows.Forms.TextBox carpanTextBox;
        private System.Windows.Forms.Button tamamButton;
        private System.Windows.Forms.Button iptalButton;
    }
}
