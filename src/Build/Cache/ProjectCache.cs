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
        /// <summary>
        /// Called once before the build to instantiate the plugin state.
        /// </summary>
        public abstract void Initialize(CacheContext context);

        /// <summary>
        /// Called for each node in the graph.
        /// Operation needs to be atomic. Any side effects (IO, environment variables, etc) need to be reverted upon cancellation.
        /// </summary>
        public abstract Task<CacheResult> GetCacheResultAsync(
            ProjectGraphNode node,
            IReadOnlyCollection<string> entryTargets,
            CancellationToken cancellationToken);

        /// <summary>
        /// Called once after the build to let the plugin do any post build operations (log metrics, cleanup, etc).
        /// </summary>
        public abstract void AfterGraphWalk();
    }
}
