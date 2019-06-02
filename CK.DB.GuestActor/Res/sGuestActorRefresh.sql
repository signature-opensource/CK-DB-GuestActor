-- SetupConfig: { "Requires": [ "CK.sTokenRefresh" ] }

create procedure CK.sGuestActorRefresh
(
     @ActorId int
    ,@GuestActorId int
    ,@ExpirationDateUtc datetime2(2)
)
as
begin

    if @GuestActorId <= 0 throw 50000, 'Argument.InvalidGuestActorId', 1;

    --[beginsp]

    declare @TokenId int;
    
    select @TokenId = TokenId
    from CK.tGuestActor
    where GuestActorId = @GuestActorId;

    exec CK.sTokenRefresh @ActorId, @TokenId, @ExpirationDateUtc;

    --[endsp]

end
