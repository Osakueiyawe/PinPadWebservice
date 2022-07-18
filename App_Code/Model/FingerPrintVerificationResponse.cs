
/// <summary>
/// Summary description for FingerPrintVerificationResponse
/// </summary>
public class FingerPrintVerificationResponse
{
    public bool ValidFingerPrint { get; set; }
    public string Description { get; set; }

    public override string ToString()
    {
        return "ValidFingerPrint : " + ValidFingerPrint + ", Description : " + Description;
    }
}