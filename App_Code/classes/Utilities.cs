using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
//using System.Data.OracleClient;
using Oracle.ManagedDataAccess.Client;
using System.Text.RegularExpressions;
using log4net;

public class Utilities
{
    private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public bool checkifnumeric(string txt)
    {
        double number;
        bool result = double.TryParse(txt, out number);

        if (result == false)
        {
            return false;
        }
        else { return true; }

    }

    public object[] getLogos()
    {
        object[] logos = new object[2];

        try
        {
            using (SqlCommand sqlcomm = new SqlCommand())
            {
                using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
                {
                    sqlcomm.Connection = PinPadCon;

                    sqlcomm.CommandText = "usp_ImagesSelect";
                    sqlcomm.CommandType = CommandType.StoredProcedure;

                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }

                    using (SqlDataReader result = sqlcomm.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (result.Read())
                        {
                            logos[0] = (byte[])result["Header"];
                            logos[1] = (byte[])result["Footer"];
                            return logos;
                        }
                        else
                        {
                            logos[0] = null;
                            logos[1] = null;
                            return logos;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
            logos[0] = null;
            logos[1] = null;
            return logos;
        }
    }
    public string[] getCustomerID(string cardNumber)
    {
        EncryptionLib.Encrypt enc = new EncryptionLib.Encrypt();

        string encryptedpan = enc.Encrypt_TrpDes(cardNumber.Substring(6, 10));

        if (isPowerCardBIN(cardNumber))
        {
            return getCustomerIDPowerCard(cardNumber);
        }

        else
        {
            return getBankCardCustomerID(encryptedpan);

        }
    }

    public string[] getBankCardCustomerID(string encryptedpan)
    {
        string[] response = new string[2];

        using (SqlCommand sqlcomm = new SqlCommand())
        {
            using (SqlConnection BankCardCon = new SqlConnection(ConfigurationManager.AppSettings["BankCardCon"]))
            {
                try
                {
                    sqlcomm.Connection = BankCardCon;
                    sqlcomm.CommandText = "PinPadCardHolderEnquiry";
                    sqlcomm.CommandType = CommandType.StoredProcedure;
                    sqlcomm.Parameters.AddWithValue("@CardNumber", encryptedpan);

                    if (BankCardCon.State == ConnectionState.Closed)
                    {
                        BankCardCon.Open();
                    }

                    using (SqlDataReader result = sqlcomm.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (result.Read())
                        {
                            response[0] = result["Branch_Code"].ToString();
                            response[1] = result["Customer_No"].ToString();
                            return response;
                        }
                        else
                        {
                            response = null;
                            return response;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal("Database Call Failed ", ex);
                    response = null;
                    return response;
                }
                finally
                {
                    BankCardCon.Close();
                }
            }
        }
    }

    public bool isPowerCardBIN(string cardnumber)
    {
        bool bankCardBIN = false;
        string[] binarray = null;
        string powercardBIN = ConfigurationManager.AppSettings["PowerCarBIN"].ToString();
        string cardbin = cardnumber.Substring(0, 8);
        binarray = powercardBIN.Split(',');

        foreach (string a in binarray)
        {

            if (a.Trim().Equals(cardbin))
            {
                bankCardBIN = true;
            }
        }

        return bankCardBIN;
    }

    public string[] getCustomerIDPowerCard(string cardNumber)
    {
        string clientcode = "";

        string[] response = new string[2];

        using (OracleCommand sqlcomm = new OracleCommand())
        {
            using (OracleConnection PowerCardCon = new OracleConnection(ConfigurationManager.AppSettings["ConnectStrPowerCard"]))
            {
                try
                {
                    sqlcomm.Connection = PowerCardCon;
                    sqlcomm.CommandText = "select client_code from card where card_number = " + cardNumber;
                    sqlcomm.CommandType = CommandType.Text;

                    if (PowerCardCon.State == ConnectionState.Closed)
                    {
                        PowerCardCon.Open();
                    }

                    using (OracleDataReader result = sqlcomm.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (result.Read())
                        {
                            clientcode = result["client_code"].ToString();
                            if (string.IsNullOrEmpty(clientcode))
                            {
                                response = null;
                            }
                            else
                            {
                                response[0] = clientcode.Substring(0, 3);
                                response[1] = clientcode.Substring(3, 6);
                            }

                            return response;
                        }
                        else
                        {
                            response = null;
                            return response;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal("Database Call Failed ", ex);
                    response = null;
                    return response;
                }
                finally
                {
                    PowerCardCon.Close();
                }
            }
        }
    }

    public string SafeSqlLiteral(object theValue, object theLevel)
    {
        string strValue = Convert.ToString(theValue);

        int intLevel = (int)theLevel;

        if (strValue != null)
        {
            if (intLevel > 0)
            {
                strValue = strValue.Replace("'", "''"); // Most important one! This line alone can prevent most injection attacks
                strValue = strValue.Replace("--", "");
                strValue = strValue.Replace("[", "[[]");
                strValue = strValue.Replace("%", "[%]");
            }
            if (intLevel > 1)
            {
                string[] myArray = new string[] { "xp_ ", "update ", "insert ", "select ", "drop ", "alter ", "create ", "rename ", "delete ", "replace ", "shutdown " };
                int i = 0;
                int i2 = 0;
                int intLenghtLeft = 0;
                for (i = 0; i < myArray.Length; i++)
                {
                    string strWord = myArray[i];
                    Regex rx = new Regex(strWord, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    MatchCollection matches = rx.Matches(strValue);
                    i2 = 0;
                    foreach (Match match in matches)
                    {
                        GroupCollection groups = match.Groups;
                        intLenghtLeft = groups[0].Index + myArray[i].Length + i2;
                        strValue = strValue.Substring(0, intLenghtLeft - 1) + "&nbsp;" + strValue.Substring(strValue.Length - (strValue.Length - intLenghtLeft), strValue.Length - intLenghtLeft);
                        i2 += 5;
                    }
                }
            }
            return strValue;
        }
        else
        {
            return strValue;
        }
    }

    public string ComposeError(string code, string text)
    {
        string xmlstring = string.Concat("<Response><CODE>", code, "</CODE><MESSAGE>", text, "</MESSAGE></Response>");

        return xmlstring;
    }

    public string ComposeReturnMessage(string code, string text, string nuban, string customerName, string bvn, string oldAccountNumber, string availableBalance)
    {
        string xmlstring = string.Concat("<Response><CODE>", code, "</CODE><MESSAGE>", text, "</MESSAGE><NUBAN>", nuban, "</NUBAN><CUSTOMERNAME>", customerName, "</CUSTOMERNAME><BVN>", bvn, "</BVN><OLDACCOUNTNUMBER>", oldAccountNumber, "</OLDACCOUNTNUMBER><AVAILABLEBALANCE>", availableBalance, "</AVAILABLEBALANCE></Response>");

        return xmlstring;
    }
}