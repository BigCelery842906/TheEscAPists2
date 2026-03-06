using BepInEx;
using UnityEngine;

namespace TheEscAPists2;

public class Logging 
{
    
    private void Awake()
    {
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
            Plugin.Logger.LogDebug("Chat Feed Manager Exists and has been found");
            Plugin.Logger.LogDebug("Sending through Chat Message RPC");
            cfManager.SendChatMessage_RPC(Gamer.GetPrimaryGamer(), message, ChatFeedManager.MessageTag.System, false, false);
        }
        else
        {
            Plugin.Logger.LogWarning("No Chat Feed Manager Found");
        }
    }
}