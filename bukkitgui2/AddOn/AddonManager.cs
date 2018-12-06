﻿// AddonManager.cs in bukkitgui2/bukkitgui2
// Created 2014/05/17
// 
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file,
// you can obtain one at http://mozilla.org/MPL/2.0/.
// 
// ©Bertware, visit http://bertware.net

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using Net.Bertware.Bukkitgui2.Core;
using Net.Bertware.Bukkitgui2.Core.Configuration;
using Net.Bertware.Bukkitgui2.Core.Logging;
using Net.Bertware.Bukkitgui2.Core.Util;
using Net.Bertware.Bukkitgui2.UI;

namespace Net.Bertware.Bukkitgui2.AddOn
{
	public static class AddonManager
	{
		private const string CfgIdent = "addons";

		/// <summary>
		///     Boolean to keep the loaded state of the addons
		/// </summary>
		public static Boolean AddonsLoaded { get; private set; }

		/// <summary>
		///     Dictionary to store all loaded addons, by [Name, IAddon]
		/// </summary>
		public static Dictionary<string, IAddon> AddonsDictionary { get; private set; }

		/// <summary>
		///     Dictionary to store all loaded tabs, by [Name, UserControl]
		/// </summary>
		public static Dictionary<IAddon, UserControl> TabsDictionary { get; private set; }

		/// <summary>
		///     Dictionary to store all loaded settings pages, by [Name, UserControl]
		/// </summary>
		public static Dictionary<IAddon, UserControl> SettingsDictionary { get; private set; }

		public static event AddonsReadyEventArgs AddonsReady;

		public delegate void AddonsReadyEventArgs();

		private static void RaiseAddonsReadyEvent()
		{
			AddonsReadyEventArgs handler = AddonsReady;
			if (handler != null) handler.Invoke();
		}

		/// <summary>
		///     Get the instance of a loaded addon
		/// </summary>
		/// <param name="name">The name of the addon</param>
		/// <returns>Returns the addon if possible, null if the addon isn't loaded</returns>
		public static IAddon GetAddon(string name)
		{
			return AddonsDictionary.ContainsKey(name) ? AddonsDictionary[name] : null;
		}

		/// <summary>
		///     Get the instance of a loaded core addon
		/// </summary>
		/// <param name="addon">The addon to load</param>
		/// <returns>Returns the addon if possible, null if the addon isn't loaded</returns>
		public static IAddon GetRequiredAddon(RequiredAddon addon)
		{
			return AddonsDictionary.ContainsKey(addon.ToString())
				? AddonsDictionary[addon.ToString()]
				: null;
		}

		/// <summary>
		///     Load all tabpages to the mainform
		/// </summary>
		public static void LoadAddons()
		{
			if (AddonsLoaded) return;

			// construct the dictionary that we'll use to store addons during runtime
			AddonsDictionary = new Dictionary<string, IAddon>();
			TabsDictionary = new Dictionary<IAddon, UserControl>();
			SettingsDictionary = new Dictionary<IAddon, UserControl>();

			// auto unload on exit
			MainForm form = (MainForm) Control.FromHandle(Share.MainFormHandle);
			form.Closing += HandleFormClose;

			// Create and store a list with the loaded tabs, then load them to the UI.
			// max 16 addons for now

			Dictionary<string, Type> addonsToLoad = GetAvailableAddons();

			foreach (Type T in addonsToLoad.Values)
			{
				try
				{
					if (String.IsNullOrEmpty(T.Name)) continue;
					if ((T.Name != "Console" && T.Name != "Settings" && T.Name != "Starter") &&
					    Config.ReadBool(CfgIdent, "enable_" + T.Name, true) == false) continue;

					CreateAddon(T.FullName);
				}
				catch (Exception exception)
				{
					Logger.Log(LogLevel.Severe, "AddonManager", "Failed to load addon! " + T.Name, exception.Message);
				}
			}

			AddonsLoaded = true;
			RaiseAddonsReadyEvent();
		}

		private static void HandleFormClose(object sender, CancelEventArgs e)
		{
			UnloadAddons();
		}

