using System;
using System.Security.Cryptography;
using System.Text;
using log4net;

public class Utility
{   
	public Utility()
	{
        
    }

    public string GeneratePassword()
    {
        char[] chars = new char[62];
        chars = "1234567890".ToCharArray();

        byte[] data = new byte[1];
        RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
        crypto.GetNonZeroBytes(data);
        data = new byte[6];
        crypto.GetNonZeroBytes(data);
        StringBuilder result = new StringBuilder(6);
        foreach (byte b in data)
        {
            result.Append(chars[b % (chars.Length - 1)]);
        }
        Log().Info("Password Generated");
        return result.ToString();
    }

    public bool ValidateIP(string IPAddress_list)
    {
        bool result = true;
        char[] charsep = {','};
        string[] iplist;

        iplist = IPAddress_list.Split(charsep, StringSplitOptions.RemoveEmptyEntries);
        Log().Info(string.Concat("IP Validated for ", IPAddress_list, "Result ", result));
        return result;
    }

    public int[] ConvertByteToInt(byte[] data)
    {
        int[] byteint = (int[])Array.CreateInstance(typeof(int), data.Length);

        return byteint;
    }

    public static ILog Log()
    {
        ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        return Log;
    } 
}