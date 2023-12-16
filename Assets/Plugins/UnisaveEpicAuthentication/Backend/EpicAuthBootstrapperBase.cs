using Unisave.Bootstrapping;
using Unisave.Foundation;

namespace Unisave.EpicAuthentication
{
    /// <summary>
    /// Base class for your Epic Auth asset bootstrapper
    /// </summary>
    public abstract class EpicAuthBootstrapperBase : Bootstrapper
    {
        /// <summary>
        /// URL of the Epic Games JSON Web Key Store
        /// (for the Auth interface only)
        /// </summary>
        public string AuthInterfaceJwksUrl { get; private set; }
        
        /// <summary>
        /// URL of the Epic Games JSON Web Key Store
        /// (for the Auth interface only)
        /// </summary>
        public string ConnectInterfaceJwksUrl { get; private set; }

        /// <summary>
        /// Downloads, refreshes, and remembers the JWKS keys
        /// (for the Auth interface only)
        /// </summary>
        public EpicJwksCache AuthInterfaceJwksCache { get; private set; }
        
        /// <summary>
        /// Downloads, refreshes, and remembers the JWKS keys
        /// (for the Connect interface only)
        /// </summary>
        public EpicJwksCache ConnectInterfaceJwksCache { get; private set; }
        
        public override void Main()
        {
            var env = Services.Resolve<EnvStore>();
            
            // The default URL is taken from here:
            // https://dev.epicgames.com/docs/epic-account-services
            // /auth/auth-interface#validating-id-tokens-on-game-server-using-sdk
            AuthInterfaceJwksUrl = env.GetString(
                "EPIC_JWKS_URL_AUTH",
                "https://api.epicgames.dev/epic/oauth/v2/.well-known/jwks.json"
            );
            
            // The default URL is taken from here:
            // https://dev.epicgames.com/docs/game-services
            // /eos-connect-interface#validating-id-tokens-on-game-server-using-sdk
            ConnectInterfaceJwksUrl = env.GetString(
                "EPIC_JWKS_URL_CONNECT",
                "https://api.epicgames.dev/auth/v1/oauth/jwks"
            );

            // create the JWKS cache services
            AuthInterfaceJwksCache = new EpicJwksCache(AuthInterfaceJwksUrl);
            ConnectInterfaceJwksCache = new EpicJwksCache(ConnectInterfaceJwksUrl);
            
            // make this bootstrapper instance be the one
            // used by the epic auth module
            Services.RegisterInstance<EpicAuthBootstrapperBase>(this);
        }

        /// <summary>
        /// Finds the player based on Epic Account ID, PUID, or both.
        /// If no such player exists, returns null.
        /// </summary>
        /// <param name="epicAccountId"></param>
        /// <param name="epicProductUserId"></param>
        /// <returns>ArangoDB document ID of the player</returns>
        public abstract string FindPlayer(
            string epicAccountId,
            string epicProductUserId
        );
        
        /// <summary>
        /// Creates a new player document for the given IDs.
        /// One of the two IDs may be null, depending on your use case.
        /// </summary>
        /// <param name="epicAccountId"></param>
        /// <param name="epicProductUserId"></param>
        /// <returns></returns>
        public abstract string RegisterNewPlayer(
            string epicAccountId,
            string epicProductUserId
        );

        /// <summary>
        /// Called after a successful login
        /// </summary>
        /// <param name="documentId">Document ID of the logged in player</param>
        /// <param name="epicAccountId">Epic Account ID of the logged in player</param>
        /// <param name="epicProductUserId">PUID of the logged in player</param>
        public virtual void PlayerHasLoggedIn(
            string documentId,
            string epicAccountId,
            string epicProductUserId
        )
        {
            // do nothing by default
        }
    }
}