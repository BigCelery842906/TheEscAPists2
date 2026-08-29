using BC_Dual_Ref_Library;

namespace TheEscAPists2Bridge
{
    
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting the EscAPists 2 Bridge...");

            MessageHandler messageHandler = new MessageHandler();
            ArchipelagoClient archipelagoClient = new ArchipelagoClient();
            GameConnector gameConnector = new GameConnector();

            PrintHelp();
            
            Update();
            
        }

        public static bool settingConnectionValue = false;
        static void Update()
        {
            bool Close = false;
            string curLine = Console.ReadLine().Trim();


            if (settingConnectionValue)
            {
                SetConnectionInfo(curLine);
                ConnectAP();
            }
            else
            {

                curLine = curLine.ToLower();
                switch (curLine)
                {
                    case "/connect":
                    {
                        ConnectBridge();
                        StartConnectionInfo();
                        break;
                    }
                    case "/connect ap":
                    {
                        StartConnectionInfo();
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
                        PrintHelp();
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

                    case "/test":
                    {
                        Packet.ChatMessagePacket packet = new Packet.ChatMessagePacket("BC_Escapists",
                            "THIS IS A MESSAGE THAT SHOULD WORK HOLY HELL");
                        ConsoleLogging.LogDebug($"{packet.slotName} : {packet.chatMessage}");
                        string packetJson = packet.ToJson();
                        ConsoleLogging.LogDebug(packetJson);

                        Packet.ConnectionPacket packet2 =
                            new Packet.ConnectionPacket("BC_Escapists", "archipelago.gg:55644", "");
                        ConsoleLogging.LogDebug($"{packet2.slotName} : {packet2.serverAddress} : {packet2.password}");
                        string packetJson2 = packet2.ToJson();
                        ConsoleLogging.LogDebug(packetJson2);
                        Packet.ConnectionPacket deserialisedPacket2 =
                            (Packet.ConnectionPacket)PacketConverter.ReadJson(packetJson2);
                        ConsoleLogging.LogDebug(
                            $"{deserialisedPacket2.slotName} : {deserialisedPacket2.serverAddress} : {deserialisedPacket2.password}");

                        Packet.CheckPacket packet3 = new Packet.CheckPacket("BC_Escapists", "CheckName");
                        ConsoleLogging.LogDebug($"{packet3.slotName} : {packet3.checkName}");
                        string packetJson3 = packet3.ToJson();
                        ConsoleLogging.LogDebug(packetJson3);
                        Packet.CheckPacket deserialisedPacket3 =
                            (Packet.CheckPacket)PacketConverter.ReadJson(packetJson3);
                        ConsoleLogging.LogDebug($"{deserialisedPacket3.slotName} : {deserialisedPacket3.checkName}");

                        Packet.DeathLinkPacket packet4 = new Packet.DeathLinkPacket("BC_Escapists", "Died of death");
                        ConsoleLogging.LogDebug($"{packet4.slotName} : {packet4.reason}");
                        string packetJson4 = packet4.ToJson();
                        Packet.DeathLinkPacket deserialisedPacket4 =
                            (Packet.DeathLinkPacket)PacketConverter.ReadJson(packetJson4);
                        ConsoleLogging.LogDebug($"{deserialisedPacket4.slotName} : {deserialisedPacket4.reason}");

                        Packet.GoalPacket packet5 = new Packet.GoalPacket("BC_Escapists");
                        ConsoleLogging.LogDebug($"{packet5.slotName}");
                        string packetJson5 = packet5.ToJson();
                        Packet.GoalPacket deserialisedPacket5 =
                            (Packet.GoalPacket)PacketConverter.ReadJson(packetJson5);
                        ConsoleLogging.LogDebug($"{deserialisedPacket5.slotName}");

                        break;
                    }

                    case "/scoutcheck":
                    {
                        if (ConsoleLogging.debugMode)
                        {
                            ConsoleLogging.LogDebug("Enter the check you want to scout: ");
                            string curLine2 = Console.ReadLine().Trim();
                            ArchipelagoClient.instance.ScoutHint(curLine2);
                        }
                        else
                        {
                            ConsoleLogging.LogWarning(
                                "Failed: Debug Mode not enabled, This is reserved for testing purposes only");
                        }

                        break;
                    }

                    case "/sendcheck":
                    {
                        if (ConsoleLogging.debugMode)
                        {
                            ConsoleLogging.LogDebug("Enter the check you want to send: ");
                            string curLine2 = Console.ReadLine().Trim();
                            ArchipelagoClient.instance.SendCheck(curLine2, false);
                        }
                        else
                        {
                            ConsoleLogging.LogWarning(
                                "Failed: Debug Mode not enabled, This is reserved for testing purposes only");
                        }

                        break;
                    }

                    case "/sendgoal":
                        if (ConsoleLogging.debugMode)
                        {
                            ArchipelagoClient.instance.SendGoalToServer();
                        }

                        break;

                    default:
                    {
                        ConsoleLogging.LogWarning("Unknown Command");
                        break;
                    }
                }
            }

            if (!Close)
            {
                Update();
            }
        }


        public static void ConnectAP()
        {
            ConsoleLogging.LogDebug("Trying to Connect to the Server...");
            if (!GameConnector.instance.GetConnectionStatusBridgeToGame() ||
                !GameConnector.instance.GetConnectionStatusGameToBridge())
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.NoGameConnection, "Not Connecting to Archipelago because the game is not connected to the bridge. Run /status for more information");
                return;
            }
            
            if (ArchipelagoClient.instance.GetConnectionInfo().slotName == "")
            { 
                StartConnectionInfo();
            }

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
            ConsoleLogging.LogDebug("Trying to Connect to the game...");
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

        private static void PrintHelp()
        {
            ConsoleLogging.LogInfo("");
            ConsoleLogging.LogInfo("--- Help Menu ---");
            ConsoleLogging.LogInfo("/connect (arg) - Default is both to game and the server, AP is for the server, bridge is for the game.");
            ConsoleLogging.LogInfo("/disconnect (arg) - Default is both to game and the server, AP is for the server, bridge is for the game.");
            ConsoleLogging.LogInfo("/help - Displays this help message.");
            ConsoleLogging.LogInfo("/status - Log the current status of the bridge connections");
            ConsoleLogging.LogInfo("/exit - Close the bridge safely");

            if (ConsoleLogging.debugMode)
            {
                ConsoleLogging.LogInfo("");
                ConsoleLogging.LogInfo("--- DEBUG Menu (IF YOU SEE THIS PLEASE PING/NOTIFY BIGCELERY842906) ---");
                ConsoleLogging.LogInfo("/test - Print a packet before serialisation, after serialisation, and after deserialisation");
                ConsoleLogging.LogInfo("/scoutcheck - This will trigger another line to scout what is at a specific location");
                ConsoleLogging.LogInfo("/sendcheck - This will trigger another line to send a check");
                ConsoleLogging.LogInfo("/sendgoal - Send the goal flag to the server"); 
            }
            ConsoleLogging.LogInfo("");
        }

        public static void StartConnectionInfo()
        {
            settingConnectionValue = true;
            ConsoleLogging.LogInfoInput("Please Enter The Slot Name: ");
        }

        private static void SetConnectionInfo(string slotName)
        {
            ConsoleLogging.LogInfoInput("Please Enter The Server Address (eg: archipelago.gg:PORT): ");
            string serverAddress = Console.ReadLine().Trim();
            ConsoleLogging.LogInfoInput("Please Enter The Server Password (Press enter if there is not one): ");
            string password = Console.ReadLine().Trim();

            if (slotName == "" || serverAddress == "")
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, "Your slot name and server address must be specified.");
            }
            
            ConsoleLogging.LogDebug($"Connecting to {serverAddress}, as {slotName}, with password of '{password}'...");
            ArchipelagoClient.instance.SetConnectionValues(ModInfo.GameName, slotName, serverAddress, password, ModInfo.GameVersion);
            
            settingConnectionValue = false;
        }
    }
    
}