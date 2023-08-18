using System;
using System.Collections;
using Epic.OnlineServices.Platform;
using Unisave.EpicAuthentication;
using UnityEngine;

namespace Unisave.EpicAuthentication.Examples.SimpleDemo
{
    public class ExampleLoginController : MonoBehaviour
    {
        void OnEnable()
        {
            // DEBUG
            // Invoke(nameof(OnLoginClick), 1.0f);
            OnLoginClick();
        }
        
        void OnLoginClick()
        {   
            // DEBUG
            Debug.Log("Login via epic button was clicked!");

            // Get the Epic SDK PlatformInterface instance from your own SDK code
            // (here we use the example SDK component as a placeholder)
            PlatformInterface platform = ExampleEOSSDKComponent.platformInterface
                ?? throw new Exception("SDK platform is not initialized.");
            
            // Call the UnisaveEpicAuthentication code
            this.LoginUnisaveViaEpic(platform);
        }
    }
}