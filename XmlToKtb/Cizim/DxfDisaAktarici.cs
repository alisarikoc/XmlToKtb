using System.Collections.Generic;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace XmlToKtb.Cizim
{
    /// <summary>Çizim ekranındaki objeleri DXF dosyasına yazar. Objelerin tabakası ve kendi rengi de yazılır.</summary>
    public static class DxfDisaAktarici
    {
        public static void Kaydet(IEnumerable<CizimNesnesi> nesneler, string dosyaYolu)
        {
            var dxfBelge = new DxfDocument();
            var katmanlar = new Dictionary<Tabaka, Layer>();

            // Varlığı, objenin tabakasında ve (varsa) kendi rengiyle belgeye ekler.
            void Ekle(EntityObject varlik, CizimNesnesi nesne)
            {
                if (nesne.Tabaka != null)
                {
                    if (!katmanlar.TryGetValue(nesne.Tabaka, out var katman))
                    {
                        katman = KatmanOlustur(nesne.Tabaka);
                        katmanlar[nesne.Tabaka] = katman;
                    }
                    if (katman != null)
                        varlik.Layer = katman;
                }

                if (nesne.OzelRenk != null)
                    varlik.Color = new AciColor(nesne.OzelRenk.Value);

                dxfBelge.Entities.Add(varlik);
            }

            foreach (var nesne in nesneler)
            {
                if (nesne is CizgiNesnesi cizgi)
                {
                    Ekle(new Line(
                        new Vector3(cizgi.BaslangicX, cizgi.BaslangicY, cizgi.BaslangicZ),
                        new Vector3(cizgi.BitisX, cizgi.BitisY, cizgi.BitisZ)), nesne);
                }
                else if (nesne is YaziNesnesi yazi && !string.IsNullOrEmpty(yazi.Metin))
                {
                    // DXF TEXT tek satırlıdır; metindeki satır sonları dosya yapısını bozmasın diye boşlukla değiştirilir.
                    var dxfMetin = yazi.Metin.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
                    var metinNesnesi = new Text(dxfMetin, new Vector3(yazi.KonumX, yazi.KonumY, yazi.KonumZ), yazi.Yukseklik)
                    {
                        Rotation = yazi.Donus
                    };
                    Ekle(metinNesnesi, nesne);
                }
                else if (nesne is NoktaNesnesi nokta)
                {
                    Ekle(new Point(new Vector3(nokta.X, nokta.Y, nokta.Z)), nesne);
                }
                else if (nesne is DaireNesnesi daire)
                {
                    Ekle(new Circle(new Vector3(daire.MerkezX, daire.MerkezY, daire.MerkezZ), daire.Yaricap), nesne);
                }
                else if (nesne is YayNesnesi yay)
                {
                    Ekle(new Arc(new Vector3(yay.MerkezX, yay.MerkezY, yay.MerkezZ), yay.Yaricap, yay.BaslangicAcisi, yay.BitisAcisi), nesne);
                }
                else if (nesne is SpiralNesnesi spiral)
                {
                    // DXF'te klotoid için özel bir varlık yok; örneklenmiş noktalarla (gerçek Z'leriyle) 3B çizgi dizisi (Polyline3D) olarak yazılır.
                    var vertices = spiral.OrnekNoktalariVeZGetir().Select(p => new Vector3(p.Nokta.X, p.Nokta.Y, p.Z)).ToList();
                    if (vertices.Count >= 2)
                        Ekle(new Polyline3D(vertices, isClosed: false), nesne);
                }
                else if (nesne is GuzergahEtiketNesnesi etiketSeti)
                {
                    // Her etiket bir TEXT (ve varsa çentiği bir LINE) olarak yazılır.
                    foreach (var etiket in etiketSeti.EtiketleriGetir())
                    {
                        var dikme = etiket.DikmeAcisi * System.Math.PI / 180.0;
                        double dx = System.Math.Cos(dikme), dy = System.Math.Sin(dikme);
                        var yarim = etiketSeti.YaziYuksekligi / 2;
                        if (etiketSeti.CentikGoster)
                            Ekle(new Line(new Vector3(etiket.X - yarim * dx, etiket.Y - yarim * dy, 0), new Vector3(etiket.X + yarim * dx, etiket.Y + yarim * dy, 0)), nesne);

                        // Eksene paralel yazılar ortalanmış, dik yazılar eksene bakan ucundan hizalanmış yazılır.
                        var etiketYazisi = new Text(etiket.Metin, new Vector3(etiket.YaziX, etiket.YaziY, 0), etiket.Yukseklik)
                        {
                            Rotation = etiket.YaziAcisi,
                            Alignment = etiket.Ortali ? TextAlignment.MiddleCenter : etiket.SagaDayali ? TextAlignment.MiddleRight : TextAlignment.MiddleLeft
                        };
                        Ekle(etiketYazisi, nesne);
                        if (!etiketSeti.Renk.IsEmpty)
                            etiketYazisi.Color = new AciColor(etiketSeti.Renk);
                    }
                }
                else if (nesne is GuzergahNesnesi guzergah)
                {
                    // Güzergah parça parça yazılır: doğru LINE, kurp ARC, spiral (DXF'te karşılığı olmadığı için) 3B polyline.
                    foreach (var parca in guzergah.Parcalar)
                    {
                        if (parca.Tip == GuzergahParcaTipi.Dogru)
                        {
                            Ekle(new Line(new Vector3(parca.BaslangicX, parca.BaslangicY, 0), new Vector3(parca.BitisX, parca.BitisY, 0)), nesne);
                        }
                        else if (parca.Tip == GuzergahParcaTipi.Kurp)
                        {
                            // Merkez, gidiş doğrultusunun dönüş tarafındaki dikmesi üzerinde yarıçap kadar ötededir.
                            var sola = parca.Yon == DonusYonu.Sol;
                            var dikme = parca.BaslangicAcisi + (sola ? System.Math.PI / 2 : -System.Math.PI / 2);
                            var mx = parca.BaslangicX + parca.YaricapBaslangic * System.Math.Cos(dikme);
                            var my = parca.BaslangicY + parca.YaricapBaslangic * System.Math.Sin(dikme);
                            var basAci = System.Math.Atan2(parca.BaslangicY - my, parca.BaslangicX - mx) * 180.0 / System.Math.PI;
                            var bitAci = System.Math.Atan2(parca.BitisY - my, parca.BitisX - mx) * 180.0 / System.Math.PI;
                            // DXF yayı başlangıç açısından bitiş açısına saat yönünün tersine çizilir.
                            Ekle(new Arc(new Vector3(mx, my, 0), parca.YaricapBaslangic, sola ? basAci : bitAci, sola ? bitAci : basAci), nesne);
                        }
                        else
                        {
                            var noktalar = new List<Vector3>();
                            for (var i = 0; i < parca.OrnekX.Length; i++)
                                noktalar.Add(new Vector3(parca.OrnekX[i], parca.OrnekY[i], 0));
                            if (noktalar.Count >= 2)
                                Ekle(new Polyline3D(noktalar, isClosed: false), nesne);
                        }
                    }
                }
            }

            dxfBelge.Save(dosyaYolu);
        }

        // Tabakanın DXF karşılığını kurar. Adı DXF'te geçersiz karakter içeriyorsa null döner; o tabakanın objeleri
        // varsayılan "0" tabakasına yazılır.
        private static Layer KatmanOlustur(Tabaka tabaka)
        {
            try
            {
                return new Layer(tabaka.Ad)
                {
                    Color = new AciColor(tabaka.Renk),
                    IsVisible = tabaka.Gorunur,
                    IsLocked = tabaka.Kilitli
                };
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }
    }
}
