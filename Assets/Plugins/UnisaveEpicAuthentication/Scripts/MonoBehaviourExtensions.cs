using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
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
        public static UnisaveOperation<EpicLoginResponse> LoginUnisaveViaEpic(
            this MonoBehaviour caller,
            PlatformInterface platform,
            EpicAccountId epicAccountId = null,
            ProductUserId productUserId = null
        )
        {
            AuthInterface auth = platform.GetAuthInterface();
            ConnectInterface connect = platform.GetConnectInterface();
            
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
            
            var authCopyOptions = new Epic.OnlineServices.Auth.CopyIdTokenOptions {
                AccountId = epicAccountId
            };

            Result authCopyResult = auth.CopyIdToken(
                ref authCopyOptions,
                out Epic.OnlineServices.Auth.IdToken? authIdToken
            );

            if (authCopyResult != Result.Success || authIdToken == null)
                throw new Exception("Copying Auth ID token failed: " + authCopyResult);

            string authJwt = authIdToken.Value.JsonWebToken.ToString();
            
            // === get the product user ID ===
                
            if (productUserId == null)
            {
                int usersCount = connect.GetLoggedInUsersCount();

                if (usersCount == 0)
                    throw new InvalidOperationException(
                        "There is no logged in product user."
                    );
                
                if (usersCount != 1)
                    throw new InvalidOperationException(
                        "There are too many logged in product users. Specify the PUID explicitly."
                    );
                
                productUserId = connect.GetLoggedInUserByIndex(0);
            }
            
            // === get the product user's JWT ===
            
            var connectCopyOptions = new Epic.OnlineServices.Connect.CopyIdTokenOptions {
                LocalUserId = productUserId
            };
            
            Result connectCopyResult = connect.CopyIdToken(
                ref connectCopyOptions,
                out Epic.OnlineServices.Connect.IdToken? copyIdToken
            );
            
            if (connectCopyResult != Result.Success || copyIdToken == null)
                throw new Exception("Copying Connect ID token failed: " + connectCopyResult);
            
            string connectJwt = copyIdToken.Value.JsonWebToken.ToString();
            
            // === send the JWT to unisave for validation and the login ===

            return caller.CallFacet(
                (EpicAuthFacet f) => f.LoginOrRegister(authJwt, connectJwt)
            );
        }
    }
}
