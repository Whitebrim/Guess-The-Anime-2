using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

/// <summary>
///     Хранит кэш для <see cref="AddressablesLoader" />
/// </summary>
public class AddressablesCache
{
    private static readonly Lazy<AddressablesCache> LazyLoader = new(() => new AddressablesCache());
    public readonly Dictionary<AssetReference, Object> Cache;

    public AddressablesCache()
    {
        Cache = new Dictionary<AssetReference, Object>();
    }

    public static AddressablesCache Instance => LazyLoader.Value;
}