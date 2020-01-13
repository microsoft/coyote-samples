// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.Samples.HelloWorld
{
    internal static class Translations
    {
        public static Dictionary<string, string> HelloWorldTexts { get; } = new Dictionary<string, string>
              {
                 { "Afrikaans", "Hello Wêreld!" },
                 { "Albanian", "Përshendetje Botë!" },
                 { "Amharic", "ሰላም ልዑል!" },
                 { "Arabic", "مرحبا بالعالم!" },
                 { "Armenian", "Բարեւ աշխարհ!" },
                 { "Basque", "Kaixo Mundua!" },
                 { "Belarussian", "Прывітанне Сусвет!" },
                 { "Bengali", "ওহে বিশ্ব!" },
                 { "Bulgarian", "Здравей свят!" },
                 { "Catalan", "Hola món!" },
                 { "Chichewa", "Moni Dziko Lapansi!" },
                 { "Chinese", "你好世界！" },
                 { "Croatian", "Pozdrav svijete!" },
                 { "Czech", "Ahoj světe!" },
                 { "Danish", "Hej Verden!" },
                 { "Dutch", "Hallo Wereld!" },
                 { "English", "Hello World!" },
                 { "Estonian", "Tere maailm!" },
                 { "Finnish", "Hei maailma!" },
                 { "French", "Bonjour monde!" },
                 { "Frisian", "Hallo wrâld!" },
                 { "Georgian", "გამარჯობა მსოფლიო!" },
                 { "German", "Hallo Welt!" },
                 { "Greek", "Γειά σου Κόσμε!" },
                 { "Hausa", "Sannu Duniya!" },
                 { "Hebrew", "שלום עולם!" },
                 { "Hindi", "नमस्ते दुनिया!" },
                 { "Hungarian", "Helló Világ!" },
                 { "Icelandic", "Halló heimur!" },
                 { "Igbo", " Ndewo Ụwa!" },
                 { "Indonesian", "Halo Dunia!" },
                 { "Italian", "Ciao mondo!" },
                 { "Japanese", "こんにちは世界！" },
                 { "Kazakh", "Сәлем Әлем!" },
                 { "Khmer", "សួស្តីពិភពលោក!" },
                 { "Kyrgyz", "Салам дүйнө!" },
                 { "Lao", "  ສະບາຍດີຊາວໂລກ!" },
                 { "Latvian", "Sveika pasaule!" },
                 { "Lithuanian", "Labas pasauli!" },
                 { "Luxemburgish", "Moien Welt!" },
                 { "Macedonian", "Здраво свету!" },
                 { "Malay", "Hai dunia!" },
                 { "Malayalam", "ഹലോ വേൾഡ്!" },
                 { "Mongolian", "Сайн уу дэлхий!" },
                 { "Myanmar", "မင်္ဂလာပါကမ္ဘာလောက!" },
                 { "Nepali", "नमस्कार संसार!" },
                 { "Norwegian", "Hei Verden!" },
                 { "Pashto", "سلام نړی!" },
                 { "Persian", "سلام دنیا!" },
                 { "Polish", "Witaj świecie!" },
                 { "Portuguese", "Olá Mundo!" },
                 { "Punjabi", "ਸਤਿ ਸ੍ਰੀ ਅਕਾਲ ਦੁਨਿਆ!" },
                 { "Romanian", "Salut Lume!" },
        };

        public static List<string> Languages = HelloWorldTexts.Keys.ToList();

        public static int LanguagesCount = Languages.Count;
    }
}
