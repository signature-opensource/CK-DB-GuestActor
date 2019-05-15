using System;
using System.Threading.Tasks;
using CK.Core;
using FluentAssertions;
using NUnit.Framework;

using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.GuestUser.Tests
{
    [TestFixture]
    public class DirectLoginAllowerTests
    {
        [Test]
        public async Task resolves_successfully()
        {
            var infoFactory = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<IGuestUserInfo>>();
            var allower = new GuestUserDirectLoginAllower( infoFactory );
            var payload = infoFactory.Create( info => info.Token = $"3712.{Guid.NewGuid().ToString()}");
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "Guest", payload );
            allowed.Should().BeTrue();
        }

        [Test]
        public async Task rejects_other_schemes()
        {
            var infoFactory = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<IGuestUserInfo>>();
            var allower = new GuestUserDirectLoginAllower( infoFactory );
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "BasicLogin", null );
            allowed.Should().BeFalse();
        }

        [Test]
        public async Task rejects_invalid_payload()
        {
            var infoFactory = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<IGuestUserInfo>>();
            var allower = new GuestUserDirectLoginAllower( infoFactory );
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "Guest", null );
            allowed.Should().BeFalse();
        }

        [Test]
        public async Task rejects_invalid_token()
        {
            var infoFactory = TestHelper.StObjMap.StObjs.Obtain<IPocoFactory<IGuestUserInfo>>();
            var allower = new GuestUserDirectLoginAllower( infoFactory );
            var payload = infoFactory.Create( info => info.Token = "   " );
            var allowed = await allower.AllowAsync( null, TestHelper.Monitor, "Guest", payload );
            allowed.Should().BeFalse();
        }
    }
}
