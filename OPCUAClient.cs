using log4net;
using com.thingworx.communications.client;
using com.thingworx.communications.client.things;
using com.thingworx.communications.common;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using log4net.Config;
using static com.thingworx.communications.common.SecurityClaims;


namespace OPCUA
{

    public class OPCUAClient : ConnectedThingClient
    {

        private new static readonly ILog LOG = LogManager.GetLogger(typeof(OPCUAClient));

        public OPCUAClient(ClientConfigurator config) 
            : base(config)
        {
        }

        private void startClient(object state)
        {
            start();
        }

        private void runClient(object state)
        {
            // Loop over all the Virtual Things and process them
            foreach (VirtualThing thing in getThings().Values)
            {
                try
                {
                    thing.processScanRequest();
                }
                catch (Exception eProcessing)
                {
                    Console.WriteLine("Error Processing Scan Request for [" + thing.getName() + "] : " + eProcessing.Message);
                }
            }
        }

        /** 
         * This function demonstrates how to add proxy information to the client configurator.
         * Note that the proxy password must be a callback function, similar to the application key.
         **/
        private static void setClientProxyInfo(ClientConfigurator config)
        {
            config.ProxyInfo = new ProxyInfo();
            config.ProxyInfo.Host = "xxx.xxx.xxx.xxx";
            config.ProxyInfo.Port = 0000;
            string uid = "proxyUser";
            TwPasswordDelegate pwdCallback = (password, maxLength) =>
            {
                string safePassword = "proxyPassword";
                if (safePassword.Length > maxLength - 1)
                {
                    safePassword = safePassword.Substring(0, maxLength - 1);
                }
                IntPtr stringPointer = (IntPtr)Marshal.StringToHGlobalAnsi(safePassword);
                twCopyMemory(password, stringPointer, (uint)safePassword.Length);
                Marshal.FreeHGlobal(stringPointer);
            };
            config.ProxyInfo.Claims = SecurityClaims.fromCredentials(uid, pwdCallback);
        }

        static private string processCommandLineArg(string[] args, int i)
        {
            if (i < args.Length && !args[i].StartsWith("-"))
            {
                return args[i];
            }
            else
            {
                Console.WriteLine("Invalid command line syntax.");
                printCommandLineUsage();
                return null;
            }
        }

        static private void printCommandLineUsage()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Usage: SteamSensorClient.exe [-h hostname] [-p port] [-k appkey] [-r scanRate] [-s] [-c] [-u]");
            sb.Append("\nOPTIONS:");
            sb.Append("\n\t-h, --host\n\t\tAddress of the ThingWorx instance. Defaults to \"localhost\".");
            sb.Append("\n\t-p, --port\n\t\tPort number of the ThingWorx instance. Defaults to \"8443\".");
            sb.Append("\n\t-k, --appkey\n\t\tApplication key for the ThingWorx instance. Default is an invalid key." +
                "\n\t\tIn production, this should be retrieved securely using a callback instead of the command line.");
            sb.Append("\n\t-n, --thingName\n\t\tThe name that will be associated with this Thing. Defaults to \"SteamSensor1\".");
            sb.Append("\n\t-r, --scanrate\n\t\tRate in milliseconds that this example will process scan requests. Defaults to 1000 (1 second).");
            sb.Append("\n\t-s\n\t\tAllow self-signed certificates from the server. False by default." +
                "\n\t\t\tThis should never be set to true in production.");
            sb.Append("\n\t-c\n\t\tDisable server certificate validation. False by default." +
                "\n\t\t\tThis should never be set to true in production.");
            sb.Append("\n\t-u, --usage, --help\n\t\tDisplays this message.");
            Console.WriteLine(sb.ToString());
        }

