using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Enum;
using Common.Packages;

namespace XmlToKtb
{
    public static class KmEsitligihesap
    {
        public static decimal Duzenleyici(LandXMLAlignmentsAlignment alignment, decimal kilometre)
        {

            List<Array> kmEsitlikListe = new List<Array>();
            foreach (var item in alignment.StaEquation)
            {
                object[] array3 = { item.staInternal, item.staBack, item.staAhead };
                kmEsitlikListe.Add(array3);
            }

            if (kmEsitlikListe == null) { return 0; }
            var profAlign = alignment.Profile.ProfAlign;
            var profAlignItems = alignment.Profile.ProfAlign.Items.ToList();
            decimal projeKm = 0;
            List<Array> Profilsatir = new List<Array>();

            for (var i = 0; i < kmEsitlikListe.Count - 1; i++)
            {
                decimal staInternal = Convert.ToDecimal(kmEsitlikListe[i].GetValue(0));
                decimal staBack = Convert.ToDecimal(kmEsitlikListe[i].GetValue(1));
                decimal staAhead = Convert.ToDecimal(kmEsitlikListe[i].GetValue(2));
                decimal ileriKM = Convert.ToDecimal(kmEsitlikListe[i + 1].GetValue(0));
                if (kilometre > staInternal && kilometre < ileriKM)
                {
                    var x = kilometre - staInternal;
                    projeKm = x+ staAhead;
                }

            }

            return projeKm;
        }


    }

}
