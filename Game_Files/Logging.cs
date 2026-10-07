using BC_Dual_Ref_Library;
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
            //TODO:kill this instance somehow
        }
        
        Logger = log;
        // Plugin startup logic
        Logger.LogInfo($"Loading {ModInfo.PLUGIN_GUID}, Version: {ModInfo.PLUGIN_VERSION}!");
    }

    public void SendChatMessage(string message)
    {
        Logger.LogInfo($"[AP] {message}");
        try
        {
            ChatFeedManager cfManager = ChatFeedManager.GetInstance();
            Gamer localGamer = Gamer.GetPrimaryGamer();
            if (cfManager != null && localGamer != null)
            {
                cfManager.SendChatMessage_RPC(localGamer, $"[AP] {message}", ChatFeedManager.MessageTag.System, false,
                    false);
            }
        }
        catch
        {
        }
    }
}