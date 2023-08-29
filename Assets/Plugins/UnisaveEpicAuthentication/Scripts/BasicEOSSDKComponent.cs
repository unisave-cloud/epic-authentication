using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using UnityEngine;

namespace Unisave.EpicAuthentication
{
    /// <summary>
    /// Basic component that initializes the PlatformInterface instance
    /// used to communicate with Epic Game Services.
    /// You can use this code as-is, or copy and modify if need to.
    ///
    /// The code is based on:
    /// https://dev.epicgames.com/docs/epic-online-services/eos-get-started/eossdkc-sharp-getting-started#unity-eossdk-component
    /// </summary>
    public class BasicEOSSDKComponent : MonoBehaviour
    {
        public string productName = "MyUnityApplication";
        public string productVersion = "1.0";
        public string productId = "";
        public string sandboxId = "";
        public string deploymentId = "";
        public string clientId = "";
        public string clientSecret = "";

        /// <summary>
        /// Publicly accessible instance of this component 
        /// </summary>
        public static BasicEOSSDKComponent Instance { get; private set; }
        
        private void Awake()
        {
            Instance = this;
            LoadNativeSDK();
            InitializePlatformInterface();
        }

        private void Start()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void Update()
        {
            UpdatePlatformInterface();
        }

        private void OnApplicationQuit()
        {
            ReleasePlatformInterface();
            UnloadNativeSDK();
        }
        
        #region "SDK Library management"

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

        private void LoadNativeSDK()
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

        private void UnloadNativeSDK()
        {
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
        
        #endregion
        
        #region "PlatformInterface management"

        public LogCategory logCategory = LogCategory.AllCategories;
        public LogLevel logLevel = LogLevel.Warning;
        
        public PlatformInterface PlatformInterface { get; private set; }
        private const float PlatformTickInterval = 0.1f;
        private float platformTickTimer = 0f;

        void InitializePlatformInterface()
        {
            var initializeOptions = new InitializeOptions {
                ProductName = productName,
                ProductVersion = productVersion
            };

            var initializeResult = PlatformInterface.Initialize(ref initializeOptions);
            if (initializeResult != Result.Success)
            {
                throw new Exception("Failed to initialize platform: " + initializeResult);
            }

            // set up logging
            LoggingInterface.SetLogLevel(logCategory, logLevel);
            LoggingInterface.SetCallback((ref LogMessage logMessage) => {
                switch (logMessage.Level)
                {
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        Debug.LogError("[EOSSDK]: " + logMessage.Message);
                        break;
                    
                    case LogLevel.Warning:
                        Debug.LogError("[EOSSDK]: " + logMessage.Message);
                        break;
                    
                    default:
                        Debug.Log("[EOSSDK]: " + logMessage.Message);
                        break;
                }
            });

            var options = new Options {
                ProductId = productId,
                SandboxId = sandboxId,
                DeploymentId = deploymentId,
                ClientCredentials = new ClientCredentials {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                }
            };

            PlatformInterface = PlatformInterface.Create(ref options);
            if (PlatformInterface == null)
            {
                throw new Exception("Failed to create platform");
            }
        }

        // Calling tick on a regular interval is required for callbacks to work.
        private void UpdatePlatformInterface()
        {
            if (PlatformInterface == null)
                return;
            
            platformTickTimer += Time.deltaTime;
            if (platformTickTimer >= PlatformTickInterval)
            {
                platformTickTimer = 0;
                PlatformInterface.Tick();
            }
        }
        
        private void ReleasePlatformInterface()
        {
            if (PlatformInterface != null)
            {
                PlatformInterface.Release();
                PlatformInterface = null;
                PlatformInterface.Shutdown();
            }
        }
        
        #endregion
        
        #region "EOS Authentication (Auth & Connect interfaces)"

        /// <summary>
        /// Scope flags need to exactly match those that you have defined
        /// in the Developer Portal
        /// </summary>
        public AuthScopeFlags[] scopeFlags = new AuthScopeFlags[] {
            AuthScopeFlags.BasicProfile,
            AuthScopeFlags.FriendsList,
            AuthScopeFlags.Presence,
            AuthScopeFlags.Country
        };
        
        private AuthScopeFlags GetCombinedScopeFlags()
        {
            AuthScopeFlags flags = AuthScopeFlags.NoFlags;
            foreach (AuthScopeFlags f in scopeFlags)
                flags |= f;
            return flags;
        }

        /// <summary>
        /// Tries to login the game client via the Auth interface using the
        /// Account Portal overlay UI (or a web browser), that asks the
        /// player for credentials directly inside your game.
        /// Works even inside Unity Editor.
        /// </summary>
        public Task<LoginCallbackInfo> AuthLoginViaAccountPortal()
        {
            TaskCompletionSource<LoginCallbackInfo> tcs
                = new TaskCompletionSource<LoginCallbackInfo>();
            
            var loginOptions = new LoginOptions {
                Credentials = new Credentials {
                    Type = LoginCredentialType.AccountPortal,
                    Id = null,
                    Token = null
                },
                ScopeFlags = GetCombinedScopeFlags()
            };

            AuthInterface auth = PlatformInterface.GetAuthInterface();

            auth.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => {
                tcs.SetResult(info);
            });

            return tcs.Task;
        }
        
