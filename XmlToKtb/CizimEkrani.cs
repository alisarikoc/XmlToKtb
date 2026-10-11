using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Common;
using Common.Common;
using netDxf;
using netDxf.Entities;
using XmlToKtb.Cizim;
using XmlToKtb.Projeksiyon;

namespace XmlToKtb
{
    public partial class CizimEkrani : Form
    {
        private ProjeksiyonSistemi _aktifSistem = ProjeksiyonSistemi.Tanimsiz;
        private DilimBilgisi? _aktifDilim;

        public CizimEkrani()
        {
            InitializeComponent();
            cizimPaneli1.SecimDegisti += CizimPaneli1_SecimDegisti;
            cizimPaneli1.FareKonumuDegisti += CizimPaneli1_FareKonumuDegisti;
        }

        private void CizimPaneli1_SecimDegisti(object sender, CizimNesnesi secilenNesne)
        {
            ozellikGrid.SelectedObject = secilenNesne;
        }

        private void CizimPaneli1_FareKonumuDegisti(object sender, System.Drawing.PointF dunyaKonumu)
        {
            konumDurumLabel.Text = $"X: {dunyaKonumu.X:0.000}   Y: {dunyaKonumu.Y:0.000}";
        }

        private void koordinatSistemiLabel_Click(object sender, EventArgs e)
        {
            using (var dlg = new KoordinatSistemiForm(_aktifSistem, _aktifDilim))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                var yeniSistem = dlg.SecilenSistem;
                var yeniDilim = dlg.SecilenDilim;
                var ilkTanim = _aktifSistem == ProjeksiyonSistemi.Tanimsiz;
                var degisti = yeniSistem != _aktifSistem || !Equals(yeniDilim, _aktifDilim);

                if (degisti && !ilkTanim && yeniSistem != ProjeksiyonSistemi.Tanimsiz)
                {
                    var cevap = MessageBox.Show(
                        "Koordinat sistemi değişti.\n\n" +
                        "Evet: çizimdeki mevcut koordinatlar yeni sisteme dönüştürülür.\n" +
                        "Hayır: koordinatlar aynı kalır, sadece sistem etiketi değişir.",
                        "Koordinat Sistemi Değişti", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (cevap == DialogResult.Yes)
                        CizimiDonustur(_aktifSistem, _aktifDilim, yeniSistem, yeniDilim);
                }

                _aktifSistem = yeniSistem;
                _aktifDilim = yeniDilim;
                koordinatSistemiLabel.Text = ProjeksiyonBilgisi.GoruntuAdiGetir(_aktifSistem);
            }
        }

        private void CizimiDonustur(ProjeksiyonSistemi eskiSistem, DilimBilgisi? eskiDilim, ProjeksiyonSistemi yeniSistem, DilimBilgisi? yeniDilim)
        {
            foreach (var nesne in cizimPaneli1.Nesneler)
            {
                if (nesne is CizgiNesnesi cizgi)
                {
                    if (KoordinatDonusturucu.Donustur(eskiSistem, eskiDilim, cizgi.BaslangicX, cizgi.BaslangicY, yeniSistem, yeniDilim, out var bx, out var by))
                    {
                        cizgi.BaslangicX = bx;
                        cizgi.BaslangicY = by;
                    }

                    if (KoordinatDonusturucu.Donustur(eskiSistem, eskiDilim, cizgi.BitisX, cizgi.BitisY, yeniSistem, yeniDilim, out var ex, out var ey))
                    {
                        cizgi.BitisX = ex;
                        cizgi.BitisY = ey;
                    }
                }
                else if (nesne is YaziNesnesi yazi)
                {
                    if (KoordinatDonusturucu.Donustur(eskiSistem, eskiDilim, yazi.KonumX, yazi.KonumY, yeniSistem, yeniDilim, out var yx, out var yy))
                    {
                        yazi.KonumX = yx;
                        yazi.KonumY = yy;
                    }
                }
            }

            cizimPaneli1.TumObjeleriGoster();
            cizimPaneli1.Invalidate();
        }

        private void cizgiCizMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Cizgi;
        }

