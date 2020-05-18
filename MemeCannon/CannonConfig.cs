using System;
using System.Collections.Generic;
using System.Text;

namespace MemeCannon
{
	/// <summary>MemeCannon User Configuration, serialized to CannonConfig.json</summary>
	public class CannonConfig
	{
		/// <summary>The access token that is returned by Twitter after the user hasallowed the MemeCannon access to their Twitter Account</summary>
		public string AccessToken { get; set; }
		/// <summary>The access token secret that is returned by Twitter after the user hasallowed the MemeCannon access to their Twitter Account</summary>
		public string AccessTokenSecret { get; set; }
		/// <summary>List of all the default hashtags to include IE: '#ObamaGate'. Should include the # symbol</summary>
		public List<string> DefaultHashtags { get; set; }
		/// <summary>Number of Hashtags to include with each Tweet, not including the Defaults (if any)</summary>
		public int HashTagCount { get; set; }
		/// <summary>The Root Folder that contains all the MemeAmmo folders IE: X:\MemeCannon\Ammo</summary>
		public string ImageSourceFolder { get; set; }
				
		public CannonConfig() 
		{
			this.DefaultHashtags = new List<string>();
		}
	}
}
