using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CK.AspNet.Auth;
using CK.Core;
using CK.DB.Auth;
using Microsoft.AspNetCore.Http;

namespace CK.DB.GuestActor
{
    public class GuestActorDirectLoginAllower : IWebFrontAuthUnsafeDirectLoginAllowService
    {
        private readonly IPocoFactory<IGuestActorInfo> _infoFactory;

        public GuestActorDirectLoginAllower( IPocoFactory<IGuestActorInfo> infoFactory )
        {
            _infoFactory = infoFactory;
        }

        public Task<bool> AllowAsync( HttpContext ctx, IActivityMonitor monitor, string scheme, object payload )
        {
            using( monitor.OpenInfo($"{GetType()}.AllowAsync challenge") )
            {
                if( scheme != "Guest" )
                {
                    monitor.Trace( "Invalid scheme" );
                    return Task.FromResult( false );
                }
                monitor.Trace( "Valid scheme" );

                IGuestActorInfo info;
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
