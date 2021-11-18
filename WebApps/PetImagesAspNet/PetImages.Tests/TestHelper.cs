// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace PetImages.Tests
{
    public static class TestHelper
    {
        public static T Clone<T>(T obj)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));
        }
    }
}
