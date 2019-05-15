--SetupConfig: { "Requires": [ "CK.sTokenDestroy" ] }

create procedure CK.sGuestUserRevoke
(
	 @ActorId int
	,@GuestUserId int
    ,@DestroyToken bit = 1
)
as
begin

	if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
	if @GuestUserId <= 0 throw 50000, 'Argument.InvalidGuestUserId', 1;

	--[beginsp]

    declare @TokenId int;

    update CK.tGuestUser
    set @TokenId = TokenId, TokenId = 0
    where GuestUserId = @GuestUserId;

    if @DestroyToken = 1
    begin
        exec CK.sTokenDestroy 1, @TokenId;
    end

	--[endsp]

end
