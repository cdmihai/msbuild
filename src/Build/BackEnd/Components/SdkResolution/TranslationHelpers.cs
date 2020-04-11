﻿using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.BackEnd.SdkResolution
{
    static class SdkResultTranslationHelpers
    {
        public static void Translate(this ITranslator t, ref SdkReference sdkReference)
        {
            string name = null;
            string version = null;
            string minimumVersion = null;

            if (t.Mode == TranslationDirection.WriteToStream)
            {
                name = sdkReference.Name;
                version = sdkReference.Version;
                minimumVersion = sdkReference.MinimumVersion;
            }

            t.Translate(ref name);
            t.Translate(ref version);
            t.Translate(ref minimumVersion);

            if (t.Mode == TranslationDirection.ReadFromStream)
            {
                sdkReference = new SdkReference(name, version, minimumVersion);
            }
        }

        public static void Translate(this ITranslator t, ref SdkResultPathAndVersion pathAndVersion)
        {
            string path = null;
            string version = null;

            if (t.Mode == TranslationDirection.WriteToStream)
            {
                path = pathAndVersion.Path;
                version = pathAndVersion.Version;
            }

            t.Translate(ref path);
            t.Translate(ref version);

            if (t.Mode == TranslationDirection.ReadFromStream)
            {
                pathAndVersion = new SdkResultPathAndVersion(path, version);
            }
        }

        public static void Translate(this ITranslator t, ref SdkResultItem item)
        {
            string itemSpec = null;
            Dictionary<string, string> metadata = null;

            if (t.Mode == TranslationDirection.WriteToStream)
            {
                itemSpec = item.ItemSpec;
                metadata = item.Metadata;
            }

            t.Translate(ref itemSpec);
            t.TranslateDictionary(ref metadata, StringComparer.InvariantCultureIgnoreCase);

            if (t.Mode == TranslationDirection.ReadFromStream)
            {
                item = new SdkResultItem(itemSpec, metadata);
            }
        }
    }
}
