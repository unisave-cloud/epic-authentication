using System;
using Unisave.Entities;

namespace Unisave.EpicAuthentication.Examples.SimpleDemo
{
    public class PlayerEntity : Entity
    {
        public string epicAccountId;
        public string epicProductUserId;
        public DateTime lastLoginAt = DateTime.UtcNow;
    }
}