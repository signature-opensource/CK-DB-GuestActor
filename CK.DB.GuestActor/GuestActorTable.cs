using System;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.DB.Actor;
using CK.DB.Auth;
using CK.DB.TokenStore;
using CK.Setup;
using CK.SqlServer;

namespace CK.DB.GuestActor
{
    /// <summary>
    /// Acts both as a GuestActor table and as Guest actors authentication provider.
    /// </summary>
    [SqlTable( "tGuestActor", Package = typeof( Package ) ), Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sAuthUserInfoRead, transform:sAuthUserOnLogin, transform:sUserDestroy, transform:vUserAuthProvider" )]
    public abstract partial class GuestActorTable : SqlTable, IGenericAuthenticationProvider<IGuestActorInfo>
    {
        private IPocoFactory<IGuestActorInfo> _infoFactory;

        /// <summary>
        /// Gets "Guest" that is the name of the provider.
        /// See <see cref="IGenericAuthenticationProvider.ProviderName"/>.
        /// </summary>
        public string ProviderName => "Guest";

        internal void StObjConstruct( IPocoFactory<IGuestActorInfo> infoFactory, TokenStoreTable tokenStoreTable, ActorTable actorTable )
        {
            _infoFactory = infoFactory;
        }

        IGuestActorInfo IGenericAuthenticationProvider<IGuestActorInfo>.CreatePayload()
            => _infoFactory.Create();

        /// <summary>
        /// Creates a <see cref="IGuestActorInfo"/> poco.
        /// </summary>
        /// <typeparam name="T">The poco type.</typeparam>
        /// <param name="configurator">An action which configures the poco.</param>
        /// <returns>A new instance.</returns>
        public T CreateUserInfo<T>( Action<T> configurator ) where T : IGuestActorInfo => ((IPocoFactory<T>)_infoFactory).Create( configurator );

        /// <summary>
        /// Capture the result of <see cref="CreateGuestActor"/> and <see cref="CreateGuestActorAsync"/> calls.
        /// </summary>
        public readonly struct CreateResult
        {
            /// <summary>
            /// The guest actor identifier.
            /// </summary>
            public readonly int GuestActorId;

            /// <summary>
            /// The token associated to the guest actor.
            /// It is the guest actor identifier whenever interacting with the outside world.
            /// </summary>
            public readonly string Token;

            /// <summary>
            /// Gets whether the guest actor has been successfully created.
            /// </summary>
            public bool Success => GuestActorId > 0 && !string.IsNullOrEmpty( Token );

            /// <summary>
            /// Instantiates a new CreateResult.
            /// </summary>
            /// <param name="guestActorIdResult">The guest actor identifier.</param>
            /// <param name="tokenResult">The guest actor associated token.</param>
            public CreateResult( int guestActorIdResult, string tokenResult )
            {
                GuestActorId = guestActorIdResult;
                Token = tokenResult;
            }
        }

        /// <summary>
        /// Creates a new guest actor.
        /// The returned guest actor identifier is a valid, newly created, actor identifier.
        /// Calls <see cref="GuestActorUCLAsync"/> under the hood in mode CreateOnly.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="expirationDateUtc">The expiration date. Must always be in the future.</param>
        /// <param name="active">Whether the guest actor will be active after its creation.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The <see cref="CreateResult"/>. with the guest actor identifier and the token to use.</returns>
        [SqlProcedure( "sGuestActorCreate" )]
        public abstract Task<CreateResult> CreateGuestActorAsync( ISqlCallContext ctx, int actorId, DateTime expirationDateUtc, bool active, CancellationToken cancellationToken = default );

        /// <summary>
        /// Destroys the binding between the guest actor and its token
        /// and then destroys the token.
        /// This basically nullify all possibilities of using the <paramref name="guestActorId"/> as a guest actor.
        /// However, the guest actor identifier is still a valid actor identifier and may be bound to a new token in the future.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor to revoke.</param>
        /// <param name="destroyToken">Whether the token is destroyed. Should be true.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The awaitable.</returns>
        [SqlProcedure( "sGuestActorRevoke" )]
        public abstract Task RevokeGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, bool destroyToken = true, CancellationToken cancellationToken = default );

        /// <summary>
        /// Creates or updates a guest actor entry.
        /// This is the "binding actor feature" since it binds a guest actor to an already existing actor.
        /// When both an <paramref name="guestActorId"/> and <see cref="IGuestActorInfo.Token"/> are provided
        /// they must match otherwise an exception will be thrown.
        /// Otherwise, if no <see cref="IGuestActorInfo.Token"/> is provider then the <paramref name="guestActorId"/>
        /// will be bound to a new guest actor, if no binding currently exists. If no <paramref name="guestActorId"/>
        /// is provided a new one will be created.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor identifier that must be registered.</param>
        /// <param name="info">Provider specific data: the <see cref="IGuestActorInfo"/> poco.</param>
        /// <param name="mode">Optionally configures Create, Update only or WithLogin behavior.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The <see cref="UCLResult"/>.</returns>
        public async Task<UCLResult> CreateOrUpdateGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, IGuestActorInfo info, UCLMode mode = UCLMode.CreateOrUpdate, CancellationToken cancellationToken = default )
            => await GuestActorUCLAsync( ctx, actorId, guestActorId, info, mode, cancellationToken );

        /// <summary>
        /// Challenges <paramref name="info"/> to identify a guest actor.
        /// Note that a successful challenge may have side effects.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="info">The payload to challenge.</param>
        /// <param name="actualLogin">Set it to false to avoid login side effect (such as updating the LastLoginTime) on success.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The <see cref="LoginResult"/>.</returns>
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

        /// <summary>
        /// Raw call to manage GuestActor. Since this should not be used directly, it is protected.
        /// Actual implementation of the centralized update, create or login procedure.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="userId">The actor identifier for which a GuestActor access must be created or updated.</param>
        /// <param name="info">Guest actor information to create or update.</param>
        /// <param name="mode">Configures Create, Update only or WithCheck/ActualLogin behavior.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The <see cref="UCLResult"/>.</returns>
        [SqlProcedure( "sGuestActorUCL" )]
        protected abstract Task<UCLResult> GuestActorUCLAsync( ISqlCallContext ctx, int actorId, int userId, [ParameterSource] IGuestActorInfo info, UCLMode mode, CancellationToken cancellationToken );

        /// <summary>
        /// Refreshes the guest actor expiration date.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor actor to refresh.</param>
        /// <param name="expirationDateUtc">The expiration date. Must always be in the future.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The awaitable.</returns>
        [SqlProcedure( "sGuestActorRefresh" )]
        public abstract Task RefreshGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, DateTime expirationDateUtc, CancellationToken cancellationToken = default );

        /// <summary>
        /// Refreshes the bound token expiration date.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor to activate.</param>
        /// <param name="active">The new activity state. <c>false</c> will deactivate the guest actor.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The awaitable.</returns>
        [SqlProcedure( "sGuestActorActivate" )]
        public abstract Task ActivateGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, bool active, CancellationToken cancellationToken = default );

        /// <summary>
        /// Destroys an existing guest actor.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor identifier to destroy.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The awaitable.</returns>
        [SqlProcedure( "sGuestActorDestroy" )]
        public abstract Task DestroyGuestActorAsync( ISqlCallContext ctx, int actorId, int guestActorId, CancellationToken cancellationToken = default );

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
