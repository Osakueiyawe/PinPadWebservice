using System;
using System.IO;
using System.Globalization;
using System.Diagnostics;

/// <summary>
/// Summary description for ErrHandler
/// Handles error by acception the error message
/// Displays the page on which the error occured
/// </summary>
/// 

public class LogHandler
{
    public LogHandler()
    {

    }

    public static void WriteLog(string Message)
    {
        string StartUpPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        try
        {

            string loc = StartUpPath + "/Logs/" + DateTime.Today.ToString("dd-MM-yy");
            if (!Directory.Exists(loc))
            {
                Directory.CreateDirectory(loc);
            }

            string path = loc + "/" + "Requery" + ".txt";// StartUpPath + "/Logs/" + DateTime.Today.ToString("dd-MM-yy") + "-" + emailtype + ".txt";

            if (!File.Exists((Path.GetFullPath(path))))
            {
                File.CreateText(path);
            }
            using (StreamWriter w = File.AppendText(path))
            {
                w.Write("\r\nLog Entry : :");
                w.Write("{0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                w.WriteLine("-:-" + Message);
                w.Flush();
                w.Close();
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);
        }
    }

    public static void WriteLog(string Message, string emailtype)
    {
        string StartUpPath = System.Web.HttpContext.Current.Server.MapPath("~");

        try
        {
            string loc = StartUpPath + "/LogsPM/" + DateTime.Today.ToString("dd-MM-yy");

            if (!Directory.Exists(loc))
            {
                Directory.CreateDirectory(loc);
            }

            string path = loc + "/" + emailtype + ".txt";// StartUpPath + "/Logs/" + DateTime.Today.ToString("dd-MM-yy") + "-" + emailtype + ".txt";

            using (StreamWriter w = File.AppendText(path))
            {
                w.Write("\r\nLog Entry : :");
                w.Write("{0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                w.WriteLine("-:-" + Message);
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