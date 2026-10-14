using System;
using System.Collections.Generic;
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
            foreach (var varlik in belge.Entities.All)
                VarlikEkle(varlik, Donusum.Kimlik, sonuc, 0, tabakaBul);
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
                    if (olcu.Block != null)
                        foreach (var altVarlik in olcu.Block.Entities)
                            VarlikEkle(altVarlik, donusum, sonuc, derinlik + 1, tabakaBul);
                    break;
            }
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
