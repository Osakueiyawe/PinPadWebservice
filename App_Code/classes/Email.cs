using System;
using System.Configuration;
using System.Net.Mail;

public class Email
{
    string emailFrom, emailServer, mPort;
    MailMessage email1 = null;

	public Email()
	{
        emailFrom = ConfigurationManager.AppSettings["GTConnectSenderEmail"];
        emailServer = ConfigurationManager.AppSettings["EmailServer"];
        mPort = ConfigurationManager.AppSettings["EmailPort"];
	}

    public string SendEmail(string recipient, string EmSuj, string EmMsg)
    {
        string sent = "Succeed";
        SmtpClient sendmail = new SmtpClient();
        try
        {
            sendmail.Host = emailServer;
            sendmail.Send(emailFrom, recipient, EmSuj, EmMsg);
            return sent;
        }
        catch (SmtpFailedRecipientException ex)
        {
            return ex.Message;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Email Message Failed ", ex);
            return ex.Message; 
        }
    }

    public string SendEmail(string recipient, string EmSuj, string EmMsg, string copy)
    {
        string sent = "Succeed";
        SmtpClient sendmail = new SmtpClient();
        try
        {

            MailAddress from = new MailAddress(emailFrom);
            MailAddress to = new MailAddress(recipient);

            email1 = new MailMessage(from, to);

            email1.Subject = EmSuj;
            email1.Body = EmMsg;

            MailAddress ccopy = null;
            if (copy != "")
            {
                ccopy = new MailAddress(copy);
                email1.CC.Add(ccopy);
            }
            sendmail.Host = emailServer;
            sendmail.Port = Convert.ToInt16(mPort);

            sendmail.Send(email1);
            email1 = null;
            return sent;
        }
        catch (SmtpFailedRecipientException ex)
        {
            return ex.Message;
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Email Message Failed ", ex);
            return ex.Message;
        }
    }
}