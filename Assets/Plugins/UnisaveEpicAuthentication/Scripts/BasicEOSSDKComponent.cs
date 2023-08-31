using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
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
        [Header("Informative Metadata")]
        public string productName = "MyUnityApplication";
        public string productVersion = "1.0";
        public bool useProjectSettingsForNameAndVersion = false;
        
        [Header("Platform Interface Initialization")]
        public string productId = "";
        public string sandboxId = "";
        public string deploymentId = "";
        public string clientId = "";
        public string clientSecret = "";
        public bool initializeOnStartNotAwake = false;

        /// <summary>
        /// Publicly accessible instance of this component 
        /// </summary>
        public static BasicEOSSDKComponent Instance { get; private set; }
        
        private void Awake()
        {
            Instance = this;
            LoadNativeSDK();
            
            if (!initializeOnStartNotAwake)
                InitializePlatformInterface();
        }

        private void Start()
        {
            if (initializeOnStartNotAwake)
                InitializePlatformInterface();
            
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

        [Header("EOS SDK Logging")]
        public LogCategory logCategory = LogCategory.AllCategories;
        public LogLevel logLevel = LogLevel.Warning;
        
        public PlatformInterface PlatformInterface { get; private set; }
        private const float PlatformTickInterval = 0.1f;
        private float platformTickTimer = 0f;

        void InitializePlatformInterface()
        {
            string name = productName;
            string version = productVersion;

            if (useProjectSettingsForNameAndVersion)
            {
                name = Application.productName;
                version = Application.version;
            }
            
            var initializeOptions = new InitializeOptions {
                ProductName = name,
                ProductVersion = version
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
        /// Holds the logged-in Epic Account ID, or null if nobody logged in
        /// (tracks only the Auth interface logins)
        /// </summary>
        public EpicAccountId EpicAccountId { get; private set; }
        
        /// <summary>
        /// Holds the logged-in Product User ID, or null if nobody logged in
        /// (tracks only the Connect interface logins)
        /// </summary>
        public ProductUserId ProductUserId { get; private set; }

        /// <summary>
        /// Holds the handle representing the registered refreshing callback
        /// </summary>
        private ulong connectLoginRefreshingHandle = 0;
        
        /// <summary>
        /// Scope flags need to exactly match those that you have defined
        /// in the Developer Portal
        /// </summary>
        [Header("Application Account Services Permissions")]
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
        public Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLoginViaAccountPortal()
        {
            TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo> tcs
                = new TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();
            
            var loginOptions = new Epic.OnlineServices.Auth.LoginOptions {
                Credentials = new Epic.OnlineServices.Auth.Credentials {
                    Type = LoginCredentialType.AccountPortal,
                    Id = null,
                    Token = null
                },
                ScopeFlags = GetCombinedScopeFlags()
            };

            AuthInterface auth = PlatformInterface.GetAuthInterface();

            auth.Login(
                ref loginOptions,
                null,
                (ref Epic.OnlineServices.Auth.LoginCallbackInfo info) => {
                    if (info.ResultCode == Result.Success)
                        EpicAccountId = info.LocalUserId;
                    
                    if (Common.IsOperationComplete(info.ResultCode))
                        tcs.SetResult(info);
                }
            );

            return tcs.Task;
        }
        
        /// <summary>
        /// Tries to login the game client via the Auth interface using the
        /// Exchange Code method. The exchange code is extracted from the
        /// command line arguments as given by the Epic Launcher. If the game
        /// is not launched via the Epic Launcher, the login will fail
        /// with result code <see cref="Result.AuthExchangeCodeNotFound"/>.
        /// </summary>
        public Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLoginViaEpicLauncher()
        {
            TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo> tcs
                = new TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();

            if (!IsLaunchedViaEpicLauncher())
            {
                tcs.SetResult(new Epic.OnlineServices.Auth.LoginCallbackInfo {
                    ResultCode = Result.AuthExchangeCodeNotFound
                });
                
                return tcs.Task;
            }
            
            var loginOptions = new Epic.OnlineServices.Auth.LoginOptions {
                Credentials = new Epic.OnlineServices.Auth.Credentials {
                    Type = LoginCredentialType.ExchangeCode,
                    Id = null,
                    Token = GetExchangeCode()
                },
                ScopeFlags = GetCombinedScopeFlags()
            };

            AuthInterface auth = PlatformInterface.GetAuthInterface();

            auth.Login(
                ref loginOptions,
                null,
                (ref Epic.OnlineServices.Auth.LoginCallbackInfo info) => {
                    if (info.ResultCode == Result.Success)
                        EpicAccountId = info.LocalUserId;
                    
                    if (Common.IsOperationComplete(info.ResultCode))
                        if (Common.IsOperationComplete(info.ResultCode))
                            tcs.SetResult(info);
                }
            );

            return tcs.Task;
        }
        
        public bool IsLaunchedViaEpicLauncher()
        {
            string[] args = Environment.GetCommandLineArgs();
            return args.Contains("-EpicPortal");
        }
        
        public string GetExchangeCode()
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
        public Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLoginViaDeveloperAuthTool(
            string host = "localhost:6547",
            string credentialName = "me"
        )
        {
            TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo> tcs
                = new TaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();

            var loginOptions = new Epic.OnlineServices.Auth.LoginOptions {
                Credentials = new Epic.OnlineServices.Auth.Credentials {
                    Type = LoginCredentialType.Developer,
                    Id = host,
                    Token = credentialName
                },
                ScopeFlags = GetCombinedScopeFlags()
            };

            AuthInterface auth = PlatformInterface.GetAuthInterface();

            auth.Login(
                ref loginOptions,
                null,
                (ref Epic.OnlineServices.Auth.LoginCallbackInfo info) => {
                    if (info.ResultCode == Result.Success)
                        EpicAccountId = info.LocalUserId;
                    
                    if (Common.IsOperationComplete(info.ResultCode))
                        tcs.SetResult(info);
                }
            );

            return tcs.Task;
        }
        
        /// <summary>
        /// Tries to login the game client via the Auth interface using all
        /// available methods in order: Developer, Epic Launcher, Account Portal
        /// </summary>
        /// <returns></returns>
        public async Task<Epic.OnlineServices.Auth.LoginCallbackInfo> AuthLogin(
            bool authToolOnlyInEditor = true,
            string authToolHost = "localhost:6547",
            string authToolCredentialName = "me"
        )
        {
            Epic.OnlineServices.Auth.LoginCallbackInfo info;
            
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

        /// <summary>
        /// Tries to login the game client via the Connect interface using
        /// the previous successful Auth interface login
        /// </summary>
        public Task<Epic.OnlineServices.Connect.LoginCallbackInfo> ConnectLoginViaAuth()
        {
            if (EpicAccountId == null)
                throw new InvalidOperationException(
                    "You cannot login into Connect via Auth, because there " +
                    "is nobody logged in via Auth."
                );
            
            TaskCompletionSource<Epic.OnlineServices.Connect.LoginCallbackInfo> tcs
                = new TaskCompletionSource<Epic.OnlineServices.Connect.LoginCallbackInfo>();
            
            AuthInterface auth = PlatformInterface.GetAuthInterface();
            ConnectInterface connect = PlatformInterface.GetConnectInterface();
            
            var copyOptions = new Epic.OnlineServices.Auth.CopyIdTokenOptions() {
                AccountId = EpicAccountId
            };
            auth.CopyIdToken(
                ref copyOptions,
                out Epic.OnlineServices.Auth.IdToken? idToken
            );

            var loginOptions = new Epic.OnlineServices.Connect.LoginOptions() {
                Credentials = new Epic.OnlineServices.Connect.Credentials() {
                    Type = ExternalCredentialType.EpicIdToken,
                    Token = idToken?.JsonWebToken
                }
            };
            connect.Login(
                ref loginOptions,
                null,
                (ref Epic.OnlineServices.Connect.LoginCallbackInfo info) => {
                    if (info.ResultCode == Result.Success)
                    {
                        ProductUserId = info.LocalUserId;
                        StartRefreshingConnectLogin();
                    }
                    
                    if (Common.IsOperationComplete(info.ResultCode))
                        tcs.SetResult(info);
                }
            );

            return tcs.Task;
        }

        /// <summary>
        /// Tries to login via the Connect interface using the existing
        /// Auth interface login, and if there is no Product User,
        /// it will not fail, but create a new one.
        /// </summary>
        public async Task<Result> ConnectLoginOrRegister()
        {
            // try login
            var loginInfo = await ConnectLoginViaAuth();
            
            // if there is no user, try to create one
            if (loginInfo.ResultCode == Result.InvalidUser)
            {
                // create new user
                CreateUserCallbackInfo createInfo = await ConnectCreateProductUser(
                    loginInfo.ContinuanceToken
                );

                // if that works, try login again
                if (createInfo.ResultCode == Result.Success)
                {
                    var secondLoginInfo = await ConnectLoginViaAuth();

                    // regardless of the outcome, this is the final result
                    // (successful or not)
                    return secondLoginInfo.ResultCode;
                }

                // user creation failed
                return createInfo.ResultCode;
            }

            // first login failed due to something other than missing user
            return loginInfo.ResultCode;
        }
        
        /// <summary>
        /// Creates a new product user from a failed Connect login attempt
        /// (failed as <see cref="Result.InvalidUser"/>, which means product
        /// user does not exist and may be created)
        /// </summary>
        /// <param name="continuanceToken">
        /// Continuance token received from the login attempt
        /// </param>
        public Task<CreateUserCallbackInfo> ConnectCreateProductUser(
            ContinuanceToken continuanceToken
        )
        {
            ConnectInterface connect = PlatformInterface.GetConnectInterface();

            TaskCompletionSource<CreateUserCallbackInfo> tcs
                = new TaskCompletionSource<CreateUserCallbackInfo>();

            var createOptions = new CreateUserOptions() {
                ContinuanceToken = continuanceToken
            };
            connect.CreateUser(
                ref createOptions,
                null,
                (ref CreateUserCallbackInfo info) => {
                    if (Common.IsOperationComplete(info.ResultCode))
                        tcs.SetResult(info);
                }
            );

            return tcs.Task;
        }

        /// <summary>
        /// Registers the Connect login refreshing callback
        /// </summary>
        private void StartRefreshingConnectLogin()
        {
            // do nothing if a callback is already registered
            if (connectLoginRefreshingHandle != 0)
                return;
            
            var options = new AddNotifyAuthExpirationOptions() {
                // empty
            };
            
            ConnectInterface connect = PlatformInterface.GetConnectInterface();

            connectLoginRefreshingHandle = connect.AddNotifyAuthExpiration(
                ref options,
                null,
                (ref AuthExpirationCallbackInfo info) => {
                    PerformConnectLoginRefresh(info);
                }
            );
        }

        /// <summary>
        /// Removes the Connect login refreshing callback
        /// </summary>
        private void StopRefreshingConnectLogin()
        {
            // do nothing if no callback is registered
            if (connectLoginRefreshingHandle == 0)
                return;
            
            ConnectInterface connect = PlatformInterface.GetConnectInterface();
            
            connect.RemoveNotifyAuthExpiration(connectLoginRefreshingHandle);
            connectLoginRefreshingHandle = 0;
        }

        /// <summary>
        /// Triggered by EOS SDK when the Connect login is 10 min from expiring
        /// </summary>
        private async void PerformConnectLoginRefresh(AuthExpirationCallbackInfo info)
        {
            // the expiring PUID is different than the logged-in PUID we know about
            if (info.LocalUserId.ToString() != ProductUserId.ToString())
                return;

            // there is no Epic Account logged in to refresh with
            if (EpicAccountId == null)
            {
                Debug.LogError(
                    $"[{nameof(BasicEOSSDKComponent)}]: Connect login refresh " +
                    $"failed: There is no logged in Epic Account anymore."
                );
                return;
            }

            Epic.OnlineServices.Connect.LoginCallbackInfo loginInfo
                = await ConnectLoginViaAuth();

            if (loginInfo.ResultCode != Result.Success)
            {
                Debug.LogError(
                    $"[{nameof(BasicEOSSDKComponent)}]: Connect login refresh " +
                    $"failed: {loginInfo.ResultCode}"
                );
            }
        }
        
        #endregion
    }
}