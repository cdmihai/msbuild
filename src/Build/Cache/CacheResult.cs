// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Build.Shared;

namespace Microsoft.Build.Cache
{
    public enum CacheResultType
    {
        CacheHit,
        CacheMiss,
        CacheNotApplicable,
        CacheError
    }

    public class CacheResult
    {
        internal CacheResultType ResultType { get; }
        internal string Details { get; }
        internal IReadOnlyCollection<string> Warnings { get; }
        internal IReadOnlyCollection<string> Errors { get; }

        public CacheResult(
            CacheResultType resultType,
            string details,
            IReadOnlyCollection<string> warnings,
            IReadOnlyCollection<string> errors
        )
        {
            ResultType = resultType;
            Details = details;
            Warnings = warnings;
            Errors = errors;

            if (resultType != CacheResultType.CacheError)
            {
                ErrorUtilities.VerifyThrowArgument(Errors == null || Errors.Count == 0, "Only cache error results can have errors");
            }
        }
    }
}
