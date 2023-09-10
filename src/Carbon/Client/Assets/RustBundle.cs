﻿using Carbon.Client.Assets;
using Newtonsoft.Json;
using ProtoBuf;

namespace Carbon.Client
{
	[ProtoContract]
	public class RustBundle
	{
		[ProtoMember(1)]
		public Dictionary<string, RustComponent> Components = new();

		public void Process(Asset asset)
		{
			foreach (var path in asset.CachedBundle.GetAllAssetNames())
			{
				var unityAsset = asset.CachedBundle.LoadAsset<UnityEngine.Object>(path);

				if (unityAsset is GameObject go)
				{
					void Recurse(Transform transform)
					{
						if (Components.TryGetValue(transform.GetRecursiveName().ToLower(), out var component))
						{
							component.ApplyComponent(transform.gameObject);
						}

						foreach (Transform subTransform in transform)
						{
							Recurse(subTransform);
						}
					}
				}
			}
		}
	}
}