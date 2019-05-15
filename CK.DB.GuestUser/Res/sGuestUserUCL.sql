-- SetupConfig: { "Requires": [ "CK.sAuthUserOnLogin" ] }
--
-- @Token is the key that identifies a guest user. It can be null for mode 1 or 3 (mode&1)!=0 (creation).
--
-- @Mode (flags): CreateOnly = 1, UpdateOnly = 2, CreateOrUpdate = 3, WithCheckLogin = 4, WithActualLogin = 8.
--                @Mode is normalized:
--                  - WithActualLogin implies WithCheckLogin.
--
-- @UCResult: None = 0, Created = 1, Updated = 2
--
-- When @UserId = 0 we are in "login mode": 
--  - @Mode must be UpdateOnly+WithCheckLogin (6) or UpdateOnly+WithActualLogin (10).
--    If the token is found, we update what we have to and output the found @UserId.
--
-- When @UserId is not 0, it must match with the one of the token otherwise it is an error
-- and an exception is thrown because:
--  - When updating it means that there is a mismatch of GuestUserId/TokenId account in the calling code.
--  - When creating it means that another user with the same token account is already registered and
--    this should never happen.
--
-- When extending this procedure, during update null parameters must be left unchanged.
--
create procedure CK.sGuestUserUCL
(
     @ActorId int
    ,@UserId int /*input*/ output
    ,@Token varchar(71) output
    ,@Mode int -- not null enum { "CreateOnly" = 1, "UpdateOnly" = 2, "CreateOrUpdate" = 3, "WithCheckLogin" = 4, "WithActualLogin" = 8, "IgnoreOptimisticKey" = 16 }
    ,@UCResult int output -- not null enum { None = 0, Created = 1, Updated = 2 }
    ,@LoginFailureCode int output -- Optional
    ,@LoginFailureReason nvarchar(255) output -- Optional
    ,@ExpirationDateUtc datetime2(2) = null
    ,@Active bit = null
)
as
begin

    declare @Now datetime2(2) = sysutcdatetime();

    -- Clears IgnoreOptimisticKey since we do not use it here.
    set @Mode =(@Mode & ~16);
    if @Mode is null or @Mode < 1 or @Mode > 15 throw 50000, 'Argument.InvalidMode', 1;

    -- Handlers @Mode: extracts @CheckLogin & @ActualLogin bit for readability.
    declare @CheckLogin bit = 0;
    declare @ActualLogin bit = 0;

    if (@Mode & 8) <> 0 -- WithActualLogin
    begin
        set @ActualLogin = 1;
        set @CheckLogin = 1;
        set @Mode = @Mode & ~(4 + 8);
    end
    else if (@Mode & 4) <> 0 -- WithCheckLogin
    begin
        set @CheckLogin = 1;
        set @Mode = @Mode & ~4;
    end

    if @ActorId is null or @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @UserId is null or @UserId < 0 throw 50000, 'Argument.InvalidUserId', 1;
    if @UserId = 0 and @Mode <> 1 and ( @Mode <> 2 or @CheckLogin = 0 ) throw 50000, 'Argument.ForUserIdZeroModeMustBeCreateOnlyOrUpdateOnlyWithLogin', 1;

    --[beginsp]

    declare @TokenId int;
    declare @ActualUserId int;
    declare @LastLoginTime datetime2(2);

    if @Token is not null
    begin
        select
            @TokenId = TokenId
        from
            CK.tTokenStore
        where
                TokenId = cast( parsename(@Token,2) as int)
            and TokenGuid = cast( parsename(@Token,1) as uniqueidentifier);

        if @TokenId is not null -- Valid Token
        begin

            -- Retrieve the bound guest user id
            select @ActualUserId = GuestUserId, @LastLoginTime = LastLoginTime
	        from CK.tGuestUser
	        where TokenId = @TokenId;

        end
        else
        begin
            set @LoginFailureReason = N'Invalid.Token'
        end
    end

    --<PreCreateOrUpdate revert />

    if @LoginFailureCode is null and @LoginFailureReason is null
    begin

        if @ActualUserId is null
        begin

            if (@Mode & 1) <> 0 -- CreateOnly or CreateOrUpdate
            begin

                set @LastLoginTime = '0001-01-01';
			    --<PreCreate revert />

                if @UserId = 0
                begin
                    exec CK.sActorCreate @ActorId, @UserId output;
                end

                declare @TokenKey varchar(30) = cast(@UserId as varchar(30)); 

                declare @ActualExpirationDateUtc datetime2(2) = case
                    when @ExpirationDateUtc is not null then @ExpirationDateUtc
                    else dateadd( hour, 1, sysutcdatetime() ) end

                declare @ActualActive bit = case
                    when @Active is not null then @Active
                    else 0 end;

                exec CK.sTokenCreate
                     @ActorId
                    ,@TokenKey
                    ,'CK.DB.GuestUser'
                    ,@ActualExpirationDateUtc
                    ,@ActualActive
                    ,@TokenId output
                    ,@Token output;

                insert into CK.tGuestUser( GuestUserId, TokenId, LastLoginTime )
                    values( @UserId, @TokenId, @LastLoginTime );

                set @UCResult = 1; -- Created

            end
            else
            begin
                set @UCResult = 0; -- None
            end

        end
        else
        begin

            -- Updating an existing registration
            if (@Mode & 2) <> 0 -- Update only or CreateOrUpdate
            begin

        	    -- When updating, we may be in "login mode" if @UserId is 0.
			    -- But if we are not, the provided @UserId must match the actual one.
        	    if @UserId = 0 set @UserId = @ActualUserId;
			    else if @UserId <> @ActualUserId throw 50000, 'Argument.UserIdAndTokenMismatch', 1;

                if @ExpirationDateUtc <> null
                begin
                    exec CK.sTokenRefresh @ActorId, @TokenId, @ExpirationDateUtc;
                end

                if @Active <> null
                begin
                    exec CK.sTokenActivate @ActorId, @TokenId, @Active;
                end

		        set @UCResult = 2; -- Updated

            end
            else
            begin
                set @UCResult = 0; -- None
            end

        end
    end
    else
    begin
        set @UCResult = 0; -- None
    end

    --<PostCreateOrUpdate />

    if @LoginFailureCode is null and @LoginFailureReason is null
    begin

        if @CheckLogin = 1
	    begin

            -- If the user is not registered and we did not create it @LastLoginTime is null.
            if @LastLoginTime is null set @LoginFailureCode = 2; -- UnregisteredUser
            else
            begin

                exec CK.sAuthUserOnLogin 'Guest', @LastLoginTime, @UserId, @ActualLogin, @Now, @LoginFailureCode output, @LoginFailureReason output;  
                if @ActualLogin = 1 and @LoginFailureCode is null
                begin

                    update CK.tGuestUser
                    set LastLoginTime = @Now
			        where GuestUserId = @UserId and TokenId = @TokenId;

                end

            end

	    end
        else
        begin
            set @LoginFailureCode = 0; -- None
        end

    end

    --[endsp]

end
