using CK.Core;

namespace CK.DB.GuestUser
{
    public interface IGuestUserInfo : IPoco
    {
        string Token { get; set; }
    }
}
