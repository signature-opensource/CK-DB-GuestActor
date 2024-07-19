using System;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using NUnit.Framework;

using static CK.Testing.MonitorTestHelper;

namespace CK.DB.GuestActor.Tests
{
    [TestFixture]
    public class StandardGenericTests
    {
        [Test]
        public void GuestActor_AuthProvider_is_registered()
        {
            Auth.Tests.AuthTests.CheckProviderRegistration( "Guest" );
        }

        [Test]
        public void standard_generic_tests_for_GuestActor_provider()
        {
            var auth = SharedEngine.Map.StObjs.Obtain<Auth.Package>();
            var f = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IGuestActorInfo>>();
            Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProvider
            (
                auth,
                "Guest",
                payloadForCreateOrUpdate: ( userId, userName ) => f.Create
                (
                    i =>
                    {
                        i.Token = GetTokenOrDefault( auth, userId );
                        i.Active = true;
                    }
                ),
                payloadForLogin: ( userId, userName ) => f.Create( i => i.Token = GetTokenOrDefault( auth, userId ) ),
                payloadForLoginFail: ( userId, userName ) => f.Create( i => i.Token = $"3712.{Guid.NewGuid()}" )
            );
        }

        [Test]
        public async Task standard_generic_tests_for_GuestActor_provider_Async()
        {
            var auth = SharedEngine.Map.StObjs.Obtain<Auth.Package>();
            var f = SharedEngine.Map.StObjs.Obtain<IPocoFactory<IGuestActorInfo>>();
            await Auth.Tests.AuthTests.StandardTestForGenericAuthenticationProviderAsync
            (
                auth,
                "Guest",
                payloadForCreateOrUpdate: ( userId, userName ) => f.Create
                (
                    i =>
                    {
                        i.Token = GetTokenOrDefault( auth, userId );
                        i.Active = true;
                    }
                ),
                payloadForLogin: ( userId, userName ) => f.Create( i => i.Token = GetTokenOrDefault( auth, userId ) ),
                payloadForLoginFail: ( userId, userName ) => f.Create( i => i.Token = $"3712.{Guid.NewGuid()}" )
            );
        }

        private static string GetTokenOrDefault( ISqlConnectionStringProvider connectionStringProvider, int userId )
        {
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                return ctx
                      .GetConnectionController( connectionStringProvider )
                      .QuerySingleOrDefault<string>
                       (
                           @"
select s.Token
from CK.tTokenStore s
inner join CK.tGuestActor u on u.TokenId = s.TokenId
where u.GuestActorId = @UserId 
                    ",
                           new { UserId = userId }
                       );
            }
        }
    }
}
