using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;
using Unisave.Facades;
using Unisave.HttpClient;
using UnityEngine;

namespace Unisave.EpicAuthentication
{
    /// <summary>
    /// Stores the Epic Games JWKS data
    /// (JSON Web Key Store)
    ///
    /// Is thread-safe.
    /// </summary>
    public class EpicJwksCache
    {
        private const double ExpirationSeconds = 3600;

        private readonly string jwksUrl;

        private object syncLock = new object();

        private List<JsonObject> keys;
        private DateTime downloadedAt;

        public EpicJwksCache(string jwksUrl)
        {
            this.jwksUrl = jwksUrl;
        }
        
        /// <summary>
        /// Prepares the cache (downloading or refreshing keys),
        /// so that it can be read.
        /// </summary>
        public void Prepare()
        {
            if (ShouldDownload())
                Download();
        }

        /// <summary>
        /// Finds a key by its ID
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns>The key JSON object as defined in the JWKS</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public JsonObject GetKey(string keyId)
        {
            lock (syncLock)
            {
                if (keys == null)
                    throw new InvalidOperationException(
                        "Prepare the cache before using it."
                    );
                
                foreach (JsonObject key in keys)
                    if (key["kid"].AsString == keyId)
                        return key;

                throw new KeyNotFoundException(
                    $"The key with ID '{keyId}' is not in the JWKS."
                );
            }
        }

        private bool ShouldDownload()
        {
            lock (syncLock)
            {
                // first download
                if (keys == null)
                    return true;
                
                // expiration refresh
                double age = (DateTime.UtcNow - downloadedAt).TotalSeconds;
                if (age > ExpirationSeconds)
                    return true;
            }

            return false;
        }

        private void Download()
        {
            Log.Info(
                "Downloading Epic Games JSON Web Key Store...",
                new JsonObject { ["url"] = jwksUrl }
            );
            
            Response response = Http.Get(jwksUrl);
            response.Throw();
            
            JsonObject body = response.Json();
            List<JsonObject> newKeys = body["keys"].AsJsonArray
                .Select(x => x.AsJsonObject)
                .ToList();
            
            Log.Info("Done.");

            // store the downloaded values
            lock (syncLock)
            {
                keys = newKeys;
                downloadedAt = DateTime.UtcNow;
            }
        }
    }
}