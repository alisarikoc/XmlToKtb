using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using netDxf;
using netDxf.Entities;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Bir netDxf DxfDocument'ındaki tüm varlıkları (INSERT/DIMENSION bloklarının içeriği dahil, iç içe
    /// bloklar da dönüşümleriyle birlikte düzleştirilerek) CizimNesnesi listesine çevirir.
    /// </summary>
    public static class DxfIceAktarici
    {
        private const int AzamiDerinlik = 8;

        private struct Donusum
        {
            public double OfsetX, OfsetY, OfsetZ, OlcekX, OlcekY, OlcekZ, DonusDerece;

            public static readonly Donusum Kimlik = new Donusum { OlcekX = 1, OlcekY = 1, OlcekZ = 1 };

            public (double x, double y) Uygula(double x, double y)
            {
                var rad = DonusDerece * Math.PI / 180.0;
                var cos = Math.Cos(rad);
                var sin = Math.Sin(rad);
                var sx = x * OlcekX;
                var sy = y * OlcekY;
                var rx = sx * cos - sy * sin;
                var ry = sx * sin + sy * cos;
                return (rx + OfsetX, ry + OfsetY);
            }

            public double UygulaZ(double z) => z * OlcekZ + OfsetZ;

            public double OlcekOrtalama => (OlcekX + OlcekY) / 2.0;

            public Donusum IceIceEkle(Vector3 pozisyon, Vector3 olcek, double donusDerece)
            {
                var (px, py) = Uygula(pozisyon.X, pozisyon.Y);
                return new Donusum
                {
                    OfsetX = px,
                    OfsetY = py,
                    OfsetZ = UygulaZ(pozisyon.Z),
                    OlcekX = OlcekX * olcek.X,
                    OlcekY = OlcekY * olcek.Y,
                    OlcekZ = OlcekZ * olcek.Z,
                    DonusDerece = DonusDerece + donusDerece
                };
            }
        }

        // true iken çoklu doğrular (polyline) ve bloklar (INSERT) parçalanmaz; çoklu doğru ve blok objesi olarak alınır.
        [ThreadStatic]
        private static bool _yapiyiKoru;

        /// <summary>
        /// Varlıkları alır; yapiyiKoru true ise çoklu doğrular ve bloklar tek obje olarak kalır (sonradan Patlat ile
        /// ayrılabilir), false ise doğru/yay parçalarına ayrılır.
        /// </summary>
        public static List<CizimNesnesi> Ayikla(DxfDocument belge, Func<netDxf.Tables.Layer, Tabaka> tabakaBul, bool yapiyiKoru)
        {
            _yapiyiKoru = yapiyiKoru;
            try
            {
                return Ayikla(belge, tabakaBul);
            }
            finally
            {
                _yapiyiKoru = false;
            }
        }

        public static List<CizimNesnesi> Ayikla(DxfDocument belge)
        {
            return Ayikla(belge, null);
        }

        /// <summary>
        /// tabakaBul verilirse her obje DXF'teki tabakasına (layer) yerleştirilir: fonksiyon, DXF tabakasının çizimdeki
        /// karşılığını döner. Rengi "tabakaya göre" olmayan varlıkların kendi rengi de korunur.
        /// </summary>
        public static List<CizimNesnesi> Ayikla(DxfDocument belge, Func<netDxf.Tables.Layer, Tabaka> tabakaBul)
        {
            var sonuc = new List<CizimNesnesi>();
            _dogrusalOlcular = DogrusalOlcuTablosu.TryGetValue(belge, out var harita) ? harita : null;
            try
            {
                foreach (var varlik in belge.Entities.All)
                    VarlikEkle(varlik, Donusum.Kimlik, sonuc, 0, tabakaBul);
                // netDxf'in okumadığı MLEADER'lar (ACadSharp ile okunmuş) eklenir.
                if (MleaderTablosu.TryGetValue(belge, out var mleaderlar))
                {
                    foreach (var m in mleaderlar)
                    {
                        var kilavuz = m.Nesne.Kopyala();
                        if (kilavuz.Stil != null && _olcuStiliBul != null)
                            kilavuz.Stil = _olcuStiliBul(kilavuz.Stil) ?? kilavuz.Stil;
                        var katman = m.Katman != null && belge.Layers.Contains(m.Katman) ? belge.Layers[m.Katman] : null;
                        var tabaka = katman != null ? tabakaBul?.Invoke(katman) : null;
                        var parcalar = _yapiyiKoru ? new List<CizimNesnesi> { kilavuz } : kilavuz.Parcala();
                        foreach (var parca in parcalar)
                        {
                            if (parca.Tabaka == null)
                                parca.Tabaka = tabaka;
                            sonuc.Add(parca);
                        }
                    }
                }
            }
            finally
            {
                _dogrusalOlcular = null;
            }
            return sonuc;
        }

        // netDxf, doğrusal (döndürülmüş) ölçüleri okurken hizalı ölçü sanar ve doğrultuyu kaybeder (yatay ölçü eğik iki
        // nokta arasında gerçek uzunluğu gösterir). Bu yüzden yüklenen DXF metnindeki doğrusal ölçülerin tutamacı ve
        // doğrultusu ayrıca okunup belgeye bağlanır.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DxfDocument, Dictionary<string, double>> DogrusalOlcuTablosu =
            new System.Runtime.CompilerServices.ConditionalWeakTable<DxfDocument, Dictionary<string, double>>();
        [ThreadStatic]
        private static Dictionary<string, double> _dogrusalOlcular;

        /// <summary>DXF dosyasını yükler (doğrusal ölçülerin türü ve doğrultusu korunur). Okunamazsa netDxf'in hatası fırlar.</summary>
        public static DxfDocument Yukle(string dosya) => Yukle(System.IO.File.ReadAllBytes(dosya));

        /// <summary>
        /// Bellekteki DXF'i yükler. DWG'den çevrilen DXF'te okunmuş DWG belgesi (cad) verilir; MLEADER'lar ondan alınır.
        /// DXF'te MLEADER varsa (netDxf okumaz) ACadSharp ile ayrıca okunur.
        /// </summary>
        public static DxfDocument Yukle(byte[] veri, ACadSharp.CadDocument cad = null)
        {
            DxfDocument belge;
            using (var akis = new System.IO.MemoryStream(veri))
                belge = DxfDocument.Load(akis);
            if (belge != null)
            {
                var harita = DogrusalOlculeriTara(veri);
                if (harita.Count > 0)
                    DogrusalOlcuTablosu.Add(belge, harita);
                try
                {
                    if (cad == null && MetinIceriyor(veri, "MULTILEADER"))
                        using (var akis = new System.IO.MemoryStream(veri))
                            cad = ACadSharp.IO.DxfReader.Read(akis, null);
                    if (cad != null)
                    {
                        var mleaderlar = MleaderlariAl(cad);
                        if (mleaderlar.Count > 0)
                            MleaderTablosu.Add(belge, mleaderlar);
                    }
                }
                catch (Exception)
                {
                    // MLEADER okunamazsa çizimin geri kalanı yine açılır.
                }
            }
            return belge;
        }

        /// <summary>
        /// ASCII DXF'teki DIMENSION kayıtlarından doğrusal olanları (70 kodunun alt 4 biti 0) bulur: tutamaç (5) →
        /// doğrultu (50, derece). İkili DXF'te boş döner.
        /// </summary>
        private static Dictionary<string, double> DogrusalOlculeriTara(byte[] veri)
        {
            var sonuc = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (veri.Length >= 18 && System.Text.Encoding.ASCII.GetString(veri, 0, 18) == "AutoCAD Binary DXF")
                return sonuc;
            using (var okuyucu = new System.IO.StreamReader(new System.IO.MemoryStream(veri), System.Text.Encoding.GetEncoding(28591)))
            {
                var olcu = false;
                string tutamac = null;
                var tur = -1;
                double donus = 0;
                void Bitir()
                {
                    if (olcu && tutamac != null && tur >= 0 && (tur & 0x0F) == 0)
                        sonuc[tutamac] = donus;
                }
                string kodSatiri;
                while ((kodSatiri = okuyucu.ReadLine()) != null)
                {
                    var deger = okuyucu.ReadLine();
                    if (deger == null)
                        break;
                    if (!int.TryParse(kodSatiri.Trim(), out var kod))
                        continue;
                    if (kod == 0)
                    {
                        Bitir();
                        olcu = deger.Trim() == "DIMENSION";
                        tutamac = null;
                        tur = -1;
                        donus = 0;
                    }
                    else if (olcu)
                    {
                        if (kod == 5)
                            tutamac = deger.Trim();
                        else if (kod == 70)
                            int.TryParse(deger.Trim(), out tur);
                        else if (kod == 50)
                            double.TryParse(deger.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out donus);
                    }
                }
                Bitir();
            }
            return sonuc;
        }

        /// <summary>DXF rengini çizim rengine çevirir. Beyaz, beyaz zeminde görünmeyeceği için siyaha çevrilir.</summary>
        public static System.Drawing.Color RenkCevir(AciColor renk)
        {
            var c = renk.ToColor();
            return c.R > 240 && c.G > 240 && c.B > 240 ? System.Drawing.Color.Black : System.Drawing.Color.FromArgb(c.R, c.G, c.B);
        }

        private static void VarlikEkle(EntityObject varlik, Donusum donusum, List<CizimNesnesi> sonuc, int derinlik,
            Func<netDxf.Tables.Layer, Tabaka> tabakaBul)
        {
            if (derinlik > AzamiDerinlik)
                return;

            var oncekiSayi = sonuc.Count;
            VarligiCevir(varlik, donusum, sonuc, derinlik, tabakaBul);

            // Çizgi kalınlığı (lineweight, 1/100 mm): varlığınki, "tabakaya göre" ise tabakanınki.
            var kalinlik = (short)varlik.Lineweight;
            if (varlik.Lineweight == Lineweight.ByLayer && varlik.Layer != null)
                kalinlik = (short)varlik.Layer.Lineweight;
            if (kalinlik > 0)
            {
                for (var i = oncekiSayi; i < sonuc.Count; i++)
                {
                    if (sonuc[i].CizgiKalinligi <= 0)
                        sonuc[i].CizgiKalinligi = kalinlik / 100.0;
                }
            }

            if (tabakaBul == null)
                return;

            // Blok içindeki "0" tabakalı varlık, AutoCAD'de olduğu gibi bloğun eklendiği tabakayı alır: burada boş
            // bırakılır, bloğu (INSERT) işleyen üst çağrı kendi tabakasını verir.
            var blokIcindeSifirTabakasi = derinlik > 0 && varlik.Layer != null && varlik.Layer.Name == "0";
            for (var i = oncekiSayi; i < sonuc.Count; i++)
            {
                var nesne = sonuc[i];
                if (nesne.Tabaka == null && !blokIcindeSifirTabakasi && varlik.Layer != null)
                    nesne.Tabaka = tabakaBul(varlik.Layer);
                if (nesne.OzelRenk == null && varlik.Color != null && !varlik.Color.IsByLayer && !varlik.Color.IsByBlock)
                    nesne.OzelRenk = RenkCevir(varlik.Color);
            }
        }

        private static void VarligiCevir(EntityObject varlik, Donusum donusum, List<CizimNesnesi> sonuc, int derinlik,
            Func<netDxf.Tables.Layer, Tabaka> tabakaBul)
        {

            switch (varlik)
            {
                case Line dogru:
                {
                    var (x1, y1) = donusum.Uygula(dogru.StartPoint.X, dogru.StartPoint.Y);
                    var (x2, y2) = donusum.Uygula(dogru.EndPoint.X, dogru.EndPoint.Y);
                    sonuc.Add(new CizgiNesnesi
                    {
                        BaslangicX = x1, BaslangicY = y1, BaslangicZ = donusum.UygulaZ(dogru.StartPoint.Z),
                        BitisX = x2, BitisY = y2, BitisZ = donusum.UygulaZ(dogru.EndPoint.Z)
                    });
                    break;
                }

                case Text yazi:
                {
                    var (yatay, dikey) = HizaCoz(yazi.Alignment);
                    YaziEkle(yazi.Value, yazi.Position, yazi.Height, yazi.Rotation, yatay, dikey, donusum, sonuc);
                    break;
                }

                case MText mYazi:
                {
                    var (yatay, dikey) = HizaCoz(mYazi.AttachmentPoint);
                    YaziEkle(MTextDuzMetin(mYazi.Value), mYazi.Position, mYazi.Height, mYazi.Rotation, yatay, dikey, donusum, sonuc);
                    break;
                }

                case netDxf.Entities.Point nokta:
                {
                    var (x, y) = donusum.Uygula(nokta.Position.X, nokta.Position.Y);
                    sonuc.Add(new NoktaNesnesi { X = x, Y = y, Z = donusum.UygulaZ(nokta.Position.Z) });
                    break;
                }

                case Circle daire:
                {
                    var (cx, cy) = donusum.Uygula(daire.Center.X, daire.Center.Y);
                    sonuc.Add(new DaireNesnesi { MerkezX = cx, MerkezY = cy, MerkezZ = donusum.UygulaZ(daire.Center.Z), Yaricap = daire.Radius * donusum.OlcekOrtalama });
                    break;
                }

                case Arc yay:
                {
                    var (cx, cy) = donusum.Uygula(yay.Center.X, yay.Center.Y);
                    sonuc.Add(new YayNesnesi
                    {
                        MerkezX = cx,
                        MerkezY = cy,
                        MerkezZ = donusum.UygulaZ(yay.Center.Z),
                        Yaricap = yay.Radius * donusum.OlcekOrtalama,
                        BaslangicAcisi = yay.StartAngle + donusum.DonusDerece,
                        BitisAcisi = yay.EndAngle + donusum.DonusDerece
                    });
                    break;
                }

                case Polyline2D polyline2D when _yapiyiKoru:
                {
                    // Aynalı (eksi ölçekli) blok içinde yayların dönüş yönü tersine döner.
                    var bulgeIsareti = donusum.OlcekX * donusum.OlcekY < 0 ? -1 : 1;
                    var cokluDogru = new CokluDogruNesnesi { Kapali = polyline2D.IsClosed };
                    foreach (var v in polyline2D.Vertexes)
                    {
                        var (x, y) = donusum.Uygula(v.Position.X, v.Position.Y);
                        cokluDogru.Koseler.Add(new CokluDogruKosesi { X = x, Y = y, Z = donusum.UygulaZ(polyline2D.Elevation), Bulge = v.Bulge * bulgeIsareti });
                    }
                    if (cokluDogru.Koseler.Count > 0)
                        sonuc.Add(cokluDogru);
                    break;
                }

                case Polyline3D polyline3D when _yapiyiKoru:
                {
                    var cokluDogru = new CokluDogruNesnesi { Kapali = polyline3D.IsClosed };
                    foreach (var v in polyline3D.Vertexes)
                    {
                        var (x, y) = donusum.Uygula(v.X, v.Y);
                        cokluDogru.Koseler.Add(new CokluDogruKosesi { X = x, Y = y, Z = donusum.UygulaZ(v.Z) });
                    }
                    if (cokluDogru.Koseler.Count > 0)
                        sonuc.Add(cokluDogru);
                    break;
                }

                case Insert eklenti when _yapiyiKoru:
                {
                    var altDonusum = donusum.IceIceEkle(eklenti.Position, eklenti.Scale, eklenti.Rotation);
                    var (bx, by) = donusum.Uygula(eklenti.Position.X, eklenti.Position.Y);
                    var blok = new BlokNesnesi { Ad = eklenti.Block?.Name ?? "", KonumX = bx, KonumY = by };
                    foreach (var altVarlik in eklenti.Block.Entities)
                        VarlikEkle(altVarlik, altDonusum, blok.Ogeler, derinlik + 1, tabakaBul);
                    if (blok.Ogeler.Count > 0)
                        sonuc.Add(blok);
                    break;
                }

                case Polyline2D polyline2D:
                {
                    var noktalar = new List<(double X, double Y, double Z)>(polyline2D.Vertexes.Count);
                    foreach (var v in polyline2D.Vertexes)
                        noktalar.Add((v.Position.X, v.Position.Y, polyline2D.Elevation));
                    CokgenEkle(noktalar, polyline2D.IsClosed, donusum, sonuc);
                    break;
                }

                case Polyline3D polyline3D:
                {
                    var noktalar = new List<(double X, double Y, double Z)>(polyline3D.Vertexes.Count);
                    foreach (var v in polyline3D.Vertexes)
                        noktalar.Add((v.X, v.Y, v.Z));
                    CokgenEkle(noktalar, polyline3D.IsClosed, donusum, sonuc);
                    break;
                }

                case Face3D yuz:
                    CokgenEkle(new List<(double X, double Y, double Z)>
                    {
                        (yuz.FirstVertex.X, yuz.FirstVertex.Y, yuz.FirstVertex.Z),
                        (yuz.SecondVertex.X, yuz.SecondVertex.Y, yuz.SecondVertex.Z),
                        (yuz.ThirdVertex.X, yuz.ThirdVertex.Y, yuz.ThirdVertex.Z),
                        (yuz.FourthVertex.X, yuz.FourthVertex.Y, yuz.FourthVertex.Z)
                    }, true, donusum, sonuc);
                    break;

                case Solid katiUcgen:
                    CokgenEkle(new List<(double X, double Y, double Z)>
                    {
                        (katiUcgen.FirstVertex.X, katiUcgen.FirstVertex.Y, katiUcgen.Elevation),
                        (katiUcgen.SecondVertex.X, katiUcgen.SecondVertex.Y, katiUcgen.Elevation),
                        (katiUcgen.ThirdVertex.X, katiUcgen.ThirdVertex.Y, katiUcgen.Elevation),
                        (katiUcgen.FourthVertex.X, katiUcgen.FourthVertex.Y, katiUcgen.Elevation)
                    }, true, donusum, sonuc);
                    break;

                case Hatch tarama:
                    foreach (var yol in tarama.BoundaryPaths)
                    foreach (var kenar in yol.Edges)
                        HatchKenariEkle(kenar, tarama.Elevation, donusum, sonuc);
                    break;

                case Insert eklenti:
                {
                    var altDonusum = donusum.IceIceEkle(eklenti.Position, eklenti.Scale, eklenti.Rotation);
                    foreach (var altVarlik in eklenti.Block.Entities)
                        VarlikEkle(altVarlik, altDonusum, sonuc, derinlik + 1, tabakaBul);
                    break;
                }

                case Dimension olcu:
                {
                    // Yapı korunurken ölçü, ölçü objesi olarak alınır; tanınmayan ölçü türleri (koordinat, yay boyu)
                    // ve diğer ekranlarda ölçünün blok içeriği (çizgiler, oklar, yazı) alınır.
                    var olcuNesnesi = _yapiyiKoru ? OlcuCevir(olcu, donusum) : null;
                    if (olcuNesnesi != null)
                        sonuc.Add(olcuNesnesi);
                    else if (olcu.Block != null)
                        foreach (var altVarlik in olcu.Block.Entities)
                            VarlikEkle(altVarlik, donusum, sonuc, derinlik + 1, tabakaBul);
                    break;
                }

                case Leader lider:
                {
                    // Kılavuz çizgi (LEADER): yapı korunurken kılavuz objesi, diğer ekranlarda çizgi/ok/yazı parçaları.
                    var kilavuz = KilavuzCevir(lider, donusum);
                    if (kilavuz != null)
                    {
                        if (_yapiyiKoru)
                            sonuc.Add(kilavuz);
                        else
                            sonuc.AddRange(kilavuz.Parcala());
                    }
                    break;
                }
            }
        }

        private static KilavuzNesnesi KilavuzCevir(Leader lider, Donusum donusum)
        {
            if (lider.Vertexes == null || lider.Vertexes.Count < 2)
                return null;
            // LEADER'ın yolu iniş dahil köşelerindedir; ayrıca iniş çizgisi eklenmez.
            var kilavuz = new KilavuzNesnesi { Inis = false, Stil = OlcuStiliGetir(lider.Style) };
            foreach (var v in lider.Vertexes)
            {
                var (x, y) = donusum.Uygula(v.X, v.Y);
                kilavuz.Koseler.Add(new[] { x, y });
            }
            switch (lider.Annotation)
            {
                case MText mYazi: kilavuz.Metin = MTextDuzMetin(mYazi.Value) ?? ""; break;
                case Text yazi: kilavuz.Metin = yazi.Value ?? ""; break;
            }
            // Stilin iniş boyunda yatay son parça (bu programın yazdığı ya da AutoCAD hookline'ı) iniş çizgisi sayılır.
            var k = kilavuz.Koseler;
            if (k.Count >= 3)
            {
                double dx = k[k.Count - 1][0] - k[k.Count - 2][0], dy = k[k.Count - 1][1] - k[k.Count - 2][1];
                var boy = kilavuz.InisBoyu;
                if (Math.Abs(dy) <= boy * 1e-6 && Math.Abs(Math.Abs(dx) - boy) <= boy * 1e-3)
                {
                    k.RemoveAt(k.Count - 1);
                    kilavuz.Inis = true;
                }
            }
            return kilavuz;
        }

        // ------------------------------------------------------------------ MLEADER (netDxf okumaz, ACadSharp ile)

        private sealed class MleaderBilgisi
        {
            public string Katman;
            public KilavuzNesnesi Nesne;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DxfDocument, List<MleaderBilgisi>> MleaderTablosu =
            new System.Runtime.CompilerServices.ConditionalWeakTable<DxfDocument, List<MleaderBilgisi>>();

        /// <summary>
        /// ACadSharp belgesindeki MLEADER (MULTILEADER) objelerini kılavuz çizgiye çevirir: her kılavuz kökünün her çizgisi
        /// bir kılavuz olur (köşeleri + bağlantı noktası); yazı ilk çizgiye yazılır.
        /// </summary>
        private static List<MleaderBilgisi> MleaderlariAl(ACadSharp.CadDocument cad)
        {
            var sonuc = new List<MleaderBilgisi>();
            var stiller = new Dictionary<string, OlcuStili>();
            foreach (var ml in cad.Entities.OfType<ACadSharp.Entities.MultiLeader>())
            {
                var cd = ml.ContextData;
                if (cd?.LeaderRoots == null)
                    continue;
                var stilAdi = "MLeader " + (ml.Style?.Name ?? "Standard");
                if (!stiller.TryGetValue(stilAdi, out var stil))
                {
                    stil = new OlcuStili
                    {
                        Ad = stilAdi,
                        GenelOlcek = 1,
                        YaziYuksekligi = cd.TextHeight > 0 ? cd.TextHeight : 2.5,
                        OkBoyu = cd.ArrowheadSize > 0 ? cd.ArrowheadSize : ml.ArrowheadSize > 0 ? ml.ArrowheadSize : 2.5,
                        YaziBoslugu = cd.LandingGap > 0 ? cd.LandingGap : 0.625,
                        OkTipi = OkTipiCevir(ml.Arrowhead?.Name)
                    };
                    stiller[stilAdi] = stil;
                }
                var metin = cd.HasTextContents ? MTextDuzMetin(cd.TextLabel) ?? "" : "";
                var ilk = true;
                foreach (var kok in cd.LeaderRoots)
                {
                    foreach (var cizgi in kok.Lines)
                    {
                        var kilavuz = new KilavuzNesnesi { Inis = ml.EnableLanding, Stil = stil, Metin = ilk ? metin : "" };
                        foreach (var p in cizgi.Points)
                            kilavuz.Koseler.Add(new[] { p.X, p.Y });
                        kilavuz.Koseler.Add(new[] { kok.ConnectionPoint.X, kok.ConnectionPoint.Y });
                        if (kilavuz.Koseler.Count < 2)
                            continue;
                        sonuc.Add(new MleaderBilgisi { Katman = ml.Layer?.Name, Nesne = kilavuz });
                        ilk = false;
                    }
                }
            }
            return sonuc;
        }

        private static bool MetinIceriyor(byte[] veri, string aranan)
        {
            var kalip = System.Text.Encoding.ASCII.GetBytes(aranan);
            for (var i = 0; i + kalip.Length <= veri.Length; i++)
            {
                var j = 0;
                while (j < kalip.Length && veri[i + j] == kalip[j])
                    j++;
                if (j == kalip.Length)
                    return true;
            }
            return false;
        }

        // Ölçü stillerinin çizimdeki karşılığı (Ayikla'ya verilir); verilmezse her DXF stili için bir stil oluşturulur.
        [ThreadStatic]
        private static Func<OlcuStili, OlcuStili> _olcuStiliBul;
        [ThreadStatic]
        private static Dictionary<netDxf.Tables.DimensionStyle, OlcuStili> _olcuStilleri;

        /// <summary>
        /// Varlıkları alır (yapı korunarak: çoklu doğrular, bloklar ve ölçüler tek obje olarak). olcuStiliBul, DXF'ten
        /// okunan ölçü stilinin çizimdeki karşılığını döner (ör. aynı adlı stil ya da eklenen kopya).
        /// </summary>
        public static List<CizimNesnesi> Ayikla(DxfDocument belge, Func<netDxf.Tables.Layer, Tabaka> tabakaBul, bool yapiyiKoru, Func<OlcuStili, OlcuStili> olcuStiliBul)
        {
            _olcuStiliBul = olcuStiliBul;
            _olcuStilleri = new Dictionary<netDxf.Tables.DimensionStyle, OlcuStili>();
            try
            {
                return Ayikla(belge, tabakaBul, yapiyiKoru);
            }
            finally
            {
                _olcuStiliBul = null;
                _olcuStilleri = null;
            }
        }

        /// <summary>DXF ölçü stilini (DIMSTYLE) ölçü stiline çevirir.</summary>
        public static OlcuStili StilCevir(netDxf.Tables.DimensionStyle ds)
        {
            var stil = new OlcuStili
            {
                Ad = string.IsNullOrEmpty(ds.Name) ? "Standart" : ds.Name,
                GenelOlcek = ds.DimScaleOverall > 0 ? ds.DimScaleOverall : 1,
                YaziYuksekligi = ds.TextHeight,
                YaziBoslugu = Math.Abs(ds.TextOffset),
                YaziKonumu = ds.TextVerticalPlacement == netDxf.Tables.DimensionStyleTextVerticalPlacement.Centered ? OlcuYaziKonumu.Ortada : OlcuYaziKonumu.Ustte,
                OndalikBasamak = ds.LengthPrecision,
                AciOndalikBasamak = ds.AngularPrecision,
                UzunlukCarpani = ds.DimScaleLinear != 0 ? ds.DimScaleLinear : 1,
                OnEk = ds.DimPrefix ?? "",
                SonEk = ds.DimSuffix ?? "",
                OndalikAyraci = ds.DecimalSeparator == '\0' ? '.' : ds.DecimalSeparator,
                AciBirimi = ds.DimAngularUnits == netDxf.Units.AngleUnitType.Gradians ? OlcuAciBirimi.Grad : OlcuAciBirimi.Derece,
                OkBoyu = ds.ArrowSize,
                UzatmaAraligi = ds.ExtLineOffset,
                UzatmaTasmasi = ds.ExtLineExtend,
                OlcuCizgisiTasmasi = ds.DimLineExtend,
                OkTipi = OkTipiCevir(ds.DimArrow1?.Name)
            };
            return stil;
        }

        // DXF ok bloğu adından ok tipi (null/boş: kapalı dolu ok).
        private static OlcuOkTipi OkTipiCevir(string blokAdi)
        {
            var ad = (blokAdi ?? "").ToUpperInvariant();
            if (ad.Length == 0 || ad == "_CLOSEDFILLED") return OlcuOkTipi.KapaliDolu;
            if (ad.Contains("OBLIQUE") || ad.Contains("ARCHTICK") || ad.Contains("INTEGRAL")) return OlcuOkTipi.EgikCizgi;
            if (ad.Contains("DOT") || ad.Contains("ORIGIN")) return OlcuOkTipi.Nokta;
            if (ad.Contains("NONE")) return OlcuOkTipi.Yok;
            if (ad.Contains("OPEN")) return OlcuOkTipi.Acik;
            return OlcuOkTipi.KapaliDolu;
        }

        private static OlcuStili OlcuStiliGetir(netDxf.Tables.DimensionStyle ds)
        {
            if (ds == null)
                return null;
            if (_olcuStilleri == null)
                _olcuStilleri = new Dictionary<netDxf.Tables.DimensionStyle, OlcuStili>();
            if (!_olcuStilleri.TryGetValue(ds, out var stil))
            {
                stil = StilCevir(ds);
                if (_olcuStiliBul != null)
                    stil = _olcuStiliBul(stil) ?? stil;
                _olcuStilleri[ds] = stil;
            }
            return stil;
        }

        /// <summary>DXF ölçüsünü ölçü objesine çevirir; tanınmayan türde null.</summary>
        private static OlcuNesnesi OlcuCevir(Dimension olcu, Donusum donusum)
        {
            OlcuNesnesi sonuc;
            switch (olcu)
            {
                case LinearDimension d:
                    sonuc = new OlcuNesnesi { Tur = OlcuTuru.Dogrusal, Aci = d.Rotation };
                    Noktalar(sonuc, donusum, d.FirstReferencePoint, d.SecondReferencePoint, d.DimLinePosition, d.DimLinePosition);
                    sonuc.Aci += donusum.DonusDerece;
                    break;
                case AlignedDimension d when _dogrusalOlcular != null && d.Handle != null && _dogrusalOlcular.TryGetValue(d.Handle, out var dogrultu):
                    // Dosyada doğrusal (döndürülmüş) ölçü; netDxf hizalı okumuştur.
                    sonuc = new OlcuNesnesi { Tur = OlcuTuru.Dogrusal, Aci = dogrultu + donusum.DonusDerece };
                    Noktalar(sonuc, donusum, d.FirstReferencePoint, d.SecondReferencePoint, d.DimLinePosition, d.DimLinePosition);
                    break;
                case AlignedDimension d:
                    sonuc = new OlcuNesnesi { Tur = OlcuTuru.Hizali };
                    Noktalar(sonuc, donusum, d.FirstReferencePoint, d.SecondReferencePoint, d.DimLinePosition, d.DimLinePosition);
                    break;
                case Angular3PointDimension d:
                    sonuc = new OlcuNesnesi { Tur = OlcuTuru.Acisal };
                    Noktalar(sonuc, donusum, d.CenterPoint, d.StartPoint, d.EndPoint, d.ArcDefinitionPoint);
                    break;
                case Angular2LineDimension d:
                {
                    // Köşe iki doğrunun kesişimi; kollar her doğrunun köşeye uzak ucu.
                    if (!KesisimBul(d.StartFirstLine, d.EndFirstLine, d.StartSecondLine, d.EndSecondLine, out var kose))
                        return null;
                    Vector2 Uzak(Vector2 a, Vector2 b) => Vector2.Distance(a, kose) >= Vector2.Distance(b, kose) ? a : b;
                    sonuc = new OlcuNesnesi { Tur = OlcuTuru.Acisal };
                    Noktalar(sonuc, donusum, kose, Uzak(d.StartFirstLine, d.EndFirstLine), Uzak(d.StartSecondLine, d.EndSecondLine), d.ArcDefinitionPoint);
                    break;
                }
                case RadialDimension d:
                    sonuc = new OlcuNesnesi { Tur = OlcuTuru.Yaricap };
                    Noktalar(sonuc, donusum, d.CenterPoint, d.ReferencePoint, d.ReferencePoint, d.ReferencePoint);
                    break;
                case DiametricDimension d:
                    sonuc = new OlcuNesnesi { Tur = OlcuTuru.Cap };
                    Noktalar(sonuc, donusum, d.CenterPoint, d.ReferencePoint, d.ReferencePoint, d.ReferencePoint);
                    break;
                default:
                    return null;
            }
            // "<>" ölçülen değerdir; yalnızca "<>" ya da boş yazı, ölçülen değerin yazılması demektir.
            sonuc.Metin = string.IsNullOrEmpty(olcu.UserText) || olcu.UserText == "<>" ? "" : olcu.UserText;
            sonuc.Stil = OlcuStiliGetir(olcu.Style);
            return sonuc;
        }

        private static void Noktalar(OlcuNesnesi o, Donusum donusum, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            double x, y;
            (x, y) = donusum.Uygula(a.X, a.Y); o.AX = x; o.AY = y;
            (x, y) = donusum.Uygula(b.X, b.Y); o.BX = x; o.BY = y;
            (x, y) = donusum.Uygula(c.X, c.Y); o.CX = x; o.CY = y;
            (x, y) = donusum.Uygula(d.X, d.Y); o.DX = x; o.DY = y;
        }

        private static bool KesisimBul(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 kesisim)
        {
            kesisim = Vector2.Zero;
            double dax = a2.X - a1.X, day = a2.Y - a1.Y, dbx = b2.X - b1.X, dby = b2.Y - b1.Y;
            var payda = dax * dby - day * dbx;
            if (Math.Abs(payda) < 1e-12)
                return false;
            var t = ((b1.X - a1.X) * dby - (b1.Y - a1.Y) * dbx) / payda;
            kesisim = new Vector2(a1.X + t * dax, a1.Y + t * day);
            return true;
        }

        private static void HatchKenariEkle(HatchBoundaryPath.Edge kenar, double elevation, Donusum donusum, List<CizimNesnesi> sonuc)
        {
            switch (kenar)
            {
                case HatchBoundaryPath.Polyline kenarPolyline:
                {
                    var noktalar = new List<(double X, double Y, double Z)>(kenarPolyline.Vertexes.Length);
                    foreach (var v in kenarPolyline.Vertexes)
                        noktalar.Add((v.X, v.Y, elevation));
                    CokgenEkle(noktalar, kenarPolyline.IsClosed, donusum, sonuc);
                    break;
                }
                case HatchBoundaryPath.Line kenarCizgi:
                {
                    var (x1, y1) = donusum.Uygula(kenarCizgi.Start.X, kenarCizgi.Start.Y);
                    var (x2, y2) = donusum.Uygula(kenarCizgi.End.X, kenarCizgi.End.Y);
                    var z = donusum.UygulaZ(elevation);
                    sonuc.Add(new CizgiNesnesi { BaslangicX = x1, BaslangicY = y1, BaslangicZ = z, BitisX = x2, BitisY = y2, BitisZ = z });
                    break;
                }
                case HatchBoundaryPath.Arc kenarYay:
                {
                    var (cx, cy) = donusum.Uygula(kenarYay.Center.X, kenarYay.Center.Y);
                    var baslangic = kenarYay.IsCounterclockwise ? kenarYay.StartAngle : kenarYay.EndAngle;
                    var bitis = kenarYay.IsCounterclockwise ? kenarYay.EndAngle : kenarYay.StartAngle;
                    sonuc.Add(new YayNesnesi
                    {
                        MerkezX = cx,
                        MerkezY = cy,
                        MerkezZ = donusum.UygulaZ(elevation),
                        Yaricap = kenarYay.Radius * donusum.OlcekOrtalama,
                        BaslangicAcisi = baslangic + donusum.DonusDerece,
                        BitisAcisi = bitis + donusum.DonusDerece
                    });
                    break;
                }
            }
        }

        private static void YaziEkle(string metin, Vector3 konum, double yukseklik, double donusDerece,
            YatayHizalama hizaYatay, DikeyHizalama hizaDikey, Donusum donusum, List<CizimNesnesi> sonuc)
        {
            if (string.IsNullOrEmpty(metin))
                return;

            var (x, y) = donusum.Uygula(konum.X, konum.Y);
            sonuc.Add(new YaziNesnesi
            {
                KonumX = x,
                KonumY = y,
                KonumZ = donusum.UygulaZ(konum.Z),
                Metin = metin,
                Yukseklik = (yukseklik > 0 ? yukseklik : 1.5) * donusum.OlcekOrtalama,
                Donus = donusDerece + donusum.DonusDerece,
                HizaYatay = hizaYatay,
                HizaDikey = hizaDikey
            });
        }

        private static (YatayHizalama Yatay, DikeyHizalama Dikey) HizaCoz(TextAlignment hiza)
        {
            switch (hiza)
            {
                case TextAlignment.TopLeft: return (YatayHizalama.Sol, DikeyHizalama.Ust);
                case TextAlignment.TopCenter: return (YatayHizalama.Orta, DikeyHizalama.Ust);
                case TextAlignment.TopRight: return (YatayHizalama.Sag, DikeyHizalama.Ust);
                case TextAlignment.MiddleLeft: return (YatayHizalama.Sol, DikeyHizalama.Orta);
                case TextAlignment.MiddleCenter: return (YatayHizalama.Orta, DikeyHizalama.Orta);
                case TextAlignment.MiddleRight: return (YatayHizalama.Sag, DikeyHizalama.Orta);
                case TextAlignment.BottomLeft: return (YatayHizalama.Sol, DikeyHizalama.Alt);
                case TextAlignment.BottomCenter: return (YatayHizalama.Orta, DikeyHizalama.Alt);
                case TextAlignment.BottomRight: return (YatayHizalama.Sag, DikeyHizalama.Alt);
                case TextAlignment.BaselineLeft: return (YatayHizalama.Sol, DikeyHizalama.Alt);
                case TextAlignment.BaselineCenter: return (YatayHizalama.Orta, DikeyHizalama.Alt);
                case TextAlignment.BaselineRight: return (YatayHizalama.Sag, DikeyHizalama.Alt);
                default: return (YatayHizalama.Orta, DikeyHizalama.Orta); // Aligned/Middle/Fit için yaklaşık
            }
        }

        private static (YatayHizalama Yatay, DikeyHizalama Dikey) HizaCoz(MTextAttachmentPoint hiza)
        {
            switch (hiza)
            {
                case MTextAttachmentPoint.TopLeft: return (YatayHizalama.Sol, DikeyHizalama.Ust);
                case MTextAttachmentPoint.TopCenter: return (YatayHizalama.Orta, DikeyHizalama.Ust);
                case MTextAttachmentPoint.TopRight: return (YatayHizalama.Sag, DikeyHizalama.Ust);
                case MTextAttachmentPoint.MiddleLeft: return (YatayHizalama.Sol, DikeyHizalama.Orta);
                case MTextAttachmentPoint.MiddleCenter: return (YatayHizalama.Orta, DikeyHizalama.Orta);
                case MTextAttachmentPoint.MiddleRight: return (YatayHizalama.Sag, DikeyHizalama.Orta);
                case MTextAttachmentPoint.BottomLeft: return (YatayHizalama.Sol, DikeyHizalama.Alt);
                case MTextAttachmentPoint.BottomCenter: return (YatayHizalama.Orta, DikeyHizalama.Alt);
                case MTextAttachmentPoint.BottomRight: return (YatayHizalama.Sag, DikeyHizalama.Alt);
                default: return (YatayHizalama.Sol, DikeyHizalama.Ust);
            }
        }

        private static void CokgenEkle(IReadOnlyList<(double X, double Y, double Z)> noktalar, bool kapali, Donusum donusum, List<CizimNesnesi> sonuc)
        {
            if (noktalar.Count < 2)
                return;

            var donusmusler = new List<(double X, double Y, double Z)>(noktalar.Count);
            foreach (var n in noktalar)
            {
                var (x, y) = donusum.Uygula(n.X, n.Y);
                donusmusler.Add((x, y, donusum.UygulaZ(n.Z)));
            }

            var kenarSayisi = kapali ? donusmusler.Count : donusmusler.Count - 1;
            for (var i = 0; i < kenarSayisi; i++)
            {
                var bas = donusmusler[i];
                var bit = donusmusler[(i + 1) % donusmusler.Count];
                sonuc.Add(new CizgiNesnesi { BaslangicX = bas.X, BaslangicY = bas.Y, BaslangicZ = bas.Z, BitisX = bit.X, BitisY = bit.Y, BitisZ = bit.Z });
            }
        }

        /// <summary>MText zengin biçimlendirme kodlarını (\P, \fArial|...;, \C6; vb.) temizleyip düz metne çevirir.</summary>
        private static string MTextDuzMetin(string mtextDegeri)
        {
            if (string.IsNullOrEmpty(mtextDegeri))
                return mtextDegeri;

            var metin = mtextDegeri.Replace("\\P", "\n").Replace("\\p", "\n");
            metin = Regex.Replace(metin, @"\\[A-Za-z][^\\{}\n;]*;", "");
            metin = Regex.Replace(metin, @"\\[A-Za-z]", "");
            metin = metin.Replace("{", "").Replace("}", "");
            return metin.Trim();
        }
    }
}
