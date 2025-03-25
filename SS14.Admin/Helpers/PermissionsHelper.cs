using System;
using System.Collections.Generic;
using Content.Server.Database;
using DBAdmin = Content.Server.Database.Admin;
using SS14.Admin.Admins;
using System.Linq;

namespace SS14.Admin.Helpers
{
    /// <summary>
    /// Helper methods for working with admin permissions.
    /// </summary>
    public static class PermissionsHelper
    {
        /// <summary>
        /// Returns a dictionary where each key is an admin flag name (as defined in AdminFlags)
        /// and the value is true if the specified admin has that flag.
        /// </summary>
        /// <param name="admin">The admin whose flags to display.</param>
        /// <returns>A dictionary mapping flag names to booleans.</returns>
        public static Dictionary<string, bool> DisplayFlags(DBAdmin admin)
        {
            // Get the combined admin flags from the existing AdminHelper.
            var flags = AdminHelper.GetFlags(admin);
            var result = new Dictionary<string, bool>();

            // Iterate over all individual admin flags (using the list from AdminFlagsHelper).
            foreach (var flag in AdminFlagsHelper.AllFlags)
            {
                // Use the flag's name (which is uppercase) as the key.
                var flagName = flag.ToString();
                // Check if the admin has this flag.
                result[flagName] = (flags & flag) == flag;
            }

            return result;
        }
    }
}
