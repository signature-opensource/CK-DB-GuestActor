using System;
using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;

using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.GuestUser.Tests
{
    [TestFixture]
    public class GuestUserTests
    {
        [Test]
        public void create_and_destroy()
        {
            var guestUserTable = TestHelper.StObjMap.StObjs.Obtain<GuestUserTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestUserTable.CreateGuestUser( ctx, 1, DateTime.Now + TimeSpan.FromMinutes( 5 ), true );
                createResult.Success.Should().BeTrue();
                createResult.GuestUserId.Should().BeGreaterThan( 0 );
                createResult.Token.Should().NotBeEmpty();
                guestUserTable.DestroyGuestUser( ctx, 1, createResult.GuestUserId );
            }
        }
        
        [TestCase( true )]
        [TestCase( false )]
        public void RevokeGuestUser( bool destroyToken )
        {
            var guestUserTable = TestHelper.StObjMap.StObjs.Obtain<GuestUserTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestUserTable.CreateGuestUser( ctx, 1, DateTime.UtcNow + TimeSpan.FromMinutes( 5 ), true );
                guestUserTable.RevokeGuestUser( ctx, 1, createResult.GuestUserId, destroyToken );
                var info = guestUserTable.CreateUserInfo<IGuestUserInfo>( i => i.Token = createResult.Token );
                var loginResult = guestUserTable.LoginUser( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
            }
        }
    }
}
