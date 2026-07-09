using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using BC_Dual_Ref_Library;

public struct ConnectionInfo
{
    public string game = ModInfo.GameName;
    public string slotName;
    public string serverAddress = "archipelago.gg:38281";
    public string password = "";
    public Version gameVersion = ModInfo.GameVersion;

    public ConnectionInfo(string slotName, string serverAddress, Version gameVersion)
    {
        this.slotName = slotName;
        this.serverAddress = serverAddress;
        this.gameVersion = gameVersion;
    }

    public ConnectionInfo(string slotName, string serverAddress, string password, Version gameVersion)
    {
        this.slotName = slotName;
        this.serverAddress = serverAddress;
        this.password = password;
        this.gameVersion = gameVersion;
    }
    public ConnectionInfo(string game, string slotName, string serverAddress, string password, Version gameVersion)
    {
        this.game = game;
        this.slotName = slotName;
        this.serverAddress = serverAddress;
        this.password = password;
        this.gameVersion = gameVersion;
    }
}

namespace TheEscAPists2Bridge
{

    public class ArchipelagoClient
    {
       
        public static ArchipelagoClient instance;
        
        private bool connectedToServer = false;
        
        public bool GetConnectedStatus()
        {
            return connectedToServer;
        }
        
        public static ArchipelagoSession Session;
        private ConnectionInfo connectionInfo;
        static DeathLinkService DeathLinkService;
        
        public Queue<string> incomingChatQueue = new Queue<string>();

        public ConnectionInfo GetConnectionInfo()
        {
            return connectionInfo;
        }

        public ArchipelagoClient()
        {
            if (instance != null && instance != this)
            {
                ConsoleLogging.LogInfo("Archipelago Client instance already running.");
                return;
            }

            if (instance == null)
            {
                instance = this;
                ConsoleLogging.LogSuccess("Archipelago Client instance created.");
            }
        }

        public void SetConnectionValues(string game, string slotName, string serverAddress, string password, Version gameVersion)
        {
            connectionInfo = new ConnectionInfo(game, slotName, serverAddress, password, gameVersion);
            connectionInfo.slotName.Trim();
            connectionInfo.serverAddress.Trim();
            
        }
        
