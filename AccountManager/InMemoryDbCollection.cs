// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager
{
    public class InMemoryDbCollection : IDbCollection
    {
        private readonly ConcurrentDictionary<string, string> Collection;

        public InMemoryDbCollection()
        {
            this.Collection = new ConcurrentDictionary<string, string>();
        }

        public Task CreateRow(string key, string value)
        {
            return Task.Run(() =>
            {
                var result = this.Collection.TryAdd(key, value);
                if (!result)
                {
                    throw new RowAlreadyExistsException();
                }
            });
        }

        public Task DeleteRow(string key)
        {
            return Task.Run(() =>
            {
                var removed = this.Collection.TryRemove(key, out string value);
                if (!removed)
                {
                    throw new RowNotFoundException();
                }
            });
        }

        public Task<bool> DoesRowExist(string key)
        {
            return Task.Run(() =>
            {
                return this.Collection.ContainsKey(key);
            });
        }

        public Task<string> GetRow(string key)
        {
            return Task.Run(() =>
            {
                var result = this.Collection.TryGetValue(key, out string value);
                if (!result)
                {
                    throw new RowNotFoundException();
                }
                return value;
            });
        }
    }
}
