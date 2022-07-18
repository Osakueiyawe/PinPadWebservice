using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

public class Encryption
{
	public Encryption()
	{

	}

    public string EncryptPIN(string plainMessage, string key)
    {
        TripleDESCryptoServiceProvider des;
        PasswordDeriveBytes pdb;
        MemoryStream ms;
        CryptoStream encStream;

        try
        {
            des = new TripleDESCryptoServiceProvider();

            des.IV = new byte[8];

            pdb = new PasswordDeriveBytes(key, new byte[0]);

            des.Key = pdb.CryptDeriveKey("RC2", "MD5", 128, new byte[8]);

            ms = new MemoryStream(plainMessage.Length * 2);

            encStream = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainMessage);

            encStream.Write(plainBytes, 0, plainBytes.Length);

            encStream.FlushFinalBlock();

            byte[] encryptedBytes = new byte[ms.Length];

            ms.Position = 0;

            ms.Read(encryptedBytes, 0, (int)ms.Length);

            encStream.Close();

            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Encryption Failed ", ex); 
            return ex.Message;
        }

    }

    public string DecryptPIN(string encryptedBase64, string key)
    {
        TripleDESCryptoServiceProvider des;
        PasswordDeriveBytes pdb;
        MemoryStream ms;
        CryptoStream decStream;

        try
        {
            des = new TripleDESCryptoServiceProvider();

            des.IV = new byte[8];

            pdb = new PasswordDeriveBytes(key, new byte[0]);

            des.Key = pdb.CryptDeriveKey("RC2", "MD5", 128, new byte[8]);

            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

            ms = new MemoryStream(encryptedBase64.Length);

            decStream = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);

            decStream.Write(encryptedBytes, 0, encryptedBytes.Length);

            decStream.FlushFinalBlock();

            byte[] plainBytes = new byte[ms.Length];

            ms.Position = 0;

            ms.Read(plainBytes, 0, (int)ms.Length);

            decStream.Close();

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch(Exception ex)
        {
            Utility.Log().Fatal("Encryption Failed ", ex);
            return ex.Message;
        }
    }
}