-- SetupConfig: { "Requires": [ "CK.sTokenActivate" ] }

create procedure CK.sGuestUserActivate
(
     @ActorId int
    ,@GuestUserId int
    ,@Active bit
)
as
begin

    if @GuestUserId <= 0 throw 50000, 'Argument.InvalidGuestUserId', 1;

    --[beginsp]

    declare @TokenId int;
    
    select @TokenId = TokenId
    from CK.tGuestUser
    where GuestUserId = @GuestUserId;

    exec CK.sTokenActivate @ActorId, @TokenId, @Active;

    --[endsp]

end
