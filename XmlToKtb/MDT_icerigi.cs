using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Modeller;

namespace XmlToKtb
{
    public static class MDT_icerigi
    {
        public static string Excellistesi(List<Array> mdtdata)
        {
            List<Array> ExcelListeArray= new List<Array>();
            StringBuilder ExcelListe=new StringBuilder();
            NivelmanNokta nivelmanNokta = new NivelmanNokta();
            bool noktaokuması = false;
            foreach (var satir in mdtdata)
            {
                char[] separator = { '�',' ' };
                var satir2=Convert.ToString(satir.GetValue(0)).Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (Convert.ToString(satir2.GetValue(0)) == "Nokta") noktaokuması = true;
                if (noktaokuması && satir2.Length == 6)
                {
                    nivelmanNokta.NokNo = Convert.ToString(satir2.GetValue(0));
                    nivelmanNokta.NokBilgi = Convert.ToString(satir2.GetValue(1));
                    nivelmanNokta.Okuma = Convert.ToDecimal(satir2.GetValue(2));
                    nivelmanNokta.Mesafe = Convert.ToDecimal(satir2.GetValue(3));
                    nivelmanNokta.Kot = Convert.ToDecimal(satir2.GetValue(4));
                    nivelmanNokta.Not = Convert.ToString(satir2.GetValue(5));
                    object[] array = { nivelmanNokta.NokNo, nivelmanNokta.NokBilgi, nivelmanNokta.Okuma, nivelmanNokta.Mesafe, nivelmanNokta.Kot, nivelmanNokta.Not };
                    ExcelListeArray.Add(array);
                }
            }
            
            for (int i = 0; i < ExcelListeArray.Count; i++)
            {
                nivelmanNokta.NokNo = ExcelListeArray[i].GetValue(0).ToString(); 
                nivelmanNokta.NokBilgi = ExcelListeArray[i].GetValue(1).ToString();
                nivelmanNokta.Okuma = Convert.ToDecimal(ExcelListeArray[i].GetValue(2));
                nivelmanNokta.Mesafe =Convert.ToDecimal(ExcelListeArray[i].GetValue(3));
                
                if (ExcelListeArray[i].GetValue(1).ToString() == "Geri")
                {
                    if (i == 0)
                    {
                        ExcelListe.AppendLine(nivelmanNokta.NokNo + "," + " " + "," + ExcelListeArray[i].GetValue(2) + "," + "," + ","  + ExcelListeArray[i].GetValue(3) + "," + ",");
                    }
                }
                if (nivelmanNokta.NokBilgi == "Orta")
                {
                    var noktageri = " "; var noktaileri = " ";   
                    var noktaorta = nivelmanNokta.Okuma;
                    ExcelListe.AppendLine(nivelmanNokta.NokNo + "," + " " + "," + noktageri + "," + noktaorta + "," + noktaileri );
                }

                if (ExcelListeArray[i].GetValue(1).ToString()== "ileri")
                {
                    var geriokuma = " ";
                    var gerimesafe = " ";
                    var noktaorta = " ";
                    if (i < ExcelListeArray.Count - 1)
                    {
                        geriokuma = ExcelListeArray[i + 1].GetValue(2).ToString();
                        gerimesafe = ExcelListeArray[i + 1].GetValue(3).ToString();
                    }
                    ExcelListe.AppendLine(nivelmanNokta.NokNo + "," + " " + "," + geriokuma + "," + noktaorta + "," + nivelmanNokta.Okuma+","+ gerimesafe + ","+nivelmanNokta.Mesafe);
                }
            }
            return ExcelListe.ToString();
        }
    }
}
