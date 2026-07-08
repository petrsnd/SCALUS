// Built-in RDP template used to prefill the editor when creating a new RDP
// application. Stored LF-canonical; the launcher reconstructs the wire format
// (CRLF + UTF-16 LE BOM for .rdp) at write time.
//
// Modeled on the connection file Safeguard (SPS) generates natively: fullscreen,
// bandwidth-optimized (16-bit color, compression, wallpaper/themes off). Dynamic
// resolution is enabled so the remote desktop matches the client monitor (true
// fullscreen covers the local taskbar instead of leaving the remote taskbar
// peeking out behind it). The per-user DPAPI "password 51" hash that suppresses
// the mstsc credential prompt is NOT stored here — it can't be static, so the RDP
// parser injects it into the generated .rdp at launch time. RemoteApp support is
// preserved via the %AlternateShell%-driven tokens.
export const DEFAULT_RDP_TEMPLATE = [
  'screen mode id:i:2',
  'use multimon:i:0',
  'dynamic resolution:i:1',
  'desktopwidth:i:1920',
  'desktopheight:i:1080',
  'session bpp:i:16',
  'winposstr:s:0,3,0,0,1024,768',
  'compression:i:1',
  'keyboardhook:i:2',
  'audiocapturemode:i:0',
  'videoplaybackmode:i:1',
  'connection type:i:6',
  'networkautodetect:i:0',
  'bandwidthautodetect:i:1',
  'displayconnectionbar:i:1',
  'enableworkspacereconnect:i:0',
  'disable wallpaper:i:1',
  'allow font smoothing:i:1',
  'allow desktop composition:i:1',
  'disable full window drag:i:1',
  'disable menu anims:i:0',
  'disable themes:i:0',
  'disable cursor setting:i:0',
  'bitmapcachepersistenable:i:1',
  'audiomode:i:0',
  'redirectprinters:i:1',
  'redirectcomports:i:0',
  'redirectsmartcards:i:1',
  'redirectclipboard:i:1',
  'redirectposdevices:i:0',
  'autoreconnection enabled:i:1',
  'authentication level:i:2',
  'prompt for credentials:i:0',
  'prompt for credentials on client:i:0',
  'negotiate security layer:i:1',
  'remoteapplicationmode:i:%AlternateShell?1:0%',
  'remoteapplicationname:s:%Remoteapplicationname%',
  'remoteapplicationprogram:s:%Remoteapplicationprogram%',
  'alternate shell:s:%AlternateShell%',
  'shell working directory:s:',
  'gatewayhostname:s:',
  'gatewayusagemethod:i:4',
  'gatewaycredentialssource:i:4',
  'gatewayprofileusagemethod:i:0',
  'promptcredentialonce:i:0',
  'gatewaybrokeringtype:i:0',
  'use redirection server name:i:0',
  'rdgiskdcproxy:i:0',
  'kdcproxyname:s:',
  'enablesuperpan:i:0',
  'pinconnectionbar:i:0',
  'disable ctrl+alt+del:i:0',
  'full address:s:%Host%',
  'server port:i:3389',
  'username:s:%user%',
].join('\n');
