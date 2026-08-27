using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BC_Dual_Ref_Library;
using HarmonyLib;

public struct ConnectionInfo
{
    public string game = ModInfo.GameName;
    public string slotName = "";
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

    public class SlotData
    {
        private static int GetSlotOption(Dictionary<string, object> slotData, string slotDataName)
        {
            return Convert.ToInt32(slotData[slotDataName]);
        }

        private static bool GetSlotOptionBool(Dictionary<string, object> slotData, string slotDataName)
        {
            return GetSlotOption(slotData, slotDataName) != 0;
        }

        public bool CenterPerks2;
        public bool CougarCreekRailroad;
        public bool RattlesnakeSprings;
        public bool KAPOWCamp;
        public bool HMSOrca;
        public bool HMPOffshore;
        public bool FortTundra;
        public bool Area17;
        public bool AirForceCon;
        public bool USSAnomaly;

        public int StrengthStep;
        public int StaminaStep;
        public int IntellectStep;
        public int UniqueEscapesRequired;

        public SlotData(Dictionary<string, object> connectedPacketSlotData)
        {
            CenterPerks2 = GetSlotOptionBool(connectedPacketSlotData, "center_perks");
            CougarCreekRailroad = GetSlotOptionBool(connectedPacketSlotData, "cougar_creek_railroad");
            RattlesnakeSprings = GetSlotOptionBool(connectedPacketSlotData, "rattlesnake_springs");
            KAPOWCamp = GetSlotOptionBool(connectedPacketSlotData, "kapow_camp");
            HMSOrca = GetSlotOptionBool(connectedPacketSlotData, "hms_orca");
            HMPOffshore = GetSlotOptionBool(connectedPacketSlotData, "hmp_offshore");
            FortTundra = GetSlotOptionBool(connectedPacketSlotData, "fort_tundra");
            Area17 = GetSlotOptionBool(connectedPacketSlotData, "area_17");
            AirForceCon = GetSlotOptionBool(connectedPacketSlotData, "air_force_con");
            USSAnomaly = GetSlotOptionBool(connectedPacketSlotData, "uss_anomaly");

            
            StrengthStep = GetSlotOption(connectedPacketSlotData, "strength_step");
            StaminaStep = GetSlotOption(connectedPacketSlotData, "stamina_step");
            IntellectStep = GetSlotOption(connectedPacketSlotData, "intellect_step");
            UniqueEscapesRequired = GetSlotOption(connectedPacketSlotData, "unique_escapes_required");
            
            foreach (KeyValuePair<string, object> slotData in connectedPacketSlotData)
            {
                ConsoleLogging.LogDebug($"String: {slotData.Key} Value: {slotData.Value}");
            }
        }
    }

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

        public List<long> checkedLocations;
        public List<long> missingLocations;
        public SlotData slotData;

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

            Session = ArchipelagoSessionFactory.CreateSession(connectionInfo.serverAddress); //Test Parameters

            Session.Socket.SocketOpened += () => { ConsoleLogging.LogDebug("Socket Opened"); };

            //RoomInfo Packet Received - Do parse on incoming packets
            //Optional GetDataPackage - Also Do parse on incoming packet if this is the case
            //TODO: https://alwaysintreble.github.io/Archipelago/network%20protocol.html#receiveditems

