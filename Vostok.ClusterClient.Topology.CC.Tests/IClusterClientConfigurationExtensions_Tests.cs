using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core;
using Vostok.ClusterConfig.Client.Abstractions;

namespace Vostok.Clusterclient.Topology.CC.Tests
{
    [TestFixture]
    internal class IClusterClientConfigurationExtensions_Tests
    {
        [TestCase("topology/hercules/gate", "hercules/gate")]
        [TestCase("topology/hercules.gate", "hercules.gate")]
        [TestCase("TOPOLOGY/hercules.gate", "hercules.gate")]
        [TestCase("hercules.gate", "hercules.gate")]
        public void Should_fill_target_service_name(string prefix, string expectedName)
        {
            var configuration = Substitute.For<IClusterClientConfiguration>();

            configuration.SetupClusterConfigTopology(Substitute.For<IClusterConfigClient>(), prefix);

            configuration.Received().TargetServiceName = expectedName;
        }
    }
}
