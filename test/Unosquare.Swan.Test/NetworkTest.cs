﻿namespace Unosquare.Swan.Test.NetworkTests
{
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading.Tasks;
    using Networking;
    using Exceptions;

    public abstract class NetworkTest
    {
        protected const string GoogleDnsFqdn = "google-public-dns-a.google.com";

        protected const string Fqdn = "pool.ntp.org";

        protected IPAddress PrivateIP { get; } = IPAddress.Parse("192.168.1.1");

        protected IPAddress PublicIP { get; } = IPAddress.Parse("200.1.1.1");
        protected IPAddress GoogleDns { get; } = IPAddress.Parse("8.8.8.8");
        protected IPAddress NullIP { get; } = null;
    }

    [TestFixture]
    public class QueryDns : NetworkTest
    {
        [Test]
        public void InvalidDnsAsParam_ThrowsDnsQueryException()
        {
            Assert.Throws<DnsQueryException>(() => Network.QueryDns("invalid.local", DnsRecordType.MX));
        }

        [TestCase(DnsRecordType.TXT)]
        [TestCase(DnsRecordType.MX)]
        [TestCase(DnsRecordType.NS)]
        [TestCase(DnsRecordType.SOA)]
        [TestCase(DnsRecordType.SRV)]
        [TestCase(DnsRecordType.WKS)]
        [TestCase(DnsRecordType.CNAME)]
        public void ValidDns_ReturnsQueryDns(DnsRecordType dnsRecordType)
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Ignored");

            var record = Network.QueryDns(GoogleDnsFqdn, dnsRecordType);
            var records = Network.QueryDns(GoogleDnsFqdn, dnsRecordType);

            Assert.AreNotEqual(records.Id, record.Id, $"Id, Testing with {dnsRecordType}");
            Assert.IsFalse(records.IsAuthoritativeServer, $"IsAuthoritativeServer, Testing with {dnsRecordType}");
            Assert.IsFalse(records.IsTruncated, $"IsTruncated, Testing with {dnsRecordType}");
            Assert.IsTrue(records.IsRecursionAvailable, $"IsRecursionAvailable, Testing with {dnsRecordType}");
            Assert.AreEqual("Query", 
                records.OperationCode.ToString(),
                $"OperationCode, Testing with {dnsRecordType}");
            Assert.AreEqual(DnsResponseCode.NoError, 
                records.ResponseCode,
                $"{GoogleDnsFqdn} {dnsRecordType} Record has no error");
            Assert.AreEqual(dnsRecordType == DnsRecordType.TXT, 
                records.AnswerRecords.Any(),
                $"AnswerRecords, Testing with {dnsRecordType}");
        }

        [Test]
        public void WithNullFqdn_ReturnsQueryDns()
        {
            Assert.Throws<ArgumentNullException>(() => Network.QueryDns(null, DnsRecordType.TXT));
        }
    }

    [TestFixture]
    public class GetDnsHostEntry : NetworkTest
    {
        [Test]
        public void WithValidDns_ReturnsDnsEntry()
        {
            if (Runtime.OS == Swan.OperatingSystem.Osx)
                Assert.Inconclusive("OSX is returning time out");

            var googleDnsIPAddresses = Network.GetDnsHostEntry(GoogleDnsFqdn);

            var targetIP =
                googleDnsIPAddresses.FirstOrDefault(p =>
                    p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            Assert.IsNotNull(targetIP);

            var googleDnsPtrRecord = Network.GetDnsPointerEntry(targetIP);

            var resolvedPtrRecord = Network.GetDnsHostEntry(googleDnsPtrRecord);

            var resolvedIP =
                resolvedPtrRecord.FirstOrDefault(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            Assert.IsNotNull(resolvedIP);
            Assert.IsTrue(resolvedIP.ToString().Equals(targetIP.ToString()));
        }

        [Test]
        public void WithValidDnsAndFinalDot_ReturnsDnsEntry()
        {
            if (Runtime.OS == Swan.OperatingSystem.Osx)
                Assert.Inconclusive("OSX is returning time out");

            var googleDnsIPAddressesWithFinalDot = Network.GetDnsHostEntry(GoogleDnsFqdn + ".");
            Assert.IsNotNull(googleDnsIPAddressesWithFinalDot,
                "GoogleDnsFqdn with trailing period resolution is not null");
        }

        [Test]
        public void WithNullFqdn_ThrowsArgumentNullException()
        {
            if (Runtime.OS == Swan.OperatingSystem.Osx)
                Assert.Inconclusive("OSX is returning time out");

            Assert.Throws<ArgumentNullException>(() => Network.GetDnsHostEntry(null));
        }
    }

    [TestFixture]
    public class IsPrivateAddress : NetworkTest
    {
        [Test]
        public void PrivateIPWithValidAddress_ReturnsTrue()
        {
            Assert.IsTrue(PrivateIP.IsPrivateAddress());
        }

        [Test]
        public void PublicIPWithValidAddress_ReturnsFalse()
        {
            Assert.IsFalse(PublicIP.IsPrivateAddress());
        }

        [Test]
        public void WithNullAddress_ReturnsFalse()
        {
            Assert.Throws<ArgumentNullException>(() => NullIP.IsPrivateAddress());
        }
    }

    [TestFixture]
    public class ToUInt32 : NetworkTest
    {
        [Test]
        public void PrivateIPWithValidAddress_ReturnsAddressAsInt()
        {
            Assert.AreEqual(3232235777, PrivateIP.ToUInt32());
        }

        [Test]
        public void PublicIPWithValidAddress_ReturnsAddressAsInt()
        {
            Assert.AreEqual(3355508993, PublicIP.ToUInt32());
        }

        [Test]
        public void WithNullAddress_ReturnsFalse()
        {
            Assert.Throws<ArgumentNullException>(() => NullIP.ToUInt32());
        }

        [Test]
        public void WithIPv6Address_ThrowsArgumentException()
        {
            var privateIP = IPAddress.Parse("2001:0db8:85a3:0000:1319:8a2e:0370:7344");

            Assert.Throws<ArgumentException>(() => privateIP.ToUInt32());
        }
    }

    [TestFixture]
    public class GetIPv4Addresses : NetworkTest
    {
        [Test]
        public void Wireless80211AsParam_ReturnsIPv4Address()
        {
            var networkType = Network.GetIPv4Addresses(NetworkInterfaceType.Wireless80211);

            Assert.IsNotNull(networkType);
        }

        [Test]
        public void LoopbackAsParam_ReturnsIPv4Address()
        {
            var networkType = Network.GetIPv4Addresses(NetworkInterfaceType.Loopback);

            Assert.AreEqual(networkType[0].ToString(), "127.0.0.1");
        }

        [Test]
        public void WithNoParam_ReturnsIPv4Address()
        {
            var networkType = Network.GetIPv4Addresses();

            Assert.IsNotNull(networkType);
        }
    }

    [TestFixture]
    public class GetPublicIPAddress : NetworkTest
    {
        [Test]
        public void WithNoParam_ReturnsIPAddress()
        {
            var publicIPAddress = Network.GetPublicIPAddress();

            Assert.IsNotEmpty(publicIPAddress.ToString());
        }
    }

    [TestFixture]
    public class GetNetworkTimeUtc : NetworkTest
    {
        [Test]
        public void WithInvalidNtpServerName_ThrowsDnsQueryException()
        {
            Assert.Throws<DnsQueryException>(() => Network.GetNetworkTimeUtc("www"));
        }

        [Test]
        public void WithNullNtpServerName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Network.GetNetworkTimeUtc(NullIP));
        }

        [Test]
        public void WithIPAddressAndPort_ReturnsDateTime()
        {
            var ntpServerAddress = IPAddress.Parse("127.0.0.1");

            var publicIPAddress = Network.GetNetworkTimeUtc(ntpServerAddress, 1203);

            Assert.AreEqual(publicIPAddress, new DateTime(1900, 1, 1));
        }
    }

    [TestFixture]
    public class GetNetworkTimeUtcAsync : NetworkTest
    {
        [Test]
        public async Task WithInvalidNtpServerName_ThrowsDnsQueryException()
        {
            Assert.ThrowsAsync<DnsQueryException>(async () => await Network.GetNetworkTimeUtcAsync("www"));
        }

        [Test]
        public async Task WithIPAddressAndPort_ReturnsDateTime()
        {
            var ntpServerAddress = IPAddress.Parse("127.0.0.1");

            var publicIPAddress = await Network.GetNetworkTimeUtcAsync(ntpServerAddress, 1203);

            Assert.AreEqual(publicIPAddress, new DateTime(1900, 1, 1));
        }
    }

    [TestFixture]
    public class GetDnsHostEntryAsync : NetworkTest
    {
        [Test]
        public async Task WithValidFqdn_ReturnsDnsHost()
        {
            var dnsHost = await Network.GetDnsHostEntryAsync(Fqdn);

            Assert.IsNotEmpty(dnsHost.ToString());
        }

        [Test]
        public async Task WithValidFqdnAndIPAddress_ReturnsDnsHost()
        {
            if (Runtime.OS == Swan.OperatingSystem.Osx)
                Assert.Inconclusive("OSX is returning time out");

            var dnsHost = await Network.GetDnsHostEntryAsync(Fqdn, GoogleDns, Network.DnsDefaultPort);

            Assert.IsNotEmpty(dnsHost.ToString());
        }
    }

    [TestFixture]
    public class GetDnsPointerEntryAsync : NetworkTest
    {
        [Test]
        public async Task WithValidFqdnAndIPAddress_ReturnsDnsHost()
        {
            var dnsPointer = await Network.GetDnsPointerEntryAsync(GoogleDns, GoogleDns, Network.DnsDefaultPort);

            Assert.AreEqual(dnsPointer, GoogleDnsFqdn);
        }

        [Test]
        public async Task WithValidIPAddress_ReturnsDnsHost()
        {
            var dnsPointer = await Network.GetDnsPointerEntryAsync(GoogleDns);

            Assert.AreEqual(dnsPointer, GoogleDnsFqdn);
        }

        [Test]
        public void WithNullIPAddress_ReturnsDnsHost()
        {
            Assert.Throws<ArgumentNullException>(() => Network.GetDnsPointerEntry(NullIP));
        }
    }

    [TestFixture]
    public class QueryDnsAsync : NetworkTest
    {
        [Test]
        public async Task ValidDnsAsDnsServer_ReturnsQueryDns()
        {
            var dnsPointer =
                await Network.QueryDnsAsync(GoogleDnsFqdn, DnsRecordType.MX, GoogleDns, Network.DnsDefaultPort);

            Assert.AreEqual(DnsResponseCode.NoError, dnsPointer.ResponseCode);
        }

        [Test]
        public async Task ValidDnsAsParam_ReturnsQueryDns()
        {
            var dnsPointer = await Network.QueryDnsAsync(GoogleDnsFqdn, DnsRecordType.MX);

            Assert.AreEqual(DnsResponseCode.NoError, dnsPointer.ResponseCode);
        }
    }
}