		/// <summary>
		///     Dispose all addons
		/// </summary>
		public static void UnloadAddons()
		{
			foreach (IAddon addon in AddonsDictionary.Values)
			{
				addon.Dispose();
			}
		}

		/// <summary>
		///     Dynamicly load an addon (also registers it)
		/// </summary>
		/// <param name="name"></param>
		private static IAddon CreateAddon(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			try
			{
				IAddon addon = (IAddon) Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType(name));
				RegisterAddon(addon);
				return addon;
			}
			catch (Exception exception)
			{
				Logger.Log(LogLevel.Severe, "AddonManager", "Failed to create addon! " + name, exception.Message);
				return null;
			}
		}

		/// <summary>
		///     Dynamicly load an addon (also registers it)
		/// </summary>
		/// <param name="addonType"></param>
		private static IAddon CreateAddon(IAddon addonType)
		{
			try
			{
				IAddon addon = (IAddon) Activator.CreateInstance(addonType.GetType());
				RegisterAddon(addon);
				return addon;
			}
			catch (Exception exception)
			{
				Logger.Log(LogLevel.Severe, "AddonManager", "Failed to create addon! " + addonType, exception.Message);
				return null;
			}
		}

		/// <summary>
		///     Initialize and register an addon
		/// </summary>
		/// <param name="addon"></param>
		private static void RegisterAddon(IAddon addon)
		{
			// initialize
			addon.Initialize();

			// add
			AddonsDictionary.Add(addon.Name, addon);

			// if this addon has a tab, add it 
			if (addon.HasTab)
			{
				TabsDictionary.Add(addon, addon.TabPage);
			}
			// if this addon has a settings page, add it 
			if (addon.HasConfig)
			{
				SettingsDictionary.Add(addon, addon.ConfigPage);
			}
		}


		/// <summary>
		///     Reload an addon
		/// </summary>
		/// <param name="name"></param>
		/// <remarks>Reloading addons is highly discouraged! Only use if no other way is possible</remarks>
		public static void ReloadAddon(string name)
		{
			if (string.IsNullOrEmpty(name)) return;
			try
			{
				IAddon addon = GetAddon(name);
				if (addon == null) return;
				if (addon.HasTab) return; // we can't reload these yet!

				if (addon.HasTab && TabsDictionary.ContainsKey(addon)) TabsDictionary.Remove(addon);
				if (addon.HasConfig && SettingsDictionary.ContainsKey(addon)) SettingsDictionary.Remove(addon);

				Settings.Settings settingsmanager = ((Settings.Settings) GetRequiredAddon(RequiredAddon.Settings));

				settingsmanager.RemoveAddonSettings(addon); //unload settings
				addon.Dispose(); //dispose old addon
				addon = CreateAddon(addon); // create new one + initialize
				settingsmanager.AddAddonSettings(addon); // load settings
			}
			catch (Exception exception)
			{
				Logger.Log(LogLevel.Severe, "AddonManager", "Failed to reload addon! " + name, exception.Message);
			}
		}

		/// <summary>
		///     Get a list of all available server types
		/// </summary>
		/// <returns></returns>
		private static Dictionary<string, Type> GetAvailableAddons()
		{
            List<Type> unsortedAddons = new List<Type>();
			Dictionary<string, Type> addons = new Dictionary<string, Type>();
            // Get unsorted list of all addons
			foreach (
				Type addon in DynamicModuleLoader.ListClassesRecursiveOfType<IAddon>("Net.Bertware.Bukkitgui2.AddOn"))
			{
                unsortedAddons.Add(addon);
			}

            unsortedAddons.Sort((t1, t2) =>
            {
                IAddon a1 = (IAddon)Activator.CreateInstance(t1);
                IAddon a2 = (IAddon)Activator.CreateInstance(t2);
                return a2.Priority.CompareTo(a1.Priority);
            });

            foreach (Type addon in unsortedAddons)
            {
                if (!addons.ContainsKey(addon.Name)) addons.Add(addon.Name, addon);
            }
            
            Logger.Log(LogLevel.Info, "AddonManager", "Found " + addons.Count + " available addons");
			return addons;
		}
	}
}