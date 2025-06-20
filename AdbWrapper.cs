using Android.Provider;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Application = Android.App.Application;

namespace DanTheMan827.OnDeviceADB
{
    /// <summary>
    /// Provides various ADB (Android Debug Bridge) functionalities.
    /// </summary>
    public static class AdbWrapper
    {
        /// <summary>
        /// The command to grant the necessary permissions for the app to toggle ADB over WiFi.
        /// </summary>
        public static string GrantPermissionsCommand => SharedData.GrantPermissionsCommand;
        public class AdbDevice
        {
            public readonly string Name;
            public readonly bool Authorized;

            public AdbDevice(string name, bool authorized)
            {
                Name = name;
                Authorized = authorized;
            }
        }

        /// <summary>
        /// Gets the path to the ADB executable.
        /// </summary>
        public static string AdbPath => AdbServer.AdbPath;

        /// <summary>
        /// Checks if the ADB server is running.
        /// </summary>
        public static bool IsServerRunning => AdbServer.Instance.IsRunning;

        /// <summary>
        /// Gets or sets the state of ADB over WiFi.
        /// </summary>
        [DebuggerHidden]
        public static AdbWifiState AdbWifiState
        {
            get => Settings.Global.GetInt(Application.Context.ContentResolver, "adb_wifi_enabled") == 1 ? AdbWifiState.Enabled : AdbWifiState.Disabled;
            set => Settings.Global.PutInt(Application.Context.ContentResolver, "adb_wifi_enabled", (int)value);
        }

        /// <summary>
        /// Starts the ADB server asynchronously.
        /// </summary>
        public static async Task StartServerAsync()
        {
            AdbServer.Instance.Start();
            await Task.Delay(500);
        }

        /// <summary>
        /// Stops the ADB server asynchronously.
        /// </summary>
        public static async Task StopServerAsync() => await Task.Run(() => AdbServer.Instance.Stop());

        /// <summary>
        /// Kills the ADB server asynchronously.
        /// </summary>
        public static async Task KillServerAsync()
        {
            await RunAdbCommandAsync("kill-server");
            await StopServerAsync();
        }

        /// <summary>
        /// Runs an ADB command asynchronously.
        /// </summary>
        /// <param name="arguments">The arguments to pass to adb.</param>
        /// <returns>An ExitInfo object containing the exit code, error message, and output of the command.</returns>
        public static async Task<ExitInfo> RunAdbCommandAsync(params string[] arguments)
        {
            Debug.WriteLine($"RunAdbCommand -P {AdbServer.AdbPort} {String.Join(" ", arguments)}");
            await StartServerAsync();

            var procStartInfo = new ProcessStartInfo(AdbPath)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            procStartInfo.ArgumentList.Add("-P");
            procStartInfo.ArgumentList.Add(AdbServer.AdbPort.ToString());

            foreach (var argument in arguments)
            {
                procStartInfo.ArgumentList.Add(argument);
            }

            var proc = Process.Start(procStartInfo);

            if (proc == null)
            {
                throw new NullReferenceException(nameof(proc));
            }

            await proc.WaitForExitAsync();

            return new ExitInfo()
            {
                ExitCode = proc.ExitCode,
                Error = await proc.StandardError.ReadToEndAsync(),
                Output = await proc.StandardOutput.ReadToEndAsync()
            };
        }

        /// <summary>
        /// Gets the ADB WiFi port asynchronously.
        /// </summary>
        /// <returns>The current ADB WiFi port, or 0 if none found.</returns>
        public static async Task<ushort> GetAdbWiFiPortAsync()
        {
            var process = Process.Start(new ProcessStartInfo(SharedData.AdbFinderPath)
            {
                RedirectStandardOutput = true
            });
            await process.WaitForExitAsync();
            return UInt16.Parse(process.StandardOutput.ReadToEnd().Trim().Split("\n").Where(l => l.Trim() != "").FirstOrDefault("0"));
        }

        /// <summary>
        /// Enables ADB over WiFi asynchronously.
        /// </summary>
        /// <param name="cycle">Whether to cycle the ADB WiFi state.  If you need to get the port number, you probably want this to be true as it will disable and enable wireless debugging causing the port to be advertised in the log.</param>
        /// <returns>The ADB WiFi port number.</returns>
        public static async Task<int> EnableAdbWiFiAsync(bool cycle = false)
        {
            if (cycle && AdbWifiState == AdbWifiState.Enabled)
            {
                AdbWifiState = AdbWifiState.Disabled;
                await Task.Delay(100);
            }

            AdbWifiState = AdbWifiState.Enabled;
            await Task.Delay(100);

            return await GetAdbWiFiPortAsync();
        }

        /// <summary>
        /// Disables ADB over WiFi asynchronously.
        /// </summary>
        public static async Task DisableAdbWiFiAsync()
        {
            AdbWifiState = AdbWifiState.Disabled;
            await Task.Delay(100);
        }

