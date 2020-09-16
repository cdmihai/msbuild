// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
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

        private static readonly IReadOnlyCollection<string> EmptyStringList = new string[0];

        public CacheResult(
            CacheResultType resultType,
            string details,
            IReadOnlyCollection<string> warnings = null,
            IReadOnlyCollection<string> errors = null
        )
        {
            ResultType = resultType;
            Details = details;
            Warnings = warnings ?? EmptyStringList;
            Errors = errors ?? EmptyStringList;

            if (resultType != CacheResultType.CacheError)
            {
                ErrorUtilities.VerifyThrowArgument(Errors == null || Errors.Count == 0, "Only cache error results can have errors");
            }
        }
    }
}
