using System;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.DB.Actor;
using CK.DB.Auth;
using CK.DB.TokenStore;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace CK.DB.GuestUser
{
    [SqlTable( "tGuestUser", Package = typeof( Package ) ), Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sAuthUserInfoRead, transform:sAuthUserOnLogin, transform:sUserDestroy, transform:vUserAuthProvider" )]
    public abstract partial class GuestUserTable : SqlTable, IGenericAuthenticationProvider<IGuestUserInfo>
    {
        private IPocoFactory<IGuestUserInfo> _infoFactory;

        public string ProviderName => "Guest";

        internal void StObjConstruct( IPocoFactory<IGuestUserInfo> infoFactory, TokenStoreTable tokenStoreTable, ActorTable actorTable )
        {
            _infoFactory = infoFactory;
        }

        IGuestUserInfo IGenericAuthenticationProvider<IGuestUserInfo>.CreatePayload()
            => _infoFactory.Create();

        public T CreateUserInfo<T>( Action<T> configurator ) where T : IGuestUserInfo => ((IPocoFactory<T>)_infoFactory).Create( configurator );

        public readonly struct CreateResult
        {
            public readonly int GuestUserId;

            public readonly string Token;

            public bool Success => GuestUserId > 0 && !string.IsNullOrEmpty( Token );

            public CreateResult( int guestUserIdResult, string tokenResult )
            {
                GuestUserId = guestUserIdResult;
                Token = tokenResult;
            }
        }

        [SqlProcedure( "sGuestUserCreate" )]
        public abstract Task<CreateResult> CreateGuestUserAsync( ISqlCallContext ctx, int actorId, DateTime expirationDateUtc, bool active, CancellationToken cancellationToken = default );

        [SqlProcedure( "sGuestUserRevoke" )]
        public abstract Task RevokeGuestUserAsync( ISqlCallContext ctx, int actorId, int guestUserId, bool destroyToken = true, CancellationToken cancellationToken = default );

        public async Task<UCLResult> CreateOrUpdateGuestUserAsync( ISqlCallContext ctx, int actorId, int userId, IGuestUserInfo info, UCLMode mode = UCLMode.CreateOrUpdate, CancellationToken cancellationToken = default )
            => await GuestUserUCLAsync( ctx, actorId, userId, info, mode, cancellationToken );

        public async Task<LoginResult> LoginUserAsync( ISqlCallContext ctx, IGuestUserInfo info, bool actualLogin = true, CancellationToken cancellationToken = default )
        {
            var mode = actualLogin
                ? UCLMode.UpdateOnly | UCLMode.WithActualLogin
                : UCLMode.UpdateOnly | UCLMode.WithCheckLogin;

            // Override expiration date and active to avoid security issues
            info.ExpirationDateUtc = null;
            info.Active = null;

            var result = await GuestUserUCLAsync( ctx, 1, 0, info, mode, cancellationToken );
            return result.LoginResult;
        }

        [SqlProcedure( "sGuestUserDestroy" )]
        public abstract Task DestroyGuestUserAsync( ISqlCallContext ctx, int actorId, int guestUserId, CancellationToken cancellationToken = default );

        [SqlProcedure( "sGuestUserUCL" )]
        protected abstract Task<UCLResult> GuestUserUCLAsync( ISqlCallContext ctx, int actorId, int userId, [ParameterSource] IGuestUserInfo info, UCLMode mode, CancellationToken cancellationToken );

        #region IGenericAuthenticationProvider explicit implementation

        UCLResult IGenericAuthenticationProvider.CreateOrUpdateUser( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode )
            => CreateOrUpdateGuestUser( ctx, actorId, userId, _infoFactory.ExtractPayload( payload ), mode );

        Task<UCLResult> IGenericAuthenticationProvider.CreateOrUpdateUserAsync( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode, CancellationToken cancellationToken )
            => CreateOrUpdateGuestUserAsync( ctx, actorId, userId, _infoFactory.ExtractPayload( payload ), mode, cancellationToken );

        LoginResult IGenericAuthenticationProvider.LoginUser( ISqlCallContext ctx, object payload, bool actualLogin )
            => LoginUser( ctx, _infoFactory.ExtractPayload( payload ), actualLogin );

        Task<LoginResult> IGenericAuthenticationProvider.LoginUserAsync( ISqlCallContext ctx, object payload, bool actualLogin, CancellationToken cancellationToken )
            => LoginUserAsync( ctx, _infoFactory.ExtractPayload( payload ), actualLogin, cancellationToken );

        void IGenericAuthenticationProvider.DestroyUser( ISqlCallContext ctx, int actorId, int userId, string schemeSuffix )
            => DestroyGuestUser( ctx, actorId, userId );

        Task IGenericAuthenticationProvider.DestroyUserAsync( ISqlCallContext ctx, int actorId, int userId, string schemeSuffix, CancellationToken cancellationToken )
            => DestroyGuestUserAsync( ctx, actorId, userId, cancellationToken );

        #endregion
    }
}
