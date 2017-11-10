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
            this.openXmlButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // xmlOpenFileDialog
            // 
            this.xmlOpenFileDialog.FileName = "xmlOpenFileDialog";
            // 
            // openXmlButton
            // 
            this.openXmlButton.Location = new System.Drawing.Point(119, 41);
            this.openXmlButton.Name = "openXmlButton";
            this.openXmlButton.Size = new System.Drawing.Size(100, 23);
            this.openXmlButton.TabIndex = 0;
            this.openXmlButton.Text = "Seç Ve Kaydet";
            this.openXmlButton.UseVisualStyleBackColor = true;
            this.openXmlButton.Click += new System.EventHandler(this.openLandXmlButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label1.Font = new System.Drawing.Font("Arial", 10F);
            this.label1.ForeColor = System.Drawing.SystemColors.Highlight;
            this.label1.Location = new System.Drawing.Point(112, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "© Burak Kalafat";
            // 
            // XmlToKtbUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(231, 92);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.openXmlButton);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(247, 131);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(247, 131);
            this.Name = "XmlToKtbUI";
            this.ShowIcon = false;
            this.Text = "XML to KTB";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog xmlOpenFileDialog;
        private System.Windows.Forms.Button openXmlButton;
        private System.Windows.Forms.Label label1;
    }
}

