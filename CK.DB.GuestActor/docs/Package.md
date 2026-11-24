Implements the `CK.tGuestActor` table and the "Guest" authentication provider: guest actors that log in with a token
(from CK.DB.TokenStore) that expires and can be deactivated or revoked.

`GuestActorTable` creates, activates, revokes and destroys the guest actors (`GuestActorTable.CreateGuestActorAsync`,
`GuestActorTable.ActivateGuestActorAsync`, `GuestActorTable.RevokeGuestActorAsync`,
`GuestActorTable.DestroyGuestActorAsync`) and logs them in (`GuestActorTable.LoginGuestActorAsync`); `IGuestActorInfo`
is the payload of the provider.
