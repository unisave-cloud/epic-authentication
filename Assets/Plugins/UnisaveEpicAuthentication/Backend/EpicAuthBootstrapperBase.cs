using Unisave.Facades;
using Unisave.Foundation;

namespace Unisave.EpicAuthentication
{
    /// <summary>
    /// Base class for your Epic Auth asset bootstrapper
    /// </summary>
    public abstract class EpicAuthBootstrapperBase : Bootstrapper
    {
        public override void Main()
        {
            // use the bootstrapper itself instead of a config object
            // (because we don't have much to configure)
            Facade.App.Instance<EpicAuthBootstrapperBase>(this);
        }

        /// <summary>
        /// Call this when you want to use the bootstrapper configuration provided by the user
        /// </summary>
        /// <returns></returns>
        public static EpicAuthBootstrapperBase Resolve()
        {
            // TODO: HACK: this will be a part of the framework and not needed
            Bootstrapper.AssertRan();
            
            return Facade.App.Resolve<EpicAuthBootstrapperBase>();
        }

        /// <summary>
        /// Finds the player based on Epic Account ID, PUID, or both.
        /// If no such player exists, returns null.
        /// </summary>
        /// <param name="epicAccountId"></param>
        /// <param name="epicProductUserId"></param>
        /// <returns>ArangoDB document ID of the player</returns>
        public abstract string FindPlayer(string epicAccountId, string epicProductUserId);
        
        /// <summary>
        /// Creates a new player document for the given IDs.
        /// One of the two IDs may be null, depending on your use case.
        /// </summary>
        /// <param name="epicAccountId"></param>
        /// <param name="epicProductUserId"></param>
        /// <returns></returns>
        public abstract string RegisterNewPlayer(string epicAccountId, string epicProductUserId);

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