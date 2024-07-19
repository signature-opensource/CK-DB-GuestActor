using System;
using System.Threading.Tasks;
using CK.Core;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;

using static CK.Testing.MonitorTestHelper;

namespace CK.DB.GuestActor.Tests
{
    [TestFixture]
    public class DirectLoginAllowerTests
    {
        [Test]
        public async Task resolves_successfully_Async()
        {
            var infoFactory = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IGuestActorInfo>>();
            var allower = new GuestActorDirectLoginAllower( infoFactory );
            var payload = infoFactory.Create( info => info.Token = $"3712.{Guid.NewGuid()}");
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "Guest", payload );
            allowed.Should().BeTrue();
        }

        [Test]
        public async Task rejects_other_schemes_Async()
        {
            var infoFactory = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IGuestActorInfo>>();
            var allower = new GuestActorDirectLoginAllower( infoFactory );
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "BasicLogin", null );
            allowed.Should().BeFalse();
        }

        [Test]
        public async Task rejects_invalid_payload_Async()
        {
            var infoFactory = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IGuestActorInfo>>();
            var allower = new GuestActorDirectLoginAllower( infoFactory );
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "Guest", null );
            allowed.Should().BeFalse();
        }

        [Test]
        public async Task rejects_invalid_token_Async()
        {
            var infoFactory = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IGuestActorInfo>>();
            var allower = new GuestActorDirectLoginAllower( infoFactory );
            var payload = infoFactory.Create( info => info.Token = "   " );
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "Guest", payload );
            allowed.Should().BeFalse();
        }
    }
}
