using System;
using System.Runtime.CompilerServices;

namespace TheEscAPists2Bridge
{


    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting the EscAPists 2 Bridge...");

            // ConsoleLogging.LogAllTypes();

            MessageHandler messageHandler = new MessageHandler();
            ArchipelagoClient archipelagoClient = new ArchipelagoClient();
            GameConnector gameConnector = new GameConnector();

            Update();

            // Console.ReadLine();
        }


        static void Update()
        {
            bool Close = false;
            string curLine = Console.ReadLine().ToLower().Trim();

            switch (curLine)
            {
                case "/connect":
                {
                    ConnectAP();
                    ConnectBridge();
                    break;
                }
                case "/connect ap":
                {
                    ConnectAP();
                    break;
                }
                case "/connect bridge":
                {
                    ConnectBridge();
                    break;
                }

                case "/disconnect":
                {
                    DisconnectAP();
                    DisconnectBridge();
                    break;
                }
                case "/disconnect ap":
                {
                    DisconnectAP();
                    break;
                }
                case "/disconnect bridge":
                {
                    DisconnectBridge();
                    break;
                }
                
                case "/help":
                {
                    ConsoleLogging.LogInfo("");
                    ConsoleLogging.LogInfo("--- Help Menu ---");
                    ConsoleLogging.LogInfo("/connect (arg) - Default is both to game and the server, AP is for the server, bridge is for the game.");
                    ConsoleLogging.LogInfo("/disconnect (arg) - Default is both to game and the server, AP is for the server, bridge is for the game.");
                    ConsoleLogging.LogInfo("/help - Displays this help message.");
                    ConsoleLogging.LogInfo("/status - Log the current status of the bridge connections");
                    ConsoleLogging.LogInfo("/exit - Close the bridge safely");
                    ConsoleLogging.LogInfo("");
                    break;
                }

                case "/status":
                {
                    ConsoleLogging.LogDebug("Running Status Check");
                    ConsoleLogging.LogStatus();
                    break;
                }

                case "/exit":
                {
                    DisconnectAP();
                    DisconnectBridge();
                    Close = true;
                    break;
                }

                default:
                {
                    ConsoleLogging.LogWarning("Unknown Command");
                    break;
                }
            }

            if (!Close)
            {
                Update();
            }
        }


        private static void ConnectAP()
        {
            ConsoleLogging.LogDebug("Trying to Connect...");
            ArchipelagoClient.instance.SetConnectionValues("Clique", "BC_Clique", "archipelago.gg:49203", "",
                new Version(0, 6, 1));

            bool connectionEstablished = ArchipelagoClient.instance.TryToConnect();

            if (connectionEstablished)
            {
                ConsoleLogging.LogSuccess("Connection established.");
            }
            else
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, "Failed to connect to a server.");
            }
        }

        private static void ConnectBridge()
        {
            GameConnector.instance.StartConnectionToGame();
            ConsoleLogging.LogDebug("Trying to Connect to the game");
        }

        private static void DisconnectAP()
        {
            ConsoleLogging.LogDebug("Trying to Disconnect");
            bool connectionDisconnected = ArchipelagoClient.instance.Disconnect();

            if (connectionDisconnected)
            {
                ConsoleLogging.LogSuccess("Connection Destroyed.");
            }
            else
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.FailToDisconnect, "Failed to disconnect from server.");
            }
        }

        private static void DisconnectBridge()
        {
            GameConnector.instance.DisconnectFromGame();
            ConsoleLogging.LogDebug("Trying to Disconnect from the game");
        }
    }
}