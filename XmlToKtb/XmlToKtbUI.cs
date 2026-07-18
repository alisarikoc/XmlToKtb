using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Shapes;
using Common;
using Common.Common;
using Common.Enum;
using Common.Modeller;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using XmlToKtb.Properties;
using static System.Net.Mime.MediaTypeNames;
using static DXFLib.DXFGenericEntity;
using Path = System.IO.Path;

namespace XmlToKtb
{
    public partial class XmlToKtbUI : Form
    {

        private string ktbIcerigi;
        private string alnIcerigi;
        private string lisIcerigi;
        private string prfIcerigi;
        private string xmlIcerigi;
        private static string dosyaAdi;
        private static string rootPath;
        private DurumTip durumTip;
        private DosyaTip dosyaTip;
        private readonly Random _rastgele = new Random();
        private string geoIcerigi;

        public XmlToKtbUI()
        {
            InitializeComponent();
            durumTip = DurumTip.Basladi;
            dosyaTuruComboBox.SelectedIndex = (int) DosyaTip.KTB;
            GorunumuAyarla();
        }

        private void xmlDonustur_Tikla(object sender, EventArgs e)
        {
            var dialogResult = openFileDialog.ShowDialog();
            dosyaAdi=Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            var dosyaUzantisi = Path.GetExtension(openFileDialog.FileName);
            if (dosyaUzantisi != null && dosyaUzantisi.ToLower().Equals(".ktb")&&dosyaTip!=DosyaTip.ALNPRF)
            {
                kayitTuruLabel.Visible = false;
                dosyaTuruComboBox.Visible = false;
                dosyaTip = DosyaTip.XML;

                StreamReader ktbStreamReader = new StreamReader(openFileDialog.FileName);
                string shortName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                dosyaAdi = shortName;
                var xmlData = KTBtoXML.GetirXmlData(ktbStreamReader);
                LandXML landXml = KTBtoXML.LandXmlOlustur(shortName, xmlData);

                xmlIcerigi = KTBtoXML.Serialize(landXml);
            }
            else if (dialogResult == DialogResult.OK)
            {
                var landXml=new LandXML();
                kayitTuruLabel.Visible = true;
                dosyaTuruComboBox.Visible = true;
                if (dosyaUzantisi.ToLower().Equals(".ktb"))
                {
                    StreamReader ktbStreamReader = new StreamReader(openFileDialog.FileName);
                    string shortName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    var xmlData = KTBtoXML.GetirXmlData(ktbStreamReader);
                    landXml= KTBtoXML.LandXmlOlustur(shortName, xmlData);
                    dosyaAdi = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    rootPath = Path.GetDirectoryName(openFileDialog.FileName);
                }
                else
                {
                    var serializer = new DosyaDeserializer();
                    landXml = serializer.XmlDeserializer<LandXML>(openFileDialog.FileName);
                }

                dosyaTip = (DosyaTip) dosyaTuruComboBox.SelectedIndex;

                switch (dosyaTip)
                {
                    case DosyaTip.KTB:

                        ktbIcerigi = KTBIcerigi.Duzenleyici(landXml);

                        break;

                    case DosyaTip.ALNPRF:

                        dosyaAdi = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                        rootPath = Path.GetDirectoryName(openFileDialog.FileName);
                        alnIcerigi = ALNIcerigi.Duzenleyici(landXml, dosyaAdi);
                        prfIcerigi = PrfIcerigi.Duzenleyici(landXml.Alignments.Alignment, dosyaAdi);

                        break;
                    case DosyaTip.TBL:

                        lisIcerigi = LISIcerigi.Duzenleyici(landXml, dosyaAdi);

                        break;

                }
            }
            durumTip = DurumTip.Donusturuldu;
            GorunumuAyarla();
        }

