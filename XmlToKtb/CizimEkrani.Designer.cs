namespace XmlToKtb
{
    partial class CizimEkrani
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
            this.anaMenuStrip = new System.Windows.Forms.MenuStrip();
            this.dosyaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dosyaAcMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dosyaKaydetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.iceAktarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.jsonIceAktarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ekranMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.girisEkraniMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.secAraciButton = new System.Windows.Forms.ToolStripButton();
            this.cizgiAraciButton = new System.Windows.Forms.ToolStripButton();
            this.yakalamaAyraci = new System.Windows.Forms.ToolStripSeparator();
            this.noktaYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.ucYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.ortaYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.dikYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.durumStrip = new System.Windows.Forms.StatusStrip();
            this.konumDurumLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.koordinatSistemiLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ozelliklerPaneli = new System.Windows.Forms.Panel();
            this.ozellikGrid = new System.Windows.Forms.PropertyGrid();
            this.ozelliklerBaslikLabel = new System.Windows.Forms.Label();
            this.cizimPaneli1 = new XmlToKtb.Cizim.CizimPaneli();
            this.anaMenuStrip.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.durumStrip.SuspendLayout();
            this.ozelliklerPaneli.SuspendLayout();
            this.SuspendLayout();
            //
            // anaMenuStrip
            //
            this.anaMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dosyaMenuItem,
            this.iceAktarMenuItem,
            this.ekranMenuItem});
            this.anaMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.anaMenuStrip.Name = "anaMenuStrip";
            this.anaMenuStrip.Size = new System.Drawing.Size(1200, 24);
            this.anaMenuStrip.TabIndex = 4;
            this.anaMenuStrip.Text = "anaMenuStrip";
            //
            // dosyaMenuItem
            //
            this.dosyaMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dosyaAcMenuItem,
            this.dosyaKaydetMenuItem});
            this.dosyaMenuItem.Name = "dosyaMenuItem";
            this.dosyaMenuItem.Size = new System.Drawing.Size(58, 20);
            this.dosyaMenuItem.Text = "Dosya";
            //
            // dosyaAcMenuItem
            //
            this.dosyaAcMenuItem.Name = "dosyaAcMenuItem";
            this.dosyaAcMenuItem.Size = new System.Drawing.Size(180, 22);
            this.dosyaAcMenuItem.Text = "Aç (DXF)";
            this.dosyaAcMenuItem.Click += new System.EventHandler(this.dosyaAcMenuItem_Click);
            //
            // dosyaKaydetMenuItem
            //
            this.dosyaKaydetMenuItem.Name = "dosyaKaydetMenuItem";
            this.dosyaKaydetMenuItem.Size = new System.Drawing.Size(180, 22);
            this.dosyaKaydetMenuItem.Text = "Kaydet (DXF)";
            this.dosyaKaydetMenuItem.Click += new System.EventHandler(this.dosyaKaydetMenuItem_Click);
            //
            // iceAktarMenuItem
            //
            this.iceAktarMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.jsonIceAktarMenuItem});
            this.iceAktarMenuItem.Name = "iceAktarMenuItem";
            this.iceAktarMenuItem.Size = new System.Drawing.Size(75, 20);
            this.iceAktarMenuItem.Text = "İçe Aktar";
            //
            // jsonIceAktarMenuItem
            //
            this.jsonIceAktarMenuItem.Name = "jsonIceAktarMenuItem";
            this.jsonIceAktarMenuItem.Size = new System.Drawing.Size(180, 22);
            this.jsonIceAktarMenuItem.Text = "JSON İçe Aktar";
            this.jsonIceAktarMenuItem.Click += new System.EventHandler(this.jsonIceAktarMenuItem_Click);
            //
            // ekranMenuItem
            //
            this.ekranMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.girisEkraniMenuItem});
            this.ekranMenuItem.Name = "ekranMenuItem";
            this.ekranMenuItem.Size = new System.Drawing.Size(50, 20);
            this.ekranMenuItem.Text = "Ekran";
            //
            // girisEkraniMenuItem
            //
            this.girisEkraniMenuItem.Name = "girisEkraniMenuItem";
            this.girisEkraniMenuItem.Size = new System.Drawing.Size(180, 22);
            this.girisEkraniMenuItem.Text = "Giriş Ekranı";
            this.girisEkraniMenuItem.Click += new System.EventHandler(this.girisEkraniMenuItem_Click);
            //
            // toolStrip1
            //
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.secAraciButton,
            this.cizgiAraciButton,
            this.yakalamaAyraci,
            this.noktaYakalaButton,
            this.ucYakalaButton,
            this.ortaYakalaButton,
            this.dikYakalaButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1200, 25);
            this.toolStrip1.TabIndex = 0;
            //
            // secAraciButton
            //
            this.secAraciButton.Checked = true;
            this.secAraciButton.CheckOnClick = true;
            this.secAraciButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.secAraciButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.secAraciButton.Name = "secAraciButton";
            this.secAraciButton.Size = new System.Drawing.Size(36, 22);
            this.secAraciButton.Text = "Seç";
            this.secAraciButton.Click += new System.EventHandler(this.secAraciButton_Click);
            //
            // cizgiAraciButton
            //
            this.cizgiAraciButton.CheckOnClick = true;
            this.cizgiAraciButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cizgiAraciButton.Name = "cizgiAraciButton";
            this.cizgiAraciButton.Size = new System.Drawing.Size(43, 22);
            this.cizgiAraciButton.Text = "Çizgi";
            this.cizgiAraciButton.Click += new System.EventHandler(this.cizgiAraciButton_Click);
            //
            // yakalamaAyraci
            //
            this.yakalamaAyraci.Name = "yakalamaAyraci";
            this.yakalamaAyraci.Size = new System.Drawing.Size(6, 25);
            //
            // noktaYakalaButton
            //
            this.noktaYakalaButton.CheckOnClick = true;
            this.noktaYakalaButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.noktaYakalaButton.Name = "noktaYakalaButton";
            this.noktaYakalaButton.Size = new System.Drawing.Size(50, 22);
            this.noktaYakalaButton.Text = "Nokta";
            this.noktaYakalaButton.ToolTipText = "Nokta Yakala";
            this.noktaYakalaButton.CheckedChanged += new System.EventHandler(this.yakalamaButonu_CheckedChanged);
            //
            // ucYakalaButton
            //
            this.ucYakalaButton.CheckOnClick = true;
            this.ucYakalaButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ucYakalaButton.Name = "ucYakalaButton";
            this.ucYakalaButton.Size = new System.Drawing.Size(30, 22);
            this.ucYakalaButton.Text = "Uç";
            this.ucYakalaButton.ToolTipText = "Doğru Ucu Yakala";
            this.ucYakalaButton.CheckedChanged += new System.EventHandler(this.yakalamaButonu_CheckedChanged);
            //
            // ortaYakalaButton
            //
            this.ortaYakalaButton.CheckOnClick = true;
            this.ortaYakalaButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ortaYakalaButton.Name = "ortaYakalaButton";
            this.ortaYakalaButton.Size = new System.Drawing.Size(40, 22);
            this.ortaYakalaButton.Text = "Orta";
            this.ortaYakalaButton.ToolTipText = "Doğru Ortası Yakala";
            this.ortaYakalaButton.CheckedChanged += new System.EventHandler(this.yakalamaButonu_CheckedChanged);
            //
            // dikYakalaButton
            //
            this.dikYakalaButton.CheckOnClick = true;
            this.dikYakalaButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.dikYakalaButton.Name = "dikYakalaButton";
            this.dikYakalaButton.Size = new System.Drawing.Size(30, 22);
            this.dikYakalaButton.Text = "Dik";
            this.dikYakalaButton.ToolTipText = "Objeye Dik Yakala";
            this.dikYakalaButton.CheckedChanged += new System.EventHandler(this.yakalamaButonu_CheckedChanged);
            //
            // durumStrip
            //
            this.durumStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.konumDurumLabel,
            this.koordinatSistemiLabel});
            this.durumStrip.Location = new System.Drawing.Point(0, 728);
            this.durumStrip.Name = "durumStrip";
            this.durumStrip.Size = new System.Drawing.Size(1200, 22);
            this.durumStrip.TabIndex = 1;
            //
            // konumDurumLabel
            //
            this.konumDurumLabel.Name = "konumDurumLabel";
            this.konumDurumLabel.Size = new System.Drawing.Size(120, 17);
            this.konumDurumLabel.Text = "X: 0.000   Y: 0.000";
            //
            // koordinatSistemiLabel
            //
            this.koordinatSistemiLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.koordinatSistemiLabel.IsLink = true;
            this.koordinatSistemiLabel.Name = "koordinatSistemiLabel";
            this.koordinatSistemiLabel.Size = new System.Drawing.Size(59, 17);
            this.koordinatSistemiLabel.Text = "Tanımsız";
            this.koordinatSistemiLabel.Click += new System.EventHandler(this.koordinatSistemiLabel_Click);
            //
            // ozelliklerPaneli
            //
            this.ozelliklerPaneli.Controls.Add(this.ozellikGrid);
            this.ozelliklerPaneli.Controls.Add(this.ozelliklerBaslikLabel);
            this.ozelliklerPaneli.Dock = System.Windows.Forms.DockStyle.Left;
            this.ozelliklerPaneli.Location = new System.Drawing.Point(0, 49);
            this.ozelliklerPaneli.Name = "ozelliklerPaneli";
            this.ozelliklerPaneli.Size = new System.Drawing.Size(260, 679);
            this.ozelliklerPaneli.TabIndex = 2;
            //
            // ozellikGrid
            //
            this.ozellikGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ozellikGrid.Location = new System.Drawing.Point(0, 24);
            this.ozellikGrid.Name = "ozellikGrid";
            this.ozellikGrid.Size = new System.Drawing.Size(260, 679);
            this.ozellikGrid.TabIndex = 1;
            this.ozellikGrid.ToolbarVisible = false;
            this.ozellikGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.ozellikGrid_PropertyValueChanged);
            //
            // ozelliklerBaslikLabel
            //
            this.ozelliklerBaslikLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ozelliklerBaslikLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.ozelliklerBaslikLabel.Location = new System.Drawing.Point(0, 0);
            this.ozelliklerBaslikLabel.Name = "ozelliklerBaslikLabel";
            this.ozelliklerBaslikLabel.Padding = new System.Windows.Forms.Padding(6, 4, 0, 4);
            this.ozelliklerBaslikLabel.Size = new System.Drawing.Size(260, 24);
            this.ozelliklerBaslikLabel.TabIndex = 0;
            this.ozelliklerBaslikLabel.Text = "Özellikler";
            //
            // cizimPaneli1
            //
            this.cizimPaneli1.BackColor = System.Drawing.Color.White;
            this.cizimPaneli1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cizimPaneli1.Location = new System.Drawing.Point(260, 49);
            this.cizimPaneli1.Name = "cizimPaneli1";
            this.cizimPaneli1.Size = new System.Drawing.Size(940, 679);
            this.cizimPaneli1.TabIndex = 3;
            //
            // CizimEkrani
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 750);
            this.Controls.Add(this.cizimPaneli1);
            this.Controls.Add(this.ozelliklerPaneli);
            this.Controls.Add(this.durumStrip);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.anaMenuStrip);
            this.MainMenuStrip = this.anaMenuStrip;
            this.MinimumSize = new System.Drawing.Size(700, 450);
            this.Name = "CizimEkrani";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Çizim Ekranı";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.anaMenuStrip.ResumeLayout(false);
            this.anaMenuStrip.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.durumStrip.ResumeLayout(false);
            this.durumStrip.PerformLayout();
            this.ozelliklerPaneli.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip anaMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem dosyaMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dosyaAcMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dosyaKaydetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem iceAktarMenuItem;
        private System.Windows.Forms.ToolStripMenuItem jsonIceAktarMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ekranMenuItem;
        private System.Windows.Forms.ToolStripMenuItem girisEkraniMenuItem;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton secAraciButton;
        private System.Windows.Forms.ToolStripButton cizgiAraciButton;
        private System.Windows.Forms.ToolStripSeparator yakalamaAyraci;
        private System.Windows.Forms.ToolStripButton noktaYakalaButton;
        private System.Windows.Forms.ToolStripButton ucYakalaButton;
        private System.Windows.Forms.ToolStripButton ortaYakalaButton;
        private System.Windows.Forms.ToolStripButton dikYakalaButton;
        private System.Windows.Forms.StatusStrip durumStrip;
        private System.Windows.Forms.ToolStripStatusLabel konumDurumLabel;
        private System.Windows.Forms.ToolStripStatusLabel koordinatSistemiLabel;
        private System.Windows.Forms.Panel ozelliklerPaneli;
        private System.Windows.Forms.PropertyGrid ozellikGrid;
        private System.Windows.Forms.Label ozelliklerBaslikLabel;
        private XmlToKtb.Cizim.CizimPaneli cizimPaneli1;
    }
}
