--SetupConfig: { "Requires": [ "CK.sTokenDestroy" ] }

create procedure CK.sGuestActorRevoke
(
	 @ActorId int
	,@GuestActorId int
    ,@DestroyToken bit = 1
)
as
begin

	if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
	if @GuestActorId <= 0 throw 50000, 'Argument.InvalidGuestActorId', 1;

	--[beginsp]

    declare @TokenId int;

    update CK.tGuestActor
    set @TokenId = TokenId, TokenId = 0
    where GuestActorId = @GuestActorId;

    if @DestroyToken = 1
    begin
        exec CK.sTokenDestroy 1, @TokenId;
    end

	--[endsp]

end
