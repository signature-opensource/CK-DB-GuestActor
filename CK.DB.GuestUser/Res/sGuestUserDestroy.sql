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

    declare @TokenId int;

    select @TokenId = s.TokenId
        from CK.tTokenStore s
        inner join CK.tGuestUser g on g.TokenId = s.TokenId
        where g.GuestUserId = @GuestUserId;

    --<PreDestroy revert />

    delete CK.tGuestUser where GuestUserId = @GuestUserId;

    if @TokenId <> 0
    begin
        exec CK.sTokenDestroy 1, @TokenId;
    end

    --<PostDestroy />

    --[endsp]

end
