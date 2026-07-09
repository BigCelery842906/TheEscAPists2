using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using AUTOGEN_T17Wwise_Enums;
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
                    isConnected = false;
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
            case PacketType.StealthLink:
            {
                Packet.StealthLinkPacket stealthLinkPacket = (Packet.StealthLinkPacket)message;
                stealthLinkCulprit = stealthLinkPacket.slotName;
                pendingStealthLink = true;
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
        
        if (pendingStealthLink)
        {
            pendingStealthLink = false;
 
 
            lastStealthLinkTime = Time.time;
 
            RoutineManager rm = RoutineManager.GetInstance();
            if (rm != null && !rm.IsLockdownActive())
            {
                SolitaryManager.GetInstance().TriggerLockdown();
 
                logging.SendChatMessage($"[StealthLink] {stealthLinkCulprit} got caught! PRISON LOCKDOWN INITIATED!");
                Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
                if (localPlayer != null) AkSoundEngine.PostEvent("Play_UI_Error", localPlayer.gameObject);
            }
        }
        
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
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Logger.LogWarning("[TEST] Simulating INBOUND DeathLink packet...");
            pendingDeathLink = true;
        }
 
        // Press F5 to simulate a friend triggering a lockdown in another prison
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Logger.LogWarning("[TEST] Simulating INBOUND StealthLink packet...");
            pendingStealthLink = true;
            stealthLinkCulprit = "A Sneakin Friend";
            SendStealthLink();
        }
 
        // ------------------------------------------
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
    
    
    #region Game's Code - Largely from GoofTarune
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
                healthProp.SetValue(localPlayer.m_CharacterStats, 0f, null);
                localPlayer.SetIsKnockedOut(true, localPlayer.m_CharacterTarget);
                logging.SendChatMessage($"[DeathLink] {lastDeathLinkReason}");
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
    
    [HarmonyPatch(typeof(RoutineManager), "SetLockdownRoutine")]
    public class RoutineManager_SetLockdownRoutine_Patch
    {
        static void Prefix(RoutineManager __instance, bool active)
        {
            if (!isConnected) return;
 
            // If the prison is going into Lockdown, and it wasn't ALREADY in lockdown, and it hasn't synced an alarm in the last 10 seconds...
            if (active && !__instance.IsLockdownActive() && Time.time - lastStealthLinkTime > 10f)
            {
                lastStealthLinkTime = Time.time;
                SendStealthLink(); // Broadcast to the server
            }
        }
    }
    
    public static void SendStealthLink()
    {
        if (!isConnected) return;

        Packet.StealthLinkPacket stealhLink = new Packet.StealthLinkPacket(slotName);
        instance.SerialiseAndSend(stealhLink);
    }
    
    [HarmonyPatch(typeof(RoutineManager), "StartInit")]
        public class RoutineManager_StartInit_Patch
        {
            // store the vanilla schedule here so there is a clean slate to shuffle from every morning
            public static List<Routines> vanillaBaseTypes = new List<Routines>();
            public static List<RoutineSubTypes> vanillaSubTypes = new List<RoutineSubTypes>();
            public static List<Events> vanillaMusic = new List<Events>();
            public static List<string> vanillaUITags = new List<string>();
 
            static void Postfix(RoutineManager __instance)
            {
                // Fully updated to use the Enum instead of strings
                // if (!configEnabled.Value || configRandSchedule.Value == ScheduleMode.Off) return;
                // if (__instance.m_RoutinesData == null || __instance.m_RoutinesData.m_Routines == null || __instance.m_RoutinesData.m_Routines.Count <= 1) return;
 
                // 1. Save the vanilla data immediately on load
                vanillaBaseTypes.Clear();
                vanillaSubTypes.Clear();
                vanillaMusic.Clear();
                vanillaUITags.Clear();
 
                foreach (var routine in __instance.m_RoutinesData.m_Routines)
                {
                    vanillaBaseTypes.Add(routine.m_BaseRoutineType);
                    vanillaSubTypes.Add(routine.m_SubRoutineType);
                    vanillaMusic.Add(routine.m_RoutineMusic);
                    vanillaUITags.Add(routine.m_LocalizationTag);
                }
 
                // 2. Hook into the daily engine trigger so it scrambles again at midnight
                __instance.OnDayChange += () => ScrambleDailySchedule(__instance);
 
                // 3. Do the initial scramble for Day 1
                ScrambleDailySchedule(__instance);
            }
 
            public static void ScrambleDailySchedule(RoutineManager rm)
            {
                // Fully updated to use the Enum instead of strings
                // ScheduleMode mode = configRandSchedule.Value;
                ScheduleMode mode = ScheduleMode.Chaos;
                if (mode == ScheduleMode.Off) return;
 
                var routines = rm.m_RoutinesData.m_Routines;
                List<int> indicesToShuffle = new List<int>();
 
                // First, restore everything to vanilla, then pick which blocks are allowed to shuffle
                for (int i = 0; i < routines.Count; i++)
                {
                    routines[i].m_BaseRoutineType = vanillaBaseTypes[i];
                    routines[i].m_SubRoutineType = vanillaSubTypes[i];
                    routines[i].m_RoutineMusic = vanillaMusic[i];
                    routines[i].m_LocalizationTag = vanillaUITags[i];
 
                    // If we are in "Safe" mode, DO NOT add Lights Out to the shuffle pool. Please leave it alone.
                    if (mode == ScheduleMode.Safe && vanillaBaseTypes[i] == Routines.LightsOut) continue;
 
                    indicesToShuffle.Add(i);
                }
 
                // Extract the data ONLY for the blocks it's allowed to shuffle
                List<Routines> shuffleBase = new List<Routines>();
                List<RoutineSubTypes> shuffleSub = new List<RoutineSubTypes>();
                List<Events> shuffleMusic = new List<Events>();
                List<string> shuffleUI = new List<string>();
 
                foreach (int i in indicesToShuffle)
                {
                    shuffleBase.Add(vanillaBaseTypes[i]);
                    shuffleSub.Add(vanillaSubTypes[i]);
                    shuffleMusic.Add(vanillaMusic[i]);
                    shuffleUI.Add(vanillaUITags[i]);
                }
 
                // RNG seed based on Roulette Seed + Current In-Game Day (So it's different every day)
                string seedStr = configRouletteSeed.Value.Trim().ToLower();
                int seed = 12345;
                if (seedStr != "random" && !string.IsNullOrEmpty(seedStr)) int.TryParse(seedStr, out seed);
 
                System.Random rng = new System.Random(seed + rm.GetDaysElapsed());
 
                // Fisher-Yates Shuffle
                int n = shuffleBase.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
 
                    var tb = shuffleBase[k]; shuffleBase[k] = shuffleBase[n]; shuffleBase[n] = tb;
                    var ts = shuffleSub[k]; shuffleSub[k] = shuffleSub[n]; shuffleSub[n] = ts;
                    var tm = shuffleMusic[k]; shuffleMusic[k] = shuffleMusic[n]; shuffleMusic[n] = tm;
                    var tu = shuffleUI[k]; shuffleUI[k] = shuffleUI[n]; shuffleUI[n] = tu;
                }
 
                // Inject the scrambled data back into their original time slots
                for (int i = 0; i < indicesToShuffle.Count; i++)
                {
                    int realIndex = indicesToShuffle[i];
                    routines[realIndex].m_BaseRoutineType = shuffleBase[i];
                    routines[realIndex].m_SubRoutineType = shuffleSub[i];
                    routines[realIndex].m_RoutineMusic = shuffleMusic[i];
                    routines[realIndex].m_LocalizationTag = shuffleUI[i];
                }
 
                Plugin.instance.Logger.LogWarning($"[CHAOS] Day {rm.GetDaysElapsed()} schedule scrambled!");
            }
        }
     #endregion
        
 #endregion       
    
}