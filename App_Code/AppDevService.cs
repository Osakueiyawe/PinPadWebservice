using System;
using System.Web.Services;
using System.Data;
using GTBSecure;
using System.Xml;
using System.Xml.XPath;
using System.Text;
using System.Configuration;
using System.Data.OracleClient;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class AppDevService : WebService
{
    public AppDevService() {

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    //  [WebMethod]
    public String ValidateBeneficiaryAccount(String userid, String benacctno, String type)
    {
        Utility.Log().Info(userid + benacctno + type);
        String ret_val = null;
        Eone eone = new Eone();
        if (type == "PRE")
            ret_val = eone.ValidateBeneficiaryAcct(userid, benacctno);
        else if (type == "ANY")
            ret_val = eone.ValidateAccountNuber(userid, benacctno);
        else
            ret_val = eone.ValidateAccountNuber(userid, benacctno);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public String checkrestriction(String accountno)
    {

        Utility.Log().Info("Account Number: "+ accountno);
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.checkrestriction(accountno);
        Utility.Log().Info("Response from check on Basis: " + ret_val);
        return ret_val;
    }

    [WebMethod]
    public String checkrestrictionBraCus(string branchCode, string customerNo)
    {
        Utility.Log().Info(branchCode + customerNo);
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.checkrestriction(branchCode, customerNo);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    // [WebMethod]
    public String ValidateAdminUser(String userid, String password, int appid)
    {
        Utility.Log().Info(userid + appid);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ValidateAdminUsr(userid, password, appid);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public DataTable GetAccountdetailsfromPhone(string phoneNumber)
    {
        var stringlength = phoneNumber.Length;
        var strBuilder = new StringBuilder();
        DataTable dt = new DataTable("Accounts");
        dt.Columns.Add("Select");
        dt.Columns.Add("TransType");
        dt.Columns.Add("RequestDate");

        try
        {
            var phonequery = "select PRIMARY_MOBILENO AS \"MobileNumber\", BRA_CODE AS \"BranchCode\", CUS_NUM AS \"CustomerNo\",CUR_CODE AS \"CurCode\", LED_CODE AS \"LedCode\", SUB_ACCT_CODE AS \"SubAcctCode\",AVAILBAL AS \"Account Balance\",CUSNAME AS CustomerFullName,NUBAN AS \"BenAcctNo\" from midwareusr.mobile_account_vw where PRIMARY_MOBILENO = :phoneNumber";
            var accountquery = "select a.mob_num AS \"MobileNumber\", a.bra_code AS \"BranchCode\", a.cus_num AS \"CustomerNo\", b.map_acc_no AS \"BenAcctNo\" from midwareusr.address_ussd a, banksys.map_acct b where a.bra_code = b.bra_code and a.cus_num = b.cus_num and b.map_acc_no = :map_acc_no";
            if (stringlength == 10) //Account Number
            {
                OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BasisCon"]);
                OracleCommand oracomm = new OracleCommand();
                OraConn.Open();
                oracomm.Connection = OraConn;
                oracomm.CommandType = CommandType.Text;
                oracomm.CommandText = accountquery;
                oracomm.Parameters.Add(":map_acc_no", phoneNumber);
                OracleDataReader oradrread = oracomm.ExecuteReader(CommandBehavior.CloseConnection);
                dt.Load(oradrread);
            }
            else if (stringlength == 11) //Phone Number
            {
                OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
                OracleCommand oracomm = new OracleCommand();
                OraConn.Open();
                oracomm.Connection = OraConn;
                oracomm.CommandType = CommandType.Text;
                oracomm.CommandText = phonequery;
                oracomm.Parameters.Add(":phoneNumber", phoneNumber);
                OracleDataReader oradrread = oracomm.ExecuteReader(CommandBehavior.CloseConnection);
                dt.Load(oradrread);
                //dt.Columns.Add("CustomerFullName");    
                dt.Columns.Add("OldAccountNo");
                dt.Columns.Add("AccountType");
                var customername = new BASIS();

                foreach (DataRow dr in dt.Rows)
                {
                    //dr["CustomerFullName"] = customername.GetCustomerName(dr["BranchCode"].ToString(), dr["CustomerNo"].ToString());
                    dr["OldAccountNo"] = dr["BranchCode"].ToString() + "/" + dr["CustomerNo"].ToString() + "/" + dr["CurCode"].ToString() + "/" + dr["LedCode"].ToString() + "/" + dr["SubAcctCode"].ToString();
                   
                    if (dr["LedCode"].ToString() == "59")
                    {
                        dr["AccountType"] = "Savings";
                    }
                    else if (dr["LedCode"].ToString() == "1")
                    {
                        dr["AccountType"] = "Current";
                    }
                }                
                dt.Columns.Remove("CurCode");
                dt.Columns.Remove("SubAcctCode");
                dt.Columns.Remove("LedCode");
            }


            foreach (DataRow dr in dt.Rows)
            {
                dr["Select"] = "Select";
                dr["TransType"] = "DEPOSIT";
                dr["RequestDate"] = DateTime.Now.ToString();
            }

        }
        catch (Exception ex)
        {
            strBuilder.AppendLine(ex.Message);
        }
        return dt;
    }
    [WebMethod]
    public String Encrypt(String EncryptString)
    {
        return Secure.EncryptString(EncryptString);
    }

    [WebMethod]
    public String ValidateEncryptedAdminUser(String userid, String password, String code, int appid)
    {
        Utility.Log().Info(userid+appid);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ValidateAdminUsr(this.DecryptData(userid,code), this.DecryptData(password,code), appid);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public string ValidateAdminUserOffSite(string userid, string password, int appid)
    {
        Utility.Log().Info(userid+appid);
        String ret_val = null;
        Eone eone = new Eone();

        ret_val = eone.ValidateAdminUsrOffSite(userid, password, appid);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public string ValidateAdminUserOffSitewithAppver(string userid, string newpassword, int appid, string appversion,string tokenvalue)
    {
        string ret_val = null;
        var strBuilder = new StringBuilder();

        try
        {
            Eone eone = new Eone();
            Token tk = new Token();

            Utility.Log().Info(userid + " : "+ appid + " : " + appversion);
            ErrHandler.WriteError(userid + " : " + appid + " : " + appversion, "ValidateAdminUserOffSitewithAppver");

            strBuilder.AppendLine("Entered the method to login. Receieved parameters => userid: " + userid + "|newpassword: " + newpassword + "|appid: " + appid + "|appversion: " + appversion + "|tokenValue: " + tokenvalue);
            ErrHandler.WriteError("Entered the method to login. Receieved parameters => userid: " + userid + "|newpassword: " + newpassword + "|appid: " + appid + "|appversion: " + appversion + "|tokenValue: ", "ValidateAdminUserOffSitewithAppver");

            string password = Secure.DecryptString(newpassword);

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["BypassTokenValidation"]))
            {
                ErrHandler.WriteError("Bypass token validation set to true", "ValidateAdminUserOffSitewithAppver");
                strBuilder.AppendLine("Bypass token validation set to true");
                ret_val = "<Response><CODE>1000</CODE>Token Validation Successful<MESSAGE>SUCCESS</MESSAGE></Response>";
            }
            else
            {
                ErrHandler.WriteError("Bypass token validation set to false", "ValidateAdminUserOffSitewithAppver");
                strBuilder.AppendLine("Bypass token validation set to false");
                ret_val = tk.ValidateToken(userid, tokenvalue, "ADMIN", "PinpadLogin", "PINPAD");
                
            }
            strBuilder.AppendLine("Response from token validation - " + ret_val);
            ErrHandler.WriteError("Response from token validation - " + ret_val, "ValidateAdminUserOffSitewithAppver");


            XmlDocument document = null;
            XPathNavigator navigator = null;
            XPathNodeIterator snodes = null;
            string retcode = null;
            document = new XmlDocument();
            document.LoadXml(ret_val);
            navigator = document.CreateNavigator();
            snodes = navigator.Select("/Response/CODE");
            snodes.MoveNext();

            retcode = snodes.Current.Value;
            //retcode = "1000";
            if (retcode != "1000")
            {
                snodes = navigator.Select("/Response/ERROR");
                snodes.MoveNext();
            }
            else
            {
                ret_val = eone.ValidateAdminUserOffSitewithAppver(userid, password, appid, appversion);
                strBuilder.AppendLine("Response from validate admin user - " + ret_val);
                ErrHandler.WriteError("Response from validate admin user  - " + ret_val, "ValidateAdminUserOffSitewithAppver");
            }

            Utility.Log().Info(ret_val);
            Utility.Log().Info(strBuilder.ToString());
        }
        catch(Exception ex)
        {
            strBuilder.AppendLine("An exception occurred in ValidateAdminUserOffSitewithAppver(outer method). Message - " + ex.Message + "|Stacktrace - " + ex.StackTrace);
            ret_val= string.Empty;
        }

        ErrHandler.WriteError("About to validate token.", "ValidateAdminUserOffSitewithAppver");
        return ret_val;
    }

    [WebMethod]
    public String ValidateUser(String userid, String password)
    {
        Utility.Log().Info(userid);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ValidateUser(userid, password);
        Utility.Log().Info(ret_val);
        return ret_val;
    }


    //[WebMethod]
    public String GetAccountBalance(String bracode, String cusnum, String curcode, String ledcode, String subacctcode)
    {
        Utility.Log().Info(bracode+cusnum+curcode+ledcode+subacctcode);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetAccountBalance(Convert.ToInt32(bracode), Convert.ToInt32(cusnum), Convert.ToInt32(curcode), Convert.ToInt32(ledcode), Convert.ToInt32(subacctcode));
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public String GetCustomerName(String bracode, String cusnum)
    {
        Utility.Log().Info(bracode+cusnum);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetCustomerName(Convert.ToInt32(bracode), Convert.ToInt32(cusnum));
        Utility.Log().Info(ret_val);

        return ret_val;
    }

  //  [WebMethod]
    public String GetLastTransactionDetails(int bracode, int cusnum, int curcode, int ledcode, int subacctcode)
    {
        Utility.Log().Info(bracode + cusnum + curcode + ledcode + subacctcode);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetLastTransactionDetails(bracode, cusnum, curcode, ledcode, subacctcode);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

  //  [WebMethod]
    public String GetLastAccountStatement(int bracode, int cusnum, int curcode, int ledcode, int subacctcode, String custemail)
    {
        Utility.Log().Info(bracode + cusnum + curcode + ledcode + subacctcode+custemail);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetLastAccountStatement(bracode, cusnum, curcode, ledcode, subacctcode, custemail);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

 //   [WebMethod]
    public String RequestChequeBook(int bracode, int cusnum, int curcode, int ledcode, int subacctcode)
    {
        String ret_val = null;
        //Eone eone = new Eone();
        //ret_val = eone.ValidateUser(userid, password);

        return ret_val;
    }

  //  [WebMethod]
    public String StopCheque(int bracode, int cusnum, int curcode, int ledcode, int subacctcode, int start, int stop, String amount)
    {
        Utility.Log().Info(bracode + cusnum + curcode + ledcode + subacctcode + start+stop+amount);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.StopCheque(bracode, cusnum, curcode, ledcode, subacctcode, start, stop, amount);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

 //   [WebMethod]
    public String ConfirmCheque(int bracode, int cusnum, int curcode, int ledcode, int subacctcode)
    {
        String ret_val = null;
        //Eone eone = new Eone();
        //ret_val = eone.ValidateUser(userid, password);

        return ret_val;
    }

  //  [WebMethod]
    public String TransferFund(String Acct_fro, String Acct_to, Double Amount, String type, String channel, String Remarks)
    {
        Utility.Log().Info(string.Concat(Acct_fro," ",Acct_to," ",Amount," ",type," ",channel," ",Remarks));
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.TransferFunds(Acct_fro, Acct_to, Amount, type, channel, Remarks);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    //[WebMethod]
    //public String Transfer(String Acct_fro, String Acct_to, Double Amount, int expl_code, String Remarks, String TellerRole, String transtype, String serviceId)
    //{
    //    string password = Secure.DecryptString(EncryptedUserPin);
    //    String ret_val = null;
    //    Eone eone = new Eone();
    //    ret_val = eone.Transfer(Acct_fro, Acct_to, Amount, expl_code, Remarks, TellerRole, transtype, serviceId);

    //    return ret_val;
    //}

    //[WebMethod]
    //public String Transfer(String parameter)//Acct_fro, String Acct_to, Double Amount, int expl_code, String Remarks, String TellerRole, String transtype, String serviceId)
    //{
    //    Utility.Log().Info(parameter);
    //    String ret_val = string.Empty;
    //    try
    //    {
    //        string values = Secure.DecryptString(parameter);
    //       string[] valuesarray = values.Split('|');
    //       string Acct_fro = valuesarray[0].ToString();
    //       string Acct_to = valuesarray[1].ToString();
    //       double Amount = Convert.ToDouble(valuesarray[2]);
    //       int expl_code = Convert.ToInt16(valuesarray[3]);
    //       string Remarks = valuesarray[4].ToString();
    //       string TellerRole = valuesarray[5].ToString();
    //       string transtype = valuesarray[6].ToString();
    //       string serviceId = valuesarray[7].ToString();
    //       string tellerID = valuesarray[9].ToString();

    //        Eone eone = new Eone();
    //        ret_val = eone.Transfer(Acct_fro, Acct_to, Amount, expl_code, Remarks, TellerRole, transtype, serviceId, tellerID);
    //        Utility.Log().Info(ret_val);
    //        return ret_val;
    //    }
    //    catch (Exception ex)

    //    {
    //        Utility.Log().Fatal("Webservice Call Failed ", ex);
    //        return ret_val;

    //    }
    //}
    //[WebMethod]
    //public string Transfer_Test(string Acct_fro, string Acct_to, Double Amount, int expl_code,
    //   string Remarks, string TellerRole, string transtype, string serviceId)//Acct_fro, String Acct_to, Double Amount, int expl_code, String Remarks, String TellerRole, String transtype, String serviceId)
    //{
    //    string ret_val = string.Empty;

    //    try
    //    {

    //        Eone eone = new Eone();
    //        ret_val = eone.Transfer(Acct_fro, Acct_to, Amount, expl_code, Remarks, TellerRole, transtype, serviceId);

    //        return ret_val;
    //    }

    //    catch (Exception ex)
    //    {
    //        return ret_val;
    //    }
    //}

    [WebMethod]
    public string Transfer(string parameter)//Acct_fro, String Acct_to, Double Amount, int expl_code, String Remarks, String TellerRole, String transtype, String serviceId)
    {
        string ret_val = string.Empty;

        try
        {
            //string values = "205/140893/1/1/0|214/8412360/1/59/0|1000.00|640|test|2|Deposit|1"
            string values = Secure.DecryptString(parameter);
            string[] valuesarray = values.Split('|');
            string Acct_fro = valuesarray[0].ToString();
            string Acct_to = valuesarray[1].ToString();
            double Amount = Convert.ToDouble(valuesarray[2]);
            int expl_code = Convert.ToInt16(valuesarray[3]);
            string Remarks = valuesarray[4].ToString();
            string TellerRole = valuesarray[5].ToString();
            string transtype = valuesarray[6].ToString();
            string serviceId = valuesarray[7].ToString();
            Eone eone = new Eone();
            ret_val = eone.Transfer(Acct_fro, Acct_to, Amount, expl_code, Remarks, TellerRole, transtype, serviceId);

            return ret_val;
        }

        catch (Exception ex)
        {
            Utility.Log().Fatal("Transfer ", ex);
            return ret_val;
        }
    }

    //[WebMethod]
    //public String TransferChargesandVAT(String Acct_fro, String Acct_to, String VATAcct_to, Double Amount, int expl_code, String Remarks)
    //{
    //    String ret_val = null;

    //    Eone eone = new Eone();
    //    ret_val = eone.TransferChargesInternal(Acct_fro, Acct_to, VATAcct_to, Amount, expl_code, Remarks);

    //    return ret_val;
    //}

    [WebMethod]
    public String TransferChargesandVAT(string parameter)
    {
        Utility.Log().Info(parameter);
        String ret_val = string.Empty;

        try
        {
            string values = Secure.DecryptString(parameter);
            string[] valuesarray = values.Split('|');
            string Acct_fro = valuesarray[0].ToString();
            string Acct_to = valuesarray[1].ToString();
            string VATAcct_to = valuesarray[2].ToString();
            double Amount = Convert.ToDouble(valuesarray[3]);
            int expl_code = Convert.ToInt16(valuesarray[4]);
            string Remarks = valuesarray[5].ToString();
            string TellerId = "9011";
            Eone eone = new Eone();
            ret_val = eone.TransferChargesInternal(Acct_fro, Acct_to, VATAcct_to, Amount, expl_code, Remarks,TellerId);
            Utility.Log().Info(ret_val);
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("TransferChargesandVAT ", ex);
        
        
        }
        return ret_val;
    }
	
//[WebMethod]
    public String TransferTraSeq(String Acct_fro, String Acct_to, Double Amount, int expl_code, String Remarks)
    {
        Utility.Log().Info(string.Concat(Acct_fro, " ", Acct_to, " ", Amount, " ", expl_code, " ", Remarks));
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.TransferTraSeq(Acct_fro, Acct_to, Amount, expl_code, Remarks);
        Utility.Log().Info(ret_val);
        return ret_val;
    }
//[WebMethod]
//public String TransferCharges(String Acct_fro, String Acct_to, String VATAcct_to, Double Amount, int expl_code, String Remarks)
//{

//    String ret_val = null;

//    try
//    {
//        Eone eone = new Eone();
//        ret_val = eone.TransferCharges(Acct_fro, Acct_to, VATAcct_to, Amount, expl_code, Remarks);
//    }
//    catch (Exception ex)
//    {



//    }
//    return ret_val;
//}

    [WebMethod]
    public String TransferCharges(String parameter)
    {
        Utility.Log().Info(parameter);
        String ret_val = null;

        try
        {
            string values = Secure.DecryptString(parameter);
            string[] valuesarray = values.Split('|');
            string Acct_fro = valuesarray[0].ToString();
            string Acct_to = valuesarray[1].ToString();
            string VATAcct_to = valuesarray[2].ToString();
            double Amount = Convert.ToDouble(valuesarray[3]);
            int expl_code = Convert.ToInt16(valuesarray[4]);
            string Remarks = valuesarray[5].ToString();
            Eone eone = new Eone();
            
            ret_val = eone.TransferCharges(Acct_fro, Acct_to, VATAcct_to, Amount, expl_code, Remarks);
            Utility.Log().Info(ret_val);
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
        }
        return ret_val;
    }

   // [WebMethod]
    //public String TransferFund_Cross(String Acct_fro, String Acct_to, Double Amount, Double rate, Double crossrate, String type, String channel, String Remarks)
    //{
    //    String ret_val = null;
    //    Eone eone = new Eone();
    //    ret_val = eone.TransferFunds_Cross(Acct_fro, Acct_to, Amount, rate, crossrate, type, channel, Remarks);

    //    return ret_val;
    //}

  //  [WebMethod]
    //public String TransferCheques(String Acct_fro, String Acct_to, Double Amount, String type, String channel, String Remarks, String docnum, Int16 identifier, Int16 bankcode, Int16 days)
    //{
    //    String ret_val = null;
    //    Eone eone = new Eone();
    //    ret_val = eone.TransferCheques(Acct_fro, Acct_to, Amount, type, channel, Remarks, docnum, identifier, bankcode, days);

    //    return ret_val;
    //}

    [WebMethod]
    public String SendSMS(String userid, String phoneno)
    {
        Utility.Log().Info(userid+" "+phoneno);
        String ret_val = null;
        //Eone eone = new Eone();
        //ret_val = eone.ValidateUser(userid, password);

        return ret_val;
    }

    [WebMethod]
    public String SendEmail(String subject, String mailmessage, String emailfrom, String emailto, String attachment)
    {
        String ret_val = null;
        //Eone eone = new Eone();
        //ret_val = eone.ValidateUser(userid, password);

        return ret_val;
    }

  //  [WebMethod]
    public String ResetUserPasscode(String userid)
    {
        Utility.Log().Info("NOPASS");
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ResetUserPassword(userid);
        Utility.Log().Info("NOPASS");

        return ret_val;
    }

   // [WebMethod]
    public String ChangeUserPasscode(String userid, String pass)
    {
        Utility.Log().Info(userid+"NOPASS");
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ChangeUserPassword(userid, pass);
        Utility.Log().Info("NOPASS");

        return ret_val;
    }

   // [WebMethod]
    public String ChangeAdminUserPasscode(String userid, String pswd)
    {
        Utility.Log().Info(userid + "NOPASS");
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ChangeAdminUserPasscode(userid, pswd);
        Utility.Log().Info("NOPASS");

        return ret_val;
    }

    [WebMethod]
    public String GetApplicationRoleUsers(int roleid, int appid)
    {
        Utility.Log().Info(roleid + appid);
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetRoleUsers(roleid, appid);
        Utility.Log().Info(ret_val);

        return ret_val;
    }

  //  [WebMethod]
    public String GetAdminUserName(String uid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetAdminUserName(uid);

        return ret_val;
    }
    
    [WebMethod]
    public String GetCustomerDetails(String userid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetCustomerDetails(userid);

        return ret_val;
    }

    [WebMethod]
    public String ValidateAccount(String userid, String acctno)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ValidateAccountNuber(userid, acctno);

        return ret_val;
    }

    [WebMethod]
    public String CheckUserFlag(String uid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.CheckUserFlag(uid);

        return ret_val;
    }

    [WebMethod]
    public String ResetUserFlag(String uid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ResetUserFlag(uid);

        return ret_val;
    }

  //  [WebMethod]
    public String EncryptData(String datavalue, String key)
    {
        String ret_val = null;
        Encryption enc = new Encryption();
        ret_val = enc.EncryptPIN(datavalue, key);

        return ret_val;
    }

   // [WebMethod]
    public String DecryptData(String datavalue, String key)
    {
        String ret_val = null;
        Encryption enc = new Encryption();
        ret_val = enc.DecryptPIN(datavalue, key);

        return ret_val;
    }

//[WebMethod]
    public String TransferCTI(String IVRINFO, String CHANID)
    {
        String ret_val = null;
        IVR ivr = new IVR();
        ret_val = ivr.SendCTIMessage(IVRINFO, CHANID);

        return ret_val;
    }

   // [WebMethod]
    public String StartCTI(String CHANID)
    {
        String ret_val = null;
        IVR ivr = new IVR();
        ret_val = ivr.SendCTIStartMessage(CHANID);

        return ret_val;
    }

 //   [WebMethod]
    public String LogAdminUserAction(int AppID, String userid, long staffid, String Action)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.LogUserAction(AppID, userid, staffid, Action);

        return ret_val;
    }

  //  [WebMethod]
    public String AddNewAdminUser(String UserID, int BASISID, String Username, String staffid, String password, int branch_code, String emailaddress, String roleid, String status)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.AddNewAdminUser(UserID, BASISID, Username, staffid, password, branch_code, emailaddress, roleid, status);

        return ret_val;
    }

   // [WebMethod]
    public String UpdateAdminUser(String UserID, int BASISID, String Username, String staffid, int branch_code, String emailaddress, String roleid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.UpdateAdminUser(UserID, BASISID, Username, staffid, branch_code, emailaddress, roleid);

        return ret_val;
    }

   // [WebMethod]
    public String GetAdminUserDetails(String UserID)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetAdminUserDetails(UserID);

        return ret_val;
    }

  //  [WebMethod]
    public String SearchAdminUser(String str)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.SearchAdminUser(str);

        return ret_val;
    }

  //  [WebMethod]
    public String GetAllRoles()
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetAllRoles();

        return ret_val;
    }

  //  [WebMethod]
    public String GetAllAdminUsers()
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetAllAdminUsers();

        return ret_val;
    }

 //   [WebMethod]
    public String GetAllBranches()
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetAllBranches();

        return ret_val;
    }

  //  [WebMethod]
    public String ActivateAdminUser(String userid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.ActivateAdminUser(userid);

        return ret_val;
    }

 //   [WebMethod]
    public String DeactivateAdminUser(String userid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.DeactivateAdminUser(userid);

        return ret_val;
    }

  //  [WebMethod]
    public String GetTellerTillAcct(String userid, int appid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetTellerTillAcct(userid, appid);

        return ret_val;
    }

   // [WebMethod]
    public String GetZoneAcct(int branch_code, int appid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetZoneAcct(branch_code, appid);

        return ret_val;
    }

   // [WebMethod]
    public String GetBranchAcct(String userid, int appid)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.GetBranchAcct(userid, appid);

        return ret_val;
    }

 //   [WebMethod]
    public String GetAllBasisRoles()
    {
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.GetAllBasisRoles();

        return ret_val;
    }

    [WebMethod]
    public CustDetRetVal GetBasisCustomerDetails(int branch_code, int Customer_no)
    {
        //String ret_val = null;
        CustDetRetVal ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.GetBasisCustomerDetails(branch_code, Customer_no);

        return ret_val;
    }

    [WebMethod]
    public DataTable GetWithdrawaldetailsfromMobile(string phoneNumber)
    {
        var stringlength = phoneNumber.Length;
        var strBuilder = new StringBuilder();
        DataTable dt = new DataTable("Accounts");     

        try
        {            
            var phonequery = "select PRIMARY_MOBILENO AS \"MobileNumber\", BRA_CODE AS \"BranchCode\", CUS_NUM AS \"CustomerNo\",CUR_CODE AS \"CurCode\", LED_CODE AS \"LedCode\", SUB_ACCT_CODE AS \"SubAcctCode\",AVAILBAL AS \"Account Balance\",CUSNAME AS CustomerFullName,NUBAN AS \"BenAcctNo\" from midwareusr.mobile_account_vw2 where PRIMARY_MOBILENO = :phoneNumber";
            
            if (stringlength == 11) //Phone Number
            {
                OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
                OracleCommand oracomm = new OracleCommand();
                OraConn.Open();
                oracomm.Connection = OraConn;
                oracomm.CommandType = CommandType.Text;
                oracomm.CommandText = phonequery;
                oracomm.Parameters.Add(":phoneNumber", phoneNumber);
                OracleDataReader oradrread = oracomm.ExecuteReader(CommandBehavior.CloseConnection);
                dt.Load(oradrread);
                //dt.Columns.Add("CustomerFullName");    
                dt.Columns.Add("OldAccountNo");
                dt.Columns.Add("AccountType");
                var customername = new BASIS();

                foreach (DataRow dr in dt.Rows)
                {
                    //dr["CustomerFullName"] = customername.GetCustomerName(dr["BranchCode"].ToString(), dr["CustomerNo"].ToString());
                    dr["OldAccountNo"] = dr["BranchCode"].ToString() + "/" + dr["CustomerNo"].ToString() + "/" + dr["CurCode"].ToString() + "/" + dr["LedCode"].ToString() + "/" + dr["SubAcctCode"].ToString();

                    if (dr["LedCode"].ToString() == "59")
                    {
                        dr["AccountType"] = "Savings";
                    }
                    else if (dr["LedCode"].ToString() == "1")
                    {
                        dr["AccountType"] = "Current";
                    }
                }
                dt.Columns.Remove("CurCode");
                dt.Columns.Remove("SubAcctCode");
                dt.Columns.Remove("LedCode");
            }           

        }
        catch (Exception ex)
        {
            strBuilder.AppendLine(ex.Message);
        }
        return dt;
    }
    [WebMethod]
    public CustDetRetVal GetBasisCustomerDetailsFullKey(int branch_code, int Customer_no, int curcode, int ledcode, int subacctcode)
    {
        //String ret_val = null;
        CustDetRetVal ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.GetBasisCustomerDetails(branch_code, Customer_no,curcode,ledcode,subacctcode);

        return ret_val;
    }
    
    [WebMethod]
    public string GetBasisTellerTillAcct(string bracode, string tellerid, string curcode)
    {
        //String ret_val = null;
        string ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.GetBasisTellerTillAcct(bracode.Replace("'", ""), tellerid.Replace("'", ""), curcode.Replace("'", ""));

        return ret_val;
    }

   // [WebMethod]
    public string getTellerLimit(string tellerId, string bracode)
    {
        //String ret_val = null;
        string ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.getTellerLimit(tellerId, bracode);

        return ret_val;
    }


    [WebMethod]
    public string getTellerLimitPinPad()
    {
        //String ret_val = null;
        string ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.getTellerLimit();

        return ret_val;
    }

    [WebMethod]
    public string[] getPinPadValues()
    {
        //String ret_val = null;
        string[] ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.getPinPadValues();

        return ret_val;
    }
    [WebMethod]
    public DataTable getTransactionHistory(string bracode, string cusnum, string curcode, string ledcode, string subacctcode, string startdate, string enddate)
    {
        //String ret_val = null;
        DataTable ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.getTransactionHistory(bracode, cusnum, curcode, ledcode, subacctcode, startdate, enddate);

        return ret_val;
    }
    [WebMethod]
    public string getTranSeq(string origbracode, string accountno, string remark,string expl_code)
    {
        //String ret_val = null;
        string ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.getTranSeq(origbracode, accountno, remark,expl_code);

        return ret_val;
    }

    [WebMethod]
    public DataTable GetBranchTeller(string bracode)
    {
        //String ret_val = null;
        DataTable ret_val = new DataTable();
        BASIS basis = new BASIS();
        if (bracode.Length > 4)
            return ret_val;
        ret_val = basis.GetBranchTeller(bracode);

        return ret_val;
    }

    [WebMethod]
    public DataTable CheckForPendingTransTeller(string bra_code, string teller_id, int isopshead)
    {
        //String ret_val = null;
        DataTable ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.CheckForPendingTransTeller(bra_code,teller_id,isopshead);

        return ret_val;
    }

    [WebMethod]
    public string[] getcusDepositDetails(string nubanaccount)
    {
        string[] ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.getcusDepositDetails(nubanaccount);
        return ret_val;
    }

    [WebMethod]
    public string[] getcusDepositDetailsOldAccount(string OldAccountNumber)
    {
        //String ret_val = null;
        string[] ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.getcusDepositDetailsOldAccount(OldAccountNumber);

        return ret_val;
    }

    //[WebMethod]
    //public string[] getWithdrawalCharge()
    //{
    //    //String ret_val = null;
    //    string[] ret_val;
    //    BASIS basis = new BASIS();
    //    ret_val = basis.getWithdrawalCharge();

    //    return ret_val;
    //}

    [WebMethod]
    public string ConvertToNuban(string OldAccountNumber)
    {
        //String ret_val = null;
        string ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.ConvertToNuban(OldAccountNumber);

        return ret_val;
    }

    [WebMethod]
    public string ConvertToOldAccountNumber(string NubanAccountNumber)
    {
        //String ret_val = null;
        string ret_val;
        BASIS basis = new BASIS();
        ret_val = basis.ConvertToOldAccountNumber(NubanAccountNumber);

        return ret_val;
    }


    [WebMethod]
    public string[] getCustomerID(string param)
    {
        string values = Secure.DecryptString(param);
       // string values = "5399830403039652";
        string[] ret_val = null;
        Utilities util = new Utilities();
        ret_val = util.getCustomerID(values);
        return ret_val;
    }

   // [WebMethod]
    public String GetAllBasisUsers()
    {
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.GetAllBasisUsers();

        return ret_val;
    }

  //  [WebMethod]
    public String AddEoneRole(String role_desc)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.AddAdminRole(role_desc);

        return ret_val;
    }

  //  [WebMethod]
    public String AddBasisRole(String role_desc)
    {
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.AddBasisRole(role_desc);

        return ret_val;
    }

 //   [WebMethod]
    public String AddNewTeller(int BASISID, String Username, String password, int branch_code, String roleid, String UpdateFlag)
    {
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.AddTeller(BASISID, Username, password, branch_code, roleid, UpdateFlag);

        return ret_val;
    }

 //   [WebMethod]
    public String SearchBASISUser(String str, String branch_code)
    {
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.SearchBASISUser(str, branch_code);

        return ret_val;
    }


 //   [WebMethod]
    public String AnalyzeFx(String acctno, Decimal transfer_amt, DateTime anal_date)
    {
        String ret_val = null;
        BASIS basis = new BASIS();
        ret_val = basis.AnalyzeFx(acctno, transfer_amt, anal_date);

        return ret_val;
    }

     [WebMethod]
    public object[] getLogos()
    {
        object[] ret_val = null;
        Utilities util = new Utilities();
        ret_val = util.getLogos();
        return ret_val;
    }

     [WebMethod]
     public UInt64 InitiateTransaction(string OrigbraCode, string tellerId, string customerAccountNo, decimal amount, string authmode, string transType, string cusName, string tellertill, string depname, string CardNumber, uint Stan, string MerchantID, string AuthCode, decimal AuthAmount,string cashanalysis) // Online Transactions
     {
         UInt64 ret_val = 0;
         Transaction tranx = new Transaction();
         ret_val = tranx.InitiateTransaction(OrigbraCode, tellerId, customerAccountNo, amount, authmode, transType, cusName, tellertill, depname, CardNumber, Stan, MerchantID, AuthCode, AuthAmount, cashanalysis);// Online Transactions;
         return ret_val;
     }

    [WebMethod]
     public UInt64 InitiateTransactionOffline(string OrigbraCode, string tellerId, string customerAccountNo, decimal amount, string authmode, string transType, string cusName, string tellertill, string depname)// third party deposits Offline Transactions
     {
         UInt64 ret_val = 0;
         Transaction tranx = new Transaction();
         ret_val = tranx.InitiateTransaction(OrigbraCode, tellerId, customerAccountNo, amount, authmode, transType, cusName, tellertill, depname);
         return ret_val;
     }


    [WebMethod]
    public UInt64 UpdateTransaction(string apprvTeller, UInt64 transId, string bracode, string acctno, string rmks, string status, string failreason, string authmode, string tellertill, string expl_code)
    {
        UInt64 ret_val = 0;
        Transaction tranx = new Transaction();
        ret_val = tranx.UpdateTransaction( apprvTeller,  transId,  bracode,  acctno,  rmks,  status,  failreason,  authmode,  tellertill,  expl_code);
        return ret_val;
    }

    [WebMethod]
    public UInt64 UpdateTransactionForApproval(UInt64 transId)
    {
        UInt64 ret_val = 0;
        Transaction tranx = new Transaction();
        ret_val = tranx.UpdateTransactionForApproval(transId);
        return ret_val;
    }

    [WebMethod]
    public DataTable getOnlineDepositPendingData(string bra_code)
    {

        DataTable ret_val =null;
        Transaction tranx = new Transaction();
        ret_val = tranx.getOnlineDepositPendingData(bra_code);
        return ret_val;
    }

    [WebMethod]
    public DataTable getOnlineWithdrawalPendingData(string bra_code)
    {
        Utility.Log().Info(bra_code);
        DataTable ret_val = null;
        Transaction tranx = new Transaction();
        ret_val = tranx.getOnlineWithdrawalPendingData(bra_code);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public DataTable getOnlinePendingPrintingData(string teller_id)
    {
        Utility.Log().Info(teller_id);
        DataTable ret_val = null;
        Transaction tranx = new Transaction();
        ret_val = tranx.getOnlinePendingPrintingData(teller_id);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public DataTable getReceiptResetPendingData(string startdate, string acctno, string traamt)
    {
        DataTable ret_val = null;
        Transaction tranx = new Transaction();
        ret_val = tranx.getReceiptResetPendingData( startdate, acctno, traamt);
        return ret_val;
    }
    [WebMethod]
    public DataTable getPinPadTransactionHistory(string startdate, string enddate, string transtype, string bracode, string tellerid)
    {
        DataTable ret_val = null;
        Transaction tranx = new Transaction();
        ret_val = tranx.getPinPadTransactionHistory(startdate, enddate, transtype, bracode, tellerid);
        return ret_val;
    }

    [WebMethod]
    public UInt64  UpdateTransactionForReceiptReprint(UInt64 transId)
    {
        Utility.Log().Info(transId);
        UInt64 ret_val = 0;
        Transaction tranx = new Transaction();
        ret_val = tranx.UpdateTransactionForReceiptReprint(transId);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public bool UpdatePrintStatus(UInt64 transsequence, int printstatus)
    {
        Utility.Log().Info(transsequence+" "+printstatus);
        bool ret_val = false;
        Transaction tranx = new Transaction();
        ret_val = tranx.UpdatePrintStatus( transsequence,  printstatus);
        Utility.Log().Info(ret_val);
        return ret_val;
    }

    [WebMethod]
    public bool InsertIntoCentralDB(int TransactionID, string CustomerNo, string Transtype, string OriginatingTellerId, string AuthenticationMode, double TransAmount, string ApprovingTellerID, string OriginatingBraCode, string TransactionStatus, string BasisTransSequence, string CustomerName, DateTime TransDate, string TellerTillAccount, string FailReason, bool PrintStatus, string DepositorName)
    {
        bool ret_val = false;
        Transaction tranx = new Transaction();
        ret_val = tranx.InsertIntoCentralDB( TransactionID,  CustomerNo,  Transtype,  OriginatingTellerId,  AuthenticationMode,  TransAmount,  ApprovingTellerID,  OriginatingBraCode,  TransactionStatus,  BasisTransSequence,  CustomerName,  TransDate,  TellerTillAccount,  FailReason,  PrintStatus,  DepositorName);
        return ret_val;
    }


    [WebMethod]
    public bool IsInProgress(UInt64 transid)
    {
        bool ret_val = false;
        Transaction tranx = new Transaction();
        ret_val = tranx.IsInProgress(transid);
        return ret_val;
    }

//[WebMethod]
    public String UpdateSalaryAdvanceStatus(string uuid,string caseid,string status,int delindex)
    {
        String ret_val = null;
        Eone eone = new Eone();
        ret_val = eone.UpdateSalaryAdvanceStatus(uuid,caseid,status,delindex);

        return ret_val;
    }

    [WebMethod]
    public DataTable getOIOpportunities(string bra_code, string cus_num)
    {
        DataTable ret_val = null;
        Transaction tranx = new Transaction();
        ret_val = tranx.getOIOpportunities(bra_code, cus_num);
        return ret_val;
    }
    [WebMethod]
    public string UpdateOIOpportunities(string bra_code, string cus_num, string feedback, string tellerid)
    {
        string ret_val = string.Empty;
        Transaction tranx = new Transaction();
        ret_val = tranx.UpdateOIOpportunities(bra_code, cus_num, feedback, tellerid);
        return ret_val;
    }

    [WebMethod]
    public string UpdateOISelectionbyTeller(string bra_code, string cus_num, string tellerid)
    {
        string ret_val = string.Empty;
        Transaction tranx = new Transaction();
        ret_val = tranx.UpdateOISelectionbyTeller(bra_code, cus_num, tellerid);
        return ret_val;
    }
    [WebMethod]
    public DataTable FastTrackGetPending(string phoneNumber, string transType)
    {
        DataTable dt = null;
        Utility.Log().Info(phoneNumber+""+transType);
        USSDCashFastTrackClass fastTrack = new USSDCashFastTrackClass
        {
            mobileNumber = phoneNumber,
            transType = transType
        };
        dt = fastTrack.RetrieveTransByMobNum();

        return dt;
    }

    [WebMethod]
    public int FastTrackUpdateTeller(string id, string status, string PostTeller, long transid)
    {
        Utility.Log().Info(id + " " + status + " " + PostTeller+" "+transid);
        USSDCashFastTrackClass fastTrack = new USSDCashFastTrackClass();
        fastTrack.id = id;
        fastTrack.status = status;
        fastTrack.post_Teller = PostTeller;
        fastTrack.transId = transid;
        int resp = fastTrack.UpdatePostingTeller();
        Utility.Log().Info(resp);
        return resp;
    }
    [WebMethod]
    public int FastTrackUpdateSupervisor(string id, string status, string PostSupervisor)
    {
        Utility.Log().Info(id+" "+status+" "+PostSupervisor);
        USSDCashFastTrackClass fastTrack = new USSDCashFastTrackClass();
        fastTrack.id = id;
        fastTrack.status = status;
        fastTrack.post_Supervisor = PostSupervisor;

        int resp = fastTrack.UpdatePostingSupervisor();
        Utility.Log().Info(resp);

        return resp;
    }

    [WebMethod]
    public string GetNubanAccountWithFunds(string XmlRequest)
    {
        Utility.Log().Info("Request passed - " + XmlRequest);

        BASIS basis = new BASIS();

        var fundedNubanAccount = basis.GetTransactionDetails(XmlRequest);

        Utility.Log().Info("Response passed - " + fundedNubanAccount);

        return fundedNubanAccount;
    }


    [WebMethod]
    public FingerPrintVerificationResponse GetVerificationResponse(FingerPrintVerificationRequest request)
    {
        //Utility.Log().Info("Request received - " + request.ToString());

        FingerPrintImplementation verificationRequest = new FingerPrintImplementation();

        var response = verificationRequest.GetVerificationResponse(request);

        Utility.Log().Info("Response returned - " + response.ToString());

        return response;
    }

}
