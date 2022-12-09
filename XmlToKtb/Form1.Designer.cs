
using System.Data;

namespace XmlToKtb
{
    partial class Form1
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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.DvrKaydet = new System.Windows.Forms.Button();
            this.DvrYukle = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(49, 35);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(517, 388);
            this.dataGridView1.TabIndex = 0;
            // 
            // DvrKaydet
            // 
            this.DvrKaydet.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.DvrKaydet.Location = new System.Drawing.Point(624, 35);
            this.DvrKaydet.Name = "DvrKaydet";
            this.DvrKaydet.Size = new System.Drawing.Size(102, 41);
            this.DvrKaydet.TabIndex = 1;
            this.DvrKaydet.Text = "Kaydet";
            this.DvrKaydet.UseVisualStyleBackColor = true;
            // 
            // DvrYukle
            // 
            this.DvrYukle.Location = new System.Drawing.Point(624, 380);
            this.DvrYukle.Name = "DvrYukle";
            this.DvrYukle.Size = new System.Drawing.Size(102, 43);
            this.DvrYukle.TabIndex = 2;
            this.DvrYukle.Text = "Dosyadan Dever Yükle";
            this.DvrYukle.UseVisualStyleBackColor = true;
            this.DvrYukle.Click += new System.EventHandler(this.DvrYukle_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.DvrYukle);
            this.Controls.Add(this.DvrKaydet);
            this.Controls.Add(this.dataGridView1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button DvrKaydet;
        private System.Windows.Forms.Button DvrYukle;
    }
}