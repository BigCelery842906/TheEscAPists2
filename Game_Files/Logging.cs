using BepInEx.Logging;

namespace TheEscAPists2;

public class Logging
{
    public Logging instance;

    internal static ManualLogSource Logger;

    public bool BridgeConnect;
    

    public Logging(ManualLogSource log)
    {
        if (instance == null)
        {
            instance = this;
        }

        if (instance != this)
        {
            //kill this instance somehow
        }
        
        Logger = log;
        // Plugin startup logic
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}!");
    }

    public void LogConsole(string message)
    {
        Logger.LogInfo("Logging item exists, sending the following message" + message);
        SendChatMessage(message);
    }

    public void SendChatMessage(string message)
    {
        GetChatBox(message);
    }

    private void GetChatBox(string message)
    {
        ChatFeedManager cfManager = ChatFeedManager.GetInstance();
            
        if ((UnityEngine.Object) cfManager != (UnityEngine.Object) null)
        {
            Logger.LogInfo("Chat Feed Manager Exists and has been found");
            Logger.LogInfo("Sending through Chat Message RPC");
            // cfManager.SendChatMessage_RPC(new Gamer().m_GamerName == "Server", message, ChatFeedManager.MessageTag.System, false, false);
            cfManager.SendChatMessage_RPC(Gamer.GetPrimaryGamer(), message, ChatFeedManager.MessageTag.System, false, false);
            cfManager.SendSystemMessageLocalized_RPC(message + " LOCALIZED", ChatFeedManager.MessageTag.System, false);
            // cfManager.SendAlertnessMessage_RPC(message + " ALERTNESS", ChatFeedManager.MessageTag.System, );
            cfManager.SendAlertnessMessage_RPC("Localization tag", message + "ALERTNESS PRISON", (string) null, -1, ChatFeedManager.MessageTag.Prison);
            cfManager.SendAlertnessMessage_RPC("Localization tag", message + "ALERTNESS SYSTEM", (string) null, -1, ChatFeedManager.MessageTag.System);
            cfManager.SendMessage(message + "MESSAGE");
            cfManager.SendMessageUpwards(message + " MESSAGE UPWARDS");
        }
        else
        {
            Logger.LogWarning("No Chat Feed Manager Found");
        }
    }
}