using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CK.AspNet.Auth;
using CK.Core;
using CK.DB.Auth;
using Microsoft.AspNetCore.Http;

namespace CK.DB.GuestActor
{
    /// <summary>
    /// Allows direct login using the Guest scheme.
    /// </summary>
    public class GuestActorDirectLoginAllower : IWebFrontAuthUnsafeDirectLoginAllowService
    {
        private readonly IPocoFactory<IGuestActorInfo> _infoFactory;

        /// <summary>
        /// Instantiates a new GuestActor direct login allower
        /// </summary>
        /// <param name="infoFactory">The factory used for payload extraction.</param>
        public GuestActorDirectLoginAllower( IPocoFactory<IGuestActorInfo> infoFactory )
        {
            _infoFactory = infoFactory;
        }

        /// <summary>
        /// Allows a direct login if and only if:
        ///     * Scheme is "Guest"
        ///     * The payload is valid
        ///     * The payload's token is neither null or whitespaces
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="scheme">The authentication scheme.</param>
        /// <param name="payload">The login payload.</param>
        /// <returns></returns>
        public Task<bool> AllowAsync( HttpContext ctx, IActivityMonitor monitor, string scheme, object payload )
        {
            using( monitor.OpenInfo( $"{GetType()}.AllowAsync challenge" ) )
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

                monitor.Info( "DirectLogin allowed." );
                return Task.FromResult( true );
            }
        }
    }
}
