using System;
using System.Text;
using LightJson;
using Unisave.Authentication;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Serialization;
using Unisave.Serialization.Exceptions;
using UnityEngine;

namespace Unisave.EpicAuthentication
{
    public class EpicAuthFacet : Facet
    {
        /// <summary>
        /// Performs player login via Epic credentials,
        /// or registration if the player is not in the database
        /// </summary>
        /// <param name="authJwt">
        /// JWT from the Auth interface, may be null
        /// </param>
        /// <param name="connectJwt">
        /// JWT from the Connect interface, may be null
        /// </param>
        public EpicLoginResponse LoginOrRegister(string authJwt, string connectJwt)
        {
            // resolve the bootstrapper (a cheap config instance)
            var bootstrapper = EpicAuthBootstrapperBase.Resolve();
            
            // validate tokens
            ValidateJwt(authJwt);
            ValidateJwt(connectJwt);
            
            // extract IDs out of these tokens
            string epicAccountId = ExtractIdFromJwt(authJwt, nameof(authJwt));
            string epicProductUserId = ExtractIdFromJwt(connectJwt, nameof(connectJwt));

            if (epicAccountId == null && epicProductUserId == null)
                throw new ArgumentException(
                    "Either Epic Account ID or PUID have to be provided, but both are null."
                );

            // find the player document
            string documentId = bootstrapper.FindPlayer(
                epicAccountId: epicAccountId,
                epicProductUserId: epicProductUserId
            );

            // register the player if not found
            if (documentId == null)
            {
                documentId = bootstrapper.RegisterNewPlayer(
                    epicAccountId: epicAccountId,
                    epicProductUserId: epicProductUserId
                );

                if (documentId == null)
                {
                    throw new NullReferenceException(
                        $"The {nameof(bootstrapper.RegisterNewPlayer)} method of " +
                        $"the bootstrapper should not return a null."
                    );
                }
            }
            
            // log the player in
            // Auth.Login(documentId); // TODO: extend the Auth API
            Session.Set(AuthenticationManager.SessionKey, documentId); // hack workaround
            
            // fire the after-login callback
            bootstrapper.PlayerHasLoggedIn(
                documentId: documentId,
                epicAccountId: epicAccountId,
                epicProductUserId: epicProductUserId
            );

            return new EpicLoginResponse {
                PlayerId = documentId
            };
        }

        private void ValidateJwt(string jwt)
        {
            if (jwt == null)
                return;

            // throw exception when invalid

            // TODO: implement token validation
        }

        private string ExtractIdFromJwt(string jwt, string jwtName)
        {
            if (jwt == null)
                return null;

            string[] parts = jwt.Split('.');

            if (parts.Length != 3)
            {
                Log.Error($"The JWT '{jwtName}' has invalid structure.");
                return null;
            }
            
            string bodyText = parts[1];
            
            // add padding (JWT omits it, but the parser wants it)
            if (bodyText.Length % 4 == 3)
                bodyText += "=";
            else if (bodyText.Length % 4 == 2)
                bodyText += "==";

            string bodyTextJson = Encoding.UTF8.GetString(
                Convert.FromBase64String(bodyText)
            );

            JsonObject body;
            try
            {
                body = Serializer.FromJsonString<JsonObject>(bodyTextJson);
            }
            catch (SerializedException)
            {
                Log.Error($"The JWT '{jwtName}' body is not a valid JSON.");
                return null;
            }

            if (!body.ContainsKey("sub"))
            {
                Log.Error($"The JWT '{jwtName}' is missing the 'sub' attribute.");
                return null;
            }

            return body["sub"].AsString;
        }

        /// <summary>
        /// Performs player logout
        /// </summary>
        /// <returns>False if the player wasn't logged in to begin with</returns>
        public bool Logout()
        {
            bool wasLoggedIn = Auth.Check();
            
            Auth.Logout();

            return wasLoggedIn;
        }
    }
}