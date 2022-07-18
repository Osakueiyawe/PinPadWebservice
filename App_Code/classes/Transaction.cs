using System;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
//using System.Data.OracleClient;
using Oracle.ManagedDataAccess.Client;

class Transaction
{
    public static DateTime postdate = DateTime.Now;
    SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]);
    SqlConnection PinPadSeqConn = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]);
    SqlConnection PinPadConOffline = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]);

    public ulong InitiateTransaction(string OrigbraCode, string tellerId, string customerAccountNo, decimal amount, string authmode, string transType, string cusName, string tellertill, string depname)// third party deposits Offline Transactions
    {
        object result = 0;

        try
        {
            using (SqlCommand sqlcomm = new SqlCommand())
            {

                using (SqlConnection PinPadConOffline = new SqlConnection(ConfigurationManager.AppSettings["PinPadConOffline"]))
                {
                    sqlcomm.Connection = PinPadConOffline;
                    sqlcomm.Parameters.Add("@CustomerNo", SqlDbType.NChar).Value = customerAccountNo;
                    sqlcomm.Parameters.Add("@Transtype", SqlDbType.NChar).Value = transType;
                    sqlcomm.Parameters.Add("@OriginatingTellerId", SqlDbType.NChar).Value = tellerId;
                    sqlcomm.Parameters.Add("@AuthenticationMode", SqlDbType.NChar).Value = authmode;
                    sqlcomm.Parameters.Add("@TransAmount", SqlDbType.Decimal).Value = amount;
                    sqlcomm.Parameters.Add("@OriginatingBraCode", SqlDbType.NChar).Value = OrigbraCode;
                    sqlcomm.Parameters.Add("@CustomerName", SqlDbType.NChar).Value = cusName;
                    sqlcomm.Parameters.Add("@TellerTillAcct", SqlDbType.NChar).Value = tellertill;
                    sqlcomm.Parameters.Add("@DepositorName", SqlDbType.NChar).Value = depname;
                    sqlcomm.CommandText = "spInsertForApproval";
                    sqlcomm.CommandType = CommandType.StoredProcedure;

                    if (PinPadConOffline.State == ConnectionState.Closed)
                    {
                        PinPadConOffline.Open();
                    }

                    result = sqlcomm.ExecuteScalar();

                    return Convert.ToUInt64(result);
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return 0;
        }
        finally
        {
            if (PinPadConOffline.State == ConnectionState.Open)
            {
                PinPadConOffline.Close();
            }
        }
    }

    public ulong InitiateTransaction(string OrigbraCode, string tellerId, string customerAccountNo, decimal amount, string authmode, string transType, string cusName, string tellertill, string depname, string CardNumber, uint Stan, string MerchantID, string AuthCode, decimal AuthAmount, string cashanalysis) // Online Transactions
    {
        object result = 0;

        try
        {
            using (SqlCommand sqlcomm = new SqlCommand())
            {
                using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
                {
                    sqlcomm.Connection = PinPadCon;
                    sqlcomm.Parameters.Add("@CustomerNo", SqlDbType.NChar).Value = customerAccountNo;
                    sqlcomm.Parameters.Add("@Transtype", SqlDbType.NChar).Value = transType;
                    sqlcomm.Parameters.Add("@OriginatingTellerId", SqlDbType.NChar).Value = tellerId;
                    sqlcomm.Parameters.Add("@AuthenticationMode", SqlDbType.NChar).Value = authmode;
                    sqlcomm.Parameters.Add("@TransAmount", SqlDbType.Decimal).Value = amount;
                    sqlcomm.Parameters.Add("@OriginatingBraCode", SqlDbType.NChar).Value = OrigbraCode;
                    sqlcomm.Parameters.Add("@CustomerName", SqlDbType.NChar).Value = cusName;
                    sqlcomm.Parameters.Add("@TellerTillAcct", SqlDbType.NChar).Value = tellertill;
                    sqlcomm.Parameters.Add("@DepositorName", SqlDbType.NChar).Value = depname;
                    sqlcomm.Parameters.Add("@CardNumber", SqlDbType.NChar).Value = CardNumber;
                    sqlcomm.Parameters.Add("@Stan", SqlDbType.Int).Value = Stan;
                    sqlcomm.Parameters.Add("@MerchantID", SqlDbType.NChar).Value = MerchantID;
                    sqlcomm.Parameters.Add("@AuthCode", SqlDbType.NChar).Value = authmode;
                    sqlcomm.Parameters.Add("@AuthAmount", SqlDbType.Decimal).Value = AuthAmount;
                    sqlcomm.Parameters.Add("@AnalysisOfCash", SqlDbType.NChar).Value = cashanalysis;

                    sqlcomm.CommandText = "spInsertForApprovalOnline";
                    sqlcomm.CommandType = CommandType.StoredProcedure;

                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }

                    result = sqlcomm.ExecuteScalar();

                    return Convert.ToUInt64(result);
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return 0;
        }
        finally
        {
            if (PinPadCon.State == ConnectionState.Open)
            {
                PinPadCon.Close();
            }
        }
    }

    public ulong UpdateTransaction(string apprvTeller, ulong transId, string bracode, string acctno, string rmks, string status, string failreason, string authmode, string tellertill, string expl_code)
    {
        try
        {
            BASIS basis = new BASIS();
            string basisSequence = "";

            if (authmode.Equals("Online"))
            {

                if (failreason.Contains("REJECTED"))
                {
                    basisSequence = "NO RECORD";
                }
                else
                {
                    basisSequence = basis.getTranSeq(bracode, acctno, rmks, expl_code);
                }
                if (basisSequence.Equals("0")) // transaction do not exist
                {
                    basisSequence = "NO RECORD";
                }
            }

            object result = 0;

            using (SqlCommand sqlcommUpdate = new SqlCommand())
            {
                if (authmode.Equals("Online"))
                {
                    sqlcommUpdate.Connection = PinPadCon;
                }
                else
                {
                    sqlcommUpdate.Connection = PinPadConOffline;
                }

                sqlcommUpdate.Parameters.Add("@ApprovingTellerId", SqlDbType.NChar).Value = apprvTeller;
                sqlcommUpdate.Parameters.Add("@BasisSequence", SqlDbType.NChar).Value = basisSequence;
                sqlcommUpdate.Parameters.Add("@TransactionID", SqlDbType.Int).Value = transId;
                sqlcommUpdate.Parameters.Add("@TransStatus", SqlDbType.NChar).Value = status;
                sqlcommUpdate.Parameters.Add("@FailureReason", SqlDbType.NChar).Value = failreason;
                sqlcommUpdate.Parameters.Add("@TellerTillAccount", SqlDbType.NChar).Value = tellertill;


                sqlcommUpdate.CommandText = "UpdatePosting";
                sqlcommUpdate.CommandType = CommandType.StoredProcedure;

                if (authmode.Equals("Online"))
                {
                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }
                }
                else
                {
                    if (PinPadConOffline.State == ConnectionState.Closed)
                    {
                        PinPadConOffline.Open();
                    }
                }
                result = sqlcommUpdate.ExecuteScalar();

                if (Convert.ToUInt64(result) != 1)// succesful but couldnt update local DB with success status. Log for the service to update. But allow transaction to go through.
                {
                    return 0;
                }
                else
                {
                    if (authmode.Equals("Online"))
                    {
                        return Convert.ToUInt64(basisSequence);
                    }
                    else
                    {
                        return Convert.ToUInt64(result);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return 0;
        }
        finally
        {
            if (authmode.Equals("Online"))
            {
                if (PinPadCon.State == ConnectionState.Open)
                {
                    PinPadCon.Close();
                }
            }
            else
            {
                if (PinPadConOffline.State == ConnectionState.Open)
                {
                    PinPadConOffline.Close();
                }
            }
        }
    }

    public ulong UpdateTransactionForApproval(ulong transId)
    {
        try
        {
            object result = 0;

            using (SqlCommand sqlcommUpdate = new SqlCommand())
            {

                using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
                {
                    sqlcommUpdate.Connection = PinPadCon;
                    sqlcommUpdate.Parameters.Add("@TransactionID", SqlDbType.Int).Value = transId;
                    sqlcommUpdate.CommandText = "UpdateForApproval";
                    sqlcommUpdate.CommandType = CommandType.StoredProcedure;

                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }

                    result = sqlcommUpdate.ExecuteScalar();

                    if (Convert.ToUInt64(result) != 1)// succesful but couldnt update local DB with success status. Log for the service to update. But allow transaction to go through.
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }
        }


        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return 0;
        }
        finally
        {
            if (PinPadCon.State == ConnectionState.Open)
            {
                PinPadCon.Close();
            }
        }
    }

    public ulong UpdateTransactionForReceiptReprint(ulong transId)
    {
        try
        {
            object result = 0;

            using (SqlCommand sqlcommUpdate = new SqlCommand())
            {
                using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
                {
                    sqlcommUpdate.Connection = PinPadCon;
                    sqlcommUpdate.Parameters.Add("@TransactionID", SqlDbType.Int).Value = transId;
                    sqlcommUpdate.CommandText = "UpdateForReceiptReprint";
                    sqlcommUpdate.CommandType = CommandType.StoredProcedure;

                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }

                    result = sqlcommUpdate.ExecuteScalar();

                    if (Convert.ToUInt64(result) != 1)// succesful but couldnt update local DB with success status. Log for the service to update. But allow transaction to go through.
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return 0;
        }
        finally
        {
            if (PinPadCon.State == ConnectionState.Open)
            {
                PinPadCon.Close();
            }
        }
    }

    public bool UpdatePrintStatus(ulong transsequence, int printstatus)
    {
        try
        {
            object result = 0;

            using (SqlCommand sqlcomm = new SqlCommand())
            {
                using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
                {
                    sqlcomm.Connection = PinPadCon;// WebConfigurationManager.ConnectionStrings["VisaCon"].ConnectionString; //"Data Source=10.2.6.225;Initial Catalog=FOPS_2;Persist Security Info=True;User ID=sa;password=sapassword"\

                    sqlcomm.CommandText = "UpdatePrintStatus";
                    sqlcomm.CommandType = CommandType.StoredProcedure;

                    sqlcomm.Parameters.AddWithValue("@printstatus", printstatus);
                    sqlcomm.Parameters.AddWithValue("@transactionseq", transsequence).DbType = DbType.Int64;

                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }
                    result = sqlcomm.ExecuteNonQuery();
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return false;
        }
        finally
        {
            PinPadCon.Close();
        }
    }

    public bool InsertIntoCentralDB(int TransactionID, string CustomerNo, string Transtype, string OriginatingTellerId, string AuthenticationMode, double TransAmount, string ApprovingTellerID, string OriginatingBraCode, string TransactionStatus, string BasisTransSequence, string CustomerName, DateTime TransDate, string TellerTillAccount, string FailReason, bool PrintStatus, string DepositorName)
    {
        using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
        {
            try
            {
                using (SqlCommand sqlcomm = new SqlCommand())
                {
                    sqlcomm.Connection = PinPadCon;
                    sqlcomm.Parameters.Add("@CustomerNo", SqlDbType.NChar).Value = CustomerNo;
                    sqlcomm.Parameters.Add("@Transtype", SqlDbType.NChar).Value = Transtype;
                    sqlcomm.Parameters.Add("@OriginatingTellerId", SqlDbType.NChar).Value = OriginatingTellerId;
                    sqlcomm.Parameters.Add("@AuthenticationMode", SqlDbType.NChar).Value = AuthenticationMode;
                    sqlcomm.Parameters.Add("@TransAmount", SqlDbType.Decimal).Value = TransAmount;
                    sqlcomm.Parameters.Add("@ApprovingTellerID", SqlDbType.NChar).Value = ApprovingTellerID;
                    sqlcomm.Parameters.Add("@OriginatingBraCode", SqlDbType.NChar).Value = OriginatingBraCode;
                    sqlcomm.Parameters.Add("@TransactionStatus", SqlDbType.NChar).Value = TransactionStatus;
                    sqlcomm.Parameters.Add("@BasisTransSequence", SqlDbType.NChar).Value = BasisTransSequence;
                    sqlcomm.Parameters.Add("@CustomerName", SqlDbType.NChar).Value = CustomerName;
                    sqlcomm.Parameters.Add("@TransDate", SqlDbType.DateTime).Value = TransDate;
                    sqlcomm.Parameters.Add("@TellerTillAccount", SqlDbType.NChar).Value = TellerTillAccount;

                    sqlcomm.Parameters.Add("@FailReason", SqlDbType.NChar).Value = FailReason;
                    sqlcomm.Parameters.Add("@PrintStatus", SqlDbType.Bit).Value = PrintStatus;
                    sqlcomm.Parameters.Add("@DepositorName", SqlDbType.NChar).Value = DepositorName;


                    sqlcomm.CommandText = "usp_TransactionsInsert";
                    sqlcomm.CommandType = CommandType.StoredProcedure;

                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }
                    int result = sqlcomm.ExecuteNonQuery();

                    if (result > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                return false;
            }
            finally
            {

                if (PinPadCon.State == ConnectionState.Open) { PinPadCon.Close(); }
            }
        }
    }

    public bool IsInProgress(ulong transid)
    {
        ulong InProgressValue = 0;
        try
        {
            using (SqlCommand sqlcomm = new SqlCommand())
            {
                using (SqlConnection PinPadCon = new SqlConnection(ConfigurationManager.AppSettings["PinPadCon"]))
                {
                    sqlcomm.Connection = PinPadCon;


                    sqlcomm.CommandText = "usp_checkIfInProgress";
                    sqlcomm.Parameters.Add("@TransactionID", SqlDbType.Int).Value = transid;
                    sqlcomm.CommandType = CommandType.StoredProcedure;
                    //offline transaction
                    if (PinPadCon.State == ConnectionState.Closed)
                    {
                        PinPadCon.Open();
                    }

                    using (SqlDataReader result = sqlcomm.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (result.Read())
                        {
                            InProgressValue = Convert.ToUInt64(result["Inprogress"]);

                            if (InProgressValue == 1)
                            {
                                return true;
                            }
                            else
                            {
                                return false;

                            }

                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            return false;
        }
    }

    public DataTable getOnlineDepositPendingData(string bra_code)
    {
        DataTable dtselect = null;
        string conString = ConfigurationManager.AppSettings["PinPadCon"];//.ConnectionString;
        using (SqlConnection sqlcon = new SqlConnection(conString))
        {
            string sqlcommand = "";

            try
            {
                sqlcommand = "spGetOnlinePendingDepositData";
                sqlcon.Open();
                SqlDataAdapter daSelect = new SqlDataAdapter();

                daSelect = new SqlDataAdapter(sqlcommand, conString);
                daSelect.SelectCommand.CommandTimeout = 0;
                daSelect.SelectCommand.CommandType = CommandType.StoredProcedure;
                daSelect.SelectCommand.Parameters.AddWithValue("@OriginatingBraCode", bra_code);
                daSelect.SelectCommand.Parameters.AddWithValue("@Transtype", "DEPOSIT");
                dtselect = new DataTable();
                daSelect.Fill(dtselect);
                dtselect.TableName = "DepositPending";
                return dtselect;
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                dtselect.TableName = "DepositPending";
                return dtselect;
            }
            finally
            {
                sqlcon.Close();
            }
        }
    }

    public DataTable getOnlineWithdrawalPendingData(string bra_code)
    {
        DataTable dtselect = null;
        string conString = ConfigurationManager.AppSettings["PinPadCon"];
        using (SqlConnection sqlcon = new SqlConnection(conString))
        {

            try
            {
                string sqlcommand = "spGetOnlinePendingData";
                sqlcon.Open();
                SqlDataAdapter daSelect = new SqlDataAdapter();

                daSelect = new SqlDataAdapter(sqlcommand, conString);
                daSelect.SelectCommand.CommandTimeout = 0;
                daSelect.SelectCommand.CommandType = CommandType.StoredProcedure;
                daSelect.SelectCommand.Parameters.AddWithValue("@OriginatingBraCode", bra_code);
                daSelect.SelectCommand.Parameters.AddWithValue("@Transtype", "WITHDRAWAL");

                dtselect = new DataTable();

                daSelect.Fill(dtselect);
                dtselect.TableName = "WithdrawalPending";
                return dtselect;
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                dtselect.TableName = "WithdrawalPending";
                return dtselect;
            }
            finally
            {
                sqlcon.Close();
            }
        }
    }

    public DataTable getOnlinePendingPrintingData(string teller_id)
    {
        string conString = ConfigurationManager.AppSettings["PinPadCon"];
        SqlConnection sqlcon = new SqlConnection(conString);
        DataTable dtselect = null;
        Utilities util = new Utilities();
        try
        {
            string sqlcommand = "SELECT  [TransactionID],TellerTillAccount ,[CustomerNo],[Transtype],[OriginatingTellerId],[AuthenticationMode],[TransAmount],[OriginatingBraCode] ,[TransactionStatus],[CustomerName],DepositorName,[TransDate] FROM [PinPad].[dbo].[Transactions] WHERE TransactionStatus = 'APPROVED' and AuthenticationMode = 'Online' AND PrintStatus = 0 and OriginatingTellerId =  @tellerid and TransDate between dateadd(day,datediff(day,0,GETDATE()),0) and dateadd(day,datediff(day,-1,GETDATE()),0) order by transdate desc";
            sqlcon.Open();
            SqlDataAdapter daSelect = new SqlDataAdapter();
            dtselect = new DataTable();
            daSelect = new SqlDataAdapter(sqlcommand, conString);
            daSelect.SelectCommand.CommandTimeout = 0;
            daSelect.SelectCommand.Parameters.AddWithValue("@tellerid", teller_id);
            daSelect.Fill(dtselect);
            dtselect.TableName = "PrintingPending";
            return dtselect;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            dtselect.TableName = "PrintingPending";
            return dtselect;
        }
        finally
        {

            sqlcon.Close();
        }
    }

    public DataTable getReceiptResetPendingData(string startdate, string acctno, string traamt)
    {
        Utilities util = new Utilities();
        DataTable dtselect = null;
        string conString = ConfigurationManager.AppSettings["PinPadCon"];
        SqlConnection sqlcon = new SqlConnection(conString);

        try
        {
            dtselect = new DataTable();
            string sqlcommand = "spGetReceiptForReprint";
            sqlcon.Open();
            SqlDataAdapter daSelect = new SqlDataAdapter();

            daSelect = new SqlDataAdapter(sqlcommand, conString);
            daSelect.SelectCommand.CommandTimeout = 0;
            daSelect.SelectCommand.CommandType = CommandType.StoredProcedure;
            daSelect.SelectCommand.Parameters.AddWithValue("@customerno", acctno);
            daSelect.SelectCommand.Parameters.AddWithValue("@tradate", startdate);
            daSelect.SelectCommand.Parameters.AddWithValue("@traamount", Convert.ToDecimal(traamt));
            daSelect.Fill(dtselect);
            dtselect.TableName = "ReprintPending";
            return dtselect;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            dtselect.TableName = "ReprintPending";
            return dtselect;
        }
        finally
        {
            sqlcon.Close();
        }
    }

    public DataTable getPinPadTransactionHistory(string startdate, string enddate, string transtype, string bracode, string tellerid)
    {
        DataTable dtselect = null;

        string squery = "";
        if (transtype.ToUpper().Equals("ALL"))
        {

            squery = "%";
        }
        else if (transtype.ToUpper().Equals("SUCCESS"))
        {

            squery = "APPROVED";
        }
        if (transtype.ToUpper().Equals("FAILED"))
        {

            squery = "FAILED";
        }
        string conString = ConfigurationManager.AppSettings["PinPadCon"];
        SqlConnection sqlcon = new SqlConnection(conString);

        try
        {
            dtselect = new DataTable();
            string sqlcommand = "spGetTransactionHistory";
            sqlcon.Open();
            SqlDataAdapter daSelect = new SqlDataAdapter();

            daSelect = new SqlDataAdapter(sqlcommand, conString);
            daSelect.SelectCommand.CommandTimeout = 0;
            daSelect.SelectCommand.CommandType = CommandType.StoredProcedure;
            daSelect.SelectCommand.Parameters.AddWithValue("@OriginatingBraCode", bracode);
            daSelect.SelectCommand.Parameters.AddWithValue("@OriginatingTellerId", tellerid);
            daSelect.SelectCommand.Parameters.AddWithValue("@startdate", startdate);
            daSelect.SelectCommand.Parameters.AddWithValue("@enddate", enddate);
            daSelect.SelectCommand.Parameters.AddWithValue("@status", squery);

            daSelect.Fill(dtselect);
            dtselect.TableName = "HistoryPending";
            return dtselect;
        }

        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
            dtselect.TableName = "HistoryPending";
            return dtselect;
        }
        finally
        {

            sqlcon.Close();
        }
    }

    public DataTable getOIOpportunities(string bra_code, string cus_num)
    {
        DataTable dtselect = null;
        Utilities util = new Utilities();
        string conString = ConfigurationManager.AppSettings["EXADATAconstring"];

        using (OracleConnection sqlcon = new OracleConnection(conString))
        {
            try
            {
                dtselect = new DataTable();
                string sqlcommand = "select  * from oi_location_intel where bra_code = " + util.SafeSqlLiteral(bra_code, 1) + " and cus_num = " + util.SafeSqlLiteral(cus_num, 1) + " and (feedback is null or length(feedback) < 3)";
                sqlcon.Open();
                OracleDataAdapter daSelect = new OracleDataAdapter();

                daSelect = new OracleDataAdapter(sqlcommand, conString);
                daSelect.SelectCommand.CommandTimeout = 0;
                daSelect.SelectCommand.CommandType = CommandType.Text;

                daSelect.Fill(dtselect);
                dtselect.TableName = "OI";
                return dtselect;

            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                dtselect.TableName = "OI";
                return dtselect;
            }
            finally
            {
                sqlcon.Close();
            }
        }
    }

    public string UpdateOIOpportunities(string bra_code, string cus_num, string feedback, string tellerid)
    {
        DataTable dtselect = null;
        Utilities util = new Utilities();
        int result = -1;
        string conString = ConfigurationManager.AppSettings["EXADATAconstring"];
        using (OracleConnection sqlcon = new OracleConnection(conString))
        {
            try
            {
                dtselect = new DataTable();
                string sqlcommUpdateText = "update oi_location_intel set feedback = '" + util.SafeSqlLiteral(feedback, 1) + "', teller = " + util.SafeSqlLiteral(tellerid, 1) + "  where bra_code = " + util.SafeSqlLiteral(bra_code, 1) + " and cus_num = " + util.SafeSqlLiteral(cus_num, 1);
                sqlcon.Open();
                // SqlDataAdapter daSelect = new SqlDataAdapter();
                OracleCommand sqlcommUpdate = new OracleCommand();
                sqlcommUpdate.Connection = sqlcon;
                sqlcommUpdate.CommandText = sqlcommUpdateText;
                sqlcommUpdate.CommandType = CommandType.Text;

                result = Convert.ToInt16(sqlcommUpdate.ExecuteScalar());

            }

            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                dtselect.TableName = "OI";

            }
            finally
            {
                if (sqlcon.State == ConnectionState.Open)
                {
                    sqlcon.Close();
                }
            }

        }

        return result.ToString();

    }

    public string UpdateOISelectionbyTeller(string bra_code, string cus_num, string tellerid)
    {
        DataTable dtselect = null;
        Utilities util = new Utilities();
        int result = -1;
        string conString = ConfigurationManager.AppSettings["EXADATAconstring"];
        using (OracleConnection sqlcon = new OracleConnection(conString))
        {
            try
            {
                dtselect = new DataTable();
                string sqlcommUpdateText = "update oi_location_intel set teller = '" + util.SafeSqlLiteral(tellerid, 1) + "'  where bra_code = " + util.SafeSqlLiteral(bra_code, 1) + " and cus_num = " + util.SafeSqlLiteral(cus_num, 1);
                sqlcon.Open();
                // SqlDataAdapter daSelect = new SqlDataAdapter();
                OracleCommand sqlcommUpdate = new OracleCommand();
                sqlcommUpdate.Connection = sqlcon;
                sqlcommUpdate.CommandText = sqlcommUpdateText;
                sqlcommUpdate.CommandType = CommandType.Text;

                result = Convert.ToInt16(sqlcommUpdate.ExecuteScalar());

            }

            catch (Exception ex)
            {
                Utility.Log().Fatal("Database Call Failed ", ex);
                dtselect.TableName = "OI";

            }
            finally
            {
                if (sqlcon.State == ConnectionState.Open)
                {
                    sqlcon.Close();
                }
            }

        }

        return result.ToString();

    }
}