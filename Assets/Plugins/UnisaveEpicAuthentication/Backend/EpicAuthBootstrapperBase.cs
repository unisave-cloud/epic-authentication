using Unisave.Bootstrapping;

namespace Unisave.EpicAuthentication
{
    /// <summary>
    /// Base class for your Epic Auth asset bootstrapper
    /// </summary>
    public abstract class EpicAuthBootstrapperBase : Bootstrapper
    {
        public override void Main()
        {
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