﻿/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Base.Interfaces;

public interface IModule : IDisposable
{
	string Name { get; }

	void Init();
	bool InitEnd();
	void Save();
	void Load();
	void Shutdown();

	void OnPostServerInit();
	void OnServerInit();
	void OnServerSaved();
	void SetEnabled(bool enabled);
	bool GetEnabled();
	void OnEnableStatus();

	Dictionary<string, Dictionary<string, string>> GetDefaultPhrases();

	void OnEnabled(bool initialized);
	void OnDisabled(bool initialized);
}