        private void daireMerkezYaricapMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.DaireMerkezYaricap;
        }

        private void daire3NoktaMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Daire3Nokta;
        }

        private void daire2NoktaYaricapMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Daire2NoktaYaricap;
        }

        private void daireTegetIkiObjeMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.DaireTegetIkiObje;
        }

        private void daireTegetUcObjeMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.DaireTegetUcObje;
        }

        private void guzergahXmlCizMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Güzergah XML Dosyası Seçin", Filter = "XML (*.xml)|*.xml" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                LandXML landXml;
                try
                {
                    var serializer = new DosyaDeserializer();
                    landXml = serializer.XmlDeserializer<LandXML>(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("XML dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var nesneler = GuzergahCizici.Olustur(landXml);
                if (nesneler.Count == 0)
                {
                    MessageBox.Show("Dosyada bir güzergah ekseni (Alignment) bulunamadı.", "Güzergah Çiz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                cizimPaneli1.Nesneler.AddRange(nesneler);
                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }

        private void kaydirMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kaydir;
        }

        private void dondurMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Dondur;
        }

        private void kesMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kes;
        }

        private void kopyalaMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Kopyala;
        }

        private void olcekleMenuItem_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Olcekle;
        }

        private void ozellikGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            cizimPaneli1.SeciliNesneGuncellendi();
        }

        private void yakalamaButonu_CheckedChanged(object sender, EventArgs e)
        {
            var mod = YakalamaModu.Yok;
            if (noktaYakalaButton.Checked) mod |= YakalamaModu.Nokta;
            if (ucYakalaButton.Checked) mod |= YakalamaModu.Uc;
            if (ortaYakalaButton.Checked) mod |= YakalamaModu.Orta;
            if (dikYakalaButton.Checked) mod |= YakalamaModu.Dik;
            if (merkezYakalaButton.Checked) mod |= YakalamaModu.Merkez;
            cizimPaneli1.AktifYakalamaModu = mod;
        }

        private void girisEkraniMenuItem_Click(object sender, EventArgs e)
        {
            var girisEkrani = new XmlToKtbUI();
            girisEkrani.Show();
        }

        private void gorunumHassasiyetiMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new GorunumAyarlariForm(GorunumAyarlari.OndalikBasamakSayisi))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                GorunumAyarlari.OndalikBasamakSayisi = dlg.OndalikBasamakSayisi;

                // Özellikler panelindeki hücreleri yeni hassasiyetle yeniden biçimlendirmek için gridi zorla yeniler.
                var seciliNesne = ozellikGrid.SelectedObject;
                ozellikGrid.SelectedObject = null;
                ozellikGrid.SelectedObject = seciliNesne;
            }
        }

        private void dosyaAcMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "DXF Dosyası Seçin", Filter = "DXF (*.dxf)|*.dxf" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                DxfDocument dxfBelge;
                try
                {
                    dxfBelge = DxfDocument.Load(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (dxfBelge == null)
                {
                    MessageBox.Show("DXF dosyası okunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var nesneler = DxfIceAktarici.Ayikla(dxfBelge);
                cizimPaneli1.Nesneler.AddRange(nesneler);

                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }

        private void dosyaKaydetMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog { Title = "DXF Olarak Kaydet", Filter = "DXF (*.dxf)|*.dxf", FileName = "cizim.dxf" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                var dxfBelge = new DxfDocument();
                foreach (var nesne in cizimPaneli1.Nesneler)
                {
                    if (nesne is CizgiNesnesi cizgi)
                    {
                        dxfBelge.Entities.Add(new Line(
                            new Vector3(cizgi.BaslangicX, cizgi.BaslangicY, cizgi.BaslangicZ),
                            new Vector3(cizgi.BitisX, cizgi.BitisY, cizgi.BitisZ)));
                    }
                    else if (nesne is YaziNesnesi yazi && !string.IsNullOrEmpty(yazi.Metin))
                    {
                        // DXF TEXT tek satırlıdır; metindeki satır sonları dosya yapısını bozmasın diye boşlukla değiştirilir.
                        var dxfMetin = yazi.Metin.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
                        var metinNesnesi = new Text(dxfMetin, new Vector3(yazi.KonumX, yazi.KonumY, yazi.KonumZ), yazi.Yukseklik)
                        {
                            Rotation = yazi.Donus
                        };
                        dxfBelge.Entities.Add(metinNesnesi);
                    }
                    else if (nesne is NoktaNesnesi nokta)
                    {
                        dxfBelge.Entities.Add(new netDxf.Entities.Point(new Vector3(nokta.X, nokta.Y, nokta.Z)));
                    }
                    else if (nesne is DaireNesnesi daire)
                    {
                        dxfBelge.Entities.Add(new Circle(new Vector3(daire.MerkezX, daire.MerkezY, daire.MerkezZ), daire.Yaricap));
                    }
                    else if (nesne is YayNesnesi yay)
                    {
                        dxfBelge.Entities.Add(new Arc(new Vector3(yay.MerkezX, yay.MerkezY, yay.MerkezZ), yay.Yaricap, yay.BaslangicAcisi, yay.BitisAcisi));
                    }
                    else if (nesne is SpiralNesnesi spiral)
                    {
                        // DXF'te klotoid için özel bir varlık yok; örneklenmiş noktalarla (gerçek Z'leriyle) 3B çizgi dizisi (Polyline3D) olarak yazılır.
                        var vertices = spiral.OrnekNoktalariVeZGetir().Select(p => new Vector3(p.Nokta.X, p.Nokta.Y, p.Z)).ToList();
                        if (vertices.Count >= 2)
                            dxfBelge.Entities.Add(new Polyline3D(vertices, isClosed: false));
                    }
                }

                try
                {
                    dxfBelge.Save(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DXF dosyası kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void googleEarthAktarMenuItem_Click(object sender, EventArgs e)
        {
            if (_aktifSistem == ProjeksiyonSistemi.Tanimsiz)
            {
                MessageBox.Show(
                    "Google Earth'e aktarmak için önce koordinat sistemini tanımlamanız gerekir (durum çubuğundaki 'Tanımsız' yazısına tıklayın).",
                    "Koordinat Sistemi Tanımsız", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var kml = GoogleEarthAktarici.KmlOlustur(cizimPaneli1.Nesneler, _aktifSistem, _aktifDilim);
            if (kml == null)
            {
                MessageBox.Show("Aktarılacak (koordinat sistemine göre dönüştürülebilir) obje bulunamadı.", "Google Earth'e Aktar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Title = "Google Earth Olarak Kaydet", Filter = "KMZ (*.kmz)|*.kmz|KML (*.kml)|*.kml", FileName = "cizim.kmz" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    if (Path.GetExtension(dlg.FileName).Equals(".kmz", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var dosyaAkisi = new FileStream(dlg.FileName, FileMode.Create))
                        using (var arsiv = new ZipArchive(dosyaAkisi, ZipArchiveMode.Create))
                        {
                            var girdi = arsiv.CreateEntry("doc.kml");
                            using (var yazici = new StreamWriter(girdi.Open(), new UTF8Encoding(false)))
                                yazici.Write(kml);
                        }
                    }
                    else
                    {
                        File.WriteAllText(dlg.FileName, kml, new UTF8Encoding(false));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void googleEarthIceAktarMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Google Earth Dosyası Seçin", Filter = "KML/KMZ (*.kml;*.kmz)|*.kml;*.kmz|KMZ (*.kmz)|*.kmz|KML (*.kml)|*.kml" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                List<CizimNesnesi> nesneler;
                try
                {
                    // Mevcut koordinat sisteminden bağımsız olarak, KML/KMZ'deki enlem/boylam doğrudan çizim
                    // ekranı koordinatı olarak alınır; içe aktarımdan sonra aktif sistem Coğrafi'ye ayarlanır.
                    nesneler = GoogleEarthIceAktarici.DosyadanOku(dlg.FileName, ProjeksiyonSistemi.Cografi, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (nesneler.Count == 0)
                {
                    MessageBox.Show("Dosyada aktarılabilecek bir obje bulunamadı.", "Google Earth İçe Aktar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                cizimPaneli1.Nesneler.AddRange(nesneler);
                _aktifSistem = ProjeksiyonSistemi.Cografi;
                _aktifDilim = null;
                koordinatSistemiLabel.Text = ProjeksiyonBilgisi.GoruntuAdiGetir(_aktifSistem);
                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }

        private void jsonIceAktarMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "JSON Dosyası Seçin", Filter = "JSON (*.json)|*.json" })
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                RootObject data;
                try
                {
                    var metin = File.ReadAllText(dlg.FileName);
                    data = JsonSerializer.Deserialize<RootObject>(metin);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("JSON dosyası okunamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (data?.Features == null)
                    return;

                foreach (var feature in data.Features)
                {
                    if (feature.Geometry?.Type != "Polygon" || feature.Geometry.Coordinates == null)
                        continue;

                    var koseNoktalari = new List<PointF>();
                    foreach (var halka in feature.Geometry.Coordinates.Values)
                    foreach (var koordinat in halka.Values)
                        koseNoktalari.Add(new PointF((float)koordinat.Longitude, (float)koordinat.Latitude));

                    if (koseNoktalari.Count < 2)
                        continue;

                    for (var i = 0; i < koseNoktalari.Count; i++)
                    {
                        var bas = koseNoktalari[i];
                        var bit = koseNoktalari[(i + 1) % koseNoktalari.Count];
                        cizimPaneli1.Nesneler.Add(new CizgiNesnesi
                        {
                            BaslangicX = bas.X,
                            BaslangicY = bas.Y,
                            BitisX = bit.X,
                            BitisY = bit.Y
                        });
                    }

                    var etiket = $"{feature.Properties?.AdaNo}/{feature.Properties?.ParselNo}";
                    cizimPaneli1.Nesneler.Add(new YaziNesnesi
                    {
                        KonumX = koseNoktalari.Average(p => p.X),
                        KonumY = koseNoktalari.Average(p => p.Y),
                        Metin = etiket,
                        Yukseklik = 1.5
                    });
                }

                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }
    }
}