            LoginResult loginResult = null;
            try
            {
                ConsoleLogging.LogInfo("Host = " + connectionInfo.serverAddress);

                // 1. Tell the server to listen to StealthLink if the config is on
                List<string> tagsList = new List<string>();
                tagsList.Add("StealthLink");
                string[] tags = tagsList.Count > 0 ? tagsList.ToArray() : null;

                
                SubscribeToServerPackets();
                loginResult = Session.TryConnectAndLogin(connectionInfo.game, connectionInfo.slotName, ItemsHandlingFlags.AllItems, connectionInfo.gameVersion, tags, "", connectionInfo.password);
            }
            catch (Exception e)
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, "Connection refused by server");
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, "Failed with these parameters: " + connectionInfo.serverAddress + ": " + connectionInfo.password + ": " + connectionInfo.slotName);
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.ServerRefusedConnection, e.ToString());
                //TODO: Send Chat Failure

                connectedToServer = false;
                Session.Socket.DisconnectAsync();

                return false;
            }

            if (loginResult is LoginSuccessful loginSuccess)
            {
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
                MessageHandler.instance.SendConnectionStatus();
                Session.Socket.DisconnectAsync();
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
            SubscribeToServerPackets();
            SubscribeToChatEvent();
        }

        private void SubscribeToServerPackets()
        {
            Session.Socket.PacketReceived += (packet) =>
            {
                if (packet.PacketType == ArchipelagoPacketType.Bounced)
                {
                    BouncedPacket bouncedPacket = (BouncedPacket)packet;
                    if (bouncedPacket.Tags != null && bouncedPacket.Tags.Contains("StealthLink"))
                    {
                        StealthLinkHandling(bouncedPacket);
                    }
                }

                if (packet.PacketType == ArchipelagoPacketType.LocationInfo)
                {
                    LocationInfoPacket locationInfoPacket = (LocationInfoPacket)packet;
                    foreach (NetworkItem item in locationInfoPacket.Locations)
                    {
                        ConsoleLogging.LogDebug($"{item.Location.ToString()}");
                        ConsoleLogging.LogDebug($"{item.Player.ToString()}");
                        ConsoleLogging.LogDebug($"{item.Flags.ToString()}");
                        ConsoleLogging.LogDebug($"{item.Item.ToString()}");
                    }

                    ConsoleLogging.LogDebug($"Item gets returned as: {Session.Locations.GetLocationNameFromId(locationInfoPacket.Locations[0].Location)}");
                    ConsoleLogging.LogDebug($"Item gets returned as: {Session.Items.GetItemName(locationInfoPacket.Locations[0].Item, ModInfo.GameName)}");
                    MessageHandler.instance.ScoutHintToGame(Session.Locations.GetLocationNameFromId(locationInfoPacket.Locations[0].Location, ModInfo.GameName), Session.Items.GetItemName(locationInfoPacket.Locations[0].Item, ModInfo.GameName));
                }

                if (packet.PacketType == ArchipelagoPacketType.Connected)
                {
                    
                    ConnectedPacket connectedPacket = (ConnectedPacket)packet;
                    
                    ProcessConnectedPacket(connectedPacket);
                }
            };
                    checkedLocations = new List<long>(connectedPacket.LocationsChecked);

                    missingLocations = new List<long>(connectedPacket.MissingChecks);

                    slotData = new SlotData(connectedPacket.SlotData);
                    
                }

                    
                    
        }

        private void SubscribeToChatEvent()
        {
            Session.MessageLog.OnMessageReceived += (message) =>
            {
                lock (incomingChatQueue)
                {
                    incomingChatQueue.Enqueue(message.ToString());
                    MessageHandler.instance.ProcessChatQueue();
                }
            };
        }

        private void SocketClosedEvent()
        {
            Session.Socket.SocketClosed += (reason) =>
            {
                connectedToServer = false;
                Session = null;
                ConsoleLogging.LogWarning($"Connection lost: {reason}");
                incomingChatQueue.Enqueue($"Connection lost to server: {reason}");
                MessageHandler.instance.ProcessChatQueue();
            };
        }

        private void SetUpDeathlink()
        {
            //TODO: Look at optional deathlink - Check player options/yaml etc
            DeathLinkService = Session.CreateDeathLinkService();
            DeathLinkService.EnableDeathLink();
            DeathLinkService.OnDeathLinkReceived += MessageHandler.instance.SendDeathlinkToGame;
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
            return connectedToServer;
        }

        public void SayToServer(string message)
        {
            Session.Say(message);
        }

        public void SendCheck(string check, bool isID)
        {
            string[] checkParts = check.Split(": ");
            string stat = "n/a";
            int lastStat = 29;
            int statToParse = int.Parse(checkParts[1]);
            if (checkParts[0] == "Stamina Stat")
            {
                stat = "Stamina";

                lastStat = lastStamina;
                if (lastStamina < statToParse)
                {
                    lastStamina = statToParse;
                }
                else
                {
                    return;
                }

            }
            else if (checkParts[0] == "Strength Stat")
            {
                stat = "Strength";
                
                lastStat = lastStrength;
                if (lastStrength < statToParse)
                {
                    lastStrength = statToParse;
                }
                else
                {
                    return;
                }
            }
            else if (checkParts[0] == "Intellect Stat")
            {
                stat = "Intellect";
                
                lastStat = lastIntellect;
                if (lastIntellect < statToParse)
                {
                    lastIntellect = statToParse;
                }
                else
                {
                    return;
                }
            }

            if (stat != "n/a")
            {
                for (int i = lastStat + 1; i <= int.Parse(checkParts[1]); i++)
                {
                    if (i != 100)
                    {
                        SendActualCheck($"{checkParts[0]}: {i}", isID);
                    }
                    else
                    {
                        SendActualCheck($"{checkParts[0]}: Max", isID);
                    }
                }
            }
            else
            {
                SendActualCheck(check, isID);
            }
        }

        private void SendActualCheck(string check, bool isID)
        {
            long id = -1;
            if (!isID)
            {
                id = Session.Locations.GetLocationIdFromName(connectionInfo.game, check);
                // ConsoleLogging.LogDebug($"{check} comes back as ID: {id}");
            }
            if (isID)
            {
                id = long.Parse(check);
            }
            
            if (id == -1)
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.CheckSendFailure,"Check came back as ID '-1', aborting check send.");
            }

            if (checkedLocations.Contains(id))
            {
                return;
            }

            if (missingLocations.Contains(id))
            {
                checkedLocations.Add(id);
                ConsoleLogging.LogDebug($"Added {id} to the checked locations, which sits at {checkedLocations.Count} items.");
                //TODO: Set up actual check sending - Difference between CompleteLocationChecks and CompleteLocationChecksAsync
                ConsoleLogging.LogSend("Check Send: " + Session.Locations.GetLocationNameFromId(id));
                Session.Locations.CompleteLocationChecks(checkedLocations.Except(Session.Locations.AllLocationsChecked).ToArray());
            }
            else
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.CheckSendFailure, "Check not found in list of locations yet to be sent");
            }

        }

        public void SendDeathLinkToServer(string slot, string reason)
        {
            DeathLink deathlink = new DeathLink(slot, reason);
            DeathLinkService.SendDeathLink(deathlink);
        }

        public void SendGoalToServer()
        {
            ConsoleLogging.LogSuccess("Slot Goaled. Congratulations!");
            Session.SetGoalAchieved();
        }

        private void StealthLinkHandling(BouncedPacket bouncedPacket)
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

        public void SendStealthLink(string slotName)
        {
            var bouncePacket = new Archipelago.MultiClient.Net.Packets.BouncePacket
            {
                // bypasses the JSON dictionary entirely and hide the sender's name inside the tags
                Tags = new List<string> { "StealthLink", "StealthSource|" + slotName }
            };
            Session.Socket.SendPacket(bouncePacket);

            // This is for diagnostics to see if the stealthlink works
            //TODO: GET A SECOND CLIENT RUNNING TO TEST THIS PROPERLY
            SayToServer($"[DEBUG ALERT] {slotName} just triggered a prison lockdown!");
        }

        public void ScoutHint(string hintPacketHintName)
        {
            ConsoleLogging.LogDebug("Starting to Scout Hint");

            long id = Session.Locations.GetLocationIdFromName(connectionInfo.game, hintPacketHintName);

            ConsoleLogging.LogDebug($"Searching for: {hintPacketHintName}. Returned ID: {id}");
            Session.Locations.ScoutLocationsAsync(true, id);

            ConsoleLogging.LogDebug("Hint should be scouted");
        }
    }
}