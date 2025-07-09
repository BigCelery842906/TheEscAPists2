using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

namespace TheEscapists2AP;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("TheEscapists2.exe")] //Set Specifically for the Escapists 2
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static ArchipelagoSession Session;
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID} !");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            TryToConnect();
        }
    }

    private void TryToConnect()
    {
        Session = ArchipelagoSessionFactory.CreateSession("localhost:38281"); //Test Parameters
        Session.TryConnectAndLogin("TheEscapists2", "Test", ItemsHandlingFlags.NoItems, new Version(0, 5, 0), null, "",
            "");
    }
}