        /// <summary>
        /// Tries to login the game client via the Auth interface using the
        /// Exchange Code method. The exchange code is extracted from the
        /// command line arguments as given by the Epic Launcher. If the game
        /// is not launched via the Epic Launcher, the login will fail
        /// with result code <see cref="Result.AuthExchangeCodeNotFound"/>.
        /// </summary>
        public Task<LoginCallbackInfo> AuthLoginViaEpicLauncher()
        {
            TaskCompletionSource<LoginCallbackInfo> tcs
                = new TaskCompletionSource<LoginCallbackInfo>();

            if (!IsLaunchedViaEpicLauncher())
            {
                tcs.SetResult(new LoginCallbackInfo {
                    ResultCode = Result.AuthExchangeCodeNotFound
                });
                
                return tcs.Task;
            }
            
            var loginOptions = new LoginOptions {
                Credentials = new Credentials {
                    Type = LoginCredentialType.ExchangeCode,
                    Id = null,
                    Token = GetExchangeCode()
                },
                ScopeFlags = GetCombinedScopeFlags()
            };

            AuthInterface auth = PlatformInterface.GetAuthInterface();

            auth.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => {
                tcs.SetResult(info);
            });

            return tcs.Task;
        }
        
        private bool IsLaunchedViaEpicLauncher()
        {
            string[] args = Environment.GetCommandLineArgs();
            return args.Contains("-EpicPortal");
        }
        
        private string GetExchangeCode()
        {
            const string prefix = "-AUTH_PASSWORD=";
            string[] args = Environment.GetCommandLineArgs();
            string arg = args.FirstOrDefault(arg => arg.StartsWith(prefix));
            return arg?.Substring(prefix.Length);
        }
        
        /// <summary>
        /// Tries to login the game client via the Auth interface using the
        /// Developer Authentication Tool that comes with the EOS SDK.
        /// This method is best suited for the Unity Editor.
        /// </summary>
        public Task<LoginCallbackInfo> AuthLoginViaDeveloperAuthTool(
            string host = "localhost:6547",
            string credentialName = "me"
        )
        {
            TaskCompletionSource<LoginCallbackInfo> tcs
                = new TaskCompletionSource<LoginCallbackInfo>();

            var loginOptions = new LoginOptions {
                Credentials = new Credentials {
                    Type = LoginCredentialType.Developer,
                    Id = host,
                    Token = credentialName
                },
                ScopeFlags = GetCombinedScopeFlags()
            };

            AuthInterface auth = PlatformInterface.GetAuthInterface();

            auth.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => {
                tcs.SetResult(info);
            });

            return tcs.Task;
        }
        
        /// <summary>
        /// Tries to login the game client via the Auth interface using all
        /// available methods in order: Developer, Epic Launcher, Account Portal
        /// </summary>
        /// <returns></returns>
        public async Task<LoginCallbackInfo> AuthLogin(
            bool authToolOnlyInEditor = true,
            string authToolHost = "localhost:6547",
            string authToolCredentialName = "me"
        )
        {
            LoginCallbackInfo info;
            
            if (Application.isEditor || !authToolOnlyInEditor)
            {
                info = await AuthLoginViaDeveloperAuthTool(
                    authToolHost, authToolCredentialName
                );

                if (info.ResultCode == Result.Success || info.ResultCode == Result.Canceled)
                    return info;
            }

            info = await AuthLoginViaEpicLauncher();
            
            if (info.ResultCode == Result.Success || info.ResultCode == Result.Canceled)
                return info;
            
            return await AuthLoginViaAccountPortal();
        }
        
        #endregion
    }
}