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
            this.googleEarthIceAktarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disaAktarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.googleEarthAktarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.duzenleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kaydirMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dondurMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kopyalaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.olcekleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noktaEditoruMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cizimMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cizgiCizMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daireCizMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daireMerkezYaricapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daire3NoktaMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daire2NoktaYaricapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daireTegetIkiObjeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daireTegetUcObjeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yolMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.guzergahXmlCizMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.guzergahEditoruMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yuzeyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yuzeyNoktalariniKaydetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yuzeyOlusturMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yuzeyXmldenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yuzeyDxftenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yuzeyNoktalardanMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kesitEditoruMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ekranMenuItem =new System.Windows.Forms.ToolStripMenuItem();
            this.girisEkraniMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ayarlarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gorunumHassasiyetiMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabakaAyarlariMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.yakalamaAyraci = new System.Windows.Forms.ToolStripSeparator();
            this.noktaYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.ucYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.ortaYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.dikYakalaButton = new System.Windows.Forms.ToolStripButton();
            this.merkezYakalaButton = new System.Windows.Forms.ToolStripButton();
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
            this.cizimMenuItem,
            this.duzenleMenuItem,
            this.yolMenuItem,
            this.yuzeyMenuItem,
            this.kesitEditoruMenuItem,
            this.ekranMenuItem,
            this.ayarlarMenuItem});
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
            this.dosyaKaydetMenuItem,
            this.iceAktarMenuItem,
            this.disaAktarMenuItem});
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
            this.jsonIceAktarMenuItem,
            this.googleEarthIceAktarMenuItem});
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
            // googleEarthIceAktarMenuItem
            //
            this.googleEarthIceAktarMenuItem.Name = "googleEarthIceAktarMenuItem";
            this.googleEarthIceAktarMenuItem.Size = new System.Drawing.Size(220, 22);
            this.googleEarthIceAktarMenuItem.Text = "Google Earth İçe Aktar (KML/KMZ)";
            this.googleEarthIceAktarMenuItem.Click += new System.EventHandler(this.googleEarthIceAktarMenuItem_Click);
            //
            // disaAktarMenuItem
            //
            this.disaAktarMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.googleEarthAktarMenuItem});
            this.disaAktarMenuItem.Name = "disaAktarMenuItem";
            this.disaAktarMenuItem.Size = new System.Drawing.Size(75, 20);
            this.disaAktarMenuItem.Text = "Dışa Aktar";
            //
            // googleEarthAktarMenuItem
            //
            this.googleEarthAktarMenuItem.Name = "googleEarthAktarMenuItem";
            this.googleEarthAktarMenuItem.Size = new System.Drawing.Size(220, 22);
            this.googleEarthAktarMenuItem.Text = "Google Earth (KML/KMZ)";
            this.googleEarthAktarMenuItem.Click += new System.EventHandler(this.googleEarthAktarMenuItem_Click);
            //
            // duzenleMenuItem
            //
            this.duzenleMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.kaydirMenuItem,
            this.dondurMenuItem,
            this.kesMenuItem,
            this.kopyalaMenuItem,
            this.olcekleMenuItem,
            this.noktaEditoruMenuItem});
            this.duzenleMenuItem.Name = "duzenleMenuItem";
            this.duzenleMenuItem.Size = new System.Drawing.Size(64, 20);
            this.duzenleMenuItem.Text = "Düzenle";
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
            // noktaEditoruMenuItem
            //
            this.noktaEditoruMenuItem.Name = "noktaEditoruMenuItem";
            this.noktaEditoruMenuItem.Size = new System.Drawing.Size(180, 22);
            this.noktaEditoruMenuItem.Text = "Nokta Editörü...";
            this.noktaEditoruMenuItem.Click += new System.EventHandler(this.noktaEditoruMenuItem_Click);
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
            this.daire3NoktaMenuItem,
            this.daire2NoktaYaricapMenuItem,
            this.daireTegetIkiObjeMenuItem,
            this.daireTegetUcObjeMenuItem});
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
            // daire2NoktaYaricapMenuItem
            //
            this.daire2NoktaYaricapMenuItem.Name = "daire2NoktaYaricapMenuItem";
            this.daire2NoktaYaricapMenuItem.Size = new System.Drawing.Size(260, 22);
            this.daire2NoktaYaricapMenuItem.Text = "2 Nokta ve Yarıçap";
            this.daire2NoktaYaricapMenuItem.Click += new System.EventHandler(this.daire2NoktaYaricapMenuItem_Click);
            //
            // daireTegetIkiObjeMenuItem
            //
            this.daireTegetIkiObjeMenuItem.Name = "daireTegetIkiObjeMenuItem";
            this.daireTegetIkiObjeMenuItem.Size = new System.Drawing.Size(260, 22);
            this.daireTegetIkiObjeMenuItem.Text = "2 Objeye Teğet ve Yarıçap";
            this.daireTegetIkiObjeMenuItem.Click += new System.EventHandler(this.daireTegetIkiObjeMenuItem_Click);
            //
            // daireTegetUcObjeMenuItem
            //
            this.daireTegetUcObjeMenuItem.Name = "daireTegetUcObjeMenuItem";
            this.daireTegetUcObjeMenuItem.Size = new System.Drawing.Size(260, 22);
            this.daireTegetUcObjeMenuItem.Text = "3 Objeye Teğet";
            this.daireTegetUcObjeMenuItem.Click += new System.EventHandler(this.daireTegetUcObjeMenuItem_Click);
            //
            // yolMenuItem
            //
            this.yolMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.guzergahXmlCizMenuItem,
            this.guzergahEditoruMenuItem});
            this.yolMenuItem.Name = "yolMenuItem";
            this.yolMenuItem.Size = new System.Drawing.Size(42, 20);
            this.yolMenuItem.Text = "Yol";
            //
            // guzergahXmlCizMenuItem
            //
            this.guzergahXmlCizMenuItem.Name = "guzergahXmlCizMenuItem";
            this.guzergahXmlCizMenuItem.Size = new System.Drawing.Size(240, 22);
            this.guzergahXmlCizMenuItem.Text = "Xml Dosyasından Güzergah Çiz";
            this.guzergahXmlCizMenuItem.Click += new System.EventHandler(this.guzergahXmlCizMenuItem_Click);
            //
            // guzergahEditoruMenuItem
            //
            this.guzergahEditoruMenuItem.Name = "guzergahEditoruMenuItem";
            this.guzergahEditoruMenuItem.Size = new System.Drawing.Size(240, 22);
            this.guzergahEditoruMenuItem.Text = "Güzergah Editörü...";
            this.guzergahEditoruMenuItem.Click += new System.EventHandler(this.guzergahEditoruMenuItem_Click);
            //
            // yuzeyMenuItem
            //
            this.yuzeyMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.yuzeyNoktalariniKaydetMenuItem,
            this.yuzeyOlusturMenuItem});
            this.yuzeyMenuItem.Name = "yuzeyMenuItem";
            this.yuzeyMenuItem.Size = new System.Drawing.Size(49, 20);
            this.yuzeyMenuItem.Text = "Yüzey";
            //
            // yuzeyNoktalariniKaydetMenuItem
            //
            this.yuzeyNoktalariniKaydetMenuItem.Name = "yuzeyNoktalariniKaydetMenuItem";
            this.yuzeyNoktalariniKaydetMenuItem.Size = new System.Drawing.Size(270, 22);
            this.yuzeyNoktalariniKaydetMenuItem.Text = "Yüzeyin Noktalarını Farklı Kaydet...";
            this.yuzeyNoktalariniKaydetMenuItem.Click += new System.EventHandler(this.yuzeyNoktalariniKaydetMenuItem_Click);
            //
            // yuzeyOlusturMenuItem
            //
            this.yuzeyOlusturMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.yuzeyXmldenMenuItem,
            this.yuzeyDxftenMenuItem,
            this.yuzeyNoktalardanMenuItem});
            this.yuzeyOlusturMenuItem.Name = "yuzeyOlusturMenuItem";
            this.yuzeyOlusturMenuItem.Size = new System.Drawing.Size(270, 22);
            this.yuzeyOlusturMenuItem.Text = "Yüzey Oluştur";
            //
            // yuzeyXmldenMenuItem
            //
            this.yuzeyXmldenMenuItem.Name = "yuzeyXmldenMenuItem";
            this.yuzeyXmldenMenuItem.Size = new System.Drawing.Size(300, 22);
            this.yuzeyXmldenMenuItem.Text = "XML\'den Yükle ve Çiz...";
            this.yuzeyXmldenMenuItem.Click += new System.EventHandler(this.yuzeyXmldenMenuItem_Click);
            //
            // yuzeyDxftenMenuItem
            //
            this.yuzeyDxftenMenuItem.Name = "yuzeyDxftenMenuItem";
            this.yuzeyDxftenMenuItem.Size = new System.Drawing.Size(300, 22);
            this.yuzeyDxftenMenuItem.Text = "DXF\'ten Yükle ve Çiz...";
            this.yuzeyDxftenMenuItem.Click += new System.EventHandler(this.yuzeyDxftenMenuItem_Click);
            //
            // yuzeyNoktalardanMenuItem
            //
            this.yuzeyNoktalardanMenuItem.Name = "yuzeyNoktalardanMenuItem";
            this.yuzeyNoktalardanMenuItem.Size = new System.Drawing.Size(300, 22);
            this.yuzeyNoktalardanMenuItem.Text = "Nokta Dosyasından Üçgenle ve Çiz...";
            this.yuzeyNoktalardanMenuItem.Click += new System.EventHandler(this.yuzeyNoktalardanMenuItem_Click);
            //
            // kesitEditoruMenuItem
            //
            this.kesitEditoruMenuItem.Name = "kesitEditoruMenuItem";
            this.kesitEditoruMenuItem.Size = new System.Drawing.Size(87, 20);
            this.kesitEditoruMenuItem.Text = "Kesit Editörü";
            this.kesitEditoruMenuItem.Click += new System.EventHandler(this.kesitEditoruMenuItem_Click);
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
            // ayarlarMenuItem
            //
            this.ayarlarMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gorunumHassasiyetiMenuItem,
            this.tabakaAyarlariMenuItem});
            this.ayarlarMenuItem.Name = "ayarlarMenuItem";
            this.ayarlarMenuItem.Size = new System.Drawing.Size(63, 20);
            this.ayarlarMenuItem.Text = "Ayarlar";
            //
            // gorunumHassasiyetiMenuItem
            //
            this.gorunumHassasiyetiMenuItem.Name = "gorunumHassasiyetiMenuItem";
            this.gorunumHassasiyetiMenuItem.Size = new System.Drawing.Size(240, 22);
            this.gorunumHassasiyetiMenuItem.Text = "Görünüm Hassasiyeti...";
            this.gorunumHassasiyetiMenuItem.Click += new System.EventHandler(this.gorunumHassasiyetiMenuItem_Click);
            //
            // tabakaAyarlariMenuItem
            //
            this.tabakaAyarlariMenuItem.Name = "tabakaAyarlariMenuItem";
            this.tabakaAyarlariMenuItem.Size = new System.Drawing.Size(240, 22);
            this.tabakaAyarlariMenuItem.Text = "Tabaka Ayarları...";
            this.tabakaAyarlariMenuItem.Click += new System.EventHandler(this.tabakaAyarlariMenuItem_Click);
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
            this.toolStrip1.TabIndex = 0;
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
        private System.Windows.Forms.ToolStripMenuItem googleEarthIceAktarMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disaAktarMenuItem;
        private System.Windows.Forms.ToolStripMenuItem googleEarthAktarMenuItem;
        private System.Windows.Forms.ToolStripMenuItem duzenleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kaydirMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dondurMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kopyalaMenuItem;
        private System.Windows.Forms.ToolStripMenuItem olcekleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noktaEditoruMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cizimMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cizgiCizMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daireCizMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daireMerkezYaricapMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daire3NoktaMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daire2NoktaYaricapMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daireTegetIkiObjeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daireTegetUcObjeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yolMenuItem;
        private System.Windows.Forms.ToolStripMenuItem guzergahXmlCizMenuItem;
        private System.Windows.Forms.ToolStripMenuItem guzergahEditoruMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yuzeyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yuzeyNoktalariniKaydetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yuzeyOlusturMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yuzeyXmldenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yuzeyDxftenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yuzeyNoktalardanMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kesitEditoruMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ekranMenuItem;
        private System.Windows.Forms.ToolStripMenuItem girisEkraniMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ayarlarMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gorunumHassasiyetiMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tabakaAyarlariMenuItem;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSeparator yakalamaAyraci;
        private System.Windows.Forms.ToolStripButton noktaYakalaButton;
        private System.Windows.Forms.ToolStripButton ucYakalaButton;
        private System.Windows.Forms.ToolStripButton ortaYakalaButton;
        private System.Windows.Forms.ToolStripButton dikYakalaButton;
        private System.Windows.Forms.ToolStripButton merkezYakalaButton;
        private System.Windows.Forms.StatusStrip durumStrip;
        private System.Windows.Forms.ToolStripStatusLabel konumDurumLabel;
        private System.Windows.Forms.ToolStripStatusLabel koordinatSistemiLabel;
        private System.Windows.Forms.Panel ozelliklerPaneli;
        private System.Windows.Forms.PropertyGrid ozellikGrid;
        private System.Windows.Forms.Label ozelliklerBaslikLabel;
        private XmlToKtb.Cizim.CizimPaneli cizimPaneli1;
    }
}
