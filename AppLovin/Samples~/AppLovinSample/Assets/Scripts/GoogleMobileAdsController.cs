using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Mediation.AppLovin.Api;

namespace GoogleMobileAds.Samples
{
    /// <summary>
    /// Demonstrates how to use the Google Mobile Ads Unity plugin.
    /// </summary>
    [AddComponentMenu("GoogleMobileAds/Samples/GoogleMobileAdsController")]
    public class GoogleMobileAdsController : MonoBehaviour
    {
        // Always use test ads.
        // https://developers.google.com/admob/unity/test-ads
        internal static List<string> TestDeviceIds = new List<string>()
        {
            AdRequest.TestDeviceSimulator,
#if UNITY_IPHONE
            "02185ADD-7B65-445B-96F4-05E05EC85A49",
            "d4bab3c73c48912ac6ca38d85b9c99e7",
            "4d28dea0c8fc7b5c90d6c60555839619",
#elif UNITY_ANDROID
            "65470B01DAF57FFECD1EF97B8FBE6B82"
#endif
        };

        // The Google Mobile Ads Unity plugin needs to be run only once.
        private static bool? _isInitialized;

        // Helper class that implements consent using the
        // Google User Messaging Platform (UMP) Unity plugin.
        [SerializeField, Tooltip("Controller for the Google User Messaging Platform (UMP) Unity plugin.")]
        private GoogleMobileAdsConsentController _consentController;

        /// <summary>
        /// Demonstrates how to configure Google Mobile Ads Unity plugin.
        /// </summary>
        private void Start()
        {
            // On Android, Unity is paused when displaying interstitial or rewarded video.
            // This setting makes iOS behave consistently with Android.
            MobileAds.SetiOSAppPauseOnBackground(true);

            // When true all events raised by GoogleMobileAds will be raised
            // on the Unity main thread. The default value is false.
            // https://developers.google.com/admob/unity/quick-start#raise_ad_events_on_the_unity_main_thread
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            // Configure your RequestConfiguration with Child Directed Treatment
            // and the Test Device Ids.
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TestDeviceIds = TestDeviceIds
            });

#if UNITY_ANDROID
            AndroidJavaClass appLovin = new AndroidJavaClass(
                    "com.google.ads.mediation.applovin.AppLovinMediationAdapter");
            if (appLovin != null)
            {
                Debug.Log("[TEST] Found AppLovinMediationAdapter class...");

                AndroidJavaClass unityPlayer =
                        new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity =
                        unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                AndroidJavaObject sdkSettings =
                        appLovin.CallStatic<AndroidJavaObject>("getSdkSettings", currentActivity);
                if (sdkSettings != null)
                {
                    Debug.Log("[TEST] Found AppLovin Adapter SDKSettings object...");

                    Debug.Log("[TEST] Enabling AppLovin Verbose Logging...");
                    sdkSettings.Call("setVerboseLogging", true);

                    Debug.Log("[TEST] Adding Test Device IDs to AppLovin SDK Settings...");
                    AndroidJavaClass collections = new AndroidJavaClass("java.util.Collections");
                    AndroidJavaObject testIds = collections.CallStatic<AndroidJavaObject>(
                            "singletonList", 
                            "5cc650b4-dea0-6f4a-30c2-3657c77c3429"); // Insert your test device ID 
                    sdkSettings.Call("setTestDeviceAdvertisingIds", testIds);
                }
            }
#endif

            AppLovin.SetHasUserConsent(false);
            AppLovin.SetIsAgeRestrictedUser(false);
            AppLovin.SetDoNotSell(true);

            // If we can request ads, we should initialize the Google Mobile Ads Unity plugin.
            // if (_consentController.CanRequestAds)
            // {
            InitializeGoogleMobileAds();
            // }

            // Ensures that privacy and consent information is up to date.
            // InitializeGoogleMobileAdsConsent();
        }

        /// <summary>
        /// Ensures that privacy and consent information is up to date.
        /// </summary>
        private void InitializeGoogleMobileAdsConsent()
        {
            Debug.Log("Google Mobile Ads gathering consent.");

            _consentController.GatherConsent((string error) =>
            {
                if (error != null)
                {
                    Debug.LogError("Failed to gather consent with error: " +
                        error);
                }
                else
                {
                    Debug.Log("Google Mobile Ads consent updated: "
                        + ConsentInformation.ConsentStatus);
                }

                if (_consentController.CanRequestAds)
                {
                    InitializeGoogleMobileAds();
                }
            });
        }

        /// <summary>
        /// Initializes the Google Mobile Ads Unity plugin.
        /// </summary>
        private void InitializeGoogleMobileAds()
        {
            // The Google Mobile Ads Unity plugin needs to be run only once and before loading any ads.
            if (_isInitialized.HasValue)
            {
                return;
            }

            _isInitialized = false;

            // Initialize the Google Mobile Ads Unity plugin.
            Debug.Log("Google Mobile Ads Initializing.");
            MobileAds.Initialize((InitializationStatus initstatus) =>
            {
                if (initstatus == null)
                {
                    Debug.LogError("Google Mobile Ads initialization failed.");
                    _isInitialized = null;
                    return;
                }

                // If you use mediation, you can check the status of each adapter.
                var adapterStatusMap = initstatus.getAdapterStatusMap();
                if (adapterStatusMap != null)
                {
                    foreach (var item in adapterStatusMap)
                    {
                        Debug.Log(string.Format("Adapter {0} is {1}",
                            item.Key,
                            item.Value.InitializationState));
                    }
                }

                Debug.Log("Google Mobile Ads initialization complete.");
                _isInitialized = true;
            });
        }

        /// <summary>
        /// Opens the AdInspector.
        /// </summary>
        public void OpenAdInspector()
        {
            Debug.Log("Opening ad Inspector.");
            MobileAds.OpenAdInspector((AdInspectorError error) =>
            {
                // If the operation failed, an error is returned.
                if (error != null)
                {
                    Debug.Log("Ad Inspector failed to open with error: " + error);
                    return;
                }

                Debug.Log("Ad Inspector opened successfully.");
            });
        }

        /// <summary>
        /// Opens the privacy options form for the user.
        /// </summary>
        /// <remarks>
        /// Your app needs to allow the user to change their consent status at any time.
        /// </remarks>
        public void OpenPrivacyOptions()
        {
            _consentController.ShowPrivacyOptionsForm((string error) =>
            {
                if (error != null)
                {
                    Debug.LogError("Failed to show consent privacy form with error: " +
                        error);
                }
                else
                {
                    Debug.Log("Privacy form opened successfully.");
                }
            });
        }
    }
}