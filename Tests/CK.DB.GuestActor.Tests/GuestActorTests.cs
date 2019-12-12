using System;
using System.Threading;
using CK.Core;
using CK.DB.Auth;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;

using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.GuestActor.Tests
{
    [TestFixture]
    public class GuestActorTests
    {
        [Test]
        public void create_and_destroy()
        {
            var guestActorTable = TestHelper.StObjMap.StObjs.Obtain<GuestActorTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestActorTable.CreateGuestActor( ctx, 1, DateTime.Now + TimeSpan.FromMinutes( 5 ), true );
                createResult.Success.Should().BeTrue();
                createResult.GuestActorId.Should().BeGreaterThan( 0 );
                createResult.Token.Should().NotBeEmpty();
                guestActorTable.DestroyGuestActor( ctx, 1, createResult.GuestActorId );
            }
        }

        [Test]
        public void default_payload_is_valid()
        {
            var guestActorTable = TestHelper.StObjMap.StObjs.Obtain<GuestActorTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var payload = guestActorTable.CreatePayload();
                var ucResult = guestActorTable.CreateOrUpdateGuestActor( ctx, 1, 0, payload as IGuestActorInfo, UCLMode.CreateOnly );
                ucResult.OperationResult.Should().Be( UCResult.Created );
            }
        }

        [Test]
        public void RefreshGuestActor()
        {
            var guestActorTable = TestHelper.StObjMap.StObjs.Obtain<GuestActorTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestActorTable.CreateGuestActor( ctx, 1, DateTime.UtcNow + TimeSpan.FromSeconds( 1 ), true );
                Thread.Sleep( TimeSpan.FromSeconds( 2 ) ); // Wait until token is expired
                var info = guestActorTable.CreateUserInfo<IGuestActorInfo>( i => i.Token = createResult.Token );
                var loginResult = guestActorTable.LoginGuestActor( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
                loginResult.FailureCode.Should().Be( (int) KnownLoginFailureCode.ProviderDisabledUser );
                guestActorTable.ActivateGuestActor( ctx, 1, createResult.GuestActorId, null, DateTime.UtcNow + TimeSpan.FromMinutes( 5 ) );
                guestActorTable.LoginGuestActor( ctx, info ).IsSuccess.Should().BeTrue();
            }
        }

        [Test]
        public void ActivateGuestActor()
        {
            var guestActorTable = TestHelper.StObjMap.StObjs.Obtain<GuestActorTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestActorTable.CreateGuestActor( ctx, 1, DateTime.UtcNow + TimeSpan.FromMinutes( 5 ), false );
                var info = guestActorTable.CreateUserInfo<IGuestActorInfo>( i => i.Token = createResult.Token );
                var loginResult = guestActorTable.LoginGuestActor( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
                loginResult.FailureCode.Should().Be( (int) KnownLoginFailureCode.ProviderDisabledUser );
                guestActorTable.ActivateGuestActor( ctx, 1, createResult.GuestActorId, true );
                var r = guestActorTable.LoginGuestActor( ctx, info );
                r.IsSuccess.Should().BeTrue();
                guestActorTable.ActivateGuestActor( ctx, 1, createResult.GuestActorId, false );
                loginResult = guestActorTable.LoginGuestActor( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
                loginResult.FailureCode.Should().Be( (int) KnownLoginFailureCode.ProviderDisabledUser );
            }
        }

        [TestCase( true )]
        [TestCase( false )]
        public void RevokeGuestActor( bool destroyToken )
        {
            var guestActorTable = TestHelper.StObjMap.StObjs.Obtain<GuestActorTable>();

            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var createResult = guestActorTable.CreateGuestActor( ctx, 1, DateTime.UtcNow + TimeSpan.FromMinutes( 5 ), true );
                guestActorTable.RevokeGuestActor( ctx, 1, createResult.GuestActorId, destroyToken );
                var info = guestActorTable.CreateUserInfo<IGuestActorInfo>( i => i.Token = createResult.Token );
                var loginResult = guestActorTable.LoginGuestActor( ctx, info );
                loginResult.IsSuccess.Should().BeFalse();
            }
        }
    }
}
