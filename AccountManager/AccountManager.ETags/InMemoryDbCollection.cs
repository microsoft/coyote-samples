// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public class InMemoryDbCollection : IDbCollection
    {
        private readonly ConcurrentDictionary<string, AccountEntity> Collection;

        public InMemoryDbCollection()
        {
            this.Collection = new ConcurrentDictionary<string, AccountEntity>();
        }

        public Task<bool> CreateRow(string key, string value)
        {
            return Task.Run(() =>
            {
                var entity = new AccountEntity()
                {
                    Account = value,
                    ETag = Guid.NewGuid()
                };

                bool success = this.Collection.TryAdd(key, entity);
                if (!success)
                {
                    throw new RowAlreadyExistsException();
                }

                return true;
            });
        }

        public Task<bool> DoesRowExist(string key)
        {
            return Task.Run(() =>
            {
                return this.Collection.ContainsKey(key);
            });
        }

        public Task<(string value, Guid etag)> GetRow(string key)
        {
            return Task.Run(() =>
            {
                bool success = this.Collection.TryGetValue(key, out AccountEntity entity);
                if (!success)
                {
                    throw new RowNotFoundException();
                }

                return (entity.Account, entity.ETag);
            });
        }

        public Task<bool> UpdateRow(string key, string value, Guid etag)
        {
            return Task.Run(() =>
            {
                lock (this.Collection)
                {
                    bool success = this.Collection.TryGetValue(key, out AccountEntity existingEntity);
                    if (!success)
                    {
                        throw new RowNotFoundException();
                    }
                    else if (success && etag != existingEntity.ETag)
                    {
                        throw new MismatchedETagException();
                    }

                    var entity = new AccountEntity()
                    {
                        Account = value,
                        ETag = Guid.NewGuid()
                    };

                    this.Collection[key] = entity;
                    return true;
                }
            });
        }

        public Task<bool> DeleteRow(string key)
        {
            return Task.Run(() =>
            {
                bool success = this.Collection.TryRemove(key, out AccountEntity _);
                if (!success)
                {
                    throw new RowNotFoundException();
                }

                return true;
            });
        }
    }
}
