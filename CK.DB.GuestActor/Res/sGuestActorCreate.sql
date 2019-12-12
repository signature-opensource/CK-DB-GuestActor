-- SetupConfig: {}

create procedure CK.sGuestActorCreate
(
      @ActorId int
     ,@ExpirationDateUtc datetime2(2)
     ,@Active bit
     ,@GuestActorIdResult int output
     ,@TokenResult varchar(128) output
)
as
begin
    declare @Now datetime2(2) = sysutcdatetime();
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @ExpirationDateUtc is null or @ExpirationDateUtc <= @Now throw 50000, 'Argument.InvalidExpirationDateUtc', 1;

    --[beginsp]

    --<PreCreate revert />

    set @GuestActorIdResult = 0;

    exec CK.sGuestActorUCL @ActorId,
                           @GuestActorIdResult output,
                           @TokenResult output,
                           1,
                           null, /*UCResult should be 1.*/
                           null, /*No login*/
                           null, /*No login*/
                           @ExpirationDateUtc,
                           @Active;

    --<PostCreate />

    --[endsp]

end
