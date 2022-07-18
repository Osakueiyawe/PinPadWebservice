using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for TransactionDetail
/// </summary>
public class TransactionDetail
{
    public TransactionDetail()
    {
        AccountList = new List<TransactionDetail>();
    }

    public string CustomerName { get; set; }
    public string BranchCode { get; set; }
    public string CustomerNumber { get; set; }
    public string LedgerCode { get; set; }
    public string CurrencyCode { get; set; }
    public string SubAccountCode { get; set; }
    public string CustomerNuban { get; set; }
    public string SourceRestriction { get; set; }
    public decimal Balance { get; set; }
    public int ResponseCode { get; set; }
    public string OldAccountNumber { get; set; }
    public string BVN { get; set; }

    public List<TransactionDetail> AccountList { get; set; }
}