using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Services.Audio
{
    [Serializable]
    public class AudioBase
    {
        public AssetReferenceT<AudioClip> Clip;
    }
}