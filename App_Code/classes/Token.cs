using System;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using Vasco;
using System.Globalization;
using log4net;

/// <summary>
/// Summary description for ValidateToken
/// </summary>
public class Token
{
    private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    private String xmlstring = null;
    private ReturnValue rt = new ReturnValue();

	public ReturnValue ValidateTokenOld(String uid, String tokenVal)
	{
        String result = null;
        int retval = 1;
        String tokendpx;
        AAL2Wrap dp = new AAL2Wrap();
        try
        {
            DataRow tokendpx_row = VerifyTokenSQL(uid,"USER");
            tokendpx = tokendpx_row["VDSTOKENBLOB1"].ToString();

            if (tokendpx_row != null)
            {
                retval = dp.AAL2VerifyPassword(ref tokendpx, tokenVal.Trim(), null);

                if (retval == 0)
                {
                    rt.value = "1000";
                    rt.message = "SUCCESS";
                }
                else
                {
                    rt.value = "1002";
                    rt.message = "FAILED";
                }
            }
            else
            {
                rt.value = "1002";
                rt.message = "FAILED";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Token Infrastructure Failed ", ex);        
            rt.value = "1002";
            rt.message = ex.Message.Replace("'", "");
        }

        return rt;
	}

    public String ValidateToken(String uid, String tokenVal, String usertype, String purp1, String channel1)    //UID is customers account no, tokenval is pawwsord generated from token
    {
        String result = null;
        //int result = 0;
        int retval = 1;
        String retmess = "";
        AAL2Wrap dp = null;
        String blobold = null;
        String tokendpx, tokenserial = "0";
        String validation_status = "";
        Boolean blob_change = false;
        int first_time_flag = 2; // "NOT VERIFIED"
        int reset_IND = 0, timeDrift = 0;
        DateTime token_date;
        Vasco.AAL2Wrap.TDigipassInfo info, info1;
        Vasco.AAL2Wrap.TDigipassInfoEx infox, infox1, infox2;
        int fcount = 0;

        try
        {
            xmlstring = "<Response>";

            DataRow tokendpx_row = VerifyTokenSQL(uid, usertype);
            
            if (tokendpx_row != null)
            {
                tokendpx = tokendpx_row["VDSTOKENBLOB1"].ToString();
                tokenserial = tokendpx_row["VDSTOKENSERIAL"].ToString().Trim();
                reset_IND = Convert.ToInt32(tokendpx_row["RESET_INDICATOR"]);

                if (reset_IND == 0)
                {
                    dp = new AAL2Wrap();
                    dp.KParams.ITimeWindow = 3;

                    infox = dp.AAL2GetTokenInfoEx(tokendpx);

                    blobold = new String(tokendpx.ToCharArray());

                    //Check if token has been reset
                    if (tokendpx_row["RESET_FLAG"].ToString() == "0")  //Not reset
                    {
                        //dp.AAL2ResetTokenInfo(ref tokendpx);
                        //first_time_flag = 0;
                    }
                    else
                    {
                        //Check last use
                        first_time_flag = 1;
                        String dateformat = "ddd MMM dd HH:mm:ss yyyy";

                        token_date = DateTime.ParseExact(infox.LastTimeUsed, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

                        TimeSpan tm = DateTime.Now.Subtract(token_date);
                        if (tm.Days > 48)
                        {
                            timeDrift = 1;
                            //dp.AAL2ResetTokenInfo(ref tokendpx);
                            //first_time_flag = 1;
                        }
                        else
                        {
                            if (infox.SyncWindow == "YES")
                            {
                                dp.AAL2ResetTokenInfo(ref tokendpx);
                                //first_time_flag = 1;
                            }
                        }
                    }

                    //Authenticate

                    if (blobold != tokendpx)
                        blob_change = true;

                    infox1 = dp.AAL2GetTokenInfoEx(tokendpx);

                    retval = dp.AAL2VerifyPassword(ref tokendpx, tokenVal.Trim(), null);

                    if (tokendpx_row["RESET_FLAG"].ToString() == "0" && retval == 0)
                    {
                        first_time_flag = 1;
                    }

                    ////return of 0 mean success otherwise failure
                    if (blobold != tokendpx)
                        blob_change = true;


                    retmess = dp.getError(retval);
                    result = retmess;

                    //Check return value to determine response


                    infox2 = dp.AAL2GetTokenInfoEx(tokendpx);

                    result = UpdateTokenSQL(uid, tokendpx, usertype, retval, first_time_flag);

                    if (retval == 0 && first_time_flag == 0)
                    {
                        xmlstring = xmlstring + "<CODE>1001</CODE>";
                        validation_status = "Token Validation Not Successful";
                        xmlstring = xmlstring + "<Error>Token validation not successful. Please generate a new transaction code and try again.</Error>";
                    }
                    else if (retval == 0 && first_time_flag == 1 && result == "SUCCESS")
                    {

                        xmlstring = xmlstring + "<CODE>1000</CODE>";
                        validation_status = "Token Validation Successful";
                        xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                    }
                    else
                    {
                        if (retval == 201)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Repeated";
                            xmlstring = xmlstring + "<Error>You entered already used code. Please generate a new transaction code and try again.</Error>";
                        }
                        else if (retval == -202)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Too Small";
                            xmlstring = xmlstring + "<Error>You did not enter the complete code. Please generate a new transaction code and try again.</Error>";
                        }
                        else if (retval == -203)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Too Large";
                            xmlstring = xmlstring + "<Error>You entered too long code. Please generate a new transaction code and try again.</Error>";
                        }
                        else if (retval == -205)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Not Decimal";
                            xmlstring = xmlstring + "<Error>You entered a character in the code. Please generate a new transaction code and try again.</Error>";
                        }
                        else
                        {


                            //Reset token
                            int res1;
                            result = UpdateTokenResetFlag(uid, usertype, timeDrift);
                            if (int.TryParse(result, out res1) == true)
                            {
                                if (res1 >= 5)
                                {
                                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                                    validation_status = "Token Locked.";
                                    xmlstring = xmlstring + "<Error>Your token has been locked due to several failed attempts. Please logoff and login again to reconfirm your token.</Error>";
                                }
                                else
                                {
                                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                                    validation_status = "Invalid Transaction Code";
                                    xmlstring = xmlstring + "<Error>Invalid Transaction Code. Please generate a new transaction code and try again.</Error>";
                                }
                            }
                            else
                            {
                                xmlstring = xmlstring + "<CODE>1001</CODE>";
                                validation_status = "Invalid Transaction Code";
                                xmlstring = xmlstring + "<Error>Invalid Transaction Code. Please generate a new transaction code and try again.</Error>";
                            }
                        }
                    }
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    validation_status = "Token Locked.";
                    xmlstring = xmlstring + "<Error>Your token has been locked due to several failed attempts. Please logoff and login again to reconfirm your token.</Error>";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                validation_status = "Token Validation Failed";
                xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
                result = "Cannot Find Token Info";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Token Infrastructure Failed ", ex); 
            Log.Fatal("TOken Call Failed ", ex);
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            validation_status = "Token Validation Failed";
            xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
            result = ex.Message.Replace("'","");
        }

        string statusdesc = "Retval=" + retval.ToString() + "(" + retmess + ")" + " first_time=" + first_time_flag.ToString() + " result=" + result + " Validation Result = " + validation_status;
        xmlstring = xmlstring + "</Response>";

        //Log validation data
        UpdateTokenValidationLog(uid, tokenserial, statusdesc, usertype, purp1, channel1);
        Log.Info("Token Call Success");

        return xmlstring;
    }

