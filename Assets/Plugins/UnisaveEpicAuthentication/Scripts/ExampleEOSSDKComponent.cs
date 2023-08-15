using System;
using System.Runtime.InteropServices;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using UnityEngine;

namespace Unisave.EpicAuthentication
{
    /// <summary>
    /// Example component that initializes the PlatformInterface instance
    /// used to communicate with Epic Game Services.
    ///
    /// The code is taken from (and then modified):
    /// https://dev.epicgames.com/docs/epic-online-services/eos-get-started/eossdkc-sharp-getting-started#unity-eossdk-component
    /// </summary>
    public class ExampleEOSSDKComponent : MonoBehaviour
    {
        public string productName = "MyUnityApplication";
        public string productVersion = "1.0";
        public string productId = "";
        public string sandboxId = "";
        public string deploymentId = "";
        public string clientId = "";
        public string clientSecret = "";
        public LoginCredentialType loginCredentialType = LoginCredentialType.AccountPortal;
        
        public string loginCredentialId = null;
        public string loginCredentialToken = null;

        public static PlatformInterface platformInterface = null;
        private const float platformTickInterval = 0.1f;
        private float platformTickTimer = 0f;

        // If we're in editor, we should dynamically load and unload the SDK between play sessions.
        // This allows us to initialize the SDK each time the game is run in editor.
#if UNITY_EDITOR
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("Kernel32.dll")]
        private static extern int FreeLibrary(IntPtr hLibModule);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private IntPtr libraryPointer;
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            var libraryPath = "Assets/Plugins/EOSSDK/" + Config.LibraryName;

            libraryPointer = LoadLibrary(libraryPath);
            if (libraryPointer == IntPtr.Zero)
            {
                throw new Exception("Failed to load library" + libraryPath);
            }

            Bindings.Hook(libraryPointer, GetProcAddress);
#endif
        }

        private void OnApplicationQuit()
        {
            if (platformInterface != null)
            {
                platformInterface.Release();
                platformInterface = null;
                PlatformInterface.Shutdown();
            }

    #if UNITY_EDITOR
            if (libraryPointer != IntPtr.Zero)
            {
                Bindings.Unhook();

                // Free until the module ref count is 0
                while (FreeLibrary(libraryPointer) != 0) { }
                libraryPointer = IntPtr.Zero;
            }
    #endif
        }

        void Start()
        {
            var initializeOptions = new InitializeOptions()
            {
                ProductName = productName,
                ProductVersion = productVersion
            };

            var initializeResult = PlatformInterface.Initialize(ref initializeOptions);
            if (initializeResult != Result.Success)
            {
                throw new Exception("Failed to initialize platform: " + initializeResult);
            }

            // The SDK outputs lots of information that is useful for debugging.
            // Make sure to set up the logging interface as early as possible: after initializing.
            LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.VeryVerbose);
            LoggingInterface.SetCallback((ref LogMessage logMessage) => Debug.Log(logMessage.Message));

            var options = new Options()
            {
                ProductId = productId,
                SandboxId = sandboxId,
                DeploymentId = deploymentId,
                ClientCredentials = new ClientCredentials()
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                }
            };

            platformInterface = PlatformInterface.Create(ref options);
            if (platformInterface == null)
            {
                throw new Exception("Failed to create platform");
            }

            var loginOptions = new LoginOptions()
            {
                Credentials = new Credentials()
                {
                    Type = loginCredentialType,
                    Id = loginCredentialId,
                    Token = loginCredentialToken
                },
                // Change these scopes to match the ones set up on your product on the Developer Portal.
                ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence
            };

            // Ensure platform tick is called on an interval, or this will not callback.
            platformInterface.GetAuthInterface().Login(ref loginOptions, null, (ref LoginCallbackInfo loginCallbackInfo) =>
            {
                if (loginCallbackInfo.ResultCode == Result.Success)
                {
                    Debug.Log("Login succeeded");
                }
                else if (Common.IsOperationComplete(loginCallbackInfo.ResultCode))
                {
                    Debug.Log("Login failed: " + loginCallbackInfo.ResultCode);
                }
            });
        }

        // Calling tick on a regular interval is required for callbacks to work.
        private void Update()
        {
            if (platformInterface != null)
            {
                platformTickTimer += Time.deltaTime;

                if (platformTickTimer >= platformTickInterval)
                {
                    platformTickTimer = 0;
                    platformInterface.Tick();
                }
            }
        }
    }
}