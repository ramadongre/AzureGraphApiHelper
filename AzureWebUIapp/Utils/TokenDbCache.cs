using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AzureWebUIapp.Models;

namespace WebAppGroupClaimsDotNet.Utils
{
    public class TokenDbCache : TokenCache
    {
        string UserObjectId = string.Empty;
        string CacheId = string.Empty;

        private static Dictionary<string, byte[]> internalCache =
            new Dictionary<string, byte[]>();

        public TokenDbCache(string userId)
        {
            UserObjectId = userId;
            CacheId = UserObjectId + "_TokenCache";

            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            Load();
        }

        public void Load()
        {
            if (internalCache.ContainsKey(CacheId))
                this.Deserialize(internalCache[CacheId]);
            else
            {
                this.Deserialize(null);
            }
        }

        public void Persist()
        {
            // Optimistically set HasStateChanged to false. We need to do it early to avoid losing changes made by a concurrent thread.
            this.HasStateChanged = false;

            // Reflect changes in the persistent store
            if (internalCache.ContainsKey(CacheId))
            {
                internalCache[CacheId] = this.Serialize();
            }
            else
            {
                internalCache.Add(CacheId, this.Serialize());
            }
        }

        // Empties the persistent store.
        public override void Clear()
        {
            base.Clear();
            internalCache.Remove(CacheId);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after ADAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (this.HasStateChanged)
            {
                Persist();
            }
        }
    }
}