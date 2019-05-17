using CK.Setup;
using CK.SqlServer.Setup;

namespace CK.DB.GuestActor
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    public class Package : SqlPackage
    {
        internal void StObjConstruct( Actor.Package actorPackage, TokenStore.Package tokenStorePackage ) { }
    }
}
