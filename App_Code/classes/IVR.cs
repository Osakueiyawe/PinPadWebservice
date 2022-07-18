using System;
using System.Net.Sockets;

/// <summary>
/// Summary description for IVR
/// </summary>
public class IVR
{
    private string xmlstring = null;
    private string serverip, serverport;
	public IVR()
	{
        serverip = "10.2.34.10";
        serverport = "9000";
	}

    public string SendCTIMessage(string ivrinfostr, string chanidstr)
    {
        string message = null;
        long epochtime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;

        try
        {
            TcpClient client = new TcpClient(serverip, Convert.ToInt32(serverport));

            message = "CHANID:" + chanidstr + ";";
            message = message + "TYPE:" + "E" + ";";
            message = message + "TIME:" + epochtime.ToString() + ";";
            message = message + ivrinfostr;

            byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

            NetworkStream stream = client.GetStream();

            stream.Write(data, 0, data.Length);

            xmlstring = xmlstring + "<CODE>1000</CODE>";
            xmlstring = xmlstring + "</MESSAGE>SUCCESS</MESSAGE>";

            stream.Close();
            client.Close();
        }
        catch (ArgumentNullException e)
        {
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + e.Message.Replace("'", "") + "</Error>";
        }
        catch (SocketException e)
        {
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + e.Message.Replace("'", "") + "</Error>";
        }

        xmlstring = xmlstring + "</Response>";
        return xmlstring;
    }

    public string SendCTIStartMessage(string chanidstr)
    {
        string message = null;
        long epochtime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        DateTime dt = DateTime.Now;

        string datestr = dt.Year.ToString().PadLeft(4, '0') + dt.Month.ToString().PadLeft(2, '0') + dt.Day.ToString().PadLeft(2, '0');

        try
        {
            TcpClient client = new TcpClient(serverip, Convert.ToInt32(serverport));

            message = "CHANID:" + chanidstr + ";";
            message = message + "TYPE:" + "S" + ";";
            message = message + "TIME:" + epochtime.ToString() + ";";
            message = message + "DATE:" + datestr + ";";

            byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

            NetworkStream stream = client.GetStream();

            stream.Write(data, 0, data.Length);

            xmlstring = xmlstring + "<CODE>1000</CODE>";
            xmlstring = xmlstring + "</MESSAGE>SUCCESS</MESSAGE>";

            stream.Close();
            client.Close();
        }
        catch (ArgumentNullException e)
        {
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + e.Message.Replace("'", "") + "</Error>";
        }
        catch (SocketException e)
        {
            xmlstring = xmlstring + "<CODE>1010</CODE>";
            xmlstring = xmlstring + "<Error>" + e.Message.Replace("'", "") + "</Error>";
        }

        xmlstring = xmlstring + "</Response>";
        return xmlstring;
    }
}