        public bool TryToConnect()
        {
            if (connectedToServer)
            {
                Session.Socket.DisconnectAsync();
                connectedToServer = false;
            }

            // ConsoleLogging.LogInfo("TLS12 enum exists = " + Enum.IsDefined(typeof(System.Net.SecurityProtocolType), 3072));
            //
            // try
            // {
            //     System.Net.ServicePointManager.SecurityProtocol =
            //         (System.Net.SecurityProtocolType)3072;
            //
            //     ConsoleLogging.LogInfo("TLS 1.2 enabled");
            // }
            // catch (Exception ex)
            // {
            //     ConsoleLogging.LogError("Failed to enable TLS 1.2");
            //     ConsoleLogging.LogError(ex.ToString());
            // }
            //
            // try
            // {
            //     var req = (System.Net.HttpWebRequest)
            //         System.Net.WebRequest.Create("https://archipelago.gg");
            //
            //     using (var resp = req.GetResponse())
            //     {
            //         ConsoleLogging.LogInfo("HTTPS succeeded");
            //     }
            // }
            // catch (Exception ex)
            // {
            //     ConsoleLogging.LogError("HTTPS failed");
            //     ConsoleLogging.LogError(ex.ToString());
            // }

            Session = ArchipelagoSessionFactory.CreateSession(connectionInfo.serverAddress); //Test Parameters
            // Session = ArchipelagoSessionFactory.CreateSession(
            //     "archipelago.gg",
            //     49203);
            ConsoleLogging.LogInfo("AHAHAHAHAHAH: " + Session.ConnectionInfo.ToString());
            ConsoleLogging.LogInfo("URI: " + Session.Socket.Uri);

           // Session.Socket.SocketClosed += reason => { ConsoleLogging.LogError("Socket closed: " + reason); };
           //
           //  Session.Socket.ErrorReceived += (exception, message) =>
           //  {
           //      ConsoleLogging.LogError("Socket error: " + message);
           //
           //      if (exception != null)
           //      {
           //          ConsoleLogging.LogError(exception.ToString());
           //      }
           //  };

            Session.Socket.SocketOpened += () => { ConsoleLogging.LogSuccess("Socket opened"); };

            var monoRuntime = Type.GetType("Mono.Runtime");

            if (monoRuntime != null)
            {
                var displayName = monoRuntime
                    .GetMethod(
                        "GetDisplayName",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static
                    );

                ConsoleLogging.LogInfo(
                    "Mono Version: " +
                    displayName.Invoke(null, null)
                );
            }
            //RoomInfo Packet Received - Do parse on incoming packets
            //Optional GetDataPackage - Also Do parse on incoming packet if this is the case
            //

            LoginResult loginResult = null;
            try
            {
                ConsoleLogging.LogInfo("Host = " + connectionInfo.serverAddress);
                ConsoleLogging.LogInfo("Runtime = " + Environment.Version);

                // 1. Tell the server to listen to StealthLink if the config is on
                List<string> tagsList = new List<string>();
                tagsList.Add("StealthLink");
                string[] tags = tagsList.Count > 0 ? tagsList.ToArray() : null;
                
                
                loginResult = Session.TryConnectAndLogin(connectionInfo.game, connectionInfo.slotName, ItemsHandlingFlags.AllItems,
                    connectionInfo.gameVersion, tags, "", connectionInfo.password);
            }
            catch (Exception e)
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection,"Connection refused by server");
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, "Failed with these parameters: " + connectionInfo.serverAddress + ": " + connectionInfo.password + ": " + connectionInfo.slotName);
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, e.ToString());
                //TODO: Send Chat Failure
                
                connectedToServer = false;
                Session.Socket.DisconnectAsync();

                return false;
            }

            // switch (loginResult)
            // {
            //     case LoginFailure:
            //         Logger.LogError("Connection refused by server");
            //         Logger.LogError("Failed with these parameters: " + host + ": " +  password + ": " + slotName);
            //         Logger.LogError(loginResult.ToString());
            //         logging.SendChatMessage("Failed to Connect to the Server");
            //         connectedToServer = false;
            //         Session.Socket.Disconnect();
            //         break;
            //     case LoginSuccessful:
            //         Logger.LogDebug("Connection Successful to server");
            //         logging.SendChatMessage("Successfully connected to Archipelago as " + slotName);
            //         //Need to find a way to subscribe to an event that will automatically parse the relevant messages to the chat box and log
            //         // Session.Socket.PacketReceived += FilterIncomingServerPackets();
            //         connectedToServer = true;
            //         break;
            //     default:
            //         break;
            // }

            if (loginResult is LoginSuccessful loginSuccess)
            {
                ConsoleLogging.LogSuccess("Connection Successful to server");
                //TODO: Successful Connect Chat
                // logging.SendChatMessage("Successfully connected to Archipelago as " + connectionInfo.slotName);
                // MessageHandler.instance.SendChatToGame("Successfully connected to Archipelago as " + connectionInfo.slotName);
                
                //Need to find a way to subscribe to an event that will automatically parse the relevant messages to the chat box and log
                // Session.Socket.PacketReceived += FilterIncomingServerPackets();
                connectedToServer = true;
                MessageHandler.instance.SendSlotnameToGame();
                MessageHandler.instance.SendConnectionStatus();
                SubscribeToServerEvents();
                return true;
            }

            else if (loginResult is LoginFailure loginFailure)
            {
                connectedToServer = false;
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, "Connection Error: " + String.Join("\n", loginFailure.Errors));
                //TODO: Connection Error Chat
                // logging.SendChatMessage("Connection Error: " + String.Join("\n", loginFailure.Errors));
                MessageHandler.instance.SendConnectionStatus();
                Session.Socket.DisconnectAsync();

                return false;
            }

            return false;
        }

        ~ArchipelagoClient()
        {
            Session.Socket.DisconnectAsync();
            connectedToServer = false;
        }

        public void SubscribeToServerEvents()
        {
            SocketClosedEvent();
            SetUpDeathlink();
            SubscribeToChatEvent();
            SetUpStealthLink();
        }

        

        private void SubscribeToChatEvent()
        {
            Session.MessageLog.OnMessageReceived += (message) =>
            {
                ConsoleLogging.LogDebug("Received Message from AP -> " + message);
                lock (incomingChatQueue)
                {
                    incomingChatQueue.Enqueue(message.ToString());
                    MessageHandler.instance.ProcessChatQueue();
                }
            };
            ConsoleLogging.LogSuccess("Subscribed to chat events");
        }

        private void SocketClosedEvent()
        {
            Session.Socket.SocketClosed += (reason) =>
            {
                connectedToServer = false;
                Session = null;
                ConsoleLogging.LogWarning($"Connection lost: {reason}");
                MessageHandler.instance.SendChatToGame($"Connection lost to server: {reason}");
            };
        }

        private void SetUpDeathlink()
        {
            //TODO: Look at optional deathlink - Check player options/yaml etc
            DeathLinkService = Session.CreateDeathLinkService();
            DeathLinkService.EnableDeathLink();
            DeathLinkService.OnDeathLinkReceived += MessageHandler.instance.SendDeathlinkToGame;
        }

        private void FilterIncomingServerPackets(ArchipelagoSocketHelperDelagates.PacketReceivedHandler packetReceived)
        {
            // packetReceived.GetObjectData();
            
        }

        public bool Disconnect()
        {
            if (connectedToServer)
            {
                Session.Socket.DisconnectAsync();
                if (DeathLinkService != null)
                {
                    DeathLinkService.OnDeathLinkReceived -= MessageHandler.instance.SendDeathlinkToGame;
                }
                connectedToServer = false;
                ConsoleLogging.LogInfo("Disconnected from Server.");
            }

            return true;
    }

        public bool GetConnectionStatusToServer()
        {

            if (connectedToServer)
            {
                return true;
            }

            return false;
        }

        public void SayToServer(string message)
        {
            Session.Say(message);
        }

        public void SendCheck(string check)
        {
            //TODO: Set up actual check sending - Difference between CompleteLocationChecks and CompleteLocationChecksAsync
            // Session.Locations.CompleteLocationChecks();
        }
        
        public void SendDeathlinkToServer(string slot, string reason)
        {
            // TODO: Set up deathlink correctly - MIGHT BE DONE, NEED TO TEST
            
            DeathLink deathlink = new DeathLink(slot, reason);
            DeathLinkService.SendDeathLink(deathlink);
        }

        public void SendGoalToServer()
        {
            Session.SetGoalAchieved();
        }
        
        private void SetUpStealthLink()
        {
            // 2. Add the custom Packet Listener for incoming StealthLinks
            Session.Socket.PacketReceived += (packet) =>
            {
                if (packet.PacketType == ArchipelagoPacketType.Bounced)
                {
                    var bouncedPacket = (Archipelago.MultiClient.Net.Packets.BouncedPacket)packet;
                    if (bouncedPacket.Tags != null && bouncedPacket.Tags.Contains("StealthLink"))
                    {
                        string source = "The Multiverse";

                        // Search the tags for the one holding their name
                        foreach (string tag in bouncedPacket.Tags)
                        {
                            if (tag.StartsWith("StealthSource|"))
                            {
                                source = tag.Substring(14); // Slices off "StealthSource|" to reveal the name
                                break;
                            }
                        }

                        MessageHandler.instance.SendStealthLinkToGame(source);
                    }
                }
            };
        }

        public void SendStealthLink(string slotName)
        {
            var bouncePacket = new Archipelago.MultiClient.Net.Packets.BouncePacket
            {
                // bypasses the JSON dictionary entirely and hide the sender's name inside the tags
                Tags = new List<string> { "StealthLink", "StealthSource|" + slotName }
            };
            Session.Socket.SendPacket(bouncePacket);
 
            // This is for diagnostics to see if the stealthlink works
            Session.Socket.SendPacket(new Archipelago.MultiClient.Net.Packets.SayPacket
            {
                Text = $"[DEBUG ALERT] {slotName} just triggered a prison lockdown!"
            });
        }

        
    }
}