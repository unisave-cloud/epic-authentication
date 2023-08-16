using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Platform;
using UnityEngine;
using Unisave.Facets;

namespace Unisave.EpicAuthentication
{
    /// <summary>
    /// Allows you to interact with the epic auth system from mono behaviours
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// If the player is locally logged in, you can call this method to
        /// perform the login in Unisave auth via this logged-in epic account
        /// </summary>
        /// <param name="caller">The MonoBehaviour triggering this Unisave operation</param>
        /// <param name="platform">An initialized EOS platform interface instance</param>
        /// <param name="epicAccountId">
        /// What logged-in account to use. If null, the first and only one will be used.
        /// </param>
        public static void LoginUnisaveViaEpic(
            this MonoBehaviour caller,
            PlatformInterface platform,
            EpicAccountId epicAccountId = null
        )
        {
            AuthInterface auth = platform.GetAuthInterface();
            
            // === get the epic account ID ===
            
            if (epicAccountId == null)
            {
                int accountCount = auth.GetLoggedInAccountsCount();

                if (accountCount == 0)
                    throw new InvalidOperationException(
                        "There is no logged in epic account."
                    );
                
                if (accountCount != 1)
                    throw new InvalidOperationException(
                        "There are too many logged in accounts. Specify the account ID explicitly."
                    );
                
                epicAccountId = auth.GetLoggedInAccountByIndex(0);
            }
            
            // === get the account's JWT ===
            
            var options = new CopyIdTokenOptions {
                AccountId = epicAccountId
            };

            Result result = auth.CopyIdToken(ref options, out IdToken? idToken);

            if (result != Result.Success || idToken == null)
                throw new Exception("Copying ID token failed: " + result);

            string jwt = idToken.Value.JsonWebToken.ToString();
            
            // === send the JWT to unisave for validation and the login ===
            
            Debug.Log("JWT: " + jwt);
            Debug.Log("ACCOUNT ID: " + epicAccountId);
        }
    }
}
