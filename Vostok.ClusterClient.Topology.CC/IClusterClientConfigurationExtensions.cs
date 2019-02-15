using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Topology;
using Vostok.ClusterConfig.Client.Abstractions;

namespace Vostok.Clusterclient.Topology.CC
{
    [PublicAPI]
    public static class IClusterClientConfigurationExtensions
    {
        /// <summary>
        /// <para>Sets up an <see cref="IClusterProvider"/> that will fetch replicas from ClusterConfig by given <paramref name="path"/> with given <paramref name="client"/>.</para>
        /// <para>See <see cref="ClusterConfigClusterProvider"/> for more details.</para>
        /// </summary>
        public static void SetupClusterConfigTopology(
            [NotNull] this IClusterClientConfiguration self,
            [NotNull] IClusterConfigClient client,
            ClusterConfigPath path)
        {
            self.ClusterProvider = new ClusterConfigClusterProvider(client, path, self.Log);
        }
    }
}