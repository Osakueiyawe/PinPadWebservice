using Newtonsoft.Json;
using RestSharp;
using System;
using System.Configuration;
using System.Net;

/// <summary>
/// Summary description for FingerPrintImplementation
/// </summary>
public class FingerPrintImplementation
{
    public FingerPrintImplementation()
    {

    }

    public FingerPrintVerificationResponse GetVerificationResponse(FingerPrintVerificationRequest request)
    {
        FingerPrintVerificationResponse verificationResponse = new FingerPrintVerificationResponse();

        try
        {
            if(request == null)
            {
                Utility.Log().Info("Fingerprint verification request cannot be null.");
                return null;
            }

            string apiUrl = ConfigurationManager.AppSettings["FingerPrintVerificationApi"];

            string serializedObject = SerializeObjectForApiCall(request);

            var client = new RestClient(apiUrl);

            var apiRequest = new RestRequest(Method.POST);

            apiRequest.AddHeader("Content-Type", "application/json");
            apiRequest.AddParameter("application/json", serializedObject, ParameterType.RequestBody);

            string responseMessage = string.Empty;

            IRestResponse restResponse = new RestResponse();

            try
            {
                restResponse = client.Execute(apiRequest);
                responseMessage = restResponse.Content;

                Utility.Log().Info("Response parameters from fingerprint verification API : " + responseMessage);

                if (restResponse.StatusCode == HttpStatusCode.OK)
                {
                    verificationResponse = JsonConvert.DeserializeObject<FingerPrintVerificationResponse>(responseMessage);
                    return verificationResponse;
                }
                else
                {
                    Utility.Log().Info("Call to fingerPrintVerification API was not successful!");
                }
            }
            catch (Exception ex)
            {
                Utility.Log().Fatal("An error occurred in GetVerificationResponse method. Message - " + ex.Message + "|StackTrace - " + ex.StackTrace);
                return verificationResponse;
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("An exception occurred in GetVerificationResponse method. Message - " + ex.Message + "|Stacktrace - " + ex.StackTrace);
            return verificationResponse;
        }

        return verificationResponse;
    }

    private string SerializeObjectForApiCall(FingerPrintVerificationRequest request)
    {
        string serializedObject = string.Empty;

        try
        {
            serializedObject = JsonConvert.SerializeObject(request);
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("An exception occurred in SerializeObjectForApiCall method. Message - " + ex.Message + "|StackTrace - " + ex.StackTrace);
            serializedObject = null;
        }

        return serializedObject;
    }
}