    public String ResetUserToken(string uid, String tokenVal, string tokenID, string usertype, string purp1, string channel1) //UID is customers account no, tokenval is pawwsord generated from token
    {
        String result = null;
        //int result = 0;
        int retval = 1;
        String retmess = "";
        AAL2Wrap dp = null;
        String blobold = null;
        String tokendpx, tokenserial = "0";
        String validation_status = "";
        Boolean blob_change = false;
        int first_time_flag = 2; // "NOT VERIFIED"
        //int reset_IND = 0, timeDrift = 0;
        DateTime token_date;
        Vasco.AAL2Wrap.TDigipassInfo info, info1;
        Vasco.AAL2Wrap.TDigipassInfoEx infox, infox1, infox2;
        //int fcount = 0;
        int valtoken = 0;

        try
        {
            xmlstring = "<Response>";

            //Check if tokenID is valid

            valtoken = ValidateTokenID(uid, tokenID, usertype);

            if (valtoken > 0)
            {
                DataRow tokendpx_row = VerifyTokenSQL(uid.ToString(), usertype);

                if (tokendpx_row != null)
                {
                    tokendpx = tokendpx_row["VDSTOKENBLOB1"].ToString();
                    tokenserial = tokendpx_row["VDSTOKENSERIAL"].ToString().Trim();

                    dp = new AAL2Wrap();
                    dp.KParams.ITimeWindow = 3;

                    infox = dp.AAL2GetTokenInfoEx(tokendpx);

                    blobold = new String(tokendpx.ToCharArray());

                    retval = dp.AAL2VerifyPassword(ref tokendpx, tokenVal.Trim(), null);
                    
                    if (blobold != tokendpx)
                        blob_change = true;

                    if (retval == 0)
                    {
                        first_time_flag = 1;

                        if (blobold != tokendpx)
                            blob_change = true;
                    }
                    else
                    {
                        String dateformat = "ddd MMM dd HH:mm:ss yyyy";

                        token_date = DateTime.ParseExact(infox.LastTimeUsed, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

                        TimeSpan tm = DateTime.Now.Subtract(token_date);
                        if (tm.Days > 48)
                        {
                            dp.AAL2ResetTokenInfo(ref tokendpx);
                            retval = dp.AAL2VerifyPassword(ref tokendpx, tokenVal.Trim(), null);
                            if (retval == 0)
                            {
                                first_time_flag = 1;
                            }
                        }
                    }

                    infox1 = dp.AAL2GetTokenInfoEx(tokendpx);

                    retmess = dp.getError(retval);
                    result = retmess;

                    infox2 = dp.AAL2GetTokenInfoEx(tokendpx);

                    result = UpdateTokenSQL(uid.ToString(), tokendpx, usertype, retval, first_time_flag);

                    if (retval == 0 && first_time_flag == 0)
                    {
                        xmlstring = xmlstring + "<CODE>1001</CODE>";
                        validation_status = "Token Validation Not Successful";
                        xmlstring = xmlstring + "<Error>Token validation not successful. Please generate a new transaction code and try again.</Error>";
                    }
                    else if (retval == 0 && first_time_flag == 1 && result == "SUCCESS")
                    {

                        xmlstring = xmlstring + "<CODE>1000</CODE>";
                        validation_status = "Token Validation Successful";
                        xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                    }
                    else
                    {
                        if (retval == 201)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Repeated";
                            xmlstring = xmlstring + "<Error>You entered already used code. Please generate a new transaction code and try again.</Error>";
                        }
                        else if (retval == -202)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Too Small";
                            xmlstring = xmlstring + "<Error>You did not enter the complete code. Please generate a new transaction code and try again.</Error>";
                        }
                        else if (retval == -203)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Too Large";
                            xmlstring = xmlstring + "<Error>You entered too long code. Please generate a new transaction code and try again.</Error>";
                        }
                        else if (retval == -205)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Not Decimal";
                            xmlstring = xmlstring + "<Error>You entered a character in the code. Please generate a new transaction code and try again.</Error>";
                        }
                        else
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Validation failed";
                            xmlstring = xmlstring + "<Error>Invalid Transaction code. Please visit the bank to reset your token if this error persist</Error>";
                        }
                    }
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    validation_status = "Token Validation Failed";
                    xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
                    result = "Cannot Find Token Info";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                validation_status = "Invalid User ID";
                xmlstring = xmlstring + "<Error>Token ID validation not successful. Please enter the correct Token serial number at the back of the device.</Error>";
            }          
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Token Infrastructure Failed ", ex); 
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            validation_status = "Token Validation Failed";
            xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
            result = ex.Message.Replace("'", "");
        }

        string statusdesc = "Retval=" + retval.ToString() + "(" + retmess + ")" + " first_time=" + first_time_flag.ToString() + " result=" + result + " Validation Result = " + validation_status;
        xmlstring = xmlstring + "</Response>";

        UpdateTokenValidationLog(uid.ToString(), tokenserial, statusdesc, usertype, purp1, channel1);

        return xmlstring;
    }

    public String ResetToken(string uid, string tokenID, string usertype, string purp1, string channel1) 
    {
        String result = null;
        //int result = 0;
        int retval = 1;
        String retmess = "";
        AAL2Wrap dp = null;
        String blobold = null;
        String tokendpx, tokenserial = "0";
        String validation_status = "";
        Boolean blob_change = false;
        int first_time_flag = 2; // "NOT VERIFIED"
        Vasco.AAL2Wrap.TDigipassInfo info, info1;
        Vasco.AAL2Wrap.TDigipassInfoEx infox, infox1, infox2;
        //int fcount = 0;
        int valtoken = 0;

        try
        {
            xmlstring = "<Response>";

            valtoken = ValidateTokenID(uid, tokenID, usertype);

            if (valtoken > 0)
            {
                DataRow tokendpx_row = VerifyTokenSQL(uid.ToString(), usertype);

                if (tokendpx_row != null)
                {
                    tokendpx = tokendpx_row["VDSTOKENBLOB1"].ToString();
                    tokenserial = tokendpx_row["VDSTOKENSERIAL"].ToString().Trim();

                    dp = new AAL2Wrap();
                    dp.KParams.ITimeWindow = 3;

                    infox = dp.AAL2GetTokenInfoEx(tokendpx);

                    blobold = new String(tokendpx.ToCharArray());
                    
                    dp.AAL2ResetTokenInfo(ref tokendpx);
                    retval = 0;
                    first_time_flag = 1;

                    if (blobold != tokendpx)
                        blob_change = true;

                    infox1 = dp.AAL2GetTokenInfoEx(tokendpx);

                    result = UpdateTokenSQL(uid.ToString(), tokendpx, usertype, retval, first_time_flag);
                    
                    if (retval == 0 && first_time_flag == 1 && result == "SUCCESS")
                    {

                        xmlstring = xmlstring + "<CODE>1000</CODE>";
                        validation_status = "Token Reset Successful";
                        xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                    }
                    else
                    {                                                
                        xmlstring = xmlstring + "<CODE>1001</CODE>";
                        validation_status = "Token Validation failed";
                        xmlstring = xmlstring + "<Error>Invalid Transaction code. Please visit the bank to reset your token if this error persist</Error>";                        
                    }               
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    validation_status = "Token Validation Failed";
                    xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
                    result = "Cannot Find Token Info";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                validation_status = "Invalid User ID or Token ID";
                xmlstring = xmlstring + "<Error>Token ID validation not successful. Please enter the correct Token serial number at the back of the device.</Error>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Token Infrastructure Failed ", ex); 
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            validation_status = "Token Validation Failed";
            xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
            result = ex.Message.Replace("'", "");
        }

        string statusdesc = "Retval=" + retval.ToString() + "(" + retmess + ")" + " first_time=" + first_time_flag.ToString() + " result=" + result + " Validation Result = " + validation_status;
        xmlstring = xmlstring + "</Response>";

        UpdateTokenValidationLog(uid.ToString(), tokenserial, statusdesc, usertype, purp1, channel1);

        return xmlstring;
    }

    private DataRow VerifyTokenSQL(string username, String type)
    {
        String result = null;
        SqlDataReader reader;
        DataTable dt_token = new DataTable();
        SqlDataAdapter adpt;
        DataRow dr_token = null;
        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["tokenConnString"].ToString());
        SqlCommand comm = new SqlCommand("SelectTokenDPData", conn);

        if(type == "ADMIN")
            comm = new SqlCommand("SelectAdminTokenDPData", conn);
        else
            comm = new SqlCommand("SelectTokenDPData", conn);

        comm.Parameters.AddWithValue("@UserID", username);
        comm.CommandType = CommandType.StoredProcedure;

        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        adpt = new SqlDataAdapter(comm);
        adpt.Fill(dt_token);

        if (dt_token.Rows.Count > 0)
        {
            dr_token = dt_token.Rows[0];
        }

        //return result;

        return dr_token;
    }

    private String UpdateTokenSQL(string username, string dpData, String type, int retVal, int firstTime)
    {
        String res = "";
        int rows = 0;
        using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["tokenConnString"].ToString()))
        {
            SqlCommand cmdUser; 
            if(type == "ADMIN")
                cmdUser = new SqlCommand("UpdateAdminToken", conn);
            else
                cmdUser = new SqlCommand("UpdateToken", conn);

            cmdUser.CommandType = CommandType.StoredProcedure;
            cmdUser.Parameters.AddWithValue("@USERID", username);
            cmdUser.Parameters.AddWithValue("@vdstokenblob1", dpData);
            cmdUser.Parameters.AddWithValue("@retVal", retVal);
            cmdUser.Parameters.AddWithValue("@firstTime", firstTime);

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                rows = cmdUser.ExecuteNonQuery();
                if (rows > 0)
                    res = "SUCCESS";
                else
                    res = "FAILED";
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex); 
                res = ex.Message;
            }
            conn.Close();
        }

        return res;
    }

    private String UpdateTokenResetFlag_OLD(string username, String type)
    {
        String res = "";
        int rows = 0;
        using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["tokenConnString"].ToString()))
        {
            SqlCommand cmdUser;

            if(type == "ADMIN")
                cmdUser = new SqlCommand("UpdateAdminTokenResetFlag", conn);
            else
                cmdUser = new SqlCommand("UpdateTokenResetFlag", conn);

            cmdUser.CommandType = CommandType.StoredProcedure;
            cmdUser.Parameters.AddWithValue("@USERID", username);
            
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                rows = cmdUser.ExecuteNonQuery();
                if (rows > 0)
                    res = "SUCCESS";
                else
                    res = "FAILED";                
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex); 
                res = ex.Message;
            }
            conn.Close();
        }

        return res;
    }

    private string UpdateTokenResetFlag(string username, String type, int timeDrift)
    {
        String res = "";
        int rows = 0;
        using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["tokenConnString"].ToString()))
        {
            SqlCommand cmdUser;

            if (type == "ADMIN")
                cmdUser = new SqlCommand("UpdateAdminTokenResetFlag", conn);
            else
                cmdUser = new SqlCommand("UpdateTokenResetFlag", conn);

            cmdUser.CommandType = CommandType.StoredProcedure;
            cmdUser.Parameters.AddWithValue("@USERID", username);
            cmdUser.Parameters.AddWithValue("@timeDrift", timeDrift);

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                rows = Convert.ToInt32(cmdUser.ExecuteScalar());
                res = rows.ToString();
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex); 
                res = ex.Message;
            }
            finally
            {
                conn.Close();
            }
        }

        return res;
    }

    private int UpdateTokenValidationLog(string userid, string tokenserial, string statusdesc, string type, string purpose, string channel)
    {
        string res = "";

        int rows = 0;

        SqlConnection conn2 = null;

        try
        {
                SqlCommand cmdUser;

                if (type == "ADMIN")
                {
                    if (bool.Parse(ConfigurationManager.AppSettings["UseAccessManager"]))
                    {
                        conn2 = new SqlConnection(ConfigurationManager.ConnectionStrings["AccessManagerConnString"].ToString());
                    }
                    else
                    {
                        conn2 = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());
                    }

                    cmdUser = new SqlCommand("dbo.UpdateAdminTokenValidationLog", conn2)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmdUser.Parameters.AddWithValue("@USERID", userid);
                }
                else
                {
                    //if (bool.Parse(ConfigurationManager.AppSettings["UseAccessManager"]))
                    //{
                    //    conn2 = new SqlConnection(ConfigurationManager.ConnectionStrings["AccessManagerConnString"].ToString());
                    //}
                    //else
                    //{
                        conn2 = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());
                    //}

                    cmdUser = new SqlCommand("dbo.UpdateTokenValidationLog", conn2)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmdUser.Parameters.AddWithValue("@USERID", decimal.Parse(userid));
                }

                cmdUser.Parameters.AddWithValue("@STATUSDESC", statusdesc);
                cmdUser.Parameters.AddWithValue("@TOKENID", tokenserial);
                cmdUser.Parameters.AddWithValue("@PURPOSE", purpose);
                cmdUser.Parameters.AddWithValue("@CHANNEL", channel);

                try
                {
                    if (conn2.State == ConnectionState.Closed)
                    {
                        conn2.Open();
                    }

                    rows = cmdUser.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    ErrHandler.WriteError("An exception occurred in UpdateTokenValidationLog method. Message - " + ex.Message + "|StackTrace - " + ex.StackTrace, "ValidateAdminUserOffSitewithAppver");
                    res = ex.Message;
                    rows = 0;
                }

                conn2.Close();
        }
        catch(Exception ex)
        {
            ErrHandler.WriteError("An exception occurred in UpdateTokenValidationLog method. Message - " + ex.Message + "|StackTrace - " + ex.StackTrace, "ValidateAdminUserOffSitewithAppver");
        }

        return rows;
    }

    private int GetFailedValidationCount(String userid)
    {
        DataTable dt_res;
        SqlDataAdapter adpt;
        String res = "";
        int rows = 0;
        using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString()))
        {
            SqlCommand cmdUser = new SqlCommand("dbo.GetFailedValidationCount", conn);
            cmdUser.CommandType = CommandType.StoredProcedure;
            cmdUser.Parameters.AddWithValue("@USERID", Decimal.Parse(userid));
            
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                dt_res = new DataTable();
                adpt = new SqlDataAdapter(cmdUser);
                adpt.Fill(dt_res);

                rows = Convert.ToInt32(dt_res.Rows[0]["fail_count"]);
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex); 
                res = ex.Message;
                rows = 0;
            }
            conn.Close();
        }

        return rows;
    }

    private int ValidateTokenID(string username, String tokenID, string type)
    {
        String res = "";
        int intVal = 0;
        SqlConnection conn2 = null;
        using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString()))
        {
            SqlCommand cmdUser;
                       
            if (type == "USER")
            {
                cmdUser = new SqlCommand("proc_SelectTokenIDReset", conn);
                cmdUser.CommandType = CommandType.StoredProcedure;
                cmdUser.Parameters.AddWithValue("@User_ID", Convert.ToInt64(username));
            }
            else
            {
                conn2 = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());
                cmdUser = new SqlCommand("proc_SelectAdminTokenIDReset", conn2);
                cmdUser.CommandType = CommandType.StoredProcedure;
                cmdUser.Parameters.AddWithValue("@User_ID", username);
            }

            cmdUser.Parameters.AddWithValue("@TokenID", tokenID);

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                if (conn2.State == ConnectionState.Closed)
                {
                    conn2.Open();
                }
                intVal = Convert.ToInt32(cmdUser.ExecuteScalar());                
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex); 
                res = ex.Message;
            }
            finally
            {
                conn.Close();
                conn2.Close();
            }
        }
        return intVal;
    }

    public String ValidateTokenWithAmount(String uid, String tokenVal, String usertype, String purp1, String channel1, double TransactionAmount)    //UID is customers account no, tokenval is pawwsord generated from token
    {
        String result = null;
        //int result = 0;
        int retval = 1;
        String retmess = "";
        AAL2Wrap dp = null;
        String blobold = null;
        String tokendpx, tokenserial = "0";
        String validation_status = "";
        Boolean blob_change = false;
        int first_time_flag = 2; // "NOT VERIFIED"
        int reset_IND = 0, timeDrift = 0;
        DateTime token_date;
        Vasco.AAL2Wrap.TDigipassInfo info, info1;
        Vasco.AAL2Wrap.TDigipassInfoEx infox, infox1, infox2;
        int fcount = 0;
        string otpRequestID = string.Empty;
        string mobilenumber = string.Empty;

        try
        {
            xmlstring = "<Response>";

            string leadingdigit = (DateTime.Now.DayOfYear % 10).ToString();

            string tokenfirstdigit = tokenVal.Substring(0, 1);

            string otpresult = string.Empty;

            if (leadingdigit.Equals(tokenfirstdigit)) //This might be a USSD OTP validate ussd OTPFirst
            {

                DataTable otpdata = new DataTable();

                otpdata = GetOTPTokenData(uid);

                if (otpdata.Rows.Count > 0) //This is a valid USSD OTP validate and return value
                {
                    foreach (DataRow dr in otpdata.Rows)
                    {
                        string tokencode = dr["TokenCode"].ToString();
                        otpRequestID = dr["RequestID"].ToString();
                        mobilenumber = dr["MobileNumber"].ToString();
                        //  string medium = dr["Medium"].ToString();
                        string decryptedtokencode = GTBSecure.Secure.DecryptString(tokencode);

                        if (decryptedtokencode.Equals(tokenVal))
                        {
                            xmlstring = xmlstring + "<CODE>1000</CODE>";
                            validation_status = "Token Validation Successful";
                            xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                            xmlstring = xmlstring + "<TOKENTYPE>USSD</TOKENTYPE>";
                            UpdateUSSDTokenAccess(otpRequestID, "USED", channel1, TransactionAmount);
                        }
                        else
                        {

                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Validation Not Successful";
                            xmlstring = xmlstring + "<Error>Token validation not successful. Please generate a new transaction code and try again.</Error>";
                            xmlstring = xmlstring + "<TOKENTYPE>USSD</TOKENTYPE>";
                        }

                        otpresult = xmlstring;
                    }
                }
            }

            if (!string.IsNullOrEmpty(otpresult)) //USSD Validation failed return the validation result
            {
                retval = 0;
                result = "USSDOTP";
                string statusdescotp = "Retval=" + retval.ToString() + "(" + retmess + ")" + " first_time=" + first_time_flag.ToString() + " result=" + result + " Validation Result = " + validation_status;
                // xmlstring = xmlstring + "</Response>";
                UpdateTokenValidationLog(uid, mobilenumber, statusdescotp, usertype, purp1, channel1);

                otpresult = otpresult + "</Response>";
                return otpresult;

            }

            DataRow tokendpx_row = VerifyTokenSQL(uid, usertype);


            if (tokendpx_row != null)
            {
                tokendpx = tokendpx_row["VDSTOKENBLOB1"].ToString();
                tokenserial = tokendpx_row["VDSTOKENSERIAL"].ToString().Trim();
                reset_IND = Convert.ToInt32(tokendpx_row["RESET_INDICATOR"]);

                if (reset_IND == 0)
                {
                    dp = new AAL2Wrap();
                    dp.KParams.ITimeWindow = 3;

                    infox = dp.AAL2GetTokenInfoEx(tokendpx);

                    blobold = new String(tokendpx.ToCharArray());

                    if (tokendpx_row["RESET_FLAG"].ToString() == "0")  //Not reset
                    {
    
                    }
                    else
                    {
                        first_time_flag = 1;
                        String dateformat = "ddd MMM dd HH:mm:ss yyyy";

                        token_date = DateTime.ParseExact(infox.LastTimeUsed, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

                        TimeSpan tm = DateTime.Now.Subtract(token_date);
                        if (tm.Days > 48)
                        {
                            timeDrift = 1;
                        }
                        else
                        {
                            if (infox.SyncWindow == "YES")
                            {
                                dp.AAL2ResetTokenInfo(ref tokendpx);
                            }
                        }
                    }

                    if (blobold != tokendpx)
                        blob_change = true;

                    infox1 = dp.AAL2GetTokenInfoEx(tokendpx);

                    retval = dp.AAL2VerifyPassword(ref tokendpx, tokenVal.Trim(), null);

                    if (tokendpx_row["RESET_FLAG"].ToString() == "0" && retval == 0)
                    {
                        first_time_flag = 1;
                    }

                    if (blobold != tokendpx)
                        blob_change = true;

                    retmess = dp.getError(retval);
                    result = retmess;

                    infox2 = dp.AAL2GetTokenInfoEx(tokendpx);

                    result = UpdateTokenSQL(uid, tokendpx, usertype, retval, first_time_flag);

                    if (retval == 0 && first_time_flag == 0)
                    {
                        xmlstring = xmlstring + "<CODE>1001</CODE>";
                        validation_status = "Token Validation Not Successful";
                        xmlstring = xmlstring + "<Error>Token validation not successful. Please generate a new transaction code and try again.</Error>";
                        xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                    }
                    else if (retval == 0 && first_time_flag == 1 && result == "SUCCESS")
                    {

                        xmlstring = xmlstring + "<CODE>1000</CODE>";
                        validation_status = "Token Validation Successful";
                        xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                        xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                    }
                    else
                    {
                        if (retval == 201)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Repeated";
                            xmlstring = xmlstring + "<Error>You entered already used code. Please generate a new transaction code and try again.</Error>";
                            xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                        }
                        else if (retval == -202)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Too Small";
                            xmlstring = xmlstring + "<Error>You did not enter the complete code. Please generate a new transaction code and try again.</Error>";
                            xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                        }
                        else if (retval == -203)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Too Large";
                            xmlstring = xmlstring + "<Error>You entered too long code. Please generate a new transaction code and try again.</Error>";
                            xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                        }
                        else if (retval == -205)
                        {
                            xmlstring = xmlstring + "<CODE>1001</CODE>";
                            validation_status = "Token Code Not Decimal";
                            xmlstring = xmlstring + "<Error>You entered a character in the code. Please generate a new transaction code and try again.</Error>";
                            xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                        }
                        else
                        {
                            int res1;
                            result = UpdateTokenResetFlag(uid, usertype, timeDrift);
                            if (int.TryParse(result, out res1) == true)
                            {
                                if (res1 >= 5)
                                {
                                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                                    validation_status = "Token Locked.";
                                    xmlstring = xmlstring + "<Error>Your token has been locked due to several failed attempts. Please logoff and login again to reconfirm your token.</Error>";
                                    xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                                }
                                else
                                {
                                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                                    validation_status = "Invalid Transaction Code";
                                    xmlstring = xmlstring + "<Error>Invalid Transaction Code. Please generate a new transaction code and try again.</Error>";
                                    xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                                }
                            }
                            else
                            {
                                xmlstring = xmlstring + "<CODE>1001</CODE>";
                                validation_status = "Invalid Transaction Code";
                                xmlstring = xmlstring + "<Error>Invalid Transaction Code. Please generate a new transaction code and try again.</Error>";
                                xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                            }
                        }
                    }
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    validation_status = "Token Locked.";
                    xmlstring = xmlstring + "<Error>Your token has been locked due to several failed attempts. Please logoff and login again to reconfirm your token.</Error>";
                    xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                validation_status = "Token Validation Failed";
                xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
                xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
                result = "Cannot Find Token Info";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Token Infrastructure Failed ", ex); 
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            validation_status = "Token Validation Failed";
            xmlstring = xmlstring + "<Error>Token validation failed. Please generate a new transaction code and try again.</Error>";
            xmlstring = xmlstring + "<TOKENTYPE>HARDWARE</TOKENTYPE>";
            result = ex.Message.Replace("'", "");
        }

        string statusdesc = "Retval=" + retval.ToString() + "(" + retmess + ")" + " first_time=" + first_time_flag.ToString() + " result=" + result + " Validation Result = " + validation_status;
        xmlstring = xmlstring + "</Response>";

        UpdateTokenValidationLog(uid, tokenserial, statusdesc, usertype, purp1, channel1);

        return xmlstring;
    }

    private DataTable GetOTPTokenData(string userid)
    {
        String result = null;
        SqlDataReader reader;
        DataTable dt_token = new DataTable();
        SqlDataAdapter adpt;
        DataRow dr_token = null;
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["CnnStrSQLPurchases"]);
        SqlCommand comm = new SqlCommand("usp_USSDOTPRequestSelect", conn);

        try
        {
            comm.Parameters.AddWithValue("@IbankUserID", userid);
            comm.CommandType = CommandType.StoredProcedure;

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            adpt = new SqlDataAdapter(comm);
            adpt.Fill(dt_token);

            if (dt_token.Rows.Count > 0)
            {
                dr_token = dt_token.Rows[0];
            }

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 

        }

        return dt_token;
    }

    private int UpdateUSSDTokenAccess(String RequestID, string Status, string ApplicationName, double TransactionAmount)
    {
        DataTable dt_res;
        SqlDataAdapter adpt;
        String res = "";
        int rows = 0;

        using (SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["CnnStrSQLPurchases"]))
        {
            SqlCommand cmdUser;

            cmdUser = new SqlCommand("dbo.usp_USSDOTPRequestUpdate", conn);
            cmdUser.CommandType = CommandType.StoredProcedure;
            cmdUser.Parameters.AddWithValue("@RequestID", RequestID);
            cmdUser.Parameters.AddWithValue("@Status", Status);
            cmdUser.Parameters.AddWithValue("@ApplicationName", ApplicationName);
            cmdUser.Parameters.AddWithValue("@TransactionAmount", TransactionAmount);

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                rows = (int)cmdUser.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex); 
                res = ex.Message;
                rows = 0;
            }
            conn.Close();
        }

        return rows;
    }
}
