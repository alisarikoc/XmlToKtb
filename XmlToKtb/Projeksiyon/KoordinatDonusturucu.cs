using System;

namespace XmlToKtb.Projeksiyon
{
    /// <summary>Bir projeksiyon sistemindeki (sağa,yukarı) koordinatını başka bir sisteme çevirir.</summary>
    public static class KoordinatDonusturucu
    {
        public static bool Donustur(
            ProjeksiyonSistemi kaynakSistem, DilimBilgisi? kaynakDilim, double kaynakSaga, double kaynakYukari,
            ProjeksiyonSistemi hedefSistem, DilimBilgisi? hedefDilim,
            out double hedefSaga, out double hedefYukari)
        {
            hedefSaga = 0;
            hedefYukari = 0;

            if (kaynakSistem == ProjeksiyonSistemi.Tanimsiz || hedefSistem == ProjeksiyonSistemi.Tanimsiz)
                return false;
            if (ProjeksiyonBilgisi.DilimGereklidir(kaynakSistem) && kaynakDilim == null)
                return false;
            if (ProjeksiyonBilgisi.DilimGereklidir(hedefSistem) && hedefDilim == null)
                return false;

            double enlem, boylam;
            if (kaynakSistem == ProjeksiyonSistemi.Cografi)
            {
                boylam = Radyana(kaynakSaga);
                enlem = Radyana(kaynakYukari);
            }
            else
            {
                var kaynakElipsoid = ProjeksiyonBilgisi.ElipsoidGetir(kaynakSistem);
                GaussKruger.Ters(kaynakSaga, kaynakYukari, Radyana(kaynakDilim.Value.MerkeziMeridyenDerece), kaynakElipsoid,
                    ProjeksiyonBilgisi.OlcekFaktoruGetir(kaynakSistem), kaynakDilim.Value.SagaKaydirma, 0,
                    out enlem, out boylam);
            }

            var kaynakEd50 = kaynakSistem == ProjeksiyonSistemi.Ed50;
            var hedefEd50 = hedefSistem == ProjeksiyonSistemi.Ed50;
            if (kaynakEd50 && !hedefEd50)
                DatumDonusumu.Ed50DenWgs84e(enlem, boylam, out enlem, out boylam);
            else if (!kaynakEd50 && hedefEd50)
                DatumDonusumu.Wgs84DenEd50e(enlem, boylam, out enlem, out boylam);

            if (hedefSistem == ProjeksiyonSistemi.Cografi)
            {
                hedefSaga = Derece(boylam);
                hedefYukari = Derece(enlem);
            }
            else
            {
                var hedefElipsoid = ProjeksiyonBilgisi.ElipsoidGetir(hedefSistem);
                GaussKruger.Ileri(enlem, boylam, Radyana(hedefDilim.Value.MerkeziMeridyenDerece), hedefElipsoid,
                    ProjeksiyonBilgisi.OlcekFaktoruGetir(hedefSistem), hedefDilim.Value.SagaKaydirma, 0,
                    out hedefSaga, out hedefYukari);
            }

            return true;
        }

        private static double Radyana(double derece) => derece * Math.PI / 180.0;
        private static double Derece(double radyan) => radyan * 180.0 / Math.PI;
    }
}
