using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CK.AspNet.Auth;
using CK.Core;
using CK.DB.Auth;
using Microsoft.AspNetCore.Http;

namespace CK.DB.GuestUser
{
    public class GuestUserDirectLoginAllower : IWebFrontAuthUnsafeDirectLoginAllowService
    {
        private readonly IPocoFactory<IGuestUserInfo> _infoFactory;

        public GuestUserDirectLoginAllower( IPocoFactory<IGuestUserInfo> infoFactory )
        {
            _infoFactory = infoFactory;
        }

        public Task<bool> AllowAsync( HttpContext ctx, IActivityMonitor monitor, string scheme, object payload )
        {
            using( monitor.OpenInfo($"{GetType()}.AllowAsync challenge") )
            {
                if( scheme != "GuestUser" )
                {
                    monitor.Trace( "Invalid scheme" );
                    return Task.FromResult( false );
                }
                monitor.Trace( "Valid scheme" );

                IGuestUserInfo info;
                try
                {
                    info = _infoFactory.ExtractPayload( payload );
                }
                catch( Exception exception )
                {
                    monitor.Error( "Error while extracting payload.", exception );
                    return Task.FromResult( false );
                }

                Debug.Assert( info != null );

                if( string.IsNullOrWhiteSpace( info.Token ) )
                {
                    monitor.Trace( "Invalid payload" );
                    return Task.FromResult( false );
                }
                monitor.Trace( "Valid payload" );

                monitor.Info( "DirectLogin allowed.");
                return Task.FromResult( true );
            }
        }
    }
}
