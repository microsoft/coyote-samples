// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace ImageGallery.Logging
{
    public static class RequestId
    {
        private static readonly AsyncLocal<Guid> AsyncLocalInstance = new AsyncLocal<Guid>();

        internal static Guid Create()
        {
            var id = Guid.NewGuid();
            AsyncLocalInstance.Value = id;
            return id;
        }

        public static Guid Get() => AsyncLocalInstance.Value;
    }
}
