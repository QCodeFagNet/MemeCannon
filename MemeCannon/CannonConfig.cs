using System;
using System.Collections.Generic;
using System.Text;

namespace MemeCannon
{
	public abstract class BaseConfig
	{
		/// <summary>List of all the default hashtags to include IE: '#ObamaGate'. Should include the # symbol</summary>
		public List<string> DefaultHashtags { get; set; }
		/// <summary>Number of Hashtags to include with each Tweet, not including the Defaults (if any)</summary>
		public int HashTagCount { get; set; }
		/// <summary>The minimum length of time in minutes to wait between posts</summary>
		public int MinimumDelay { get; set; }
		/// <summary>The maximum length of time in minutes to wait between posts</summary>
		public int MaximumDelay { get; set; }

		public BaseConfig()
		{
			this.DefaultHashtags = new List<string>();
			this.HashTagCount = 1;
			this.MinimumDelay = 1;
			this.MaximumDelay = 3;
		}
	}

	public class CampaignConfig : BaseConfig
	{
		/// <summary>List of all the campaign hashtags to include IE: '#ObamaGate'. Should include the # symbol</summary>
		public List<string> Hashtags { get; set; }
		/// <summary>List of all the campaign mentions to include IE: '@nygovcuomo'. Should include the @ symbol</summary>
		public List<string> Mentions { get; set; }

		public CampaignConfig() : base()
		{
			this.Hashtags = new List<string>();
			this.Mentions = new List<string>();
		}

		// Force the system to read in hashtags to see if there is an update
		public bool UpdateHashtags(string path)
		{
			string npath = String.Format(@"{0}\hashtags\CampaignConfig.json", path);
			return true;
		}
	}

	/// <summary>MemeCannon User Configuration, serialized to CannonConfig.json</summary>
	public class CannonConfig : BaseConfig
	{
		/// <summary>The access token that is returned by Twitter after the user hasallowed the MemeCannon access to their Twitter Account</summary>
		public string AccessToken { get; set; }
		/// <summary>The access token secret that is returned by Twitter after the user hasallowed the MemeCannon access to their Twitter Account</summary>
		public string AccessTokenSecret { get; set; }
		/// <summary>The Root Folder that contains all the MemeAmmo folders IE: X:\MemeCannon\Ammo</summary>
		public string ImageSourceFolder { get; set; }

		public CannonConfig() : base() { }
	}
}