        private void GorunumuAyarla()
        {
            switch (durumTip)
            {
                case DurumTip.Hicbiri:
                    break;
                case DurumTip.Basladi:
                    kaydetButton.Enabled = false;
                    break;
                case DurumTip.Donusturuldu:
                    kaydetButton.Enabled = true;
                    donusturButton.Enabled = false;

                    if (openFileDialog.SafeFileName != null)
                    {
                        bilgiLabel.Text = openFileDialog.SafeFileName.Length > 8
                            ? openFileDialog.SafeFileName.Substring(8)
                            : openFileDialog.SafeFileName;
                        bilgiLabel.Text += @" dosyası dönüştürüldü.";
                    }
                    break;
                case DurumTip.Kaydedildi:
                    kaydetButton.Enabled = false;
                    donusturButton.Enabled = true;
                    bilgiLabel.Text = @"Dosya kaydedildi. Yeni dosya dönüştürebilirsiniz.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var rastgeleRenk = Color.FromArgb(_rastgele.Next(256), _rastgele.Next(256), _rastgele.Next(256));
            bilgiLabel.ForeColor = rastgeleRenk;
        }
        private static void DosyaKaydetFiltreIle(string icerik, string filtre, string dosyaadi)
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = dosyaadi,
                Filter = filtre
            };
            saveFileDialog.Title = "Kaydedilecek Dosya Adı Girin";
            if (dosyaadi != null)
            {
                saveFileDialog.FileName = dosyaadi;
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveFileDialog.FileName))
                    sw.WriteLine(icerik);
            }
        }
        private static void AlnPrfDosyasiniKaydet(string alnIcerigi, string prfIcerigi)
        {
            var saveAlnPrfFile = new FolderBrowserDialog
            {
                SelectedPath = rootPath

            };

            if (saveAlnPrfFile.ShowDialog() == DialogResult.OK)
            {
                string alnFileFullPath = saveAlnPrfFile.SelectedPath + "/ALN" + dosyaAdi + ".gsi";
                string prfFileFullPath = saveAlnPrfFile.SelectedPath + "/PRF" + dosyaAdi + ".gsi";

                if (!File.Exists(alnFileFullPath))
                {
                    using (var sw = new StreamWriter(alnFileFullPath))
                        sw.WriteLine(alnIcerigi);
                }
                else
                {
                    var dialogResult = MessageBox.Show(@"Aynı isimde bir ALN dosyası mevcut. Üzerine yazılsın mı?", @"Uyarı", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        using (var sw = new StreamWriter(alnFileFullPath))
                            sw.WriteLine(alnIcerigi);
                    }
                }

                if (!File.Exists(prfFileFullPath))
                {
                    using (var sw = new StreamWriter(prfFileFullPath))
                        sw.WriteLine(prfIcerigi);
                }
                else
                {
                    var dialogResult = MessageBox.Show(@"Aynı isimde bir PRF dosyası mevcut. Üzerine yazılsın mı?", @"Uyarı", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        using (var sw = new StreamWriter(prfFileFullPath))
                            sw.WriteLine(prfIcerigi);
                    }
                }
            }
        }
        private void kaydet_Tikla(object sender, EventArgs e)
        {
            switch (dosyaTip)
            {
                case DosyaTip.KTB:
                    DosyaKaydetFiltreIle(ktbIcerigi, Resources.ktbExtensionFilter, dosyaAdi);
                    break;

                case DosyaTip.ALNPRF:

                    AlnPrfDosyasiniKaydet(alnIcerigi, prfIcerigi);
                    break;

                case DosyaTip.XML:

                    DosyaKaydetFiltreIle(xmlIcerigi, Resources.xmlExtensionFilter, dosyaAdi);
                    break;
                case DosyaTip.TBL:

                    DosyaKaydetFiltreIle(lisIcerigi, Resources.lisExtensionFilter, dosyaAdi);
                    break;
            }
            durumTip = DurumTip.Kaydedildi;
            GorunumuAyarla();
        }
        private void dosyaTuruComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            dosyaTip = (DosyaTip) dosyaTuruComboBox.SelectedIndex;
        }
        private void enkesitButton_Click(object sender, EventArgs e)
        {
            bool aranokta = false;
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> ncnData = GetirNcnData();
            List<Array> kesitler = KSEIcerigi.KseGetir(landXml, ncnData, aranokta);

            string kseIcerigi = KSEIcerigi.NoktalardanEnkesitOlustur(kesitler);
            string listeIcerigi = KSEIcerigi.EnkesitListesiOlustur(kesitler);
            if (enkestlistesiBox.Checked == true) //Eğer checkbox seçiliyse
            {
                DosyaKaydetFiltreIle(listeIcerigi, Resources.lisExtensionFilter, "KSE");
            }


            DosyaKaydetFiltreIle(kseIcerigi, Resources.kseExtensionFilter, "KSE");
        }
        private void daire_enkesitButton_Click(object sender, EventArgs e)
        {
            bool aranokta = true;
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> ncnData = GetirNcnData();
            List<Array> kesitler = KSEIcerigi.KseGetir(landXml, ncnData, aranokta);

            string kseIcerigi = KSEIcerigi.NoktalardanEnkesitOlustur(kesitler);


            DosyaKaydetFiltreIle(kseIcerigi, Resources.kseExtensionFilter, "KSE");

        }
        private void tipKesitButton_Click(object sender, EventArgs e)
        {
            LandXML guzergah = GetirLandXmlDosyaTipiIle();
            List<Array> kmData = GetirKmListeDosyasi();
            DXFLib.DXFDocument dxfdosya = this.getirDxfDocument();

            string kseIcerigi = KSEIcerigi.TipkesittenEnkesitOlustur(dxfdosya, kmData, guzergah);

            DosyaKaydetFiltreIle(kseIcerigi, Resources.kseExtensionFilter, "KSE");
        }
        private void gabariButton_Click(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> kmData = GetirKmListeDosyasi();

            string kseIcerigi = KSEIcerigi.GabariKesitleriOlustur(kmData, landXml);

            DosyaKaydetFiltreIle(kseIcerigi, Resources.kseExtensionFilter, "KSE");
        }
        private void guzerhah_tablo_click(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> ncnData = GetirNcnData();
            List<Array> kesitler = GzgIcerigi.GzgGetir(landXml, ncnData);

            string GzhIcerigi = GzgIcerigi.NoktalardanTabloOlustur(kesitler);


            DosyaKaydetFiltreIle(GzhIcerigi, Resources.lisExtensionFilter, "TBL");
        }
        private void elKitabiXML_click(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> ncnData = GetirNcnData();
            List<Array> kesitler = TcddteknikIcerigi.GzgGetir(landXml, ncnData);

            LandXML ekliLandXml = TcddteknikIcerigi.deverprofil(landXml, kesitler);///// yeni dosya olacak xml kaydeden
            xmlIcerigi = KTBtoXML.Serialize(landXml);
            DosyaKaydetFiltreIle(xmlIcerigi, Resources.xmlExtensionFilter, "xml");

        }
        private void dxftoXML_click(object sender, EventArgs e)
        {
            DialogResult secenek = MessageBox.Show("NETCAD'de DXF Kaydeilmiş Ücgen Model Dosyasındaki 3DPolyline ları XML kaydeder." +
                                                   "\t\n \t\n Seçilecek DXF Dosyası 3DPolyline lardan mı oluşuyor?", "Bilgilendirme Penceresi", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (secenek == DialogResult.Yes)
            {
                DXFLib.DXFDocument doc = getirDxfDocument();
                DateTime start = DateTime.UtcNow;
                // Parse DXF

                // Process entities
                if (doc.Entities.Count > 0)
                {
                    // Create shapes
                    start = DateTime.UtcNow;
                    List<Nokta> nokList = new List<Nokta>();
                    List<Ucgen> ucgenList = new List<Ucgen>();
                    int noktano = 2;
                    foreach (DXFLib.DXFEntity entity in doc.Entities)
                    {
                        if (entity.EntityType == "DXFPolyLine")
                        {

                            DXFLib.DXFVertex vertex0 = new DXFLib.DXFVertex();
                            DXFLib.DXFVertex vertex1 = new DXFLib.DXFVertex();
                            DXFLib.DXFVertex vertex2 = new DXFLib.DXFVertex();
                            DXFLib.DXFPolyLine polyLine = (DXFLib.DXFPolyLine) entity;
                            bool isClosed = polyLine.Flags == DXFLib.DXFPolyLine.FlagsEnum.closed;

                            vertex0 = (DXFLib.DXFVertex) polyLine.Children[0];
                            vertex1 = (DXFLib.DXFVertex) polyLine.Children[1];
                            vertex2 = (DXFLib.DXFVertex) polyLine.Children[2];

                            noktano = noktano + 3;
                            ucgenList.Add(new Ucgen { Numara2 = noktano, Numara1 = noktano + 1, Numara0 = noktano + 2 });
                            nokList.Add(new Nokta { Numara = noktano, X = vertex0.Location.Y, Y = vertex0.Location.X, Z = vertex0.Location.Z });
                            nokList.Add(new Nokta { Numara = noktano + 1, X = vertex1.Location.Y, Y = vertex1.Location.X, Z = vertex1.Location.Z });
                            nokList.Add(new Nokta { Numara = noktano + 2, X = vertex2.Location.Y, Y = vertex2.Location.X, Z = vertex2.Location.Z });
                        }
                        if (entity.EntityType == "POLYLINE")
                        {

                            DXFLib.DXFVertex vertex0 = new DXFLib.DXFVertex();
                            DXFLib.DXFVertex vertex1 = new DXFLib.DXFVertex();
                            DXFLib.DXFVertex vertex2 = new DXFLib.DXFVertex();
                            DXFLib.DXFPolyLine polyLine = (DXFLib.DXFPolyLine) entity;
                            bool isClosed = polyLine.Flags == DXFLib.DXFPolyLine.FlagsEnum.closed;

                            vertex0 = (DXFLib.DXFVertex) polyLine.Children[0];
                            vertex1 = (DXFLib.DXFVertex) polyLine.Children[1];
                            vertex2 = (DXFLib.DXFVertex) polyLine.Children[2];

                            noktano = noktano + 3;
                            ucgenList.Add(new Ucgen { Numara2 = noktano, Numara1 = noktano + 1, Numara0 = noktano + 2 });
                            nokList.Add(new Nokta { Numara = noktano, X = vertex0.Location.Y, Y = vertex0.Location.X, Z = vertex0.Location.Z });
                            nokList.Add(new Nokta { Numara = noktano + 1, X = vertex1.Location.Y, Y = vertex1.Location.X, Z = vertex1.Location.Z });
                            nokList.Add(new Nokta { Numara = noktano + 2, X = vertex2.Location.Y, Y = vertex2.Location.X, Z = vertex2.Location.Z });
                        }

                        if (entity.EntityType =="3DFACE")
                        {
                            DXFLib.DXF3DFace dxf3DFace = (DXFLib.DXF3DFace) entity;
                            noktano = noktano + 3;
                            ucgenList.Add(new Ucgen { Numara2 = noktano, Numara1 = noktano + 1, Numara0 = noktano + 2 });

                            nokList.Add(new Nokta { Numara = noktano, X = dxf3DFace.Corners[0].Y, Y = dxf3DFace.Corners[0].X, Z = dxf3DFace.Corners[0].Z });
                            nokList.Add(new Nokta { Numara = noktano+1, X = dxf3DFace.Corners[1].Y, Y = dxf3DFace.Corners[1].X, Z = dxf3DFace.Corners[1].Z });
                            nokList.Add(new Nokta { Numara = noktano+2, X = dxf3DFace.Corners[2].Y, Y = dxf3DFace.Corners[2].X, Z = dxf3DFace.Corners[2].Z });


                        }

                        

                    }
                    System.Diagnostics.Debug.WriteLine("Created shapes in {0}ms", DateTime.UtcNow.Subtract(start).TotalMilliseconds);
                    yuzeyXML.yuzeyXmlOlustur(nokList, ucgenList, doc.Filename);
                }
            }
            else if (secenek == DialogResult.No)
            {
                //Hayır seçeneğine tıklandığında çalıştırılacak kodlar
            }
            
        }
        private void JsontoDXF_click(object sender, EventArgs e)
        {
            DialogResult secenek = MessageBox.Show("Parsel Sorguda JSON Kaydeilmiş Ücgen Model Dosyasındaki Polyline ları DXF kaydeder." +
                                                   "\t\n \t\n Seçilecek JSON Dosyası 3DPolyline lardan mı oluşuyor?", "Bilgilendirme Penceresi", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (secenek == DialogResult.Yes)
            {
                DateTime start = DateTime.UtcNow;
                // Parse DXF
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Json Dosyası Seçin";
                dlg.Filter = "JSON (*.json)|*.json";
                string doc = null;
                RootObject data = null;
                List<Feature> datafeatures = null;

                netDxf.DxfDocument dxfdosya = new netDxf.DxfDocument();
                Layer customlayer = new Layer("Kadastro_parsel")
                {
                    Color = AciColor.Red
                };
                dxfdosya.Layers.Add(customlayer);

                if (DialogResult.OK == dlg.ShowDialog())
                {
                    var filename = dlg.FileName;
                    if (!string.IsNullOrEmpty(filename))
                    {
                        // Parse DXF
                        doc = File.ReadAllText(filename);

                        data = JsonSerializer.Deserialize<RootObject>(doc);
                        datafeatures = data.Features;
                        var merkeznokta = datafeatures[1].Geometry.Coordinates.Values;
                        foreach (var item in datafeatures)
                        {
                            double ortalamaX = 0;
                            double ortalamaY = 0;
                            var objetipi = item.Geometry.Type;
                            if(objetipi== "Polygon")
                            {

                                List<Vector2> vertices= new List<Vector2>();
                                
                                    foreach (var noktalar in item.Geometry.Coordinates)
                                    {
                                        var değerler = noktalar.Value;
                                        foreach (var koordinatlar in değerler)
                                        {
                                        Vector2 array = new Vector2();
                                        array.X = koordinatlar.Value.Longitude;
                                        array.Y = koordinatlar.Value.Latitude;
                                        vertices.Add(array);
                                        }
                                    }
                                ortalamaX = vertices.Average(k => k.X);
                                ortalamaY = vertices.Average(k => k.Y);
                                netDxf.Entities.Polyline2D polyline2D= new Polyline2D(vertices,isClosed:true);
                                dxfdosya.Entities.Add(polyline2D);
                            }
                            var adano = item.Properties.AdaNo;
                            var parselNo = item.Properties.ParselNo;
                            double textheigth = 1.5;
                            Vector3 textposition = new Vector3(ortalamaX, ortalamaY, 0);
                            netDxf.Entities.Text textEntity = new netDxf.Entities.Text((adano+"/"+parselNo), textposition, textheigth);
                            textEntity.Rotation = 0;
                            dxfdosya.Entities.Add(textEntity);
                        }
                        // Process entities
                    }
                }
                //DXF KAYDET
                var dosyaadi = "objectoutput";
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = "objectoutput",
                    Filter = Resources.dxfExtensionFilter
                };
                saveFileDialog.Title = "Kaydedilecek Dosya Adı Girin";
                if (dosyaadi != null)
                {
                    saveFileDialog.FileName = dosyaadi;
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    dxfdosya.Save(saveFileDialog.FileName);
                }

            }
            else if (secenek == DialogResult.No)
            {
                //Hayır seçeneğine tıklandığında çalıştırılacak kodlar
            }

        }
        private void KoordinatHesapla(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> kmData = GetirKmListeDosyasi();

            var ncnIcerigi = KmdenKoorHesapla.KoordinatHesabi(kmData, landXml);

            DosyaKaydetFiltreIle(ncnIcerigi, Resources.ncnExtensionFilter, "Koorliste");
        }
        private void OfsetKotHesapla(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> ncnData = GetirNcnData();
            string kesitler = OfsetVeKoticerigi.OfsetVeKotGetir(landXml, ncnData);

            //string listeIcerigi = KSEIcerigi.EnkesitListesiOlustur(kesitler);
            DosyaKaydetFiltreIle(kesitler, Resources.ofkExtensionFilter, "OfsetKotliste");
        }
        private DXFLib.DXFDocument getirDxfDocument()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "DXF Dosyası Seçin";
            dlg.Filter = "DXF (*.dxf)|*.dxf";
            DXFLib.DXFDocument doc = null;

            if (DialogResult.OK == dlg.ShowDialog())
            {
                var filename = dlg.FileName;
                if (!string.IsNullOrEmpty(filename))
                {
                    // Parse DXF
                    DateTime start = DateTime.UtcNow;
                    doc = new DXFLib.DXFDocument();

                    doc.Load(filename);
                    doc.Filename = filename;

                    System.Diagnostics.Debug.WriteLine("Loaded {0} in {1}ms", System.IO.Path.GetFileName(filename), DateTime.UtcNow.Subtract(start).TotalMilliseconds);

                    // Process entities
                }
            }

            return doc;
        }
        private void tcddteknik_tablo_click(object sender, EventArgs e)
        {
            LandXML landXml = GetirLandXmlDosyaTipiIle();
            List<Array> ncnData = GetirNcnData();
            List<Array> kesitler = TcddteknikIcerigi.GzgGetir(landXml, ncnData);

            string TcddIcerigi = TcddteknikIcerigi.NoktalardanTabloOlustur(kesitler);


            DosyaKaydetFiltreIle(TcddIcerigi, Resources.lisExtensionFilter, "TBL");
        }
        private LandXML GetirLandXmlDosyaTipiIle()
        {
            openFileDialog.Title = "Güzergah Dosyasını Seçin";
            openFileDialog.ShowDialog();

            var dosyaUzantisi = Path.GetExtension(openFileDialog.FileName);
            LandXML landXml = new LandXML();

            if (dosyaUzantisi != null && dosyaUzantisi.ToLower().Equals(".ktb"))
            {
                StreamReader ktbStreamReader = new StreamReader(openFileDialog.FileName);
                string shortName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                var xmlData = KTBtoXML.GetirXmlData(ktbStreamReader);
                landXml = KTBtoXML.LandXmlOlustur(shortName, xmlData);
            }
            else if (dosyaUzantisi != null && dosyaUzantisi.ToLower().Equals(".xml"))
            {
                var serializer = new DosyaDeserializer();
                landXml = serializer.XmlDeserializer<LandXML>(openFileDialog.FileName);
            }

            return landXml;
        }
        private List<Array> GetirNcnData()        {
            ncnOpenFileDialog.Title = "NCN Türü Nokta Dosyası Seçin";
            ncnOpenFileDialog.ShowDialog();
            StreamReader ncnIcerigi = new StreamReader(ncnOpenFileDialog.FileName);
            List<Array> tumsatirlar = new List<Array>();
            while (!ncnIcerigi.EndOfStream)
            {
                char[] separator = { ' ', '\t' };
                var veriSatir = ncnIcerigi.ReadLine();
                if (veriSatir != null)
                {
                    var veriKelimeleri = veriSatir.Split(separator, (StringSplitOptions) 1);
                    tumsatirlar.Add(veriKelimeleri);
                }
            }
            return tumsatirlar;
        }
        private List<Array> GetirGSIData()
        {
            OpenFileDialog file = new OpenFileDialog();
            file.FilterIndex = 0;
            file.DefaultExt = "gsi";
            file.Title = "Nivelman GSI Dosyası Seçiniz..";
            file.Filter = "GSI Dosyası |*.gsi";
            if (file.ShowDialog() == DialogResult.OK)
            {

            }
            StreamReader GsiIcerigi = new StreamReader(file.FileName);
            List<Array> tumsatirlar = new List<Array>();
            //StreamReader lisIcerigi = new StreamReader(LisopenFileDialog.FileName);

            while (!GsiIcerigi.EndOfStream)
            {
                char[] separator = { ' ', '\t' };
                var veriSatir = GsiIcerigi.ReadLine();
                if (veriSatir != null)
                {
                    var veriKelimeleri = veriSatir.Split(separator, (StringSplitOptions) 1);
                    tumsatirlar.Add(veriKelimeleri);
                }
            }

            return tumsatirlar;

        }
        private List<Array> GetirKmListeDosyasi()
        {
            LisopenFileDialog.Title = "Kilometre Liste Dosyası Seçin";
            LisopenFileDialog.ShowDialog();
            StreamReader lisIcerigi = new StreamReader(LisopenFileDialog.FileName);

            List<Array> kmSatirlar = new List<Array>();

            while (!lisIcerigi.EndOfStream)
            {
                char[] separator = { ' ', '\t' };
                var veriSatir = lisIcerigi.ReadLine();
                if (veriSatir != null)
                {
                    var veriKelimeleri = veriSatir.Split(separator, (StringSplitOptions) 1);
                    kmSatirlar.Add(veriKelimeleri);
                }
            }

            return kmSatirlar;
        }
        private void MDT_button_Click(object sender, EventArgs e)
        {
            List<Array> mdtData = GetirMdtData();
            string liste = MDT_icerigi.Excellistesi(mdtData);
            DosyaKaydetFiltreIle(liste, Resources.TXTExtensionFilter, "Nivelmanliste");
        }
        private void GSIoku_Click(object sender, EventArgs e)
        {
            List<Array> GsiData = GetirGSIData();
            // TODO yüklenen GSI dosyası text formatında boşlukla ayrılmış olarak kayıt edilecek..
            // Buraya kod yazılacak pull için yazdım.tekas
        }
        private List<Array> GetirMdtData()
        {
            mdtOpenFileDialog.Title = "MDT Türü Nokta Nivelman Dosyası Seçin";
            mdtOpenFileDialog.ShowDialog();
            StreamReader MdtIcerigi = new StreamReader(mdtOpenFileDialog.FileName);
            List<Array> tumsatirlar = new List<Array>();
            while (!MdtIcerigi.EndOfStream)
            {
                char[] separator = { '\t' };
                var veriSatir = MdtIcerigi.ReadLine();
                if (veriSatir != null)
                {
                    var veriKelimeleri = veriSatir.Split(separator, (StringSplitOptions) 1);
                    tumsatirlar.Add(veriKelimeleri);
                }
            }
            return tumsatirlar;
        }
        private void XmlToKtbUI_Load(object sender, EventArgs e)
        {

        }
        private void cizimEkraniButton_Click(object sender, EventArgs e)
        {
            var cizimEkrani = new CizimEkrani();
            cizimEkrani.Show();
        }
        private void deverdenemesil_Click_1(object sender, EventArgs e)
        {
            Dever.Devergoster();
        }
        private void Geodosyasi_olusturma(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.FilterIndex = 0;
            file.DefaultExt = "xml";
            file.Title = "Güzergah XML Dosyası Seçiniz..";
            file.Filter = "XML Dosyası |*.xml";
            if (file.ShowDialog() == DialogResult.OK)
            {

            }

            var serializer = new DosyaDeserializer();
            var landXml = serializer.XmlDeserializer<LandXML>(file.FileName);
            geoIcerigi= GeoCevir.Duzenleyici(landXml);
            DosyaKaydetFiltreIle(geoIcerigi, Resources.geoExtensionFilter, "GEO");
        }
        private void DXFdosyasi_kaydet(DXFLib.DXFDocument dxfdosya)
        {
            var texett = new DXFLib.DXFHeader();
            texett = dxfdosya.Header;
            var text2=texett.ToString();
            DosyaKaydetFiltreIle(text2, Resources.dxfExtensionFilter, "DXF");
        }

        private void nivelmantabkontrol_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }

}