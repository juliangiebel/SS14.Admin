using SS14.Admin.Admins;

namespace SS14.Admin.Helpers;

public static class AdminFlagRoleExtension
{
    public static string RoleName(this AdminFlags flag)
    {
        return flag.ToString().ToUpper();
    }
}
