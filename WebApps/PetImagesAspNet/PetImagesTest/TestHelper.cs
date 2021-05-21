namespace PetImagesTest
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    public static class TestHelper
    {
        public static T Clone<T>(T obj)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));
        }
    }
}
