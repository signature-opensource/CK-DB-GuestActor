-- SetupConfig: {}

create procedure CK.sGuestUserDestroy
(
     @ActorId int
    ,@GuestUserId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @GuestUserId = 0 throw 50000, 'Argument.InvalidGuestUserId', 1;

    --[beginsp]

    --<PreDestroy revert />

    delete CK.tGuestUser where GuestUserId = @GuestUserId;

    --<PostDestroy />

    --[endsp]

end
