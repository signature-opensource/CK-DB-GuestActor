using System;
using CK.Core;

namespace CK.DB.GuestActor
{
    /// <summary>
    /// Simple poco which represents guest actor information.
    /// </summary>
    public interface IGuestActorInfo : IPoco
    {
        /// <summary>
        /// The token associated to the guest actor.
        /// It is the guest actor identifier whenever interacting with the outside world.
        /// </summary>
        string Token { get; set; }

        /// <summary>
        /// The expiration date. Must always be in the future.
        /// </summary>
        DateTime? ExpirationDateUtc { get; set; }

        /// <summary>
        /// Gets or sets whether this guest actor is active.
        /// An inactive guest actor acts as if its access was expired.
        /// See <see cref="GuestActorTable.ActivateGuestActor"/>
        /// </summary>
        bool? Active { get; set; }
    }
}
