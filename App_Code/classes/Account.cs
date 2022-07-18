public class Account
{
    private int bracode;
    private int cusnum;
    private int curcode;
    private int ledcode;
    private int subacctcode;

	public Account()
	{

	}

    public int bra
    {
        get{return bracode;}
        set{bracode = value;}
    }
    public int cus
    {
        get { return cusnum; }
        set { cusnum = value; }
    }
    public int cur
    {
        get { return curcode; }
        set { curcode = value; }
    }
    public int led
    {
        get { return ledcode; }
        set { ledcode = value; }
    }
    public int sub
    {
        get { return subacctcode; }
        set { subacctcode = value; }
    }
}