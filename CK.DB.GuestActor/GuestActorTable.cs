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

namespace CK.DB.GuestActor
{
    [SqlTable( "tGuestActor", Package = typeof( Package ) ), Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sAuthUserInfoRead, transform:sAuthUserOnLogin, transform:sUserDestroy, transform:vUserAuthProvider" )]
    public abstract partial class GuestActorTable : SqlTable, IGenericAuthenticationProvider<IGuestActorInfo>
    {
        private IPocoFactory<IGuestActorInfo> _infoFactory;

        public string ProviderName => "Guest";

        internal void StObjConstruct( IPocoFactory<IGuestActorInfo> infoFactory, TokenStoreTable tokenStoreTable, ActorTable actorTable )
        {
            _infoFactory = infoFactory;
        }

        IGuestActorInfo IGenericAuthenticationProvider<IGuestActorInfo>.CreatePayload()
            => _infoFactory.Create();

        public T CreateUserInfo<T>( Action<T> configurator ) where T : IGuestActorInfo => ((IPocoFactory<T>)_infoFactory).Create( configurator );

        public readonly struct CreateResult
        {
            public readonly int GuestActorId;

            public readonly string Token;

            public bool Success => GuestActorId > 0 && !string.IsNullOrEmpty( Token );

            public CreateResult( int guestActorIdResult, string tokenResult )
            {
                GuestActorId = guestActorIdResult;
                Token = tokenResult;
            }
        }

        [SqlProcedure( "sGuestActorCreate" )]
        public abstract Task<CreateResult> CreateGuestActorAsync( ISqlCallContext ctx, int actorId, DateTime expirationDateUtc, bool active, CancellationToken cancellationToken = default );

        [SqlProcedure( "sGuestActorRevoke" )]
        public abstract Task RevokeGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, bool destroyToken = true, CancellationToken cancellationToken = default );

        public async Task<UCLResult> CreateOrUpdateGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, IGuestActorInfo info, UCLMode mode = UCLMode.CreateOrUpdate, CancellationToken cancellationToken = default )
            => await GuestActorUCLAsync( ctx, actorId, guestActorId, info, mode, cancellationToken );

        public async Task<LoginResult> LoginGuestActorAsync( ISqlCallContext ctx, IGuestActorInfo info, bool actualLogin = true, CancellationToken cancellationToken = default )
        {
            var mode = actualLogin
                ? UCLMode.UpdateOnly | UCLMode.WithActualLogin
                : UCLMode.UpdateOnly | UCLMode.WithCheckLogin;

            // Override expiration date and active to avoid security issues
            info.ExpirationDateUtc = null;
            info.Active = null;

            var result = await GuestActorUCLAsync( ctx, 1, 0, info, mode, cancellationToken );
            return result.LoginResult;
        }

        [SqlProcedure( "sGuestActorRefresh" )]
        public abstract Task RefreshGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, DateTime expirationDateUtc, CancellationToken cancellationToken = default );

        [SqlProcedure( "sGuestActorActivate" )]
        public abstract Task ActivateGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, bool active, CancellationToken cancellationToken = default );

        [SqlProcedure( "sGuestActorDestroy" )]
        public abstract Task DestroyGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, CancellationToken cancellationToken = default );

        [SqlProcedure( "sGuestActorUCL" )]
        protected abstract Task<UCLResult> GuestActorUCLAsync( ISqlCallContext ctx, int actorId, int userId, [ParameterSource] IGuestActorInfo info, UCLMode mode, CancellationToken cancellationToken );

        #region IGenericAuthenticationProvider explicit implementation

        UCLResult IGenericAuthenticationProvider.CreateOrUpdateUser( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode )
            => CreateOrUpdateGuestActor( ctx, actorId, userId, _infoFactory.ExtractPayload( payload ), mode );

        Task<UCLResult> IGenericAuthenticationProvider.CreateOrUpdateUserAsync( ISqlCallContext ctx, int actorId, int userId, object payload, UCLMode mode, CancellationToken cancellationToken )
            => CreateOrUpdateGuestActorAsync( ctx, actorId, userId, _infoFactory.ExtractPayload( payload ), mode, cancellationToken );

        LoginResult IGenericAuthenticationProvider.LoginUser( ISqlCallContext ctx, object payload, bool actualLogin )
            => LoginGuestActor( ctx, _infoFactory.ExtractPayload( payload ), actualLogin );

        Task<LoginResult> IGenericAuthenticationProvider.LoginUserAsync( ISqlCallContext ctx, object payload, bool actualLogin, CancellationToken cancellationToken )
            => LoginGuestActorAsync( ctx, _infoFactory.ExtractPayload( payload ), actualLogin, cancellationToken );

        void IGenericAuthenticationProvider.DestroyUser( ISqlCallContext ctx, int actorId, int userId, string schemeSuffix )
            => DestroyGuestActor( ctx, actorId, userId );

        Task IGenericAuthenticationProvider.DestroyUserAsync( ISqlCallContext ctx, int actorId, int userId, string schemeSuffix, CancellationToken cancellationToken )
            => DestroyGuestActorAsync( ctx, actorId, userId, cancellationToken );

        #endregion
    }
}
