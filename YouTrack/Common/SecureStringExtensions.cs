#if Otter
using System.Security;

namespace Inedo.Extensions.YouTrack
{
    internal static class SecureStringExtensions
    {
        public static string ToUnsecureString(this SecureString s)
        {
            return AH.Unprotect(s);
        }

        public static SecureString ToSecureString(this string s)
        {
            return AH.CreateSecureString(s);
        }
    }
}
#endif
