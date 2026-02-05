
using System.Security.Principal;

namespace HuLoopBOT.Services;

public static class AdminService
{
    public static bool IsAdmin()
    {
        using var id = WindowsIdentity.GetCurrent();
        var p = new WindowsPrincipal(id);
        return p.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
