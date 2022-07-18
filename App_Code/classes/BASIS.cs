using System;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
//using System.Data.OracleClient;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;
using System.Xml;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public class BASIS
{
    private string xmlstring = null;

    public BASIS()
    {

    }
   
    public DataTable getTransactionHistory(string bracode, string cusnum, string curcode, string ledcode, string subacctcode, string startdate, string enddate)
    {
        DataTable cheques = new DataTable();
        cheques.TableName = "TransactionHistory";
        OracleCommand oracomm = new OracleCommand();
        OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);

        try
        {
            string transact = "select t.tra_date,t.origt_tra_seq1,t.ORIGT_BRA_CODE,t.doc_num as reference,e.des_eng ||' '|| t.remarks as Narrative,t.val_date,decode(t.Deb_cre_ind,1,t.tra_amt) as Debit,decode(t.Deb_cre_ind,2,t.tra_amt) as Credit,b.Des_eng,t.expl_code from transact t,Text_tab e,branch b where t.ORIGT_BRA_CODE =b.bra_code and t.expl_code=e.tab_ent (+) and e.tab_id=60 and t.bra_code = " + bracode + " and t.cus_num = " + cusnum + " and t.cur_code = " + curcode + " and t.led_code = " + ledcode + " and t.sub_acct_code = " + subacctcode + " and t.CAN_REA_CODE = 0 AND t.tra_date >= '" + startdate + "' and t.tra_date <= '" + enddate + "' ";
            string tell_act = " union select t.tra_date,t.origt_tra_seq1,t.ORIGT_BRA_CODE,t.doc_num as reference,e.des_eng ||' '|| t.remarks as Narrative,t.val_date,decode(t.Deb_cre_ind,1,t.tra_amt) as Debit,decode(t.Deb_cre_ind,2,t.tra_amt) as Credit,b.Des_eng,t.expl_code from tell_Act t,Text_tab e,branch b where t.origt_bra_code=b.bra_code and t.expl_code=e.tab_ent (+) and e.tab_id=60 and t.bra_code = " + bracode + " and t.cus_num = " + cusnum + " and t.cur_code = " + curcode + " and t.led_code = " + ledcode + " and t.sub_acct_code = " + subacctcode + " and t.CAN_REA_CODE = 0 order by 1,2";
            oracomm.CommandType = CommandType.Text;
            oracomm.CommandText = transact + " " + tell_act;
            if (oraconn.State == ConnectionState.Closed)
            {
                oraconn.Open();
            }
            oracomm.Connection = oraconn;
            using (OracleDataAdapter da = new OracleDataAdapter(oracomm))
            {
                da.Fill(cheques);
            }

            return cheques;



        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return cheques;
            // LogHandler.WriteLog("Error Occurred: Aseesment status could not be Updated to Sent For confirmation (4) :: " + AssessmentID + "::" + ex.Message);
            // return false;
        }
        finally
        {
            oraconn.Close();

        }


       // return cheques;
    }

    public DataTable GetBranchTeller(string bracode)
    {
        DataTable cheques = new DataTable();
                                                                                      
        cheques.TableName = "BranchTeller";
        SqlCommand sqlcomm = new SqlCommand();
        SqlConnection sqlconn = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]);

        try
        {
            string query = "SELECT distinct OriginatingTellerId FROM [PinPad].[dbo].[Transactions] WHERE OriginatingBraCode = '" + bracode.Replace("'","") + "'";
            sqlcomm.CommandType = CommandType.Text;
            sqlcomm.CommandText = query;
            if (sqlconn.State == ConnectionState.Closed)
            {
                sqlconn.Open();
            }
            sqlcomm.Connection = sqlconn;
            using (SqlDataAdapter da = new SqlDataAdapter(sqlcomm))
            {
                da.Fill(cheques);
            }

            return cheques;

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return cheques;
        }
        finally
        {
            sqlconn.Close();

        }
    }

    public DataTable CheckForPendingTransTeller(string bra_code, string teller_id, int isopshead)
    {
        DataTable cheques = new DataTable();

        cheques.TableName = "PendingTeller";

        try
        {        
            using (SqlCommand sqlcomm = new SqlCommand())
            {
                using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
                {                  
                    sqlcomm.Connection = PinPadCon;
                    sqlcomm.Parameters.Add("@OriginatingBraCode", SqlDbType.NChar).Value = bra_code;
                    sqlcomm.Parameters.Add("@TellerId", SqlDbType.NChar).Value = teller_id;
                    sqlcomm.Parameters.Add("@isOpsHead", SqlDbType.Int).Value = isopshead;
                    sqlcomm.CommandText = "spGetAwaitingTransForApproval";
                    sqlcomm.CommandType = CommandType.StoredProcedure;
                    
                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }

                    sqlcomm.Connection = PinPadCon;
                    using (SqlDataAdapter da = new SqlDataAdapter(sqlcomm))
                    {
                        da.Fill(cheques);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            cheques = null;
        }

        return cheques;
    }

    public string getTellerLimit()
    {
        string withdrawalauthlimit = "";
        string depositauthlimit = "";
        string thirdcurwithdrawalauthlimit = "";
        string thirdcurdepositauthlimit = "";
        try
        {
            withdrawalauthlimit = ConfigurationManager.AppSettings["PinPadNairaWithdrawalAuthLimit"];
            depositauthlimit = ConfigurationManager.AppSettings["PinPadNairaDepositAuthLimit"];
            thirdcurwithdrawalauthlimit = ConfigurationManager.AppSettings["PinPadThirdCurWithdrawalAuthLimit"];
            thirdcurdepositauthlimit = ConfigurationManager.AppSettings["PinPadThirdCurDepositAuthLimit"];
            return withdrawalauthlimit + "," + depositauthlimit + "," + thirdcurwithdrawalauthlimit + "," + thirdcurdepositauthlimit;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return "0,0,0,0";
        }
    }

    public string getTellerLimit(string tellerId, string bracode)
    {
        {
            try
            {
                Utilities util = new Utilities();
                using (OracleCommand oracomm = new OracleCommand())
                {
                    using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
                    {
                        oracomm.Connection = OraConn;
                        oracomm.CommandText = "select aut_amt||','||aut_amt_cr aut_amt from teller where tell_id =" + util.SafeSqlLiteral( tellerId,1) + " and bra_code = " + util.SafeSqlLiteral(bracode,2);


                        oracomm.CommandType = CommandType.Text;
                        if (OraConn.State == ConnectionState.Closed)
                        {
                            OraConn.Open();
                        }
                        using (OracleDataReader dr = oracomm.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            if (dr.Read())
                            {
                                return dr["aut_amt"].ToString();
                            }
                            else
                            {
                                return "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                //ErrHandler.WriteError(ex.Message);
                return "";
            }

            finally
            {

            }

        }


    }

    public string GetBasisTellerTillAcct(string bracode, string tellerid, string curcode)
    {
        try
        {
            if (bracode.Length != 3)
                return "0/0/0/0";


            using (OracleCommand oracomm = new OracleCommand())
            {
                using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
                {
                    oracomm.Connection = OraConn;
                    oracomm.CommandText = "select distinct b.bra_code || '/' || b.cus_num || '/' || b.cur_code || '/' ||  b.led_code || '/' || a.cash_sub_acct acct_key from teller a, account b where a.tell_id = " + tellerid + " and cur_code = " + curcode + " and a.bra_code = " + bracode + "  and a.cus_num = b.cus_num and a.bra_code = b.bra_Code  and led_code = 31 and b.cus_num > 180 and b.cus_num < 201";//acceptable range is from 181 to 200



                    oracomm.CommandType = CommandType.Text;
                    if (OraConn.State == ConnectionState.Closed)
                    {
                        OraConn.Open();
                    }

                    using (OracleDataReader dr = oracomm.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (dr.Read())
                        {
                            //string a = "Posting Teller:" + dr["PostingTellerId"].ToString();
                            //a = a + ", Teller Bra Code " + dr["OriginatingBraCode"].ToString();
                            return dr["acct_key"].ToString();
                        }
                        else
                        {
                            return "0/0/0/0";
                        }
                    }
                }
            }


        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return "0/0/0/0";

        }

        finally { }
    }

    public string getTranSeq(string origbracode, string accountno, string remark, string expl_code)
    {
        // string payDate = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)).Date.ToString("ddMMMyyyy", CultureInfo.CreateSpecificCulture("en-GB"));
        string payDate = DateTime.Now.Date.ToString("ddMMMyyyy", CultureInfo.CreateSpecificCulture("en-GB"));
        {
            try
            {
                char acctsplit = Convert.ToChar("/");
                string[] accountkey = new string[4];// 
                accountkey = accountno.Split(acctsplit);
                string bra_code = null;
                string cus_num = null;
                string cur_code = null;
                string led_code = null;
                string sub_acct_code = null;

                bra_code = accountkey[0];// accountno.Substring(0, 3);
                cus_num = accountkey[1];// accountno.Substring(3, 6);
                cur_code = accountkey[2];// accountno.Substring(9, 1);
                led_code = accountkey[3];// accountno.Substring(10, 1);
                sub_acct_code = accountkey[4];//
                using (OracleCommand oracomm = new OracleCommand())
                {
                    using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
                    {
                        oracomm.Connection = OraConn;
                        oracomm.CommandText = "select origt_tra_seq1 from tell_act " +
                        "where bra_code = " + bra_code + "and cus_num = " + cus_num + " and cur_code = " + cur_code + " and led_code =" + led_code +
                        " and sub_acct_code =" + sub_acct_code + " and expl_code = " + expl_code + " and origt_bra_code = " + origbracode +" and remarks like '%" + remark + "%'";



                        oracomm.CommandType = CommandType.Text;
                        if (OraConn.State == ConnectionState.Closed)
                        {
                            OraConn.Open();
                        }

                        using (OracleDataReader dr = oracomm.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            if (dr.Read())
                            {
                                //string a = "Posting Teller:" + dr["PostingTellerId"].ToString();
                                //a = a + ", Teller Bra Code " + dr["OriginatingBraCode"].ToString();
                                return dr["origt_tra_seq1"].ToString();
                            }
                            else
                            {
                                return "0";
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Utility.Log().Fatal("Database Call Failed ", ex);
                return "ERROR"; }

            finally { }

        }


    }

    public string checkrestriction(string accountno)
    {

        {
            try
            {
                char acctsplit = Convert.ToChar("/");
                string[] accountkey = new string[4];// 
                accountkey = accountno.Split(acctsplit);
                string bra_code = null;
                string cus_num = null;
                string cur_code = null;
                string led_code = null;
                string sub_acct_code = null;

                bra_code = accountkey[0];// accountno.Substring(0, 3);
                cus_num = accountkey[1];// accountno.Substring(3, 6);
                cur_code = accountkey[2];// accountno.Substring(9, 1);
                led_code = accountkey[3];// accountno.Substring(10, 1);
                sub_acct_code = accountkey[4];//
                using (OracleCommand oracomm = new OracleCommand())
                {
                    using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
                    {
                        oracomm.Connection = OraConn;

                        oracomm.CommandText = "select distinct text_tab.tab_ent,acct_res.text_rest from text_tab inner join acct_res  on text_tab.tab_ent = acct_res.rest_code where text_tab.tab_id = 75 " +
                               "and acct_res.bra_code = " + bra_code + " and acct_res.cus_num = " + cus_num + " and acct_res.cur_code = " + cur_code + " and acct_res.led_code = " + led_code + " and acct_res.sub_acct_code = " + sub_acct_code;

                        oracomm.CommandType = CommandType.Text;
                        if (OraConn.State == ConnectionState.Closed)
                        {
                            OraConn.Open();
                        }
                        string text_tab = string.Empty;
                        using (OracleDataReader dr = oracomm.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            while (dr.Read())
                            {
                                if (dr["tab_ent"].ToString().Trim().Equals("0"))
                                {
                                    text_tab = text_tab + "," + dr["text_rest"].ToString();
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(text_tab))
                            {
                                if (text_tab.StartsWith(","))
                                {
                                    return text_tab.Substring(1);
                                }
                                else
                                {
                                    return text_tab;
                                }
                            }
                            else
                            {
                                return "0";
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Utility.Log().Fatal("Database Call Failed ", ex);
                return "UNABLE TO RETRIEVE"; }

            finally { }

        }


    }

    public string checkrestriction(string bracode, string customerno)
    {

        {
            try
            {
                char acctsplit = Convert.ToChar("/");
                //string[] accountkey = new string[4];// 
                //accountkey = accountno.Split(acctsplit);
                //string bra_code = null;
                //string cus_num = null;
                //string cur_code = null;
                //string led_code = null;
                //string sub_acct_code = null;

                //bra_code = accountkey[0];// accountno.Substring(0, 3);
                //cus_num = accountkey[1];// accountno.Substring(3, 6);
                //cur_code = accountkey[2];// accountno.Substring(9, 1);
                //led_code = accountkey[3];// accountno.Substring(10, 1);
                //sub_acct_code = accountkey[4];//
                string pinPadLedgers = ConfigurationManager.AppSettings["PinPadAllowedLedgers"].ToString();
                using (OracleCommand oracomm = new OracleCommand())
                {
                    using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
                    {
                        oracomm.Connection = OraConn;

                        oracomm.CommandText = "select distinct text_tab.tab_ent,acct_res.text_rest from text_tab inner join acct_res  on text_tab.tab_ent = acct_res.rest_code where text_tab.tab_id = 75 " +
                               "and acct_res.bra_code = " + bracode + " and acct_res.cus_num = " + customerno + " and acct_res.cur_code = 1 and acct_res.led_code in (" + pinPadLedgers + ") ";// and acct_res.sub_acct_code = " + sub_acct_code;

                        oracomm.CommandType = CommandType.Text;
                        if (OraConn.State == ConnectionState.Closed)
                        {
                            OraConn.Open();
                        }
                        string text_tab = string.Empty;
                        using (OracleDataReader dr = oracomm.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            while (dr.Read())
                            {
                                if (dr["tab_ent"].ToString().Trim().Equals("0"))
                                {
                                    text_tab = text_tab + "," + dr["text_rest"].ToString();
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(text_tab))
                            {
                                if (text_tab.StartsWith(","))
                                {
                                    return text_tab.Substring(1);
                                }
                                else
                                {
                                    return text_tab;
                                }
                            }
                            else
                            {
                                return "0";
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Utility.Log().Fatal("Database Call Failed ", ex);
                return "ERROR"; }

            finally { }

        }


    }

    public string[] getcusDepositDetailsOldAccount(string OldAccountNumber)
    {
        string bra_code = "";
        string cus_num = "";
        string cur_code = "";
        string led_code = "";
        string sub_acct_code = "";
        string[] acctdetails = OldAccountNumber.Split('/');
        string[] result = new string[6];
        //if (OldAccountNumber.Length == 12)    // Current Account
        //{
        //    bra_code = OldAccountNumber.Substring(0, 3);
        //    cus_num = OldAccountNumber.Substring(3, 6);
        //    cur_code = OldAccountNumber.Substring(9, 1);
        //    led_code = OldAccountNumber.Substring(10, 1);
        //    sub_acct_code = OldAccountNumber.Substring(11, 1);

        //}
        //else if (OldAccountNumber.Length == 13)    // savings Account
        //{
        //    bra_code = OldAccountNumber.Substring(0, 3);
        //    cus_num = OldAccountNumber.Substring(3, 6);
        //    cur_code = OldAccountNumber.Substring(9, 1);
        //    led_code = OldAccountNumber.Substring(10, 2);
        //    sub_acct_code = OldAccountNumber.Substring(12, 1);

        //}
        //else
        //{
        //    result[0] = "";
        //    result[1] = "";
        //    return result;

        //}


        bra_code = acctdetails[0].ToString();
        cus_num = acctdetails[1].ToString();
        cur_code = acctdetails[2].ToString();
        led_code = acctdetails[3].ToString();
        sub_acct_code = acctdetails[4].ToString();

        string param  = bra_code + "," + cus_num + "," + cur_code + "," + led_code + "," + sub_acct_code ;
        string Oraclecommand = "";

        using (OracleCommand oracom = new OracleCommand())
        {

            using (OracleConnection oracon = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
            {
                string Accountnumber = bra_code + "/" + cus_num + "/" + cur_code + "/" + led_code + "/" + sub_acct_code;
                oracom.Connection = oracon;
           //     Oraclecommand = "select '" + Accountnumber + "' as accountnum,get_name2(" + param + ") as cus_sho_name from address a  where a.bra_code = " + bra_code + " and a.cus_num = " + cus_num + " and a.cur_code = 0";



                Oraclecommand = " select a.bra_code, b.des_eng, d.cust_type,get_name1(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code) as cus_sho_name, "
                                + " navailbal(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code) as availbal,  "
                                + " a.bra_code ||'/'|| a.cus_num ||'/'|| a.cur_code ||'/'|| a.led_code ||'/'|| a.sub_acct_code as accountnumber "
                                + " from account a, branch b,  cust_pro d  where a.bra_code = b.bra_code  and a.bra_code = d.bra_code  and a.cus_num = d.cus_num "
                                + "  and a.bra_code = " + bra_code + " and a.cus_num = " + cus_num + " and a.cur_code = " + cur_code + " and a.led_code = " + led_code + " and a.sub_acct_code = " + sub_acct_code;



                oracom.CommandText = Oraclecommand;
                oracom.CommandType = CommandType.Text;
                try
                {
                    if (oracon.State == ConnectionState.Closed)
                    {
                        oracon.Open();
                    }

                    using (OracleDataReader oraread = oracom.ExecuteReader(CommandBehavior.CloseConnection))
                    {

                        result[0] = "";
                        result[1] = "";
                        result[2] = "";
                        result[3] = "";
                        result[4] = "";
                        result[5] = "";
                        oraread.Read();

                       
                        if (oraread["bra_code"] != System.DBNull.Value)
                        {
                            result[0] = (string)(oraread["bra_code"].ToString());
                           // result[0] = accountnumber;
                        }
                        if (oraread["des_eng"] != System.DBNull.Value)
                        {
                            result[1] = (string)(oraread["des_eng"].ToString());
                          //  result[1] = acctname;
                        }
                        if (oraread["cust_type"] != System.DBNull.Value)
                        {
                            result[2] = (string)(oraread["cust_type"].ToString());
                            //  result[1] = acctname;
                        }
                        if (oraread["cus_sho_name"] != System.DBNull.Value)
                        {
                            result[3] = (string)(oraread["cus_sho_name"].ToString());
                            //  result[1] = acctname;
                        }
                        if (oraread["availbal"] != System.DBNull.Value)
                        {
                            result[4] = (string)(oraread["availbal"].ToString());
                            //  result[1] = acctname;
                        }
                        if (oraread["accountnumber"] != System.DBNull.Value)
                        {
                            result[5] = (string)(oraread["accountnumber"].ToString());
                            //  result[1] = acctname;
                        }
                        

                        return result;

                    }

                }
                catch (Exception ex)
                {
                    Utility.Log().Fatal("Database Call Failed ", ex);
                    result[0] = "";
                    result[1] = "";
                    result[2] = "";
                    result[3] = "";
                    result[4] = "";
                    result[5] = "";
                    return result;
                }

                finally
                {

                    oracon.Close();
                }
            }


        }
    }

    public string[] getPinPadValues()  //getPinPadValues
    {
        string[] charges = new string[28];
        string charge = ConfigurationManager.AppSettings["WithdrawalCharges"];
        string chargelimit = ConfigurationManager.AppSettings["WithdrawalLimit"];
        string chargeaccount = ConfigurationManager.AppSettings["WithdrawalChargesAccount"];
        string DepositLimit = ConfigurationManager.AppSettings["DepositLimit"];
        string OpsHeadRoleId = ConfigurationManager.AppSettings["OpsHeadRoleId"];

        string TellerRoleId = ConfigurationManager.AppSettings["TellerRoleId"];
        string DepositITRoleId = ConfigurationManager.AppSettings["DepositITRoleId"];
        string WithdrwalExpl_Code = ConfigurationManager.AppSettings["WithdrwalExpl_Code"];
        string DepositExpl_Code = ConfigurationManager.AppSettings["DepositExpl_Code"];
        string AdminRoleId = ConfigurationManager.AppSettings["AdminRoleId"];
        string BasisIP = ConfigurationManager.AppSettings["BasisIP"];
        string BasisPort = ConfigurationManager.AppSettings["BasisPort"];

        string PinPadDepositLimit = ConfigurationManager.AppSettings["PinPadDepositLimit"];
        string PinPadWithdrawalLimit = ConfigurationManager.AppSettings["PinPadWithdrawalLimit"];
        string VATAccount = ConfigurationManager.AppSettings["WithdrawalChargesVATAccount"];
        string thirdcurrenyLimit = ConfigurationManager.AppSettings["PinPadThirdCurWithdrawalLimit"];

        string PinPadCon = ConfigurationManager.AppSettings["PinPadCon"];
        string gapsCon = ConfigurationManager.AppSettings["gapssortcode"];
        string BankCardCon = ConfigurationManager.AppSettings["BankCardCon"];
        string AppID = ConfigurationManager.AppSettings["ApplicationID"];
        string HostID = ConfigurationManager.AppSettings["HostType"];
        string AppCurrentVersion = ConfigurationManager.AppSettings["AppCurrentVersion"];

        string Level1TransactionLimit = ConfigurationManager.AppSettings["Level1TransactionLimit"];
        string Level2TransactionLimit = ConfigurationManager.AppSettings["Level2TransactionLimit"];
        string Level1BalanceLimit = ConfigurationManager.AppSettings["Level1BalanceLimit"];
        string Level2BalanceLimit = ConfigurationManager.AppSettings["Level2BalanceLimit"];
        string IsCardLessAllowed = ConfigurationManager.AppSettings["CardLessAllowed"];
        string TwigWebsrviceUrl = ConfigurationManager.AppSettings["TwigWebsrviceUrl"];

        charges[0] = charge;
        charges[1] = chargelimit;
        charges[2] = chargeaccount;
        charges[3] = DepositLimit;

        charges[4] = OpsHeadRoleId;
        charges[5] = TellerRoleId;
        charges[6] = DepositITRoleId;
        charges[7] = WithdrwalExpl_Code;
        charges[8] = DepositExpl_Code;
        charges[9] = AdminRoleId;
        charges[10] = BasisIP;
        charges[11] = BasisPort;
        charges[12] = PinPadDepositLimit;
        charges[13] = PinPadWithdrawalLimit;
        charges[14] = VATAccount;
        charges[15] = thirdcurrenyLimit;

        charges[16] = "";// Secure.EncryptString(PinPadCon);
        charges[17] = "";
        charges[18] = "";
        charges[19] = AppID;
        charges[20] = HostID;
        charges[21] = AppCurrentVersion;

        charges[22] = Level1TransactionLimit;
        charges[23] = Level2TransactionLimit;
        charges[24] = Level1BalanceLimit;
        charges[25] = Level2BalanceLimit;
        charges[26] = IsCardLessAllowed;
        charges[27] = TwigWebsrviceUrl;

        return charges;

    }
   
    public string[] getcusDepositDetails(string nubanaccount)
    {
        Utilities util = new Utilities();
        string Oraclecommand = "";
        string[] result = new string[6];
        using (OracleCommand oracom = new OracleCommand())
        {

            using (OracleConnection oracon = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
            {
                oracom.Connection = oracon;
               // Oraclecommand = "select a.bra_code||'/'||a.cus_num||'/'||a.cur_code||'/'||a.led_code||'/'||a.sub_acct_code as accountnum,b.cus_sho_name from map_acct a, address b  where a.bra_code = b.bra_code and a.cus_num = b.cus_num and b.cur_code = 0 and a.map_acc_no = '" + nubanaccount + "'";

                          Oraclecommand = "select a.bra_code,b.des_eng, d.cust_type,get_name1(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code) as cus_sho_name, " 
                             + " navailbal(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code) as availbal, "
                             + " c.bra_code ||'/'|| c.cus_num ||'/'|| c.cur_code ||'/'|| c.led_code ||'/'|| c.sub_acct_code as accountnumber "
                             + " from account a, branch b, map_acct c, cust_pro d  where a.bra_code = b.bra_code  and a.bra_code = c.bra_code and a.cus_num = c.cus_num "
                             + " and a.cur_code = c.cur_code and a.led_code = c.led_code and a.sub_acct_code = c.sub_acct_code and a.bra_code = d.bra_code "
                             + " and a.cus_num = d.cus_num and c.map_acc_no = '" + util.SafeSqlLiteral( nubanaccount,1) +"'";// 0004498914'


                oracom.CommandText = Oraclecommand;
                oracom.CommandType = CommandType.Text;
                try
                {
                    if (oracon.State == ConnectionState.Closed)
                    {
                        oracon.Open();
                    }

                    using (OracleDataReader oraread = oracom.ExecuteReader(CommandBehavior.CloseConnection))
                    {

                        result[0] = "";
                        result[1] = "";
                        result[2] = "";
                        result[3] = "";
                        result[4] = "";
                        result[5] = "";
                        oraread.Read();


                        if (oraread["bra_code"] != System.DBNull.Value)
                        {
                            result[0] = (string)(oraread["bra_code"].ToString());
                            // result[0] = accountnumber;
                        }
                        if (oraread["des_eng"] != System.DBNull.Value)
                        {
                            result[1] = (string)(oraread["des_eng"].ToString());
                            //  result[1] = acctname;
                        }
                        if (oraread["cust_type"] != System.DBNull.Value)
                        {
                            result[2] = (string)(oraread["cust_type"].ToString());
                            //  result[1] = acctname;
                        }
                        if (oraread["cus_sho_name"] != System.DBNull.Value)
                        {
                            result[3] = (string)(oraread["cus_sho_name"].ToString());
                            //  result[1] = acctname;
                        }
                        if (oraread["availbal"] != System.DBNull.Value)
                        {
                            result[4] = (string)(oraread["availbal"].ToString());
                            //  result[1] = acctname;
                        }
                        if (oraread["accountnumber"] != System.DBNull.Value)
                        {
                            result[5] = (string)(oraread["accountnumber"].ToString());
                            //  result[1] = acctname;
                        }


                        return result;

                    }

                }
                catch (Exception ex)
                {
                    Utility.Log().Fatal("Database Call Failed ", ex);
                    result[0] = "";
                    result[1] = "";
                    result[2] = "";
                    result[3] = "";
                    result[4] = "";
                    result[5] = "";
                    return result;
                }


                finally
                {

                    oracon.Close();
                }
            }


        }
    }

    public string ConvertToNuban(string OldAccountNumber)//, string cus_num, string cur_code, string led_code, string sub_acct_code)
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
        string NubanAccountNumber = null;


        try
        {
            using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
            {
                if (OraConn.State == ConnectionState.Closed)
                {
                    OraConn.Open();
                }
                OraSelect.Connection = OraConn;
                string selectquery = "select  MAP_ACC_NO from map_acct where bra_code = " + bra_code + " and cus_num = " + cus_num + "and cur_code = " + cur_code + "and led_code = " + led_code + " and sub_acct_code = " + sub_acct_code;// from map_acct where MAP_ACC_NO = '" + NubanAccountNumber + "'";
                OraSelect.CommandText = selectquery;
                OraSelect.CommandType = CommandType.Text;
                using (OraDrSelect = OraSelect.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (OraDrSelect.HasRows == true)
                    {
                        OraDrSelect.Read();
                        NubanAccountNumber = OraDrSelect["MAP_ACC_NO"].ToString();
                        return NubanAccountNumber;

                    }
                    else
                    {
                        return "-2";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);

            return "-1";
        }
        finally
        {

        }

    }

    public string ConvertToOldAccountNumber(string NubanAccountNumber)
    {
        using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
        {
            if (verifyNubanCheckDigit("058", NubanAccountNumber))
            {
                using (OracleCommand OraSelect = new OracleCommand())
                {
                    OracleDataReader OraDrSelect;
                    string bra_code = null;
                    string cus_num = null;
                    string cur_code = null;
                    string led_code = null;
                    string sub_acct_code = null;


                    try
                    {


                        //bra_code = bracode;
                        //cus_num = cusnum;
                        //cur_code = curcode;
                        //led_code = ledcode;
                        //sub_acct_code = subacctcode;
                        if (OraConn.State == ConnectionState.Closed)
                        {
                            OraConn.Open();
                        }
                        OraSelect.Connection = OraConn;
                        string selectquery = "select  bra_code ,cus_num ,cur_code,led_code,sub_acct_code from map_acct where MAP_ACC_NO = '" + NubanAccountNumber + "'";
                        OraSelect.CommandText = selectquery;
                        OraSelect.CommandType = CommandType.Text;
                        using (OraDrSelect = OraSelect.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            if (OraDrSelect.HasRows == true)
                            {
                                OraDrSelect.Read();
                                bra_code = OraDrSelect["bra_code"].ToString();
                                cus_num = OraDrSelect["cus_num"].ToString();
                                cur_code = OraDrSelect["cur_code"].ToString();
                                led_code = OraDrSelect["led_code"].ToString();
                                sub_acct_code = OraDrSelect["sub_acct_code"].ToString();
                                return bra_code + "/" + cus_num + "/" + cur_code + "/" + led_code + "/" + sub_acct_code;
                            }
                            else
                            {
                                return "-2";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utility.Log().Fatal("Database called Failed ", ex); 
                        return "-1";
                    }
                    finally
                    {
                        if (OraConn.State == ConnectionState.Open)
                        {
                            OraConn.Close();
                        }
                        OraConn.Dispose();
                    }
                }
            }
            else
            {
                return "0";

            }
        }

    }

    private bool verifyNubanCheckDigit(string bankcode, string NubanAccountNumber)
    {
        NubanAccountNumber = bankcode + NubanAccountNumber.Trim();

        if (NubanAccountNumber.Length != 13)
        {// Need to decide on response codes;
            return false;
        }
        string NubanAcct = NubanAccountNumber.Substring(0, 12);
        int CheckDigitFromAcctNumber = Convert.ToInt16(NubanAccountNumber.Substring(12, 1));

        string MagicNumber = "373373373373";
        int TotalValue = 0;
        int calculatedCheckDigit = 0;
        for (int i = 0; i < 12; i++)
        {
            TotalValue = TotalValue + (Convert.ToInt16(MagicNumber.Substring(i, 1)) * Convert.ToInt16(NubanAcct.Substring(i, 1)));
        }
        if ((TotalValue % 10) == 0)
        {
            calculatedCheckDigit = 0;
        }
        else
        {

            calculatedCheckDigit = 10 - (TotalValue % 10);
        }

        if (CheckDigitFromAcctNumber == calculatedCheckDigit)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public string AddTeller(int BASISID, string Username, string password, int branch_code, string roleid, string UpdateFlag)
    {
        string Result = string.Empty;
        xmlstring = "<Response>";
        OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
        OracleCommand oracomm = new OracleCommand("EBIZ_PACKAGE.GTB_TELLERS", oraconn);
        oracomm.CommandType = CommandType.StoredProcedure;
        try
        {
            if (oraconn.State == ConnectionState.Closed)
            {
                oraconn.Open();
            }
            oracomm.Parameters.Add("inp_bra_code", OracleDbType.Int32, 15).Value = branch_code;
            oracomm.Parameters.Add("inp_tell_id", OracleDbType.Int32, 15).Value = BASISID;
            oracomm.Parameters.Add("inprole", OracleDbType.Varchar2, 30).Value = roleid.Trim();

            oracomm.Parameters.Add("inp_ho_code", OracleDbType.Int32, 15).Value = 999;
            oracomm.Parameters.Add("inphorole", OracleDbType.Varchar2, 30).Value = roleid.Trim();

            oracomm.Parameters.Add("inp_OPER_type", OracleDbType.Int32, 15).Value = UpdateFlag.Trim();
            oracomm.Parameters.Add("inp_CUS_SHO_NAME", OracleDbType.Varchar2, 100).Value = Username.Trim();
            oracomm.Parameters.Add("inp_PASSWORD", OracleDbType.Varchar2, 100).Value = password;
            oracomm.Parameters.Add("RETURN_STATUS", OracleDbType.Int32, 15).Direction = ParameterDirection.Output;

            oracomm.ExecuteNonQuery();
            Result = oracomm.Parameters["RETURN_STATUS"].Value.ToString();
            oraconn.Close();

            if (Result.CompareTo("0") == 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                xmlstring = xmlstring + "</Response>";
            }
            else
            {
                //Get BASIS Error Description
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + GetBASISError(Convert.ToInt32(Result.Trim())) + "</Error>";
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

        oracomm = null;
        oraconn = null;

        return xmlstring;
    }

    public string GetAllBasisRoles()
    {
        string Result = string.Empty;
        OracleCommand cmd;
        OracleConnection oraconn;
        OracleDataAdapter ora_adpt;
        DataTable dt;

        string query_str = null;

        try
        {
            oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);

            // create the command for the function
            query_str = "select role_num as role_id, des_eng as role_desc from role_inf";

            oraconn.Open();
            cmd = new OracleCommand(query_str, oraconn);
            ora_adpt = new OracleDataAdapter(cmd);
            dt = new DataTable();

            ora_adpt.Fill(dt);

            xmlstring = "<Response>";

            if (dt.Rows.Count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<ROLES>";
                foreach (DataRow dr in dt.Rows)
                {
                    //GET ROLE
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

            dt = null;
            ora_adpt = null;
            cmd = null;
        }
        catch (Exception ex)

        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            xmlstring = xmlstring + "<CODE>1002</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string GetCustomerName(string bracode, string cusnum)
    {
        DataTable dt = new DataTable();
        string customername = "";
        try
        {
            var query = "select a.bra_code, b.des_eng, a.cus_num, a.aut_amt, to_char(a.del_date,'DDMMYYYY') del_date, to_char(a.eff_date,'DDMMYYYY') eff_date,a.comments, a.signature,d.cust_type,get_name1(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code) as cus_sho_name  from acct_sig a, branch b,address c,cust_pro d where a.bra_code=b.bra_code and a.bra_code=c.bra_code and a.cus_num = c.cus_num  and a.bra_code = d.bra_code and a.cus_num = d.cus_num and c.cur_code = 0 and a.bra_code=" + bracode + " and a.cus_num=" + cusnum;
            OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
            OracleCommand oracomm = new OracleCommand();
            OraConn.Open();
            oracomm.Connection = OraConn;
            oracomm.CommandType = CommandType.Text;
            oracomm.CommandText = query;            
            OracleDataReader oradrread = oracomm.ExecuteReader(CommandBehavior.CloseConnection);
            dt.Load(oradrread);
            customername = dt.Rows[0]["cus_sho_name"].ToString();

        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
        }
        return customername;
    }
    public CustDetRetVal GetBasisCustomerDetails(int bra_code, int cus_num)
    {
        string Result = string.Empty;
        OracleCommand cmd;
        OracleConnection oraconn;
        OracleDataAdapter ora_adpt;
        DataTable dt;
        CustDetRetVal custretval = null;
        int i = 0;

        string query_str = null;

        try
        {
            oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
            xmlstring = "<Response>";
            //if (checkifcorporateacct(bra_code, cus_num))
            //{

                // create the command for the function

                //query_str = "select a.bra_code, b.des_eng, a.cus_num, a.aut_amt, to_char(a.del_date,'DDMMYYYY') del_date, to_char(a.eff_date,'DDMMYYYY') eff_date,a.comments, a.signature,c.cus_sho_name  from acct_sig a, branch b,address c where a.bra_code=b.bra_code and a.bra_code=c.bra_code and a.cus_num = c.cus_num and c.cur_code = 0 and a.bra_code=" + bra_code.ToString() + " and a.cus_num=" + cus_num.ToString();
                query_str = "select a.bra_code, b.des_eng, a.cus_num, a.aut_amt, to_char(a.del_date,'DDMMYYYY') del_date, to_char(a.eff_date,'DDMMYYYY') eff_date,a.comments, a.signature,d.cust_type,get_name1(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code) as cus_sho_name  from acct_sig a, branch b,address c,cust_pro d where a.bra_code=b.bra_code and a.bra_code=c.bra_code and a.cus_num = c.cus_num  and a.bra_code = d.bra_code and a.cus_num = d.cus_num and c.cur_code = 0 and a.bra_code=" + bra_code.ToString() + " and a.cus_num=" + cus_num.ToString();

                //   query_str = "select a.bra_code, b.des_eng, a.cus_num, a.cus_sho_name, a.aut_amt, to_char(a.del_date,'DDMMYYYY') del_date, to_char(a.eff_date,'DDMMYYYY') eff_date,a.comments, a.signature  from acct_sig a, branch b where a.bra_code=b.bra_code and a.bra_code=" + bra_code.ToString() + " and a.cus_num=" + cus_num.ToString();

                oraconn.Open();
                cmd = new OracleCommand(query_str, oraconn);
                cmd.InitialLONGFetchSize = -1;
                ora_adpt = new OracleDataAdapter(cmd);
                dt = new DataTable();

                ora_adpt.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    custretval = new CustDetRetVal();
                    custretval.picture = new object[dt.Rows.Count];
                    custretval.Mandates = new string[dt.Rows.Count];
                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "<CUSTOMERS>";
                    foreach (DataRow dr in dt.Rows)
                    {
                        //GET ROLE
                        xmlstring = xmlstring + "<CUSTOMER>";
                        xmlstring = xmlstring + "<BRANCH_CODE>" + dr["bra_code"] + "</BRANCH_CODE>";
                        xmlstring = xmlstring + "<BRANCH_NAME>" + dr["des_eng"] + "</BRANCH_NAME>";
                        xmlstring = xmlstring + "<SIGNATORY_NAME>" + dr["cus_sho_name"] + "</SIGNATORY_NAME>";
                        xmlstring = xmlstring + "<AUTHORITY_AMOUNT>" + dr["aut_amt"] + "</AUTHORITY_AMOUNT>";
                        xmlstring = xmlstring + "<DELETION_DATE>" + dr["del_date"] + "</DELETION_DATE>";
                        xmlstring = xmlstring + "<CUST_TYPE>" + dr["CUST_TYPE"] + "</CUST_TYPE>";
                        xmlstring = xmlstring + "<EFFECTIVE_DATE>" + dr["eff_date"] + "</EFFECTIVE_DATE>";
                        xmlstring = xmlstring + "<COMMENTS>" + dr["comments"] + "</COMMENTS>";

                        custretval.picture[i] = dr["signature"];
                        if (!string.IsNullOrEmpty(dr["comments"].ToString()))
                        {
                            custretval.Mandates[i] = dr["comments"].ToString();
                        }
                        else
                        {
                            custretval.Mandates[i] = "";
                        }
                        //xmlstring = xmlstring + "<IMAGE>" + Encoding.ASCII.GetString( + "</IMAGE>";
                        xmlstring = xmlstring + "</CUSTOMER>";
                        i = i + 1;
                    }
                    xmlstring = xmlstring + "</CUSTOMERS>";
                    xmlstring = xmlstring + "</Response>";
                }
                else
                {
                    custretval = new CustDetRetVal();
                    custretval.Accounts = null;
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<Error>" + "NO RECORDS" + "</Error>";
                    xmlstring = xmlstring + "</Response>";
                }


                dt = null;
                ora_adpt = null;
                cmd = null;
                string allowedledgers = ConfigurationManager.AppSettings["PinPadAllowedLedgers"].ToString();
                //query_str = "select a.bra_code||'/'||a.cus_num||'/'||cur_code||'/'||led_code||'/'||sub_acct_code acct_key from account a where a.bra_code=" + bra_code.ToString() + " and cus_num=" + cus_num.ToString() + " and led_code in (1,2,3,59,82,65)";
                query_str = "select  b.map_acc_no ||')-'|| a.bra_code||'/'||a.cus_num||'/'||a.cur_code||'/'||a.led_code||'/'||a.sub_acct_code||'-('||navailbal(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code)   acct_key  from account a, map_acct b where a.bra_code=" + bra_code.ToString() + " and a.cus_num=" + cus_num.ToString() + "  and a.cur_code = 1 and  a.led_code in (" + allowedledgers + ") and a.bra_code = b.bra_code and a.cus_num = b.cus_num and a.cur_code = b.cur_code and a.led_code = b.led_code and a.sub_acct_code = b.sub_acct_code";

                cmd = new OracleCommand(query_str, oraconn);
                ora_adpt = new OracleDataAdapter(cmd);
                dt = new DataTable();

                ora_adpt.Fill(dt);
                i = 0;
                if (dt.Rows.Count > 0)
                {
                    custretval.Accounts = new string[dt.Rows.Count];

                    foreach (DataRow dr in dt.Rows)
                    {
                        //GET ACCOUNT
                        custretval.Accounts[i] = Convert.ToString(dr["acct_key"]);

                        i = i + 1;
                    }
                }

                oraconn.Close();
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 

            if(custretval.Accounts != null)                
            custretval.Accounts = null;
        }

        custretval.custdet = xmlstring;
        return custretval;
    }

    public CustDetRetVal GetBasisCustomerDetails(int bra_code, int cus_num,int curcode,int ledcode,int subacctcode)
    {
        string Result = string.Empty;
        OracleCommand cmd;
        OracleConnection oraconn;
        OracleDataAdapter ora_adpt;
        DataTable dt;
        CustDetRetVal custretval = null;
        int i = 0;

        string query_str = null;

        try
        {
            oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
            xmlstring = "<Response>";
            //if (checkifcorporateacct(bra_code, cus_num))
            //{

                // create the command for the function

                //query_str = "select a.bra_code, b.des_eng, a.cus_num, a.aut_amt, to_char(a.del_date,'DDMMYYYY') del_date, to_char(a.eff_date,'DDMMYYYY') eff_date,a.comments, a.signature,c.cus_sho_name  from acct_sig a, branch b,address c where a.bra_code=b.bra_code and a.bra_code=c.bra_code and a.cus_num = c.cus_num and c.cur_code = 0 and a.bra_code=" + bra_code.ToString() + " and a.cus_num=" + cus_num.ToString();
                query_str = "select a.bra_code, b.des_eng, a.cus_num, a.aut_amt, to_char(a.del_date,'DDMMYYYY') del_date, to_char(a.eff_date,'DDMMYYYY') eff_date,a.comments, a.signature,d.cust_type,get_name1(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code) as cus_sho_name  from acct_sig a, branch b,address c,cust_pro d where a.bra_code=b.bra_code and a.bra_code=c.bra_code and a.cus_num = c.cus_num  and a.bra_code = d.bra_code and a.cus_num = d.cus_num and c.cur_code = 0 and a.bra_code=" + bra_code.ToString() + " and a.cus_num=" + cus_num.ToString();

                //   query_str = "select a.bra_code, b.des_eng, a.cus_num, a.cus_sho_name, a.aut_amt, to_char(a.del_date,'DDMMYYYY') del_date, to_char(a.eff_date,'DDMMYYYY') eff_date,a.comments, a.signature  from acct_sig a, branch b where a.bra_code=b.bra_code and a.bra_code=" + bra_code.ToString() + " and a.cus_num=" + cus_num.ToString();

                oraconn.Open();
                cmd = new OracleCommand(query_str, oraconn);
            cmd.InitialLONGFetchSize = -1;
            ora_adpt = new OracleDataAdapter(cmd);
                dt = new DataTable();

                ora_adpt.Fill(dt);



                if (dt.Rows.Count > 0)
                {
                    custretval = new CustDetRetVal();
                    custretval.picture = new object[dt.Rows.Count];
                    custretval.Mandates = new string[dt.Rows.Count];
                    xmlstring = xmlstring + "<CODE>1000</CODE>";
                    xmlstring = xmlstring + "<CUSTOMERS>";
                    foreach (DataRow dr in dt.Rows)
                    {
                        //GET ROLE
                        xmlstring = xmlstring + "<CUSTOMER>";
                        xmlstring = xmlstring + "<BRANCH_CODE>" + dr["bra_code"] + "</BRANCH_CODE>";
                        xmlstring = xmlstring + "<BRANCH_NAME>" + dr["des_eng"] + "</BRANCH_NAME>";
                        xmlstring = xmlstring + "<SIGNATORY_NAME>" + dr["cus_sho_name"] + "</SIGNATORY_NAME>";
                        xmlstring = xmlstring + "<AUTHORITY_AMOUNT>" + dr["aut_amt"] + "</AUTHORITY_AMOUNT>";
                        xmlstring = xmlstring + "<DELETION_DATE>" + dr["del_date"] + "</DELETION_DATE>";
                        xmlstring = xmlstring + "<CUST_TYPE>" + dr["CUST_TYPE"] + "</CUST_TYPE>";
                        xmlstring = xmlstring + "<EFFECTIVE_DATE>" + dr["eff_date"] + "</EFFECTIVE_DATE>";
                        xmlstring = xmlstring + "<COMMENTS>" + dr["comments"] + "</COMMENTS>";

                        custretval.picture[i] = dr["signature"];
                        if (!string.IsNullOrEmpty(dr["comments"].ToString()))
                        {
                            custretval.Mandates[i] = dr["comments"].ToString();
                        }
                        else
                        {
                            custretval.Mandates[i] = "";
                        }
                        //xmlstring = xmlstring + "<IMAGE>" + Encoding.ASCII.GetString( + "</IMAGE>";
                        xmlstring = xmlstring + "</CUSTOMER>";
                        i = i + 1;
                    }
                    xmlstring = xmlstring + "</CUSTOMERS>";
                    xmlstring = xmlstring + "</Response>";
                }
                else
                {
                    custretval = new CustDetRetVal();
                    custretval.Accounts = null;
                    xmlstring = xmlstring + "<CODE>1001</CODE>";
                    xmlstring = xmlstring + "<Error>" + "NO RECORDS" + "</Error>";
                    xmlstring = xmlstring + "</Response>";
                }


                dt = null;
                ora_adpt = null;
                cmd = null;
                string allowedledgers = ConfigurationManager.AppSettings["PinPadAllowedLedgers"].ToString();
                //query_str = "select a.bra_code||'/'||a.cus_num||'/'||cur_code||'/'||led_code||'/'||sub_acct_code acct_key from account a where a.bra_code=" + bra_code.ToString() + " and cus_num=" + cus_num.ToString() + " and led_code in (1,2,3,59,82,65)";
                query_str = "select  b.map_acc_no ||')-'|| a.bra_code||'/'||a.cus_num||'/'||a.cur_code||'/'||a.led_code||'/'||a.sub_acct_code||'-('||navailbal(a.bra_code,a.cus_num,a.cur_code,a.led_code,a.sub_acct_code)   acct_key  from account a, map_acct b where a.bra_code=" + bra_code.ToString() + " and a.cus_num= " + cus_num.ToString() + "  and a.cur_code = " + curcode.ToString() + " and  a.led_code = " + ledcode.ToString() + "and a.sub_acct_code = " + subacctcode.ToString() + " and a.bra_code = b.bra_code and a.cus_num = b.cus_num and a.cur_code = b.cur_code and a.led_code = b.led_code and a.sub_acct_code = b.sub_acct_code";

                cmd = new OracleCommand(query_str, oraconn);
                ora_adpt = new OracleDataAdapter(cmd);
                dt = new DataTable();

                ora_adpt.Fill(dt);
                i = 0;
                if (dt.Rows.Count > 0)
                {
                    custretval.Accounts = new string[dt.Rows.Count];

                    foreach (DataRow dr in dt.Rows)
                    {
                        //GET ACCOUNT
                        custretval.Accounts[i] = Convert.ToString(dr["acct_key"]);

                        i = i + 1;
                    }
                }

                oraconn.Close();
            //}
            //else
            //{
            //    custretval = new CustDetRetVal();
            //    custretval.Accounts = null;
            //    xmlstring = xmlstring + "<CODE>1001</CODE>";
            //    xmlstring = xmlstring + "<Error>" + "ONLY INDIVIDUAL ACCOUNTS IS ALLOWED" + "</Error>";
            //    xmlstring = xmlstring + "</Response>";


            //}
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            custretval.Accounts = null;
        }

        custretval.custdet = xmlstring;
        return custretval;
    }

    public string AddBasisRole(string role_desc)
    {
        string Result = string.Empty;
        xmlstring = "<Response>";
        OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
        OracleCommand oracomm = new OracleCommand("GTB_ADDROLE", oraconn);
        oracomm.CommandType = CommandType.StoredProcedure;
        try
        {
            if (oraconn.State == ConnectionState.Closed)
            {
                oraconn.Open();
            }
            oracomm.Parameters.Add("INP_ROLE_NAME", OracleDbType.Varchar2, 50).Value = role_desc;
            oracomm.Parameters.Add("RETURN_STATUS", OracleDbType.Int32, 15).Direction = ParameterDirection.Output;

            oracomm.ExecuteNonQuery();
            Result = oracomm.Parameters["RETURN_STATUS"].Value.ToString();
            //oraconn.Close();

            if (Result.CompareTo("0") == 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<MESSAGE>SUCCESS</MESSAGE>";
                xmlstring = xmlstring + "</Response>";
            }
            else if (Result == "")
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>EMPTY string RETURNED</Error>";
                xmlstring = xmlstring + "</Response>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + GetBASISError(Convert.ToInt32(Result.Trim())) + "</Error>";
                xmlstring = xmlstring + "</Response>";
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            xmlstring = xmlstring + "<CODE>1002</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
            //return ex.Message;
        }

        oracomm = null;
        oraconn.Close();
        oraconn = null;

        return xmlstring;
    }

    public string GetAllBasisUsers()
    {
        string Result = string.Empty;
        OracleCommand cmd;
        OracleConnection oraconn;
        OracleDataAdapter ora_adpt;
        DataTable dt;

        string query_str = null;

        try
        {
            oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);

            // create the command for the function
            query_str = "select tell_id as basis_id, max(cus_sho_name) as user_name from teller where cus_sho_name is not null group by tell_id";

            oraconn.Open();
            cmd = new OracleCommand(query_str, oraconn);
            ora_adpt = new OracleDataAdapter(cmd);
            dt = new DataTable();

            ora_adpt.Fill(dt);

            xmlstring = "<Response>";

            if (dt.Rows.Count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<USERS>";
                foreach (DataRow dr in dt.Rows)
                {
                    //GET ROLE
                    xmlstring = xmlstring + "<USER>";
                    xmlstring = xmlstring + "<BASIS_ID>" + dr["basis_id"] + "</BASIS_ID>";
                    xmlstring = xmlstring + "<USER_NAME>" + dr["user_name"] + "</USER_NAME>";
                    xmlstring = xmlstring + "</USER>";
                }
                xmlstring = xmlstring + "</USERS>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "NO ROLES" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";

            dt = null;
            ora_adpt = null;
            cmd = null;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            xmlstring = xmlstring + "<CODE>1002</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string SearchBASISUser(string criteria, string bracode)
    {
        string Result = string.Empty;
        OracleCommand cmd;
        OracleConnection oraconn;
        OracleDataAdapter ora_adpt;
        DataTable dt;

        string query_str = null;
        string search = null;

        try
        {
            oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);

            // create the command for the function
            search = criteria.Replace("*", "%");
            //query_str = "select tell_id as basis_id, max(cus_sho_name) as user_name, max(case when mat_date < trunc(sysdate) then 'I' else 'A' end) as status from teller where cus_sho_name like '" + search + "' group by tell_id";
            query_str = "select tell_id as basis_id, cus_sho_name as user_name, bra_code as branch, (case when mat_date < trunc(sysdate) then 'I' else 'A' end) as status from teller where bra_code = " + bracode + " and cus_sho_name like '" + search + "'";
            oraconn.Open();
            cmd = new OracleCommand(query_str, oraconn);
            ora_adpt = new OracleDataAdapter(cmd);
            dt = new DataTable();

            ora_adpt.Fill(dt);

            xmlstring = "<Response>";

            if (dt.Rows.Count > 0)
            {
                xmlstring = xmlstring + "<CODE>1000</CODE>";
                xmlstring = xmlstring + "<USERS>";
                foreach (DataRow dr in dt.Rows)
                {
                    //GET ROLE
                    xmlstring = xmlstring + "<USER>";
                    xmlstring = xmlstring + "<BASIS_ID>" + dr["basis_id"] + "</BASIS_ID>";
                    xmlstring = xmlstring + "<USER_NAME>" + dr["user_name"] + "</USER_NAME>";
                    xmlstring = xmlstring + "<BRANCH>" + dr["branch"] + "</BRANCH>";
                    xmlstring = xmlstring + "<STATUS>" + dr["status"] + "</STATUS>";

                    xmlstring = xmlstring + "</USER>";
                }
                xmlstring = xmlstring + "</USERS>";
            }
            else
            {
                xmlstring = xmlstring + "<CODE>1001</CODE>";
                xmlstring = xmlstring + "<Error>" + "NO ROLES" + "</Error>";
            }

            xmlstring = xmlstring + "</Response>";

            dt = null;
            ora_adpt = null;
            cmd = null;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            xmlstring = xmlstring + "<CODE>1002</CODE>";
            xmlstring = xmlstring + "<Error>" + ex.Message.Replace("'", "") + "</Error>";
            xmlstring = xmlstring + "</Response>";
        }

        return xmlstring;
    }

    public string GetBASISError(int errorcode)
    {
        string Result = string.Empty;
        OracleCommand cmd;
        OracleConnection oraconn;
        OracleDataAdapter ora_adpt;
        DataTable dt;

        string query_str = null;

        try
        {
            oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);

            // create the command for the function

            //query_str = "select tell_id as basis_id, max(cus_sho_name) as user_name, max(case when mat_date < trunc(sysdate) then 'I' else 'A' end) as status from teller where cus_sho_name like '" + search + "' group by tell_id";
            query_str = "select tab_ent,des_eng from text_tab where tab_id=50 and tab_ent=" + errorcode.ToString();
            oraconn.Open();
            cmd = new OracleCommand(query_str, oraconn);
            ora_adpt = new OracleDataAdapter(cmd);
            dt = new DataTable();

            ora_adpt.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    //GET ERROR

                    xmlstring = errorcode.ToString() + ":" + dr["des_eng"].ToString();

                }

            }
            else
            {

                xmlstring = "UNKNOWN ERROR";
            }

            dt = null;
            ora_adpt = null;
            cmd = null;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            xmlstring = xmlstring = "UNKNOWN ERROR";
        }

        return xmlstring;
    }

    public string AnalyzeFx(string acctno, decimal transfer_amt, DateTime anal_date)
    {
        char[] delim = new char[] { '|' };
        char[] delim2 = new char[] { '/' };

        decimal rate_gbp = 1.54M, rate_eur = 1.32M, rate_rnd = 0.15M, rate_usd = 1.0M, rate = 0.0M;
        string resultstr = "", narration = "";
        string[] tempstr;
        decimal AcctBal = 0, nairabal = 0, total_fx = 0, total_usd_fx = 0, curBal, tra_amt = 0, cd_limit = 10000;
        DataSet dsTrans, dsDet;
        decimal out_amt = 0, out_amt_usd = 0, valid_out = 0, tot_cr = 0, tot_valid_cr = 0, tot_invalid_cr = 0, tot_cd = 0, tot_inv_cd = 0, tot_inf = 0, tot_trf = 0, tot_valid_trf = 0, tot_debit = 0, tot_invst = 0;
        decimal daily_limit = 0, weekly_limit = 0;
        int deb_cre, expl_code;
        DataRow drTrans;
        string customer_name = null;

        xmlstring = "<Response>";

        out_amt = transfer_amt;
        resultstr = GetAcctBal(acctno.Trim(), anal_date);
        dsTrans = GetTransactions(acctno.Trim(), anal_date, AcctBal);

        tempstr = resultstr.Split(delim);
        AcctBal = Convert.ToDecimal(tempstr[0]);
        total_fx = Convert.ToDecimal(tempstr[1]);
        customer_name = tempstr[2];
        nairabal = Convert.ToDecimal(tempstr[3]);

        //Convert amount to dollar.
        tempstr = acctno.Split(delim2);
        if (tempstr[2].Trim() == "2")
            rate = rate_usd;
        else if (tempstr[2].Trim() == "3")
            rate = rate_gbp;
        else if (tempstr[2].Trim() == "46")
            rate = rate_eur;
        else if (tempstr[2].Trim() == "43")
            rate = rate_rnd;
        else
            rate = rate_usd;

        total_usd_fx = total_fx * rate;

        //Set limits
        daily_limit = 10000;    //Applicable to cash deposit
        weekly_limit = 25000;   //Applicable to cash deposit
        if (total_usd_fx >= weekly_limit)
        {
            weekly_limit = 0;
        }
        else
        {
            weekly_limit = weekly_limit - total_usd_fx;
        }

        if (weekly_limit <= daily_limit)
        {
            daily_limit = weekly_limit;
        }

        if (AcctBal < out_amt)
        {
            resultstr = "NOT VALID";
            narration = "Available Balance less than Transfer Amount";
            xmlstring = xmlstring + "<RESULT>" + resultstr + "</RESULT>";
            xmlstring = xmlstring + "<ACCTBAL>" + AcctBal.ToString("N") + "</ACCTBAL>";
            xmlstring = xmlstring + "<NAIRAACCTBAL>" + nairabal.ToString("N") + "</NAIRAACCTBAL>";
            xmlstring = xmlstring + "<CUSTNAME>" + customer_name + "</CUSTNAME>";
            xmlstring = xmlstring + "<VALIDOUT>0.00</VALIDOUT>";
            xmlstring = xmlstring + "<NARRATIONS>";
            xmlstring = xmlstring + "<NARRATION>" + narration + "</NARRATION>";
            xmlstring = xmlstring + "</NARRATIONS>";
            xmlstring = xmlstring + "</Response>";

            return xmlstring;
        }

        for (int i = dsTrans.Tables[0].Rows.Count - 1; i > 0; i--)
        {
            drTrans = dsTrans.Tables[0].Rows[i];
            tra_amt = Convert.ToDecimal(drTrans["tra_amt"]);
            curBal = Convert.ToDecimal(drTrans["crnt_bal"]);
            expl_code = Convert.ToInt32(drTrans["expl_code"]);
            deb_cre = Convert.ToInt32(drTrans["deb_cre_ind"]);

            if (curBal >= 0 && curBal <= 10)
                break;

            if (deb_cre == 2)
            {
                tot_cr = tot_cr + tra_amt;

                if (expl_code == 700 || expl_code == 312 || expl_code == 953 || expl_code == 45 || expl_code == 316 || expl_code == 330) //Inflow -- valid
                {
                    tot_inf = tot_inf + tra_amt;
                    tot_valid_cr = tot_valid_cr + tra_amt;
                }
                if (expl_code == 104 || expl_code == 218) //Investment -- invalid
                {
                    tot_invst = tot_invst + tra_amt;
                    tot_invalid_cr = tot_invalid_cr + tra_amt;
                }
                else if ((expl_code == 1) && (tra_amt <= 10000)) //Cash Deposit -- valid
                {
                    tot_cd = tot_cd + tra_amt;
                    tot_valid_cr = tot_valid_cr + tra_amt;
                }
                else if ((expl_code == 1) && (tra_amt > 10000)) //Invalid Cash Deposit -- invalid
                {
                    tot_inv_cd = tot_inv_cd + tra_amt;
                    tot_invalid_cr = tot_invalid_cr + tra_amt;
                }
                else if (expl_code == 102 || expl_code == 100 || expl_code == 99 || expl_code == 147) //Local transfer -- invalid
                {
                    tot_trf = tot_trf + tra_amt;
                    tot_invalid_cr = tot_invalid_cr + tra_amt;
                }

                else //398, 98, 903, 242
                {
                    tot_invalid_cr = tot_invalid_cr + tra_amt;
                }
            }
            else
                tot_debit = tot_debit + tra_amt;

            if ((Math.Abs(tot_cr - (tot_debit + AcctBal)) <= 10))
                break;
        }

        if (tot_invalid_cr > tot_debit)
        {
            valid_out = AcctBal - (tot_invalid_cr - tot_debit);
        }
        else
        {
            valid_out = AcctBal;
        }

        resultstr = "VALID";
        if (valid_out < out_amt)
        {
            resultstr = "NOT VALID";
        }
        else
        {
            if ((out_amt * rate) <= weekly_limit)
            {
                if ((out_amt > daily_limit) && ((tot_inf * rate) < (out_amt - daily_limit)))
                {
                    resultstr = "NOT VALID";
                }
            }
            else
            {
                if ((tot_inf * rate) < ((out_amt * rate) - weekly_limit))
                    resultstr = "NOT VALID";
            }
        }

        xmlstring = xmlstring + "<RESULT>" + resultstr + "</RESULT>";
        xmlstring = xmlstring + "<ACCTBAL>" + AcctBal.ToString("N") + "</ACCTBAL>";
        xmlstring = xmlstring + "<NAIRAACCTBAL>" + nairabal.ToString("N") + "</NAIRAACCTBAL>";
        xmlstring = xmlstring + "<CUSTNAME>" + customer_name + "</CUSTNAME>";
        xmlstring = xmlstring + "<VALIDOUT>" + valid_out.ToString("N") + "</VALIDOUT>";

        if (resultstr == "NOT VALID")
        {
            xmlstring = xmlstring + "<NARRATIONS>";
            if (tot_trf > 0)
            {
                narration = "Found funds transferred locally";
                xmlstring = xmlstring + "<NARRATION>" + narration + "</NARRATION>";
            }
            if (tot_invst > 0)
            {
                narration = "Found Matured Investment";
                xmlstring = xmlstring + "<NARRATION>" + narration + "</NARRATION>";
            }
            if (tot_inv_cd > 0)
            {
                narration = "Found Invalid Cash Deposit";
                xmlstring = xmlstring + "<NARRATION>" + narration + "</NARRATION>";
            }

            xmlstring = xmlstring + "</NARRATIONS>";
        }

        xmlstring = xmlstring + "</Response>";

        return xmlstring;
    }

    private DataSet GetTransactions(string acctno, DateTime datetm, decimal balance)
    {
        string query_str, Remark = string.Empty;
        string daterange;
        char[] delim = new char[] { '/' };
        string[] tempstr;
        DataSet ds = null, ds1 = null;
        //string commstr, vatstr;

        try
        {
            daterange = datetm.Day.ToString().PadLeft(2, '0') + datetm.Month.ToString().PadLeft(2, '0') + datetm.Year.ToString().PadLeft(4, '0');
            tempstr = acctno.Split(delim);

            // create the connection
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);

            // create the command for the function

            query_str = "select tra_date, deb_cre_ind, tra_amt, expl_code, crnt_bal, remarks from transact where bra_code=" + tempstr[0].Trim() + " and cus_num=" + tempstr[1].Trim() + " and cur_code=" + tempstr[2].Trim() + " and led_code=" + tempstr[3].Trim() + " and sub_acct_code=" + tempstr[4].Trim() + " and tra_date <= '" + daterange + "' order by tra_date, upd_time";

            oraconn.Open();
            OracleCommand cmd = new OracleCommand(query_str, oraconn);
            ds = new DataSet();
            OracleDataAdapter adpt = new OracleDataAdapter(cmd);

            // execute the function
            adpt.Fill(ds);
            adpt = null;
            cmd = null;

            if (datetm.ToShortDateString() == DateTime.Now.ToShortDateString())
            {
                query_str = "select tra_date, deb_cre_ind, tra_amt, expl_code," + balance.ToString() + " crnt_bal, remarks from tell_act where bra_code=" + tempstr[0].Trim() + " and cus_num=" + tempstr[1].Trim() + " and cur_code=" + tempstr[2].Trim() + " and led_code=" + tempstr[3].Trim() + " and sub_acct_code=" + tempstr[4].Trim() + " order by tra_date, upd_time";
                cmd = new OracleCommand(query_str, oraconn);
                ds1 = new DataSet();
                adpt = new OracleDataAdapter(cmd);

                // execute the function
                adpt.Fill(ds1);
                if (ds1.Tables[0].Rows.Count > 0)
                    ds.Merge(ds1, true, MissingSchemaAction.Ignore);

                adpt = null;
                cmd = null;
            }

            oraconn.Close();
            oraconn = null;
            return ds;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            return ds;
        }
    }

    private string GetAcctBal(string acctno, DateTime datetm)
    {
        string query_str, Remark = string.Empty;
        string daterange, mondaystr;
        decimal retval = 0;
        char[] delim = new char[] { '/' };
        string[] tempstr;
        decimal bal = 0, nairabal = 0;
        DataSet ds = null;
        DateTime monday = DateTime.Now;
        decimal FxDone = 0;
        string custname = null;

        OracleCommand cmd;
        OracleDataReader reader;
        OracleDataAdapter adpt;
        //string commstr, vatstr;

        try
        {
            tempstr = acctno.Split(delim);
            if (datetm.DayOfWeek == DayOfWeek.Monday)
                monday = datetm;
            else if (datetm.DayOfWeek == DayOfWeek.Tuesday)
                monday = datetm.AddDays(-1.0);
            else if (datetm.DayOfWeek == DayOfWeek.Wednesday)
                monday = datetm.AddDays(-2.0);
            else if (datetm.DayOfWeek == DayOfWeek.Thursday)
                monday = datetm.AddDays(-3.0);
            else if (datetm.DayOfWeek == DayOfWeek.Friday)
                monday = datetm.AddDays(-4.0);
            else if (datetm.DayOfWeek == DayOfWeek.Saturday)
                monday = datetm.AddDays(-5.0);
            else if (datetm.DayOfWeek == DayOfWeek.Sunday)
                monday = datetm.AddDays(-6.0);

            mondaystr = monday.Day.ToString().PadLeft(2, '0') + monday.Month.ToString().PadLeft(2, '0') + monday.Year.ToString().PadLeft(4, '0');
            daterange = datetm.Day.ToString().PadLeft(2, '0') + datetm.Month.ToString().PadLeft(2, '0') + datetm.Year.ToString().PadLeft(4, '0');

            // create the connection
            OracleConnection oraconn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]);
            oraconn.Open();

            //Get total sum of Fx from Monday of the selected week
            query_str = "select nvl(sum(tra_amt),0) TOTAL_FX from transact where bra_code=" + tempstr[0].Trim() + " and cus_num=" + tempstr[1].Trim() + " and cur_code=" + tempstr[2].Trim() + " and led_code=" + tempstr[3].Trim() + " and sub_acct_code=" + tempstr[4].Trim() + " and tra_date between '" + mondaystr + "' and '" + daterange + "' and deb_cre_ind=1 and expl_code=305";
            cmd = new OracleCommand(query_str, oraconn);

            // execute the function
            reader = cmd.ExecuteReader();
            if (reader.HasRows == true)
            {
                reader.Read();
                FxDone = Convert.ToDecimal(reader["TOTAL_FX"]);
                reader.Close();
                reader = null;
                cmd = null;
            }
            else
            {
                reader = null;
                reader.Close();
                reader = null;
                cmd = null;
                FxDone = 0;
            }

            //Get Customer's name
            query_str = "select cus_sho_name from address where bra_code=" + tempstr[0].Trim() + " and cus_num=" + tempstr[1].Trim() + " and cur_code=0 and led_code=0 and sub_acct_code=0";
            cmd = new OracleCommand(query_str, oraconn);

            // execute the function
            reader = cmd.ExecuteReader();
            if (reader.HasRows == true)
            {
                reader.Read();
                custname = reader["cus_sho_name"].ToString();
                reader.Close();
                reader = null;
                cmd = null;
            }
            else
            {
                custname = "EMPTY";
                reader.Close();
                reader = null;
                cmd = null;
            }

            // create the command for the function
            if (datetm.ToShortDateString() == DateTime.Now.ToShortDateString())
            {
                query_str = "select nvl(navailbal(" + tempstr[0].Trim() + "," + tempstr[1].Trim() + "," + tempstr[2].Trim() + "," + tempstr[3].Trim() + "," + tempstr[4].Trim() + "),0) as ACCTBAL from dual";

                cmd = new OracleCommand(query_str, oraconn);

                // execute the function
                reader = cmd.ExecuteReader();
                if (reader.HasRows == true)
                {
                    reader.Read();
                    bal = Convert.ToDecimal(reader["ACCTBAL"]);
                    reader.Close();
                    reader = null;
                    cmd = null;

                }
                else
                {
                    bal = 0;
                    reader = null;
                    cmd = null;

                }

                //Get Balance from Naira Account
                query_str = "select nvl(navailbal(" + tempstr[0].Trim() + "," + tempstr[1].Trim() + ",1,1,0),0) as ACCTBAL from dual";

                cmd = new OracleCommand(query_str, oraconn);

                // execute the function
                reader = cmd.ExecuteReader();
                if (reader.HasRows == true)
                {
                    reader.Read();
                    nairabal = Convert.ToDecimal(reader["ACCTBAL"]);
                    reader.Close();
                    reader = null;
                    cmd = null;
                    oraconn.Close();
                }
                else
                {
                    nairabal = 0;
                    reader = null;
                    cmd = null;
                    oraconn.Close();
                }

            }
            else
            {
                query_str = "select crnt_bal from transact where bra_code=" + tempstr[0].Trim() + " and cus_num=" + tempstr[1].Trim() + " and cur_code=" + tempstr[2].Trim() + " and led_code=" + tempstr[3].Trim() + " and sub_acct_code=" + tempstr[4].Trim() + " and tra_date <= '" + daterange + "' order by tra_date, upd_time";
                cmd = new OracleCommand(query_str, oraconn);
                ds = new DataSet();
                adpt = new OracleDataAdapter(cmd);

                // execute the function
                adpt.Fill(ds);
                adpt = null;
                cmd = null;
                if (ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[ds.Tables[0].Rows.Count - 1];
                    bal = Convert.ToDecimal(dr["crnt_bal"]);

                }
                else
                {
                    bal = 0;
                    retval = 0;

                }

                //get balance from Naira account
                query_str = "select crnt_bal from transact where bra_code=" + tempstr[0].Trim() + " and cus_num=" + tempstr[1].Trim() + " and cur_code=1 and led_code=1 and sub_acct_code=0 and tra_date <= '" + daterange + "' order by tra_date, upd_time";
                cmd = new OracleCommand(query_str, oraconn);
                ds = new DataSet();
                adpt = new OracleDataAdapter(cmd);

                // execute the function
                adpt.Fill(ds);
                adpt = null;
                cmd = null;
                if (ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[ds.Tables[0].Rows.Count - 1];
                    nairabal = Convert.ToDecimal(dr["crnt_bal"]);
                    oraconn.Close();
                }
                else
                {
                    nairabal = 0;
                    retval = 0;
                    oraconn.Close();
                }
            }

            return bal.ToString() + "|" + FxDone.ToString() + "|" + custname.Trim() + "|" + nairabal.ToString();
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            return "0|0|0|0";
        }
    }

    private bool checkifcorporateacct(int bra_code, int cus_num)
    {    
        string selectqry = "";
        SqlDataReader dr;
        SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["BankCardCon"].ToString());
        try
        {
            //open connetion
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            string allowedcusttypes = ConfigurationManager.AppSettings["AllowedCustTypes"];
            selectqry = "select cus_type from Cards_Mastercard where branch_code = " + bra_code + " and Customer_No = " + cus_num + " and CUS_TYPE in (" + allowedcusttypes + ")";// )";
            SqlCommand comm = new SqlCommand(selectqry, conn);
             dr = comm.ExecuteReader();

            if (dr.Read())
            {

                return true;
            }
            else
            {

                return false;
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex); 
            return false;
        }
        finally
        {
            conn.Close();

        }
        
    }

    public string GetTransactionDetails(string xmlRequest)
    {
        Utilities utilities = new Utilities();

        string xmlString = "";

        if (string.IsNullOrEmpty(xmlRequest))
        {
            Utility.Log().Info("Response for " + xmlRequest + " is : " + xmlString);
            return utilities.ComposeError("1004", "string cannot be empty");
        }

        XmlDocument xmlRequestDoc = new XmlDocument();

        xmlRequestDoc.LoadXml(xmlRequest);

        try
        {
            string phoneNumber = xmlRequestDoc.SelectSingleNode("FetchFundedAccountDetails/PhoneNumber").InnerXml;

            if (!phoneNumber.StartsWith("0"))
            {
                return utilities.ComposeError("1002", "Invalid Phone Number");
            }

            ulong tempCustomerNumber;

            var isGoodNumber = ulong.TryParse(phoneNumber, out tempCustomerNumber);

            if (!isGoodNumber || phoneNumber.Length != 11)
            {
                return utilities.ComposeError("1002", "Invalid Phone Number");
            }

            decimal amount = decimal.Parse(xmlRequestDoc.SelectSingleNode("FetchFundedAccountDetails/Amount").InnerXml);

            string PreferredLedger = "";

            Eone eone = new Eone();

            TransactionDetail t = new TransactionDetail();
            TransactionDetail m = new TransactionDetail();

            try
            {
                Utility.Log().Info("About to get accounts linked to mobile number : " + phoneNumber);

                DataTable response = eone.getAllLinkedAccountGENSDB(phoneNumber);

                Utility.Log().Info("Response from getAllLinkedAccountGENSDB returned " + response.Rows.Count + " accounts for : " + phoneNumber);

                if (response.Rows.Count > 0)
                {
                    int count = 0;

                    int rowCount = response.Rows.Count;
                    string[] BeneficiaryResult = new string[3];
                    BeneficiaryResult.SetValue(string.Empty, 0);
                    BeneficiaryResult.SetValue(string.Empty, 1);
                    BeneficiaryResult.SetValue(string.Empty, 2);

                    string BenName = BeneficiaryResult[0];
                    string BenRestriction = BeneficiaryResult[1];
                    string BeneOldAccount = BeneficiaryResult[2];

                    foreach (DataRow item in response.Rows)
                    {
                        count += 1;

                        m = new TransactionDetail();

                        string branchCode = item["bra_code"].ToString();
                        string customerNo = item["cus_num"].ToString();
                        string currencyCode = item["cur_code"].ToString();
                        string ledgerCode = item["led_code"].ToString();
                        string subAcctCode = item["sub_acct_code"].ToString();
                        string balance = item["avail_bal"].ToString();

                        string oldAccount = string.Concat(branchCode, "/", customerNo, "/", currencyCode, "/", ledgerCode, "/", subAcctCode);

                        m.Balance = Convert.ToDecimal(balance);

                        Utility.Log().Info("About to call GetSenderTypeOfDepNameSourceRestNubanCusProfile for phone number : " + phoneNumber);

                        string[] SenderResult = GetSenderTypeOfDepNameSourceRestNubanCusProfile(oldAccount);

                        Utility.Log().Info("Gotten response from GetSenderTypeOfDepNameSourceRestNubanCusProfile for phone number : " + phoneNumber);

                        m.LedgerCode = ledgerCode;
                        m.OldAccountNumber = oldAccount;
                        m.CustomerName = SenderResult[0];
                        m.CustomerNuban = SenderResult[1];
                        m.BVN = SenderResult[5];

                        if (string.IsNullOrEmpty(m.CustomerName))
                        {
                            return utilities.ComposeError("1001", "Not funded");
                        }
                        else
                        {
                            if (m.Balance > Convert.ToDecimal(amount) && string.IsNullOrWhiteSpace(m.SourceRestriction))
                            {
                                t.AccountList.Add(m);
                                t.ResponseCode = 1000;

                                if (string.IsNullOrWhiteSpace(PreferredLedger))
                                {
                                    var index = t.AccountList.FindIndex(x => x.LedgerCode.Equals("26", StringComparison.OrdinalIgnoreCase));

                                    if (index > 0)
                                    {
                                        var topItem = t.AccountList[index];
                                        t.AccountList.RemoveAt(index);
                                        t.AccountList.Insert(0, topItem);

                                        var finalAccount = t.AccountList.FirstOrDefault(a => a.Balance >= amount);
                                        return utilities.ComposeReturnMessage("1000", "Successful", finalAccount.CustomerNuban, finalAccount.CustomerName, finalAccount.BVN, finalAccount.OldAccountNumber, finalAccount.Balance.ToString());
                                    }

                                    else
                                    {
                                        var finalAccount = t.AccountList.FirstOrDefault(a => a.Balance >= amount);
                                        return utilities.ComposeReturnMessage("1000", "Successful", finalAccount.CustomerNuban, finalAccount.CustomerName, finalAccount.BVN, finalAccount.OldAccountNumber, finalAccount.Balance.ToString());
                                    }
                                }
                                else
                                {
                                    var index = t.AccountList.FindIndex(x => x.LedgerCode.Equals(PreferredLedger, StringComparison.OrdinalIgnoreCase));

                                    if (index > 0)
                                    {
                                        var topItem = t.AccountList[index];
                                        t.AccountList.RemoveAt(index);
                                        t.AccountList.Insert(0, topItem);

                                        var finalAccount = t.AccountList.FirstOrDefault(a => a.Balance >= amount);
                                        return utilities.ComposeReturnMessage("1000", "Successful", finalAccount.CustomerNuban, finalAccount.CustomerName, finalAccount.BVN, finalAccount.OldAccountNumber, finalAccount.Balance.ToString());
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    if (rowCount == count && t.ResponseCode != 1000)
                    {
                        return utilities.ComposeError("1003", "Not funded");
                    }

                    return utilities.ComposeError("1001", "No record found");
                }
                else
                {
                    return utilities.ComposeError("1002", "Mobile Number does not exist");
                }
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("An exception occured connecting to GENS DB. Message - " + ex.Message + "|Stacktrace - " + ex.StackTrace);
                return utilities.ComposeError("1005", "An exception occurred!");
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("An exception occured connecting to GENS DB. Message - " + ex.Message + "|Stacktrace - " + ex.StackTrace);
            return utilities.ComposeError("1005", "An exception occurred");
        }
    }

    private string[] GetSenderTypeOfDepNameSourceRestNubanCusProfile(string oldAccount)
    {
        string[] result = new string[6];

        string Query = "select get_name2(b.bra_code,b.cus_num,b.cur_code,b.led_code,b.sub_acct_code) CusName, get_nuban(b.bra_code, b.cus_num,b.cur_code,b.led_code, b.sub_acct_code) nuban,c.cust_type,d.type_of_dep, d.cus_class,get_bvn1(b.bra_code, b.cus_num) BVN from account b, cust_pro c, customer d where c.bra_code = b.bra_code and c.cus_num = b.cus_num and d.bra_code = b.bra_code and d.cus_num = b.cus_num and b.bra_code = :BraCode and b.cus_num = :CusNum and b.cur_code = :CurCode and b.led_code = :LedCode and b.sub_acct_code = :SubAcctCode and b.rest_ind < 4";

        string[] SplitAccount = null;

        if (oldAccount.Contains("/"))
        {
            SplitAccount = oldAccount.Split('/');
        }

        try
        {
            using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["BASISConString_eone"]))
            {
                using (OracleCommand OraSelect = new OracleCommand(Query, OraConn))
                {
                    OraSelect.CommandType = CommandType.Text;

                    if (SplitAccount != null)
                    {
                        OraSelect.Parameters.Add(":bra_code", OracleDbType.Varchar2).Value = SplitAccount[0];
                        OraSelect.Parameters.Add(":CusNum", OracleDbType.Varchar2).Value = SplitAccount[1];
                        OraSelect.Parameters.Add(":CurCode", OracleDbType.Varchar2).Value = SplitAccount[2];
                        OraSelect.Parameters.Add(":LedCode", OracleDbType.Varchar2).Value = SplitAccount[3];
                        OraSelect.Parameters.Add(":SubAcctCode", OracleDbType.Varchar2).Value = SplitAccount[4];
                    }
                    else
                    {
                        result.SetValue(string.Empty, 0);
                        result.SetValue(string.Empty, 1);
                        result.SetValue(string.Empty, 2);
                        result.SetValue(string.Empty, 3);
                        result.SetValue(string.Empty, 4);
                        result.SetValue(string.Empty, 5);
                        return result;
                    }

                    OraConn.Open();

                    using (OracleDataReader ODR = OraSelect.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (ODR.HasRows)
                        {
                            ODR.Read();

                            string SenderName = ODR.IsDBNull(0) ? string.Empty : ODR.GetString(0);
                            string NubanNumber = ODR.IsDBNull(1) ? string.Empty : ODR.GetString(1);
                            string CusType = ODR.IsDBNull(2) ? "0" : ODR.GetInt32(2).ToString();
                            string TYPE_OF_DEP = ODR.IsDBNull(3) ? "81" : ODR.GetInt32(3).ToString();
                            string CUS_CLASS = ODR.IsDBNull(4) ? "5" : ODR.GetInt32(4).ToString();
                            string BVN = ODR.IsDBNull(5) ? string.Empty : ODR.GetString(5).ToString();

                            OraConn.Close();
                            result.SetValue(SenderName, 0);
                            result.SetValue(NubanNumber, 1);
                            result.SetValue(CusType, 2);
                            result.SetValue(TYPE_OF_DEP, 3);
                            result.SetValue(CUS_CLASS, 4);
                            result.SetValue(BVN, 5);

                            return result;
                        }
                        else
                        {
                            result.SetValue(string.Empty, 0);
                            result.SetValue(string.Empty, 1);
                            result.SetValue("0", 2);
                            result.SetValue("81", 3);
                            result.SetValue("5", 4);
                            result.SetValue(string.Empty, 5);

                            return result;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Exception when retrieving CustomerDetails : Message - " + ex.Message + "|Stacktrace - " + ex.StackTrace);
            result.SetValue(string.Empty, 0);
            result.SetValue(string.Empty, 1);
            result.SetValue("0", 2);
            result.SetValue("81", 3);
            result.SetValue(string.Empty, 4);
            return result;
        }
    }
}