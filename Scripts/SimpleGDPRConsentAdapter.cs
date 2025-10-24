// Copyright © 2024 no-pact
// Author: Kaan Karagulle

// #define TEST_MODE

using nopact.Packages.Privacy.GdprBridge;
using System;
using SimpleGDPRConsent;
using UnityEngine;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class SimpleGDPRConsentAdapter : IGdprConsentAdapter
{
    private const string HAS_SHOWN_GDPR_SCREEN_KEY = "GdprHasShown";

    private const string ADS_IDENTIFIER = "GdprAds";
    private const string ADS_TITLE = "Serve you with personalised advertising";
    private const string ADS_DESCRIPTION = "";

    private const string ANALYTICS_IDENTIFIER = "GdprAnalytics";
    private const string ANALYTICS_TITLE = "Measure content performance that allows us to improve our games";
    private const string ANALYTICS_DESCRIPTION = "";

    private const string AGE_IDENTIFIER = "GdprAge";

    private const string AGE_TITLE =
        "I certify that I am over the age of sixteen and that I have read, understood and accepted no-pact's privacy policy from the link below";

    private const string AGE_DESCRIPTION = "";

    private string[] POLICY_LINKS;

    private SimpleGDPR simpleGDPR;
    private Action<GdprConsent> onGdprConsentComplete;
    private GdprConsent gdprConsent;
    private bool isGDPRApplicable;

    public void Initialize(Action<GdprConsent> onGdprConsentComplete, string[] policyLinks, bool isDebugging)
    {
        this.IsDebugging = isDebugging;
        POLICY_LINKS = policyLinks;
        this.onGdprConsentComplete = onGdprConsentComplete;
        simpleGDPR = new SimpleGDPR(isDebugging);

#if TEST_MODE
        HandleGDPRConsentInit(true);
#else
        simpleGDPR.IsGDPRApplicable(HandleGDPRConsentInit);
#endif
    }

    public void UpdateFrame()
    {
        simpleGDPR.UpdateFrame();
    }

    public void ShowModifyConsentOverlay()
    {
        ShowGdprConsentOverlay(false);
    }

    private void HandleGDPRConsentInit(bool isGDPRApplicable)
    {
        this.isGDPRApplicable = isGDPRApplicable;
        if (isGDPRApplicable)
        {
#if UNITY_IOS
            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.DENIED ||
                status == ATTrackingStatusBinding.AuthorizationTrackingStatus.RESTRICTED)
            {
                if (!GDPRConsentCanvas.HasConsentStateBeenSet(ADS_IDENTIFIER))
                {
                    GDPRConsentCanvas.SetConsentState(ADS_IDENTIFIER, SimpleGDPR.ConsentState.No);
                }

                if (!GDPRConsentCanvas.HasConsentStateBeenSet(ANALYTICS_IDENTIFIER))
                {
                    GDPRConsentCanvas.SetConsentState(ANALYTICS_IDENTIFIER, SimpleGDPR.ConsentState.No);
                }

                if (!GDPRConsentCanvas.HasConsentStateBeenSet(AGE_IDENTIFIER))
                {
                    GDPRConsentCanvas.SetConsentState(AGE_IDENTIFIER, SimpleGDPR.ConsentState.Yes);
                }

                gdprConsent = new GdprConsent()
                {
                    IsGdprApplicable = true,
                    IsConsentGivenForAds = SimpleGDPR.GetConsentState(ADS_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
                    IsConsentGivenForAnalytics =
                        SimpleGDPR.GetConsentState(ANALYTICS_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
                    IsAboveAgeRestriction = SimpleGDPR.GetConsentState(AGE_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
                };

                this.onGdprConsentComplete?.Invoke(gdprConsent);
                return;
            }
#endif

            var hasGdprScreenBeenShownBefore = PlayerPrefs.GetInt(HAS_SHOWN_GDPR_SCREEN_KEY, -1) > 0;
            if (hasGdprScreenBeenShownBefore)
            {
                gdprConsent = new GdprConsent()
                {
                    IsGdprApplicable = true,
                    IsConsentGivenForAds = SimpleGDPR.GetConsentState(ADS_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
                    IsConsentGivenForAnalytics =
                        SimpleGDPR.GetConsentState(ANALYTICS_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
                    IsAboveAgeRestriction = SimpleGDPR.GetConsentState(AGE_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
                };

                this.onGdprConsentComplete?.Invoke(gdprConsent);
                return;
            }

            ShowGdprConsentOverlay(true);
        }
        else
        {
            if (!GDPRConsentCanvas.HasConsentStateBeenSet(ADS_IDENTIFIER))
            {
                GDPRConsentCanvas.SetConsentState(ADS_IDENTIFIER, SimpleGDPR.ConsentState.Yes);
            }

            if (!GDPRConsentCanvas.HasConsentStateBeenSet(ANALYTICS_IDENTIFIER))
            {
                GDPRConsentCanvas.SetConsentState(ANALYTICS_IDENTIFIER, SimpleGDPR.ConsentState.Yes);
            }

            if (!GDPRConsentCanvas.HasConsentStateBeenSet(AGE_IDENTIFIER))
            {
                GDPRConsentCanvas.SetConsentState(AGE_IDENTIFIER, SimpleGDPR.ConsentState.Yes);
            }

            gdprConsent = new GdprConsent()
            {
                IsGdprApplicable = false,
                IsConsentGivenForAds = SimpleGDPR.GetConsentState(ADS_IDENTIFIER) != SimpleGDPR.ConsentState.No,
                IsConsentGivenForAnalytics = SimpleGDPR.GetConsentState(ANALYTICS_IDENTIFIER) != SimpleGDPR.ConsentState.No,
                IsAboveAgeRestriction = SimpleGDPR.GetConsentState(AGE_IDENTIFIER) != SimpleGDPR.ConsentState.No,
            };

            this.onGdprConsentComplete?.Invoke(gdprConsent);
        }
    }

    private void ShowGdprConsentOverlay(bool isGameStartUpOverlay)
    {
        //Warning: Only one essential toggle is allowed in the current implementation
        var gdprConsentDialogue = new GDPRConsentDialog()
            .AddSectionWithToggle(null, ADS_IDENTIFIER, ADS_TITLE, ADS_DESCRIPTION)
            .AddSectionWithToggle(null, ANALYTICS_IDENTIFIER, ANALYTICS_TITLE, ANALYTICS_DESCRIPTION)
            .AddSectionWithToggle(OnEssentialToggleStateChanged, AGE_IDENTIFIER, AGE_TITLE, AGE_DESCRIPTION)
            .AddPrivacyPolicies(POLICY_LINKS);
        gdprConsentDialogue.ShowDialog(OnGdprConsentDialogueClosed);
    }

    private void OnGdprConsentDialogueClosed()
    {
        gdprConsent = new GdprConsent()
        {
            IsGdprApplicable = isGDPRApplicable,
            IsConsentGivenForAds = SimpleGDPR.GetConsentState(ADS_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
            IsConsentGivenForAnalytics = SimpleGDPR.GetConsentState(ANALYTICS_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
            IsAboveAgeRestriction = SimpleGDPR.GetConsentState(AGE_IDENTIFIER) == SimpleGDPR.ConsentState.Yes,
        };

        PlayerPrefs.SetInt(HAS_SHOWN_GDPR_SCREEN_KEY, 1);
        onGdprConsentComplete?.Invoke(gdprConsent);
    }

    //Warning: Only one essential toggle is allowed in the current implementation
    private void OnEssentialToggleStateChanged(bool isEnabled)
    {
        GDPRConsentCanvas.Instance.ToggleCloseButtonInteractivity(isEnabled);
    }

    public GdprConsent GdprConsent => gdprConsent;
    public bool IsDebugging { get; private set; }
}