namespace DanTheMan827.OnDeviceADB
{
    /// <summary>
    /// Represents a sample class for interacting with ADB (Android Debug Bridge).
    /// </summary>
    internal class AdbSample
    {
        /// <summary>
        /// Indicates whether the instance has been initialized.
        /// </summary>
        private bool _hasInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdbSample"/> class.
        /// </summary>
        public AdbSample() { }

        /// <summary>
        /// Initializes the ADB sample asynchronously.  This should only be run once.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<AdbSample> Initialize()
        {
            if (!_hasInitialized)
            {
                _hasInitialized = true;

                // Kill any existing ADB server instance.
                await AdbWrapper.KillServerAsync();

                // Start a new ADB server instance.
                await AdbWrapper.StartServerAsync();

                // Get the list of connected devices.
                var devices = await AdbWrapper.GetDevicesAsync();

                // If no devices are connected, enable ADB over WiFi.
                if (devices.Length == 0)
                {
                    // Store the current wireless debugging state.
                    var adbWifiState = AdbWrapper.AdbWifiState;

                    // Enable wireless debugging while cycling it to ensure we go from a disabled state to enabled.
                    var port = await AdbWrapper.EnableAdbWiFiAsync(true);

                    // If the port is above 0 we were successful.
                    if (port > 0)
                    {
                        // Connect to the loopback IP on the detected port.
                        var device = await AdbWrapper.ConnectAsync("127.0.0.1", port);

                        // Switch to tcpip mode.
                        await AdbWrapper.TcpIpMode(device);

                        // Kill the server to ensure it refreshes.
                        await AdbWrapper.KillServerAsync();

                        // Launch the server.
                        await AdbWrapper.StartServerAsync();
                    }

                    // Restore the saved wireless debugging state.
                    AdbWrapper.AdbWifiState = adbWifiState;
                }

                // Grant necessary permissions to all connected devices.
                await AdbWrapper.GrantPermissionsAsync();
            }

            // Return our own instance for chaining.
            return this;
        }

        /// <summary>
        /// Tests ADB by running a shell command asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the command output as a result.</returns>
        public async Task<string> TestAdb()
        {
            return (await AdbWrapper.RunShellCommand(null, "ls", "-lah", "/sdcard/")).Output;
        }
    }
}
