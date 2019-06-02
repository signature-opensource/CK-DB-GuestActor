-- SetupConfig: {}

create procedure CK.sGuestActorDestroy
(
     @ActorId int
    ,@GuestActorId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @GuestActorId <= 0 throw 50000, 'Argument.InvalidGuestActorId', 1;

    --[beginsp]

    declare @TokenId int;
    declare @IsUser bit;

    select @TokenId = s.TokenId,
           @IsUser = case when u.UserId is null then 0 else 1 end
        from CK.tTokenStore s
        inner join CK.tGuestActor g on g.TokenId = s.TokenId
        left outer join CK.tUser u on u.UserId = @GuestActorId
        where g.GuestActorId = @GuestActorId;

    --<PreDestroy revert />

    delete from CK.tGuestActor where GuestActorId = @GuestActorId;

    if @TokenId <> 0
    begin
        exec CK.sTokenDestroy 1, @TokenId;
    end

    if @IsUser = 0
    begin

        --<PreDestroyActorOnly revert />

        delete from CK.tActorProfile where ActorId = @GuestActorId;
        delete from CK.tActor where ActorId = @GuestActorId;

        --<PostDestroyActorOnly />

    end

    --<PostDestroy />

    --[endsp]

end
