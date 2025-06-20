namespace DanTheMan827.OnDeviceADB
{
    internal class SharedData
    {
        private static Lazy<Android.Content.Context> _context = new(() => Android.App.Application.Context ?? throw new Exception("Unable to retrieve application context"));
        private static Lazy<string> _packageName = new(() => Context.PackageName ?? throw new Exception("Unknown package name"));
        private static Lazy<string> _filesDir = new(() => Context.FilesDir?.Path ?? throw new Exception("Unable to determine application files path"));
        private static Lazy<string> _cacheDir = new(() => Context.CacheDir?.Path ?? throw new Exception("Unable to determine application cache path"));
        private static Lazy<string> _nativeLibraryDir = new(() => Context.ApplicationInfo?.NativeLibraryDir ?? throw new Exception("Unable to get native library path"));
        private static Lazy<string> _grantPermissionsCommand = new(() => $"(pm grant {PackageName} android.permission.WRITE_SECURE_SETTINGS; pm grant {PackageName} android.permission.READ_LOGS)");
        private static Lazy<string> _appRestartCommand = new(() => $"am force-stop {PackageName}; monkey -p {PackageName} -c android.intent.category.LAUNCHER 1");
        private static Lazy<string> _adbPath = new(() => Path.Combine(NativeLibraryDir, "libadb.so") ?? throw new Exception("Unable to determine adb path"));
        private static Lazy<string> _adbFinderPath = new(() => Path.Combine(NativeLibraryDir, "libAdbFinder.so") ?? throw new Exception("Unable to determine adb finder path"));

        public static Android.Content.Context Context => _context.Value;
        public static string PackageName => _packageName.Value;
        public static string FilesDir => _filesDir.Value;
        public static string CacheDir => _cacheDir.Value;
        public static string NativeLibraryDir => _nativeLibraryDir.Value;
        public static string GrantPermissionsCommand => _grantPermissionsCommand.Value;
        public static string AppRestartCommand => _appRestartCommand.Value;
        public static string AdbPath => _adbPath.Value;
        public static string AdbFinderPath => _adbFinderPath.Value;
    }
}
