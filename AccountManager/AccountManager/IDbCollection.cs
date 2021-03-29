// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager
{
    public interface IDbCollection
    {
        Task CreateRow(string key, string value);

        Task<bool> DoesRowExist(string key);

        Task<string> GetRow(string key);

        Task DeleteRow(string key);
    }
}
