using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
//using System.Data.OracleClient;
using Oracle.ManagedDataAccess.Client;
using log4net;

public class USSDCashFastTrackClass
{
    private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public string id { get; set; }
    public string bra_Code { get; set; }
    public string cus_num { get; set; }
    public string cur_code { get; set; }
    public string led_code { get; set; }
    public string sub_acct_code { get; set; }
    public string payeeFullAcctKey { get; set; }
    public string beneFullAcctKey { get; set; }
    public string transType { get; set; }
    public int expl_code { get; set; }
    public string doc_Alp { get; set; }
    public string remark { get; set; }
    public Int64 purchases_ID { get; set; }
    public string status { get; set; }
    public string post_Seq { get; set; }
    public string post_Msg { get; set; }
    public string post_Teller { get; set; }
    public int post_TellerID { get; set; }
    public string post_Supervisor { get; set; }
    public string origBraCode { get; set; }
    public int post_SupervisorID { get; set; }
    public string mobileNumber { get; set; }
    public int validationCode { get; set; }
    public bool validationStatus { get; set; }
    public string validationResponse { get; set; }
    public string tellerTillAcct { get; set; }
    public double tellerCreditLimit { get; set; }
    public double tellerDebitLimit { get; set; }
    public int deb_cred { get; set; }
    public double transAmt { get; set; }
    public string transSeq { get; set; }
    public long transId { get; set; }
    public string BasisConString = string.Empty;
    string purchasesConnString = string.Empty;
    string eOneConnString = string.Empty;
    public USSDCashFastTrackClass()
    {
        purchasesConnString = ConfigurationManager.ConnectionStrings["PurchasesConnString"].ToString();
        BasisConString = ConfigurationManager.AppSettings["BASISConString_eone"];
        eOneConnString = ConfigurationManager.AppSettings["e_oneConnString"].ToString();
    }

    public USSDCashFastTrackClass RetrieveTransByID(long requestID)
    {
        DataTable dtTrans = new DataTable();
        USSDCashFastTrackClass usdCls = new USSDCashFastTrackClass();
        using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
        {
            string query = @"select Id, TransactionType [TransType], MobileNumber, AccountToDebit, AccountToCredit,  Amount,
	        Originator, Remarks, PostTeller from UssdFastTrack where ID = @requestID";

            if (sqlConn.State == ConnectionState.Closed)
            {
                sqlConn.Open();
            }
            SqlCommand sqlComm = new SqlCommand(query, sqlConn);
            sqlComm.CommandType = CommandType.Text ;
            sqlComm.Parameters.AddWithValue("@requestID", requestID);

            SqlDataAdapter sqlAdapter = new SqlDataAdapter(sqlComm);
            sqlAdapter.Fill(dtTrans);


            if (dtTrans != null && dtTrans.Rows.Count > 0)
            {
                usdCls.purchases_ID = Convert.ToInt64(dtTrans.Rows[0]["Id"]);
                usdCls.transType = dtTrans.Rows[0]["TransType"].ToString();
                usdCls.payeeFullAcctKey = dtTrans.Rows[0]["AccountToDebit"].ToString();
                usdCls.beneFullAcctKey = dtTrans.Rows[0]["AccountToCredit"].ToString();
                usdCls.transAmt = Convert.ToInt64(dtTrans.Rows[0]["Amount"]);
                usdCls.remark = dtTrans.Rows[0]["Remarks"].ToString();
                usdCls.post_TellerID = getBasisTellerID(dtTrans.Rows[0]["PostTeller"].ToString());
            }
        }

        return usdCls;
    }

    public DataTable RetrieveTransByMobNum()
    {
        DataTable dtTrans = new DataTable("FastTrack");
        using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
        {
            if (sqlConn.State == ConnectionState.Closed)
            {
                sqlConn.Open();
            }
            SqlCommand sqlComm = new SqlCommand("proc_SearchForCashFastTrack", sqlConn);
            sqlComm.CommandType = CommandType.StoredProcedure;
            sqlComm.Parameters.AddWithValue("@MobileNumber", mobileNumber);
            sqlComm.Parameters.AddWithValue("@TransType", transType);

            SqlDataAdapter sqlAdapter = new SqlDataAdapter(sqlComm);
            sqlAdapter.Fill(dtTrans);
        }

        return dtTrans;
    }


    public DataTable RetrieveTransByMobNum(string mobileNumber)
    {
        DataTable dtTrans = new DataTable();
        using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
        {
            if (sqlConn.State == ConnectionState.Closed)
            {
                sqlConn.Open();
            }
            SqlCommand sqlComm = new SqlCommand("proc_SearchForCashFastTrack", sqlConn);
            sqlComm.CommandType = CommandType.StoredProcedure;
            sqlComm.Parameters.AddWithValue("@MobileNumber", mobileNumber);

            SqlDataAdapter sqlAdapter = new SqlDataAdapter(sqlComm);
            sqlAdapter.Fill(dtTrans);
        }

        return dtTrans;
    }

