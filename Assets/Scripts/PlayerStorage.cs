using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStorage
{
    internal string Nickname { get; private set; }

    public PlayerStorage()
    {
        if (PlayerPrefs.HasKey("nick"))
        {
            Nickname = PlayerPrefs.GetString("nick");
        }
        else
        {
            Nickname = "default player";
        }
    }

    internal void SetNewNickname(string nick)
    {
        Nickname = nick;
        PlayerPrefs.SetString("nick", Nickname);
        PlayerPrefs.Save();
    }

}
