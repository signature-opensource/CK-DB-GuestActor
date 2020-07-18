--[beginscript]

create table CK.tGuestActor
(
     GuestActorId int not null
        constraint PK_CK_GuestActor primary key clustered
        constraint FK_CK_GuestActor_Actor_ActorId foreign key references CK.tActor( ActorId )
    ,TokenId int not null
        constraint FK_CK_GuestActor_TokenStore_TokenId foreign key references CK.tTokenStore( TokenId )
    ,LastLoginTime datetime2(2) not null
        constraint DF_CK_GuestActor_LastLoginTime default( '0001-01-01' )
    ,SuccessfullLoginCount int not null
        constraint DF_CK_GuestActor_SuccessfullLoginCount default( 0 )
);

create unique index UK_CK_GuestActor_TokenId_NotRevoked on CK.tGuestActor ( TokenId ) where TokenId <> 0

insert into CK.tGuestActor ( GuestActorId, TokenId )
    values( 0, 0 );

--[endscript]
