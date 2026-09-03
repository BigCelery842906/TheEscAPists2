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
using UnityEngine.SceneManagement;
using Random = System.Random;

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
    
    private List<string> prisonUnlocksReceived;
    private List<int> blueprintsReceived;
        
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
 
        Harmony harmony = new Harmony("com.bigcelery842906.escapists2.archipelago.randomiser");
        harmony.PatchAll();
        Logger.LogInfo("V3.2.0 - Architecture Organized"); //??

        GlobalStart.EnteredLevelEvent += DoPlayerStats;
        GlobalStart.EndLevelEvent += InGameFalse;
    }

    void InGameFalse()
    {
        inGame = false;
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
            case PacketType.DeathLink:
            {
                Packet.DeathLinkPacket deathLinkPacket = (Packet.DeathLinkPacket)message;
                lastDeathLinkSlot = deathLinkPacket.slotName;
                lastDeathLinkReason = deathLinkPacket.reason;
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
                
                strength_prog_count = 0;
                stamina_prog_count = 0;
                intellect_prog_count = 0;
                break;
            }
            case PacketType.Hint:
            {
                Packet.HintPacket hintPacket = (Packet.HintPacket)message;
                logging.instance.SendChatMessage("Hint Packet Received but not implemented");
                break;
            }
            case PacketType.Item:
            {
                Packet.ItemPacket itemPacket = (Packet.ItemPacket)message;
                logging.instance.SendChatMessage("Item Packet Received but not implemented - NEXT THING TO DO");
                
                ProcessItem(itemPacket);
                break;
            }
            case PacketType.SlotData:
            {
                Packet.SlotDataPacket slotDataPacket = (Packet.SlotDataPacket)message;
                
                strength_step = slotDataPacket.strength_step;
                stamina_step = slotDataPacket.stamina_step;
                intellect_step = slotDataPacket.intellect_step;
                
                logging.SendChatMessage($"Strength Step: {strength_step},  Stamina Step: {stamina_step}, Intellect Step: {intellect_step}");

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

            TriggerLockdown(true);
        }

        if (inGame)
        {
            if (itemQueue.Count != 0 || moneyToAdd != 0)
            {
                DequeueItemQueue();
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

        if (Input.GetKeyDown(KeyCode.F7))
        {
            SetStats();
        }
        
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            if (localPlayer != null && localPlayer.m_CharacterStats != null)
            {
                localPlayer.m_CharacterStats.Strength = 100;
                localPlayer.m_CharacterStats.Intellect = 100;
                localPlayer.m_CharacterStats.Cardio = 100;
                localPlayer.m_CharacterStats.Heat = 0;
                localPlayer.m_CharacterStats.Energy = 100;
                localPlayer.m_CharacterStats.Health = 100;
 
                // Add $50 to the player's wallet
                localPlayer.m_CharacterStats.Money += 500f;
 
                logging.SendChatMessage("All Stats Maxed and added $500!");
                AkSoundEngine.PostEvent("Play_UI_Take_Item", localPlayer.gameObject);
            }
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            ItemData[] items = Resources.LoadAll<ItemData>("Prefabs/Items");

            foreach (ItemData item in items)
            {
                instance.SendMessageToBridge($"ID: {item.m_ItemDataID} - Tag: {item.m_ItemLocalizationTag}");
            }

            foreach (CraftManager.Recipe recipe in CraftManager.GetInstance().GetAllRecipes())
            {
                instance.SendMessageToBridge($" {{\"{recipe.m_Product.name}\", {CraftManager.GetInstance().GetRecipeId(recipe)}}},");
            }
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

    void SendCheckIDToBridge(long checkID)
    {
        SendCheckToBridge(checkID.ToString(), true);
    }
    
    void SendCheckToBridge(string check, bool isID = false)
    {
        Packet.CheckPacket checkPacket = new Packet.CheckPacket(slotName, check, isID);
        SerialiseAndSend(checkPacket);
        
    }

    void SendAllPacketsToBridge()
    {
        SerialiseAndSend(new Packet.ConnectionPacket(slotName, "archipelago.gg:56644", ""));
        SerialiseAndSend(new Packet.ChatMessagePacket(slotName, "This is a test message packet"));
        SerialiseAndSend(new Packet.CheckPacket(slotName, "Check Name"));
        SerialiseAndSend(new Packet.DeathLinkPacket(slotName, "Got beaten by dogs"));
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

    #region Item Handling
    void ProcessItem(Packet.ItemPacket itemPacket)
    {
        bool isID = itemPacket.isItemID;
        string itemName = itemPacket.itemName;

        logging.SendChatMessage($"Item Received: {itemName}");
        
            if (itemName.Contains("Trap"))
            {
                logging.SendChatMessage("Contains the word trap, going to handle it as a custom item");
                HandleCustomAPTrap(itemName);
            }
            else if (itemName.Contains("Blueprint"))
            {
                logging.SendChatMessage("Contains the word Blueprint, going to handle it as a custom item");
                HandleCustomAPBlueprint(itemName);
            }
            else if (itemName.Contains("Progressive"))
            {
                logging.SendChatMessage("Contains the word Progressive, going to handle it as a custom item");
                HandleCustomAPProgressiveStat(itemName);
            }
            else if (itemName.Contains("Prison Unlock"))
            {
                logging.SendChatMessage("Contains the phrase Prison Unlock, going to handle it as a custom item");
                HandleCustomAPPrisonUnlock(itemName);
            }
            else if (itemName.Contains("Coins"))
            {
                logging.SendChatMessage("Contains the word Coins, going to add it to the users wallet");
                moneyToAdd += 50;
            }
            else
            {
                logging.SendChatMessage("Not triggered any of the catches, going to treat as a vanilla game item");
                HandleGameItem(itemName);
            }
    }

    void TriggerLockdown(bool isStealthlink = false)
    {
        RoutineManager rm = RoutineManager.GetInstance();
        if (rm != null && !rm.IsLockdownActive())
        {
            SolitaryManager.GetInstance().TriggerLockdown();

            if (isStealthlink)
            {
                logging.SendChatMessage($"[StealthLink] {stealthLinkCulprit} got caught! PRISON LOCKDOWN INITIATED!");
            }
            else
            {
                logging.SendChatMessage("Lockdown Trap Triggered!");
            }

            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            if (localPlayer != null) AkSoundEngine.PostEvent("Play_UI_Error", localPlayer.gameObject);
        }
    }

    void SearchPlayersDesk()
    {
        List<DeskInteraction> playerDesks = DeskInteraction.GetPlayerDesks();

        if (playerDesks == null || playerDesks.Count == 0)
        {
            return;
        }

        List<AICharacter_Guard> m_ShakeDownGuards = new List<AICharacter_Guard>();

        List<AICharacter_Guard> guards = new List<AICharacter_Guard>(FindObjectsOfType<AICharacter_Guard>());

        if (guards.Count == 0)
        {
            return;
        }

        for (int i = 0; i < guards.Count; i++)
        {
            if (guards[i].m_RollCallStatus == AICharacter_Guard.RollCallStatus.DoesShakeDown)
            {
                m_ShakeDownGuards.Add(guards[i]);
            }
        }

        if (m_ShakeDownGuards.Count == 0)
        {
            return;
        }

        int guardIndex = 0;

        for (int i = 0; i < playerDesks.Count; i++)
        {
            DeskEventManager component = playerDesks[i].GetComponent<DeskEventManager>();
            if (component == null)
            {
                continue;
            }

            AIEvent investigateObjectEvent = component.GetInvestigateObjectEvent();
            if (investigateObjectEvent == null)
            {
                continue;
            }

            m_ShakeDownGuards[guardIndex].AddEvent(investigateObjectEvent);

            guardIndex++;
            if (guardIndex >= m_ShakeDownGuards.Count)
            {
                guardIndex = 0;
            }
        }
    }

    void HandleCustomAPTrap(string customItem)
        {
            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            if (localPlayer == null || localPlayer.m_CharacterStats == null) return;
 
            switch (customItem)
            {
                case "Max Heat Trap":
                {
                    localPlayer.m_CharacterStats.IncreaseHeat(100.0f);
                    instance.logging.SendChatMessage("TRAP TRIGGERED! Heat maxed to 100!");
                    AkSoundEngine.PostEvent("Play_UI_Error", localPlayer.gameObject);

                    break;
                }
                case "Cell Search Trap":
                {
                    SearchPlayersDesk();
                    break;
                }
                case "Lockdown Trap":
                {
                    TriggerLockdown(false);
                    break;
                }
                case "Butterfingers Trap":
                {
                    //TODO: THIS CURRENTLY DROPS ONE OF THE ITEM BUT LETS THE PLAYER KEEP IT
                    Item equippedItem = localPlayer.GetEquippedItem();
                    if (equippedItem != null)
                    {
                        localPlayer.m_ItemContainer.RemoveItemRPC(equippedItem, false, RPC_CallContexts.All);
                        equippedItem.transform.position = localPlayer.transform.position;
                        LevelScript.GetInstance().m_LevelItemContainer
                            .AddItemRPC(equippedItem, false, RPC_CallContexts.All);
                        if (equippedItem.MeshRendererProp != null) equippedItem.MeshRendererProp.enabled = true;
                        if (equippedItem.BoxColliderProp != null) equippedItem.BoxColliderProp.enabled = true;
                        equippedItem.AddToCullingSystem();
                        equippedItem.ItemDropped();
                        instance.logging.SendChatMessage("TRAP TRIGGERED! Butterfingers!");
                    }
                    break;
                }
                default:
                    // if (customID >= 91000 && customID <= 92000)
                    // {
                    //     int targetItemID = customID - 91000;
                    // unlockedBlueprints.Add(targetItemID);
                    //     logging.SendChatMessage($"BLUEPRINT ACQUIRED! You can now craft Item ID: {targetItemID}!");
                    //     AkSoundEngine.PostEvent("Play_UI_Take_Item", localPlayer.gameObject);
                    // }
                    logging.SendChatMessage($"Not a trap but item has been recognised as {customItem}");
                    break;
            }
        }

    

    void HandleCustomAPBlueprint(string blueprintName)
    {
        if (blueprintsReceived == null)
        {
            blueprintsReceived = new List<int>();
        }
        
        string cutItemName = blueprintName.Remove(0, 11);
        instance.logging.SendChatMessage($"Reduced {blueprintName} to {cutItemName}");

        int recipeID = References.GetRecipeIDFromName(cutItemName);

        if (recipeID == -1)
        {
            instance.logging.SendChatMessage("Couldn't find the blueprint reference in the dictionary. Aborting. (NOTIFY BIGCELERY842906 IF YOU SEE THIS");
            return;
        }
        
        blueprintsReceived.Add(recipeID);
    }

    void HandleCustomAPProgressiveStat(string itemName)
    {
        switch (itemName)
        {
            case "Progressive Strength":
            {
                strength_prog_count++;
                break;
            }

            case "Progressive Stamina":
            {
                stamina_prog_count++;
                break;
            }

            case "Progressive Intellect":
            {
                intellect_prog_count++;
                break;
            }
            default:
            {
                Logger.LogError($"Item Received but it doesn't match a progressive item. Item Name: {itemName}");
                return;
            }
        }
        
        SetStats();

    }

   

    void HandleCustomAPPrisonUnlock(string itemName)
    {
        if (prisonUnlocksReceived == null)
        {
            prisonUnlocksReceived = new List<string>();
        }
        
        prisonUnlocksReceived.Add(itemName);
    }

    void HandleGameItem(string gameItem)
    {
        instance.logging.SendChatMessage($"Handle Game Item Function Entered for {gameItem}");
        int itemID = References.GetItemIDFromName(gameItem);
        instance.logging.SendChatMessage($"Returned an id: {itemID} from {gameItem}");
        if (itemID == -1)
        {
            instance.logging.SendChatMessage("No Vanilla Game Item found, please notify BigCelery842906 if you see this!");
            return;
        }
        
        itemQueue.Enqueue(itemID);

    }

    bool inGame =  false;
    Queue<int> itemQueue = new Queue<int>();
    int moneyToAdd = 0;
    void DequeueItemQueue()
    {
        if (inGame)
        {
            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            
            localPlayer.m_CharacterStats.Money += moneyToAdd;
            moneyToAdd = 0;
            
            int playerNetID = localPlayer.m_NetView.viewID;
            int dummyRequestID = 0;


            if (itemQueue.Count == 0 || itemQueue == null)
            {
                return;
            }
            for (int i = 0; i < itemQueue.Count; i++)
            {
                int itemID = itemQueue.Dequeue();

                ItemManager itemManager = ItemManager.GetInstance();

                if (itemManager != null)
                {
                    itemManager.AssignItemRPC(playerNetID, itemID, OnItemReceivedFromAP, ref dummyRequestID, -1);
                    instance.logging.SendChatMessage($"Item {itemID} should have been given to the player - DequeueItemQueue");
                }
            }
        }
    }
    
    public static void OnItemReceivedFromAP(Item item, int eventID)
    {
        if (item == null) return;
        Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
        if (localPlayer != null && localPlayer.m_ItemContainer != null)
        {
            bool successfullyAdded = localPlayer.m_ItemContainer.AddItemRPC(item, false, RPC_CallContexts.All);
            if (successfullyAdded)
            {
                string safeName = string.IsNullOrEmpty(item.m_ItemData.name) ? "Archipelago Item" : item.m_ItemData.name;
                instance.logging.SendChatMessage($"Received {safeName}!");
                AkSoundEngine.PostEvent("Play_UI_Take_Item", localPlayer.gameObject);
                instance.logging.SendChatMessage($"Item {safeName} should have been given to the player - OnItemReceivedFromAP");
            }
            else
            {
                item.transform.position = localPlayer.transform.position;
                LevelScript.GetInstance().m_LevelItemContainer.AddItemRPC(item, false, RPC_CallContexts.All);
                if (item.MeshRendererProp != null) item.MeshRendererProp.enabled = true;
                if (item.BoxColliderProp != null) item.BoxColliderProp.enabled = true;
                item.AddToCullingSystem();
                item.ItemDropped();
                instance.logging.SendChatMessage($"Inventory full! Dropped {item.m_ItemData.name} at your feet.");
                AkSoundEngine.PostEvent("Play_UI_Drop_Item", localPlayer.gameObject);
            }
        }
    }
    
    #endregion Item Handling

    #region Game's Code - Largely from GoofTarune
    
    #region GoofCode
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
        
        #endregion //Config Stuff
        
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
    #endregion Goof's Code
    
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
                    killerName = characterResponsible.m_CharacterCustomisation.m_DisplayName;
                }

                string deathMessage = "";
                Random rand = new Random();
                int deathMessageIndex = rand.Next(0, 4);
                switch (deathMessageIndex)
                {
                    case 0:
                        deathMessage = $"{slotName} was beaten to a pulp by {killerName}!";
                        break;
                    case 1:
                        deathMessage = $"{slotName} picked a fight against {killerName} and lost!";
                        break;
                    case 2:
                        deathMessage = $"{slotName} overestimated their fighting ability";
                        break;
                    case 3:
                        deathMessage = $"{slotName} found their limit";
                        break;
                }
 
                Packet.DeathLinkPacket deathLink = new Packet.DeathLinkPacket(slotName, deathMessage);
                instance.SerialiseAndSend(deathLink);
            }
        }
    }
    
    #region StealthLink
    
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
    
    #endregion //Stealthlink
    
    #region ScheduleScrambler
    // [HarmonyPatch(typeof(RoutineManager), "StartInit")]
    //     public class RoutineManager_StartInit_Patch
    //     {
    //         // store the vanilla schedule here so there is a clean slate to shuffle from every morning
    //         public static List<Routines> vanillaBaseTypes = new List<Routines>();
    //         public static List<RoutineSubTypes> vanillaSubTypes = new List<RoutineSubTypes>();
    //         public static List<Events> vanillaMusic = new List<Events>();
    //         public static List<string> vanillaUITags = new List<string>();
    //
    //         static void Postfix(RoutineManager __instance)
    //         {
    //             // Fully updated to use the Enum instead of strings
    //             // if (!configEnabled.Value || configRandSchedule.Value == ScheduleMode.Off) return;
    //             // if (__instance.m_RoutinesData == null || __instance.m_RoutinesData.m_Routines == null || __instance.m_RoutinesData.m_Routines.Count <= 1) return;
    //
    //             // 1. Save the vanilla data immediately on load
    //             vanillaBaseTypes.Clear();
    //             vanillaSubTypes.Clear();
    //             vanillaMusic.Clear();
    //             vanillaUITags.Clear();
    //
    //             foreach (var routine in __instance.m_RoutinesData.m_Routines)
    //             {
    //                 vanillaBaseTypes.Add(routine.m_BaseRoutineType);
    //                 vanillaSubTypes.Add(routine.m_SubRoutineType);
    //                 vanillaMusic.Add(routine.m_RoutineMusic);
    //                 vanillaUITags.Add(routine.m_LocalizationTag);
    //             }
    //
    //             // 2. Hook into the daily engine trigger so it scrambles again at midnight
    //             __instance.OnDayChange += () => ScrambleDailySchedule(__instance);
    //
    //             // 3. Do the initial scramble for Day 1
    //             ScrambleDailySchedule(__instance);
    //         }
    //
    //         public static void ScrambleDailySchedule(RoutineManager rm)
    //         {
    //             // Fully updated to use the Enum instead of strings
    //             // ScheduleMode mode = configRandSchedule.Value;
    //             ScheduleMode mode = ScheduleMode.Chaos;
    //             if (mode == ScheduleMode.Off) return;
    //
    //             var routines = rm.m_RoutinesData.m_Routines;
    //             List<int> indicesToShuffle = new List<int>();
    //
    //             // First, restore everything to vanilla, then pick which blocks are allowed to shuffle
    //             for (int i = 0; i < routines.Count; i++)
    //             {
    //                 routines[i].m_BaseRoutineType = vanillaBaseTypes[i];
    //                 routines[i].m_SubRoutineType = vanillaSubTypes[i];
    //                 routines[i].m_RoutineMusic = vanillaMusic[i];
    //                 routines[i].m_LocalizationTag = vanillaUITags[i];
    //
    //                 // If we are in "Safe" mode, DO NOT add Lights Out to the shuffle pool. Please leave it alone.
    //                 if (mode == ScheduleMode.Safe && vanillaBaseTypes[i] == Routines.LightsOut) continue;
    //
    //                 indicesToShuffle.Add(i);
    //             }
    //
    //             // Extract the data ONLY for the blocks it's allowed to shuffle
    //             List<Routines> shuffleBase = new List<Routines>();
    //             List<RoutineSubTypes> shuffleSub = new List<RoutineSubTypes>();
    //             List<Events> shuffleMusic = new List<Events>();
    //             List<string> shuffleUI = new List<string>();
    //
    //             foreach (int i in indicesToShuffle)
    //             {
    //                 shuffleBase.Add(vanillaBaseTypes[i]);
    //                 shuffleSub.Add(vanillaSubTypes[i]);
    //                 shuffleMusic.Add(vanillaMusic[i]);
    //                 shuffleUI.Add(vanillaUITags[i]);
    //             }
    //
    //             // RNG seed based on Roulette Seed + Current In-Game Day (So it's different every day)
    //             string seedStr = configRouletteSeed.Value.Trim().ToLower();
    //             int seed = 12345;
    //             if (seedStr != "random" && !string.IsNullOrEmpty(seedStr)) int.TryParse(seedStr, out seed);
    //
    //             System.Random rng = new System.Random(seed + rm.GetDaysElapsed());
    //
    //             // Fisher-Yates Shuffle
    //             int n = shuffleBase.Count;
    //             while (n > 1)
    //             {
    //                 n--;
    //                 int k = rng.Next(n + 1);
    //
    //                 var tb = shuffleBase[k]; shuffleBase[k] = shuffleBase[n]; shuffleBase[n] = tb;
    //                 var ts = shuffleSub[k]; shuffleSub[k] = shuffleSub[n]; shuffleSub[n] = ts;
    //                 var tm = shuffleMusic[k]; shuffleMusic[k] = shuffleMusic[n]; shuffleMusic[n] = tm;
    //                 var tu = shuffleUI[k]; shuffleUI[k] = shuffleUI[n]; shuffleUI[n] = tu;
    //             }
    //
    //             // Inject the scrambled data back into their original time slots
    //             for (int i = 0; i < indicesToShuffle.Count; i++)
    //             {
    //                 int realIndex = indicesToShuffle[i];
    //                 routines[realIndex].m_BaseRoutineType = shuffleBase[i];
    //                 routines[realIndex].m_SubRoutineType = shuffleSub[i];
    //                 routines[realIndex].m_RoutineMusic = shuffleMusic[i];
    //                 routines[realIndex].m_LocalizationTag = shuffleUI[i];
    //             }
    //
    //             Plugin.instance.Logger.LogWarning($"[CHAOS] Day {rm.GetDaysElapsed()} schedule scrambled!");
    //         }
    //     }
    //     
    #endregion //Schedule Scrambler - Commented Currently for testing. Add in post release
        
    #region PayPhone Hints
    
    // 1. REWRITE THE BUTTON TEXT AND HIDE VANILLA HINTS
    [HarmonyPatch(typeof(PayphoneMenu), "ResetButtons")]
    public class PayphoneMenu_ResetButtons_Patch
    {
        static void Postfix(PayphoneMenu __instance)
        {
            if (!configEnabled.Value || !isConnected) return;
 
            FieldInfo buttonsField = typeof(PayphoneMenu).GetField("m_HintButtons", BindingFlags.NonPublic | BindingFlags.Instance);
            if (buttonsField == null) return;
 
            Array hintButtons = (Array)buttonsField.GetValue(__instance);
            if (hintButtons != null && hintButtons.Length > 0)
            {
                for (int i = 0; i < hintButtons.Length; i++)
                {
                    object hintButtonObj = hintButtons.GetValue(i);
                    Component hintButtonComp = (Component)hintButtonObj;
 
                    if (i == 0)
                    {
                        FieldInfo textField = hintButtonObj.GetType().GetField("m_Text", BindingFlags.Public | BindingFlags.Instance);
                        if (textField != null)
                        {
                            object t17Text = textField.GetValue(hintButtonObj);
                            if (t17Text != null)
                            {
                                // Turn off translation tags and overwrite the string
                                FieldInfo locField = t17Text.GetType().GetField("m_bNeedsLocalization", BindingFlags.Public | BindingFlags.Instance);
                                PropertyInfo textProp = t17Text.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
 
                                if (locField != null) locField.SetValue(t17Text, false);
                                if (textProp != null) textProp.SetValue(t17Text, "[TICON=Coin] 50 (AP HINT)", null);
                            }
                        }
                    }
                    else
                    {
                        // Hide all the other vanilla buttons so they don't clutter the UI
                        hintButtonComp.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
 
    // 2. OVERWRITE THE DESCRIPTION TEXT AT THE BOTTOM
    [HarmonyPatch(typeof(PayphoneMenu), "ShowHintDescription")]
    public class PayphoneMenu_ShowHintDescription_Patch
    {
        static void Postfix(PayphoneMenu __instance)
        {
            if (!configEnabled.Value || !isConnected) return;
 
            if (__instance.m_HintVagueDescription != null)
            {
                // Clear the vanilla hint text and put in my custom description
                __instance.m_HintVagueDescription.SetNewLocalizationTag(string.Empty);
                __instance.m_HintVagueDescription.SetNewPlaceHolder("Contact the Archipelago Server to reveal the location of a random progression item.");
            }
        }
    }
 
    // 3. GREY OUT THE BUTTON IF WE ARE BROKE
    [HarmonyPatch(typeof(PayphoneMenu), "CheckMoneyForHints")]
    public class PayphoneMenu_CheckMoneyForHints_Patch
    {
        static void Postfix(PayphoneMenu __instance)
        {
            if (!configEnabled.Value || !isConnected) return;
 
            FieldInfo buttonsField = typeof(PayphoneMenu).GetField("m_HintButtons", BindingFlags.NonPublic | BindingFlags.Instance);
            if (buttonsField == null) return;
 
            Array hintButtons = (Array)buttonsField.GetValue(__instance);
            if (hintButtons != null && hintButtons.Length > 0)
            {
                object firstButtonObj = hintButtons.GetValue(0);
                FieldInfo btnField = firstButtonObj.GetType().GetField("m_Button", BindingFlags.Public | BindingFlags.Instance);
                if (btnField != null)
                {
                    MonoBehaviour btnObj = (MonoBehaviour)btnField.GetValue(firstButtonObj);
                    Player player = Gamer.GetPrimaryGamer()?.m_PlayerObject;
 
                    if (btnObj != null && player != null && player.m_CharacterStats != null)
                    {
                        // Set the button to clickable ONLY if the player has $50 or more
                        btnObj.enabled = (player.m_CharacterStats.Money >= 50f);
                    }
                }
            }
        }
    }
 
    // 4. THE TRANSACTION 
    [HarmonyPatch(typeof(PayphoneMenu), "HintClicked")]
    public class PayphoneMenu_HintClicked_Patch
    {
        //TODO: Replace (IT'S ALL WRONG)
        // A master list of the most important progression blueprints and their ingame IDs (Mostly just placeholder for now)
        public static Dictionary<string, int> blueprintNamesToID = new Dictionary<string, int>()
        {
            { "Blueprint: Fake Audio Equipment", 182 },
            { "Blueprint: Civilian Clothes", 136 },
            { "Blueprint: Fake Carrot", 322 },
            { "Blueprint: Grappling Hook", 210 },
            { "Blueprint: Complete Crossbow", 140 },
            { "Blueprint: Makeshift Rocket Thruster", 268 },
            { "Blueprint: Makeshift Ladder", 265 },
            { "Blueprint: Makeshift Breathing Apparatus", 259 },
            { "Blueprint: Bed Dummy", 113 },
            { "Blueprint: Guard Outfit", 426 },
            { "Blueprint: Security Pass", 343 },
            { "Blueprint: Plastic Red Key", 313 },
            { "Blueprint: Plastic Cyan Key", 307 },
            { "Blueprint: Key Mould Red", 420 },
            { "Blueprint: Key Mould Cyan", 417 },
            { "Blueprint: Makeshift Harness", 264 },
            { "Blueprint: Parachute", 300 },
            { "Blueprint: Lightweight Cutters", 249 }
        };

        static bool Prefix(PayphoneMenu __instance, int index)
        {
            if (!configEnabled.Value || !isConnected) return true;

            Player player = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            if (player != null && player.m_CharacterStats != null)
            {
                int hintCost = 50;

                if (player.m_CharacterStats.Money >= hintCost)
                {
                    // 1. Figure out what the player actually needs
                    List<string> availableHints = new List<string>();
                    foreach (var kvp in blueprintNamesToID)
                    {
                        // If the ID is NOT in our unlocked blueprints, it's still needed
                        if (!unlockedBlueprints.Contains(kvp.Value))
                        {
                            availableHints.Add(kvp.Key);
                        }
                    }

                    if (availableHints.Count > 0)
                    {

                        string chosenHint = availableHints[UnityEngine.Random.Range(0, availableHints.Count)];


                        player.m_CharacterStats.DecreaseMoney(hintCost);
                        AkSoundEngine.PostEvent("Play_UI_Payphone_Purchase", __instance.gameObject);


                        if (isConnected)
                        {
                            //TODO: Replace this with hint scouting, accept the hint packet, and then funnel it back into this somehow without running into a null error
                            // var sayPacket = new Archipelago.MultiClient.Net.Packets.SayPacket()
                            //     { Text = $"!hint {chosenHint}" };
                            Plugin.instance.logging.SendChatMessage(
                                $"You dropped ${hintCost} into the payphone to locate: {chosenHint.Replace("Blueprint: ", "")}...");
                        }
                    }
                    else
                    {
                        // They already have all the main blueprints
                        Plugin.instance.logging.SendChatMessage(
                            "The multiverse has nothing left to teach you! (Stop trying to waste your money)");
                        AkSoundEngine.PostEvent("Play_UI_Error", __instance.gameObject);
                    }
                }
                else
                {
                    AkSoundEngine.PostEvent("Play_UI_Unavailable", __instance.gameObject);
                    Plugin.instance.logging.SendChatMessage($"You need ${hintCost} to phone the multiverse!");
                }

                // Force the screen to close and unlock the player's movement
                __instance.Hide();
                player.RequestStopInteraction();
            }

            // Return false to shred the vanilla logic
            return false;
        }
    }

    #endregion //Payphone Hints
    
    #region DeskInteractions

    public static string GetCurrentPrisonScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            string sceneName = SceneManager.GetSceneAt(i).name;
            if (sceneName != "Global" && sceneName != "RewiredInputSettings" && sceneName != "loading" &&
                sceneName != "Hud" && sceneName != "InGameMenus" && sceneName != "FrontEnd")
            {
                return sceneName;
            }
        }
        return "Unknown";
    }
    
    [HarmonyPatch(typeof(DeskInteraction), "TryOpenDesk")]
    public class DeskInteraction_TryOpenDesk_Patch
    {
        public static HashSet<int> checkedDesks = new HashSet<int>();

        public static Dictionary<string, long> deskLocationIDs = new Dictionary<string, long>()
        {
            // === CENTER PERKS (Centre_Perks) ===
            { "Centre_Perks|Desk_R_01 (1) (-25.5, 26.5, 0.0)", 81001 }, { "Centre_Perks|Desk_R_01 (2) (-19.5, 26.5, 0.0)", 81002 },
            { "Centre_Perks|Desk_R_01 (3) (-13.5, 26.5, 0.0)", 81003 }, { "Centre_Perks|Desk_R_01 (4) (-7.5, 26.5, 0.0)", 81004 },
            { "Centre_Perks|Desk_R_01 (5) (2.5, 26.5, 0.0)", 81005 },   { "Centre_Perks|Desk_R_01 (6) (8.5, 26.5, 0.0)", 81006 },
            { "Centre_Perks|Desk_R_01 (7) (14.5, 26.5, 0.0)", 81007 },  { "Centre_Perks|Desk_R_01 (8) (20.5, 26.5, 0.0)", 81008 },
            { "Centre_Perks|Desk_D_01 (1) (-4.5, 19.5, 0.0)", 81009 },  { "Centre_Perks|Desk_D_01 (2) (3.5, 19.5, 0.0)", 81010 },
            { "Centre_Perks|Desk_D_01 (3) (-4.5, 13.5, 0.0)", 81011 },  { "Centre_Perks|Desk_D_01 (4) (3.5, 13.5, 0.0)", 81012 },
            { "Centre_Perks|Desk_D_01 (5) (-4.5, 3.5, 0.0)", 81013 },   { "Centre_Perks|Desk_D_01 (6) (3.5, 3.5, 0.0)", 81014 },
            { "Centre_Perks|Desk_D_01 (7) (3.5, -2.5, 0.0)", 81015 },   { "Centre_Perks|Desk_D_01 (8) (-4.5, -2.5, 0.0)", 81016 },
            { "Centre_Perks|Desk_L_01 (-21.5, -13.5, 0.0)", 81017 },    { "Centre_Perks|Desk_L_01 (1) (-15.5, -13.5, 0.0)", 81018 },
            { "Centre_Perks|Desk_L_01 (2) (-9.5, -13.5, 0.0)", 81019 }, { "Centre_Perks|Desk_L_01 (3) (-3.5, -13.5, 0.0)", 81020 },
            { "Centre_Perks|Desk_L_01 (4) (6.5, -13.5, 0.0)", 81021 },  { "Centre_Perks|Desk_L_01 (5) (12.5, -13.5, 0.0)", 81022 },
            { "Centre_Perks|Desk_L_01 (6) (18.5, -13.5, 0.0)", 81023 }, { "Centre_Perks|Desk_L_01 (7) (24.5, -13.5, 0.0)", 81024 },
            { "Centre_Perks|Cutlery_Tray_Down (-32.5, 10.5, 0.0)", 81025 },
            { "Centre_Perks|Desk_D_01 (1) (-8.5, -38.5, 0.0)", 81026 },
            { "Centre_Perks|Desk_D_01 (4) (-17.5, 37.5, 0.0)", 81027 },
            { "Centre_Perks|Desk_D_01 (-16.5, -38.5, 0.0)", 81028 },
            { "Centre_Perks|Desk_D_01 (33.5, 3.5, 0.0)", 81029 },
            { "Centre_Perks|Desk_D_01 (9) (-41.5, -1.5, 0.0)", 81030 },
            { "Centre_Perks|Desk_D_01 (2) (-7.5, 28.5, -6.0)", 81031 },
            { "Centre_Perks|Desk_D_01 (1) (41.5, 26.5, -6.0)", 81032 },
            { "Centre_Perks|Desk_D_01 (1) (6.5, -11.5, -6.0)", 81033 },
            { "Centre_Perks|Desk_D_01 (3) (-19.5, 27.5, -6.0)", 81034 },
            { "Centre_Perks|Desk_L_01 (-38.5, 1.5, -6.0)", 81035 },
            { "Centre_Perks|Desk_D_01 (-0.5, 30.5, -12.0)", 81037 },
            
            // === RATTLESNAKE SPRINGS (OldWestFort) ===
            { "OldWestFort|Desk_D_01 (31.5, 9.5, 0.0)", 82001 },
            { "OldWestFort|Desk_U_01 (-28.5, 5.5, 0.0)", 82002 },
            { "OldWestFort|Desk_D_01 (8) (21.5, 14.5, -6.0)", 82003 },
            { "OldWestFort|Desk_U_01 (-29.5, -12.5, 0.0)", 82004 },
            { "OldWestFort|Desk_R_01 (1) (-15.5, 38.5, 0.0)", 82005 },
            { "OldWestFort|Desk_D_01 (11.5, 14.5, -6.0)", 82006 },
            { "OldWestFort|Desk_U_01 (-29.5, -18.5, 0.0)", 82007 },
            { "OldWestFort|Cutlery_Tray_Down (29.5, -23.5, 0.0)", 82008 },
            { "OldWestFort|Desk_D_01 (6) (-4.5, -28.5, -6.0)", 82009 },
            { "OldWestFort|Desk_U_01 (24.5, 5.5, 0.0)", 82010 },
            { "OldWestFort|Desk_R_01 (-15.5, 37.5, 0.0)", 82011 },
            { "OldWestFort|Desk_U_01 (-29.5, -24.5, 0.0)", 82012 },
            { "OldWestFort|Desk_D_01 (10) (-10.5, 43.5, -6.0)", 82013 },
            { "OldWestFort|Desk_D_01 (26.5, 45.5, -6.0)", 82014 },
            { "OldWestFort|Desk_U_01 (1) (-33.5, 25.5, 0.0)", 82015 },
            { "OldWestFort|Desk_R_01 (-21.5, -31.5, -6.0)", 82016 },
            { "OldWestFort|Desk_R_01 (-33.5, 14.5, -6.0)", 82017 },
            { "OldWestFort|Desk_D_01 (-28.5, -2.5, 0.0)", 82018 },
            { "OldWestFort|Desk_D_01 (13) (16.5, 12.5, -12.0)", 82019 },
            { "OldWestFort|Desk_D_01 (11) (3.5, 48.5, -6.0)", 82020 },
            { "OldWestFort|Desk_D_01 (-29.5, 9.5, 0.0)", 82021 },
            { "OldWestFort|Desk_D_01 (37.5, 26.5, 0.0)", 82022 },
            { "OldWestFort|Desk_D_01 (-30.5, 3.5, 0.0)", 82023 },
            { "OldWestFort|Desk_D_01 (-30.5, -20.5, 0.0)", 82024 },
            { "OldWestFort|Desk_D_01 (-18.5, 43.5, -6.0)", 82025 },
            { "OldWestFort|Desk_D_01 (5) (14.5, -30.5, -6.0)", 82026 },
            { "OldWestFort|Desk_D_01 (2) (11.5, 1.5, -6.0)", 82027 },
            { "OldWestFort|Desk_D_01 (3) (-25.5, 25.5, -6.0)", 82028 },
            { "OldWestFort|Desk_D_01 (-30.5, -8.5, 0.0)", 82029 },
            { "OldWestFort|Desk_U_01 (-29.5, -0.5, 0.0)", 82030 },
            { "OldWestFort|Desk_D_01 (12) (2.5, 49.5, -12.0)", 82031 },
            { "OldWestFort|Desk_D_01 (9) (0.5, -0.5, -6.0)", 82032 },
            { "OldWestFort|Desk_D_01 (5.5, 27.5, 3.0)", 82033 },
            { "OldWestFort|Desk_D_01 (14) (28.5, 6.5, -12.0)", 82034 },
            { "OldWestFort|Desk_D_01 (-29.5, -14.5, 0.0)", 82035 },
            { "OldWestFort|Desk_D_01 (1) (34.5, 20.5, -6.0)", 82036 },
            { "OldWestFort|Desk_D_01 (4) (-4.5, -42.5, -6.0)", 82037 },
            { "OldWestFort|Desk_U_01 (-29.5, -6.5, 0.0)", 82038 },
 
            // === K.A.P.O.W. CAMP (POW_Camp) ===
            { "POW_Camp|Desk_U_01 (1) (-14.5, -21.5, 0.0)", 83001 },
            { "POW_Camp|Desk_D_01 (-18.5, -15.5, 0.0)", 83002 },
            { "POW_Camp|Desk_D_01 (4) (-18.5, 8.5, 0.0)", 83003 },
            { "POW_Camp|Desk_D_01 (20.5, 26.5, -6.0)", 83004 },
            { "POW_Camp|Desk_U_01 (7) (-18.5, 14.5, 0.0)", 83005 },
            { "POW_Camp|Desk_D_01 (1) (21.5, 25.5, 0.0)", 83006 },
            { "POW_Camp|Desk_D_01 (2) (-14.5, -3.5, 0.0)", 83007 },
            { "POW_Camp|Desk_U_01 (-16.5, 30.5, 0.0)", 83008 },
            { "POW_Camp|Desk_R_01 (9.5, 12.5, -6.0)", 83009 },
            { "POW_Camp|Desk_D_01 (-8.5, 35.5, 0.0)", 83010 },
            { "POW_Camp|Desk_D_01 (39.5, 0.5, -6.0)", 83011 },
            { "POW_Camp|Desk_U_01 (2) (-14.5, -9.5, 0.0)", 83012 },
            { "POW_Camp|Desk_D_01 (6) (-14.5, 20.5, 0.0)", 83013 },
            { "POW_Camp|Desk_D_01 (2) (-32.5, -30.4, 0.0)", 83014 },
            { "POW_Camp|Desk_D_01 (5) (-14.5, 8.5, 0.0)", 83015 },
            { "POW_Camp|Desk_U_01 (4) (-18.5, 2.5, 0.0)", 83016 },
            { "POW_Camp|Desk_D_01 (3) (11.5, -22.5, 0.0)", 83017 },
            { "POW_Camp|Desk_U_01 (6) (-14.5, 14.5, 0.0)", 83018 },
            { "POW_Camp|Desk_D_01 (1) (-14.5, -15.5, 0.0)", 83019 },
            { "POW_Camp|Desk_U_01 (-18.5, -21.5, 0.0)", 83020 },
            { "POW_Camp|Desk_D_01 (1) (-30.5, 34.5, 0.0)", 83021 },
            { "POW_Camp|Cutlery_Tray_Down (16.5, 6.5, 0.0)", 83022 },
            { "POW_Camp|Desk_D_01 (3) (-18.5, -3.5, 0.0)", 83023 },
            { "POW_Camp|Desk_U_01 (3) (-18.5, -9.5, 0.0)", 83024 },
            { "POW_Camp|Desk_D_01 (7) (-18.5, 20.5, 0.0)", 83025 },
            { "POW_Camp|Desk_D_01 (1) (21.5, 26.5, -6.0)", 83026 },
            { "POW_Camp|Desk_U_01 (5) (-14.5, 2.5, 0.0)", 83027 },
 
            
 
            // === HMP OFFSHORE (Oil_Rig) ===
            { "Oil_Rig|Desk_U_01 (3) (-18.5, -11.5, -18.0)", 84001 },
            { "Oil_Rig|Desk_U_01 (13) (-40.5, 19.5, -24.0)", 84002 },
            { "Oil_Rig|Desk_R_01 (0.5, 11.5, -24.0)", 84003 },
            { "Oil_Rig|Desk_U_01 (-44.5, 11.5, -3.0)", 84004 },
            { "Oil_Rig|Desk_U_01 (5) (-18.5, 0.5, -18.0)", 84005 },
            { "Oil_Rig|Desk_U_01 (10) (-18.5, -4.5, -24.0)", 84006 },
            { "Oil_Rig|Desk_U_01 (11) (-18.5, -10.5, -24.0)", 84007 },
            { "Oil_Rig|Desk_D_01 (-11.5, 41.5, -36.0)", 84008 },
            { "Oil_Rig|Desk_R_01 (1) (-43.5, 20.5, -12.0)", 84009 },
            { "Oil_Rig|Desk_D_01 (-40.5, 35.5, -24.0)", 84010 },
            { "Oil_Rig|Desk_R_01 (17.5, -34.5, -12.0)", 84011 },
            { "Oil_Rig|Desk_R_01 (2.5, 27.5, -18.0)", 84012 },
            { "Oil_Rig|Desk_D_01 (6.5, 13.5, -6.0)", 84013 },
            { "Oil_Rig|Desk_U_01 (1) (21.5, -26.5, -18.0)", 84014 },
            { "Oil_Rig|Desk_R_01 (-14.5, 29.5, -30.0)", 84015 },
            { "Oil_Rig|Desk_U_01 (2) (-23.5, -11.5, -18.0)", 84016 },
            { "Oil_Rig|Cutlery_Tray_Down (-19.6, 29.5, -24.0)", 84017 },
            { "Oil_Rig|Desk_U_01 (12) (-37.5, 4.5, -24.0)", 84018 },
            { "Oil_Rig|Desk_R_01 (-41.5, 41.5, -18.0)", 84019 },
            { "Oil_Rig|Desk_U_01 (7) (-23.5, -4.5, -24.0)", 84020 },
            { "Oil_Rig|Desk_U_01 (6) (-31.5, 32.5, -18.0)", 84021 },
            { "Oil_Rig|Desk_U_01 (21.5, -20.5, -18.0)", 84022 },
            { "Oil_Rig|Desk_U_01 (4) (-18.5, -5.5, -18.0)", 84023 },
            { "Oil_Rig|Desk_U_01 (8) (-23.5, -10.5, -24.0)", 84024 },
            { "Oil_Rig|Desk_D_01 (1.5, 30.5, -18.0)", 84025 },
            { "Oil_Rig|Desk_D_01 (30.5, -32.5, -12.0)", 84026 },
            { "Oil_Rig|Desk_U_01 (-41.5, 28.5, -24.0)", 84027 },
            { "Oil_Rig|Desk_R_01 (1) (-34.5, 37.5, -36.0)", 84028 },
            { "Oil_Rig|Desk_U_01 (9) (-18.5, 1.5, -24.0)", 84029 },
            { "Oil_Rig|Desk_U_01 (6) (-23.5, 1.5, -24.0)", 84030 },
            { "Oil_Rig|Desk_R_01 (-27.5, 38.5, -36.0)", 84031 },
            { "Oil_Rig|Desk_U_01 (1) (-23.5, -5.5, -18.0)", 84032 },
            { "Oil_Rig|Desk_U_01 (14) (7.5, 17.5, -24.0)", 84033 },
            { "Oil_Rig|Desk_U_01 (-23.5, 0.5, -18.0)", 84034 },
 
            // === FORT TUNDRA (Gulag_Prison) ===
            { "Gulag_Prison|Desk_L_01 (3) (14.5, 0.5, 0.0)", 85001 },
            { "Gulag_Prison|Desk_L_01 (5) (14.5, 6.5, 0.0)", 85002 },
            { "Gulag_Prison|Desk_L_01 (5) (14.5, 7.5, -6.0)", 85003 },
            { "Gulag_Prison|Desk_R_01 (5) (-21.5, 0.5, 0.0)", 85004 },
            { "Gulag_Prison|Desk_R_01 (-21.5, -15.5, 0.0)", 85005 },
            { "Gulag_Prison|Cutlery_Tray_Down (-21.5, -32.5, 0.0)", 85006 },
            { "Gulag_Prison|Desk_L_01 (4) (14.5, 3.5, 0.0)", 85007 },
            { "Gulag_Prison|Desk_D_01 (5.5, -23.5, -6.0)", 85008 },
            { "Gulag_Prison|Desk_R_01 (1) (-21.5, -12.5, 0.0)", 85009 },
            { "Gulag_Prison|Desk_L_01 (4) (14.5, 4.5, -6.0)", 85010 },
            { "Gulag_Prison|Desk_D_01 (1) (21.5, 25.5, 0.0)", 85011 },
            { "Gulag_Prison|Desk_R_01 (3) (-21.5, 7.5, -6.0)", 85012 },
            { "Gulag_Prison|Desk_L_01 (1) (14.5, -12.5, 0.0)", 85013 },
            { "Gulag_Prison|Desk_R_01 (4) (-21.5, 3.5, 0.0)", 85014 },
            { "Gulag_Prison|Desk_D_01 (-32.5, -2.5, 0.0)", 85015 },
            { "Gulag_Prison|Desk_D_01 (34.5, 0.5, 0.0)", 85016 },
            { "Gulag_Prison|Desk_R_01 (32.5, 28.5, 0.0)", 85017 },
            { "Gulag_Prison|Desk_D_01 (12.5, -35.5, 0.0)", 85018 },
            { "Gulag_Prison|Desk_R_01 (4) (-21.5, 4.5, -6.0)", 85019 },
            { "Gulag_Prison|Desk_R_01 (2) (-21.5, -9.5, 0.0)", 85020 },
            { "Gulag_Prison|Desk_R_01 (1) (-21.5, -11.5, -6.0)", 85021 },
            { "Gulag_Prison|Desk_D_01 (-27.5, 13.5, 0.0)", 85022 },
            { "Gulag_Prison|Desk_R_01 (2) (-21.5, -8.5, -6.0)", 85023 },
            { "Gulag_Prison|Desk_D_01 (1) (29.5, 43.5, 0.0)", 85024 },
            { "Gulag_Prison|Desk_L_01 (2) (14.5, -9.5, 0.0)", 85025 },
            { "Gulag_Prison|Desk_L_01 (2) (14.5, -8.5, -6.0)", 85026 },
            { "Gulag_Prison|Desk_D_01 (1) (20.5, -35.5, 0.0)", 85027 },
            { "Gulag_Prison|Desk_D_01 (1) (11.5, -23.5, -6.0)", 85028 },
            { "Gulag_Prison|Desk_L_01 (14.5, -14.5, -6.0)", 85029 },
            { "Gulag_Prison|Desk_R_01 (-21.5, -14.5, -6.0)", 85030 },
            { "Gulag_Prison|Desk_R_01 (5) (-21.5, 1.5, -6.0)", 85031 },
            { "Gulag_Prison|Desk_R_01 (3) (-21.5, 6.5, 0.0)", 85032 },
            { "Gulag_Prison|Desk_D_01 (2) (25.5, 13.5, -6.0)", 85033 },
            { "Gulag_Prison|Desk_L_01 (3) (14.5, 1.5, -6.0)", 85034 },
            { "Gulag_Prison|Desk_L_01 (14.5, -15.5, 0.0)", 85035 },
            { "Gulag_Prison|Desk_L_01 (1) (14.5, -11.5, -6.0)", 85036 },
 
            // === AREA 17 (Area_17) ===
            { "Area_17|Desk_U_01 (4) (-14.5, -17.5, 3.0)", 86001 },
            { "Area_17|Desk_R_01 (30.5, 26.5, -6.0)", 86002 },
            { "Area_17|Desk_U_01 (2) (27.5, 34.5, -6.0)", 86003 },
            { "Area_17|Desk_U_01 (3) (-36.5, 3.5, 3.0)", 86004 },
            { "Area_17|Desk_L_01 (34.5, 10.5, 3.0)", 86005 },
            { "Area_17|Desk_D_01 (3) (31.5, 0.5, -6.0)", 86006 },
            { "Area_17|Desk_D_01 (1) (2.5, -11.5, 3.0)", 86007 },
            { "Area_17|Desk_D_01 (2) (-2.5, 9.5, 0.0)", 86008 },
            { "Area_17|Desk_R_01 (30.5, 25.5, 0.0)", 86009 },
            { "Area_17|Desk_U_01_Lunar (-33.5, 15.6, 0.0)", 86010 },
            { "Area_17|Desk_L_01 (11.5, -13.5, 0.0)", 86011 },
            { "Area_17|Desk_U_01 (5) (38.5, 43.5, -6.0)", 86012 },
            { "Area_17|Cutlery_Tray_Down (-12.0, 26.9, 3.0)", 86013 },
            { "Area_17|Desk_L_01 (10.5, 26.5, -6.0)", 86014 },
            { "Area_17|Desk_D_01 (29.5, 4.5, 0.0)", 86015 },
            { "Area_17|Desk_U_01 (1) (13.5, 34.5, -6.0)", 86016 },
            { "Area_17|Desk_U_01 (1) (27.5, 33.5, 0.0)", 86017 },
            { "Area_17|Desk_D_01 (1) (33.5, 14.5, 0.0)", 86018 },
            { "Area_17|Desk_U_01 (6) (39.5, 21.5, 0.0)", 86019 },
            { "Area_17|Desk_U_01 (3) (29.5, -26.5, 3.0)", 86020 },
            { "Area_17|Desk_U_01 (13.5, 33.5, 0.0)", 86021 },
            { "Area_17|Desk_D_01 (33.5, -4.5, 0.0)", 86022 },
            { "Area_17|Desk_U_01 (1) (38.5, -3.5, 3.0)", 86023 },
            { "Area_17|Desk_D_01 (4) (4.5, 10.5, -6.0)", 86024 },
            { "Area_17|Desk_U_01 (2) (-21.5, -11.5, 3.0)", 86025 },
            { "Area_17|Desk_R_01 (-4.5, 10.5, 3.0)", 86026 },
            { "Area_17|Desk_U_01 (5) (22.5, 42.5, 0.0)", 86027 },
            { "Area_17|Desk_U_01 (6) (2.5, 21.5, -6.0)", 86028 },
            { "Area_17|Desk_U_01 (7) (1.5, -25.5, 0.0)", 86029 },
            { "Area_17|Desk_D_01 (1) (13.5, 13.5, -6.0)", 86030 },
            { "Area_17|Desk_U_01 (1) (44.5, 31.5, 3.0)", 86031 },
            { "Area_17|Desk_L_01 (10.5, 25.5, 0.0)", 86032 },
            { "Area_17|Desk_U_01 (2) (26.5, -27.5, 3.0)", 86033 },
 
          
            // === U.S.S. ANOMALY (Space Prison) ===
            { "Space Prison|Desk_D_01 (-12.5, 8.5, 0.0)", 87001 },
            { "Space Prison|Desk_U_01 (8) (6.5, -33.5, 0.0)", 87002 },
            { "Space Prison|Desk_U_01 (11) (-20.5, -33.5, 0.0)", 87003 },
            { "Space Prison|Desk_U_01 (1) (-14.5, -23.5, 0.0)", 87004 },
            { "Space Prison|Desk_D_01 (1) (-9.5, 29.5, 0.0)", 87005 },
            { "Space Prison|Desk_D_01 (40.5, -12.5, 0.0)", 87006 },
            { "Space Prison|Desk_U_01 (5) (18.5, -23.5, 0.0)", 87007 },
            { "Space Prison|Desk_U_01 (-20.5, -23.5, 0.0)", 87008 },
            { "Space Prison|Desk_D_01 (1) (45.5, 6.5, -6.0)", 87009 },
            { "Space Prison|Desk_D_01 (34.5, -12.5, 0.0)", 87010 },
            { "Space Prison|Desk_D_01 (4) (13.5, 25.5, -6.0)", 87011 },
            { "Space Prison|Desk_U_01 (3) (6.5, -23.5, 0.0)", 87012 },
            { "Space Prison|Desk_D_01 (1) (49.5, -27.5, -6.0)", 87013 },
            { "Space Prison|Desk_D_01 (-11.5, 43.5, -6.0)", 87014 },
            { "Space Prison|Desk_U_01 (2) (-8.5, -23.5, 0.0)", 87015 },
            { "Space Prison|Desk_U_01 (9) (-8.5, -33.5, 0.0)", 87016 },
            { "Space Prison|Desk_D_01 (36.5, -16.5, -0.3)", 87017 },
            { "Space Prison|Desk_U_01 (-6.5, 8.5, 0.0)", 87018 },
            { "Space Prison|Desk_U_01 (4) (12.5, -23.5, 0.0)", 87019 },
            { "Space Prison|Desk_U_01 (10) (-14.5, -33.5, 0.0)", 87020 },
            { "Space Prison|Desk_D_01 (2) (-35.5, -16.5, 0.0)", 87021 },
            { "Space Prison|Desk_U_01 (7) (12.5, -33.5, 0.0)", 87022 },
            { "Space Prison|Cutlery_Tray_Down (18.5, 5.3, 0.0)", 87023 },
            { "Space Prison|Desk_D_01 (6.5, 8.5, 0.0)", 87024 },
            { "Space Prison|Desk_D_01 (3) (-18.5, 11.5, -6.0)", 87025 },
            { "Space Prison|Desk_D_01 (2) (-35.5, 13.5, -6.0)", 87026 },
            { "Space Prison|Desk_U_01 (6) (18.5, -33.5, 0.0)", 87027 },
            { "Space Prison|Desk_D_01 (3) (45.5, 12.5, 0.0)", 87028 },
            
            
            // === COUGAR CREEK RAILROAD (Transport_Train) ===
            { "Transport_Train|Desk_R_01 (30.5, -1.5, 3.0)", 88001 },
            { "Transport_Train|Desk_D_01 (2) (-28.5, 0.5, 3.0)", 88002 },
            { "Transport_Train|Desk_D_01 (1) (-88.5, 0.5, 3.0)", 88003 },
            { "Transport_Train|Desk_U_01 (55.5, -3.5, 3.0)", 88004 },
            { "Transport_Train|Desk_U_01 (-11.5, -1.5, 3.0)", 88005 },
            { "Transport_Train|Desk_R_01 (80.5, -1.5, 3.0)", 88006 },
            { "Transport_Train|Desk_D_01 (-103.5, 0.5, 3.0)", 88007 },
            { "Transport_Train|Desk_D_01 (3) (7.5, 0.5, 3.0)", 88008 },
            
            // === H.M.S. ORCA (Transport_Boat) ===
            { "Transport_Boat|Desk_D_01 (12.5, 4.5, -3.0)", 89001 },
            { "Transport_Boat|Desk_U_01 (-9.5, -3.5, -3.0)", 89002 },
            { "Transport_Boat|Desk_R_01 (-20.5, 1.5, 0.0)", 89003 },
            { "Transport_Boat|Desk_R_01 (-12.5, -8.5, 0.0)", 89004 },
            { "Transport_Boat|Desk_D_01 (17.5, -0.5, 3.0)", 89005 },
            { "Transport_Boat|Desk_D_01 (9.5, -8.5, 3.0)", 89006 },
            { "Transport_Boat|Desk_U_01 (12.5, -4.5, 0.0)", 89007 },
            { "Transport_Boat|Desk_R_01 (-12.5, -7.5, -3.0)", 89008 },
            { "Transport_Boat|Desk_D_01 (3.5, -7.5, -3.0)", 89009 },
            { "Transport_Boat|Desk_R_01 (5.5, -4.5, -3.0)", 89010 },
            { "Transport_Boat|Desk_D_01 (-23.5, -7.5, 3.0)", 89011 },
            { "Transport_Boat|Desk_U_01 (-14.5, -3.5, 3.0)", 89012 },
            { "Transport_Boat|Desk_D_01 (9.5, 2.5, 3.0)", 89013 },
            { "Transport_Boat|Desk_U_01 (-5.5, -11.5, 0.0)", 89014 },
            { "Transport_Boat|Desk_D_01 (1) (24.5, -9.5, 3.0)", 89015 },
            
            // === AIR FORCE CON (Transport_Plane)
            { "Transport_Plane|Desk_R_01 (0.5, 16.5, -3.0)", 90001 },
            { "Transport_Plane|Desk_L_01 (2) (4.5, 9.5, 0.0)", 90002 },
            { "Transport_Plane|Desk_R_01 (1) (-4.5, 38.5, 0.0)", 90003 },
            { "Transport_Plane|Desk_D_01 (-3.5, 11.5, 3.0)", 90004 },
            { "Transport_Plane|Desk_L_01 (1) (4.5, -6.5, -3.0)", 90005 },
            { "Transport_Plane|Desk_L_01 (1) (1.5, -10.5, 0.0)", 90006 },
            { "Transport_Plane|Desk_L_01 (1) (4.5, -4.5, 3.0)", 90007 },
            { "Transport_Plane|Desk_U_01 (1) (1.5, -19.5, -3.0)", 90008 },
            { "Transport_Plane|Desk_U_01 (4.5, 3.5, -3.0)", 90009 },
            { "Transport_Plane|Desk_R_01 (-5.5, 28.5, -3.0)", 90010 },
            { "Transport_Plane|Desk_R_01 (-5.5, 1.5, 3.0)", 90011 },
            { "Transport_Plane|Desk_D_01 (-5.5, -1.5, -3.0)", 90012 },

        };
            
        static void Postfix(DeskInteraction __instance)
        {
            if (!configEnabled.Value || !isConnected) return;

            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            if (localPlayer != null &&
                Vector3.Distance(localPlayer.transform.position, __instance.transform.position) > 3.0f) return;

            string currentScene = GetCurrentPrisonScene();
            string uniqueDeskKey = $"{currentScene}|{__instance.gameObject.name} {__instance.transform.position}";
            int deskInstanceID = __instance.GetInstanceID();

            if (!checkedDesks.Contains(deskInstanceID))
            {
                if (deskLocationIDs.ContainsKey(uniqueDeskKey))
                {
                    long apLocationId = deskLocationIDs[uniqueDeskKey];

                    // Check if the AP Server actually generated this location
                    if (isConnected)
                    {
                        Plugin.instance.SendCheckToBridge(apLocationId.ToString(), true);
                        instance.logging.SendChatMessage($"Searched Desk! (Sent ID: {apLocationId})");
                    }
                    // else if (isConnected &&
                    //          APManager.Session.Locations.AllLocationsChecked.Contains(apLocationId))
                    // {
                    //     instance.logging.SendChatMessage("Desk already searched in previous session!");
                    // }
                    // else
                    // { 
                    //     instance.logging.SendChatMessage($"[ERROR] Desk ID {apLocationId} is NOT in your AP World!");
                    // }
                }
                else
                {
                    instance.Logger.LogWarning($"[AP UNMAPPED DESK] Missing desk! Copy this exact string: \"{uniqueDeskKey}\"");
                }

                checkedDesks.Add(deskInstanceID);
            }
        }

    }

    #endregion //Desk Checks
    
    #region Stat Handling
    
    private int strength_prog_count;
    private int stamina_prog_count;
    private int intellect_prog_count;

    private int strength_step;
    private int stamina_step;
    private int intellect_step;

    private int base_Stat = 30;

    private int synced_strength;
    private int synced_stamina;
    private int synced_intellect;
    
    void CalculateStats()
    {
        synced_strength = base_Stat + (strength_prog_count * strength_step);
        synced_stamina = base_Stat + (stamina_prog_count * stamina_step);
        synced_intellect = base_Stat + (intellect_prog_count * intellect_step);
    }

    void SetStats()
    {
        //TODO: SET THIS TO TRIGGER ON ANY PRISON LOADED
        CalculateStats();
        
        Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
        CharacterStats charStats = localPlayer?.m_CharacterStats;

        if (charStats == null)
        {return;}

        charStats.Strength = synced_strength;
        charStats.Cardio = synced_stamina;
        charStats.Intellect = synced_intellect;
    }


    [HarmonyPatch(typeof(CharacterStats), "IncreaseStrengthRPC")]
    public static class CharacterStats_IncreaseStrengthRPC_Patch
    {
        public static void Prefix(CharacterStats __instance, float amount)
        {
            instance.SendCheckToBridge($"Strength Stat: {(int)amount}");
            
             Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
             CharacterStats charStats = localPlayer?.m_CharacterStats;
            
             charStats.Strength = instance.synced_strength;
            
            return;
        }
    }

    [HarmonyPatch(typeof(CharacterStats), "IncreaseCardioRPC")]
    public static class CharacterStats_IncreaseCardioRPC_Patch
    {
        public static void Prefix(CharacterStats __instance, float amount)
        {
            instance.SendCheckToBridge($"Stamina Stat: {(int)amount}");
            
            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            CharacterStats charStats = localPlayer?.m_CharacterStats;
            
            charStats.Cardio = instance.synced_stamina;
            
            return;
        }
    }

    [HarmonyPatch(typeof(CharacterStats), "IncreaseIntellectRPC")]
    public static class CharacterStats_IncreaseIntellectRPC_Patch
    {
        public static void Prefix(CharacterStats __instance, float amount)
        {
            instance.SendCheckToBridge($"Intellect Stat: {(int)amount}");
            
            Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            CharacterStats charStats = localPlayer?.m_CharacterStats;
            
            charStats.Intellect = instance.synced_intellect;
            
            return;
        }
    }
    
    #endregion // Stat Handling
    
    #region PrisonLoading

    private GameObject KeysRequiredObject = null;
    T17Text KeysRequiredText = null;
    
    [HarmonyPatch(typeof(SaveSlotController), "UpdateEnabledState")]
    public static class SaveSlotController_UpdateEnabledState_Patch
    {
        public static void Postfix(SaveSlotController __instance, T17Button ___m_ButtonClass)
        {
            if (__instance == null)
                return;

            if (___m_ButtonClass == null)
                return;

            bool vanillaInteractable = ___m_ButtonClass.interactable;

            if (instance.KeysRequiredObject == null)
            {
                instance.logging.SendChatMessage("No Keys Required Object found, looking again");
                instance.KeysRequiredObject = GameObject.Find("FrontendParent/RootMenu/SlideBottom/SafeZone/MenuBody/CampaignMenu/PanelLeft/Column_01/PrisonSelect/PrisonPreviewImage/KeysRequired");
                return;
            }

            if (instance.KeysRequiredText == null)
            {
                instance.logging.SendChatMessage("No Keys Required Text found, looking again");
                instance.KeysRequiredText = instance.KeysRequiredObject.GetComponent<T17Text>();
                return;
            }

            if (!isConnected)
            {
                instance.StartPipes();
                
                instance.KeysRequiredObject.SetActive(true);
                instance.KeysRequiredText.text = "Connect to Archipelago via the Console Window!";
            }

            LevelScript.PRISON_ENUM prison_Enum = GlobalStart.GetInstance().GetCurrentSelectedPrisonData().m_LevelInfo.m_PrisonEnum;

            if (instance.prisonUnlocksReceived == null)
            { // Disable all buttons until connection
                ___m_ButtonClass.interactable = false;
                ___m_ButtonClass.m_bCanInteractWithMouse = false;
                return;
            }

            bool isPrisonUnlocked = false;

            switch (prison_Enum)
            {
                case LevelScript.PRISON_ENUM.Centre_Perks:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("Center Perks 2.0 Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.OldWestFort:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("Rattlesnake Springs Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.POW_Camp:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("K.A.P.O.W Camp Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.Oil_Rig:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("H.M.P. Offshore Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.Gulag_Prison:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("Fort Tundra Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.Area_17:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("Area 17 Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.Space_Prison:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("U.S.S. Anomaly Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.Transport_Train:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("Cougar Creek Railroad Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.Transport_Boat:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("H.M.S. Orca Prison Unlock");
                    break;
                case LevelScript.PRISON_ENUM.Transport_Plane:
                    isPrisonUnlocked = instance.prisonUnlocksReceived.Contains("Air Force Con Prison Unlock");
                    break;
                default:
                    break;
            }

            if (!isPrisonUnlocked)
            {
                ___m_ButtonClass.interactable = isPrisonUnlocked;
                ___m_ButtonClass.m_bCanInteractWithMouse = isPrisonUnlocked;
                
                instance.KeysRequiredObject.SetActive(true);
                instance.KeysRequiredText.text = "Prison Unlock not received yet!";
            }
            else
            {
                instance.KeysRequiredObject.SetActive(true);
                instance.KeysRequiredText.text = "Prison Unlock Received!";
                
                if (__instance.m_Controlling == SaveSlotController.ControllingWhat.NewGame)
                {
                    ___m_ButtonClass.interactable = isPrisonUnlocked;
                    ___m_ButtonClass.m_bCanInteractWithMouse = isPrisonUnlocked;
                }
                else
                {
                    ___m_ButtonClass.interactable = vanillaInteractable;
                    ___m_ButtonClass.m_bCanInteractWithMouse = vanillaInteractable;
                }
            }
        }
    }

    void DoPlayerStats()
    {
        instance.logging.SendChatMessage("Doing the player stats now!!!");
        instance.SetStats();
        instance.inGame = true;
    }

    [HarmonyPatch(typeof(PlayerSelectMenu), "OnAppearanceSelected")]
    public static class PlayerSelectMenu_OnAppearanceSelected_Patch
    {
        public static void Postfix(PlayerSelectMenu __instance)
        {
            instance.DoPlayerStats();
        }
    }
    
    #endregion //Prison Loading
    
    #region Escape Checks
    
     [HarmonyPatch(typeof(EscapePrisonFunctionality), "RPC_EscapePrison")]
    public class EscapePrisonFunctionality_RPC_EscapePrison_Patch
    {
        static void Prefix(EscapePrisonFunctionality __instance, EscapeMethod escapeMethod)
        {
            
            // instance.Logger.LogWarning($"[ESCAPE DETECT] Scene: {currentScene}, Engine Method: {escapeMethod.ToString()}");

            switch (escapeMethod)
            {
                case EscapeMethod.NothingSpecial: //PERIMETER BREAKOUT
                {
                    string currentScene = GetCurrentPrisonScene();

                    string prison = "";
                    switch (currentScene)
                    {
                        case "Centre_Perks": // Center Perks 2.0
                        {
                            prison = "Center Perks 2.0";
                            break;
                        }
                        case "OldWestFort":   // Rattlesnake Springs
                        {
                            prison = "Rattlesnake Springs";
                            break;
                        }
                        case "POW_Camp": // KAPOW Camp
                        {
                            prison = "K.A.P.O.W Camp";
                            break;
                        }
                        case "Oil_Rig": // HMP Offshore
                        {
                            prison = "H.M.P. Offshore";
                            break;
                        }
                        case "Gulag_Prison": // Fort Tundra
                        {
                            prison = "Fort Tundra";
                            break;
                        }
                        case "Area_17": // Area 17
                        {
                            prison = "Area 17";
                            break;
                        }
                        case "Space Prison": // USS Anomaly
                        {
                            prison = "U.S.S. Anomaly";
                            break;
                        }
                    }

                    if (prison == "")
                    {
                        instance.logging.SendChatMessage("Prison Escape Check failed. Please report this to BigCelery842906 along with which escape you tried to complete.");
                        return;
                    }
                    
                    instance.SendCheckToBridge($"Escape: Perimeter Breakout ({prison})");

                    break;
                }

                case EscapeMethod.Documentary:
                    instance.SendCheckToBridge("Escape: Meet the Crew (Center Perks 2.0)");
                    break;
                case EscapeMethod.Zipline:
                    instance.SendCheckToBridge("Escape: Zip It Up (Rattlesnake Springs)");
                    break;
                case EscapeMethod.Motorcycle:
                    instance.SendCheckToBridge("Escape: Speed McQueen (K.A.P.O.W Camp)");
                    break;
                case EscapeMethod.SingleplayerTrashBag:
                    instance.SendCheckToBridge("Escape: Trash Talk  (H.M.P. Offshore)");
                    break;
                case EscapeMethod.Dolphin:
                    instance.SendCheckToBridge("Escape: Swimming With Dolphins  (H.M.P. Offshore)");
                    break;
                case EscapeMethod.Sewers:
                    instance.SendCheckToBridge("Escape: Rock-hammer Hard Place (Fort Tundra)");
                    break;
                case EscapeMethod.HumanDisguise:
                    instance.SendCheckToBridge("Escape: I'm Only Human (Area 17)");
                    break;
                case EscapeMethod.Jetpack:
                    instance.SendCheckToBridge("Escape: Race From Space (U.S.S. Anomaly)");
                    break;
                
                //TRANSPORT
                case EscapeMethod.HorseEscape:
                    instance.SendCheckToBridge("Escape: My Little Phoney (Cougar Creek Railroad)");
                    break;
                case EscapeMethod.GrappleGantry: //TODO: TEST THIS ONE
                    instance.SendCheckToBridge("Escape: Hooked On You (Cougar Creek Railroad)");
                    break;
                case EscapeMethod.ScubaGear:
                    instance.SendCheckToBridge("Escape: Scuba Doo (H.M.S. Orca)");
                    break;
                case EscapeMethod.StolenJetski:
                    instance.SendCheckToBridge("Escape: Wave Goodbye (H.M.S. Orca)");
                    break;
                case EscapeMethod.Parachute:
                    instance.SendCheckToBridge( "Escape: Plane Crazy (Air Force Con)");
                    break;
                case EscapeMethod.ForceEmergencyLanding:
                    instance.SendCheckToBridge("Escape: Passport To Freedom (Air Force Con)");
                    break;
                
                default:
                    instance.logging.SendChatMessage("Prison Escape Check failed. Please report this to BigCelery842906 along with which escape you tried to complete.");
                    break;
            }
        }
    }
    
    #endregion //Escape Checks

    #region Job Checks

    [HarmonyPatch(typeof(BaseJob), "QuotaAchieved", MethodType.Setter)]
    public class BaseJob_QuotaAchieved_Patch
    {
        static void Postfix(BaseJob __instance)
        {
            if (__instance.NormalizedProgress >= 1)
            {
                if (__instance.Employee.m_CharacterStats.m_bIsPlayer)
                {
                    JobType type = __instance.m_Type;

                    string jobType = type.ToString();

                    switch (type)
                    {
                        case JobType.Electrician:
                            jobType = "Engineering";
                            break;
                        case JobType.WasteDisposal:
                            jobType = "Waste Disposal";
                            break;
                        case JobType.MailSorting:
                            jobType = "Mail Sorting";
                            break;
                        case JobType.CanineCarer:
                            jobType = "Canine Carer";
                            break;
                            
                    }
                    
                    instance.SendCheckToBridge($"Job: {jobType}");
                }
            }
        }
    } 
    
    #endregion //Job Checks
    
    #region Crafting

    [HarmonyPatch(typeof(CraftManager), "GetIntellectRequiredForRecipe")]
    public class CraftManager_GetIntellectRequiredForRecipe_Patch
    {
        static void Postfix(CraftManager __instance, int recipeID, ref int __result)
        {
            instance.logging.SendChatMessage($"Recipe id trying to send is: {recipeID}");
            if (References.apworldBlueprints.ContainsValue(recipeID))
            {
                instance.logging.SendChatMessage("Recipe ID found in the apworld blueprints");

                if (!instance.blueprintsReceived.Contains(recipeID))
                {
                    instance.logging.SendChatMessage("Found the blueprint in the received list");
                    __result = 101;
                }
                else
                {
                    instance.logging.SendChatMessage("Did not find the blueprint in the received list");
                }
            }
            else
            {
                instance.logging.SendChatMessage("Recipe ID not found in the apworld blueprints");
            }
            
        }
    }
    
    #endregion //Crafting
    #endregion //Harmony Patches

    #endregion //Game's Code

}