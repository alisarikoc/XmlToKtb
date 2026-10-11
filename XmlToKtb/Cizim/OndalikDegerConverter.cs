using System;
using System.ComponentModel;
using System.Globalization;

namespace XmlToKtb.Cizim
{
    /// <summary>
    /// Özellikler panelindeki double değerleri, GorunumAyarlari.OndalikBasamakSayisi ile belirlenen
    /// ondalık basamak sayısına göre biçimlendirir. Düzenleme (kullanıcının yeni değer girmesi) standart
    /// DoubleConverter davranışıyla aynı kalır, sadece görüntüleme biçimi değişir.
    /// </summary>
    public class OndalikDegerConverter : DoubleConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is double sayi)
                return sayi.ToString("F" + GorunumAyarlari.OndalikBasamakSayisi, culture ?? CultureInfo.CurrentCulture);

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
