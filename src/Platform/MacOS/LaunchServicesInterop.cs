// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LaunchServicesInterop.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Platform.MacOS
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    // Thin managed wrapper over the macOS LaunchServices default-handler APIs.
    // This replaces the former external "scalusmac" Swift helper: the same
    // LSCopyDefaultHandlerForURLScheme / LSSetDefaultHandlerForURLScheme calls
    // are made in-process so there is nothing extra to build, ship or sign.
    internal static partial class LaunchServicesInterop
    {
        private const uint KCFStringEncodingUtf8 = 0x08000100;

        private const string CoreFoundation =
            "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        private const string CoreServices =
            "/System/Library/Frameworks/CoreServices.framework/CoreServices";

        // Returns the bundle identifier currently registered to handle the scheme,
        // or an empty string when no handler is set.
        internal static string GetDefaultHandler(string scheme)
        {
            var schemeRef = CFStringCreateWithCString(IntPtr.Zero, scheme, KCFStringEncodingUtf8);
            if (schemeRef == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                var handlerRef = LSCopyDefaultHandlerForURLScheme(schemeRef);
                if (handlerRef == IntPtr.Zero)
                {
                    return string.Empty;
                }

                try
                {
                    return CFStringToManaged(handlerRef);
                }
                finally
                {
                    CFRelease(handlerRef);
                }
            }
            finally
            {
                CFRelease(schemeRef);
            }
        }

        // Sets (bundleId non-empty) or clears (bundleId empty) the default handler
        // for the scheme. Returns the LaunchServices OSStatus (0 == success).
        internal static int SetDefaultHandler(string scheme, string bundleId)
        {
            var schemeRef = CFStringCreateWithCString(IntPtr.Zero, scheme, KCFStringEncodingUtf8);
            if (schemeRef == IntPtr.Zero)
            {
                return -1;
            }

            var handlerRef = CFStringCreateWithCString(IntPtr.Zero, bundleId ?? string.Empty, KCFStringEncodingUtf8);
            try
            {
                if (handlerRef == IntPtr.Zero)
                {
                    return -1;
                }

                return LSSetDefaultHandlerForURLScheme(schemeRef, handlerRef);
            }
            finally
            {
                CFRelease(schemeRef);
                if (handlerRef != IntPtr.Zero)
                {
                    CFRelease(handlerRef);
                }
            }
        }

        private static string CFStringToManaged(IntPtr cfString)
        {
            var direct = CFStringGetCStringPtr(cfString, KCFStringEncodingUtf8);
            if (direct != IntPtr.Zero)
            {
                return Marshal.PtrToStringUTF8(direct) ?? string.Empty;
            }

            var length = (long)CFStringGetLength(cfString);
            var capacity = (length * 4) + 1;
            var buffer = new byte[capacity];
            if (CFStringGetCString(cfString, buffer, checked((IntPtr)capacity), KCFStringEncodingUtf8))
            {
                var end = Array.IndexOf(buffer, (byte)0);
                if (end < 0)
                {
                    end = buffer.Length;
                }

                return Encoding.UTF8.GetString(buffer, 0, end);
            }

            return string.Empty;
        }

        [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr CFStringCreateWithCString(IntPtr allocator, string cStr, uint encoding);

        [LibraryImport(CoreFoundation)]
        private static partial IntPtr CFStringGetCStringPtr(IntPtr theString, uint encoding);

        [LibraryImport(CoreFoundation)]
        private static partial IntPtr CFStringGetLength(IntPtr theString);

        [LibraryImport(CoreFoundation)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool CFStringGetCString(IntPtr theString, [Out] byte[] buffer, IntPtr bufferSize, uint encoding);

        [LibraryImport(CoreFoundation)]
        private static partial void CFRelease(IntPtr cf);

        [LibraryImport(CoreServices)]
        private static partial IntPtr LSCopyDefaultHandlerForURLScheme(IntPtr inUrlScheme);

        [LibraryImport(CoreServices)]
        private static partial int LSSetDefaultHandlerForURLScheme(IntPtr inUrlScheme, IntPtr inHandlerBundleId);
    }
}
