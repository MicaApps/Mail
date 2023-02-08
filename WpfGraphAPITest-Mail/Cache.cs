using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfGraphAPITest_Mail
{
    static class TokenCacheHelper
    {
        //static TokenCacheHelper()
        //{
    //        try
    //        {
    //            // For packaged desktop apps (MSIX packages, also called desktop bridge) the executing assembly folder is read-only. 
    //            // In that case we need to use Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path + "\msalcache.bin" 
    //            // which is a per-app read/write folder for packaged apps.
    //            // See https://docs.microsoft.com/windows/msix/desktop/desktop-to-uwp-behind-the-scenes
    //            CacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".msalcache.bin3");
    //        }
    //        catch (System.InvalidOperationException)
    //        {
    //            // Fall back for an unpackaged desktop app
    //            CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";
    //        }
    //    }

    //    /// <summary>
    //    /// Path to the token cache
    //    /// </summary>
    //    public static string CacheFilePath { get; private set; }

    //    private static readonly object FileLock = new object();

    //    public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
    //    {
    //        lock (FileLock)
    //        {
    //            args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(CacheFilePath));
    //        }
    //    }

    //    public static void AfterAccessNotification(TokenCacheNotificationArgs args)
    //    {
    //        // if the access operation resulted in a cache update
    //        if (args.HasStateChanged)
    //        {
    //            lock (FileLock)
    //            {
    //                // reflect changes in the persistent store
    //                //File.WriteAllBytes(CacheFilePath,
    //                //                   ProtectedData.Protect(args.TokenCache.SerializeMsalV3(),
    //                //                                         null,
    //                //                                         DataProtectionScope.CurrentUser)
    //                //                  );
    //                File.WriteAllBytes(CacheFilePath, args.TokenCache.SerializeMsalV3()
    //                                  );
    //            }
    //        }
    //    }

    //    internal static void EnableSerialization(ITokenCache tokenCache)
    //    {
    //        tokenCache.SetBeforeAccess(BeforeAccessNotification);
    //        tokenCache.SetAfterAccess(AfterAccessNotification);
    //    }
    }

    public static class CacheSettings
    {
        // computing the root directory is not very simple on Linux and Mac, so a helper is provided
        private static readonly string s_cacheFilePath =
                   Path.Combine(MsalCacheHelper.UserRootDirectory, "msal.contoso.cache");

        public static readonly string CacheFileName = Path.GetFileName(s_cacheFilePath);
        public static readonly string CacheDir = Path.GetDirectoryName(s_cacheFilePath);
    }
}
