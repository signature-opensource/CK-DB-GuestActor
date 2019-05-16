-- SetupConfig: { "Requires": [ "CK.sTokenRefresh" ] }

create procedure CK.sGuestUserRefresh
(
     @ActorId int
    ,@GuestUserId int
    ,@ExpirationDateUtc datetime2(2)
)
as
begin

    if @GuestUserId <= 0 throw 50000, 'Argument.InvalidGuestUserId', 1;

    --[beginsp]

    declare @TokenId int;
    
    select @TokenId = TokenId
    from CK.tGuestUser
    where GuestUserId = @GuestUserId;

    exec CK.sTokenRefresh @ActorId, @TokenId, @ExpirationDateUtc;

    --[endsp]

end
