using System;
using Unisave.EpicAuthentication;
using Unisave.Facades;

namespace Unisave.EpicAuthentication.Examples.SimpleDemo
{
    public class EpicAuthBootstrapper : EpicAuthBootstrapperBase
    {
        public override string FindPlayer(string epicAccountId, string epicProductUserId)
        {
            // find by epic account ID
            if (epicAccountId != null)
            {
                PlayerEntity player = DB.TakeAll<PlayerEntity>()
                    .Filter(e => e.epicAccountId == epicAccountId)
                    .First();

                return player?.EntityId;
            }
            
            // else find by PUID
            if (epicProductUserId != null)
            {
                PlayerEntity player = DB.TakeAll<PlayerEntity>()
                    .Filter(e => e.epicProductUserId == epicProductUserId)
                    .First();

                return player?.EntityId;
            }

            // else there is no such player
            return null;
        }

        public override string RegisterNewPlayer(string epicAccountId, string epicProductUserId)
        {
            // create the player entity
            var player = new PlayerEntity {
                epicAccountId = epicAccountId,
                epicProductUserId = epicProductUserId
                
                // ... you can do your own initialization here ...
            };
            
            // insert it to the database to obtain the document ID
            player.Save();

            // return the document ID
            return player.EntityId;
        }
        
        public override void PlayerHasLoggedIn(
            string documentId,
            string epicAccountId,
            string epicProductUserId
        )
        {
            var player = DB.Find<PlayerEntity>(documentId);

            // store the other ID if it's known during this login attempt,
            // but is missing in the entity
            if (player.epicAccountId == null)
                player.epicAccountId = epicAccountId;
            if (player.epicProductUserId == null)
                player.epicProductUserId = epicProductUserId;
            
            // update login timestamp
            player.lastLoginAt = DateTime.UtcNow;

            player.Save();
        }
    }
}