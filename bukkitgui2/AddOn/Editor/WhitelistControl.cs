﻿// EditorTab.cs in bukkitgui2/bukkitgui2
// Created 2014/01/17
// Last edited at 2014/08/17 11:19
// ©Bertware, visit http://bertware.net

namespace Net.Bertware.Bukkitgui2.AddOn.Editor
{
    public partial class WhitelistControl : IAddonTab
    {
		public WhitelistControl()
        {
            InitializeComponent();
		}

        public IAddon ParentAddon { get; set; }
    }
}