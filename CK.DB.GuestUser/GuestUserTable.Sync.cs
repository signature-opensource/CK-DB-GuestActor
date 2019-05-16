using System;
using CK.DB.Auth;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace CK.DB.GuestUser
{
    public abstract partial class GuestUserTable
    {
        [SqlProcedure( "sGuestUserCreate" )]
        public abstract CreateResult CreateGuestUser( ISqlCallContext ctx, int actorId, DateTime expirationDateUtc, bool active );

        [SqlProcedure( "sGuestUserRevoke" )]
        public abstract void RevokeGuestUser( ISqlCallContext ctx, int actorId, int guestUserId, bool destroyToken = true );

        public UCLResult CreateOrUpdateGuestUser( ISqlCallContext ctx, int actorId, int userId, IGuestUserInfo info, UCLMode mode = UCLMode.CreateOrUpdate )
            => GuestUserUCL( ctx, actorId, userId, info, mode );

        public LoginResult LoginUser( ISqlCallContext ctx, IGuestUserInfo info, bool actualLogin = true )
        {
            var mode = actualLogin
                ? UCLMode.UpdateOnly | UCLMode.WithActualLogin
                : UCLMode.UpdateOnly | UCLMode.WithCheckLogin;
            var result = GuestUserUCL( ctx, 1, 0, info, mode );
            return result.LoginResult;
        }

        [SqlProcedure( "sGuestUserRefresh" )]
        public abstract void RefreshGuestUser( ISqlCallContext ctx, int actorId, int guestUserId, DateTime expirationDateUtc );

        [SqlProcedure( "sGuestUserActivate" )]
        public abstract void ActivateGuestUser( ISqlCallContext ctx, int actorId, int guestUserId, bool active );

        [SqlProcedure( "sGuestUserDestroy" )]
        public abstract void DestroyGuestUser( ISqlCallContext ctx, int actorId, int guestUserId );

        [SqlProcedure( "sGuestUserUCL" )]
        protected abstract UCLResult GuestUserUCL( ISqlCallContext ctx, int actorId, int userId, [ParameterSource] IGuestUserInfo info, UCLMode mode );
    }
}