        /// <summary>
        /// Connects to an ADB device over WiFi asynchronously.
        /// </summary>
        /// <param name="host">The host of the ADB device.</param>
        /// <param name="port">The port to connect to (default is 5555).</param>
        /// <returns>The connected host.</returns>
        public static async Task<string> ConnectAsync(string host, int port = 5555)
        {
            await StartServerAsync();

            var output = await RunAdbCommandAsync("connect", $"{host}:{port}");
            var match = Regex.Match(output.Output, "^connected to (.*)$", RegexOptions.Multiline);

            if (output.ExitCode != 0 || !match.Success)
            {
                throw new AdbException(output.Output);
            }

            return match.Groups[1].Value.Trim();
        }

        /// <summary>
        /// Disconnects ADB devices asynchronously. If no device identifier is specified, all devices will disconnect.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        /// <returns>True if all devices were disconnected; otherwise, false.</returns>
        public static async Task<bool> DisconnectAsync(string? device = null)
        {
            await StartServerAsync();

            if (device == null)
            {
                return (await RunAdbCommandAsync("disconnect")).Output.Contains("disconnected everything");
            }
            else
            {
                return (await RunAdbCommandAsync("disconnect", device)).Output.Contains("disconnected ");
            }
        }

        /// <summary>
        /// Gets the list of connected ADB devices asynchronously.
        /// </summary>
        /// <returns>An array of device identifiers.</returns>
        public static async Task<AdbDevice[]> GetDevicesAsync()
        {
            await StartServerAsync();

            var output = await RunAdbCommandAsync("devices");

            if (output.ExitCode != 0 || !output.Output.Contains("List of devices attached"))
            {
                throw new AdbException(output.Output);
            }

            var matches = Regex.Matches(output.Output, "^(.*?)\\t(device|unauthorized)$", RegexOptions.Multiline);

            return matches.Select(match => new AdbDevice(match.Groups[1].Value.Trim(), match.Groups[2].Value == "device")).ToArray();
        }

        /// <summary>
        /// Grants necessary permissions to an ADB device asynchronously.
        /// </summary>
        /// <param name="device">The device identifier.</param>
        public static async Task GrantPermissionsAsync(string device)
        {
            await StartServerAsync();
            await RunAdbCommandAsync("-s", device, "shell", $"sh -c '{SharedData.GrantPermissionsCommand}; {SharedData.AppRestartCommand}' > /dev/null 2>&1 < /dev/null &");
        }

        /// <summary>
        /// Grants necessary permissions to all connected ADB devices asynchronously.
        /// </summary>
        public static async Task GrantPermissionsAsync()
        {
            await StartServerAsync();

            var devices = await GetDevicesAsync();

            foreach (var device in devices.Where(d => d.Authorized))
            {
                await GrantPermissionsAsync(device.Name);
            }
        }

        /// <summary>
        /// Checks if we have the necessary permissions.
        /// </summary>
        /// <returns></returns>
        public static bool HasPermissions()
        {
            try
            {
                AdbWrapper.AdbWifiState = AdbWrapper.AdbWifiState;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Restarts the app on a specific ADB device asynchronously.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static async Task RestartAppAsync(string device)
        {
            await StartServerAsync();
            await RunAdbCommandAsync("-s", device, "shell", $"sh -c '{SharedData.AppRestartCommand}' > /dev/null 2>&1 < /dev/null &");
        }

        /// <summary>
        /// Restarts the application on all authorized devices.
        /// </summary>
        /// <returns>.</returns>
        public static async Task RestartAppAsync()
        {
            await StartServerAsync();

            var devices = await GetDevicesAsync();

            foreach (var device in devices.Where(d => d.Authorized))
            {
                await RestartAppAsync(device.Name);
            }
        }

        /// <summary>
        /// Sets ADB to TCP/IP mode asynchronously.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        /// <param name="port">The port to use (default is 5555).</param>
        public static async Task TcpIpMode(string? device = null, int port = 5555)
        {
            await StartServerAsync();

            if (device == null)
            {
                await RunAdbCommandAsync("tcpip", port.ToString());
            }
            else
            {
                await RunAdbCommandAsync("-s", device, "tcpip", port.ToString());
                await DisconnectAsync(device);
            }

            await Task.Delay(1000);
        }

        /// <summary>
        /// Sets ADB to TCP/IP mode asynchronously.
        /// </summary>
        /// <param name="port">The port to use (default is 5555).</param>
        public static async Task TcpIpMode(int port = 5555) => await TcpIpMode(null, port);

        /// <summary>
        /// Sets ADB to USB mode asynchronously.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        public static async Task UsbMode(string? device = null)
        {
            await StartServerAsync();

            if (device == null)
            {
                await RunAdbCommandAsync($"usb");
            }
            else
            {
                await RunAdbCommandAsync("-s", device, "usb");
                await DisconnectAsync(device);
            }
        }

        /// <summary>
        /// Executes an ADB shell command and returns the output.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        /// <param name="command">The command and arguments to run.</param>
        /// <returns></returns>
        public static async Task<ExitInfo> RunShellCommand(string? device = null, params string[] command)
        {
            var arguments = new List<string>();

            if (device != null)
            {
                arguments.Add("-s");
                arguments.Add(device);
            }

            arguments.Add("shell");
            arguments.AddRange(command);

            return await RunAdbCommandAsync(arguments.ToArray());
        }
    }
}
