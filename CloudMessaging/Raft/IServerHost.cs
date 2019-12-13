// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Interface of a host that wraps the <see cref="Server"/> state machine
    /// instance executing in each Raft service process.
    /// </summary>
    public interface IServerHost
    {
        /// <summary>
        /// Starts executing the hosted server.
        /// </summary>
        Task RunAsync(CancellationToken cancellationToken);
    }
}
