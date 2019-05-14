--[beginscript]

create table CK.tGuestUser
(
     GuestUserId int not null
        constraint PK_CK_GuestUser primary key clustered
        constraint FK_CK_GuestUser_Actor_ActorId foreign key references CK.tActor( ActorId )
    ,TokenId int not null
        constraint FK_CK_GuestUser_TokenStore_TokenId foreign key references CK.tTokenStore( TokenId )
    ,LastLoginTime datetime2(2) not null
        constraint DF_CK_GuestUser_LastLoginTime default( '0001-01-01' )
    ,SuccessfullLoginCount int not null
        constraint DF_CK_GuestUser_SuccessfullLoginCount default( 0 )
);

create unique index UK_CK_GuestUser_TokenId_NotRevoked on CK.tGuestUser ( TokenId ) where TokenId <> 0

insert into CK.tGuestUser ( GuestUserId, TokenId )
    values( 0, 0 );

--[endscript]
