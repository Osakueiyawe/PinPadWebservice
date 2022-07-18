using System;
using System.Data;
using System.Configuration;
using System.Globalization;
using System.DirectoryServices;
using System.Data.SqlClient;
//using System.Data.OracleClient;
using Appdev.Caller;
using Appdev.Model.Enum;
using Appdev.Model.Request;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Microsoft.SqlServer.Server;

public class Eone
{
    private string xmlstring = null;

    private ReturnValue rt = new ReturnValue();

    public Eone()
    {

    }

    public string ValidateBeneficiaryAcct(string uid, string acctStr)
    {
        SqlDataReader reader;
        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());
        SqlCommand comm = new SqlCommand("SelectUserBeneficiary", conn);
        comm.Parameters.AddWithValue("@UserID", Decimal.Parse(uid));
        comm.Parameters.AddWithValue("@benacctstr", acctStr);
        comm.CommandType = CommandType.StoredProcedure;
        try
        {
            xmlstring = "<Response>";

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            reader = comm.ExecuteReader();
            if (reader.HasRows == true)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                reader.Read();
                xmlstring = xmlstring + "<BENEFICIARY>";
                xmlstring = xmlstring + "<AccountNo>" + reader["AccountString"].ToString() + "</AccountNo>";
                xmlstring = xmlstring + "<Limit>" + reader["Limit"] + "</Limit>";
                xmlstring = xmlstring + "</BENEFICIARY>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "CANNOT RETRIEVE BENEFICIARY INFORMATION" + "</Error>";
            }
            reader.Close();
            reader = null;

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
        }

        xmlstring = xmlstring + "</Response>";

        return xmlstring;
    }

    public string ValidateAdminUsr(string id, string pswd, int appid)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        DataRow dr, dr_app;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            if (bool.Parse(ConfigurationManager.AppSettings["UseAccessManager"]))
            {
                conn = new SqlConnection(ConfigurationManager.ConnectionStrings["AccessManagerConnString"].ToString());
            }
            else
            {
                conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());
            }

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("AuthenticateAdminUserPinPad", conn);
            comm.Parameters.AddWithValue("@UserID", id);
            comm.Parameters.AddWithValue("@userPassword", pswd);
            comm.Parameters.AddWithValue("@ApplicationID", appid);

            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "USER";
                ds.Tables[1].TableName = "ROLE";
                ds.Tables[2].TableName = "MENUS";
                ds.Tables[3].TableName = "ACTIONS";

                dr = ds.Tables[0].Rows[0];
                dr_app = ds.Tables[4].Rows[0];

                if (dr_app["Active"].ToString() != "1")
                {
                    xmlstring = xmlstring + "<CODE>1002</CODE>";
                    xmlstring = xmlstring + "<Error>" + "APPLICATION IS TEMPORARILY NOT AVAILABLE" + "</Error>";
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1000</CODE>";

                    //GET USER
                    xmlstring = xmlstring + "<USER>";
                    xmlstring = xmlstring + "<ID>" + dr["BASIS_ID"] + "</ID>";
                    xmlstring = xmlstring + "<NAME>" + dr["User_name"] + "</NAME>";
                    xmlstring = xmlstring + "<BRANCH>" + dr["branch_code"] + "</BRANCH>";
                    xmlstring = xmlstring + "<BRANCH_NAME>" + dr["Branch_Name"] + "</BRANCH_NAME>";
                    xmlstring = xmlstring + "<EMAIL>" + dr["email"] + "</EMAIL>";
                    //xmlstring = xmlstring + "<PWDEXPIRE>" + dr["Expiry"].ToString() + "</PWDEXPIRE>";
                    xmlstring = xmlstring + "<PWDEXPIRE>0</PWDEXPIRE>";
                    xmlstring = xmlstring + "<DOMAINID>" + dr["User_id"].ToString() + "</DOMAINID>";
                    xmlstring = xmlstring + "<TerminalID>" + dr["TerminalID"].ToString() + "</TerminalID>";
                    xmlstring = xmlstring + "<TerminalSerial>" + dr["TerminalSerial"].ToString() + "</TerminalSerial>";
                    xmlstring = xmlstring + "</USER>";

                    //GET ROLE
                    xmlstring = xmlstring + "<ROLE>";
                    dr = ds.Tables[1].Rows[0];
                    xmlstring = xmlstring + "<RID>" + dr["ROLE_ID"] + "</RID>";
                    xmlstring = xmlstring + "<RNAME>" + dr["ROLE_DESC"] + "</RNAME>";
                    xmlstring = xmlstring + "</ROLE>";

                    //GET MENUS
                    xmlstring = xmlstring + "<MENUS>";
                    foreach (DataRow dr1 in ds.Tables[2].Rows)
                    {
                        xmlstring = xmlstring + "<MENU>";
                        xmlstring = xmlstring + "<MID>" + dr1["MENU_ID"] + "</MID>";
                        xmlstring = xmlstring + "<MCATEGORY>" + dr1["MENU_CATEGORY"] + "</MCATEGORY>";
                        xmlstring = xmlstring + "<MCAPTION>" + dr1["MENU_CAPTION"] + "</MCAPTION>";
                        xmlstring = xmlstring + "<MURL>" + dr1["RESOURCE"] + "</MURL>";
                        xmlstring = xmlstring + "</MENU>";
                    }
                    xmlstring = xmlstring + "</MENUS>";

                    //GET ACTIONS
                    xmlstring = xmlstring + "<ACTIONS>";
                    foreach (DataRow dr1 in ds.Tables[3].Rows)
                    {
                        xmlstring = xmlstring + "<ACTION>";
                        xmlstring = xmlstring + "<ACTIONCODE>" + dr1["ACTION_CODE"] + "</ACTIONCODE>";
                        xmlstring = xmlstring + "<ACTIONDESC>" + dr1["ACTION_DESC"] + "</ACTIONDESC>";
                        xmlstring = xmlstring + "</ACTION>";
                    }
                    xmlstring = xmlstring + "</ACTIONS>";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "INVALID USERNAME OR PASSWORD, OR ACCESS DENIED" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + "Could not retrieve user details: " + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string ValidateAdminUsrOffSite(string id, string pswd, int appid)
    {
        string result = "";
        xmlstring = "<Response>";
        xmlstring = xmlstring + "<CODE>1005</CODE>";
        xmlstring = xmlstring + "<Error>User is running an invalid version of PINPAD</Error>";
        xmlstring = xmlstring + "</Response>";
        result = xmlstring;

        return result;
    }

    public string ValidateAdminUserOffSitewithAppver(string id, string pswd, int appid, string appversion)
    {
        string result = null;

        try
        {
            string appver = ConfigurationManager.AppSettings["AppCurrentVersion"].ToString();

            if (!appver.Equals(appversion))
            {
                xmlstring = "<Response>";
                xmlstring = xmlstring + "<CODE>1005</CODE>";
                xmlstring = xmlstring + "<Error>User is running an invalid version of PINPAD</Error>";
                xmlstring = xmlstring + "</Response>";
                result = xmlstring;

                return result;
            }

            DirectoryEntry Entry = new DirectoryEntry(string.Format("LDAP://{0}", ConfigurationManager.AppSettings["Ldap"]), id, pswd);

            DirectorySearcher Searcher = new DirectorySearcher(Entry);

            SearchResult result1;

            try
            {
                Searcher.Filter = "(anr=" + id + ")";

                //result1 = Searcher.FindOne();
                

                //if (result1 != null)
                //{
                //    result = ValidateAdminUsr(id, pswd, appid);
                //}

                result = ValidateAdminUsr(id, pswd, appid);
                //else
                //{
                //    xmlstring = "<Response>";
                //    xmlstring = xmlstring + "<CODE>1001</CODE>";
                //    xmlstring = xmlstring + "<Error>User Does Not Exist</Error>";
                //    xmlstring = xmlstring + "</Response>";
                //    result = xmlstring;
                //}
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                xmlstring = "<Response>";
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "Could not retrieve user details: " + ex.Message + "</Error>";
                xmlstring = xmlstring + "</Response>";
                result = xmlstring;
            }
        }
        catch (Exception ex)
        {
            ErrHandler.WriteError("An exception occurred in ValidateAdminUserOffSitewithAppver method. Message - " + ex.Message + "|Stacktrace - " + ex.StackTrace, "ValidateAdminUserOffSitewithAppver");
        }

        return result;
    }

    public string TransferChargesInternal(string Acct_fro, string Acct_to, string VATAcct_to, double Amount, int expl_code, string Remarks)
    {
        string t_from, t_to, vat_t_to, Req_code, ResultStr, ResultStrVAT;
        char[] charsep = { '+' };
        double T_amt;
        int Expl_code = expl_code;
        char[] delim = new char[] { '/' };
        string[] tempstr;
        bool chk_bal = false;
        string to_bra_code = string.Empty;

        try
        {
            xmlstring = "<Response>";

            //Check account format
            chk_bal = checkAccountFormat(Acct_fro.Trim());
            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to debit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            chk_bal = checkAccountFormat(Acct_to.Trim());

            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to credit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            tempstr = Acct_to.Split(delim);
            to_bra_code = tempstr[0].ToString();
            t_to = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');
            tempstr = Acct_fro.Split(delim);
            t_from = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

            tempstr = VATAcct_to.Split(delim);
            vat_t_to = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

            Amount = Math.Round(Amount, 2);

            T_amt = Amount;

            Req_code = "32";

            //Check if customer is not corporate. Only process for individual account
            int acctnat = GetCustomerAccountNature(Acct_fro);

            if (acctnat == 1)
            {
                ResultStr = PostToBasis(t_from, t_to, T_amt, expl_code, Remarks, Req_code, to_bra_code); //Post Main commission  expl_code = 429

                if (ResultStr.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
                {
                    ResultStrVAT = PostToBasis(t_from, vat_t_to, T_amt * (0.05), 157, Remarks + "-VAT", Req_code, to_bra_code); //Post VAT Charge expl_code = 157

                    if (ResultStrVAT.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
                    {
                        xmlstring = xmlstring + "<CODE>1000</CODE>"; //both are successful
                        xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                        xmlstring = xmlstring + "<VAT>SUCESS</VAT>";

                    }
                    else
                    {
                        xmlstring = xmlstring + "<CODE>1005</CODE>";  //only main charge succeeded VAT Failed
                        xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                        xmlstring = xmlstring + "<VAT>ERROR</VAT>";
                    }
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1002</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else
                {
                    //get basis return error
                    xmlstring = xmlstring + "<CODE>1004</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }

                xmlstring = xmlstring + "</Response>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>"; //customer not Individual
                xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                xmlstring = xmlstring + "<VAT>SUCESS</VAT>";
                xmlstring = xmlstring + "</Response>";
            }
        }
        catch (Exception ex)
        {
            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1004</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";

            Utility.Log().Fatal("Exception: Message: " + ex.Message + " |StackTrace: " + ex.StackTrace + " |Inner Exception : " + ex.InnerException);
        }

        return xmlstring;
    }

    private string PostToBasis(string acctFrom, string acctTo, double traAmt, int explCode, string remark, string reqCode, string origtBraCode)
    {
        string channel = ConfigurationManager.AppSettings["ChannelCode"].ToString();// "PS";
        string docAlpha = ConfigurationManager.AppSettings["DOCALPHA"];

        string uniqueRef = GenerateTransactionReference(30);

        var r = new TransferRequest
        {
            AccFrom = ConvertPaddedToOldAcct(acctFrom),
            AccTo = ConvertPaddedToOldAcct(acctTo),
            Channel = channel,
            DocAlpha = docAlpha,
            ExplCode = explCode,
            Remark = remark,
            TraAmount = Convert.ToDecimal(traAmt),
            TellerId = 9011,
            ReqCode = Convert.ToInt32(reqCode),
            UniqueTransactionReference = uniqueRef,
            OrigtBraCode = Convert.ToInt32(origtBraCode)
        };

        Entry e = new Entry { TransferRequest = r };

        string token = GetAppToken(channel);
        e.AppCode = channel;
        e.Token = token;
        e.RequestCode = ServiceCodes.TransferRequest;

        try
        {
            bool ConnectToBasisDirectly = Convert.ToBoolean(ConfigurationManager.AppSettings["ConnectToBasisDirectly"]);
            uniqueRef = uniqueRef.PadRight(30, '0');

            if (remark.Length > 170)
            {
                remark = remark.Substring(0, 169).PadRight(170, ' ');
            }
            else
                remark = remark.PadRight(170, ' ');

            remark = remark + uniqueRef;

            if (ConnectToBasisDirectly)
            {
                try
                {
                    using (OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
                    {
                        if (oraconn.State == ConnectionState.Closed)
                        {
                            oraconn.Open();
                        }

                        using (OracleTransaction transaction = oraconn.BeginTransaction(IsolationLevel.ReadCommitted))
                        {
                            try
                            {
                                using (OracleCommand oracomm = oraconn.CreateCommand())
                                using (OracleCommand blockCommand = oraconn.CreateCommand())
                                {
                                    oracomm.CommandType = CommandType.StoredProcedure;
                                    oracomm.CommandTimeout = 60;

                                    oracomm.Transaction = transaction;

                                    //oracomm.CommandText = "EONEPKG1.GTBPBSC0_FULL";
                                    oracomm.CommandText = "EONEPKG_NIP.GTBPBSC0_FULL";
                                    oracomm.Parameters.Add("INP_ACCT_FROM", OracleDbType.Varchar2, 21).Value = acctFrom.Trim();
                                    oracomm.Parameters.Add("INP_ACCT_TO", OracleDbType.Varchar2, 21).Value = acctTo.Trim();
                                    oracomm.Parameters.Add("INP_TRA_AMT", OracleDbType.Double, 20).Value = traAmt;
                                    oracomm.Parameters.Add("INP_EXPL_CODE", OracleDbType.Int32, 15).Value = explCode;
                                    oracomm.Parameters.Add("INP_REMARKS", OracleDbType.Varchar2, 200).Value = remark.Replace("'", "");
                                    oracomm.Parameters.Add("inp_rqst_code", OracleDbType.Varchar2, 15).Value = reqCode;
                                    oracomm.Parameters.Add("INP_MAN_APP1", OracleDbType.Int32, 15).Value = 0;
                                    oracomm.Parameters.Add("inp_tell_id", OracleDbType.Int32, 15).Value = 9011;
                                    oracomm.Parameters.Add("INP_DOC_ALP", OracleDbType.Varchar2, 200).Value = docAlpha;
                                    oracomm.Parameters.Add("out_tra_seq1", OracleDbType.Int32, 15).Direction = ParameterDirection.Output;
                                    oracomm.Parameters.Add("inp_tra_seq1", OracleDbType.Int32, 15).Direction = ParameterDirection.Output;
                                    oracomm.Parameters.Add("INP_ORIGT_BRA_CODE", OracleDbType.Int32, 15).Value = origtBraCode;
                                    oracomm.Parameters.Add("out_return_status", OracleDbType.Varchar2, 100).Direction = ParameterDirection.Output;

                                    oracomm.ExecuteNonQuery();

                                    string returnStatus = oracomm.Parameters["OUT_RETURN_STATUS"].Value.ToString();
                                    Utility.Log().Fatal(uniqueRef + " - Basis Response - " + returnStatus + ",AcctFrom-"+ acctFrom + ",AcctTo-"+ acctTo+",Amount-"+ traAmt);

                                    if (returnStatus.Equals("@ERR7@"))
                                    {
                                        transaction.Commit();
                                        //returnStatus = "@ERR7@";
                                    }
                                    else
                                        transaction.Rollback();

                                   // else
                                    return returnStatus;
                                }
                            }
                            catch (Exception ex)
                            {
                                Utility.Log().Fatal("Error in Basis Posting! Response from Basis Posting : Message: " + ex.Message + "|StackTrace: " + ex.StackTrace);
                                transaction.Rollback();
                                return "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utility.Log().Fatal("Error in Basis Posting! Response from Basis Posting : Message: " + ex.Message + "|StackTrace: " + ex.StackTrace);
                    return "";
                }
                //return "@ERR7@";
            }
            else
            {
                Utility.Log().Fatal("Request " + Newtonsoft.Json.JsonConvert.SerializeObject(r));

                e.Process();

                Utility.Log().Fatal("Response " + Newtonsoft.Json.JsonConvert.SerializeObject(e.Response));

                if (e.Response.Code == ResponseCodes.Success.Item1)
                {
                    return "@ERR7@";
                }
                return e.Response.OtherMessage ?? e.Response.Code.ToString();
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Error Occured While Posting to Basis", ex);
            return "";
        }
    }

    public string ValidateUser(string uid, string pswd)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("IVR_USER_ACCOUNTS", conn);
            comm.Parameters.AddWithValue("@u_id", uid);
            comm.Parameters.AddWithValue("@u_pwd", long.Parse(pswd));

            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();
            adpt.Fill(ds);

            if (ds.Tables[0].Rows.Count > 0)
            {
                DataRow drt = (DataRow)ds.Tables[0].Rows[0];
                if (drt["pwd"].ToString() == "1")
                {
                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "<USERID>" + drt["user_id"].ToString() + "</USERID>";
                    xmlstring = xmlstring + "<ACCOUNTS>";
                    foreach (DataRow dr1 in ds.Tables[1].Rows)
                    {
                        xmlstring = xmlstring + "<ACCOUNT>" + dr1[0].ToString() + "</ACCOUNT>";
                    }
                    xmlstring = xmlstring + "</ACCOUNTS>";
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<Error>" + "INVALID USERNAME OR PASSWORD" + "</Error>";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "INVALID USERNAME OR PASSWORD" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
        //ret.value = "1000";
        //ret.message = xmlstring;
        //return ret;
    }

    public string GetCustomerDetails(string uid)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        Account[] accts;
        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("IVR_USER_DETAILS2", conn);
            comm.Parameters.AddWithValue("@u_id", uid);

            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();
            adpt.Fill(ds);

            if (ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr = (DataRow)ds.Tables[0].Rows[0];

                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<DETAILS>";
                xmlstring = xmlstring + "<EMAIL>" + dr["EMAIL"].ToString() + "</EMAIL>";
                xmlstring = xmlstring + "<PHONE>" + dr["PHONE"].ToString() + "</PHONE>";
                xmlstring = xmlstring + "<PARTYID>" + dr["PARTYID"].ToString() + "</PARTYID>";
                xmlstring = xmlstring + "</DETAILS>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "INVALID USERNAME OR PASSWORD" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
        //ret.value = "1000";
        //ret.message = xmlstring;
        //return ret;
    }

    public string ResetUserPassword(string uid)
    {
        ReturnValue ret = null;
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataReader dr;
        SqlDataAdapter adpt;
        Account[] accts;

        string sentemail;
        Email email = new Email();
        string sub_mess;
        string main_mess;
        string selectqry = null;
        string emailaddress = "none@gtbank.com";

        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            if (uid.Length == 9)
                uid = uid + "01";

            selectqry = "select user_id,email from users where user_id=" + uid;
            comm = new SqlCommand(selectqry, conn);
            dr = comm.ExecuteReader();
            if (dr.HasRows)
            {
                dr.Read();
                if (!dr.IsDBNull(1))
                {
                    emailaddress = dr.GetString(1);
                    dr.Close();
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<Error>EMAIL ADDRESS IS NOT VALID</Error>";
                    xmlstring = xmlstring + "</Response>";
                    dr.Close();
                    return xmlstring;
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>UNABLE TO RETRIEVE USER EMAIL ADDRESS</Error>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }
            dr = null;
            //get a new password
            Utility ut = new Utility();
            string password = null;
            password = ut.GeneratePassword();

            comm = new SqlCommand("updatePassCode", conn);
            comm.Parameters.AddWithValue("@u_id", long.Parse(uid));
            comm.Parameters.AddWithValue("@p_code", long.Parse(password));
            comm.Parameters.AddWithValue("@p_type", Int32.Parse("0"));

            comm.CommandType = CommandType.StoredProcedure;
            count = comm.ExecuteNonQuery();
            if (count > 0)
            {
                sub_mess = "GTBank IVR - Your new passcode";
                main_mess = "\n" + password + "\n";

                try
                {
                    sentemail = email.SendEmail(emailaddress, sub_mess, main_mess, "");
                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                }
                catch (Exception ex)
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "UNABLE TO UPDATE PASSWORD" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string ChangeUserPassword(string uid, string password)
    {
        SqlConnection conn;
        SqlCommand comm;
        Email email = new Email();

        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("updatePassCode", conn);
            comm.Parameters.AddWithValue("@u_id", long.Parse(uid));
            comm.Parameters.AddWithValue("@p_code", long.Parse(password));
            comm.Parameters.AddWithValue("@p_type", int.Parse("0"));

            comm.CommandType = CommandType.StoredProcedure;
            count = comm.ExecuteNonQuery();
            if (count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>1000</MESSAGE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "UNABLE TO UPDATE PASSWORD" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string TransferFunds(string Acct_fro, string Acct_to, double Amount, string type, string channel, string Remarks)
    {
        string t_from, t_to, Req_code, ResultStr;
        char[] charsep = { '+' };
        double T_amt;
        int Expl_code = 102;
        char[] delim = new char[] { '/' };
        string[] tempstr;
        bool chk_bal = false;
        string Remarks1 = null;

        try
        {
            xmlstring = "<Response>";

            //Check account format
            chk_bal = checkAccountFormat(Acct_fro.Trim());
            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to debit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            chk_bal = checkAccountFormat(Acct_to.Trim());
            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to credit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            tempstr = Acct_to.Split(delim);
            t_to = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');
            tempstr = Acct_fro.Split(delim);
            t_from = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

            Amount = Math.Round(Amount, 2);

            T_amt = Amount;

            Req_code = "32";

            if (type == "OWN")
            {
                Expl_code = 100;
                Remarks1 = channel + " - OWN Account Transfer from " + Acct_fro + " to " + Acct_to;
            }
            else if (type == "PRE")
            {
                Expl_code = 102;
                Remarks1 = channel + " - PRE-REGISTERED Account Transfer from " + Acct_fro + " to " + Acct_to;
            }
            else if (type == "ANY")
            {
                Expl_code = 102;
                Remarks1 = channel + " - ANY Account Transfer from " + Acct_fro + " to " + Acct_to;
            }
            else if (type == "OTH")
            {
                Expl_code = 102;
                Remarks1 = Remarks;
            }
            else if (type == "OTHNOCHARGE")
            {
                Expl_code = 302;
                Remarks1 = Remarks;
            }
            else if (type == "DEPOSIT") // Cash Deposit
            {
                Expl_code = 1;
                Remarks1 = Remarks;
            }
            else if (type == "WITHDRAWAL") // Cash Withdrawal
            {
                Expl_code = 2;
                Remarks1 = Remarks;
            }

            ResultStr = this.PostToBasis(t_from, t_to, T_amt, Expl_code, Remarks1, Req_code);

            if (ResultStr.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
            {
                //log output
                if (type == "OWN" || type == "PRE" || type == "ANY")
                {
                    LogTransfer(Remarks, Acct_fro, Acct_to, Convert.ToDecimal(T_amt), channel, Remarks1);
                }
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
            }
            else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }
            else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
            {
                xmlstring = xmlstring + "<CODE>1002</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }
            else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }
            else
            {
                //get basis return error
                xmlstring = xmlstring + "<CODE>1004</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }

            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1004</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string TransferTraSeq(string Acct_fro, string Acct_to, double Amount, int expl_code, string Remarks)
    {
        string t_from, t_to, Req_code, ResultStr, ResultStr2, TraSeq;
        char[] charsep = { '+' };
        double T_amt;
        int Expl_code = expl_code;
        char[] delim = new char[] { '/' };
        string[] tempstr;
        bool chk_bal = false;
        string[] resultset = null;

        try
        {
            xmlstring = "<Response>";

            //Check account format
            chk_bal = checkAccountFormat(Acct_fro.Trim());
            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to debit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            chk_bal = checkAccountFormat(Acct_to.Trim());
            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to credit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            tempstr = Acct_to.Split(delim);
            t_to = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');
            tempstr = Acct_fro.Split(delim);
            t_from = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

            Amount = Math.Round(Amount, 2);

            T_amt = Amount;

            Req_code = "32";

            ResultStr2 = this.PostToBasisTraSeq(t_from, t_to, T_amt, expl_code, Remarks, Req_code);
            resultset = ResultStr2.Split(',');
            ResultStr = resultset[0].ToString();
            TraSeq = resultset[1].ToString();

            if (ResultStr.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                xmlstring = xmlstring + "<TRASEQ>" + TraSeq + "</TRASEQ>";
            }
            else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }
            else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
            {
                xmlstring = xmlstring + "<CODE>1002</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }
            else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1004</CODE>";
                xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
            }

            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1004</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string Transfer(string Acct_fro, string Acct_to, double Amount, int expl_code, string Remarks, string TellerRole, string transtype, string serviceId) //1 is teller 2 is 
    {
        if (isServiceActive(serviceId))
        {
            try
            {
                xmlstring = "<Response>";

                string teller_origt_bra_code = "";
                string Opsheadroleid = ConfigurationManager.AppSettings["OpsHeadRoleId"];
                string TellerRoleId = ConfigurationManager.AppSettings["TellerRoleId"];
                string ITRoleId = ConfigurationManager.AppSettings["DepositITRoleId"];
                double TellerWithdrawalLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadNairaWithdrawalAuthLimit"]);
                double TellerDepositLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadNairaDepositAuthLimit"]);
                double PinPadAppDepositLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadDepositLimit"]);
                double PinPadAppWithdrawalLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadWithdrawalLimit"]);

                double WithdrawalChargeLimit = Convert.ToDouble(ConfigurationManager.AppSettings["WithdrawalLimit"]);

                double Cummulative = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadCummulativeWithdrawal"]);
                string withdrawal_expl_code = ConfigurationManager.AppSettings["WithdrwalExpl_Code"];
                string deposit_expl_code = ConfigurationManager.AppSettings["DepositExpl_Code"];
                double cashlesswithdrawallimit = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessWithdrawalLimit"]);
                double cashlessdepositlimit = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessDepositLimit"]);
                double cashlesswithdrawalrate = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessWithdrawalRate"]);
                double cashlessdepositrate = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessDepositRate"]);
                double depositcumulativeamt = 0;
                double withdrawalcumulativeamt = 0;
                double cashlessdepositcumulativeamt = 0;
                double cashlesswithdrawalcumulativeamt = 0;

                if (TellerRole.Equals(Opsheadroleid))
                {

                }

                if (transtype.ToUpper().Equals("DEPOSIT"))
                {// Perform Application Deposit Check
                    string[] acctdetails = null;
                    string[] cusdetails = null;
                    depositcumulativeamt = cummulativeamount(Acct_to, deposit_expl_code);
                    cashlessdepositcumulativeamt = cashlesscummulativeamount(Acct_to, deposit_expl_code);
                    acctdetails = Acct_fro.Split('/');
                    string bra_code = acctdetails[0];
                    teller_origt_bra_code = bra_code;
                    cusdetails = Acct_to.Split('/');
                    string led_code = cusdetails[3].ToString();

                    //if (!led_code.Equals("59")) // Savings account has no limit
                    //{

                        if (Amount > PinPadAppDepositLimit) //check application deposit limit.
                        {
                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Transaction failed: Amount Exceeds Allowed Deposit Amount For PinPad...</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;
                        }
                        //deposit cummulative check...
                        if (depositcumulativeamt + Amount > Cummulative)
                        {

                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Customer has exceeded the maximum cummulative amount on PinPad Deposit!.</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;
                        }
                   // }

                    //check teller authorization
                    if (TellerRole.Equals(TellerRoleId) & (Amount + depositcumulativeamt > TellerDepositLimit)) //Above Teller Authorization
                    {

                        xmlstring = xmlstring + "<CODE>1010</CODE>";
                        xmlstring = xmlstring + "<MESSAGE>Transaction Pending: Transaction Awaiting Ops Head Approval...</MESSAGE>";
                        xmlstring = xmlstring + "</Response>";
                        return xmlstring;
                    }
                }
                else if (transtype.ToUpper().Equals("WITHDRAWAL"))
                {
                    string[] acctdetails = null;
                    acctdetails = Acct_to.Split('/');
                    string bra_code = acctdetails[0];
                    string[] cusdetails = null;
                    teller_origt_bra_code = bra_code;
                    cusdetails = Acct_fro.Split('/');
                    string led_code = cusdetails[3].ToString();
                    withdrawalcumulativeamt = cummulativeamount(Acct_fro, withdrawal_expl_code);
                    cashlesswithdrawalcumulativeamt = cashlesscummulativeamount(Acct_fro, withdrawal_expl_code);

                   // if (!led_code.Equals("59")) // Savings account has no limit
                   // {
                        if (Amount > PinPadAppWithdrawalLimit) //check application deposit limit.
                        {
                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Transaction failed: Amount Exceeds Allowed Withdrawal Amount For PinPad...</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;
                        }

                        if (withdrawalcumulativeamt + Amount > Cummulative)
                        {
                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Customer has exceeded the maximum cummulative amount on PinPad withdrawal!.</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;
                        }
                   // }

                    //overide for cummulative
                    if ((TellerRole.Equals(TellerRoleId)) & (withdrawalcumulativeamt + Amount > TellerWithdrawalLimit) & (withdrawalcumulativeamt < Cummulative))
                    {
                        xmlstring = xmlstring + "<CODE>1010</CODE>";
                        xmlstring = xmlstring + "<MESSAGE>Transaction Pending: Transaction Awaiting Ops Head Approval...</MESSAGE>";
                        xmlstring = xmlstring + "</Response>";
                        return xmlstring;
                    }

                    if (TellerRole.Equals(TellerRoleId) & (Amount > TellerWithdrawalLimit)) //Above Teller Authorization
                    {

                        xmlstring = xmlstring + "<CODE>1010</CODE>";
                        xmlstring = xmlstring + "<MESSAGE>Transaction Pending: Transaction Awaiting Ops Head Approval...</MESSAGE>";
                        xmlstring = xmlstring + "</Response>";
                        return xmlstring;
                    }

                    if (!led_code.Equals("26")) //Ledger 26 (Seniors Account should not be charged for Banking hall withdrawal
                    {
                        if (Amount < WithdrawalChargeLimit)
                        {
                            double WithdrawalCharge = Convert.ToDouble(ConfigurationManager.AppSettings["WithdrawalCharges"]);

                            double Availbal = GetAvailBalance(Acct_fro);

                            if (Availbal < (Amount + (WithdrawalCharge + ((0.05) * WithdrawalCharge))))
                            {
                                xmlstring = xmlstring + "<CODE>1050</CODE>";
                                xmlstring = xmlstring + "<ERROR>Customer Account is NOT funded for Principal And Charges.</ERROR>";
                                xmlstring = xmlstring + "</Response>";
                                return xmlstring;
                            }
                            else
                            {
                                string chargeaccount = ConfigurationManager.AppSettings["WithdrawalChargesAccount"];
                                string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
                                string ChargeAcct_to = bra_code + chargeaccount;
                                string VATAcct_to = bra_code + VATAccount;
                                TransferChargesInternal(Acct_fro, ChargeAcct_to, VATAcct_to, WithdrawalCharge, 429, "PinPad Withdrawal Charge : " + Acct_fro);
                                xmlstring = "<Response>"; //reinitialise xmlstring
                            }
                        }
                    }

                    //cashless lagos charges (withdrawal) 

                    if ((withdrawalcumulativeamt + Amount) > cashlesswithdrawallimit)
                    {
                        double ChargeAmount = 0;
                        double Availbal = 0;

                        teller_origt_bra_code = bra_code;
                        // if (bra_code.StartsWith("2") && (!cusdetails[0].ToString().StartsWith("2")))// the account to debit(tellers till is a lagos account take the cashless charges)
                        if (qualifiesForCashLess(bra_code, cusdetails[0].ToString()))
                        {
                            if (withdrawalcumulativeamt > cashlessdepositlimit) //the cummlative amount is already over hence the customer must have been charged before on the excess
                            {

                                ChargeAmount = Amount; //just charge on the current amount

                                Availbal = GetAvailBalance(Acct_fro);
                            }
                            else //the cummlative amount is now over hence the customer has not been charged before calculate the excess to charge
                            {

                                ChargeAmount = (withdrawalcumulativeamt + Amount) - cashlesswithdrawallimit;

                                Availbal = GetAvailBalance(Acct_fro);
                            }
                            if (Availbal < (Amount + (cashlesswithdrawalrate * ChargeAmount)))
                            {
                                xmlstring = xmlstring + "<CODE>1060</CODE>";
                                xmlstring = xmlstring + "<ERROR>Customer Account is NOT funded for Principal And Cashless withdrawal limit Charges.</ERROR>";
                                xmlstring = xmlstring + "</Response>";
                                return xmlstring;
                            }

                        }
                    }
                }

                string t_from, t_to, Req_code, ResultStr;
                char[] charsep = { '+' };
                double T_amt;
                char[] delim = new char[] { '/' };
                string[] tempstr_fro, tempstr_to;
                bool chk_bal = false;

                chk_bal = checkAccountFormat(Acct_fro.Trim());
                if (chk_bal == false)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to debit is wrong or account does not exist in BASIS.</ERROR>";
                    xmlstring = xmlstring + "</Response>";
                    return xmlstring;
                }

                chk_bal = checkAccountFormat(Acct_to.Trim());

                if (chk_bal == false)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to credit is wrong or account does not exist in BASIS.</ERROR>";
                    xmlstring = xmlstring + "</Response>";
                    return xmlstring;
                }

                tempstr_to = Acct_to.Split(delim);
                t_to = tempstr_to[0].PadLeft(4, '0') + tempstr_to[1].PadLeft(7, '0') + tempstr_to[2].PadLeft(3, '0') + tempstr_to[3].PadLeft(4, '0') + tempstr_to[4].PadLeft(3, '0');
                tempstr_fro = Acct_fro.Split(delim);
                t_from = tempstr_fro[0].PadLeft(4, '0') + tempstr_fro[1].PadLeft(7, '0') + tempstr_fro[2].PadLeft(3, '0') + tempstr_fro[3].PadLeft(4, '0') + tempstr_fro[4].PadLeft(3, '0');

                Amount = Math.Round(Amount, 2);

                T_amt = Amount;

                Req_code = "32";

                ResultStr = PostToBasis(t_from, t_to, T_amt, expl_code, Remarks, Req_code, teller_origt_bra_code);
                Utility.Log().Info(string.Format("{0} - Basis Posting for {1},{2},{3},{4}", ResultStr, Acct_to, Acct_fro, Amount, Remarks));


                if (ResultStr.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
                {
                    if (transtype.ToUpper().Equals("WITHDRAWAL"))
                    {

                        double ChargeAmount = 0;
                        if (cashlesswithdrawalcumulativeamt > cashlesswithdrawallimit)
                        {
                            ChargeAmount = Amount;
                        }
                        else
                        {
                            ChargeAmount = (cashlesswithdrawalcumulativeamt + Amount) - cashlesswithdrawallimit;
                        }
                        if (ChargeAmount > 0)
                        {
                            string[] acctdetails = null;
                            acctdetails = Acct_to.Split('/');
                            string bra_code = acctdetails[0];
                            string[] cusdetails = null;
                            teller_origt_bra_code = bra_code;
                            cusdetails = Acct_fro.Split('/');
                            //  if (bra_code.StartsWith("2") && (!cusdetails[0].ToString().StartsWith("2")))// the account to debit(tellers till is a lagos account take the cashless charges)
                            if (qualifiesForCashLess(bra_code, cusdetails[0].ToString()))
                            // the account to credit(tellers till is a lagos account take the cashless charges)
                            {
                                string cashlesschargeaccount = ConfigurationManager.AppSettings["CashlessChargeAccount"];
                                string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
                                string cashlessChargeAcct_to = bra_code + cashlesschargeaccount;
                                string VATAcct_to = bra_code + VATAccount;
                                string response = TransferChargesInternal(Acct_fro, cashlessChargeAcct_to, VATAcct_to, cashlesswithdrawalrate * ChargeAmount, 3020, "PinPad Cashless Withdrawal Charge : " + Acct_fro);
                                Utility.Log().Info(string.Format("Posting For {0}", Acct_fro));
                            }     
                        }
                    }
                    else if (transtype.ToUpper().Equals("DEPOSIT"))
                    {
                        double ChargeAmount = 0;
                        if (cashlessdepositcumulativeamt > cashlessdepositlimit) // the cummulative is already greater than the limit the customer must have been charged before; so charge only the current amount
                        {
                            ChargeAmount = Amount;
                        }
                        else
                        {
                            ChargeAmount = (cashlessdepositcumulativeamt + Amount) - cashlessdepositlimit; //the total cummulative + amt is now over calculate the difference and charge.
                        }
                        if (ChargeAmount > 0)
                        {
                            string[] acctdetails = null;
                            string[] cusdetails = null;
                            acctdetails = Acct_fro.Split('/');
                            string bra_code = acctdetails[0];
                            teller_origt_bra_code = bra_code;
                            cusdetails = Acct_to.Split('/');

                            // if (bra_code.StartsWith("2") && (!cusdetails[0].ToString().StartsWith("2")))// the account to debit(tellers till is a lagos account take the cashless charges)
                            if (qualifiesForCashLess(bra_code, cusdetails[0].ToString()))
                            // the account to credit(tellers till is a lagos account take the cashless charges)
                            {
                                string cashlesschargeaccount = ConfigurationManager.AppSettings["CashlessChargeAccount"];
                                string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
                                string cashlessChargeAcct_to = bra_code + cashlesschargeaccount;
                                string VATAcct_to = bra_code + VATAccount;
                                TransferChargesInternal(Acct_to, cashlessChargeAcct_to, VATAcct_to, cashlessdepositrate * ChargeAmount, 3020, "PinPad Cashless Deposit Charge : " + Acct_to);
                            }
                        }
                    }

                    xmlstring = "";
                    xmlstring = xmlstring + "<Response>";
                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";

                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1002</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1004</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }

                xmlstring = xmlstring + "</Response>";
            }
            catch (Exception ex)
            {
                xmlstring = "<Response>";
                xmlstring = xmlstring + "<CODE>1004</CODE>";
                xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
                xmlstring = xmlstring + "</Response>";
                Utility.Log().Fatal("Exception: Message: " + ex.Message + "|StackTrace: " + ex.StackTrace + "|Inner Exception : " + ex.InnerException);
            }

            return xmlstring;
        }
        else
        {
            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1003</CODE>";
            xmlstring = xmlstring + "<ERROR>Transaction failed: The Service is currently not available on PinPad. Please process on BASIS.</ERROR>";
            xmlstring = xmlstring + "</Response>";
            return xmlstring;

        }
    }

    public string Transfer(string Acct_fro, string Acct_to, double Amount, int expl_code, string Remarks, string TellerRole, string transtype, string serviceId, string tellerid) //1 is teller 2 is 
    {
        //There should be three levels of check
        //Application Limit   (Amount Must Not exceed set limit for deposit and withdrawal
        //Teller Limit
        //customer cummulative limit

        if (isServiceActive(serviceId))
        {

            try
            {
                xmlstring = "<Response>";

                string teller_origt_bra_code = "";
                string Opsheadroleid = ConfigurationManager.AppSettings["OpsHeadRoleId"];
                string TellerRoleId = ConfigurationManager.AppSettings["TellerRoleId"];
                string ITRoleId = ConfigurationManager.AppSettings["DepositITRoleId"];
                double TellerWithdrawalLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadNairaWithdrawalAuthLimit"]);
                double TellerDepositLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadNairaDepositAuthLimit"]);
                double PinPadAppDepositLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadDepositLimit"]);
                double PinPadAppWithdrawalLimit = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadWithdrawalLimit"]);

                double WithdrawalChargeLimit = Convert.ToDouble(ConfigurationManager.AppSettings["WithdrawalLimit"]);

                double Cummulative = Convert.ToDouble(ConfigurationManager.AppSettings["PinPadCummulativeWithdrawal"]);
                string withdrawal_expl_code = ConfigurationManager.AppSettings["WithdrwalExpl_Code"];
                string deposit_expl_code = ConfigurationManager.AppSettings["DepositExpl_Code"];
                double cashlesswithdrawallimit = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessWithdrawalLimit"]);
                double cashlessdepositlimit = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessDepositLimit"]);
                double cashlesswithdrawalrate = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessWithdrawalRate"]);
                double cashlessdepositrate = Convert.ToDouble(ConfigurationManager.AppSettings["CashlessDepositRate"]);
                double depositcumulativeamt = 0;
                double withdrawalcumulativeamt = 0;
                double cashlessdepositcumulativeamt = 0;
                double cashlesswithdrawalcumulativeamt = 0;

                if (TellerRole.Equals(Opsheadroleid))
                {

                }

                if (transtype.ToUpper().Equals("DEPOSIT"))
                {// Perform Application Deposit Check
                    string[] acctdetails = null;
                    string[] cusdetails = null;
                    depositcumulativeamt = cummulativeamount(Acct_to, deposit_expl_code);
                    cashlessdepositcumulativeamt = cashlesscummulativeamount(Acct_to, deposit_expl_code);
                    acctdetails = Acct_fro.Split('/');
                    string bra_code = acctdetails[0];
                    teller_origt_bra_code = bra_code;
                    cusdetails = Acct_to.Split('/');
                    string led_code = cusdetails[3].ToString();

                    if (!led_code.Equals("59")) // Savings account has no limit
                    {

                        if (Amount > PinPadAppDepositLimit) //check application deposit limit.
                        {
                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Transaction failed: Amount Exceeds Allowed Deposit Amount For PinPad...</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;
                        }
                        //deposit cummulative check...
                        if (depositcumulativeamt + Amount > Cummulative)
                        {

                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Customer has exceeded the maximum cummulative amount on PinPad Deposit!.</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;
                        }
                    }

                    //check teller authorization
                    if (TellerRole.Equals(TellerRoleId) & (Amount + depositcumulativeamt > TellerDepositLimit)) //Above Teller Authorization
                    {

                        xmlstring = xmlstring + "<CODE>1010</CODE>";
                        xmlstring = xmlstring + "<MESSAGE>Transaction Pending: Transaction Awaiting Ops Head Approval...</MESSAGE>";
                        xmlstring = xmlstring + "</Response>";
                        return xmlstring;
                    }
                }
                else if (transtype.ToUpper().Equals("WITHDRAWAL"))
                {
                    string[] acctdetails = null;
                    acctdetails = Acct_to.Split('/');
                    string bra_code = acctdetails[0];
                    string[] cusdetails = null;
                    teller_origt_bra_code = bra_code;
                    cusdetails = Acct_fro.Split('/');
                    string led_code = cusdetails[3].ToString();
                    withdrawalcumulativeamt = cummulativeamount(Acct_fro, withdrawal_expl_code);
                    cashlesswithdrawalcumulativeamt = cashlesscummulativeamount(Acct_fro, withdrawal_expl_code);

                    if (!led_code.Equals("59")) // Savings account has no limit
                    {
                        if (Amount > PinPadAppWithdrawalLimit) //check application deposit limit.
                        {
                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Transaction failed: Amount Exceeds Allowed Withdrawal Amount For PinPad...</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;


                        }

                        //withdrawal cummulative check...
                        if (withdrawalcumulativeamt + Amount > Cummulative)
                        {

                            xmlstring = xmlstring + "<CODE>1003</CODE>";
                            xmlstring = xmlstring + "<ERROR>Customer has exceeded the maximum cummulative amount on PinPad withdrawal!.</ERROR>";
                            xmlstring = xmlstring + "</Response>";
                            return xmlstring;

                        }
                    }

                    //overide for cummulative
                    if ((TellerRole.Equals(TellerRoleId)) & (withdrawalcumulativeamt + Amount > TellerWithdrawalLimit) & (withdrawalcumulativeamt < Cummulative))
                    {

                        xmlstring = xmlstring + "<CODE>1010</CODE>";
                        xmlstring = xmlstring + "<MESSAGE>Transaction Pending: Transaction Awaiting Ops Head Approval...</MESSAGE>";
                        xmlstring = xmlstring + "</Response>";
                        return xmlstring;
                    }

                    //if (led_code.Equals("13"))//GTCreate is not allowed for withdrawal transactions.
                    //{

                    //    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    //    xmlstring = xmlstring + "<ERROR>This Ledger is NOT allowed to perform withdrawal in the Banking HALL</ERROR>";
                    //    xmlstring = xmlstring + "</Response>";
                    //    return xmlstring;
                    //}


                    if (TellerRole.Equals(TellerRoleId) & (Amount > TellerWithdrawalLimit)) //Above Teller Authorization
                    {

                        xmlstring = xmlstring + "<CODE>1010</CODE>";
                        xmlstring = xmlstring + "<MESSAGE>Transaction Pending: Transaction Awaiting Ops Head Approval...</MESSAGE>";
                        xmlstring = xmlstring + "</Response>";
                        return xmlstring;
                    }


                    if (!led_code.Equals("26")) //Ledger 26 (Seniors Account should not be charged for Banking hall withdrawal
                    {

                        if (Amount < WithdrawalChargeLimit)
                        {

                            double WithdrawalCharge = Convert.ToDouble(ConfigurationManager.AppSettings["WithdrawalCharges"]);

                            double Availbal = GetAvailBalance(Acct_fro);
                            if (Availbal < (Amount + (WithdrawalCharge + ((0.05) * WithdrawalCharge))))
                            {


                                xmlstring = xmlstring + "<CODE>1050</CODE>";
                                xmlstring = xmlstring + "<ERROR>Customer Account is NOT funded for Principal And Charges.</ERROR>";
                                xmlstring = xmlstring + "</Response>";
                                return xmlstring;



                            }
                            else
                            {
                                string chargeaccount = ConfigurationManager.AppSettings["WithdrawalChargesAccount"];
                                string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
                                string ChargeAcct_to = bra_code + chargeaccount;
                                string VATAcct_to = bra_code + VATAccount;
                                TransferChargesInternal(Acct_fro, ChargeAcct_to, VATAcct_to, WithdrawalCharge, 429, "PinPad Withdrawal Charge : " + Acct_fro, tellerid);
                                xmlstring = "<Response>"; //reinitialise xmlstring
                            }
                        }
                    }

                    //cashless lagos charges (withdrawal) 

                    if ((withdrawalcumulativeamt + Amount) > cashlesswithdrawallimit)
                    {
                        double ChargeAmount = 0;
                        double Availbal = 0;
                        //string[] acctdetails = null;
                        // acctdetails = acctTo.Split('/');
                        //   string bra_code = acctdetails[0];
                        teller_origt_bra_code = bra_code;
                        // if (bra_code.StartsWith("2") && (!cusdetails[0].ToString().StartsWith("2")))// the account to debit(tellers till is a lagos account take the cashless charges)
                        if (qualifiesForCashLess(bra_code, cusdetails[0].ToString()))
                        // the account to credit(tellers till is a lagos account take the cashless charges)
                        // the account to credit(tellers till is a lagos account take the cashless charges)
                        {
                            if (withdrawalcumulativeamt > cashlessdepositlimit) //the cummlative amount is already over hence the customer must have been charged before on the excess
                            {

                                ChargeAmount = Amount; //just charge on the current amount

                                Availbal = GetAvailBalance(Acct_fro);
                            }
                            else //the cummlative amount is now over hence the customer has not been charged before calculate the excess to charge
                            {

                                ChargeAmount = (withdrawalcumulativeamt + Amount) - cashlesswithdrawallimit;

                                Availbal = GetAvailBalance(Acct_fro);
                            }
                            if (Availbal < (Amount + (cashlesswithdrawalrate * ChargeAmount)))
                            {
                                xmlstring = xmlstring + "<CODE>1060</CODE>";
                                xmlstring = xmlstring + "<ERROR>Customer Account is NOT funded for Principal And Cashless withdrawal limit Charges.</ERROR>";
                                xmlstring = xmlstring + "</Response>";
                                return xmlstring;
                            }
                            //else // Take charges
                            //{
                            //    string cashlesschargeaccount = ConfigurationManager.AppSettings["CashlessChargeAccount"];
                            //    string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
                            //    string cashlessChargeAcct_to = bra_code + cashlesschargeaccount;
                            //    string VATAcct_to = bra_code + VATAccount;
                            //    TransferChargesInternal(Acct_fro, cashlessChargeAcct_to, VATAcct_to, cashlesswithdrawalrate * ChargeAmount, 3020, "PinPad Cashless Withdrawal Charge : " + Acct_fro);
                            //    //Account to debit must be the customer's account (for withdrawal that is the account to debit)

                            //}
                        }
                    }

                }

                string t_from, t_to, Req_code, ResultStr;
                char[] charsep = { '+' };
                double T_amt;
                char[] delim = new char[] { '/' };
                string[] tempstr_fro, tempstr_to;
                bool chk_bal = false;


                //Check account format
                chk_bal = checkAccountFormat(Acct_fro.Trim());
                if (chk_bal == false)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to debit is wrong or account does not exist in BASIS.</ERROR>";
                    xmlstring = xmlstring + "</Response>";
                    return xmlstring;
                }

                chk_bal = checkAccountFormat(Acct_to.Trim());
                if (chk_bal == false)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to credit is wrong or account does not exist in BASIS.</ERROR>";
                    xmlstring = xmlstring + "</Response>";
                    return xmlstring;
                }

                tempstr_to = Acct_to.Split(delim);
                t_to = tempstr_to[0].PadLeft(4, '0') + tempstr_to[1].PadLeft(7, '0') + tempstr_to[2].PadLeft(3, '0') + tempstr_to[3].PadLeft(4, '0') + tempstr_to[4].PadLeft(3, '0');
                tempstr_fro = Acct_fro.Split(delim);
                t_from = tempstr_fro[0].PadLeft(4, '0') + tempstr_fro[1].PadLeft(7, '0') + tempstr_fro[2].PadLeft(3, '0') + tempstr_fro[3].PadLeft(4, '0') + tempstr_fro[4].PadLeft(3, '0');

                //Customer Cummulative check Applies only to withdrawal
                //if (tempstr_to[3].Equals("31"))  //when account to credit is tellers till then its a withdrawal transactions.
                //{
                //    }

                Amount = Math.Round(Amount, 2);

                T_amt = Amount;

                Req_code = "32";

                ResultStr = this.PostToBasis(t_from, t_to, T_amt, expl_code, Remarks, Req_code, teller_origt_bra_code, tellerid);

                if (ResultStr.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
                {
                    if (transtype.ToUpper().Equals("WITHDRAWAL"))
                    {

                        double ChargeAmount = 0;
                        if (cashlesswithdrawalcumulativeamt > cashlesswithdrawallimit)
                        {
                            ChargeAmount = Amount;
                        }
                        else
                        {
                            ChargeAmount = (cashlesswithdrawalcumulativeamt + Amount) - cashlesswithdrawallimit;
                        }
                        if (ChargeAmount > 0)
                        {
                            string[] acctdetails = null;
                            acctdetails = Acct_to.Split('/');
                            string bra_code = acctdetails[0];
                            string[] cusdetails = null;
                            teller_origt_bra_code = bra_code;
                            cusdetails = Acct_fro.Split('/');
                            //  if (bra_code.StartsWith("2") && (!cusdetails[0].ToString().StartsWith("2")))// the account to debit(tellers till is a lagos account take the cashless charges)
                            if (qualifiesForCashLess(bra_code, cusdetails[0].ToString()))
                            // the account to credit(tellers till is a lagos account take the cashless charges)
                            {
                                string cashlesschargeaccount = ConfigurationManager.AppSettings["CashlessChargeAccount"];
                                string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
                                string cashlessChargeAcct_to = bra_code + cashlesschargeaccount;
                                string VATAcct_to = bra_code + VATAccount;
                                string response = TransferChargesInternal(Acct_fro, cashlessChargeAcct_to, VATAcct_to, cashlesswithdrawalrate * ChargeAmount, 3020, "PinPad Cashless Withdrawal Charge : " + Acct_fro, tellerid);
                                Utility.Log().Info(string.Format("Posting For {0}", Acct_fro));
                            }     //Account to debit must be the customer's account (for withdrawal that is the account to debit)
                        }
                    }
                    else if (transtype.ToUpper().Equals("DEPOSIT"))
                    {
                        double ChargeAmount = 0;
                        if (cashlessdepositcumulativeamt > cashlessdepositlimit) // the cummulative is already greater than the limit the customer must have been charged before; so charge only the current amount
                        {
                            ChargeAmount = Amount;
                        }
                        else
                        {
                            ChargeAmount = (cashlessdepositcumulativeamt + Amount) - cashlessdepositlimit; //the total cummulative + amt is now over calculate the difference and charge.
                        }
                        if (ChargeAmount > 0)
                        {
                            string[] acctdetails = null;
                            string[] cusdetails = null;
                            acctdetails = Acct_fro.Split('/');
                            string bra_code = acctdetails[0];
                            teller_origt_bra_code = bra_code;
                            cusdetails = Acct_to.Split('/');
                            // if (bra_code.StartsWith("2") && (!cusdetails[0].ToString().StartsWith("2")))// the account to debit(tellers till is a lagos account take the cashless charges)
                            if (qualifiesForCashLess(bra_code, cusdetails[0].ToString()))
                            // the account to credit(tellers till is a lagos account take the cashless charges)
                            {
                                string cashlesschargeaccount = ConfigurationManager.AppSettings["CashlessChargeAccount"];
                                string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
                                string cashlessChargeAcct_to = bra_code + cashlesschargeaccount;
                                string VATAcct_to = bra_code + VATAccount;
                                TransferChargesInternal(Acct_to, cashlessChargeAcct_to, VATAcct_to, cashlessdepositrate * ChargeAmount, 3020, "PinPad Cashless Deposit Charge : " + Acct_to, tellerid);
                            }
                        }
                    }

                    xmlstring = "";
                    xmlstring = xmlstring + "<Response>";
                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1002</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else
                {
                    //get basis return error
                    xmlstring = xmlstring + "<CODE>1004</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }

                xmlstring = xmlstring + "</Response>";
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                xmlstring = "<Response>";
                xmlstring = xmlstring + "<CODE>1004</CODE>";
                xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
                xmlstring = xmlstring + "</Response>";
            }

            return xmlstring;
        }
        else
        {
            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1003</CODE>";
            xmlstring = xmlstring + "<ERROR>Transaction failed: The Service is currently not available on PinPad. Please process on BASIS.</ERROR>";
            xmlstring = xmlstring + "</Response>";
            return xmlstring;
        }

    }

    private bool qualifiesForCashLess(string teller_bra_code, string customer_bra_code)
    {
        bool qualifies = false;

        if ((checkBranchCashlessStatus(teller_bra_code).Equals("1")) && (!checkBranchCashlessStatus(customer_bra_code).Equals("1")))
        {
            qualifies = true;
        }
        else
        {
            qualifies = false;
        }

        return qualifies;
    }

    private string checkBranchCashlessStatus(string bra_code)
    {
        try
        {
            using (OracleCommand oracomm = new OracleCommand())
            {
                using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
                {
                    oracomm.Connection = OraConn;
                    oracomm.CommandText = " select nvl(con_val_1,'0') cashlessenabled from text_tab a ,branch b where b.bra_code = " + bra_code + " and a.tab_ent= b.city_loc_code and a.tab_id = 190 ";

                    oracomm.CommandType = CommandType.Text;
                    if (OraConn.State == ConnectionState.Closed)
                    {
                        OraConn.Open();
                    }

                    using (OracleDataReader dr = oracomm.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (dr.Read())
                        {
                            return dr["cashlessenabled"].ToString();
                        }
                        else
                        {
                            return "0";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("checkBranchCashlessStatus ", ex);
            return "0";
        }
    }

    public bool isServiceActive(string serviceid)
    {
        //1 withdrawal
        //2 deposit
        //3 thirdpartydeposit

        if (serviceid.Equals("1"))
        {
            return Convert.ToBoolean(ConfigurationManager.AppSettings["WithdrawalActive"]);
        }
        else if (serviceid.Equals("2"))
        {
            return Convert.ToBoolean(ConfigurationManager.AppSettings["DepositActive"]);
        }
        else if (serviceid.Equals("3"))
        {
            return Convert.ToBoolean(ConfigurationManager.AppSettings["ThirdPartyActive"]);
        }
        else
        {
            return false;
        }
    }

    public double cummulativeamount(string OldAccountNumber, string expl_code)
    {
        char acctsplit = Convert.ToChar("/");
        string[] accountkey = new string[4];// 
        string bra_code = null;
        string cus_num = null;
        string cur_code = null;
        string led_code = null;
        string sub_acct_code = null;

        accountkey = OldAccountNumber.Trim().Split(acctsplit);
        bra_code = accountkey[0];
        cus_num = accountkey[1];
        cur_code = accountkey[2];
        led_code = accountkey[3];
        sub_acct_code = accountkey[4];

        OracleCommand OraSelect = new OracleCommand();
        OracleDataReader OraDrSelect;
        double CummulativeAmount = 0;

        try
        {
            using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
            {
                if (OraConn.State == ConnectionState.Closed)
                {
                    OraConn.Open();
                }
                OraSelect.Connection = OraConn;
                string selectquery = "select nvl(sum(tra_amt),0) cummulativetransamt from tell_act where bra_code = " + bra_code + " and cus_num = " + cus_num + " and cur_code = " + cur_code + " and expl_code = " + expl_code;
                OraSelect.CommandText = selectquery;
                OraSelect.CommandType = CommandType.Text;
                using (OraDrSelect = OraSelect.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (OraDrSelect.HasRows == true)
                    {
                        OraDrSelect.Read();
                        CummulativeAmount = Convert.ToDouble(OraDrSelect["cummulativetransamt"].ToString());
                        return CummulativeAmount;
                    }
                    else
                    {
                        return 0; // no records;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("cummulativeamount ", ex);

            return 0;
        }
    }

    public double cashlesscummulativeamount(string OldAccountNumber, string expl_code)
    {
        char acctsplit = Convert.ToChar("/");
        string[] accountkey = new string[4];// 
        string bra_code = null;
        string cus_num = null;
        string cur_code = null;
        string led_code = null;
        string sub_acct_code = null;

        accountkey = OldAccountNumber.Trim().Split(acctsplit);
        bra_code = accountkey[0];
        cus_num = accountkey[1];
        cur_code = accountkey[2];
        led_code = accountkey[3];
        sub_acct_code = accountkey[4];

        OracleCommand OraSelect = new OracleCommand();
        OracleDataReader OraDrSelect;
        double CummulativeAmount = 0;


        try
        {
            using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
            {
                if (OraConn.State == ConnectionState.Closed)
                {
                    OraConn.Open();
                }
                OraSelect.Connection = OraConn;
                //  string selectquery = "select nvl(sum(tra_amt),0) cummulativetransamt from tell_act where bra_code = " + bra_code + " and cus_num = " + cus_num + " and cur_code = " + cur_code + " and led_code = " + led_code + " and sub_acct_code = " + sub_acct_code + " and origt_bra_code like '2_%' and expl_code = " + expl_code; //only lagos transactions
                //  string selectquery = "select nvl(sum(tra_amt),0) cummulativetransamt from tell_act where bra_code = " + bra_code + " and cus_num = " + cus_num + " and cur_code = " + cur_code + " and led_code = " + led_code + " and sub_acct_code = " + sub_acct_code + " and origt_bra_code in (select b.bra_code from text_tab a ,branch b where a.tab_ent= b.city_loc_code and a.tab_id = 190 and a.con_val_1 = 1) and expl_code =" + expl_code;
                string selectquery = "select nvl(sum(tra_amt),0) cummulativetransamt from tell_act where bra_code = " + bra_code + " and cus_num = " + cus_num + " and cur_code = " + cur_code + " and led_code = " + led_code + " and origt_bra_code in (select b.bra_code from text_tab a ,branch b where a.tab_ent= b.city_loc_code and a.tab_id = 190 and a.con_val_1 = 1) and expl_code =" + expl_code;

                OraSelect.CommandText = selectquery;
                OraSelect.CommandType = CommandType.Text;
                using (OraDrSelect = OraSelect.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (OraDrSelect.HasRows == true)
                    {
                        OraDrSelect.Read();
                        CummulativeAmount = Convert.ToDouble(OraDrSelect["cummulativetransamt"].ToString());
                        return CummulativeAmount;

                    }
                    else
                    {
                        return 0; // no records;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            return -1;
        }
    }

    public string TransferCharges(string Acct_fro, string Acct_to, string VATAcct_to, double Amount, int expl_code, string Remarks)
    {
        xmlstring = "<Response>";
        // No Posting Done
        xmlstring = xmlstring + "<CODE>1000</CODE>";
        xmlstring = xmlstring + "<ERROR>OK.</ERROR>";
        xmlstring = xmlstring + "</Response>";
        return xmlstring;
    }

    public string TransferChargesInternal(string Acct_fro, string Acct_to, string VATAcct_to, double Amount, int expl_code, string Remarks, string tellerid)
    {
        string t_from, t_to, vat_t_to, Req_code, ResultStr, ResultStrVAT;
        char[] charsep = { '+' };
        double T_amt;
        int Expl_code = expl_code;
        char[] delim = new char[] { '/' };
        string[] tempstr;
        bool chk_bal = false;
        string Remarks1 = null;
        //  string fro_bra_code, fro_cus_num;
        string to_bra_code = string.Empty;
        try
        {
            xmlstring = "<Response>";

            //Check account format
            chk_bal = checkAccountFormat(Acct_fro.Trim());
            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to debit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            chk_bal = checkAccountFormat(Acct_to.Trim());
            if (chk_bal == false)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<ERROR>Transaction failed: Format of account to credit is wrong or account does not exist in BASIS.</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            tempstr = Acct_to.Split(delim);
            to_bra_code = tempstr[0].ToString();
            t_to = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');
            tempstr = Acct_fro.Split(delim);
            t_from = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

            tempstr = VATAcct_to.Split(delim);
            vat_t_to = tempstr[0].PadLeft(4, '0') + tempstr[1].PadLeft(7, '0') + tempstr[2].PadLeft(3, '0') + tempstr[3].PadLeft(4, '0') + tempstr[4].PadLeft(3, '0');

            Amount = Math.Round(Amount, 2);

            T_amt = Amount;

            Req_code = "32";

            //Check if customer is not corporate. Only process for individual account
            int acctnat = GetCustomerAccountNature(Acct_fro);

            if (acctnat == 1)
            {
                ResultStr = this.PostToBasis(t_from, t_to, T_amt, expl_code, Remarks, Req_code, to_bra_code, tellerid); //Post Main commission  expl_code = 429

                if (ResultStr.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
                {


                    ResultStrVAT = this.PostToBasis(t_from, vat_t_to, T_amt * (0.05), 157, Remarks + "-VAT", Req_code, to_bra_code, tellerid); //Post VAT Charge expl_code = 157

                    if (ResultStrVAT.CompareTo("@ERR7@") == 0 || ResultStr.CompareTo("@ERR19@") == 0)
                    {
                        xmlstring = xmlstring + "<CODE>1000</CODE>"; //both are successful
                        xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                        xmlstring = xmlstring + "<VAT>SUCESS</VAT>";

                    }
                    else
                    {
                        xmlstring = xmlstring + "<CODE>1005</CODE>";  //only main charge succeeded VAT Failed
                        xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                        xmlstring = xmlstring + "<VAT>ERROR</VAT>";
                    }

                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1002</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else if (ResultStr.CompareTo("@ERR23@") == 0 || ResultStr.CompareTo("@ERR24@") == 0)
                {
                    xmlstring = xmlstring + "<CODE>1003</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }
                else
                {
                    //get basis return error
                    xmlstring = xmlstring + "<CODE>1004</CODE>";
                    xmlstring = xmlstring + "<ERROR> " + ErrorMsg(ResultStr) + "</ERROR>";
                }

                xmlstring = xmlstring + "</Response>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>"; //customer not Individual
                xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
                xmlstring = xmlstring + "<VAT>SUCESS</VAT>";
                xmlstring = xmlstring + "</Response>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1004</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    private int GetCustomerAccountNature(string acctno)
    {
        string query_str, Remark = string.Empty;
        int acct_nat = 0;

        char[] delim = new char[] { '/' };
        string[] tempstr;
        //double comamt, vatamt;
        //string commstr, vatstr;

        try
        {
            tempstr = acctno.Split(delim);

            // create the connection
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);

            // create the command for the function
            query_str = "select acct_nat from account where bra_code=" + tempstr[0].Trim() + " and cus_num=" + tempstr[1].Trim() + " and cur_code=" + tempstr[2].Trim() + " and led_code=" + tempstr[3].Trim() + " and sub_acct_code=" + tempstr[4].Trim();

            oraconn.Open();
            OracleCommand cmd = new OracleCommand(query_str, oraconn);
            OracleDataReader reader;

            // execute the function
            reader = cmd.ExecuteReader();
            if (reader.HasRows == true)
            {
                reader.Read();
                acct_nat = Convert.ToInt32(reader["acct_nat"]);
                cmd = null;
                oraconn.Close();
                return acct_nat;
            }
            else
            {
                reader = null;
                cmd = null;
                oraconn.Close();
                return acct_nat;
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            return acct_nat;
        }
    }

    public string GetAccountBalance(int bracode, int cusnum, int curcode, int ledcode, int subacctcode)
    {
        string Remark = string.Empty, query_str;
        char[] charsep = { '+' };
        string[] outputStr = { "A", "B" };
        char[] delim = new char[] { '/' };
        string param_str = null;
        double amt = 0.0;

        try
        {
            xmlstring = "<Response>";

            // create the connection
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
            param_str = bracode.ToString() + "," + cusnum.ToString() + "," + curcode.ToString() + "," + ledcode.ToString() + "," + subacctcode.ToString();
            // create the command for the function
            query_str = "select navailbal(" + param_str + ") as Avail_Bal, cle_bal, crnt_bal from account where bra_code=" + bracode.ToString() + " and cus_num=" + cusnum.ToString() + " and cur_code=" + curcode.ToString() + " and led_code=" + ledcode.ToString() + " and sub_acct_code=" + subacctcode.ToString();

            oraconn.Open();
            OracleCommand cmd = new OracleCommand(query_str, oraconn);
            OracleDataReader reader;

            // execute the function
            reader = cmd.ExecuteReader();
            if (reader.HasRows == true)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                reader.Read();

                //get current balance
                amt = Convert.ToDouble(reader["crnt_bal"]);
                amt = Math.Round(amt, 2);
                xmlstring = xmlstring + "<BOOKBALANCE>" + amt.ToString("N").Replace(",", "") + "</BOOKBALANCE>";

                //get available balance
                amt = Convert.ToDouble(reader["Avail_Bal"]);
                amt = Math.Round(amt, 2);
                xmlstring = xmlstring + "<AVAILABLEBALANCE>" + amt.ToString("N").Replace(",", "") + "</AVAILABLEBALANCE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR>ACCOUNT DOES NOT EXIST</ERROR>";
            }

            reader.Close();
            reader = null;
            cmd = null;
            oraconn.Close();
            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public double GetAvailBalance(string cusnum)
    {
        string Remark = string.Empty, query_str;
        char[] charsep = { '+' };

        string[] outputStr = { "A", "B" };
        char[] delim = new char[] { '/' };
        string param_str = null;
        double amt = 0.0;

        try
        {
            xmlstring = "<Response>";

            // create the connection
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
            param_str = cusnum.Replace("/", ",");// bracode.ToString() + "," + cusnum.ToString() + "," + curcode.ToString() + "," + ledcode.ToString() + "," + subacctcode.ToString();
            // create the command for the function
            query_str = "select navailbal(" + param_str + ") as Avail_Bal from dual";

            oraconn.Open();
            OracleCommand cmd = new OracleCommand(query_str, oraconn);
            OracleDataReader reader;

            // execute the function
            reader = cmd.ExecuteReader();
            if (reader.HasRows == true)
            {
                reader.Read();
                //get current balance
                amt = Convert.ToDouble(reader["Avail_Bal"]);
                return amt;
            }
            else
            {
                return 0;
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            return 0;
        }
    }

    public string GetCustomerName(int bracode, int cusnum)
    {
        string Remark = string.Empty, query_str;
        char[] charsep = { '+' };
        string[] outputStr = { "A", "B" };
        char[] delim = new char[] { '/' };


        try
        {
            xmlstring = "<Response>";

            // create the connection
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
            // create the command for the function
            query_str = "select cus_sho_name from address where bra_code=" + bracode.ToString() + " and cus_num=" + cusnum.ToString() + " and cur_code=0 and led_code=0 and sub_acct_code=0";

            oraconn.Open();
            OracleCommand cmd = new OracleCommand(query_str, oraconn);
            OracleDataReader reader;

            // execute the function
            reader = cmd.ExecuteReader();
            if (reader.HasRows == true)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                reader.Read();

                //get current balance
                xmlstring = xmlstring + "<CUSTOMERNAME>" + reader["cus_sho_name"].ToString() + "</CUSTOMERNAME>";

            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR>ACCOUNT DOES NOT EXIST</ERROR>";
            }

            reader.Close();
            reader = null;
            cmd = null;
            oraconn.Close();
            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string GetLastTransactionDetails(int bracode, int cusnum, int curcode, int ledcode, int subacctcode)
    {
        string Remark = string.Empty;
        char[] charsep = { '+' };
        string[] outputStr = { "A", "B" };
        char[] delim = new char[] { '/' };
        double amt = 0.0;
        OracleDataAdapter adpt = null;
        DataSet ds, dsfinal = null;
        DateTime dt;
        string datestr = null;

        try
        {
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_ivr"]);
            xmlstring = "<Response>";
            OracleCommand oracomm = new OracleCommand("IVRTRANS.TRANSACTS", oraconn);
            oracomm.CommandType = CommandType.StoredProcedure;

            try
            {
                if (oraconn.State == ConnectionState.Closed)
                {
                    oraconn.Open();
                }
                oracomm.Parameters.Add("bra", OracleDbType.Int32).Value = bracode;
                oracomm.Parameters.Add("cusnum", OracleDbType.Int32).Value = cusnum;
                oracomm.Parameters.Add("cur", OracleDbType.Int32).Value = curcode;
                oracomm.Parameters.Add("led", OracleDbType.Int32).Value = ledcode;
                oracomm.Parameters.Add("sub", OracleDbType.Int32).Value = subacctcode;
                oracomm.Parameters.Add("results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                adpt = new OracleDataAdapter(oracomm);
                dsfinal = new DataSet();
                adpt.Fill(dsfinal);
                oraconn.Close();
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);

                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR>" + ex.Message.Replace("'", "") + "</ERROR>";
                xmlstring = xmlstring + "</Response>";
                return xmlstring;
            }

            oraconn.Close();
            oraconn = null;

            if (dsfinal.Tables[0].Rows.Count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<TRANSACTIONS>";
                foreach (DataRow dr in dsfinal.Tables[0].Rows)
                {
                    xmlstring = xmlstring + "<TRANSACTION>";
                    dt = Convert.ToDateTime(dr["TRA_DATE"]);
                    datestr = dt.Year.ToString().PadLeft(4, '0') + "-" + dt.Month.ToString().PadLeft(2, '0') + "-" + dt.Day.ToString().PadLeft(2, '0');
                    xmlstring = xmlstring + "<TRADATE>" + datestr + "</TRADATE>";
                    amt = Math.Abs(Convert.ToDouble(dr["amt"]));
                    amt = Math.Round(Math.Abs(amt), 2);
                    xmlstring = xmlstring + "<TRAAMT>" + amt.ToString("N").Replace(",", "") + "</TRAAMT>";
                    xmlstring = xmlstring + "<TRASTATUS>" + GetIVRPrompt(dr["EXPL_CODE"].ToString()) + "</TRASTATUS>";
                    xmlstring = xmlstring + "<TRABRACODE>" + dr["ORIGT_BRA_CODE"].ToString() + "</TRABRACODE>";
                    xmlstring = xmlstring + "<PROMPTFILE>" + GetIVRPrompt("BR" + dr["ORIGT_BRA_CODE"].ToString()) + "</PROMPTFILE>";
                    xmlstring = xmlstring + "</TRANSACTION>";
                }
                xmlstring = xmlstring + "</TRANSACTIONS>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR>COULD NOT RETRIEVE TRANSACTIONS</ERROR>";
            }
            dsfinal = null;
            oracomm = null;
            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string GetLastAccountStatement(int bracode, int cusnum, int curcode, int ledcode, int subacctcode, string custemail)
    {
        string Remark = string.Empty, status_str;
        char[] charsep = { '+' };
        string[] outputStr = { "A", "B" };
        char[] delim = new char[] { '/' };
        string sentemail;
        Email email = new Email();
        string sub_mess;
        string main_mess;
        double amt = 0.0;

        OracleDataAdapter adpt = null;
        DataSet dsfinal = null;

        try
        {
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_ivr"]);
            xmlstring = "<Response>";
            OracleCommand oracomm = new OracleCommand("IVRTRANS.TRANSACTS", oraconn);
            oracomm.CommandType = CommandType.StoredProcedure;
            try
            {
                if (oraconn.State == ConnectionState.Closed)
                {
                    oraconn.Open();
                }
                oracomm.Parameters.Add("bra", OracleDbType.Int32).Value = bracode;
                oracomm.Parameters.Add("cusnum", OracleDbType.Int32).Value = cusnum;
                oracomm.Parameters.Add("cur", OracleDbType.Int32).Value = curcode;
                oracomm.Parameters.Add("led", OracleDbType.Int32).Value = ledcode;
                oracomm.Parameters.Add("sub", OracleDbType.Int32).Value = subacctcode;
                oracomm.Parameters.Add("results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                adpt = new OracleDataAdapter(oracomm);
                dsfinal = new DataSet();
                adpt.Fill(dsfinal);
                oraconn.Close();
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);

                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR>" + ex.Message.Replace("'", "") + "</ERROR>";
                xmlstring = xmlstring + "</Response>";

                return xmlstring;
            }

            oraconn.Close();
            oraconn = null;

            if (dsfinal.Tables[0].Rows.Count > 0)
            {
                try
                {
                    sub_mess = "GTBank IVR - Your statement by email";
                    sub_mess = sub_mess + " (" + bracode.ToString() + "/" + cusnum.ToString() + "/" + curcode.ToString() + "/" + ledcode.ToString() + "/" + subacctcode.ToString() + ")";
                    main_mess = "\n" + "S/No".PadRight(5) + "Transaction Amount".PadRight(20) + "Transaction Date".PadRight(20) + "\n";

                    foreach (DataRow dr in dsfinal.Tables[0].Rows)
                    {
                        amt = Math.Abs(Convert.ToDouble(dr["AMT"]));
                        amt = Math.Round(Math.Abs(amt), 2);
                        if (dr["DEB_CRE_IND"].ToString() == "1")
                            status_str = amt.ToString("N") + "(DR)";
                        else
                            status_str = amt.ToString("N") + "(CR)";

                        main_mess = main_mess + "\n" + dr["ROWNUM"].ToString().PadRight(5) + status_str.PadRight(20) + Convert.ToDateTime(dr["TRA_DATE"]).ToShortDateString().PadRight(20);

                    }
                    sentemail = email.SendEmail(custemail, sub_mess, main_mess, "");

                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "</MESSAGE>SUCCESS</MESSAGE>";

                }
                catch (Exception ex)
                {
                    xmlstring = xmlstring + "<CODE>1010</CODE>";
                    xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR>COULD NOT RETRIEVE TRANSACTIONS</ERROR>";
            }
            dsfinal = null;
            oracomm = null;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
        }

        xmlstring = xmlstring + "</Response>";
        return xmlstring;
    }

    public string StopCheque(int bracode, int cusnum, int curcode, int ledcode, int subacctcode, int startno, int endno, string amt)
    {
        string Result = string.Empty;
        Decimal Amount = 0;
        OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_ivr"]);
        OracleCommand oracomm = new OracleCommand("ivrstopay", oraconn);
        oracomm.CommandType = CommandType.StoredProcedure;
        xmlstring = "<Response>";
        try
        {
            Amount = Convert.ToDecimal(amt);
            if (oraconn.State == ConnectionState.Closed)
            {
                oraconn.Open();
            }
            oracomm.Parameters.Add("BRA", OracleDbType.Int32).Value = bracode;
            oracomm.Parameters.Add("CUSNUM", OracleDbType.Int32).Value = cusnum;
            oracomm.Parameters.Add("CUR", OracleDbType.Int32).Value = curcode;
            oracomm.Parameters.Add("LED", OracleDbType.Int32).Value = ledcode;
            oracomm.Parameters.Add("SUB", OracleDbType.Int32).Value = subacctcode;
            oracomm.Parameters.Add("DOCNUMFROM", OracleDbType.Int32).Value = startno;
            oracomm.Parameters.Add("DOCNUMTO", OracleDbType.Int32).Value = endno;
            oracomm.Parameters.Add("CHQAMT", OracleDbType.Int32).Value = Amount;
            oracomm.Parameters.Add("RETURN_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;

            oracomm.ExecuteNonQuery();
            Result = oracomm.Parameters["RETURN_STATUS"].Value.ToString();
            oraconn.Close();

            if (Result == "0")
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<ERROR>STOP CHEQUE FAILED WITH RESULT CODE:" + Result + "</ERROR>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<ERROR>STOP CHEQUE FAILED WITH BASIS ERROR:" + ex.Message.Replace("'", "") + "</ERROR>";
        }

        oraconn.Close();
        oracomm = null;
        xmlstring = xmlstring + "</Response>";

        return xmlstring;
    }

    public string GetRoleUsers(int id, int appid)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        DataTable dt;
        DataRow dr;
        string xmlrep = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetApplicationRoleUsers", conn);
            comm.Parameters.AddWithValue("@RoleID", id);
            comm.Parameters.AddWithValue("@ApplicationID", appid);

            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();
            adpt.Fill(ds);

            xmlstring = "<Response>";
            xmlrep = ds.GetXml();
            if (ds.Tables[0].Rows.Count > 0)
            {
                dr = ds.Tables[0].Rows[0];

                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<USER>";
                xmlstring = xmlstring + "<ID>" + dr["User_id"] + "</ID>";
                xmlstring = xmlstring + "<NAME>" + dr["User_name"] + "</NAME>";
                xmlstring = xmlstring + "<BRANCH>" + dr["branch_code"] + "</BRANCH>";
                xmlstring = xmlstring + "<EMAIL>" + dr["email"].ToString() + "</EMAIL>";
                xmlstring = xmlstring + "</USER>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "COULD NOT RETRIEVE USERS" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    private string GetIVRPrompt(string code)
    {
        string uname = null;
        SqlDataReader reader;
        //SqlConnection conn = new SqlConnection(TTrackerDAL.Properties.Settings.Default.E_OneDB);
        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());
        string queryString = "Select description, code, file_name From promptsetup where code = '" + code + "'";

        SqlCommand comm = new SqlCommand(queryString, conn);
        comm.CommandType = CommandType.Text;
        //open connetion
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }
        reader = comm.ExecuteReader();
        if (reader.HasRows)
        {
            reader.Read();
            uname = reader["file_name"].ToString();
        }
        else
        {
            uname = "plural.wav";
        }
        reader.Close();
        reader = null;
        conn.Close();
        conn = null;
        return uname;
    }
    public string GetAdminUserName(string userid)
    {
        string uname = null;
        SqlDataReader reader;
        Utilities util = new Utilities();
        //SqlConnection conn = new SqlConnection(TTrackerDAL.Properties.Settings.Default.E_OneDB);
        SqlConnection conn;
        if (bool.Parse(ConfigurationManager.AppSettings["UseAccessManager"]))
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["AccessManagerConnString"].ToString());
        }
        else
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());
        }


        //SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());
        string queryString = "Select user_name From admin_users where user_id=" + util.SafeSqlLiteral(userid, 1);

        SqlCommand comm = new SqlCommand(queryString, conn);
        comm.CommandType = CommandType.Text;
        //open connetion
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }
        reader = comm.ExecuteReader();
        reader.Read();
        uname = reader["user_name"].ToString();
        reader.Close();
        reader = null;
        conn.Close();
        conn = null;
        return uname;
    }

    private string PostToBasisTraSeq(string acctFrom, string acctTo, double traAmt, int explCode, string remark, string reqCode)
    {
        string Result = string.Empty;
        string Result2 = string.Empty;
        
        string uniqueRef = GenerateTransactionReference(30);
        
        try
        {
            string channel = ConfigurationManager.AppSettings["ChannelCode"].ToString();// "PS";
            string docAlpha = ConfigurationManager.AppSettings["DOCALPHA"];

            var r = new TransferRequest
            {
                AccFrom = ConvertPaddedToOldAcct(acctFrom),
                AccTo = ConvertPaddedToOldAcct(acctTo),
                Channel = channel,
                DocAlpha = docAlpha,
                ExplCode = explCode,
                Remark = remark,
                TraAmount = Convert.ToDecimal(traAmt),
                TellerId = 9011,
                ReqCode = Convert.ToInt32(reqCode),
                UniqueTransactionReference = uniqueRef
            };

            Entry e = new Entry { TransferRequest = r };

            string token = GetAppToken(channel);
            e.AppCode = channel;
            e.Token = token;
            e.RequestCode = ServiceCodes.TransferRequest;

            Utility.Log().Info("Request " + Newtonsoft.Json.JsonConvert.SerializeObject(r));

            e.Process();

            Utility.Log().Info("Response " + Newtonsoft.Json.JsonConvert.SerializeObject(e.Response));

            if (e.Response.Code == ResponseCodes.Success.Item1)
            {
                JObject j = JObject.Parse(e.Response.OtherMessage);
                string resp = j.GetValue("dsdddd").Value<string>();

                return "@ERR7@" + "," + resp;
            }
            return e.Response.OtherMessage ?? e.Response.Code.ToString();
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            return "";
        }
    }

    private string PostToBasis(string acctFrom, string acctTo, double traAmt, int explCode, string remark, string reqCode)
    {
        const string channel = "PS";
        const string docAlpha = "PPAD";
        string uniqueRef = GenerateTransactionReference(30);

        try
        {
            var r = new TransferRequest
            {
                AccFrom = ConvertPaddedToOldAcct(acctFrom),
                AccTo = ConvertPaddedToOldAcct(acctTo),
                Channel = channel,
                DocAlpha = docAlpha,
                ExplCode = explCode,
                Remark = remark,
                TraAmount = Convert.ToDecimal(traAmt),
                TellerId = 9011,
                ReqCode = Convert.ToInt32(reqCode),
                UniqueTransactionReference = uniqueRef
            };

            Entry e = new Entry { TransferRequest = r };

            string token = GetAppToken(channel);
            e.AppCode = channel;
            e.Token = token;
            e.RequestCode = ServiceCodes.TransferRequest;

            Utility.Log().Info("Request " + Newtonsoft.Json.JsonConvert.SerializeObject(r));
            e.Process();

            Utility.Log().Info("Response " + Newtonsoft.Json.JsonConvert.SerializeObject(e.Response));

            if (e.Response.Code == ResponseCodes.Success.Item1)
            {
                return "@ERR7@";
            }

            return e.Response.OtherMessage ?? e.Response.Code.ToString();
        }
        catch (Exception ex)
        {
            Utility.Log().Info("Error Occured While Posting to Basis" + ex);
            return "";
        }
    }

    private string PostToBasis(string acctFrom, string acctTo, double traAmt, int explCode, string remark, string reqCode, string origtBraCode, string tellerid)
    {
        string channel = ConfigurationManager.AppSettings["ChannelCode"].ToString();// "PS";
        string docAlpha = ConfigurationManager.AppSettings["DOCALPHA"];

        string uniqueRef = GenerateTransactionReference(30);
        int TellerID;
        bool ValidTellerID = int.TryParse(tellerid.Trim(), out TellerID);
        if (!ValidTellerID)
        {
            TellerID = 9011;
        }
        var r = new TransferRequest
        {
            AccFrom = ConvertPaddedToOldAcct(acctFrom),
            AccTo = ConvertPaddedToOldAcct(acctTo),
            Channel = channel,
            DocAlpha = docAlpha,
            ExplCode = explCode,
            Remark = remark,
            TraAmount = Convert.ToDecimal(traAmt),
            TellerId = TellerID,
            ReqCode = Convert.ToInt32(reqCode),
            UniqueTransactionReference = uniqueRef,
            OrigtBraCode = Convert.ToInt32(origtBraCode)
        };

        Entry e = new Entry { TransferRequest = r };

        string token = GetAppToken(channel);
        e.AppCode = channel;
        e.Token = token;
        e.RequestCode = ServiceCodes.TransferRequest;
        try
        {
            Utility.Log().Info("Request " + Newtonsoft.Json.JsonConvert.SerializeObject(r));
            e.Process();

            Utility.Log().Info("Response " + Newtonsoft.Json.JsonConvert.SerializeObject(e.Response));

            if (e.Response.Code == ResponseCodes.Success.Item1)
            {
                return "@ERR7@";
            }
            return e.Response.OtherMessage ?? e.Response.Code.ToString();
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Error Occured While Posting to Basis. ", ex);

            return "";
        }    
    }

    public string GetAppToken(string appcode)
    {
        string resp = "";
        var s = new Sql();

        string connstring = ConfigurationManager.ConnectionStrings["GTBWEBAPI"].ToString();
        resp = s.ExecuteSqlScalar("select token from Channel_DocAlp where Code = @appcode", connstring, false, new SqlParameter("@appcode", appcode));

        return resp;
    }

    public static string GenerateTransactionReference(int length)
    {
        var r = new Random(908992);

        string val = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        val = val + r.Next().ToString(CultureInfo.InvariantCulture);
        return val.PadLeft(length, '0');
    }

    private string ErrorMsg(string errorCode)
    {
        string errorDescription = string.Empty;
        switch (errorCode.Replace("-", ""))
        {
            case "@ERR1@":
                errorDescription = "Invalid Source Account";
                break;
            case "@ERR2@":
                errorDescription = "Source Account has restrictions";
                break;
            case "@ERR3@":
                errorDescription = "Invalid Target Account";
                break;
            case "@ERR4@":
                errorDescription = "Target Account has restrictions";
                break;
            case "@ERR5@":
                errorDescription = "Invalid Amount";
                break;
            case "@ERR6@":
                errorDescription = "Unknown Error: Transaction Unsuccessful!" + errorCode;
                break;
            case "@ERR7@":
                errorDescription = "Operation Successful!";
                break;
            case "@ERR8@":
                errorDescription = "Unknown Error: Transaction Unsuccessful!+errorCode";
                break;
            case "@ERR9@":
                errorDescription = "Transfer cannot be Executed";
                break;
            case "@ERR10@":
                errorDescription = "Invalid Account";
                break;
            case "@ERR11@":
                errorDescription = "Invalid Password";
                break;
            case "@ERR12@":
                errorDescription = "Invalid Branch";
                break;
            case "@ERR13@":
                errorDescription = "Invalid Check Number!";
                break;
            case "@ERR14@":
                errorDescription = "Cheque Cashed";
                break;
            case "@ERR15@":
                errorDescription = "Invalid Sub Account";
                break;
            case "@ERR16@":
                errorDescription = "Invalid Customer number";
                break;
            case "@ERR17@":
                errorDescription = "Customer has no PBS Account";
                break;
            case "@ERR18@":
                errorDescription = "Request Already Exists";
                break;
            case "@ERR19@":
                errorDescription = "Request accepted for further processing";
                break;
            case "@ERR20@":
                errorDescription = "Accounts involved do not belong to the same branch";
                break;
            case "@ERR21@":
                errorDescription = "Accounts involved do not belong to the same customer";
                break;
            case "@ERR22@":
                errorDescription = "Accounts involved are not of the same currency";
                break;
            case "@ERR23@":
                errorDescription = "Transfer amount exceeds accounts allowed transfer limit";
                break;
            case "@ERR24@":
                errorDescription = "Transfer Amount exceeds Accounts available balance";
                break;
            case "@ERR25@":
                errorDescription = "One of the Branches Involved is not on the network";
                break;
            case "@ERR26@":
                errorDescription = "No Data Retrieved";
                break;
            case "@ERR27@":
                errorDescription = "Not a checking Account";
                break;
            case "@ERR28@":
                errorDescription = "Source Account is Dormant";
                break;
            case "@ERR29@":
                errorDescription = "Target Account is Dormant";
                break;
            case "@ERR30@":
                errorDescription = "Source Account is Closed";
                break;
            case "@ERR31@":
                errorDescription = "Target Account is closed";
                break;
            case "@ERR32@":
                errorDescription = "External Transactions are not allowed on Source Account";
                break;
            case "@ERR33@":
                errorDescription = "External Transactions are not allowed on Target Account";
                break;
            case "@ERR58@":
                errorDescription = "Either the Source or the Target Account Has Restriction";
                break;
            case "@ERR65@":
                errorDescription = "Target Account is Dormant";
                break;
            case "@ERR844@":
                errorDescription = "Cheque is not within customer series";
                break;

            case "@ERR845@":
                errorDescription = "Cheque Has Been Processed Before";
                break;
            default:
                errorDescription = "Unknown Error With Error Code: " + errorCode + " Transaction Unsuccessful!";
                break;
        }
        return errorDescription;
    }

    private bool checkAccountFormat(string acctno)
    {
        char[] delim = new char[] { '/' };
        string[] tempstr;

        tempstr = acctno.Split(delim);
        if (tempstr.GetLength(0) != 5)
            return false;
        else if (tempstr[0].Length != 3)
            return false;
        else
            return true;
    }

    private string GetAccountFromBasis(string acctno)
    {
        string Result = string.Empty;
        string Result1 = string.Empty;
        
        try
        {

            using (OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_ivr"]))
            {
                using (OracleCommand oracomm = new OracleCommand("IVRCHECKACCOUNT", oraconn))
                {
                    oracomm.CommandType = CommandType.StoredProcedure;

                    if (oraconn.State == ConnectionState.Closed)
                    {
                        oraconn.Open();
                    }
                    oracomm.Parameters.Add("INP_ACCT", OracleDbType.Varchar2, 21).Value = acctno.Trim();
                    oracomm.Parameters.Add("OUT_ACCT", OracleDbType.Varchar2, 100).Direction = ParameterDirection.Output;
                    oracomm.Parameters.Add("OUT_RETURN_STATUS", OracleDbType.Varchar2, 100).Direction = ParameterDirection.Output;

                    oracomm.ExecuteNonQuery();
                    Result = oracomm.Parameters["OUT_RETURN_STATUS"].Value.ToString();
                    if (Result == "0")
                    {
                        Result1 = oracomm.Parameters["OUT_ACCT"].Value.ToString();
                    }
                    else
                    {
                        Result1 = "INVALID";
                    }

                    oraconn.Close();
                }
            }           
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            Result1 = ex.Message;
            Result1 = "INVALID";
        }

        return Result1;
    }

    public string ValidateAccountNuber(string uid, string acctno)
    {
        string finalstr = null;
        string limit = null;

        try
        {
            xmlstring = "<Response>";

            //Parse Account
            if (acctno.Length < 12)
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<Error>" + "INVALID ACCOUNT NUMBER" + "</Error>";
            }

            finalstr = GetAccountFromBasis(acctno);
            if (finalstr != "INVALID")
            {
                //get aggregated limit
                limit = CheckDailyLimit(uid);
                if (limit == "ERROR")
                    limit = "0";

                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<BENEFICIARY>";
                xmlstring = xmlstring + "<AccountNo>" + finalstr + "</AccountNo>";
                xmlstring = xmlstring + "<Limit>" + limit + "</Limit>";
                xmlstring = xmlstring + "</BENEFICIARY>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1003</CODE>";
                xmlstring = xmlstring + "<Error>" + "INVALID ACCOUNT NUMBER" + "</Error>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1004</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
        }

        xmlstring = xmlstring + "</Response>";
        return xmlstring;
    }

    public string CheckUserFlag(string uid)
    {
        SqlConnection conn = null;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("CheckUserflag", conn);
            comm.Parameters.AddWithValue("@UserID", uid);

            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();
            adpt.Fill(ds);

            if (ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr = (DataRow)ds.Tables[0].Rows[0];
                count = Convert.ToInt32(dr["mins"]);

                if (dr["pwdreq"].ToString() == "1" && count < 5)
                {
                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                }
                else
                {
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<ERROR>USER FLAG NOT SET</ERROR>";
                }

                dr = null;
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "INVALID USERNAME OR PASSWORD" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        ds = null;
        adpt = null;
        comm = null;
        conn.Close();
        conn = null;
        return xmlstring;

    }

    public string ResetUserFlag(string uid)
    {
        SqlConnection conn = null;
        SqlCommand comm;
        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("ResetUserflag", conn);
            comm.Parameters.AddWithValue("@UserID", uid);

            comm.CommandType = CommandType.StoredProcedure;
            comm.ExecuteNonQuery();
            xmlstring = xmlstring + "<CODE>1000</CODE>";
            xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        comm = null;
        conn.Close();
        conn = null;
        return xmlstring;

    }

    private string CheckDailyLimit(string userid)
    {
        string res = null;
        SqlDataReader reader;
        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());
        SqlCommand comm = new SqlCommand("Check_Daily_Limit2", conn);
        comm.Parameters.AddWithValue("@User_Id", Decimal.Parse(userid));
        comm.CommandType = CommandType.StoredProcedure;
        try
        {
            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            reader = comm.ExecuteReader();
            if (reader.HasRows == true)
            {
                reader.Read();
                res = reader["limit"].ToString();
            }
            else
            {
                res = "ERROR";
            }
            reader.Close();
            reader = null;

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            res = "ERROR";
        }

        return res;
    }

    private void LogTransfer(string userid, string fromacct, string toacct, Decimal amount, string medium, string Remarks)
    {
        string res = null;
        string from, to;
        char[] delim = new char[] { '/' };
        string[] tempstr;

        tempstr = fromacct.Split(delim);
        //from = tempstr[0]
        SqlDataReader reader;
        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());
        SqlCommand comm = new SqlCommand("Log_Transfer", conn);
        comm.Parameters.AddWithValue("@User_Id", Decimal.Parse(userid));
        comm.Parameters.AddWithValue("Source_Acct_No", fromacct.Trim());
        comm.Parameters.AddWithValue("@Beneficiary_Acct_No", toacct.Trim());
        comm.Parameters.AddWithValue("@Amount", amount);
        comm.Parameters.AddWithValue("@Medium", medium.Trim());
        comm.Parameters.AddWithValue("@Remarks", Remarks.Trim());

        comm.CommandType = CommandType.StoredProcedure;
        try
        {
            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            reader = comm.ExecuteReader();
            //if (reader.HasRows == true)
            //{
            //    reader.Read();
            //    res = reader["limit"].ToString();
            //}
            //else
            //{
            //    res = "ERROR";
            //}
            reader.Close();
            reader = null;

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            res = ex.Message;
        }

        return;
    }

    public string LogUserAction(int appid, string userid, long staffid, string actiondesc)
    {
        SqlConnection conn = null;
        SqlCommand comm;
        int count = 0, res = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("LogUserAction", conn);
            comm.Parameters.AddWithValue("@AppID", appid);
            comm.Parameters.AddWithValue("@UserID", userid);
            comm.Parameters.AddWithValue("@StaffID", staffid);
            comm.Parameters.AddWithValue("@ActionDesc", actiondesc);

            comm.CommandType = CommandType.StoredProcedure;
            res = comm.ExecuteNonQuery();

            if (res > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                xmlstring = xmlstring + "</Response>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>Could not insert new record</Error>";
                xmlstring = xmlstring + "</Response>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1002</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        comm = null;
        conn.Close();
        conn = null;
        return xmlstring;

    }

    public string AddNewAdminUser(string UserID, int BASISID, string Username, string staffid, string password, int branch_code, string emailaddress, string roleid, string status)
    {
        SqlConnection conn = null;
        SqlCommand comm;
        int res = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("AddNewAdminUser", conn);
            comm.Parameters.AddWithValue("@User_Id", UserID);
            comm.Parameters.AddWithValue("@basis_id", BASISID);
            comm.Parameters.AddWithValue("@UserName", Username);
            comm.Parameters.AddWithValue("@StaffId", staffid);
            comm.Parameters.AddWithValue("@Password", password);
            comm.Parameters.AddWithValue("@BranchCode", branch_code);
            comm.Parameters.AddWithValue("@Email", emailaddress);
            comm.Parameters.AddWithValue("@RoleID", roleid);
            comm.Parameters.AddWithValue("@Status", status);

            comm.CommandType = CommandType.StoredProcedure;
            res = comm.ExecuteNonQuery();

            if (res > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                xmlstring = xmlstring + "</Response>";
            }
            else if (res == -1)
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>Role does not exist</Error>";
                xmlstring = xmlstring + "</Response>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>Could not insert new record</Error>";
                xmlstring = xmlstring + "</Response>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1002</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        comm = null;
        conn.Close();
        conn = null;
        return xmlstring;

    }

    public string UpdateAdminUser(string UserID, int BASISID, string Username, string staffid, int branch_code, string emailaddress, string roleid)
    {
        SqlConnection conn = null;
        SqlCommand comm;
        int count = 0, res = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("UpdateAdminUser", conn);
            comm.Parameters.AddWithValue("@User_Id", UserID);
            comm.Parameters.AddWithValue("@basis_id", BASISID);
            comm.Parameters.AddWithValue("@UserName", Username);
            comm.Parameters.AddWithValue("@StaffId", staffid);
            comm.Parameters.AddWithValue("@BranchCode", branch_code);
            comm.Parameters.AddWithValue("@Email", emailaddress);
            comm.Parameters.AddWithValue("@RoleID", roleid);

            comm.CommandType = CommandType.StoredProcedure;
            res = comm.ExecuteNonQuery();

            if (res > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                xmlstring = xmlstring + "</Response>";
            }
            else if (res == -1)
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>Role does not exist</Error>";
                xmlstring = xmlstring + "</Response>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>Could not insert new record</Error>";
                xmlstring = xmlstring + "</Response>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1002</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        comm = null;
        conn.Close();
        conn = null;
        return xmlstring;

    }

    public string GetAdminUserDetails(string userid)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        DataRow dr, dr_app;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetAdminUserDetails", conn);
            comm.Parameters.AddWithValue("@UserID", userid);

            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "USER";

                dr = ds.Tables[0].Rows[0];

                xmlstring = xmlstring + "<CODE>1000</CODE>";

                //GET USER
                xmlstring = xmlstring + "<USER>";
                xmlstring = xmlstring + "<ID>" + dr["User_id"] + "</ID>";
                xmlstring = xmlstring + "<BASISID>" + dr["basis_id"].ToString() + "</BASISID>";
                xmlstring = xmlstring + "<NAME>" + dr["User_name"] + "</NAME>";
                xmlstring = xmlstring + "<STAFFID>" + dr["staff_id"] + "</STAFFID>";
                xmlstring = xmlstring + "<BRANCH>" + dr["branch_code"] + "</BRANCH>";
                xmlstring = xmlstring + "<EMAIL>" + dr["email"] + "</EMAIL>";
                xmlstring = xmlstring + "<ROLE_ID>" + dr["ROLE_ID"] + "</ROLE_ID>";
                xmlstring = xmlstring + "<DATE_CREATED>" + Convert.ToDateTime(dr["registration_date"]).ToShortDateString() + "</DATE_CREATED>";
                xmlstring = xmlstring + "<STATUS>" + dr["status"] + "</STATUS>";
                //xmlstring = xmlstring + "<CONFIRMED>" + dr["confirmed"] + "</CONFIRMED>";
                //xmlstring = xmlstring + "<LOCKED>" + dr["locked"] + "</LOCKED>";
                xmlstring = xmlstring + "<LAST_MODIFIED>" + Convert.ToDateTime(dr["last_modified_date"]).ToShortDateString() + "</LAST_MODIFIED>";

                xmlstring = xmlstring + "</USER>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "INVALID USER" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
        //return xmlrep;
    }

    public string SearchAdminUser(string searchstr)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        DataRow dr_app;
        string xmlrep = null;
        string xmlschema = null;
        string search = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            search = searchstr.Replace("*", "%");
            //search = search;
            comm = new SqlCommand("SearchAdminUser", conn);
            comm.Parameters.AddWithValue("@searchstr", search);

            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "USER";
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<USERS>";
                xmlstring = xmlstring + "<COUNT>" + ds.Tables[0].Rows.Count.ToString() + "</COUNT>";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    //GET USER
                    xmlstring = xmlstring + "<USER>";
                    xmlstring = xmlstring + "<ID>" + dr["User_id"] + "</ID>";
                    xmlstring = xmlstring + "<STAFFID>" + dr["staff_id"] + "</STAFFID>";
                    //xmlstring = xmlstring + "<NAME>" + dr["User_name"] + "</NAME>";
                    //xmlstring = xmlstring + "<BRANCH>" + dr["branch_code"] + "</BRANCH>";
                    xmlstring = xmlstring + "<EMAIL>" + dr["email"] + "</EMAIL>";
                    //xmlstring = xmlstring + "<ROLE_ID>" + dr["ROLE_ID"] + "</ROLE_ID>";
                    //xmlstring = xmlstring + "<DATE_CREATED>" + Convert.ToDateTime(dr["registration_date"]).ToShortDateString() + "</DATE_CREATED>";
                    xmlstring = xmlstring + "<STATUS>" + dr["status"] + "</STATUS>";
                    //xmlstring = xmlstring + "<CONFIRMED>" + dr["confirmed"] + "</CONFIRMED>";
                    //xmlstring = xmlstring + "<LOCKED>" + dr["locked"] + "</LOCKED>";
                    //xmlstring = xmlstring + "<LAST_MODIFIED>" + Convert.ToDateTime(dr["last_modified"]).ToShortDateString() + "</LAST_MODIFIED>";

                    xmlstring = xmlstring + "</USER>";
                }
                xmlstring = xmlstring + "</USERS>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "NO USER" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
        //return xmlrep;
    }

    public string GetAllRoles()
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        DataRow dr_app;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetAllRoles", conn);
            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "USER";
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<ROLES>";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    //GET USER
                    xmlstring = xmlstring + "<ROLE>";
                    xmlstring = xmlstring + "<ROLE_ID>" + dr["role_id"] + "</ROLE_ID>";
                    xmlstring = xmlstring + "<ROLE_NAME>" + dr["role_desc"] + "</ROLE_NAME>";
                    xmlstring = xmlstring + "</ROLE>";
                }
                xmlstring = xmlstring + "</ROLES>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "NO ROLES" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
        //return xmlrep;
    }

    public string GetTellerTillAcct(string userid, int appid)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetTellerTillAcct", conn);
            comm.CommandType = CommandType.StoredProcedure;
            comm.Parameters.AddWithValue("@user_id", userid);
            comm.Parameters.AddWithValue("@app_id", appid);

            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "TILL_ACCOUNT";
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    //GET TILL ACCOUNT
                    xmlstring = xmlstring + "<TILL_ACCOUNT_NO>" + dr["till_account"] + "</TILL_ACCOUNT_NO>";
                }

            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "0/0/0/0/0" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
        //return xmlrep;
    }

    public string GetAllBranches()
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        DataRow dr_app;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetAllBranches", conn);
            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "USER";
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<BRANCHES>";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    //GET USER
                    xmlstring = xmlstring + "<BRANCH>";
                    xmlstring = xmlstring + "<BRANCH_CODE>" + dr["branch_code"] + "</BRANCH_CODE>";
                    xmlstring = xmlstring + "<BRANCH_NAME>" + dr["branch_name"] + "</BRANCH_NAME>";
                    xmlstring = xmlstring + "</BRANCH>";
                }
                xmlstring = xmlstring + "</BRANCHES>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "NO ROLES" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
        //return xmlrep;
    }

    public string ChangeAdminUserPasscode(string userid, string password)
    {
        SqlConnection conn;
        SqlCommand comm;
        Email email = new Email();

        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("updateAdminPassCode", conn);
            comm.Parameters.AddWithValue("@u_id", userid);
            comm.Parameters.AddWithValue("@p_code", password);
            comm.Parameters.AddWithValue("@p_type", Int32.Parse("1"));

            comm.CommandType = CommandType.StoredProcedure;
            count = comm.ExecuteNonQuery();
            if (count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>1000</MESSAGE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "UNABLE TO UPDATE PASSWORD" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string ActivateAdminUser(string userid)
    {
        SqlConnection conn;
        SqlCommand comm;
        Email email = new Email();

        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("ActivateAdminUser", conn);
            comm.Parameters.AddWithValue("@userid", userid);

            comm.CommandType = CommandType.StoredProcedure;
            count = comm.ExecuteNonQuery();
            if (count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "UNABLE TO ACTIVATE USER" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string DeactivateAdminUser(string userid)
    {
        SqlConnection conn;
        SqlCommand comm;
        Email email = new Email();

        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("DeactivateAdminUser", conn);
            comm.Parameters.AddWithValue("@userid", userid);

            comm.CommandType = CommandType.StoredProcedure;
            count = comm.ExecuteNonQuery();
            if (count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "UNABLE TO UPDATE PASSWORD" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string AddAdminRole(string rolename)
    {
        SqlConnection conn;
        SqlCommand comm;
        Email email = new Email();

        int count = 0;

        try
        {
            xmlstring = "<Response>";
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("AddAdminRole", conn);
            comm.Parameters.AddWithValue("@rolename", rolename);

            comm.CommandType = CommandType.StoredProcedure;
            count = comm.ExecuteNonQuery();
            if (count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "UNABLE TO ADD ROLE" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string GetZoneAcct(int branch_code, int appid)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetZoneAcct", conn);
            comm.CommandType = CommandType.StoredProcedure;
            comm.Parameters.AddWithValue("@branch_code", branch_code);
            comm.Parameters.AddWithValue("@app_id", appid);

            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "ZONE_ACCOUNT";
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    //GET TILL ACCOUNT
                    xmlstring = xmlstring + "<ZONE_ACCOUNT_NO>" + dr["zone_account"] + "</ZONE_ACCOUNT_NO>";
                }

            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "0/0/0/0/0" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
            //----------------------------------------------------------------
            //Create XML string
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string GetBranchAcct(string user_id, int appid)
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetBranchAcct", conn);
            comm.CommandType = CommandType.StoredProcedure;
            comm.Parameters.AddWithValue("@user_id", user_id);
            comm.Parameters.AddWithValue("@app_id", appid);

            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "BRANCH_ACCOUNT";
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    xmlstring = xmlstring + "<BRANCH_ACCOUNT_NO>" + dr["branch_till_acct"] + "</BRANCH_ACCOUNT_NO>";
                }
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "0/0/0/0/0" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string GetAllAdminUsers()
    {
        SqlConnection conn;
        SqlCommand comm;
        DataSet ds;
        SqlDataAdapter adpt;
        DataRow dr_app;
        string xmlrep = null;
        string xmlschema = null;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneConnString2"].ToString());

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            comm = new SqlCommand("GetAllAdminUsers", conn);
            comm.CommandType = CommandType.StoredProcedure;
            adpt = new SqlDataAdapter(comm);
            ds = new DataSet();

            adpt.Fill(ds);
            ds.DataSetName = "RESPONSE";

            xmlrep = ds.GetXml();
            xmlschema = ds.GetXmlSchema();
            xmlstring = "<Response>";

            if (ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].TableName = "USERS";
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<USERS>";
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    //GET USER
                    xmlstring = xmlstring + "<USER>";
                    xmlstring = xmlstring + "<USER_ID>" + dr["user_id"] + "</USER_ID>";
                    xmlstring = xmlstring + "<STAFF_ID>" + dr["staff_id"] + "</STAFF_ID>";
                    xmlstring = xmlstring + "<USER_NAME>" + dr["user_name"] + "</USER_NAME>";
                    xmlstring = xmlstring + "</USER>";
                }
                xmlstring = xmlstring + "</USERS>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "NO USERS" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string UpdateSalaryAdvanceStatus(string uuid, string caseid, string status, int delindex)
    {
        SqlConnection conn;
        SqlCommand comm;

        try
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["e_oneIbank"].ToString());

            string id = caseid + "-" + uuid;
            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            xmlstring = xmlstring + "<Response>";
            comm = new SqlCommand("usp_IbankUpdateSADProcessMaker", conn);
            comm.Parameters.AddWithValue("@CaseID", id);
            comm.Parameters.AddWithValue("@Status", status);
            comm.Parameters.AddWithValue("@DelIndex", delindex);


            comm.CommandType = CommandType.StoredProcedure;

            int i = comm.ExecuteNonQuery();

            if (i > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCESS</MESSAGE>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "COULD NOT UPDATE REQUEST" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            xmlstring = "<Response>";
            xmlstring = xmlstring + "<CODE>1001</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string ConvertPaddedToOldAcct(string paddedacct)
    {
        string bracode = Convert.ToInt32(paddedacct.Substring(0, 4)).ToString();
        string cusnum = Convert.ToInt32(paddedacct.Substring(4, 7)).ToString();
        string cur = Convert.ToInt32(paddedacct.Substring(11, 3)).ToString();
        string led = Convert.ToInt32(paddedacct.Substring(14, 4)).ToString();
        string subacct = Convert.ToInt32(paddedacct.Substring(18, 3)).ToString();
        return bracode + "/" + cusnum + "/" + cur + "/" + led + "/" + subacct;
    }

    internal DataTable getAllLinkedAccountGENSDB(string mobileNum)
    {
        DataTable result = new DataTable();

        try
        {
            using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
            {
                using (OracleCommand OraSelect = new OracleCommand())
                {

                    if (OraConn.State == ConnectionState.Closed)
                    {
                        OraConn.Open();
                    }

                    OraSelect.Connection = OraConn;

                    string selectquery = "select bra_code,cus_num,cur_code, led_code,sub_acct_code, availbal avail_bal,PRIMARY_MOBILENO,SEQ,ACCT_NAT from ussd_account_vw where primary_mobileno = :PHONENUMBER ";

                    OraSelect.CommandText = selectquery;
                    OraSelect.CommandType = CommandType.Text;
                    OraSelect.Parameters.Add(":PHONENUMBER", mobileNum);

                    using (OracleDataAdapter adpt = new OracleDataAdapter(OraSelect))
                    {
                        adpt.Fill(result);
                        adpt.Dispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Error Occured getting all linked accounts from Basis. Message - " + ex.Message + "|StackTrace - " + ex.StackTrace);
        }
    
         result.TableName = "AllAccounts";
         return result;

    }
}