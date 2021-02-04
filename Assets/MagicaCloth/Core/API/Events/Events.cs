// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

namespace MagicaCloth
{
    /// <summary>
    /// アバターパーツ接続イベント
    /// Avatar parts attach event.
    /// </summary>
    [System.Serializable]
    public class AvatarPartsAttachEvent : UnityEngine.Events.UnityEvent<MagicaAvatar, MagicaAvatarParts>
    {
    }

    /// <summary>
    /// アバターパーツ分離イベント
    /// Avatar parts detach event.
    /// </summary>
    [System.Serializable]
    public class AvatarPartsDetachEvent : UnityEngine.Events.UnityEvent<MagicaAvatar>
    {
    }
}
