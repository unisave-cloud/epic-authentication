using System;
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
        public static void LoginViaEpic(
            this MonoBehaviour caller,
            PlatformInterface platform
        )
        {
            AuthInterface auth = platform.GetAuthInterface();
            
            // do interesting stuff here
        }
    }
}
