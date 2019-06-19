using System;
using CK.DB.Auth;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace CK.DB.GuestActor
{
    public abstract partial class GuestActorTable
    {
        /// <summary>
        /// Creates a new guest actor.
        /// The returned guest actor identifier is a valid, newly created, actor identifier.
        /// Calls <see cref="GuestActorUCLAsync"/> under the hood in mode CreateOnly.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="expirationDateUtc">The expiration date. Must always be in the future.</param>
        /// <param name="active">Whether the guest actor will be active after its creation.</param>
        /// <returns>The <see cref="CreateResult"/>. with the guest actor identifier and the token to use.</returns>
        [SqlProcedure( "sGuestActorCreate" )]
        public abstract CreateResult CreateGuestActor( ISqlCallContext ctx, int actorId, DateTime expirationDateUtc, bool active );

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
        [SqlProcedure( "sGuestActorRevoke" )]
        public abstract void RevokeGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, bool destroyToken = true );

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
        /// <returns>The <see cref="UCLResult"/>.</returns>
        public UCLResult CreateOrUpdateGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, IGuestActorInfo info, UCLMode mode = UCLMode.CreateOrUpdate )
            => GuestActorUCL( ctx, actorId, guestActorId, info, mode );

        /// <summary>
        /// Challenges <paramref name="info"/> to identify a guest actor.
        /// Note that a successful challenge may have side effects.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="info">The payload to challenge.</param>
        /// <param name="actualLogin">Set it to false to avoid login side effect (such as updating the LastLoginTime) on success.</param>
        /// <returns>The <see cref="LoginResult"/>.</returns>
        public LoginResult LoginGuestActor( ISqlCallContext ctx, IGuestActorInfo info, bool actualLogin = true )
        {
            var mode = actualLogin
                ? UCLMode.UpdateOnly | UCLMode.WithActualLogin
                : UCLMode.UpdateOnly | UCLMode.WithCheckLogin;
            var result = GuestActorUCL( ctx, 1, 0, info, mode );
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
        /// <returns>The <see cref="UCLResult"/>.</returns>
        [SqlProcedure( "sGuestActorUCL" )]
        protected abstract UCLResult GuestActorUCL( ISqlCallContext ctx, int actorId, int userId, [ParameterSource] IGuestActorInfo info, UCLMode mode );

        /// <summary>
        /// Refreshes the guest actor expiration date.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor actor to refresh.</param>
        /// <param name="expirationDateUtc">The expiration date. Must always be in the future.</param>
        [SqlProcedure( "sGuestActorRefresh" )]
        public abstract void RefreshGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, DateTime expirationDateUtc );

        /// <summary>
        /// Refreshes the bound token expiration date.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor to activate.</param>
        /// <param name="active">The new activity state. <c>false</c> will deactivate the guest actor.</param>
        [SqlProcedure( "sGuestActorActivate" )]
        public abstract void ActivateGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, bool active );

        /// <summary>
        /// Destroys an existing guest actor.
        /// </summary>
        /// <param name="ctx">The call context to use.</param>
        /// <param name="actorId">The acting actor identifier.</param>
        /// <param name="guestActorId">The guest actor identifier to destroy.</param>
        [SqlProcedure( "sGuestActorDestroy" )]
        public abstract void DestroyGuestActor( ISqlCallContext ctx, int actorId, int guestActorId );
    }
}
