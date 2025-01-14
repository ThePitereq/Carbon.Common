﻿/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Text;
namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("reload", "Reloads all or specific mods / plugins. E.g 'c.reload * <except[]>'' to reload everything.")]
	[AuthLevel(2)]
	private void Reload(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1)) return;

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				Community.Runtime.ReloadPlugins(arg.Args.Skip(1));
				break;

			default:
				var path = GetPluginPath(name);

				if (!string.IsNullOrEmpty(path))
				{
					Community.Runtime.ScriptProcessor.ClearIgnore(path);
					Community.Runtime.ScriptProcessor.Prepare(name, path);
					return;
				}

				var pluginFound = false;
				var pluginPrecompiled = false;

				foreach (var mod in ModLoader.LoadedPackages)
				{
					var plugins = Facepunch.Pool.GetList<RustPlugin>();
					plugins.AddRange(mod.Plugins);

					foreach (var plugin in plugins)
					{
						if (plugin.IsPrecompiled) continue;

						if (plugin.Name == name)
						{
							pluginFound = true;

							if (plugin.IsPrecompiled)
							{
								pluginPrecompiled = true;
							}
							else
							{
								plugin.ProcessorProcess.Dispose();
								plugin.ProcessorProcess.Execute(plugin.Processor);
								mod.Plugins.Remove(plugin);
							}
						}
					}

					Facepunch.Pool.FreeList(ref plugins);
				}

				if (!pluginFound)
				{
					Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
				}
				else if (pluginPrecompiled)
				{
					Logger.Warn($"Plugin {name} is a precompiled plugin which can only be reloaded programmatically.");
				}
				break;
		}
	}

	[ConsoleCommand("load", "Loads all mods and/or plugins. E.g 'c.load * <except[]>'' to load everything you've unloaded.")]
	[AuthLevel(2)]
	private void LoadPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin or use * to load all plugins.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				//
				// Scripts
				//
				{
					var except = arg.Args.Skip(1);

					Community.Runtime.ScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || except.Any(x.Contains));

					foreach (var plugin in OrderedFiles)
					{
						if (except.Any(plugin.Value.Contains) || Community.Runtime.ScriptProcessor.InstanceBuffer.ContainsKey(plugin.Key))
						{
							continue;
						}

						if (!Community.Runtime.ScriptProcessor.Exists(plugin.Value))
						{
							Community.Runtime.ScriptProcessor.Prepare(plugin.Key, plugin.Value);
						}
					}
					break;
				}

			default:
				{
					var path = GetPluginPath(name);
					if (!string.IsNullOrEmpty(path))
					{
						Community.Runtime.ScriptProcessor.ClearIgnore(path);

						if (!Community.Runtime.ScriptProcessor.Exists(path))
						{
							Community.Runtime.ScriptProcessor.Prepare(path);
						}

						return;
					}

					Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");

					/*var module = BaseModule.GetModule<DRMModule>();
					foreach (var drm in module.Config.DRMs)
					{
						foreach (var entry in drm.Entries)
						{
							if (entry.Id == name) drm.RequestEntry(entry);
						}
					}*/
					break;
				}
		}
	}

	[ConsoleCommand("unload", "Unloads all mods and/or plugins. E.g 'c.unload * <except[]>' to unload everything. They'll be marked as 'ignored'.")]
	[AuthLevel(2)]
	private void UnloadPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin or use * to unload all plugins.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				var except = arg.Args.Skip(1);

				//
				// Scripts
				//
				{
					var tempList = Facepunch.Pool.GetList<string>();

					foreach (var bufferInstance in Community.Runtime.ScriptProcessor.InstanceBuffer)
					{
						tempList.Add(bufferInstance.Value.File);
					}

					Community.Runtime.ScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || (except.Any() && !except.Any(x.Contains)));
					Community.Runtime.ScriptProcessor.Clear(except);

					foreach (var plugin in tempList)
					{
						if (except.Any(plugin.Contains))
						{
							continue;
						}

						Community.Runtime.ScriptProcessor.Ignore(plugin);
					}
				}

				//
				// Web-Scripts
				//
				{
					var tempList = Facepunch.Pool.GetList<string>();
					tempList.AddRange(Community.Runtime.WebScriptProcessor.IgnoreList);
					Community.Runtime.WebScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || (except.Any() && !except.Any(x.Contains)));
					Community.Runtime.WebScriptProcessor.Clear(except);

					foreach (var plugin in tempList)
					{
						if (except.Any(plugin.Contains))
						{
							continue;
						}

						Community.Runtime.WebScriptProcessor.Ignore(plugin);
					}
					Facepunch.Pool.FreeList(ref tempList);
					break;
				}

			default:
				{
					var path = GetPluginPath(name);
					if (!string.IsNullOrEmpty(path))
					{
						Community.Runtime.ScriptProcessor.Ignore(path);
						Community.Runtime.WebScriptProcessor.Ignore(path);
					}

					var pluginFound = false;
					var pluginPrecompiled = false;

					foreach (var mod in ModLoader.LoadedPackages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name == name)
							{
								pluginFound = true;

								if (plugin.IsPrecompiled)
								{
									pluginPrecompiled = true;
								}
								else
								{
									plugin.ProcessorProcess?.Dispose();
									mod.Plugins.Remove(plugin);
								}
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						if (string.IsNullOrEmpty(path)) Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						else Logger.Warn($"Plugin {name} was not loaded but was marked as ignored.");
					}
					else if (pluginPrecompiled)
					{
						Logger.Warn($"Plugin {name} is a precompiled plugin which can only be unloaded programmatically.");
					}
					break;
				}
		}
	}

	[ConsoleCommand("plugininfo", "Prints advanced information about a currently loaded plugin. From hooks, hook times, hook memory usage and other things.")]
	[AuthLevel(2)]
	private void PluginInfo(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin to print plugin advanced information.");
			return;
		}

		var name = arg.GetString(0);
		var plugin = ModLoader.LoadedPackages.SelectMany(x => x.Plugins).FirstOrDefault(x => string.IsNullOrEmpty(x.FileName) ? x.Name == name : x.FileName.Contains(name));
		var count = 1;

		if (plugin == null)
		{
			arg.ReplyWith("Couldn't find that plugin.");
			return;
		}

		using (var table = new StringTable("#", "Id", "Hook", "Time", "Memory", "Subscribed", "Async/Overrides"))
		{
			foreach (var hook in plugin.HookCache)
			{
				if (hook.Value.Count == 0)
				{
					continue;
				}

				var current = hook.Value[0];
				var hookName = current.Method.Name;

				var hookId = HookStringPool.GetOrAdd(hookName);
				var hookTime = hook.Value.Sum(x => x.HookTime);
				var hookMemoryUsage = hook.Value.Sum(x => x.MemoryUsage);
				var hookCount = hook.Value.Count;
				var hookAsyncCount = hook.Value.Count(x => x.IsAsync);

				if (!plugin.Hooks.Contains(hookId))
				{
					continue;
				}

				table.AddRow(count, hookId, $"{hookName}", $"{hookTime:0}ms", $"{ByteEx.Format(hookMemoryUsage, shortName: true).ToLower()}", !plugin.IgnoredHooks.Contains(hookId), $"{hookAsyncCount:n0}/{hookCount:n0}");

				count++;
			}

			var builder = new StringBuilder();

			builder.AppendLine($"{plugin.Name} v{plugin.Version} by {plugin.Author}{(plugin.IsCorePlugin ? $" [core]" : string.Empty)}");
			builder.AppendLine($"  Path:                   {plugin.FilePath}");
			builder.AppendLine($"  Compile Time:           {plugin.CompileTime}ms{(plugin.IsPrecompiled ? " [precompiled]" : string.Empty)}{(plugin.IsExtension ? " [ext]" : string.Empty)}");
			builder.AppendLine($"  Uptime:                 {TimeEx.Format(plugin.Uptime, true).ToLower()}");
			builder.AppendLine($"  Total Hook Time:        {plugin.TotalHookTime:0}ms");
			builder.AppendLine($"  Total Memory Used:      {ByteEx.Format(plugin.TotalMemoryUsed, shortName: true).ToLower()}");
			builder.AppendLine($"  Internal Hook Override: {plugin.InternalCallHookOverriden}");
			builder.AppendLine($"  Has Conditionals:       {plugin.HasConditionals}");
			builder.AppendLine($"  Mod Package:            {plugin.Package?.Name} ({plugin.Package?.Plugins.Count}){((plugin.Package?.IsCoreMod).GetValueOrDefault() ? $" [core]" : string.Empty)}");
			builder.AppendLine($"  Processor:              {plugin.Processor.Name} [{plugin.Processor.Extension}]");

			if (plugin is CarbonPlugin carbonPlugin)
			{
				builder.AppendLine($"  Carbon CUI:             {carbonPlugin.CuiHandler.Pooled:n0} pooled, {carbonPlugin.CuiHandler.Used:n0} used");
			}

			if (count == 1)
			{
				builder.AppendLine($"No hooks found.");
			}
			else
			{
				builder.AppendLine($"Hooks:");
				builder.AppendLine(table.ToStringMinimal());
			}

			arg.ReplyWith(builder.ToString());
		}
	}

	[ConsoleCommand("reloadconfig", "Reloads a plugin's config file. This might have unexpected results, use cautiously.")]
	[AuthLevel(2)]
	private void ReloadConfig(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin or use * to reload all plugin configs.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				{

					foreach (var package in ModLoader.LoadedPackages)
					{
						foreach (var plugin in package.Plugins)
						{
							plugin.ILoadConfig();
							plugin.Load();
							plugin.Puts($"Reloaded plugin's config.");
						}
					}

					break;
				}

			default:
				{
					var pluginFound = false;

					foreach (var mod in ModLoader.LoadedPackages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name == name)
							{
								plugin.ILoadConfig();
								plugin.Load();
								plugin.Puts($"Reloaded plugin's config.");
								pluginFound = true;
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
					}
					break;
				}
		}
	}

	[ConsoleCommand("uninstallplugin", "Unloads and uninstalls (moves the file to the backup folder) the plugin with the name.")]
	[AuthLevel(2)]
	private void UninstallPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin to uninstall it.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			default:
				{
					var path = GetPluginPath(name);

					var pluginFound = false;
					var pluginPrecompiled = false;

					foreach (var mod in ModLoader.LoadedPackages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name == name)
							{
								pluginFound = true;

								if (plugin.IsPrecompiled)
								{
									pluginPrecompiled = true;
								}
								else
								{
									plugin.ProcessorProcess?.Dispose();
									mod.Plugins.Remove(plugin);
								}
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						if (string.IsNullOrEmpty(path)) Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						else Logger.Warn($"Plugin {name} was not loaded but was marked as ignored.");

						return;
					}
					else if (pluginPrecompiled)
					{
						Logger.Warn($"Plugin {name} is a precompiled plugin which can only be unloaded/uninstalled programmatically.");
						return;
					}

					OsEx.File.Move(path, Path.Combine(Defines.GetScriptBackupFolder(), Path.GetFileName(path)));
					break;
				}
		}
	}

	[ConsoleCommand("installplugin", "Looks up the backups directory and moves the plugin back in the plugins folder installing it with the name.")]
	[AuthLevel(2)]
	private void InstallPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin to uninstall it.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			default:
				{
					var path = Path.Combine(Defines.GetScriptBackupFolder(), $"{name}.cs");

					if (!OsEx.File.Exists(path))
					{
						Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						return;
					}

					OsEx.File.Move(path, Path.Combine(Defines.GetScriptFolder(), Path.GetFileName(path)));
					break;
				}
		}
	}
}
