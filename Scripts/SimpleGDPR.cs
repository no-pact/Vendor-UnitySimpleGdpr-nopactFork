using System;
using SimpleGDPRConsent;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleGDPR
{
    public enum ConsentState
    {
        Unknown = 0,
        No = 1,
        Yes = 2
    };

    private const string EU_QUERY_URL = "http://adservice.google.com/getconfig/pubvendors";

    public delegate void ButtonClickDelegate();

    public delegate void DialogClosedDelegate();

    public static bool IsDialogVisible
    {
        get { return GDPRConsentCanvas.IsVisible; }
    }
    public static bool IsTermsOfServiceAccepted
    {
        get { return GDPRConsentCanvas.GetTermsOfServiceState() == ConsentState.Yes; }
    }
    private static bool? m_isGDPRApplicable = null;
    private static Task<string> webResponseTask;
    private static Action<bool> isGDPRApplicableResultCallback;

    private const int TIME_OUT_SPAN = 10;
    private float webRequestStartTime;
    private bool shouldCheckTimeout;

    public SimpleGDPR(bool isDebugging)
    {
        IsDebugging = isDebugging;
    }

    public void IsGDPRApplicable(Action<bool> resultCallback)
    {
        if (!m_isGDPRApplicable.HasValue)
        {
            shouldCheckTimeout = true;
            webRequestStartTime = Time.time;

            isGDPRApplicableResultCallback = resultCallback;
            webResponseTask = GetWebResponseAsStringAsync(EU_QUERY_URL);
        }
        else
        {
            resultCallback?.Invoke(m_isGDPRApplicable.Value);
        }
    }

    private static async Task<string> GetWebResponseAsStringAsync(string url)
    {
        HttpClient webClient = new HttpClient();
        return await webClient.GetStringAsync(url);
    }

    private static bool ParseWebResponse(string response)
    {
        int index = response.IndexOf("is_request_in_eea_or_unknown\":");
        if (index < 0)
        {
            return true;
        }
        else
        {
            index += 30;
            return index >= response.Length || !response.Substring(index).TrimStart().StartsWith("false");
        }
    }

    public static ConsentState GetConsentState(string identifier)
    {
        return GDPRConsentCanvas.GetConsentState(identifier);
    }

    public static void OpenURL(string url)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
		Application.ExternalEval( "window.open(\"" + url + "\",\"_blank\")" );
#else
        Application.OpenURL(url);
#endif
    }

    public void UpdateFrame()
    {
        CheckTimeout();

        if (webResponseTask == null)
        {
            return;
        }

        if (webResponseTask.IsFaulted && webResponseTask.Exception != null)
        {
            if (IsDebugging)
            {
                Debug.Log(webResponseTask.Exception);
            }

            HandleCompletion(false);
            return;
        }

        if (!webResponseTask.IsCompleted)
        {
            return;
        }

        var response = webResponseTask.Result;
        HandleCompletion(true, response);
    }

    private void CheckTimeout()
    {
        if (!shouldCheckTimeout)
        {
            return;
        }

        if (Time.time - webRequestStartTime <= TIME_OUT_SPAN)
        {
            return;
        }

        HandleTimeout();
    }

    private void HandleTimeout()
    {
        m_isGDPRApplicable = true;
        shouldCheckTimeout = false;
        isGDPRApplicableResultCallback?.Invoke(m_isGDPRApplicable.Value);
        isGDPRApplicableResultCallback = null;
        webResponseTask = null;
    }

    private void HandleCompletion(bool taskSuccess, string response = "")
    {
        if (taskSuccess)
        {
            m_isGDPRApplicable = ParseWebResponse(response);
        }
        else
        {
            m_isGDPRApplicable = false;
        }

        shouldCheckTimeout = false;
        isGDPRApplicableResultCallback?.Invoke(m_isGDPRApplicable.Value);
        isGDPRApplicableResultCallback = null;
        webResponseTask = null;
    }

    public static bool IsDebugging { get; private set; }
}