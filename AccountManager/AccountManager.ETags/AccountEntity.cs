// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public class AccountEntity
    {
        public string Account { get; set; }

        public Guid ETag { get; set; }
    }
}
