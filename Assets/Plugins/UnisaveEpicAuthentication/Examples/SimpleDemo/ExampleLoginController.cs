using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Platform;
using TMPro;
using Unisave.EpicAuthentication;
using Unisave.EpicAuthentication.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.EpicAuthentication.Examples.SimpleDemo
{
    public class ExampleLoginController : MonoBehaviour
    {
        public Button loginButton;
        public TMP_Text guideText;

        private BasicEOSSDKComponent sdkComponent;
        
        void Start()
        {
            // get the SDK component responsible for the management of the EOS SDK
            sdkComponent = BasicEOSSDKComponent.Instance;
            if (sdkComponent == null)
            {
                guideText.text =
                    "ERROR: The BasicEOSSDKComponent seems to be " +
                    "missing from the scene.";
                loginButton.interactable = false;
                return;
            }
            
            // register the button click handler
            loginButton.onClick.AddListener(OnLoginClick);
        }

        async void OnLoginClick()
        {
            loginButton.interactable = false;
            guideText.text = "";

            // get the Epic SDK PlatformInterface instance
            guideText.text += "Getting platform interface...\n";
            PlatformInterface platform = sdkComponent.PlatformInterface;
            if (platform == null)
            {
                guideText.text +=
                    "ERROR: Platform interface has failed to initialize.\n" +
                    "Check the console for more info.\n";
                return;
            }
            
            // login EOS via the Auth interface
            guideText.text += "Logging EOS in via Auth interface...\n";
            LoginCallbackInfo info = await sdkComponent.AuthLogin(
                authToolHost: "localhost:6547",
                authToolCredentialName: "me"
            );

            if (info.ResultCode == Result.Success)
            {
                guideText.text +=
                    $"Done. Your EpicAccountId is: {info.LocalUserId}\n";
            }
            else
            {
                guideText.text +=
                    $"ERROR: Auth EOS login failed: {info.ResultCode}\n";
                return;
            }
            
            // TODO: login EOS via Connect interface

            // call the UnisaveEpicAuthentication code
            guideText.text += "Logging into Unisave using the Epic user session...\n";
            EpicLoginResponse response = await this.LoginUnisaveViaEpic(platform);
            guideText.text += $"Done. Your Unisave player ID is: {response.PlayerId}\n";
            
            // done, now this session is logged-in in unisave 
            guideText.text += "\n<b>Success!</b>\n";
        }
    }
}