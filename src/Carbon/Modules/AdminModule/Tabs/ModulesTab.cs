﻿#if !MINIMAL

/*
*
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using Newtonsoft.Json;

namespace Carbon.Modules;

public partial class AdminModule : CarbonModule<AdminConfig, AdminData>
{
	public class ModulesTab
	{
		public static Tab Get()
		{
			var tab = (Tab)null;
			void Draw()
			{
				tab.AddColumn(0, true);
				tab.AddColumn(1, true);

				tab.AddName(0, "Core Modules");
				Generate(x => x.ForceEnabled);

				tab.AddName(0, "Other Modules");
				Generate(x => !x.ForceEnabled);

				void Generate(Func<BaseModule, bool> condition)
				{
					foreach (var hookable in Community.Runtime.ModuleProcessor.Modules)
					{
						if (hookable is BaseModule module)
						{
							if (!condition(module)) continue;

							tab.AddButtonArray(0,
								new Tab.OptionButton(hookable.Name, ap =>
								{
									ap.SetStorage(tab, "selectedmodule", module);
									Draw();
									DrawModuleSettings(tab, module);
								}, type: ap => ap.GetStorage<BaseModule>(tab, "selectedmodule") == module ? Tab.OptionButton.Types.Selected : Tab.OptionButton.Types.None),
								new Tab.OptionButton($"{(module.ForceEnabled ? "Always Enabled" : module.GetEnabled() ? "Enabled" : "Disabled")}", ap =>
								{
									if (module.ForceEnabled) return;

									module.SetEnabled(!module.GetEnabled());
									module.Save();
									ap.SetStorage(tab, "selectedmodule", module);
									Draw();
									DrawModuleSettings(tab, module);
								}, type: ap => module.ForceEnabled ? Tab.OptionButton.Types.Warned : module.GetEnabled() ? Tab.OptionButton.Types.Selected : Tab.OptionButton.Types.None));
						}
					}
				}
			}

			tab = new Tab("modules", "Modules", Community.Runtime.CorePlugin, accessLevel: 3, onChange: (ap, tab) =>
			{
				ap.ClearStorage(tab, "selectedmodule");
				Draw();
			});

			return tab;
		}

		internal static void DrawModuleSettings(Tab tab, BaseModule module)
		{
			tab.ClearColumn(1);

			var carbonModule = module.GetType();
			tab.AddInput(1, "Name", ap => module.Name, null);

			if (!module.ForceEnabled)
			{
				tab.AddToggle(1, "Enabled", ap2 => { module.SetEnabled(!module.GetEnabled()); module.Save(); DrawModuleSettings(tab, module); }, ap2 => module.GetEnabled());
			}

			tab.AddButtonArray(1,
				new Tab.OptionButton("Save", ap => { module.Save(); }),
				new Tab.OptionButton("Load", ap => { module.Load(); }));

			tab.AddButton(1, "Edit Config", ap =>
			{
				var moduleConfigFile = Path.Combine(Core.Defines.GetModulesFolder(), module.Name, "config.json");
				ap.SelectedTab = ConfigEditor.Make(OsEx.File.ReadText(moduleConfigFile),
					(ap, jobject) =>
					{
						Singleton.SetTab(ap.Player, "modules");
						Singleton.Draw(ap.Player);
					},
					(ap, jobject) =>
					{
						OsEx.File.Create(moduleConfigFile, jobject.ToString(Formatting.Indented));
						module.SetEnabled(false);
						module.Load();

						Singleton.SetTab(ap.Player, "modules");
						Singleton.Draw(ap.Player);
					}, null);
			});
		}
	}
}

#endif
