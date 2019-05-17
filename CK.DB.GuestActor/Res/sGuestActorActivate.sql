-- SetupConfig: { "Requires": [ "CK.sTokenActivate" ] }

create procedure CK.sGuestActorActivate
(
     @ActorId int
    ,@GuestActorId int
    ,@Active bit
)
as
begin

    if @GuestActorId <= 0 throw 50000, 'Argument.InvalidGuestActorId', 1;

    --[beginsp]

    declare @TokenId int;
    
    select @TokenId = TokenId
    from CK.tGuestActor
    where GuestActorId = @GuestActorId;

    exec CK.sTokenActivate @ActorId, @TokenId, @Active;

    --[endsp]

end
