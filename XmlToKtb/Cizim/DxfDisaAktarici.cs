using System;
using System.Collections.Generic;
using System.Linq;
using netDxf;
using netDxf.Blocks;
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
            var olcuStilleri = new Dictionary<OlcuStili, DimensionStyle>();

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

                // DXF yalnızca belirli kalınlıkları tanır: en yakını seçilir.
                if (nesne.CizgiKalinligi > 0)
                {
                    var yuzdeMm = nesne.CizgiKalinligi * 100;
                    varlik.Lineweight = ((Lineweight[])Enum.GetValues(typeof(Lineweight)))
                        .Where(k => (short)k >= 0)
                        .OrderBy(k => Math.Abs((short)k - yuzdeMm))
                        .First();
                }

                dxfBelge.Entities.Add(varlik);
            }

            void Yaz(CizimNesnesi nesne)
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
                else if (nesne is CokluDogruNesnesi cokluDogru && cokluDogru.Koseler.Count >= 2)
                {
                    // Bütün köşeleri aynı kottaysa yaylarıyla birlikte 2B polyline, değilse örneklenmiş 3B polyline yazılır.
                    var kot = cokluDogru.Koseler[0].Z;
                    if (cokluDogru.Koseler.All(k => System.Math.Abs(k.Z - kot) < 1e-9))
                    {
                        var koseler = cokluDogru.Koseler.Select(k => new Polyline2DVertex(new Vector2(k.X, k.Y), k.Bulge)).ToList();
                        Ekle(new Polyline2D(koseler, cokluDogru.Kapali) { Elevation = kot }, nesne);
                    }
                    else
                    {
                        Ekle(new Polyline3D(cokluDogru.Noktalar().Select(p => new Vector3(p[0], p[1], p[2])).ToList(), cokluDogru.Kapali), nesne);
                    }
                }
                else if (nesne is BlokNesnesi blok)
                {
                    // Blok, içindeki objeler olarak yazılır; tabakasız objeler bloğun tabakasına girer.
                    foreach (var oge in blok.Ogeler)
                    {
                        var eskiTabaka = oge.Tabaka;
                        if (oge.Tabaka == null)
                            oge.Tabaka = blok.Tabaka;
                        Yaz(oge);
                        oge.Tabaka = eskiTabaka;
                    }
                }
                else if (nesne is OlcuNesnesi olcu)
                {
                    var varlik = OlcuVarligi(olcu, olcuStilleri);
                    if (varlik != null)
                        Ekle(varlik, nesne);
                }
                else if (nesne is KilavuzNesnesi kilavuz && kilavuz.Koseler.Count >= 2)
                {
                    // Kılavuz çizgi LEADER olarak yazılır; yazısı MText (satır sonları \P).
                    var stil = kilavuz.Stil ?? OlcuStili.Varsayilan;
                    if (!olcuStilleri.TryGetValue(stil, out var ds))
                        olcuStilleri[stil] = ds = StilOlustur(stil);
                    // İniş çizgisi LEADER'a son köşe olarak yazılır (içe aktarırken stile uyan yatay son parça tekrar
                    // iniş çizgisi sayılır); netDxf'in kendi hookline köşesi kapatılır.
                    var koseler = kilavuz.Koseler.Select(k => new Vector2(k[0], k[1])).ToList();
                    if (kilavuz.Inis)
                    {
                        var son = koseler[koseler.Count - 1];
                        koseler.Add(new Vector2(son.X + kilavuz.InisYonu * kilavuz.InisBoyu, son.Y));
                    }
                    var metin = (kilavuz.Metin ?? "").Replace("\r\n", "\n").Replace("\n", "\\P");
                    try
                    {
                        var lider = metin.Length > 0 ? new Leader(metin, koseler, ds) : new Leader(koseler, ds);
                        lider.HasHookline = false;
                        Ekle(lider, nesne);
                    }
                    catch (ArgumentException)
                    {
                        // Çakışan köşeler gibi geçersiz geometri: kılavuz parçalarıyla yazılır.
                        foreach (var parca in kilavuz.Parcala())
                            Yaz(parca);
                    }
                }
            }

            foreach (var obje in nesneler)
                Yaz(obje);

            dxfBelge.Save(dosyaYolu);
        }

        /// <summary>Ölçü stilinin DXF karşılığı (DIMSTYLE). Adı DXF'te geçersizse "Standart" adıyla yazılır.</summary>
        private static DimensionStyle StilOlustur(OlcuStili stil)
        {
            DimensionStyle ds;
            try
            {
                ds = new DimensionStyle(stil.Ad);
            }
            catch (ArgumentException)
            {
                ds = new DimensionStyle("Standart");
            }
            ds.DimScaleOverall = stil.GenelOlcek > 0 ? stil.GenelOlcek : 1;
            ds.TextHeight = stil.YaziYuksekligi;
            ds.TextOffset = stil.YaziBoslugu;
            ds.TextVerticalPlacement = stil.YaziKonumu == OlcuYaziKonumu.Ortada ? DimensionStyleTextVerticalPlacement.Centered : DimensionStyleTextVerticalPlacement.Above;
            ds.LengthPrecision = (short)Math.Max(0, Math.Min(8, stil.OndalikBasamak));
            ds.AngularPrecision = (short)Math.Max(0, Math.Min(8, stil.AciOndalikBasamak));
            ds.DimScaleLinear = stil.UzunlukCarpani;
            ds.DimPrefix = stil.OnEk ?? "";
            ds.DimSuffix = stil.SonEk ?? "";
            ds.DecimalSeparator = stil.OndalikAyraci;
            ds.DimAngularUnits = stil.AciBirimi == OlcuAciBirimi.Grad ? netDxf.Units.AngleUnitType.Gradians : netDxf.Units.AngleUnitType.DecimalDegrees;
            ds.ArrowSize = stil.OkBoyu;
            ds.ExtLineOffset = stil.UzatmaAraligi;
            ds.ExtLineExtend = stil.UzatmaTasmasi;
            ds.DimLineExtend = stil.OlcuCizgisiTasmasi;
            Block ok = null;
            switch (stil.OkTipi)
            {
                case OlcuOkTipi.EgikCizgi: ok = DimensionArrowhead.ArchitecturalTick; break;
                case OlcuOkTipi.Nokta: ok = DimensionArrowhead.Dot; break;
                case OlcuOkTipi.Acik: ok = DimensionArrowhead.Open; break;
                case OlcuOkTipi.Yok: ok = DimensionArrowhead.None; break;
            }
            ds.DimArrow1 = ok;
            ds.DimArrow2 = ok;
            return ds;
        }

        /// <summary>Ölçü objesinin DXF ölçü varlığı (DIMENSION); geometrisi geçersizse null.</summary>
        private static Dimension OlcuVarligi(OlcuNesnesi olcu, Dictionary<OlcuStili, DimensionStyle> stiller)
        {
            var stil = olcu.Stil ?? OlcuStili.Varsayilan;
            if (!stiller.TryGetValue(stil, out var ds))
                stiller[stil] = ds = StilOlustur(stil);
            Vector2 a = new Vector2(olcu.AX, olcu.AY), b = new Vector2(olcu.BX, olcu.BY), c = new Vector2(olcu.CX, olcu.CY), d = new Vector2(olcu.DX, olcu.DY);
            Dimension varlik;
            try
            {
                switch (olcu.Tur)
                {
                    case OlcuTuru.Dogrusal:
                    {
                        var dogrusal = new LinearDimension(a, b, 0, olcu.Aci, ds);
                        dogrusal.SetDimensionLinePosition(c);
                        varlik = dogrusal;
                        break;
                    }
                    case OlcuTuru.Hizali:
                    {
                        var hizali = new AlignedDimension(a, b, 0, ds);
                        hizali.SetDimensionLinePosition(c);
                        varlik = hizali;
                        break;
                    }
                    case OlcuTuru.Acisal:
                    {
                        // DXF'te açı başlangıç kolundan bitiş koluna saat yönünün tersine ölçülür.
                        var acisal = olcu.AciB_denBasliyor()
                            ? new Angular3PointDimension(a, b, c, Vector2.Distance(a, d), ds)
                            : new Angular3PointDimension(a, c, b, Vector2.Distance(a, d), ds);
                        acisal.SetDimensionLinePosition(d);
                        varlik = acisal;
                        break;
                    }
                    case OlcuTuru.Yaricap:
                        varlik = new RadialDimension(a, b, ds);
                        break;
                    case OlcuTuru.Cap:
                        varlik = new DiametricDimension(a, b, ds);
                        break;
                    default:
                        return null;
                }
            }
            catch (ArgumentException)
            {
                // Sıfır uzunluk gibi geçersiz geometri.
                return null;
            }
            if (!string.IsNullOrEmpty(olcu.Metin))
                varlik.UserText = olcu.Metin;
            return varlik;
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
