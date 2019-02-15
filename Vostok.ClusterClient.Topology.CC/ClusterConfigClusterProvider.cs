using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Topology;
using Vostok.ClusterConfig.Client.Abstractions;
using Vostok.Commons.Collections;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Topology.CC
{
    /// <summary>
    /// <para>An implementation of <see cref="IClusterProvider"/> that fetches and parses a topology from ClusterConfog.</para>
    /// <para>Provided <see cref="ClusterConfigPath"/> should point to a file containing replica addresses in ClusterConfig (such as <c>topology/hercules</c>).</para>
    /// <para>The topology file itself should contain replica addresses as a set of unkeyed values representing absolute HTTP urls.</para>
    /// <para>Scheme and port may be omitted to use default values (<c>http</c> and <c>80</c>, respectively).</para>
    /// <para>Additionally, path segments may be specified. Query parameters are forbidden in replica addresses.</para>
    /// <example>
    ///     <para>An example of well-formed topology file contents:</para>
    ///     <para><c>http://host-1:123/v1</c></para>
    ///     <para><c>host-2:123/v1</c></para>
    ///     <para><c>host-3</c></para>
    /// </example>
    /// </summary>
    [PublicAPI]
    public class ClusterConfigClusterProvider : IClusterProvider
    {
        private const string Slash = "/";

        private readonly IClusterConfigClient client;
        private readonly ILog log;

        private readonly ClusterConfigPath path;

        private readonly CachingTransform<ISettingsNode, Uri[]> transform;

        public ClusterConfigClusterProvider([NotNull] IClusterConfigClient client, ClusterConfigPath path, ILog log)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.log = log ?? LogProvider.Get();
            this.path = path;

            transform = new CachingTransform<ISettingsNode, Uri[]>(ParseReplicas, comparer: EqualityComparer<ISettingsNode>.Default);
        }

        public IList<Uri> GetCluster() 
            => transform.Get(client.Get(path));

        [CanBeNull]
        private Uri[] ParseReplicas([CanBeNull] ISettingsNode settings)
        {
            if (settings == null || !settings.Flatten().TryGetValue(string.Empty, out var replicas))
            {
                LogTopologyNotFound();
                return null;
            }

            var result = replicas
                .Select(TryParseReplica)
                .Where(r => r != null)
                .ToArray();

            LogResolvedReplicas(result);

            return result;
        }

        [CanBeNull]
        private Uri TryParseReplica(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            if (!input.EndsWith(Slash))
                input += Slash;

            if (!input.Contains(Uri.SchemeDelimiter))
                input = $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{input}";

            if (!Uri.TryCreate(input, UriKind.Absolute, out var replica))
            {
                LogFailedToParseUrl(input);
                return null;
            }

            if (!replica.Scheme.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                LogUnexpectedScheme(input, replica.Scheme);
                return null;
            }

            if (replica.Query.Length > 0)
            {
                LogQueryFound(input);
                return null;
            }

            return replica;
        }

        #region Logging

        private void LogTopologyNotFound()
        {
            log.Warn("Topology with name '{TopologyName}' was not found in ClusterConfig.", path);
        }

        private void LogFailedToParseUrl(string input)
        {
            log.Warn("Failed to parse replica Uri from following input: '{Replica}'.", input);
        }

        private void LogUnexpectedScheme(string input, string scheme)
        {
            log.Warn("Unexpected scheme '{Scheme}' in replica '{Replica}'. Expecting http or https.", scheme, input);
        }

        private void LogQueryFound(string input)
        {
            log.Warn("Replica address'{Replica}' contains a query string. Won't use it..", input);
        }

        private void LogResolvedReplicas(Uri[] replicas)
        {
            if (replicas.Length == 0)
            {
                log.Info("Resolved ClusterConfig topology '{TopologyName}' to an empty set of replicas.", path);
            }
            else
            {
                log.Info("Resolved ClusterConfig topology '{TopologyName}' to following replicas: \n\t{Replicas}", 
                    path, string.Join("\n\t", replicas as IEnumerable<Uri>));
            }
        }

        #endregion
    }
}
