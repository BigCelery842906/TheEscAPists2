using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using BC_Dual_Ref_Library;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace TheEscAPists2;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("TheEscapists2.exe")] //Set Specifically for the Escapists 2
public class Plugin : BaseUnityPlugin
{
    private static Plugin instance;

    private Logging logging;

    private Process bridgeProcess;
    private NamedPipeClientStream pipeClientForBridge;
    private NamedPipeClientStream pipeClientForGame;
    private StreamReader reader;
    private StreamWriter writer;

    private static string slotName;
    public static bool isConnected;

    public bool pendingDeathLink;
    public static string lastDeathLinkReason = "";

    public bool pendingStealthLink;
    public string stealthLinkCulprit = "";
    public static float lastStealthLinkTime;

    private List<string> prisonUnlocksReceived;
    private List<int[]> blueprintsReceived;

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

        logging = new Logging(Logger);
        StartBridge();
        StartPipes();
        

        Harmony harmony = new Harmony("com.bigcelery842906.escapists2.archipelago.randomiser");
        harmony.PatchAll();
        Logger.LogInfo("V0.0.1 - Pre-Initial Release");

        GlobalStart.EnteredLevelEvent += DoPlayerStats;
        GlobalStart.EndLevelEvent += InGameFalse;
    }

    void InGameFalse()
    {
        inGame = false;
    }

    private void StartBridge()
    {
        if ((bridgeProcess != null && !bridgeProcess.HasExited) ||
            Process.GetProcessesByName("TheEscAPists2Bridge").Length > 0)
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

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c start \"The EscAPists 2 Bridge\" \"{bridgePath}\"",
            WorkingDirectory = Path.GetDirectoryName(bridgePath),
            UseShellExecute = true,
            CreateNoWindow = false,
        };

        bridgeProcess = Process.Start(startInfo);

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
            pipeClientForBridge = null;
            return;
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
                Logger.LogInfo("Waiting for line....");
                string message = reader.ReadLine();

                if (message == null)
                {
                    Logger.LogWarning("Bridge disconnected. Please restart both the bridge and the game.");
                    isConnected = false;
                    ClosePipes();
                    
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
            Logger.LogInfo("Bridge disconnected. Holding Pipe for reconnect.");
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
                break;
            }
            case PacketType.Item:
            {
                Packet.ItemPacket itemPacket = (Packet.ItemPacket)message;

                ProcessItem(itemPacket);
                break;
            }
            case PacketType.SlotData:
            {
                Packet.SlotDataPacket slotDataPacket = (Packet.SlotDataPacket)message;

                strength_step = slotDataPacket.strength_step;
                stamina_step = slotDataPacket.stamina_step;
                intellect_step = slotDataPacket.intellect_step;

                Logger.LogInfo($"Strength Step: {strength_step},  Stamina Step: {stamina_step}, Intellect Step: {intellect_step}");

                break;
            }
            default:
            {
                Logger.LogWarning("Unknown message type: " + message.Type);
                break;
            }
        }
    }

    private bool isDeskDetecting = false;
    private Player localPlayer;
    private void Update()
    {
        if (inGame)
        {
            if (pendingDeathLink) ProcessDeathLink();

            if (pendingStealthLink)
            {
                pendingStealthLink = false;

                lastStealthLinkTime = Time.time;

                TriggerLockdown(true);
            }

            if (itemQueue.Count != 0 || moneyToAdd != 0)
            {
                DequeueItemQueue();
            }

            if (trapQueue.Count != 0)
            {
                HandleCustomAPTrap();
            }
            
            // if (Input.GetKeyDown(KeyCode.F4))
            // {
            //     isDeskDetecting = !isDeskDetecting;
            //     localPlayer = Gamer.GetPrimaryGamer().m_PlayerObject;
            //     localPlayer.m_RigidBody.detectCollisions = !isDeskDetecting;
            // }
            //
            // if (Input.GetKeyDown(KeyCode.F5))
            // {
            //     Vector3 transform = localPlayer.m_Transform.position;
            //     logging.SendChatMessage($"Transform: {transform.x}, {transform.y}, {transform.z}");
            // }
            //
            // if (Input.GetKeyDown(KeyCode.F6))
            // {
            //     localPlayer.m_RigidBody.detectCollisions = !localPlayer.m_RigidBody.detectCollisions;
            // }
            //
            // if (isDeskDetecting)
            // {
            //     Vector3 transform = localPlayer.m_Transform.position;
            //     logging.SendChatMessage($"Transform: {transform.x}, {transform.y}, {transform.z}");
            //     
            // }
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

    void ClosePipes()
    {
        if (pipeClientForBridge.IsConnected)
        {
            pipeClientForBridge.Dispose();
            // pipeClientForBridge.Close();
            pipeClientForBridge = null;
            
            
        }
        if (pipeClientForGame.IsConnected)
        {
            pipeClientForGame.Dispose();
            // pipeClientForGame.Close();
            pipeClientForGame = null;
        }
    }
    void KillEverything()
    {
        bridgeProcess?.Kill();

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

    void SendPayphoneHintToBridge()
    {
        Packet.HintPacket hintPacket = new Packet.HintPacket(Packet.HintPacket.TypeOfHint.Payphone);
        SerialiseAndSend(hintPacket);
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
        
        Logger.LogInfo($"Item Received: {itemName}");

        if (itemName.Contains("Trap"))
        {
            Logger.LogInfo("Contains the word trap, going to handle it as a custom item");
            trapQueue.Enqueue(itemName);
        }
        else if (itemName.Contains("Blueprint"))
        {
            Logger.LogInfo("Contains the word Blueprint, going to handle it as a custom item");
            HandleCustomAPBlueprint(itemName);
        }
        else if (itemName.Contains("Progressive"))
        {
            Logger.LogInfo("Contains the word Progressive, going to handle it as a custom item");
            HandleCustomAPProgressiveStat(itemName);
        }
        else if (itemName.Contains("Prison Unlock"))
        {
            Logger.LogInfo("Contains the phrase Prison Unlock, going to handle it as a custom item");
            HandleCustomAPPrisonUnlock(itemName);
        }
        else if (itemName.Contains("Coins"))
        {
            Logger.LogInfo("Contains the word Coins, going to add it to the users wallet");
            moneyToAdd += 50;
        }
        else
        {
            Logger.LogInfo("Not triggered any of the catches, going to treat as a vanilla game item");
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

    void HandleCustomAPTrap()
    {
        if (trapQueue.Count == 0) return;
        
        string customItem = trapQueue.Dequeue();
        
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
                instance.logging.SendChatMessage("Cell Search Trap Triggered!");
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
                Item equippedItem = localPlayer.GetEquippedItem();
                if (equippedItem != null)
                {
                    localPlayer.DropEquipedItem(localPlayer.m_Transform.position);
                    instance.logging.SendChatMessage("TRAP TRIGGERED! Butterfingers!");
                    
                }

                break;
            }
            default:
                Logger.LogInfo($"Not a trap but item has been recognised as {customItem}");
                break;
        }
    }



    void HandleCustomAPBlueprint(string blueprintName)
    {
        if (blueprintsReceived == null)
        {
            blueprintsReceived = new List<int[]>();
        }

        string cutBlueprintName = blueprintName.Remove(0, 11);
        instance.Logger.LogInfo($"Reduced {blueprintName} to {cutBlueprintName}");

        int[] recipeIngredients = References.GetRecipeIngredientsFromName(cutBlueprintName);

        if (recipeIngredients == (int[])[-1, -1, -1])
        {
            instance.logging.SendChatMessage("Couldn't find the blueprint reference in the dictionary. Aborting. (NOTIFY BIGCELERY842906 IF YOU SEE THIS");
            Logger.LogError($"Tried to get: {blueprintName} ({cutBlueprintName}). Ingredients got from that are:{recipeIngredients[0]}, {recipeIngredients[1]}, {recipeIngredients[2]}");
            foreach (int[] blueprint in blueprintsReceived)
            {
                string stringToLog = "";
                for (int i = 0; i < blueprint.Length; i++)
                {
                    stringToLog += blueprint[i] + ", ";
                }
                Logger.LogError(stringToLog);
            }

            return;
        }

        blueprintsReceived.Add(recipeIngredients);
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
        instance.Logger.LogInfo($"Handle Game Item Function Entered for {gameItem}");
        int itemID = References.GetItemIDFromName(gameItem);
        instance.Logger.LogInfo($"Returned an id: {itemID} from {gameItem}");
        if (itemID == -1)
        {
            instance.logging.SendChatMessage("No Vanilla Game Item found, please notify BigCelery842906 if you see this!");
            Logger.LogError($"Tried to get: {gameItem}");
            return;
        }

        itemQueue.Enqueue(itemID);

    }

    bool inGame = false;
    Queue<int> itemQueue = new Queue<int>();
    int moneyToAdd = 0;
    Queue<string> trapQueue = new Queue<string>();

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
                    instance.Logger.LogInfo($"Item {itemID} should have been given to the player - DequeueItemQueue");
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
                string safeName = string.IsNullOrEmpty(item.m_ItemData.name)
                    ? "Archipelago Item"
                    : item.m_ItemData.name;
                instance.Logger.LogInfo($"Received {safeName}!");
                AkSoundEngine.PostEvent("Play_UI_Take_Item", localPlayer.gameObject);
                instance.Logger.LogInfo($"Item {safeName} should have been given to the player - OnItemReceivedFromAP");
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

    #region Game's Code
    private void ProcessDeathLink()
    {
        Player localPlayer = Gamer.GetPrimaryGamer()?.m_PlayerObject;
        if (localPlayer != null && localPlayer.m_CharacterStats != null)
        {
            PropertyInfo healthProp =
                typeof(CharacterStats).GetProperty("Health", BindingFlags.Public | BindingFlags.Instance)
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
    
    #region Harmony Patches

    [HarmonyPatch(typeof(Player), "SetIsKnockedOut")]
    public class Player_SetIsKnockedOut_Patch
    {
        public static float lastDeathLinkTime = 0f;

        static void Postfix(Player __instance, bool knockedOut, Character characterResponsible)
        {
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
            FieldInfo buttonsField =
                typeof(PayphoneMenu).GetField("m_HintButtons", BindingFlags.NonPublic | BindingFlags.Instance);
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
                        FieldInfo textField = hintButtonObj.GetType()
                            .GetField("m_Text", BindingFlags.Public | BindingFlags.Instance);
                        if (textField != null)
                        {
                            object t17Text = textField.GetValue(hintButtonObj);
                            if (t17Text != null)
                            {
                                // Turn off translation tags and overwrite the string
                                FieldInfo locField = t17Text.GetType().GetField("m_bNeedsLocalization",
                                    BindingFlags.Public | BindingFlags.Instance);
                                PropertyInfo textProp = t17Text.GetType()
                                    .GetProperty("text", BindingFlags.Public | BindingFlags.Instance);

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
            if (__instance.m_HintVagueDescription != null)
            {
                // Clear the vanilla hint text and put in my custom description
                __instance.m_HintVagueDescription.SetNewLocalizationTag(string.Empty);
                __instance.m_HintVagueDescription.SetNewPlaceHolder(
                    "Contact the Archipelago Server to reveal the location of a random progression item.");
            }
        }
    }

    // 3. GREY OUT THE BUTTON IF WE ARE BROKE
    [HarmonyPatch(typeof(PayphoneMenu), "CheckMoneyForHints")]
    public class PayphoneMenu_CheckMoneyForHints_Patch
    {
        static void Postfix(PayphoneMenu __instance)
        {
            FieldInfo buttonsField =
                typeof(PayphoneMenu).GetField("m_HintButtons", BindingFlags.NonPublic | BindingFlags.Instance);
            if (buttonsField == null) return;

            Array hintButtons = (Array)buttonsField.GetValue(__instance);
            if (hintButtons != null && hintButtons.Length > 0)
            {
                object firstButtonObj = hintButtons.GetValue(0);
                FieldInfo btnField = firstButtonObj.GetType()
                    .GetField("m_Button", BindingFlags.Public | BindingFlags.Instance);
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
        static bool Prefix(PayphoneMenu __instance, int index)
        {
            Player player = Gamer.GetPrimaryGamer()?.m_PlayerObject;
            if (player != null && player.m_CharacterStats != null)
            {
                int hintCost = 50;

                if (player.m_CharacterStats.Money >= hintCost)
                {
                    if (isConnected)
                    {
                        player.m_CharacterStats.DecreaseMoney(hintCost);
                        AkSoundEngine.PostEvent("Play_UI_Payphone_Purchase", __instance.gameObject);
                        //TODO: Replace this with hint scouting, accept the hint packet, and then funnel it back into this somehow without running into a null error
                        // var sayPacket = new Archipelago.MultiClient.Net.Packets.SayPacket()
                        //     { Text = $"!hint {chosenHint}" };

                        instance.SendPayphoneHintToBridge();
                        instance.logging.SendChatMessage($"You dropped ${hintCost} into the payphone and rang the Multiworld...");
                    }
                }
                else
                {
                    AkSoundEngine.PostEvent("Play_UI_Unavailable", __instance.gameObject);
                    instance.logging.SendChatMessage($"You need ${hintCost} to phone the multiverse!");
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

    public static HashSet<int> checkedDesks = new HashSet<int>();

    //Guard Outfit - 40 Intellect and Blueprint
    ////Infirmary Outfit - 50 Intellect
    //Red Key - Blueprints and 60 Intellect
    //Cyan Key - Blueprints and 60 Intellect
    public static Dictionary<string, long> deskLocationIDs = new Dictionary<string, long>()
    {
        // === CENTER PERKS (Centre_Perks) ===
        { "Centre_Perks|Desk_R_01 (1) (-25.5, 26.5, 0.0)", 81001 }, //Top Row Cell 1
        { "Centre_Perks|Desk_R_01 (2) (-19.5, 26.5, 0.0)", 81002 }, //Top Row Cell 2
        { "Centre_Perks|Desk_R_01 (3) (-13.5, 26.5, 0.0)", 81003 }, //Top Row Cell 3
        { "Centre_Perks|Desk_R_01 (4) (-7.5, 26.5, 0.0)", 81004 }, //Top Row Cell 4
        { "Centre_Perks|Desk_R_01 (5) (2.5, 26.5, 0.0)", 81005 }, //Top Row Cell 5
        { "Centre_Perks|Desk_R_01 (6) (8.5, 26.5, 0.0)", 81006 }, //Top Row Cell 6
        { "Centre_Perks|Desk_R_01 (7) (14.5, 26.5, 0.0)", 81007 }, //Top Row Cell 7
        { "Centre_Perks|Desk_R_01 (8) (20.5, 26.5, 0.0)", 81008 }, //Top Row Cell 8

        { "Centre_Perks|Desk_D_01 (1) (-4.5, 19.5, 0.0)", 81009 }, //Middle Row Cell 1
        { "Centre_Perks|Desk_D_01 (2) (3.5, 19.5, 0.0)", 81010 }, //Middle Row Cell 2
        { "Centre_Perks|Desk_D_01 (3) (-4.5, 13.5, 0.0)", 81011 }, //Middle Row Cell 3
        { "Centre_Perks|Desk_D_01 (4) (3.5, 13.5, 0.0)", 81012 }, //Middle Row Cell 4
        { "Centre_Perks|Desk_D_01 (5) (-4.5, 3.5, 0.0)", 81013 }, //Middle Row Cell 5
        { "Centre_Perks|Desk_D_01 (6) (3.5, 3.5, 0.0)", 81014 }, //Middle Row Cell 6
        { "Centre_Perks|Desk_D_01 (8) (-4.5, -2.5, 0.0)", 81015 }, //Middle Row Cell 7
        { "Centre_Perks|Desk_D_01 (7) (3.5, -2.5, 0.0)", 81016 }, //Middle Row Cell 8

        { "Centre_Perks|Desk_L_01 (-21.5, -13.5, 0.0)", 81017 }, //Bottom Row Cell 1
        { "Centre_Perks|Desk_L_01 (1) (-15.5, -13.5, 0.0)", 81018 }, //Bottom Row Cell 2
        { "Centre_Perks|Desk_L_01 (2) (-9.5, -13.5, 0.0)", 81019 }, //Bottom Row Cell 3
        { "Centre_Perks|Desk_L_01 (3) (-3.5, -13.5, 0.0)", 81020 }, //Bottom Row Cell 4
        { "Centre_Perks|Desk_L_01 (4) (6.5, -13.5, 0.0)", 81021 }, //Bottom Row Cell 5
        { "Centre_Perks|Desk_L_01 (5) (12.5, -13.5, 0.0)", 81022 }, //Bottom Row Cell 6
        { "Centre_Perks|Desk_L_01 (6) (18.5, -13.5, 0.0)", 81023 }, //Bottom Row Cell 7
        { "Centre_Perks|Desk_L_01 (7) (24.5, -13.5, 0.0)", 81024 }, //Bottom Row Cell 8


        { "Centre_Perks|Desk_D_01 (4) (-17.5, 37.5, 0.0)", 81025 }, //Desk next to car park
        { "Centre_Perks|Desk_D_01 (2) (-7.5, 28.5, -6.0)", 81026 }, //Room above gym Floor 1
        { "Centre_Perks|Desk_D_01 (1) (6.5, -11.5, -6.0)", 81027 }, //Classroom Desk Floor 1
        { "Centre_Perks|Desk_D_01 (3) (-19.5, 27.5, -6.0)", 81028 }, //Meet the Crew Desk

        { "Centre_Perks|Desk_L_01 (-38.5, 1.5, -6.0)", 81029 }, //Medic Desk Floor 1 (Need Infirmary Outfit)

        {
            "Centre_Perks|Desk_D_01 (9) (-41.5, -1.5, 0.0)", 81030
        }, //Maintenance Desk Floor 0 next to infirmary (Cyan Door)
        {
            "Centre_Perks|Desk_D_01 (1) (41.5, 26.5, -6.0)", 81031
        }, //Maintenance Desk Floor 1 Right Hand Side (Cyan Door)

        { "Centre_Perks|Desk_D_01 (-0.5, 30.5, -12.0)", 81032 }, //Center North on Roof (Red Door)
        { "Centre_Perks|Desk_D_01 (-16.5, -38.5, 0.0)", 81033 }, //Guard Cell 1 (Red Door)
        { "Centre_Perks|Desk_D_01 (1) (-8.5, -38.5, 0.0)", 81034 }, //Guard Cell 3 (Red Door)

        { "Centre_Perks|Desk_D_01 (33.5, 3.5, 0.0)", 81035 }, //Contraband Desk (Red Door & Guard outfit)
        { "Centre_Perks|Cutlery_Tray_Down (-32.5, 10.5, 0.0)", 81036 }, //Cutlery Tray

        //ALSO A MULTIPLAYER DESK NOT MAPPED

        // === RATTLESNAKE SPRINGS (OldWestFort) ===
        { "OldWestFort|Desk_D_01 (-29.5, 9.5, 0.0)", 82001 }, //Cell 1
        { "OldWestFort|Desk_U_01 (-28.5, 5.5, 0.0)", 82002 }, //Cell 2
        { "OldWestFort|Desk_D_01 (-30.5, 3.5, 0.0)", 82003 }, //Cell 3
        { "OldWestFort|Desk_U_01 (-29.5, -0.5, 0.0)", 82004 }, //Cell 4
        { "OldWestFort|Desk_D_01 (-28.5, -2.5, 0.0)", 82005 }, //Cell 5
        { "OldWestFort|Desk_U_01 (-29.5, -6.5, 0.0)", 82006 }, //Cell 6
        { "OldWestFort|Desk_D_01 (-30.5, -8.5, 0.0)", 82007 }, //Cell 7
        { "OldWestFort|Desk_U_01 (-29.5, -12.5, 0.0)", 82008 }, //Cell 8
        { "OldWestFort|Desk_D_01 (-29.5, -14.5, 0.0)", 82009 }, //Cell 9
        { "OldWestFort|Desk_U_01 (-29.5, -18.5, 0.0)", 82010 }, //Cell 10
        { "OldWestFort|Desk_D_01 (-30.5, -20.5, 0.0)", 82011 }, //Cell 11
        { "OldWestFort|Desk_U_01 (-29.5, -24.5, 0.0)", 82012 }, //Cell 12

        { "OldWestFort|Desk_D_01 (6) (-4.5, -28.5, -6.0)", 820014 }, //Bottom Left Hallway Floor 2
        { "OldWestFort|Desk_R_01 (-21.5, -31.5, -6.0)", 82015 }, //Down From Cells Floor 2
        { "OldWestFort|Desk_D_01 (5) (14.5, -30.5, -6.0)", 82016 }, //Floor 2 Bottom Right
        { "OldWestFort|Desk_D_01 (2) (11.5, 1.5, -6.0)", 82017 }, //Floor 2 Left From Infirmary
        { "OldWestFort|Desk_D_01 (9) (0.5, -0.5, -6.0)", 82018 }, //Floor 2 Left of Central Stairs
        { "OldWestFort|Desk_D_01 (4) (-4.5, -42.5, -6.0)", 82019 }, //Floor 2 Bottom Left
        { "OldWestFort|Desk_D_01 (8) (21.5, 14.5, -6.0)", 82020 }, //Floor 3 Up From Infirmary
        { "OldWestFort|Desk_D_01 (13) (16.5, 12.5, -12.0)", 82021 }, //Floor 3 Next to stairs
        { "OldWestFort|Desk_D_01 (14) (28.5, 6.5, -12.0)", 82022 }, //Floor 3 Room Right of stairs

        { "OldWestFort|Desk_U_01 (24.5, 5.5, 0.0)", 82023 }, //Medic Desk (Infirmary Outfit)

        { "OldWestFort|Desk_D_01 (31.5, 9.5, 0.0)", 82024 }, //Maintenance Floor 1 (Cyan Door)
        { "OldWestFort|Desk_D_01 (11.5, 14.5, -6.0)", 82025 }, //Maintenance Desk Floor 2 (Cyan Door)

        { "OldWestFort|Desk_U_01 (1) (-33.5, 25.5, 0.0)", 82026 }, //Generator Room Desk (Red Door)
        { "OldWestFort|Desk_R_01 (-15.5, 37.5, 0.0)", 82027 }, //Bottom Guard Desk Floor 1 (Red Door)
        { "OldWestFort|Desk_R_01 (1) (-15.5, 38.5, 0.0)", 82028 }, //Floor 1 Top Guard Desk (Red Door)
        { "OldWestFort|Desk_D_01 (10) (-10.5, 43.5, -6.0)", 82029 }, //Central Guard Desk Floor 2 (Red Door)
        { "OldWestFort|Desk_D_01 (26.5, 45.5, -6.0)", 82030 }, //Floor 2 Right Guard Desk (Red Door)
        { "OldWestFort|Desk_R_01 (-33.5, 14.5, -6.0)", 82031 }, //Floor 2 Right of Red Door (Red Door)
        { "OldWestFort|Desk_D_01 (11) (3.5, 48.5, -6.0)", 82032 }, //Floor 2 Top Guard Desk (Red Door)
        { "OldWestFort|Desk_D_01 (-18.5, 43.5, -6.0)", 82033 }, //Floor 2 Left Guard Desk (Red Door)
        { "OldWestFort|Desk_D_01 (3) (-25.5, 25.5, -6.0)", 82034 }, //Floor 2 Up from Red Door (Red Door)
        { "OldWestFort|Desk_D_01 (12) (2.5, 49.5, -12.0)", 82035 }, //Floor 3 Guard Desk (Red Door)

        { "OldWestFort|Desk_D_01 (37.5, 26.5, 0.0)", 82036 }, //Contraband Desk (Red Door)
        { "OldWestFort|Cutlery_Tray_Down (29.5, -23.5, 0.0)", 82037 }, //Cutlery Tray


        // { "OldWestFort|Desk_D_01 (5.5, 27.5, 3.0)", 82037 },                //Multiplayer
        // { "OldWestFort|Desk_D_01 (1) (34.5, 20.5, -6.0)", 82038 },          //Multiplayer

        // === K.A.P.O.W. CAMP (POW_Camp) ===
        { "POW_Camp|Desk_D_01 (7) (-18.5, 20.5, 0.0)", 83001 }, //Cell 1 Desk 1
        { "POW_Camp|Desk_D_01 (6) (-14.5, 20.5, 0.0)", 83002 }, //Cell 1 Desk 2
        { "POW_Camp|Desk_U_01 (7) (-18.5, 14.5, 0.0)", 83003 }, //Cell 1 Desk 3
        { "POW_Camp|Desk_U_01 (6) (-14.5, 14.5, 0.0)", 83004 }, //Cell 1 Desk 4

        { "POW_Camp|Desk_D_01 (4) (-18.5, 8.5, 0.0)", 83005 }, //Cell 2 Desk 1
        { "POW_Camp|Desk_D_01 (5) (-14.5, 8.5, 0.0)", 83006 }, //Cell 2 Desk 2
        { "POW_Camp|Desk_U_01 (4) (-18.5, 2.5, 0.0)", 83007 }, //Cell 2 Desk 3
        { "POW_Camp|Desk_U_01 (5) (-14.5, 2.5, 0.0)", 83008 }, //Cell 2 Desk 4

        { "POW_Camp|Desk_D_01 (3) (-18.5, -3.5, 0.0)", 83009 }, //Cell 3 Desk 1
        { "POW_Camp|Desk_D_01 (2) (-14.5, -3.5, 0.0)", 83010 }, //Cell 3 Desk 2
        { "POW_Camp|Desk_U_01 (3) (-18.5, -9.5, 0.0)", 83011 }, //Cell 3 Desk 3
        { "POW_Camp|Desk_U_01 (2) (-14.5, -9.5, 0.0)", 83012 }, //Cell 3 Desk 4

        { "POW_Camp|Desk_D_01 (-18.5, -15.5, 0.0)", 83013 }, //Cell 4 Desk 1
        { "POW_Camp|Desk_D_01 (1) (-14.5, -15.5, 0.0)", 83014 }, //Cell 4 Desk 2
        { "POW_Camp|Desk_U_01 (-18.5, -21.5, 0.0)", 83015 }, //Cell 4 Desk 3
        { "POW_Camp|Desk_U_01 (1) (-14.5, -21.5, 0.0)", 83016 }, //Cell 4 Desk 4

        { "POW_Camp|Desk_D_01 (3) (11.5, -22.5, 0.0)", 83017 }, //Southern Storage Desk Floor 1
        { "POW_Camp|Desk_R_01 (9.5, 12.5, -6.0)", 83018 }, //Green Lounge Desk Floor 2
        { "POW_Camp|Desk_D_01 (-8.5, 35.5, 0.0)", 83019 }, //Medic Desk Floor 1 (Infirmary Outfit)

        { "POW_Camp|Desk_D_01 (2) (-32.5, -30.4, 0.0)", 83020 }, //Motorcycle Room Desk (Makeshift Ladder)

        { "POW_Camp|Desk_U_01 (-16.5, 30.5, 0.0)", 83021 }, //Maintenance Desk Floor 1 (Cyan Door)
        { "POW_Camp|Desk_D_01 (39.5, 0.5, -6.0)", 83022 }, //Maintenance Desk Floor 2 (Cyan Door)

        { "POW_Camp|Desk_D_01 (20.5, 26.5, -6.0)", 83023 }, //Left Guard Room Desk Floor 2 (Red Door) 
        { "POW_Camp|Desk_D_01 (1) (21.5, 26.5, -6.0)", 83024 }, //Right Guard Room Desk Floor 2 (Red Door)

        { "POW_Camp|Desk_D_01 (1) (21.5, 25.5, 0.0)", 83025 }, //Contraband Desk (Red Door AND Guard Outfit)
        { "POW_Camp|Cutlery_Tray_Down (16.5, 6.5, 0.0)", 83026 }, //Cutlery Tray

        // { "POW_Camp|Desk_D_01 (1) (-30.5, 34.5, 0.0)", 83021 },             //Multiplayer


        // === HMP OFFSHORE (Oil_Rig) ===
        { "Oil_Rig|Desk_U_01 (-23.5, 0.5, -18.0)", 84001 }, //Top Left Cell Floor 6
        { "Oil_Rig|Desk_U_01 (1) (-23.5, -5.5, -18.0)", 84002 }, //Middle Left Cell Floor 6
        { "Oil_Rig|Desk_U_01 (2) (-23.5, -11.5, -18.0)", 84003 }, //Bottom Left Cell Floor 6

        { "Oil_Rig|Desk_U_01 (5) (-18.5, 0.5, -18.0)", 84004 }, //Top Right Cell Floor 6
        { "Oil_Rig|Desk_U_01 (4) (-18.5, -5.5, -18.0)", 84005 }, //Middle Right Cell Floor 6
        { "Oil_Rig|Desk_U_01 (3) (-18.5, -11.5, -18.0)", 84006 }, //Bottom Right Cell Floor 6

        { "Oil_Rig|Desk_U_01 (6) (-23.5, 1.5, -24.0)", 84007 }, //Top Left Cell Floor 7
        { "Oil_Rig|Desk_U_01 (7) (-23.5, -4.5, -24.0)", 84008 }, //Middle Left Cell Floor 7
        { "Oil_Rig|Desk_U_01 (8) (-23.5, -10.5, -24.0)", 84009 }, //Bottom Left Cell Floor 7

        { "Oil_Rig|Desk_U_01 (9) (-18.5, 1.5, -24.0)", 84010 }, //Top Right Cell Floor 7
        { "Oil_Rig|Desk_U_01 (10) (-18.5, -4.5, -24.0)", 84011 }, //Middle Right Cell Floor 7
        { "Oil_Rig|Desk_U_01 (11) (-18.5, -10.5, -24.0)", 84012 }, //Bottom Right Cell Floor 7

        { "Oil_Rig|Desk_U_01 (6) (-31.5, 32.5, -18.0)", 84013 }, //Library Floor 6 

        { "Oil_Rig|Desk_U_01 (13) (-40.5, 19.5, -24.0)", 84014 }, //Middle Left Top of Stairs Floor 7
        { "Oil_Rig|Desk_U_01 (14) (7.5, 17.5, -24.0)", 84015 }, //Middle Right Building Middle of Stairs Floor 7
        { "Oil_Rig|Desk_U_01 (12) (-37.5, 4.5, -24.0)", 84016 }, //Bottom Left Building Library Floor 7

        { "Oil_Rig|Desk_R_01 (-27.5, 38.5, -36.0)", 84017 }, //Top Building Top of Stairs Floor 9

        { "Oil_Rig|Desk_R_01 (-14.5, 29.5, -30.0)", 84018 }, //Medic Desk next to job office (Infirmary outfit)
        { "Oil_Rig|Desk_R_01 (-41.5, 41.5, -18.0)", 84019 }, //Maintenance Desk Floor 6 (Cyan Door)
        { "Oil_Rig|Desk_R_01 (0.5, 11.5, -24.0)", 84020 }, //Maintenance Desk South East Map Floor 7 (Cyan Door)
        {
            "Oil_Rig|Desk_D_01 (-11.5, 41.5, -36.0)", 84021
        }, //Maintenance Desk North Map (Next to Cyan Key Card room) Floor 9 (Cyan Door)
        {
            "Oil_Rig|Desk_R_01 (17.5, -34.5, -12.0)", 84022
        }, //Maintenance Desk In Security Area (Cyan Key AND Red Door)

        { "Oil_Rig|Desk_R_01 (1) (-43.5, 20.5, -12.0)", 84023 }, //Left Stairwell (Red Door)
        { "Oil_Rig|Desk_D_01 (6.5, 13.5, -6.0)", 84024 }, //Right Stairwell (Red Door)
        { "Oil_Rig|Desk_U_01 (1) (21.5, -26.5, -18.0)", 84025 }, //Guard Area (Red Door)
        { "Oil_Rig|Desk_U_01 (-44.5, 11.5, -3.0)", 84026 }, //Bottom of Left Stairwell (Red Door)
        { "Oil_Rig|Desk_U_01 (21.5, -20.5, -18.0)", 84027 }, //Guard Area (Red Door)

        { "Oil_Rig|Desk_D_01 (30.5, -32.5, -12.0)", 84028 }, //Contraband Desk (Red Door AND Guard Outfit)

        { "Oil_Rig|Cutlery_Tray_Down (-19.6, 29.5, -24.0)", 84029 }, //Cutlery Tray

        // { "Oil_Rig|Desk_D_01 (-40.5, 35.5, -24.0)", 84010 },                //Multiplayer
        // { "Oil_Rig|Desk_R_01 (2.5, 27.5, -18.0)", 84012 },                  //Multiplayer
        // { "Oil_Rig|Desk_D_01 (1.5, 30.5, -18.0)", 84025 },                  //Multiplayer
        // { "Oil_Rig|Desk_U_01 (-41.5, 28.5, -24.0)", 84027 },                //Multiplayer
        // { "Oil_Rig|Desk_R_01 (1) (-34.5, 37.5, -36.0)", 84028 },            //Multiplayer

        // === FORT TUNDRA (Gulag_Prison) ===
        { "Gulag_Prison|Desk_L_01 (5) (14.5, 6.5, 0.0)", 85001 }, //North East Cell Upper Desk Floor 1
        { "Gulag_Prison|Desk_L_01 (4) (14.5, 3.5, 0.0)", 85002 }, //North East Cell Middle Desk Floor 1
        { "Gulag_Prison|Desk_L_01 (3) (14.5, 0.5, 0.0)", 85003 }, //North East Cell Lower Desk Floor 1

        { "Gulag_Prison|Desk_R_01 (3) (-21.5, 6.5, 0.0)", 85004 }, //North West Cell Upper Desk Floor 1
        { "Gulag_Prison|Desk_R_01 (4) (-21.5, 3.5, 0.0)", 85005 }, //North West Cell Middle Desk Floor 1
        { "Gulag_Prison|Desk_R_01 (5) (-21.5, 0.5, 0.0)", 85006 }, //North West Cell Lower Desk Floor 1

        { "Gulag_Prison|Desk_R_01 (2) (-21.5, -9.5, 0.0)", 85007 }, //South West Cell Upper Desk Floor 1
        { "Gulag_Prison|Desk_R_01 (1) (-21.5, -12.5, 0.0)", 85008 }, //South West Cell Middle Desk Floor 1
        { "Gulag_Prison|Desk_R_01 (-21.5, -15.5, 0.0)", 85009 }, //South West Cell Lower Desk Floor 1

        { "Gulag_Prison|Desk_L_01 (2) (14.5, -9.5, 0.0)", 85010 }, //South East Cell Upper Desk Floor 1
        { "Gulag_Prison|Desk_L_01 (1) (14.5, -12.5, 0.0)", 85011 }, //South East Cell Middle Desk Floor 1
        { "Gulag_Prison|Desk_L_01 (14.5, -15.5, 0.0)", 85012 }, //South East Cell Lower Desk Floor 1

        { "Gulag_Prison|Desk_L_01 (5) (14.5, 7.5, -6.0)", 85013 }, //North East Cell Upper Desk Floor 2
        { "Gulag_Prison|Desk_L_01 (4) (14.5, 4.5, -6.0)", 85014 }, //North East Cell Middle Desk Floor 2
        { "Gulag_Prison|Desk_L_01 (3) (14.5, 1.5, -6.0)", 85015 }, //North East Cell: Lower Desk Floor 2

        { "Gulag_Prison|Desk_R_01 (3) (-21.5, 7.5, -6.0)", 85016 }, //North West Cell Upper Desk Floor 2
        { "Gulag_Prison|Desk_R_01 (4) (-21.5, 4.5, -6.0)", 85017 }, //North West Cell Middle Desk Floor 2
        { "Gulag_Prison|Desk_R_01 (5) (-21.5, 1.5, -6.0)", 85018 }, //North West Cell Lower Desk Floor 2

        { "Gulag_Prison|Desk_R_01 (2) (-21.5, -8.5, -6.0)", 85019 }, //South West Cell Upper Desk Floor 2
        { "Gulag_Prison|Desk_R_01 (1) (-21.5, -11.5, -6.0)", 85020 }, //South West Cell Middle Desk Floor 2
        { "Gulag_Prison|Desk_R_01 (-21.5, -14.5, -6.0)", 85021 }, //South West Cell Lower Desk Floor 2

        { "Gulag_Prison|Desk_L_01 (2) (14.5, -8.5, -6.0)", 85022 }, //South East Cell Upper Desk Floor 2
        { "Gulag_Prison|Desk_L_01 (1) (14.5, -11.5, -6.0)", 85023 }, //South East Cell Middle Desk Floor 2
        { "Gulag_Prison|Desk_L_01 (14.5, -14.5, -6.0)", 85024 }, //South East Cell Lower Desk Floor 2

        { "Gulag_Prison|Desk_D_01 (1) (21.5, 25.5, 0.0)", 85025 }, //Social Room Floor 1
        { "Gulag_Prison|Desk_D_01 (-27.5, 13.5, 0.0)", 85026 }, //Above Nurse Office, Next to maintenance Floor 1

        { "Gulag_Prison|Desk_D_01 (-32.5, -2.5, 0.0)", 85027 }, //Medic Desk (Infirmary Outfit)

        { "Gulag_Prison|Desk_R_01 (32.5, 28.5, 0.0)", 85028 }, //Maintenance Room (Cyan Door)
        { "Gulag_Prison|Desk_D_01 (12.5, -35.5, 0.0)", 85029 }, //Lower Pat of the Map Left Desk Floor 1 (Red Door)
        { "Gulag_Prison|Desk_D_01 (5.5, -23.5, -6.0)", 85030 }, //Guard Bedroom Lower Area Floor 2 (Red Door)
        { "Gulag_Prison|Desk_D_01 (1) (20.5, -35.5, 0.0)", 85031 }, //Lower Part of the Map Right Desk (Red Door)
        {
            "Gulag_Prison|Desk_D_01 (1) (11.5, -23.5, -6.0)", 85032
        }, //Guard Break Room Lower Area Floor 2 (Red Door)
        { "Gulag_Prison|Desk_D_01 (2) (25.5, 13.5, -6.0)", 85033 }, //Above Power Generator Floor 2 (Red Door)

        { "Gulag_Prison|Desk_D_01 (34.5, 0.5, 0.0)", 85034 }, //Contraband Desk (Red Door AND Guard Outfit)
        { "Gulag_Prison|Cutlery_Tray_Down (-21.5, -32.5, 0.0)", 85035 }, //Cutlery Tray

        // { "Gulag_Prison|Desk_D_01 (1) (29.5, 43.5, 0.0)", 85024 },          //Multiplayer

        // === AREA 17 (Area_17) ===
        { "Area_17|Desk_L_01 (10.5, 25.5, 0.0)", 86001 }, //Cell 1 Floor 1
        { "Area_17|Desk_U_01 (13.5, 33.5, 0.0)", 86002 }, //Cell 2 Floor 1
        { "Area_17|Desk_U_01 (1) (27.5, 33.5, 0.0)", 86003 }, //Cell 3 Floor 1
        { "Area_17|Desk_R_01 (30.5, 25.5, 0.0)", 86004 }, //Cell 4 Floor 1

        { "Area_17|Desk_L_01 (10.5, 26.5, -6.0)", 86005 }, //Cell 1 Floor 2
        { "Area_17|Desk_U_01 (1) (13.5, 34.5, -6.0)", 86006 }, //Cell 2 Floor 2
        { "Area_17|Desk_U_01 (2) (27.5, 34.5, -6.0)", 86007 }, //Cell 3 Floor 2
        { "Area_17|Desk_R_01 (30.5, 26.5, -6.0)", 86008 }, //Cell 4 Floor 2

        { "Area_17|Desk_U_01 (4) (-14.5, -17.5, 3.0)", 86009 }, //Desk Outside Infirmary (Underground)
        { "Area_17|Desk_U_01 (3) (-36.5, 3.5, 3.0)", 86010 }, //Western Stray Underground
        { "Area_17|Desk_U_01 (1) (38.5, -3.5, 3.0)", 86011 }, //Eastern Stray Underground
        { "Area_17|Desk_U_01 (7) (1.5, -25.5, 0.0)", 86012 }, //UFO Room Desk Floor 1

        { "Area_17|Desk_D_01 (1) (2.5, -11.5, 3.0)", 86013 }, //Medic Desk Underground (Infirmary Outfit)

        { "Area_17|Desk_L_01 (34.5, 10.5, 3.0)", 86014 }, //Maintenance Desk Underground (Cyan Door)

        { "Area_17|Desk_U_01 (1) (44.5, 31.5, 3.0)", 86015 }, //Alien Pod Room Guard Desk Underground (Red Door OR Infirmary Outfit)

        { "Area_17|Desk_D_01 (33.5, -4.5, 0.0)", 86016 }, //Floor 1 Southern Guard Storage Desk
        { "Area_17|Desk_D_01 (2) (-2.5, 9.5, 0.0)", 86017 }, //Musical Room Guard Desk Floor 1 (Red Door OR Infirmary Outfit)
        { "Area_17|Desk_L_01 (11.5, -13.5, 0.0)", 86018 }, //Outdoor Maintenance Desk Floor 1 (Red Door)
        { "Area_17|Desk_D_01 (29.5, 4.5, 0.0)", 86019 }, //Guard Lounge Floor 1 (Red Door OR Infirmary Outfit)
        { "Area_17|Desk_D_01 (1) (33.5, 14.5, 0.0)", 86020 }, //Northern Guard Storage Floor 1 (Red Door OR Infirmary Outfit)
        { "Area_17|Desk_U_01 (6) (39.5, 21.5, 0.0)", 86021 }, //Cell Outskirts South East Desk Floor 1 (Red Door)
        { "Area_17|Desk_U_01 (5) (22.5, 42.5, 0.0)", 86022 }, //Cell Outskirts: Northern Desk Floor 1 (Red Door)

        { "Area_17|Desk_U_01 (5) (38.5, 43.5, -6.0)", 86023 }, //Cell Outskirts North east Floor 2 (Red Door)
        { "Area_17|Desk_D_01 (3) (31.5, 0.5, -6.0)", 86024 }, //Guard Lounge Floor 2 (Red Door OR Infirmary Outfit)
        { "Area_17|Desk_D_01 (4) (4.5, 10.5, -6.0)", 86025 }, //Guard Storage Floor 2 (Red Door OR Infirmary Outfit)
        { "Area_17|Desk_U_01 (6) (2.5, 21.5, -6.0)", 86026 }, //Cell Outskirts South West Desk Floor 2 (Red Door)
        { "Area_17|Desk_D_01 (1) (13.5, 13.5, -6.0)", 86027 }, //Guard Office Desk Floor 2 (Red Door)

        { "Area_17|Desk_R_01 (-4.5, 10.5, 3.0)", 86028 }, //Contraband Desk Underground (Red door and Guard outfit)
        { "Area_17|Cutlery_Tray_Down (-12.0, 26.9, 3.0)", 86029 }, //Cutlery Tray

        // { "Area_17|Desk_U_01_Lunar (-33.5, 15.6, 0.0)", 86010 },            //Multiplayer
        // { "Area_17|Desk_U_01 (3) (29.5, -26.5, 3.0)", 86020 },              //Multiplayer
        // { "Area_17|Desk_U_01 (2) (-21.5, -11.5, 3.0)", 86025 },             //Multiplayer
        // { "Area_17|Desk_U_01 (2) (26.5, -27.5, 3.0)", 86033 },              //Multiplayer

        // === U.S.S. ANOMALY (Space Prison) ===
        { "Space Prison|Desk_U_01 (3) (6.5, -23.5, 0.0)", 87001 }, //Eastern Cell 1
        { "Space Prison|Desk_U_01 (4) (12.5, -23.5, 0.0)", 87002 }, //Eastern Cell 2
        { "Space Prison|Desk_U_01 (5) (18.5, -23.5, 0.0)", 87003 }, //Eastern Cell 3
        { "Space Prison|Desk_U_01 (8) (6.5, -33.5, 0.0)", 87004 }, //Eastern Cell 4
        { "Space Prison|Desk_U_01 (7) (12.5, -33.5, 0.0)", 87005 }, //Eastern Cell 5
        { "Space Prison|Desk_U_01 (6) (18.5, -33.5, 0.0)", 87006 }, //Eastern Cell 6

        { "Space Prison|Desk_U_01 (-20.5, -23.5, 0.0)", 87007 }, //Western Cell 1
        { "Space Prison|Desk_U_01 (1) (-14.5, -23.5, 0.0)", 87008 }, //Western Cell 2
        { "Space Prison|Desk_U_01 (2) (-8.5, -23.5, 0.0)", 87009 }, //Western Cell 3
        { "Space Prison|Desk_U_01 (11) (-20.5, -33.5, 0.0)", 87010 }, //Western Cell 4
        { "Space Prison|Desk_U_01 (10) (-14.5, -33.5, 0.0)", 87011 }, //Western Cell 5
        { "Space Prison|Desk_U_01 (9) (-8.5, -33.5, 0.0)", 87012 }, //Western Cell 6

        { "Space Prison|Desk_D_01 (-12.5, 8.5, 0.0)", 87013 }, //Medic Desk
        { "Space Prison|Desk_D_01 (1) (-9.5, 29.5, 0.0)", 87014 }, //Northern Storage Desk
        { "Space Prison|Desk_D_01 (1) (45.5, 6.5, -6.0)", 87015 }, //Jetpack/Forcefield Desk
        { "Space Prison|Desk_D_01 (34.5, -12.5, 0.0)", 87016 }, //Eastern Storage Desk
        { "Space Prison|Desk_D_01 (4) (13.5, 25.5, -6.0)", 87017 }, //Northern Stray Desk
        { "Space Prison|Desk_D_01 (1) (49.5, -27.5, -6.0)", 87018 }, //Generator Stray Desk
        { "Space Prison|Desk_D_01 (2) (-35.5, -16.5, 0.0)", 87019 }, //Kennels Desk
        { "Space Prison|Desk_D_01 (6.5, 8.5, 0.0)", 87020 }, //Storage Desk near Kitchen
        { "Space Prison|Desk_D_01 (3) (-18.5, 11.5, -6.0)", 87021 }, //Inmate Lounge Desk
        { "Space Prison|Desk_D_01 (2) (-35.5, 13.5, -6.0)", 87022 }, //Western Office Desk
        { "Space Prison|Desk_D_01 (3) (45.5, 12.5, 0.0)", 87023 }, //Guard Pod Room Desk

        { "Space Prison|Desk_D_01 (-11.5, 43.5, -6.0)", 87024 }, //Northern Hidden Stray Desk (Cutting Tool)

        { "Space Prison|Desk_D_01 (36.5, -16.5, -0.3)", 87025 }, //Eastern Maintenance Desk (Cyan Door)
        { "Space Prison|Desk_U_01 (-6.5, 8.5, 0.0)", 87026 }, //Central Maintenance Desk (Cyan Door)

        { "Space Prison|Desk_D_01 (40.5, -12.5, 0.0)", 87027 }, //Contraband Desk (Red Door AND Guard Outfit)

        { "Space Prison|Cutlery_Tray_Down (18.5, 5.3, 0.0)", 87028 }, //Cutlery Tray


        // === COUGAR CREEK RAILROAD (Transport_Train) ===
        { "Transport_Train|Desk_D_01 (-103.5, 0.5, 3.0)", 88001 }, //Last Carriage 
        { "Transport_Train|Desk_D_01 (1) (-88.5, 0.5, 3.0)", 88002 }, //Second to last carriage (With the horse)
        { "Transport_Train|Desk_R_01 (30.5, -1.5, 3.0)", 88003 }, //Kitchen Desk
        { "Transport_Train|Desk_U_01 (-11.5, -1.5, 3.0)", 88004 }, //Medic Desk
        { "Transport_Train|Desk_D_01 (3) (7.5, 0.5, 3.0)", 88005 }, //Carriage to right of medics
        { "Transport_Train|Desk_R_01 (80.5, -1.5, 3.0)", 88006 }, //First class Desk
        { "Transport_Train|Desk_U_01 (55.5, -3.5, 3.0)", 88007 }, //Carriage for Grapple Escape

        { "Transport_Train|Desk_D_01 (2) (-28.5, 0.5, 3.0)", 88008 }, //Contraband Desk in guards office

        //THERE IS ONE BEHIND THE MULTIPLAYER DOOR BUT IT'S NOT MAPPED

        // === H.M.S. ORCA (Transport_Boat) ===
        { "Transport_Boat|Desk_D_01 (-23.5, -7.5, 3.0)", 89001 }, //Library Desk Floor 1
        { "Transport_Boat|Desk_U_01 (-14.5, -3.5, 3.0)", 89002 }, //Solitary Office Desk Floor 1
        { "Transport_Boat|Desk_D_01 (9.5, 2.5, 3.0)", 89003 }, //Desk in top of corridor Floor 1
        { "Transport_Boat|Desk_D_01 (1) (24.5, -9.5, 3.0)", 89004 }, //Engine Room Right Desk Floor 1
        { "Transport_Boat|Desk_D_01 (17.5, -0.5, 3.0)", 89005 }, //Engine Room Left Desk Floor 1
        { "Transport_Boat|Desk_D_01 (9.5, -8.5, 3.0)", 89006 }, //Office to left of engine room Desk Floor 1

        { "Transport_Boat|Desk_R_01 (-12.5, -8.5, 0.0)", 89007 }, //By Cells Next to Stairs Floor 2
        { "Transport_Boat|Desk_U_01 (12.5, -4.5, 0.0)", 89008 }, //Medic Desk Floor 2
        { "Transport_Boat|Desk_U_01 (-5.5, -11.5, 0.0)", 89009 }, //Contraband Desk Floor 2

        { "Transport_Boat|Desk_D_01 (12.5, 4.5, -3.0)", 89010 }, //Guard Rest Room Floor 3
        { "Transport_Boat|Desk_R_01 (-12.5, -7.5, -3.0)", 89011 }, //Room 'below' control panel Floor 3
        {
            "Transport_Boat|Desk_U_01 (-9.5, -3.5, -3.0)", 89012
        }, //Room to right of control panel Desk 1 (Left) Floor 3
        {
            "Transport_Boat|Desk_D_01 (3.5, -7.5, -3.0)", 89013
        }, //Room to right of control panel Desk 1 (Right) Floor 3
        { "Transport_Boat|Desk_R_01 (5.5, -4.5, -3.0)", 89014 }, //Room to left of the 2 staircases Floor 3

        // { "Transport_Boat|Desk_R_01 (-20.5, 1.5, 0.0)", 89003 },            //CANT ACCESS IN SINGLE PLAYER (FLOOR 2)


        // === AIR FORCE CON (Transport_Plane)
        { "Transport_Plane|Desk_R_01 (-5.5, 1.5, 3.0)", 90001 }, //Left room on double doors Floor 1
        { "Transport_Plane|Desk_D_01 (-3.5, 11.5, 3.0)", 90002 }, //Room below solitary floor 1

        { "Transport_Plane|Desk_L_01 (2) (4.5, 9.5, 0.0)", 90003 }, //Mid Plane floor 2
        { "Transport_Plane|Desk_R_01 (1) (-4.5, 38.5, 0.0)", 90004 }, //Next to control panel top of plane Floor 2
        { "Transport_Plane|Desk_L_01 (1) (1.5, -10.5, 0.0)", 90005 }, //Contraband Desk in guards room Floor 2

        { "Transport_Plane|Desk_L_01 (1) (4.5, -6.5, -3.0)", 90006 }, //Hallway above kitchen Floor 3
        { "Transport_Plane|Desk_R_01 (0.5, 16.5, -3.0)", 90007 }, //Guard Rest Room Floor 3
        { "Transport_Plane|Desk_U_01 (1) (1.5, -19.5, -3.0)", 90008 }, //Below Kitchen Floor 3
        { "Transport_Plane|Desk_U_01 (4.5, 3.5, -3.0)", 90009 }, //Medic Desk Floor 3
        { "Transport_Plane|Desk_R_01 (-5.5, 28.5, -3.0)", 90010 }, //Office Floor 3
        { "Transport_Plane|Desk_D_01 (-5.5, -1.5, -3.0)", 90011 }, //Contraband Desk Floor 3

        // { "Transport_Plane|Desk_L_01 (1) (4.5, -4.5, 3.0)", 90007 },        //CANT ACCESS IN SINGLE PLAYER (FLOOR 1)
    };

    public static void FindIDAndSendCheck(DeskInteraction __instance)
    {
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
                    instance.Logger.LogInfo($"Searched Desk! (Sent ID: {apLocationId})");
                }
            }
            else
            {
                instance.Logger.LogWarning($"[AP UNMAPPED DESK] Missing desk! NOTIFY BIGCELERY842906 IF YOU SEE THIS - Copy this exact string: \"{uniqueDeskKey}\"");
            }

            checkedDesks.Add(deskInstanceID);
        }
    }
        
    [HarmonyPatch(typeof(DeskInteraction), "TryOpenDesk")]
    public class DeskInteraction_TryOpenDesk_Patch
    {
            
        static void Postfix(DeskInteraction __instance)
        {
            FindIDAndSendCheck(__instance);
        }

    }

    [HarmonyPatch(typeof(CarryObjectInteraction), "PickUpRPC")]
    public class CarryObjectInteraction_PickUpRPC_Patch
    {
        //Find desk interaction (or related) component, do the check as above, then get transform parameters and send before the pickup occurs
        static void Prefix(CarryObjectInteraction __instance, CarryObjectInteraction.AI_Decorations ___m_Decoration)
        {
            if (___m_Decoration == CarryObjectInteraction.AI_Decorations.Desk)
            {
                DeskInteraction desk = __instance.gameObject.GetComponent<DeskInteraction>();

                if (desk != null)
                {
                    FindIDAndSendCheck(desk);
                }
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
        if (strength_step != 0)
            synced_strength = base_Stat + (strength_prog_count * strength_step);
        else
            synced_strength = 100;

        if (stamina_step != 0)
            synced_stamina = base_Stat + (stamina_prog_count * stamina_step);
        else
            synced_stamina = 100;

        if (intellect_step != 0)
            synced_intellect = base_Stat + (intellect_prog_count * intellect_step);
        else
            synced_intellect = 100;
    }

    void SetStats()
    {
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
                instance.Logger.LogInfo("No Keys Required Object found, looking again");
                instance.KeysRequiredObject = GameObject.Find("FrontendParent/RootMenu/SlideBottom/SafeZone/MenuBody/CampaignMenu/PanelLeft/Column_01/PrisonSelect/PrisonPreviewImage/KeysRequired");
                return;
            }

            if (instance.KeysRequiredText == null)
            {
                instance.Logger.LogInfo("No Keys Required Text found, looking again");
                instance.KeysRequiredText = instance.KeysRequiredObject.GetComponent<T17Text>();
                return;
            }

            if (!isConnected)
            {
                instance.StartPipes();
                
                instance.KeysRequiredObject.SetActive(true);
                instance.KeysRequiredText.text = "Connect to Archipelago via the Console Window!";
                return;
            }

            LevelScript.PRISON_ENUM prison_Enum = GlobalStart.GetInstance().GetCurrentSelectedPrisonData().m_LevelInfo.m_PrisonEnum;

            if (instance.prisonUnlocksReceived == null)
            { // Disable all buttons until connection
                instance.KeysRequiredObject.SetActive(true);
                instance.KeysRequiredText.text = "Haven't received any prison unlocks yet...";
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
        instance.Logger.LogInfo("Doing the player stats now!!!");
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
                        instance.logging.SendChatMessage($"Prison Escape Check failed. Please report this to BigCelery842906 along with which escape you tried to complete, and the phrase: {escapeMethod}");
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
                    instance.logging.SendChatMessage($"Prison Escape Check failed. Please report this to BigCelery842906 along with which escape you tried to complete, and the phrase: {escapeMethod}");
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
            instance.Logger.LogInfo($"Recipe id trying to send is: {recipeID}");
            
            CraftManager.Recipe recipe = __instance.GetRecipeByID(recipeID);

            if (recipe == null || recipe.m_Ingredients == null)
            {
                instance.Logger.LogInfo("Recipe or Ingredients are null");
                return;
            }
            
            int[] ingredients = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (recipe.m_Ingredients[i] != null)
                {
                    ingredients[i] = recipe.m_Ingredients[i].m_ItemDataID;
                }
                else
                {
                    ingredients[i] = -1;
                }
            }
            
            instance.Logger.LogInfo($"Recipe Ingredients: [{ingredients[0]}, {ingredients[1]}, {ingredients[2]}]");

            int[] matchingBlueprint = null;

            foreach (KeyValuePair<string, int[]> blueprint in References.apworldBlueprints)
            {
                if (IngredientsMatch(ingredients, blueprint.Value))
                {
                    matchingBlueprint = blueprint.Value;
                    
                    instance.Logger.LogInfo($"Found AP Blueprint: {blueprint.Key}");
                    break;
                }
            }

            if (matchingBlueprint == null)
            {
                instance.Logger.LogInfo("Recipe not found in the apworld blueprints");
                return;
            }

            bool blueprintReceived = false;

            foreach (int[] receivedBlueprint in instance.blueprintsReceived)
            {
                if (IngredientsMatch(ingredients, receivedBlueprint))
                {
                    blueprintReceived = true;
                    break;
                }
            }

            if (!blueprintReceived)
            {
                __result = 101;
            }
            
        }

        private static bool IngredientsMatch(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            int[] sortedA = a;
            int[] sortedB = b;
            
            Array.Sort(sortedA);
            Array.Sort(sortedB);

            for (int i = 0; i < sortedA.Length; i++)
            {
                if (sortedA[i] != sortedB[i])
                    return false;
            }

            return true;

        }
    }
    
    #endregion //Crafting
    #endregion //Harmony Patches

    #endregion //Game's Code

}