        static void Main(string[] args)
        {
            /** 
             * Set server defaults for command line arguments. These are examples and must be changed in production.
             **/
            var host = "localhost";
            var port = "8443";
            var appKey = "abf036c2-7009-4462-8a96-6036ee7eba53";
            var thingName = "OPCUAClient1";
            var scanRate = 1000;
            var enableSelfSignedCertificates = true;
            var disableCertificateValidation = false;

            /** 
             * Process command line options. Malformed arguments will cause the program to print a help message and then close. 
             **/
            for (int i = 0; i < args.Length; i++)
            {
                switch(args[i])
                {
                    case "--host":
                    case "-h":
                        host = processCommandLineArg(args, ++i);
                        if (host == null) return;
                        break;
                    case "--port":
                    case "-p":
                        port = processCommandLineArg(args, ++i);
                        if (port == null) return;
                        break;
                    case "--appkey":
                    case "-k":
                        appKey = processCommandLineArg(args, ++i);
                        if (appKey == null) return;
                        break;
                    case "-n":
                    case "-thingName":
                        thingName = processCommandLineArg(args, ++i);
                        if (thingName == null) return;
                        break;
                    case "--scanrate":
                    case "-r":
                        Boolean validScanRate = Int32.TryParse(processCommandLineArg(args, ++i), out scanRate);
                        if (!validScanRate)
                        {
                            Console.WriteLine("Invalid command line syntax.");
                            printCommandLineUsage();
                            return;
                        }
                        break;
                    case "-s":
                        enableSelfSignedCertificates = true;
                        break;
                    case "-c":
                        disableCertificateValidation = true;
                        break;
                    case "--help":
                    case "--usage":
                    case "-u":
                        printCommandLineUsage();
                        return;
                    default:
                        Console.WriteLine("Invalid command line syntax.");
                        printCommandLineUsage();
                        return;
                }
            }

            /**
             * Establish logging if App.config is not supported, such as in .NET Core.
             **/
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log4net.config");
            var ret = XmlConfigurator.Configure(logRepository,new FileInfo(logFilePath));

            /**
             * The app key callback function is called whenever the SDK requires the current app key to authenticate.
             * This example pulls the appkey from the command line arguments.
             * In production, this callback should obtain an app key from a secure source.
             **/
            TwPasswordDelegate appKeyCallback = (password, maxLength) =>
            {
                string safePassword = appKey;
                if (safePassword.Length > maxLength - 1)
                {
                    safePassword = safePassword.Substring(0, maxLength - 1);
                }
                IntPtr stringPointer = (IntPtr)Marshal.StringToHGlobalAnsi(safePassword);
                twCopyMemory(password, stringPointer, (uint)safePassword.Length);
                Marshal.FreeHGlobal(stringPointer);
            };

            /**
             * Create and populate the client configuration object.
             * Refer to the "ClientConfigurator Class" section of the documentation for a complete list of options.
             **/
            var config = new ClientConfigurator
            {
                // Set the size of the threadpools.
                MaxMsgHandlerThreadCount = 8,
                MaxApiTaskerThreadCount = 8,

                /***** 
                 * WARNING: Allowing self-signed certificates, or disabling certificate validation, bypasses
                 * vital security features and should never be done in a production environment. Making
                 * these settings user-configurable should only be done for development purposes.
                 *****/
                AllowSelfSignedCertificates = enableSelfSignedCertificates,
                DisableCertValidation = disableCertificateValidation,
                /***** WARNING *****/

                // The uri for connecting to Thingworx. Defaults to "wss://localhost:8443/Thingworx/WS".
                Uri = "wss://" + host + ":" + port + "/Thingworx/WS",

                // Reconnect every 15 seconds if a disconnect occurs or if initial connection cannot be made.
                ReconnectInterval = 15,

                // Give the appKeyCallback function to the ClientConfigurator's Claims object, to use as needed.
                Claims = SecurityClaims.fromAppKeyCallback(appKeyCallback),

                // Set the unique name of the client to identify it on the platform.
                Name = "SteamSensorGateway",
            };

            /**
             * Utility function to demonstrate configuring connection through a proxy.
             **/
            // setClientProxyInfo(config);
            
            /**
             * Put the offline message store into a writable directory.
             **/
            config.OfflineMsgStoreDir = "/opt/thingworx";
            if (Environment.GetEnvironmentVariable("userprofile") != null)
            {
                config.OfflineMsgStoreDir = Environment.GetEnvironmentVariable("userprofile");
            }
            if (Environment.GetEnvironmentVariable("tmp") != null)
            {
                config.OfflineMsgStoreDir = Environment.GetEnvironmentVariable("TMPDIR");
            }

            /** 
             * Create the client using the configuration settings from above.
             **/
            OPCUAClient client = new OPCUAClient(config);

            try
            {
                // Create the virtual thing.
                OPCUAThing sensor = new OPCUAThing(thingName, "1st Floor Steam Sensor", null, client);

                // Bind the Virtual Thing.
                client.bindThing(sensor);

                /***** WARNING: For Development purposes only. Do not use these settings in a production environment. *****/
                // To connect to an insecure (non-SSL) server
                // ConnectedThingClient.disableEncryption();
                /***** WARNING *****/

                LOG.InfoFormat( 
                    "CONNECTNG TO PLATFORM:\n   " +
                    "Uri: {0} \n   " +
                    "AppKey: {1} \n   " +
                    "Thing: [name: {2}, identifier: {3}] \n   " +
                    "AllowSelfSignedCertificates: {4} \n   " +
                    "DisableCertValidation: {5}",   
                    config.Uri, appKey, sensor.getName(), sensor.getIdentifier(), config.AllowSelfSignedCertificates, config.DisableCertValidation);
                
                // Start the client.
                ThreadPool.QueueUserWorkItem(client.startClient);
            }
            catch (Exception eStart)
            {
                LOG.ErrorFormat("Initial Start Failed : {0}", eStart.Message);
            }

            // Wait for the SteamSensorClient to connect, then process its associated things.
            // As long as the client has not been shutdown, continue.
            while (!client.isShutdown())
            {
                // Only process the Virtual Things if the client is connected.
                if (client.isConnected())
                {
                    ThreadPool.QueueUserWorkItem(client.runClient);
                }
                
                // Suspend processing at the scan rate interval.
                Thread.Sleep(scanRate);
            }
        }
    }
}
