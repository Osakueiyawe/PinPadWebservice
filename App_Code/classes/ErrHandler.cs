using System;
using System.Configuration;
using System.IO;

/// <summary>
/// Summary description for ErrHandler
/// Handles error by acception the error message
/// Displays the page on which the error occured
/// </summary>
public class ErrHandler
{
    public ErrHandler()
    {

    }

    public static void WriteError(string errorMessage, string filename)
    {
        try
        {
            string logstatus = ConfigurationManager.AppSettings["DetailedLogging"];

            if (logstatus.Equals("1"))
            {
                string errorfolder = ConfigurationManager.AppSettings["ErrorFolder"];

                string loc = errorfolder + DateTime.Today.ToString("dd-MM-yy");

                if (!Directory.Exists(loc))
                {
                    Directory.CreateDirectory(loc);
                }

                string path = loc + "/" + filename + ".txt";

                using (StreamWriter w = File.AppendText(path))
                {
                    w.Write("{0}", DateTime.Now.ToString("hh:mm:ss.ff"));
                    string err = " Log : " + errorMessage;
                    w.WriteLine(err);
                    w.WriteLine("_______________________________________");
                    w.Flush();
                    w.Close();
                }
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Debug("Database Call Failed ", ex);
        }
    }

    public void WriteError(string errorMessage, string filename, string logfile)
    {
        try
        {
            string localErrorPath = ConfigurationManager.AppSettings["USSDUpdateFolder"].ToString();

            string path = localErrorPath + filename + ".txt";

            using (StreamWriter w = File.AppendText(path))
            {
                string err = errorMessage;
                w.WriteLine(err);
                w.Flush();
                w.Close();
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
        }
    }
}
