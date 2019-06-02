using System;
using CK.DB.Auth;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace CK.DB.GuestActor
{
    public abstract partial class GuestActorTable
    {
        [SqlProcedure( "sGuestActorCreate" )]
        public abstract CreateResult CreateGuestActor( ISqlCallContext ctx, int actorId, DateTime expirationDateUtc, bool active );

        [SqlProcedure( "sGuestActorRevoke" )]
        public abstract void RevokeGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, bool destroyToken = true );

        public UCLResult CreateOrUpdateGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, IGuestActorInfo info, UCLMode mode = UCLMode.CreateOrUpdate )
            => GuestActorUCL( ctx, actorId, guestActorId, info, mode );

        public LoginResult LoginGuestActor( ISqlCallContext ctx, IGuestActorInfo info, bool actualLogin = true )
        {
            var mode = actualLogin
                ? UCLMode.UpdateOnly | UCLMode.WithActualLogin
                : UCLMode.UpdateOnly | UCLMode.WithCheckLogin;
            var result = GuestActorUCL( ctx, 1, 0, info, mode );
            return result.LoginResult;
        }

        [SqlProcedure( "sGuestActorRefresh" )]
        public abstract void RefreshGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, DateTime expirationDateUtc );

        [SqlProcedure( "sGuestActorActivate" )]
        public abstract void ActivateGuestActor( ISqlCallContext ctx, int actorId, int guestActorId, bool active );

        [SqlProcedure( "sGuestActorDestroy" )]
        public abstract void DestroyGuestActor( ISqlCallContext ctx, int actorId, int guestActorId );

        [SqlProcedure( "sGuestActorUCL" )]
        protected abstract UCLResult GuestActorUCL( ISqlCallContext ctx, int actorId, int userId, [ParameterSource] IGuestActorInfo info, UCLMode mode );
    }
}
