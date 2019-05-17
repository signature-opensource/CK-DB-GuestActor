using System;
using CK.Core;

namespace CK.DB.GuestActor
{
    public interface IGuestActorInfo : IPoco
    {
        string Token { get; set; }

        DateTime? ExpirationDateUtc { get; set; }

        bool? Active { get; set; }
    }
}
