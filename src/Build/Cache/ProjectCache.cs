// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Graph;

namespace Microsoft.Build.Cache
{
    /// <summary>
    /// Created and called for each node.
    /// Each project defines multiple of them
    /// </summary>
    public abstract class ProjectCache
    {
        public ProjectCache(ProjectGraphNode node, IReadOnlyCollection<string> entryTargets, CacheContext context) { }

        public abstract Task<CacheResult> GetCacheResultAsync(CancellationToken cancellationToken);
    }

    public abstract class ProjectCacheProvider
    {
        protected ProjectCacheProvider(CacheContext context) { }
    }

    public class TestCache : ProjectCache
    {
        public TestCache(ProjectGraphNode node, IReadOnlyCollection<string> entryTargets, CacheContext context) : base(
            node,
            entryTargets,
            context) { }

        public override Task<CacheResult> GetCacheResultAsync(CancellationToken cancellationToken)
        {
            var results = new List<(CacheResultType, string)>
            {
                (CacheResultType.CacheError, "Error!!"),
                (CacheResultType.CacheHit, "Hit!!"),
                (CacheResultType.CacheMiss, "Miss!!"),
                (CacheResultType.CacheNotApplicable, "Not applicable!!")
            };

            var ret = results.ElementAt(new Random().Next(0, 5));

            return Task.FromResult(new CacheResult(ret.Item1, ret.Item2, null, null));
        }
    }
}
