using System.IO;
using ACadSharp.IO;
using netDxf;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// AutoCAD DWG dosyasını okur (ACadSharp). DWG önce bellekte DXF'e yazılır, sonra netDxf ile yüklenir; böylece
    /// tabaka, renk, kalınlık, blok ve çoklu doğru eşlemesi DXF içe aktarmayla (DxfIceAktarici) aynı kalır.
    /// </summary>
    public static class DwgOkuyucu
    {
        /// <summary>DWG dosyasını netDxf belgesine çevirir. ACadSharp'ın desteklemediği objeler atlanır.</summary>
        public static DxfDocument Oku(string dosya)
        {
            var cad = DwgReader.Read(dosya, null);

            byte[] dxf;
            using (var bellek = new MemoryStream())
            {
                using (var yazici = new DxfWriter(bellek, cad, false))
                    yazici.Write();
                // Yazıcı akışı kapatsa da MemoryStream'in içeriği alınabilir.
                dxf = bellek.ToArray();
            }
            // MLEADER'lar (netDxf okumaz) okunmuş DWG belgesinden alınır.
            return DxfIceAktarici.Yukle(dxf, cad);
        }
    }
}
