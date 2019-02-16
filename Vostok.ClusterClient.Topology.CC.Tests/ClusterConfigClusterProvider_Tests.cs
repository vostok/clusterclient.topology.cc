using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.ClusterConfig.Client.Abstractions;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Logging.Console;

namespace Vostok.Clusterclient.Topology.CC.Tests
{
    [TestFixture]
    internal class ClusterConfigClusterProvider_Tests
    {
        private IClusterConfigClient client;
        private ISettingsNode settings;
        private ClusterConfigPath path;

        private ClusterConfigClusterProvider provider;

        private const string Replica1 = "http://replica1:123/v1/";
        private const string Replica2 = "http://replica2:456/v1/";

        [SetUp]
        public void TestSetup()
        {
            path = "topology/hercules";

            client = Substitute.For<IClusterConfigClient>();
            client.Get(path).Returns(_ => settings);

            SetupReplicas(Replica1, Replica2);

            provider = new ClusterConfigClusterProvider(client, path, new ConsoleLog());
        }

        [TearDown]
        public void TearDown()
        {
            ConsoleLog.Flush();
        }

        [Test]
        public void Should_return_null_when_given_a_null_tree()
        {
            settings = null;

            provider.GetCluster().Should().BeNull();
        }

        [Test]
        public void Should_return_null_when_given_an_unexpected_tree()
        {
            settings = new ObjectNode(null, null);

            provider.GetCluster().Should().BeNull();
        }

        [Test]
        public void Should_resolve_empty_cluster_from_empty_file()
        {
            SetupReplicas();

            provider.GetCluster().Should().BeEmpty();
        }

        [Test]
        public void Should_resolve_single_replica_from_well_formed_addresses()
        {
            SetupReplicas(Replica2);

            provider.GetCluster().Should().Equal(new Uri(Replica2));
        }

        [Test]
        public void Should_resolve_multiple_replicas_from_well_formed_addresses()
        {
            provider.GetCluster().Should().Equal(new Uri(Replica1), new Uri(Replica2));
        }

        [Test]
        public void Should_append_missing_trailing_slash_to_urls()
        {
            SetupReplicas(Replica1.TrimEnd('/'), Replica2.TrimEnd('/'));

            provider.GetCluster().Should().Equal(new Uri(Replica1), new Uri(Replica2));
        }

        [Test]
        public void Should_assume_http_scheme_by_default()
        {
            SetupReplicas(Replica1.Remove(0, "http://".Length), Replica2.Remove(0, "http://".Length));

            provider.GetCluster().Should().Equal(new Uri(Replica1), new Uri(Replica2));
        }

        [Test]
        public void Should_support_https_scheme()
        {
            SetupReplicas(Replica1.Replace("http", "https"), Replica2.Replace("http", "https"));

            provider.GetCluster().Should().Equal(new Uri(Replica1.Replace("http", "https")), new Uri(Replica2.Replace("http", "https")));
        }

        [Test]
        public void Should_assume_port_80_by_default()
        {
            SetupReplicas("foo.bar");

            provider.GetCluster().Should().Equal(new Uri("http://foo.bar:80/"));
        }

        [Test]
        public void Should_cache_cluster_for_performance()
        {
            var result1 = provider.GetCluster();
            var result2 = provider.GetCluster();

            result2.Should().BeSameAs(result1);
        }

        [Test]
        public void Should_react_to_changes_in_clusterconfig_settings()
        {
            SetupReplicas(Replica2);

            provider.GetCluster().Should().Equal(new Uri(Replica2));

            SetupReplicas(Replica1);

            provider.GetCluster().Should().Equal(new Uri(Replica1));
        }

        [Test]
        public void Should_ignore_replicas_with_query_string()
        {
            SetupReplicas(Replica1 + "?a=b", Replica2);

            provider.GetCluster().Should().Equal(new Uri(Replica2));
        }

        [Test]
        public void Should_ignore_replicas_with_unexpected_schemes()
        {
            SetupReplicas("mongodb://foo.bar", Replica2);

            provider.GetCluster().Should().Equal(new Uri(Replica2));
        }

        private void SetupReplicas(params string[] replicas)
        {
            switch (replicas.Length)
            {
                case 0:
                    settings = new ObjectNode(path.ToString(), new ISettingsNode[]
                    {
                        new ValueNode(string.Empty, string.Empty)
                    });
                    break;

                case 1:
                    settings = new ObjectNode(path.ToString(), new ISettingsNode[]
                    {
                        new ValueNode(string.Empty, replicas.Single())
                    });
                    break;

                default:
                    settings = new ObjectNode(path.ToString(), new ISettingsNode[]
                    {
                        new ArrayNode(string.Empty, replicas.Select(r => new ValueNode(null, r)).ToArray()),
                    });
                    break;
            }
        }
    }
}