using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

namespace TheEscAPists2;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("TheEscapists2.exe")] //Set Specifically for the Escapists 2
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static ArchipelagoSession Session;

    private Logging logging;
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID} !");

        logging = new Logging();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            TryToConnect();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (logging != null)
            {
                Logger.LogDebug("Logging item exists");
                logging.SendChatMessage("Testing the Chat box. This could be a check");
            }
        }
    }

    private void TryToConnect()
    {
        string slotName = "Test";
        string host = "localhost";
        int port = 38121;
        string password = "";
        slotName.Trim();
        host.Trim();
        
        Session = ArchipelagoSessionFactory.CreateSession(host, port); //Test Parameters
        LoginResult loginResult= Session.TryConnectAndLogin("The Escapists 2", slotName, ItemsHandlingFlags.AllItems, new Version(MyPluginInfo.PLUGIN_VERSION), null, "",
            password);

        switch (loginResult)
        {
            case LoginFailure:
                Logger.LogError("Connection refused by server");
                logging.SendChatMessage("Failed to Connect to the Server");
                break;
            case LoginSuccessful:
                Logger.LogDebug("Connection Successful to server");
                logging.SendChatMessage("Successfully connected to Archipelago as " + slotName);
                break;
            default:
                break;
        }
    }
}
