
/// <summary>
/// Summary description for FingerPrintVerificationRequest
/// </summary>
public class FingerPrintVerificationRequest
{
    public FingerPrintVerificationRequest()
    {
        FingerPrint = new FingerPrintModel();
    }
    public string Nuban { get; set; }
    public FingerPrintModel FingerPrint { get; set; }

    //public override string ToString()
    //{
    //    return base.ToString();
    //}
}

public class FingerImage
{
    public string type { get; set; }
    public string position { get; set; }
    public string nist_impression_type { get; set; }
    public string value { get; set; }
}

public class FingerPrintModel
{
    public FingerPrintModel()
    {
        FingerImage = new FingerImage();
    }
    public string BVN { get; set; }
    public string DeviceId { get; set; }
    public string ReferenceNumber { get; set; }
    public FingerImage FingerImage { get; set; }
}


