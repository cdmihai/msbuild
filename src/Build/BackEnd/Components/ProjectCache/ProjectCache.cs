// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.BackEnd;
using Microsoft.Build.Execution;
using Microsoft.Build.FileSystem;
using Microsoft.Build.Framework;
using Microsoft.Build.Graph;
using Microsoft.Build.Shared;

namespace Microsoft.Build.Experimental.ProjectCache
{
    /// <summary>
    ///     Only one plugin instance can exist for a given BuildManager BeginBuild / EndBuild session.
    /// </summary>
    public abstract class ProjectCacheBase
    {
        /// <summary>
        ///     Called once before the build, to have the plugin instantiate its state.
        /// </summary>
        public abstract Task<bool> BeginBuildAsync(
            CacheContext context,
            PluginLoggerBase logger,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Called once for each node in the graph.
        ///     Operation needs to be atomic. Any side effects (IO, environment variables, etc) need to be reverted upon
        ///     cancellation.
        ///     MSBuild may choose to cancel this method and build the project itself.
        /// </summary>
        public abstract Task<CacheResult> GetCacheResultAsync(
            BuildRequestData node,
            PluginLoggerBase logger,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Called once after all the build to let the plugin do any post build operations (log metrics, cleanup, etc).
        /// </summary>
        public abstract Task EndBuildAsync(PluginLoggerBase logger, CancellationToken cancellationToken);
    }

    /// <summary>
    ///     Either Graph is null, or GraphEntryPoints is null. Not Both.
    /// </summary>
    public class CacheContext
    {
        public CacheContext(
            MSBuildFileSystemBase fileSystem,
            ProjectGraph? graph = null,
            IReadOnlyCollection<ProjectGraphEntryPoint>? graphEntryPoints = null)
        {
            ErrorUtilities.VerifyThrow(
                graph != null || graphEntryPoints != null,
                "Either Graph is null, or GraphEntryPoints is null. Not both.");

            Graph = graph;
            GraphEntryPoints = graphEntryPoints;
            FileSystem = fileSystem;
        }

        public ProjectGraph? Graph { get; }
        public IReadOnlyCollection<ProjectGraphEntryPoint>? GraphEntryPoints { get; }
        public MSBuildFileSystemBase FileSystem { get; }
    }

    /// <summary>
    ///     Events logged with this logger will get pushed into MSBuild's logging infrastructure.
    /// </summary>
    public abstract class PluginLoggerBase
    {
        /// <summary>
        /// See <see cref="ILogger.Verbosity"/>
        /// </summary>
        LoggerVerbosity Verbosity { get; }

        public abstract bool HasLoggedErrors { get; protected set; }

        public abstract void LogMessage(string message);

        public abstract void LogWarning(string warning);

        public abstract void LogError(string error);

        public PluginLoggerBase(LoggerVerbosity verbosity)
        {
            Verbosity = verbosity;
        }
    }

    public enum CacheResultType
    {
        CacheHit,
        CacheMiss,
        CacheNotApplicable,
        CacheError
    }

    /// <summary>
    /// Only cache hits have non null build result information.
    /// </summary>
    public readonly struct CacheResult
    {
        internal CacheResultType ResultType { get; }
        internal BuildResult? BuildResult { get; }
        internal ProxyBuildResults? ProxyBuildResults { get; }

        public CacheResult(
            CacheResultType resultType,
            // null when it's not a cache hit
            ProxyBuildResults? proxyBuildResults = null
        ) : this(resultType)
        {
            if (resultType == CacheResultType.CacheHit && proxyBuildResults == null)
            {
                ErrorUtilities.ThrowArgument("Build Result must be set on cache hits");
            }

            if (resultType != CacheResultType.CacheHit && proxyBuildResults != null)
            {
                ErrorUtilities.ThrowArgument("Build Result must not be set on non cache hits");
            }

            ResultType = resultType;
            ProxyBuildResults = proxyBuildResults;
        }

        public CacheResult(
            CacheResultType resultType,
            // null when it's not a cache hit
            BuildResult? buildResult = null
        ) : this(resultType)
        {
            if (resultType == CacheResultType.CacheHit && buildResult == null)
            {
                ErrorUtilities.ThrowArgument("Build Result must be set on cache hits");
            }

            if (resultType != CacheResultType.CacheHit && buildResult != null)
            {
                ErrorUtilities.ThrowArgument("Build Result must not be set on non cache hits");
            }

            ResultType = resultType;
            BuildResult = buildResult;
        }

        public CacheResult(
            CacheResultType resultType,
            // null when it's not a cache hit
            IReadOnlyCollection<PluginTargetResult>? targetResults
        ) : this(resultType)
        {
            if (resultType == CacheResultType.CacheHit && targetResults == null)
            {
                ErrorUtilities.ThrowArgument("Target results must be set on cache hits");
            }

            if (resultType != CacheResultType.CacheHit && targetResults != null)
            {
                ErrorUtilities.ThrowArgument("Target results must not be set on non cache hits");
            }

            BuildResult = targetResults != null ? ConstructBuildResult(targetResults) : null;
        }

        public CacheResult(CacheResultType resultType)
        {
            BuildResult = null;
            ProxyBuildResults = null;
            ResultType = resultType;
        }

        private static BuildResult ConstructBuildResult(IReadOnlyCollection<PluginTargetResult> targetResults)
        {
            var buildResult = new BuildResult();

            foreach (var pluginTargetResult in targetResults)
            {
                buildResult.AddResultsForTarget(
                    pluginTargetResult.TargetName,
                    new TargetResult(
                        pluginTargetResult.TaskItems.Select(ti => CreateTaskItem(ti)).ToArray(),
                        CreateWorkUnitResult(pluginTargetResult.ResultCode)));
            }

            return buildResult;
        }

        private static WorkUnitResult CreateWorkUnitResult(BuildResultCode resultCode)
        {
            return resultCode == BuildResultCode.Success
                ? new WorkUnitResult(WorkUnitResultCode.Success, WorkUnitActionCode.Continue, null)
                : new WorkUnitResult(WorkUnitResultCode.Failed, WorkUnitActionCode.Stop, null);
        }

        private static ProjectItemInstance.TaskItem CreateTaskItem(ITaskItem2 taskItemInterface)
        {
            var taskItem = new ProjectItemInstance.TaskItem(taskItemInterface.EvaluatedIncludeEscaped, null);

            foreach (string metadataName in taskItemInterface.MetadataNames)
            {
                taskItem.SetMetadata(metadataName, taskItemInterface.GetMetadataValueEscaped(metadataName));
            }

            return taskItem;
        }
    }

    /// <summary>
    /// A cache hit can use this to instruct MSBuild to build the cheaper version of the targets that the plugin avoided running.
    /// For example, GetTargetPath is the cheaper version of Build.
    /// </summary>
    public readonly struct ProxyBuildResults
    {
        public IReadOnlyDictionary<string, string> ProxyTargetToRealTargetMap { get; }

        public ProxyBuildResults(IReadOnlyDictionary<string, string> proxyTargetToRealTargetMap)
        {
            ProxyTargetToRealTargetMap = proxyTargetToRealTargetMap;
        }
    }

    /// <summary>
    /// A cache hit can use this to instruct MSBuild to construct a BuildResult with the target result specified in this type.
    /// </summary>
    public readonly struct PluginTargetResult
    {
        public string TargetName { get; }
        public IReadOnlyCollection<ITaskItem2> TaskItems { get; }
        public BuildResultCode ResultCode { get; }

        public PluginTargetResult(
            string targetName,
            IReadOnlyCollection<ITaskItem2> taskItems,
            BuildResultCode resultCode)
        {
            TargetName = targetName;
            TaskItems = taskItems;
            ResultCode = resultCode;
        }
    }
}
