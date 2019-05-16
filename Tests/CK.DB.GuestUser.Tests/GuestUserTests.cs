using System;
using System.Threading;
using CK.Core;
using CK.DB.Auth;
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

        [Test]
        public void RefreshGuestUser()
        {
            var guestUserTable =TestHelper.StObjMap.StObjs.Obtain<GuestUserTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestUserTable.CreateGuestUser( ctx, 1, DateTime.UtcNow + TimeSpan.FromSeconds( 1 ), true );
                Thread.Sleep( TimeSpan.FromSeconds( 2 ) ); // Wait until token is expired
                var info = guestUserTable.CreateUserInfo<IGuestUserInfo>( i => i.Token = createResult.Token );
                var loginResult = guestUserTable.LoginUser( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
                loginResult.FailureCode.Should().Be( (int) KnownLoginFailureCode.ProviderDisabledUser );
                guestUserTable.RefreshGuestUser( ctx, 1, createResult.GuestUserId, DateTime.UtcNow + TimeSpan.FromMinutes( 5 ) );
                guestUserTable.LoginUser( ctx, info ).IsSuccess.Should().BeTrue();
            }
        }

        [Test]
        public void ActivateGuestUser()
        {
            var guestUserTable = TestHelper.StObjMap.StObjs.Obtain<GuestUserTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestUserTable.CreateGuestUser( ctx, 1, DateTime.UtcNow + TimeSpan.FromMinutes( 5 ), false );
                var info = guestUserTable.CreateUserInfo<IGuestUserInfo>( i => i.Token = createResult.Token );
                var loginResult = guestUserTable.LoginUser( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
                loginResult.FailureCode.Should().Be( (int) KnownLoginFailureCode.ProviderDisabledUser );
                guestUserTable.ActivateGuestUser( ctx, 1, createResult.GuestUserId, true );
                var r = guestUserTable.LoginUser( ctx, info );
                r.IsSuccess.Should().BeTrue();
                guestUserTable.ActivateGuestUser( ctx, 1, createResult.GuestUserId, false );
                loginResult = guestUserTable.LoginUser( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
                loginResult.FailureCode.Should().Be( (int) KnownLoginFailureCode.ProviderDisabledUser );
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
