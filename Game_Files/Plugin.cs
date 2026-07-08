using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using BC_Dual_Ref_Library;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TheEscAPists2;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("TheEscapists2.exe")] //Set Specifically for the Escapists 2
public class Plugin : BaseUnityPlugin
{
    public static Plugin instance;
    
    private Logging logging;

    private Process bridgeProcess;
    private NamedPipeClientStream pipeClientForBridge;
    private NamedPipeClientStream pipeClientForGame;
    private StreamReader reader;
    private StreamWriter writer;

    private static string slotName;
    public static bool isConnected; 
    
    public static string lastDeathLinkSlot = "";
    public static string lastDeathLinkReason = "";
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (instance != this)
        {
            //TODO:kill this instance somehow
        }
        
        logging = new Logging(base.Logger);
        StartBridge();
        StartPipes();
        
        InitializeConfigs();
        UpdateBlacklist();
 
        Harmony harmony = new Harmony("com.yourname.escapists2.randomizer");
        harmony.PatchAll();
        Logger.LogInfo("V3.2.0 - Architecture Organized"); //??
    }

    private void StartBridge()
    {
        if ((bridgeProcess != null && !bridgeProcess.HasExited) || Process.GetProcessesByName("TheEscAPists2Bridge").Length > 0)
        {
            Logger.LogInfo("Bridge already running.");
            return;
        }
        
        
        string pluginDir = Path.GetDirectoryName(Info.Location); 
        string newPluginDir = Directory.GetParent(pluginDir).ToString();
        
        string bridgePath = Path.Combine(newPluginDir, Path.Combine("Bridge", "TheEscAPists2Bridge.exe"));
        Logger.LogError(bridgePath);

        if (!File.Exists(bridgePath))
        {
            Logger.LogError("Bridge exe not found: " + bridgePath);
            return;
        }
        
        bridgeProcess = Process.Start(new ProcessStartInfo{FileName = bridgePath, WorkingDirectory = pluginDir, UseShellExecute = true});

        if (bridgeProcess != null)
        {
            Logger.LogInfo("Bridge running.");
        }
        else
        {
            Logger.LogWarning("Bridge not running.");
        }
        
    }

    void StartPipes()
    {
        StartPipeForBridge();
        StartPipeForGame();
    }

    private void StartPipeForGame()
    {
        if (pipeClientForGame != null && pipeClientForGame.IsConnected)
        {
            return;
        }
        
        pipeClientForGame = new NamedPipeClientStream(".", "EscAPists2PipeToGame", PipeDirection.In);
        
            try
            {
                pipeClientForGame.Connect(500);
            }
            catch (Exception ex)
            {
                Logger.LogError("(ForGame) Failed to connect to bridge.");
                pipeClientForGame = null;
                return;
            }
        
        reader = new StreamReader(pipeClientForGame);
        
        Logger.LogInfo("(ForGame) Connected to bridge.");
        
        Thread thread = new Thread(Listen);
        thread.IsBackground = true;
        thread.Start();
    }

    void StartPipeForBridge()
    {

        if (pipeClientForBridge != null && pipeClientForBridge.IsConnected)
        {
            return;
        }
        
        pipeClientForBridge = new NamedPipeClientStream(".", "EscAPists2PipeToBridge", PipeDirection.Out);
        
            try
            {
                pipeClientForBridge.Connect(500);

                if (pipeClientForBridge.IsConnected)
                {
                    Logger.LogInfo("(ForBridge) Connected to bridge.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("(ForBridge) Failed to connect to bridge.");
            }
            
        writer = new StreamWriter(pipeClientForBridge)
        {
            AutoFlush = true
        };
        
        Logger.LogInfo("(ForBridge) Connected to bridge.");

       
    }

    void Listen()
    {
        try
        {
            while (true)
            {
                Logger.LogDebug("Waiting for line....");
                string message = reader.ReadLine();

                if (message == null)
                {
                    Logger.LogWarning("Bridge disconnected. Holding Pipe for reconnect.");
                    return;
                }

                ProcessIncomingPacket(message);
                Logger.LogInfo("Received from bridge -> " + message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.ToString());
        }
        finally
        {
            logging.SendChatMessage("Bridge disconnected. Holding Pipe for reconnect.");
        }

    }

    private void ProcessIncomingPacket(string serialisedMessage)
    {
        if (serialisedMessage == null)
        {
            return;
        }
            
        Packet? message = Packet.Deserialise(serialisedMessage);
            
        switch (message.Type)
        {
            case PacketType.Connection:
            {
                Packet.ConnectionPacket connectionPacket = (Packet.ConnectionPacket)message;

                slotName = connectionPacket.slotName;
                
                break;
            }
            case PacketType.ChatMessage:
            {
                Packet.ChatMessagePacket chatMessagePacket = (Packet.ChatMessagePacket)message;
                logging.SendChatMessage(chatMessagePacket.chatMessage);
                break;
            }
            case PacketType.Check:
            {
                Packet.CheckPacket checkPacket = (Packet.CheckPacket)message;
                //TODO: Implement goof's code
                break;
            }
            case PacketType.Deathlink:
            {
                Packet.DeathlinkPacket deathlinkPacket = (Packet.DeathlinkPacket)message;
                lastDeathLinkSlot = deathlinkPacket.slotName;
                lastDeathLinkReason = deathlinkPacket.reason;
                pendingDeathLink = true;
                break;
            }
            case PacketType.Goal:
            {
                //TODO: implement goof's code
                break;
            }
            case PacketType.IsConnected:
            {
                Packet.IsConnectedPacket isConnectedPacket = (Packet.IsConnectedPacket)message;
                isConnected = isConnectedPacket.isConnected;
                break;
            }
            default:
            {
                Logger.LogWarning("Unknown message type: " + message.Type);
                break;
            }
        }
    }


    private void Update()
    {
        if (pendingDeathLink) ProcessDeathLink();
        
        if (Input.GetKeyDown(KeyCode.F6))
        {
           StartPipes();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (logging != null)
            {
               logging.SendChatMessage("Message test");
               SendMessageToBridge("Test message");
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            SendAllPacketsToBridge();
            // Logging.BridgeConnect.SetGoalAchieved();
        }
    }

   

    private void OnDestroy()
    {
        KillEverything();
    }

    ~Plugin()
    {
        KillEverything();
    }

    void KillEverything()
    {
        bridgeProcess?.Kill();
        pipeClientForBridge?.Dispose(); 
        pipeClientForBridge = null;
        
        pipeClientForGame?.Dispose();
        pipeClientForGame = null;
            
        writer?.Dispose();
        writer = null;
            
        reader?.Dispose();
        reader = null;
    }

    void SendMessageToBridge(string message)
    {
        //TODO - REPLACE WITH ACTUAL SLOT NAME
        Packet.ChatMessagePacket chatMessagePacket = new Packet.ChatMessagePacket(slotName, message);
            
        // SerialiseAndSend(chatMessagePacket);
        
        // Logger.LogInfo(chatMessagePacket.ToString());
        SerialiseAndSend(chatMessagePacket);
        // writer?.WriteLine(chatMessagePacket.ToString());
    }

    void SendAllPacketsToBridge()
    {
        SerialiseAndSend(new Packet.ConnectionPacket(slotName, "archipelago.gg:56644", ""));
        SerialiseAndSend(new Packet.ChatMessagePacket(slotName, "This is a test message packet"));
        SerialiseAndSend(new Packet.CheckPacket(slotName, "Check Name"));
        SerialiseAndSend(new Packet.DeathlinkPacket(slotName, "Got beaten by dogs"));
        SerialiseAndSend(new Packet.GoalPacket(slotName));
    }
    
    public void SerialiseAndSend(Packet packet)
    {
        string packetJson = packet.ToJson();
        SendToBridge(packetJson);
    }
    
    private void SendToBridge(string message)
    {
        Logger.LogInfo($"Sending to bridge -> {message}");
        writer?.WriteLine(message);
    }
    
    
    #region Goof's Code
    //GOOF'S CODE
    private void ProcessDeathLink()
    {
        Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
        if (localPlayer != null && localPlayer.m_CharacterStats != null)
        {
            PropertyInfo healthProp = typeof(CharacterStats).GetProperty("Health", BindingFlags.Public | BindingFlags.Instance)
                                      ?? typeof(Character).GetProperty("Health", BindingFlags.Public | BindingFlags.Instance);
            if (healthProp != null)
            {
                Player_SetIsKnockedOut_Patch.lastDeathLinkTime = Time.time;
                // healthProp.SetValue(localPlayer.m_CharacterStats, 0f, null);
                localPlayer.m_CharacterStats.DecreaseHealth(localPlayer.m_CharacterStats.Health+1);
                //I want to be able to get medics deployed so you don't just get set to 0
                // localPlayer.m_CharacterTarget.SetIsKnockedOut(true, localPlayer);
                logging.SendChatMessage($"[DeathLink] {lastDeathLinkSlot} {lastDeathLinkReason}");
                AkSoundEngine.PostEvent("Play_UI_Error", localPlayer.gameObject);
            }
        }
        pendingDeathLink = false;
    }
    
    
    #region Config Stuff
    
    public enum ScheduleMode { Off, Safe, Chaos }
    // ==========================================
        // === CONFIGURATION VARIABLES            ===
        // ==========================================
        public static ConfigEntry<bool> configEnabled;
        public static ConfigEntry<string> configMode;
        public static ConfigEntry<string> configBlacklist;
        public static ConfigEntry<string> configEscapeItemBans;
        public static ConfigEntry<bool> configRandDesks;
        public static ConfigEntry<bool> configRandVendors;
        public static ConfigEntry<bool> configRandGuards;
        public static ConfigEntry<bool> configRandInmates;
        public static ConfigEntry<bool> configContrabandRoulette;
        public static ConfigEntry<string> configRouletteSeed;
        public static ConfigEntry<bool> configWeightedLoot;
        public static ConfigEntry<bool> configRandomizeRecipes;
        public static ConfigEntry<string> configAPServerIP;
        public static ConfigEntry<int> configAPServerPort;
        public static ConfigEntry<string> configAPSlotName;
        public static ConfigEntry<string> configAPPassword;
        public static ConfigEntry<string> configAPCurrentSeed;
        public static ConfigEntry<int> configAPItemIndex;
        public static ConfigEntry<bool> configAPConnectTrigger;
        public static ConfigEntry<bool> configRandNPCStats;
        public static ConfigEntry<ScheduleMode> configRandSchedule;
        public static ConfigEntry<bool> configStealthLink;
 
 
        // ==========================================
        // === GAME STATE TRACKING                ===
        // ==========================================
        public static List<int> parsedBlacklist = new List<int>();
        public static HashSet<int> escapeBansList = new HashSet<int>();
        public static List<ItemData> globalCleanPool = new List<ItemData>();
        public static HashSet<int> unlockedBlueprints = new HashSet<int>();
        public static List<int> cloudInventory = new List<int>();
 
        public static Queue<int> incomingItemsQueue = new Queue<int>();
        public static Queue<string> incomingChatQueue = new Queue<string>();
 
        public static bool sentMaxIntellect = false;
        public static bool sentMaxStrength = false;
        public static bool sentMaxFitness = false;
        public static bool pendingDeathLink = false;
        private float cloudSyncTimer = 0f;
 
        // ==========================================
        // === STEALTH TRACKING                   ===
        // ==========================================
 
 
        public static bool pendingStealthLink = false;
        public static string stealthLinkCulprit = "";
        public static float lastStealthLinkTime = 0f; // To prevent infinite alarm loops
 
        // ==========================================
        // === CORE ID LISTS                      ===
        // ==========================================
        public static List<int> blueprintRequiredItems = new List<int>
        {
            113, 182, 322, 210, 140, 268, 259, 442, 313, 307, 309, 312, 315,
            420, 417, 418, 419, 244, 415, 412, 413, 414, 185, 143, 175, 286,
            387, 193, 250, 371, 195, 249, 370, 196, 251, 372, 187, 186, 459,
            265, 384, 426, 316, 377, 376, 379, 271, 220, 284, 184, 343, 264, 300, 136
        };
 
        public static Dictionary<int, long> craftingLocationIDs = new Dictionary<int, long>()
        {
            { 387, 80100 }, { 193, 80101 }, { 196, 80102 }, { 195, 80103 },
            { 187, 80104 }, { 113, 80106 }, { 313, 80105 }, { 307, 80107 },
            { 309, 80108 }, { 312, 80109 }, { 315, 80110 }, { 322, 81100 },
            { 210, 81101 }
        };
        
        #endregion
        
        private void InitializeConfigs()
        {
            configEnabled = Config.Bind("1. General", "Enabled", true, "Turn the randomizer on or off.");
            configMode = Config.Bind("1. General", "Mode", "Chaos", "Options: Chaos, Safe.");
 
            string defaultBans = "476,457,446,458,441,367,484,132,160,161,212,213,326,327,335,336,406,407,449,166,432,433,450,429,426,452,242,243,411,412,413,414,415,185,113";
            configBlacklist = Config.Bind("2. Rules", "BlacklistedIDs", defaultBans, "Comma-separated Item IDs that will NEVER spawn.");
            string escapeBans = "136,182,148,150,151,322,140,152,153,141,221,205,352,268,259,184,437,442,223,331,343,146,431,264,300,267,227,501,1123,1124,1126,1133,1129,1152,1168,1169,1177,1178,186,187,183,398";
            configEscapeItemBans = Config.Bind("2. Rules", "EscapeItemIDs", escapeBans, "These items will NEVER spawn in desks/shops");
 
            configRandDesks = Config.Bind("3. Targets", "RandomizeDesks", true, "Randomize loot found inside desks.");
            configRandVendors = Config.Bind("3. Targets", "RandomizeShops", true, "Randomize the items sold by inmates.");
            configRandGuards = Config.Bind("3. Targets", "RandomizeGuardPockets", true, "Randomize the weapons/loot guards carry.");
            configRandInmates = Config.Bind("3. Targets", "RandomizeInmatePockets", true, "Randomize the weapons/loot normal inmates carry.");
            configContrabandRoulette = Config.Bind("4. Chaos Modes", "ContrabandRoulette", true, "Randomly scrambles which items are illegal.");
            configRouletteSeed = Config.Bind("4. Chaos Modes", "RouletteSeed", "Random", "Seed for procedural laws.");
            configWeightedLoot = Config.Bind("5. Balance", "WeightedLoot", true, "Make crafting items common, and weapons/contraband rare.");
            configRandomizeRecipes = Config.Bind("6. Overhaul", "RandomizeRecipes", true, "Scrambles ALL crafting recipes.");
 
            configAPServerIP = Config.Bind("7. Archipelago", "ServerIP", "localhost", "The IP address of your AP server.");
            configAPServerPort = Config.Bind("7. Archipelago", "ServerPort", 38281, "The port of your AP server.");
            configAPSlotName = Config.Bind("7. Archipelago", "SlotName", "Player1", "Your slot name in the AP server.");
            configAPPassword = Config.Bind("7. Archipelago", "Password", "", "The password for the AP server.");
            configAPCurrentSeed = Config.Bind("7. Archipelago", "CurrentSeed", "", "Tracks the current AP seed to prevent item storms.");
            configAPItemIndex = Config.Bind("7. Archipelago", "ProcessedItemIndex", 0, "Do not touch. Tracks how many items you've received.");
            configRandNPCStats = Config.Bind("3. Targets", "RandomizeNPCStats", true, "Scramble the strength, fitness, and intellect of all guards and inmates.");
            configAPConnectTrigger = Config.Bind("7. Archipelago", "Connect to Server", false, "Check this box to connect. (It will automatically uncheck itself)");
            configRandSchedule = Config.Bind("4. Chaos Modes", "RandomizeSchedule", ScheduleMode.Safe, "Off: Vanilla schedule. Safe: Random daytime, normal sleep. Chaos: 8-hour Rollcall at 2 AM.");
            configStealthLink = Config.Bind("7. Archipelago", "StealthLink", false, "Share alarms and lockdowns with other players across the multiverse.");
 
            configAPConnectTrigger.SettingChanged += (sender, e) =>
            {
                if (configAPConnectTrigger.Value)
                {
                    if (!isConnected)
                    {
                        Logger.LogInfo("(GOOFS) Attempting to connect from F1 Menu...");
                        Packet.ConnectionPacket connectionPacket = new Packet.ConnectionPacket(slotName, "TESTING", "");
                    }
                    else
                    {
                        logging.SendChatMessage("You are already connected to the server!");
                    }
 
 
                    configAPConnectTrigger.Value = false;
                }
            };
        }
    
        private void UpdateBlacklist()
        {
            parsedBlacklist.Clear();
            escapeBansList.Clear();
 
            string[] rawIDs = configBlacklist.Value.Split(',');
            foreach (string rawID in rawIDs)
            {
                if (int.TryParse(rawID.Trim(), out int id)) parsedBlacklist.Add(id);
            }
 
            string[] escapeIDs = configEscapeItemBans.Value.Split(',');
            foreach (string escID in escapeIDs)
            {
                if (int.TryParse(escID.Trim(), out int id)) escapeBansList.Add(id);
            }
        }
        
        #region Harmony Patches
    [HarmonyPatch(typeof(Player), "SetIsKnockedOut")]
    public class Player_SetIsKnockedOut_Patch
    {
        public static float lastDeathLinkTime = 0f;
 
        static void Postfix(Player __instance, bool knockedOut, Character characterResponsible)
        {
            // if (!RandomizerPlugin.configEnabled.Value) return;
 
 
            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
 
            if (localPlayer != null && __instance == localPlayer && knockedOut)
            {
                if (Time.time - lastDeathLinkTime < 5f) return;
                lastDeathLinkTime = Time.time;
 
                string killerName = "their own incompetence";
                if (characterResponsible != null)
                {
                    killerName = characterResponsible.name;
    
                    if (killerName.Contains("(")) killerName = killerName.Substring(0, killerName.IndexOf("(")).Trim();
                }
                
                string deathMessage = $"{slotName} was beaten to a pulp by {killerName}!";
 
                Packet.DeathlinkPacket deathLink = new Packet.DeathlinkPacket(slotName, deathMessage);
                Plugin.instance.SerialiseAndSend(deathLink);
            }
        }
    }
    
    
     #endregion
        
 #endregion       
    
}