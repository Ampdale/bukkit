﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bukkitgui2.MinecraftInterop.OutputHandler.PlayerActions
{
	class PlayerActionKick : IPlayerAction
	{
		public string PlayerName { get; set; }
		public string Details { get; set; }
		public DateTime Time { get; set; }
	}
}
