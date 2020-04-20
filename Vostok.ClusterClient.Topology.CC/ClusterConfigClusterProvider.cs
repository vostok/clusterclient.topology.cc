using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Topology.CC.Helpers;
using Vostok.ClusterConfig.Client.Abstractions;
using Vostok.Commons.Collections;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Topology.CC
{
    /// <summary>
    /// <para>An implementation of <see cref="IClusterProvider"/> that fetches and parses a topology from ClusterConfig.</para>
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
        private readonly IClusterConfigClient client;

        private readonly ClusterConfigPath path;

        private readonly CachingTransform<ISettingsNode, Uri[]> transform;

        public ClusterConfigClusterProvider([NotNull] IClusterConfigClient client, ClusterConfigPath path, ILog log)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));

            this.path = path;

            var parser = new ClusterConfigReplicasParser(log);
            transform = new CachingTransform<ISettingsNode, Uri[]>(node => parser.Parse(node, path));
        }

        public IList<Uri> GetCluster()
            => transform.Get(client.Get(path));
    }
}