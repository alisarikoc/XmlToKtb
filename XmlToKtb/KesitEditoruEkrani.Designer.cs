namespace XmlToKtb
{
    partial class KesitEditoruEkrani
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
            this.components = new System.ComponentModel.Container();
            this.anaMenuStrip = new System.Windows.Forms.MenuStrip();
            this.dosyaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dosyaAcMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dosyaKaydetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cizimMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cizgiCizMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daireCizMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daireMerkezYaricapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daire3NoktaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.duzenleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.geriAlMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ilerleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.duzenleAyraci = new System.Windows.Forms.ToolStripSeparator();
            this.kaydirMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dondurMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kopyalaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.olcekleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.projeKesitiOlusturMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.yakalamaAyraci = new System.Windows.Forms.ToolStripSeparator();
            this.noktaYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.ucYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.ortaYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.dikYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.merkezYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.durumStrip = new System.Windows.Forms.StatusStrip();
            this.konumDurumLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.solPanel = new System.Windows.Forms.Panel();
            this.kesitListView = new System.Windows.Forms.ListView();
            this.kesitNoKolon = new System.Windows.Forms.ColumnHeader();
            this.kilometreKolon = new System.Windows.Forms.ColumnHeader();
            this.aciklamaKolon = new System.Windows.Forms.ColumnHeader();
            this.solTabControl = new System.Windows.Forms.TabControl();
            this.dosyalarTabPage = new System.Windows.Forms.TabPage();
            this.kesitlerTabPage = new System.Windows.Forms.TabPage();
            this.dosyaListView = new System.Windows.Forms.ListView();
            this.dosyaAdiKolon = new System.Windows.Forms.ColumnHeader();
            this.referansKolon = new System.Windows.Forms.ColumnHeader();
            this.dosyaMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.referansYapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dosyaOzelliklerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dosyaKapatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ozelliklerPaneli = new System.Windows.Forms.Panel();
            this.ozellikGrid = new System.Windows.Forms.PropertyGrid();
            this.ozelliklerBaslikLabel = new System.Windows.Forms.Label();
            this.cizimPaneli1 = new XmlToKtb.Cizim.CizimPaneli();
            this.anaMenuStrip.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.durumStrip.SuspendLayout();
            this.solPanel.SuspendLayout();
            this.solTabControl.SuspendLayout();
            this.dosyalarTabPage.SuspendLayout();
            this.kesitlerTabPage.SuspendLayout();
            this.dosyaMenuStrip.SuspendLayout();
            this.ozelliklerPaneli.SuspendLayout();
            this.SuspendLayout();
            //
            // anaMenuStrip
            //
            this.anaMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dosyaMenuItem,
            this.cizimMenuItem,
            this.duzenleMenuItem,
            this.projeMenuItem});
            this.anaMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.anaMenuStrip.Name = "anaMenuStrip";
            this.anaMenuStrip.Size = new System.Drawing.Size(1000, 24);
            this.anaMenuStrip.TabIndex = 3;
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
            this.dosyaAcMenuItem.Text = "Aç (KSE)";
            this.dosyaAcMenuItem.Click += new System.EventHandler(this.dosyaAcMenuItem_Click);
            //
            // dosyaKaydetMenuItem
            //
            this.dosyaKaydetMenuItem.Name = "dosyaKaydetMenuItem";
            this.dosyaKaydetMenuItem.Size = new System.Drawing.Size(180, 22);
            this.dosyaKaydetMenuItem.Text = "Kaydet (KSE)";
            this.dosyaKaydetMenuItem.Click += new System.EventHandler(this.dosyaKaydetMenuItem_Click);
            //
            // cizimMenuItem
            //
            this.cizimMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cizgiCizMenuItem,
            this.daireCizMenuItem});
            this.cizimMenuItem.Name = "cizimMenuItem";
            this.cizimMenuItem.Size = new System.Drawing.Size(58, 20);
            this.cizimMenuItem.Text = "Çizim";
            //
            // cizgiCizMenuItem
            //
            this.cizgiCizMenuItem.Name = "cizgiCizMenuItem";
            this.cizgiCizMenuItem.Size = new System.Drawing.Size(220, 22);
            this.cizgiCizMenuItem.Text = "Çizgi Çiz";
            this.cizgiCizMenuItem.Click += new System.EventHandler(this.cizgiCizMenuItem_Click);
            //
            // daireCizMenuItem
            //
            this.daireCizMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.daireMerkezYaricapMenuItem,
            this.daire3NoktaMenuItem});
            this.daireCizMenuItem.Name = "daireCizMenuItem";
            this.daireCizMenuItem.Size = new System.Drawing.Size(220, 22);
            this.daireCizMenuItem.Text = "Daire Çiz";
            //
            // daireMerkezYaricapMenuItem
            //
            this.daireMerkezYaricapMenuItem.Name = "daireMerkezYaricapMenuItem";
            this.daireMerkezYaricapMenuItem.Size = new System.Drawing.Size(260, 22);
            this.daireMerkezYaricapMenuItem.Text = "Merkez ve Yarıçap";
            this.daireMerkezYaricapMenuItem.Click += new System.EventHandler(this.daireMerkezYaricapMenuItem_Click);
            //
            // daire3NoktaMenuItem
            //
            this.daire3NoktaMenuItem.Name = "daire3NoktaMenuItem";
            this.daire3NoktaMenuItem.Size = new System.Drawing.Size(260, 22);
            this.daire3NoktaMenuItem.Text = "3 Nokta";
            this.daire3NoktaMenuItem.Click += new System.EventHandler(this.daire3NoktaMenuItem_Click);
            //
            // duzenleMenuItem
            //
            this.duzenleMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.geriAlMenuItem,
            this.ilerleMenuItem,
            this.duzenleAyraci,
            this.kaydirMenuItem,
            this.dondurMenuItem,
            this.kesMenuItem,
            this.kopyalaMenuItem,
            this.olcekleMenuItem});
            this.duzenleMenuItem.Name = "duzenleMenuItem";
            this.duzenleMenuItem.Size = new System.Drawing.Size(64, 20);
            this.duzenleMenuItem.Text = "Düzenle";
            //
            // geriAlMenuItem
            //
            this.geriAlMenuItem.Name = "geriAlMenuItem";
            this.geriAlMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.geriAlMenuItem.Size = new System.Drawing.Size(180, 22);
            this.geriAlMenuItem.Text = "Geri Al";
            this.geriAlMenuItem.Click += new System.EventHandler(this.geriAlMenuItem_Click);
            //
            // ilerleMenuItem
            //
            this.ilerleMenuItem.Name = "ilerleMenuItem";
            this.ilerleMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
            this.ilerleMenuItem.Size = new System.Drawing.Size(180, 22);
            this.ilerleMenuItem.Text = "İlerle";
            this.ilerleMenuItem.Click += new System.EventHandler(this.ilerleMenuItem_Click);
            //
            // duzenleAyraci
            //
            this.duzenleAyraci.Name = "duzenleAyraci";
            this.duzenleAyraci.Size = new System.Drawing.Size(177, 6);
            //
            // kaydirMenuItem
            //
            this.kaydirMenuItem.Name = "kaydirMenuItem";
            this.kaydirMenuItem.Size = new System.Drawing.Size(180, 22);
            this.kaydirMenuItem.Text = "Kaydır";
            this.kaydirMenuItem.Click += new System.EventHandler(this.kaydirMenuItem_Click);
            //
            // dondurMenuItem
            //
            this.dondurMenuItem.Name = "dondurMenuItem";
            this.dondurMenuItem.Size = new System.Drawing.Size(180, 22);
            this.dondurMenuItem.Text = "Döndür";
            this.dondurMenuItem.Click += new System.EventHandler(this.dondurMenuItem_Click);
            //
            // kesMenuItem
            //
            this.kesMenuItem.Name = "kesMenuItem";
            this.kesMenuItem.Size = new System.Drawing.Size(180, 22);
            this.kesMenuItem.Text = "Kes";
            this.kesMenuItem.Click += new System.EventHandler(this.kesMenuItem_Click);
            //
            // kopyalaMenuItem
            //
            this.kopyalaMenuItem.Name = "kopyalaMenuItem";
            this.kopyalaMenuItem.Size = new System.Drawing.Size(180, 22);
            this.kopyalaMenuItem.Text = "Kopyala";
            this.kopyalaMenuItem.Click += new System.EventHandler(this.kopyalaMenuItem_Click);
            //
            // olcekleMenuItem
            //
            this.olcekleMenuItem.Name = "olcekleMenuItem";
            this.olcekleMenuItem.Size = new System.Drawing.Size(180, 22);
            this.olcekleMenuItem.Text = "Ölçekle";
            this.olcekleMenuItem.Click += new System.EventHandler(this.olcekleMenuItem_Click);
            //
            // projeMenuItem
            //
            this.projeMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.projeKesitiOlusturMenuItem});
            this.projeMenuItem.Name = "projeMenuItem";
            this.projeMenuItem.Size = new System.Drawing.Size(46, 20);
            this.projeMenuItem.Text = "Proje";
            //
            // projeKesitiOlusturMenuItem
            //
            this.projeKesitiOlusturMenuItem.Name = "projeKesitiOlusturMenuItem";
            this.projeKesitiOlusturMenuItem.Size = new System.Drawing.Size(200, 22);
            this.projeKesitiOlusturMenuItem.Text = "Proje Kesiti Oluştur...";
            this.projeKesitiOlusturMenuItem.Click += new System.EventHandler(this.projeKesitiOlusturMenuItem_Click);
            //
            // toolStrip1
            //
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.yakalamaAyraci,
            this.noktaYakalaButton,
            this.ucYakalaButton,
            this.ortaYakalaButton,
            this.dikYakalaButton,
            this.merkezYakalaButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1200, 25);
            this.toolStrip1.TabIndex = 5;
            //
            // yakalamaAyraci
            //
            this.yakalamaAyraci.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.yakalamaAyraci.Name = "yakalamaAyraci";
            this.yakalamaAyraci.Size = new System.Drawing.Size(6, 25);
            //
            // noktaYakalaButton
            //
            this.noktaYakalaButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
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
            this.ucYakalaButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
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
            this.ortaYakalaButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
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
            this.dikYakalaButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.dikYakalaButton.CheckOnClick = true;
            this.dikYakalaButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.dikYakalaButton.Name = "dikYakalaButton";
            this.dikYakalaButton.Size = new System.Drawing.Size(30, 22);
            this.dikYakalaButton.Text = "Dik";
            this.dikYakalaButton.ToolTipText = "Objeye Dik Yakala";
            this.dikYakalaButton.CheckedChanged += new System.EventHandler(this.yakalamaButonu_CheckedChanged);
            //
            // merkezYakalaButton
            //
            this.merkezYakalaButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.merkezYakalaButton.CheckOnClick = true;
            this.merkezYakalaButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.merkezYakalaButton.Name = "merkezYakalaButton";
            this.merkezYakalaButton.Size = new System.Drawing.Size(50, 22);
            this.merkezYakalaButton.Text = "Merkez";
            this.merkezYakalaButton.ToolTipText = "Daire/Yay Merkezi Yakala";
            this.merkezYakalaButton.CheckedChanged += new System.EventHandler(this.yakalamaButonu_CheckedChanged);
            //
            // durumStrip
            //
            this.durumStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.konumDurumLabel});
            this.durumStrip.Location = new System.Drawing.Point(0, 628);
            this.durumStrip.Name = "durumStrip";
            this.durumStrip.Size = new System.Drawing.Size(1000, 22);
            this.durumStrip.TabIndex = 0;
            //
            // konumDurumLabel
            //
            this.konumDurumLabel.Name = "konumDurumLabel";
            this.konumDurumLabel.Size = new System.Drawing.Size(120, 17);
            this.konumDurumLabel.Text = "X: 0.000   Y: 0.000";
            //
            // solPanel
            //
            this.solPanel.Controls.Add(this.solTabControl);
            this.solPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.solPanel.Location = new System.Drawing.Point(0, 24);
            this.solPanel.Name = "solPanel";
            this.solPanel.Size = new System.Drawing.Size(260, 604);
            this.solPanel.TabIndex = 1;
            //
            // solTabControl
            //
            this.solTabControl.Controls.Add(this.dosyalarTabPage);
            this.solTabControl.Controls.Add(this.kesitlerTabPage);
            this.solTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.solTabControl.Location = new System.Drawing.Point(0, 0);
            this.solTabControl.Name = "solTabControl";
            this.solTabControl.SelectedIndex = 0;
            this.solTabControl.Size = new System.Drawing.Size(260, 604);
            this.solTabControl.TabIndex = 0;
            //
            // dosyalarTabPage
            //
            this.dosyalarTabPage.Controls.Add(this.dosyaListView);
            this.dosyalarTabPage.Location = new System.Drawing.Point(4, 22);
            this.dosyalarTabPage.Name = "dosyalarTabPage";
            this.dosyalarTabPage.Size = new System.Drawing.Size(252, 578);
            this.dosyalarTabPage.TabIndex = 0;
            this.dosyalarTabPage.Text = "Dosyalar";
            this.dosyalarTabPage.UseVisualStyleBackColor = true;
            //
            // kesitlerTabPage
            //
            this.kesitlerTabPage.Controls.Add(this.kesitListView);
            this.kesitlerTabPage.Location = new System.Drawing.Point(4, 22);
            this.kesitlerTabPage.Name = "kesitlerTabPage";
            this.kesitlerTabPage.Size = new System.Drawing.Size(252, 578);
            this.kesitlerTabPage.TabIndex = 1;
            this.kesitlerTabPage.Text = "Kesitler";
            this.kesitlerTabPage.UseVisualStyleBackColor = true;
            //
            // dosyaListView
            //
            this.dosyaListView.CheckBoxes = true;
            this.dosyaListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.dosyaAdiKolon,
            this.referansKolon});
            this.dosyaListView.ContextMenuStrip = this.dosyaMenuStrip;
            this.dosyaListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dosyaListView.FullRowSelect = true;
            this.dosyaListView.HideSelection = false;
            this.dosyaListView.Location = new System.Drawing.Point(0, 0);
            this.dosyaListView.MultiSelect = false;
            this.dosyaListView.Name = "dosyaListView";
            this.dosyaListView.ShowItemToolTips = true;
            this.dosyaListView.Size = new System.Drawing.Size(252, 578);
            this.dosyaListView.TabIndex = 0;
            this.dosyaListView.UseCompatibleStateImageBehavior = false;
            this.dosyaListView.View = System.Windows.Forms.View.Details;
            this.dosyaListView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.dosyaListView_ItemChecked);
            //
            // dosyaAdiKolon
            //
            this.dosyaAdiKolon.Text = "Dosya";
            this.dosyaAdiKolon.Width = 170;
            //
            // referansKolon
            //
            this.referansKolon.Text = "Referans";
            this.referansKolon.Width = 70;
            //
            // dosyaMenuStrip
            //
            this.dosyaMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.referansYapMenuItem,
            this.dosyaOzelliklerMenuItem,
            this.dosyaKapatMenuItem});
            this.dosyaMenuStrip.Name = "dosyaMenuStrip";
            this.dosyaMenuStrip.Size = new System.Drawing.Size(140, 70);
            this.dosyaMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.dosyaMenuStrip_Opening);
            //
            // referansYapMenuItem
            //
            this.referansYapMenuItem.Name = "referansYapMenuItem";
            this.referansYapMenuItem.Size = new System.Drawing.Size(139, 22);
            this.referansYapMenuItem.Text = "Referans Yap";
            this.referansYapMenuItem.Click += new System.EventHandler(this.referansYapMenuItem_Click);
            //
            // dosyaOzelliklerMenuItem
            //
            this.dosyaOzelliklerMenuItem.Name = "dosyaOzelliklerMenuItem";
            this.dosyaOzelliklerMenuItem.Size = new System.Drawing.Size(139, 22);
            this.dosyaOzelliklerMenuItem.Text = "Özellikler...";
            this.dosyaOzelliklerMenuItem.Click += new System.EventHandler(this.dosyaOzelliklerMenuItem_Click);
            //
            // dosyaKapatMenuItem
            //
            this.dosyaKapatMenuItem.Name = "dosyaKapatMenuItem";
            this.dosyaKapatMenuItem.Size = new System.Drawing.Size(139, 22);
            this.dosyaKapatMenuItem.Text = "Kapat";
            this.dosyaKapatMenuItem.Click += new System.EventHandler(this.dosyaKapatMenuItem_Click);
            //
            // kesitListView
            //
            this.kesitListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.kesitNoKolon,
            this.kilometreKolon,
            this.aciklamaKolon});
            this.kesitListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kesitListView.FullRowSelect = true;
            this.kesitListView.GridLines = true;
            this.kesitListView.HideSelection = false;
            this.kesitListView.Location = new System.Drawing.Point(0, 0);
            this.kesitListView.MultiSelect = false;
            this.kesitListView.Name = "kesitListView";
            this.kesitListView.Size = new System.Drawing.Size(252, 578);
            this.kesitListView.TabIndex = 1;
            this.kesitListView.UseCompatibleStateImageBehavior = false;
            this.kesitListView.View = System.Windows.Forms.View.Details;
            this.kesitListView.SelectedIndexChanged += new System.EventHandler(this.kesitListView_SelectedIndexChanged);
            //
            // kesitNoKolon
            //
            this.kesitNoKolon.Text = "Kesit No";
            this.kesitNoKolon.Width = 58;
            //
            // kilometreKolon
            //
            this.kilometreKolon.Text = "Kilometre";
            this.kilometreKolon.Width = 90;
            //
            // aciklamaKolon
            //
            this.aciklamaKolon.Text = "Açıklama";
            this.aciklamaKolon.Width = 90;

            //
            // ozelliklerPaneli
            //
            this.ozelliklerPaneli.Controls.Add(this.ozellikGrid);
            this.ozelliklerPaneli.Controls.Add(this.ozelliklerBaslikLabel);
            this.ozelliklerPaneli.Dock = System.Windows.Forms.DockStyle.Right;
            this.ozelliklerPaneli.Location = new System.Drawing.Point(940, 24);
            this.ozelliklerPaneli.Name = "ozelliklerPaneli";
            this.ozelliklerPaneli.Size = new System.Drawing.Size(260, 604);
            this.ozelliklerPaneli.TabIndex = 4;
            //
            // ozellikGrid
            //
            this.ozellikGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ozellikGrid.Location = new System.Drawing.Point(0, 24);
            this.ozellikGrid.Name = "ozellikGrid";
            this.ozellikGrid.Size = new System.Drawing.Size(260, 580);
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
            this.cizimPaneli1.Location = new System.Drawing.Point(260, 24);
            this.cizimPaneli1.Name = "cizimPaneli1";
            this.cizimPaneli1.Size = new System.Drawing.Size(680, 604);
            this.cizimPaneli1.TabIndex = 2;
            //
            // KesitEditoruEkrani
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 650);
            this.Controls.Add(this.cizimPaneli1);
            this.Controls.Add(this.ozelliklerPaneli);
            this.Controls.Add(this.solPanel);
            this.Controls.Add(this.durumStrip);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.anaMenuStrip);
            this.MainMenuStrip = this.anaMenuStrip;
            this.MinimumSize = new System.Drawing.Size(700, 450);
            this.Name = "KesitEditoruEkrani";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Kesit Editörü";
            this.anaMenuStrip.ResumeLayout(false);
            this.anaMenuStrip.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.durumStrip.ResumeLayout(false);
            this.durumStrip.PerformLayout();
            this.solPanel.ResumeLayout(false);
            this.solTabControl.ResumeLayout(false);
            this.dosyalarTabPage.ResumeLayout(false);
            this.kesitlerTabPage.ResumeLayout(false);
            this.dosyaMenuStrip.ResumeLayout(false);
            this.ozelliklerPaneli.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip anaMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem dosyaMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dosyaAcMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dosyaKaydetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cizimMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cizgiCizMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daireCizMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daireMerkezYaricapMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daire3NoktaMenuItem;
        private System.Windows.Forms.ToolStripMenuItem duzenleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem geriAlMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ilerleMenuItem;
        private System.Windows.Forms.ToolStripSeparator duzenleAyraci;
        private System.Windows.Forms.ToolStripMenuItem kaydirMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dondurMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kopyalaMenuItem;
        private System.Windows.Forms.ToolStripMenuItem olcekleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem projeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem projeKesitiOlusturMenuItem;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSeparator yakalamaAyraci;
        private System.Windows.Forms.ToolStripButton noktaYakalaButton;
        private System.Windows.Forms.ToolStripButton ucYakalaButton;
        private System.Windows.Forms.ToolStripButton ortaYakalaButton;
        private System.Windows.Forms.ToolStripButton dikYakalaButton;
        private System.Windows.Forms.ToolStripButton merkezYakalaButton;
        private System.Windows.Forms.StatusStrip durumStrip;
        private System.Windows.Forms.ToolStripStatusLabel konumDurumLabel;
        private System.Windows.Forms.Panel solPanel;
        private System.Windows.Forms.Panel ozelliklerPaneli;
        private System.Windows.Forms.PropertyGrid ozellikGrid;
        private System.Windows.Forms.Label ozelliklerBaslikLabel;
        private System.Windows.Forms.ListView kesitListView;
        private System.Windows.Forms.ColumnHeader kesitNoKolon;
        private System.Windows.Forms.ColumnHeader kilometreKolon;
        private System.Windows.Forms.ColumnHeader aciklamaKolon;
        private System.Windows.Forms.TabControl solTabControl;
        private System.Windows.Forms.TabPage dosyalarTabPage;
        private System.Windows.Forms.TabPage kesitlerTabPage;
        private System.Windows.Forms.ListView dosyaListView;
        private System.Windows.Forms.ColumnHeader dosyaAdiKolon;
        private System.Windows.Forms.ColumnHeader referansKolon;
        private System.Windows.Forms.ContextMenuStrip dosyaMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem referansYapMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dosyaOzelliklerMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dosyaKapatMenuItem;
        private XmlToKtb.Cizim.CizimPaneli cizimPaneli1;
    }
}
