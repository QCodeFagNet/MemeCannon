using System;
using System.Collections.Generic;
using System.Text;
//
using System.Text.Json;

namespace MemeCannon
{
	/// <summary>MemeCannon User Configuration, serialized to CannonConfig.json</summary>
	public class CannonConfig
	{
		/// <summary>The access token that is returned by Twitter after the user has allowed the MemeCannon access to their Twitter Account</summary>
		public string AccessToken { get; set; }
		/// <summary>The access token secret that is returned by Twitter after the user has allowed the MemeCannon access to their Twitter Account</summary>
		public string AccessTokenSecret { get; set; }
		/// <summary>The Root Folder that contains all the MemeAmmo folders IE: X:\MemeCannon\Ammo</summary>
		public string ImageSourceFolder { get; set; }

		public CannonConfig() { }// : base() { }
	}
}
