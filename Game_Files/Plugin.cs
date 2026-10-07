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

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
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
                    Logger.LogWarning("Bridge disconnected. Please restart the bridge, and back out to the prison select screen.");
                    isConnected = false;
                    ClosePipes();
                    ResetStats();
                    
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

                if (!isConnected)
                {
                    strength_prog_count = 0;
                    stamina_prog_count = 0;
                    intellect_prog_count = 0;

                    ResetStats();
                }

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

    private void ResetStats()
    {
        slotName = "";
        pendingDeathLink = false;
        lastDeathLinkReason = "";
        pendingStealthLink = false;
        stealthLinkCulprit = "";

        prisonUnlocksReceived = new List<string>();
        blueprintsReceived = new List<int[]>();

        itemQueue = new Queue<int>();
        moneyToAdd = 0;
        trapQueue = new Queue<string>();

        strength_prog_count = 0;
        stamina_prog_count = 0;
        intellect_prog_count = 0;
        strength_step = 0;
        stamina_step = 0;
        intellect_step = 0;
        synced_strength = 0;
        synced_stamina = 0;
        synced_intellect = 0;
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
                Logger.LogInfo($"Progressive Strength Count: {strength_prog_count}");
                break;
            }

            case "Progressive Stamina":
            {
                stamina_prog_count++;
                Logger.LogInfo($"Progressive Stamina Count: {stamina_prog_count}");
                break;
            }

            case "Progressive Intellect":
            {
                intellect_prog_count++;
                Logger.LogInfo($"Progressive Intellect Count: {intellect_prog_count}");
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
            if (Desks.deskLocationIDs.ContainsKey(uniqueDeskKey))
            {
                long apLocationId = Desks.deskLocationIDs[uniqueDeskKey];

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
    
    private int strength_prog_count = 0;
    private int stamina_prog_count = 0;
    private int intellect_prog_count = 0;

    private int strength_step = 0;
    private int stamina_step = 0;
    private int intellect_step = 0;

    private int base_Stat = 30;

    private int synced_strength = 0;
    private int synced_stamina = 0;
    private int synced_intellect = 0;
    
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
        
        Logger.LogInfo($"Calculated: {instance.synced_strength}, {instance.synced_stamina}, {instance.synced_intellect}");
        Logger.LogInfo($"Prog Count: {instance.strength_prog_count}, {instance.stamina_prog_count}, {instance.intellect_prog_count}");
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
            }

            LevelScript.PRISON_ENUM prison_Enum = GlobalStart.GetInstance().GetCurrentSelectedPrisonData().m_LevelInfo.m_PrisonEnum;

            if (instance.prisonUnlocksReceived == null)
            { // Disable all buttons until connection
                ___m_ButtonClass.interactable = false;
                ___m_ButtonClass.m_bCanInteractWithMouse = false;
                if (isConnected)
                {
                    instance.KeysRequiredObject.SetActive(true);
                    instance.KeysRequiredText.text = "Haven't received any prison unlocks yet...";
                }

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