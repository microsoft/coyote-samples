// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public class InMemoryDbCollection : IDbCollection
    {
        private readonly Dictionary<string, DbRow> Collection;

        public InMemoryDbCollection()
        {
            this.Collection = new Dictionary<string, DbRow>();
        }

        public Task<bool> CreateRow(string key, string value)
        {
            return Task.Run(() =>
            {
                lock (this.Collection)
                {
                    // Generate a new ETag when creating a brand new row.
                    var dbRow = new DbRow()
                    {
                        Value = value,
                        ETag = Guid.NewGuid()
                    };

                    bool success = this.Collection.TryAdd(key, dbRow);
                    if (!success)
                    {
                        throw new RowAlreadyExistsException();
                    }

                    return true;
                }
            });
        }

        public Task<bool> DoesRowExist(string key)
        {
            return Task.Run(() =>
            {
                lock (this.Collection)
                {
                    return this.Collection.ContainsKey(key);
                }
            });
        }

        public Task<(string value, Guid etag)> GetRow(string key)
        {
            return Task.Run(() =>
            {
                lock (this.Collection)
                {
                    bool success = this.Collection.TryGetValue(key, out DbRow dbRow);
                    if (!success)
                    {
                        throw new RowNotFoundException();
                    }

                    return (dbRow.Value, dbRow.ETag);
                }
            });
        }

        public Task<bool> UpdateRow(string key, string value, Guid etag)
        {
            return Task.Run(() =>
            {
                lock (this.Collection)
                {
                    bool success = this.Collection.TryGetValue(key, out DbRow existingDbRow);
                    if (!success)
                    {
                        throw new RowNotFoundException();
                    }
                    else if (etag != existingDbRow.ETag)
                    {
                        throw new MismatchedETagException();
                    }

                    // Update the Etag value when updating the row.
                    var dbRow = new DbRow()
                    {
                        Value = value,
                        ETag = Guid.NewGuid()
                    };

                    this.Collection[key] = dbRow;
                    return true;
                }
            });
        }

        public Task<bool> DeleteRow(string key)
        {
            return Task.Run(() =>
            {
                lock (this.Collection)
                {
                    bool success = this.Collection.Remove(key, out DbRow _);
                    if (!success)
                    {
                        throw new RowNotFoundException();
                    }

                    return true;
                }
            });
        }
    }
}
