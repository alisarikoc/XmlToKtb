using System;

public class Class1
{
	public Class1()
	{
	    public static decimal yuvarla(object deger)
	    {
	        return Math.Round(Convert.ToDecimal(deger.ToString()), 4, MidpointRounding.AwayFromZero);
	    }
	    public static string SifirDolgusu(decimal deger)
	    {
	        return Convert.ToInt64(Math.Abs(deger * 1000)).ToString().PadLeft(16, '0');
	    }
    }
}