    public DataTable RetrieveTransForApproval(string origBraCode)
    {
        DataTable dtTrans = new DataTable();
        try
        {
            using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
            {
                if (sqlConn.State == ConnectionState.Closed)
                {
                    sqlConn.Open();
                }
                SqlCommand sqlComm = new SqlCommand("proc_SelectCashFastTrackForApproval", sqlConn);
                sqlComm.CommandType = CommandType.StoredProcedure;
                sqlComm.Parameters.AddWithValue("@OrigBraCode", origBraCode);

                SqlDataAdapter sqlAdapter = new SqlDataAdapter(sqlComm);
                sqlAdapter.Fill(dtTrans);
            }

            return dtTrans;
        }
        catch (Exception ex)
        {
             Log.Fatal("Database Call Failed ", ex);
            throw;
        }
    }
  
    public List<string> getAcctsForFastTrack(string phoneNumber)
    {
        List<string> lstAccts = new List<string>();

        DataTable dtTrans = new DataTable();

        try
        {
            using (OracleConnection OraConn = new OracleConnection(ConfigurationManager.AppSettings["GENSConString"]))
            {
                using (OracleCommand OraSelect = new OracleCommand())
                {
                    if (OraConn.State == ConnectionState.Closed)
                    {
                        OraConn.Open();
                    }
                    OraSelect.Connection = OraConn;
                    string selectquery = "select bra_code,cus_num,cur_code, led_code,sub_acct_code, availbal avail_bal from ussd_account_vw where primary_mobileno =  '" + phoneNumber + "' order by led_code asc";

                    OraSelect.CommandText = selectquery;
                    OraSelect.CommandType = CommandType.Text;

                    OracleDataAdapter sqlAdapter = new OracleDataAdapter(OraSelect);
                    sqlAdapter.Fill(dtTrans);
                }
            }
            if (dtTrans != null && dtTrans.Rows.Count > 0)
            {
                string acctNumber = string.Empty;
                foreach (DataRow dwRow in dtTrans.Rows)
                {
                    acctNumber = string.Format("{0}/{1}/{2}/{3}/{4}", dwRow["bra_code"], dwRow["cus_num"], dwRow["cur_code"], dwRow["led_code"], dwRow["sub_acct_code"]);
                    lstAccts.Add(acctNumber);
                }          
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
        }
        return lstAccts;
    }

    public int UpdatePostingTeller()
    {
        int result = -1;
        try
        {
            using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
            {
                if (sqlConn.State == ConnectionState.Closed)
                {
                    sqlConn.Open();
                }
                SqlCommand sqlComm = new SqlCommand("proc_UpdateFastTrackTeller", sqlConn);
                sqlComm.CommandType = CommandType.StoredProcedure;
                sqlComm.Parameters.AddWithValue("@ID", id);
                sqlComm.Parameters.AddWithValue("@Status", status);
                sqlComm.Parameters.AddWithValue("@PostingTeller", post_Teller);
                sqlComm.Parameters.AddWithValue("@PinpadTransId", transId); 
                    
                result = Convert.ToInt32(sqlComm.ExecuteScalar());
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
        }
        return result;
    }

    public int UpdatePostingSupervisor()
    {
        int result = -1;

        try
        {
            using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
            {
                if (sqlConn.State == ConnectionState.Closed)
                {
                    sqlConn.Open();
                }

                SqlCommand sqlComm = new SqlCommand("proc_UpdateFastTrackSupervisor", sqlConn);
                sqlComm.CommandType = CommandType.StoredProcedure;
                sqlComm.Parameters.AddWithValue("@ID", id);
                sqlComm.Parameters.AddWithValue("@Status", status);
                sqlComm.Parameters.AddWithValue("@PostingSupervisor", post_Supervisor);

                result = Convert.ToInt32(sqlComm.ExecuteScalar());
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
        }

        return result;
    }

    public long UpdatePosting(long purchasesID, string status, string postSeq, string postMsg, string postTeller, string postSupervisor, string acctToDebit, string acctToCredit, string remark)
    {
        long result = -1;

        try
        {
            using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
            {
                if (sqlConn.State == ConnectionState.Closed)
                {
                    sqlConn.Open();
                }

                SqlCommand sqlComm = new SqlCommand("proc_UpdateUSSDFastTrack", sqlConn);
                sqlComm.CommandType = CommandType.StoredProcedure;
                sqlComm.Parameters.AddWithValue("@ID", purchasesID);
                sqlComm.Parameters.AddWithValue("@Status", status);
                sqlComm.Parameters.AddWithValue("@PostingSeq", postSeq);
                sqlComm.Parameters.AddWithValue("@PostingMessage", postMsg);
                sqlComm.Parameters.AddWithValue("@PostingTeller", postTeller);
                sqlComm.Parameters.AddWithValue("@Remark", remark);
                sqlComm.Parameters.AddWithValue("@PostingSupervisor", postSupervisor);
                sqlComm.Parameters.AddWithValue("@AcctToDebit", acctToDebit);
                sqlComm.Parameters.AddWithValue("@AcctToCredit", acctToCredit);

                result = Convert.ToInt64(sqlComm.ExecuteScalar());
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
        }

        return result;
    }

    public int UpdateFastTrackForApproval(long purchasesID, int origBraCode, string postTeller, string approvalReason, string acctToDebit, string acctToCredit, string remark)
    {
        int result = -1;

        try
        {
            using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
            {
                if (sqlConn.State == ConnectionState.Closed)
                {
                    sqlConn.Open();
                }

                SqlCommand sqlComm = new SqlCommand("proc_UpdateUSSDFastTrackForApproval", sqlConn);
                sqlComm.CommandType = CommandType.StoredProcedure;
                sqlComm.Parameters.AddWithValue("@ID", purchasesID);
                sqlComm.Parameters.AddWithValue("@OrigBraCode", origBraCode);
                sqlComm.Parameters.AddWithValue("@Approve", true);
                sqlComm.Parameters.AddWithValue("@Remark", remark);
                sqlComm.Parameters.AddWithValue("@PostingTeller", postTeller);
                sqlComm.Parameters.AddWithValue("@ApprovalReason", approvalReason);
                sqlComm.Parameters.AddWithValue("@AcctToDebit", acctToDebit);
                sqlComm.Parameters.AddWithValue("@AcctToCredit", acctToCredit);

                result = Convert.ToInt16(sqlComm.ExecuteScalar());
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
        }

        return result;
    }
 
    public int UpdateUSSDFastTrackAfterApproval(long purchasesID, string postSeq, string postMsg, string postSupervisor, string status, bool approve)
    {
        int result = -1;

        try
        {
            using (SqlConnection sqlConn = new SqlConnection(purchasesConnString))
            {
                if (sqlConn.State == ConnectionState.Closed)
                {
                    sqlConn.Open();
                }
               
                SqlCommand sqlComm = new SqlCommand("proc_UpdateUSSDFastTrackAfterApproval", sqlConn);
                sqlComm.CommandType = CommandType.StoredProcedure;
                sqlComm.Parameters.AddWithValue("@ID", purchasesID);
                sqlComm.Parameters.AddWithValue("@approve", approve);
                sqlComm.Parameters.AddWithValue("@Status", status);
                sqlComm.Parameters.AddWithValue("@PostingSeq", postSeq);
                sqlComm.Parameters.AddWithValue("@PostingMessage", postMsg);
                sqlComm.Parameters.AddWithValue("@PostingSupervisor", postSupervisor);

                result = Convert.ToInt16(sqlComm.ExecuteScalar());
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
        }

        return result;
    }

    public int CheckDebitFastTrackTransactionExist(int bra_code, int cus_num, int cur_code, int led_code, int sub_acct_code, double tra_amt, int expl_code, string docAlp, string remark, out string tranSeq) //string tra_date,
    {
        int Result = 0;
        tranSeq = string.Empty;

        using (OracleConnection oraconn = new OracleConnection(BasisConString))
        {
            try
            {
                string query_str = string.Format("Select tra_seq1 from Tell_Act where bra_code = :braCode and cus_num = :cusNum and cur_code = :curCode and led_code = :ledCode and sub_acct_code = :subAcctCode and tra_amt = :traAmt and deb_cre_ind =1 and expl_code = :explCode and Doc_Alp = :docAlp and remarks like '%'+ :remark +'%'", remark);

                OracleCommand cmd = new OracleCommand(query_str, oraconn);
                cmd.CommandType = CommandType.Text;

                if (oraconn.State == ConnectionState.Closed)
                {
                    oraconn.Open();
                }

                cmd.Parameters.Add(":braCode", OracleDbType.Varchar2, 3).Value = bra_code;
                cmd.Parameters.Add(":cusNum", OracleDbType.Varchar2, 7).Value = cus_num;
                cmd.Parameters.Add(":curCode", OracleDbType.Varchar2, 2).Value = cur_code;
                cmd.Parameters.Add(":ledCode", OracleDbType.Varchar2, 2).Value = led_code;
                cmd.Parameters.Add(":subAcctCode", OracleDbType.Varchar2, 2).Value = sub_acct_code;
                cmd.Parameters.Add(":traAmt", OracleDbType.Varchar2, 2).Value = tra_amt;
                cmd.Parameters.Add(":explCode", OracleDbType.Varchar2, 2).Value = expl_code;
                cmd.Parameters.Add(":docAlp", OracleDbType.Varchar2, 2).Value = docAlp;
                cmd.Parameters.Add(":remark", OracleDbType.Varchar2, 2).Value = remark;

                OracleDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tranSeq = reader["tra_seq1"] == DBNull.Value ? string.Empty : reader["tra_seq1"].ToString();
                    }
                }
                Result = reader.Cast<object>().Count();
            }
            catch (Exception ex)
            {
                Log.Fatal("Database Call Failed ", ex);
                Result = -1;
            }
            finally
            {
                oraconn.Close();
            }
        }
        return Result;
    }

    public int CheckCreditFastTrackTransactionExist(int bra_code, int cus_num, int cur_code, int led_code, int sub_acct_code, double tra_amt, int expl_code, string docAlp, string remark, out string tranSeq) //string tra_date,
    {
        int Result = 0;
        tranSeq = string.Empty;

        using (OracleConnection oraconn = new OracleConnection(BasisConString))
        {
            try
            {
                string query_str = string.Format("Select tra_seq1 from Tell_Act where bra_code = :braCode and cus_num = :cusNum and cur_code = :curCode and led_code = :ledCode and sub_acct_code = :subAcctCode and tra_amt = :traAmt and deb_cre_ind =2 and expl_code = :explCode and Doc_Alp = :docAlp and remarks like '%'+ :remark +'%'", remark);

                OracleCommand cmd = new OracleCommand(query_str, oraconn);
                cmd.CommandType = CommandType.Text;

                if (oraconn.State == ConnectionState.Closed)
                {
                    oraconn.Open();
                }

                cmd.Parameters.Add(":braCode", OracleDbType.Varchar2, 3).Value = bra_code;
                cmd.Parameters.Add(":cusNum", OracleDbType.Varchar2, 7).Value = cus_num;
                cmd.Parameters.Add(":curCode", OracleDbType.Varchar2, 2).Value = cur_code;
                cmd.Parameters.Add(":ledCode", OracleDbType.Varchar2, 2).Value = led_code;
                cmd.Parameters.Add(":subAcctCode", OracleDbType.Varchar2, 2).Value = sub_acct_code;
                cmd.Parameters.Add(":traAmt", OracleDbType.Varchar2, 2).Value = tra_amt;
                cmd.Parameters.Add(":explCode", OracleDbType.Varchar2, 2).Value = expl_code;
                cmd.Parameters.Add(":docAlp", OracleDbType.Varchar2, 2).Value = docAlp;
                cmd.Parameters.Add(":remark", OracleDbType.Varchar2, 2).Value = remark;

                OracleDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tranSeq = reader["tra_seq1"] == DBNull.Value ? string.Empty : reader["tra_seq1"].ToString();
                    }
                }
                Result = reader.Cast<object>().Count();
            }
            catch (Exception ex)
            {
                Log.Fatal("Database Call Failed ", ex);
                Result = -1;
            }
            finally
            {
                oraconn.Close();
            }
        }
        return Result;
    }

    int getBasisTellerID(string tellerDomainID)
    {
        int result = -1;

        try
        {
            string query = "Select basis_id from admin_users where user_id = @user_id";

            using (SqlConnection sqlConn = new SqlConnection(eOneConnString))
            {
                if (sqlConn.State == ConnectionState.Closed)
                {
                    sqlConn.Open();
                }
                SqlCommand sqlComm = new SqlCommand(query, sqlConn);
                sqlComm.CommandType = CommandType.Text;
                sqlComm.Parameters.AddWithValue("@user_id", tellerDomainID);

                SqlDataReader reader = sqlComm.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result = reader[0] == DBNull.Value ? -1 : Convert.ToInt16(reader[0]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Database Call Failed ", ex);
        }
        return result;
    }

    public string decodeCustomerName(string xmlString)
    {
        XmlDocument document = null;
        XPathNavigator navigator = null;
        XPathNodeIterator snodes = null;
        string retcode = null;

        document = new XmlDocument();
        document.LoadXml(xmlString);
        navigator = document.CreateNavigator();
        snodes = navigator.Select("/Response/CODE");
        snodes.MoveNext();
        retcode = snodes.Current.Value;

        if (retcode != "1000")
        {
            snodes = navigator.Select("/Response/Error");
            snodes.MoveNext();
            retcode = snodes.Current.Value;
        }
        else
        {
            snodes = navigator.Select("/Response/CUSTOMERNAME");
            snodes.MoveNext();
            return snodes.Current.Value;
        }

        return retcode;
    }
}