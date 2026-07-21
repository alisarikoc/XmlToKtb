using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
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
            cizimPaneli1.AracDegisti += CizimPaneli1_AracDegisti;
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
                if (!(nesne is CizgiNesnesi cizgi))
                    continue;

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

            cizimPaneli1.TumObjeleriGoster();
            cizimPaneli1.Invalidate();
        }

        private void CizimPaneli1_AracDegisti(object sender, CizimAraci araci)
        {
            secAraciButton.Checked = araci == CizimAraci.Sec;
            cizgiAraciButton.Checked = araci == CizimAraci.Cizgi;
        }

        private void secAraciButton_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Sec;
        }

        private void cizgiAraciButton_Click(object sender, EventArgs e)
        {
            cizimPaneli1.AktifArac = CizimAraci.Cizgi;
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
            cizimPaneli1.AktifYakalamaModu = mod;
        }

        private void girisEkraniMenuItem_Click(object sender, EventArgs e)
        {
            var girisEkrani = new XmlToKtbUI();
            girisEkrani.Show();
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

                foreach (var dogru in dxfBelge.Entities.Lines)
                    CizgiEkle(dogru.StartPoint.X, dogru.StartPoint.Y, dogru.EndPoint.X, dogru.EndPoint.Y);

                foreach (var polyline in dxfBelge.Entities.Polylines2D)
                {
                    var vertexler = polyline.Vertexes;
                    for (var i = 0; i < vertexler.Count - 1; i++)
                        CizgiEkle(vertexler[i].Position.X, vertexler[i].Position.Y, vertexler[i + 1].Position.X, vertexler[i + 1].Position.Y);
                    if (polyline.IsClosed && vertexler.Count > 1)
                        CizgiEkle(vertexler[vertexler.Count - 1].Position.X, vertexler[vertexler.Count - 1].Position.Y, vertexler[0].Position.X, vertexler[0].Position.Y);
                }

                foreach (var polyline in dxfBelge.Entities.Polylines3D)
                {
                    var vertexler = polyline.Vertexes;
                    for (var i = 0; i < vertexler.Count - 1; i++)
                        CizgiEkle(vertexler[i].X, vertexler[i].Y, vertexler[i + 1].X, vertexler[i + 1].Y);
                    if (polyline.IsClosed && vertexler.Count > 1)
                        CizgiEkle(vertexler[vertexler.Count - 1].X, vertexler[vertexler.Count - 1].Y, vertexler[0].X, vertexler[0].Y);
                }

                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }

        private void CizgiEkle(double x1, double y1, double x2, double y2)
        {
            cizimPaneli1.Nesneler.Add(new CizgiNesnesi { BaslangicX = x1, BaslangicY = y1, BitisX = x2, BitisY = y2 });
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
                            new Vector2(cizgi.BaslangicX, cizgi.BaslangicY),
                            new Vector2(cizgi.BitisX, cizgi.BitisY)));
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
                }

                cizimPaneli1.TumObjeleriGoster();
                cizimPaneli1.Invalidate();
            }
        }
    }
}
