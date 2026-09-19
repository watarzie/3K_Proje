using System.Runtime.CompilerServices;

namespace _3K.Application.Tests;

internal static class TestRuntimeConfiguration
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Program.cs ile aynı tarih sözleşmesi; Npgsql ilk kez yüklenmeden önce uygulanır.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
}
