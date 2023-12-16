using System;
using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using LightJson;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.JWT;

namespace Unisave.EpicAuthentication
{
    public class EpicAuthFacet : Facet
    {
        private readonly EpicAuthBootstrapperBase bootstrapper;
        
        public EpicAuthFacet(EpicAuthBootstrapperBase bootstrapper)
        {
            this.bootstrapper = bootstrapper;
        }
        
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
            // validate tokens and get IDs out of these tokens
            string epicAccountId = ValidateJwtAndExtractId(
                authJwt,
                nameof(authJwt),
                bootstrapper.AuthInterfaceJwksCache
            );
            string epicProductUserId = ValidateJwtAndExtractId(
                connectJwt,
                nameof(connectJwt),
                bootstrapper.ConnectInterfaceJwksCache
            );

            if (epicAccountId == null && epicProductUserId == null)
                throw new ArgumentException(
                    "Either Epic Account ID or PUID have to be provided, " +
                    "but both are null."
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
                        $"The {nameof(bootstrapper.RegisterNewPlayer)} " +
                        $"method of the bootstrapper should not return a null."
                    );
                }
            }
            
            // log the player in
            Auth.Login(documentId);
            
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

        /// <summary>
        /// Validates the given JWT and extracts the respective ID
        /// it stores (EpicAccountID or ProductUserID)
        /// </summary>
        /// <param name="jwt"></param>
        /// <param name="jwtName">Name of th JWT token (auth or connect)</param>
        /// <param name="jwksCache">Auth or Connect JWKS cache</param>
        private static string ValidateJwtAndExtractId(
            string jwt,
            string jwtName,
            EpicJwksCache jwksCache
        )
        {
            // the player is not logged in via the given interface
            // (Auth or Connect)
            if (jwt == null)
                return null;
            
            /*
             * The validation is based on:
             * https://dev.epicgames.com/docs/epic-account-services
             * /auth/auth-interface#validating-id-tokens-on-backend-without-sdk
             */
            
            jwksCache.Prepare();
            
            var algorithmFactory = new DelegateAlgorithmFactory(ctx => {
                if (ctx.Header.Algorithm == "RS256")
                    return ConstructRS256Algorithm(ctx.Header.KeyId, jwksCache);
                
                throw new SignatureVerificationException(
                    $"Unsupported algorithm {ctx.Header.Algorithm}"
                );
            });
            
            IJsonSerializer serializer = new LightJsonSerializer();
            IDateTimeProvider provider = new UtcDateTimeProvider();
            IJwtValidator validator = new JwtValidator(serializer, provider);
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtDecoder decoder = new JwtDecoder(
                serializer, validator, urlEncoder, algorithmFactory
            );

            // decode and verify signature,
            // throws appropriate exceptions when verification fails
            JsonObject jwtPayload = decoder.DecodeToObject<JsonObject>(jwt);

            /*
             * The step 6. (epic game client ID verification) is not performed.
             * We assume privileges are granted based on the authenticated
             * player, not the client used. For server clients, a different
             * authentication scheme should be used.
             */
            
            if (!jwtPayload.ContainsKey("sub"))
            {
                Log.Error($"The JWT '{jwtName}' is missing the 'sub' attribute.");
                return null;
            }
            
            return jwtPayload["sub"].AsString;
        }

        private static RS256Algorithm ConstructRS256Algorithm(
            string keyId,
            EpicJwksCache jwksCache
        )
        {
            JsonObject key = jwksCache.GetKey(keyId);

            if (key["kty"].AsString != "RSA")
                throw new SignatureVerificationException(
                    $"The key with ID '{keyId}' does not " +
                    $"have the 'RSA' key type."
                );

            string modulus = key["n"].AsString;
            string exponent = key["e"].AsString; 
                    
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            var parameters = new RSAParameters() {
                Modulus = urlEncoder.Decode(modulus),
                Exponent = urlEncoder.Decode(exponent)
            };
            
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(parameters);
            return new RS256Algorithm(rsa);
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