namespace XmlToKtb
{
    partial class KoordinatSistemiForm
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
            this.sistemLabel = new System.Windows.Forms.Label();
            this.sistemCombo = new System.Windows.Forms.ComboBox();
            this.dilimLabel = new System.Windows.Forms.Label();
            this.dilimCombo = new System.Windows.Forms.ComboBox();
            this.tamamButton = new System.Windows.Forms.Button();
            this.iptalButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // sistemLabel
            //
            this.sistemLabel.AutoSize = true;
            this.sistemLabel.Location = new System.Drawing.Point(12, 15);
            this.sistemLabel.Name = "sistemLabel";
            this.sistemLabel.Size = new System.Drawing.Size(45, 13);
            this.sistemLabel.TabIndex = 0;
            this.sistemLabel.Text = "Sistem:";
            //
            // sistemCombo
            //
            this.sistemCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sistemCombo.FormattingEnabled = true;
            this.sistemCombo.Location = new System.Drawing.Point(90, 12);
            this.sistemCombo.Name = "sistemCombo";
            this.sistemCombo.Size = new System.Drawing.Size(330, 21);
            this.sistemCombo.TabIndex = 1;
            this.sistemCombo.SelectedIndexChanged += new System.EventHandler(this.sistemCombo_SelectedIndexChanged);
            //
            // dilimLabel
            //
            this.dilimLabel.AutoSize = true;
            this.dilimLabel.Location = new System.Drawing.Point(12, 45);
            this.dilimLabel.Name = "dilimLabel";
            this.dilimLabel.Size = new System.Drawing.Size(35, 13);
            this.dilimLabel.TabIndex = 2;
            this.dilimLabel.Text = "Dilim:";
            //
            // dilimCombo
            //
            this.dilimCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dilimCombo.FormattingEnabled = true;
            this.dilimCombo.Location = new System.Drawing.Point(90, 42);
            this.dilimCombo.Name = "dilimCombo";
            this.dilimCombo.Size = new System.Drawing.Size(330, 21);
            this.dilimCombo.TabIndex = 3;
            //
            // tamamButton
            //
            this.tamamButton.Location = new System.Drawing.Point(264, 78);
            this.tamamButton.Name = "tamamButton";
            this.tamamButton.Size = new System.Drawing.Size(75, 27);
            this.tamamButton.TabIndex = 4;
            this.tamamButton.Text = "Tamam";
            this.tamamButton.UseVisualStyleBackColor = true;
            this.tamamButton.Click += new System.EventHandler(this.tamamButton_Click);
            //
            // iptalButton
            //
            this.iptalButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.iptalButton.Location = new System.Drawing.Point(345, 78);
            this.iptalButton.Name = "iptalButton";
            this.iptalButton.Size = new System.Drawing.Size(75, 27);
            this.iptalButton.TabIndex = 5;
            this.iptalButton.Text = "İptal";
            this.iptalButton.UseVisualStyleBackColor = true;
            //
            // KoordinatSistemiForm
            //
            this.AcceptButton = this.tamamButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.iptalButton;
            this.ClientSize = new System.Drawing.Size(432, 117);
            this.Controls.Add(this.iptalButton);
            this.Controls.Add(this.tamamButton);
            this.Controls.Add(this.dilimCombo);
            this.Controls.Add(this.dilimLabel);
            this.Controls.Add(this.sistemCombo);
            this.Controls.Add(this.sistemLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "KoordinatSistemiForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Koordinat Sistemi";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label sistemLabel;
        private System.Windows.Forms.ComboBox sistemCombo;
        private System.Windows.Forms.Label dilimLabel;
        private System.Windows.Forms.ComboBox dilimCombo;
        private System.Windows.Forms.Button tamamButton;
        private System.Windows.Forms.Button iptalButton;
    }
}
