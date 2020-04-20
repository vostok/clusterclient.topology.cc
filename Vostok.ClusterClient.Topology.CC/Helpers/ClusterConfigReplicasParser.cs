using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.ClusterConfig.Client.Abstractions;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Topology.CC.Helpers
{
    internal class ClusterConfigReplicasParser
    {
        private const string Slash = "/";
        private readonly ILog log;

        public ClusterConfigReplicasParser(ILog log) =>
            this.log = log ?? LogProvider.Get();

        [CanBeNull]
        public Uri[] Parse([CanBeNull] ISettingsNode settings, ClusterConfigPath path)
        {
            if (settings == null || !settings.Flatten().TryGetValue(string.Empty, out var replicas))
            {
                LogTopologyNotFound(path);
                return null;
            }

            var result = replicas
                .Select(TryParseReplica)
                .Where(r => r != null)
                .ToArray();

            LogResolvedReplicas(result, path);

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

        private void LogTopologyNotFound(ClusterConfigPath path)
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

        private void LogResolvedReplicas(Uri[] replicas, ClusterConfigPath path)
        {
            if (replicas.Length == 0)
            {
                log.Info("Resolved ClusterConfig topology '{TopologyName}' to an empty set of replicas.", path);
            }
            else
            {
                log.Info(
                    "Resolved ClusterConfig topology '{TopologyName}' to following replicas: \n\t{Replicas}",
                    path,
                    string.Join("\n\t", replicas as IEnumerable<Uri>));
            }
        }

        #endregion
    }
}