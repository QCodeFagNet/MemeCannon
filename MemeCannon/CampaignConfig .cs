using System;
using System.Collections.Generic;
using System.Text;
//
using System.Text.Json;

namespace MemeCannon
{
	public class CampaignConfig
	{
		/// <summary>List of all the default hashtags to include IE: '#ObamaGate'. Should include the # symbol</summary>
		public List<string> DefaultHashtags { get; set; }
		/// <summary>List of all the campaign hashtags to include IE: '#ObamaGate'. Should include the # symbol</summary>
		public List<string> Hashtags { get; set; }
		/// <summary>List of all the campaign mentions to include IE: '@nygovcuomo'. Should include the @ symbol</summary>
		public List<string> Mentions { get; set; }
		/// <summary>Number of Hashtags to include with each Tweet, not including the Defaults (if any)</summary>
		public int HashTagCount { get; set; }
		/// <summary>The minimum length of time in minutes to wait between posts</summary>
		public int MinimumDelay { get; set; }
		/// <summary>The maximum length of time in minutes to wait between posts</summary>
		public int MaximumDelay { get; set; }

		public CampaignConfig() : base()
		{
			this.DefaultHashtags = new List<string>();
			this.Hashtags = new List<string>();
			this.Mentions = new List<string>();
			this.HashTagCount = 1;
			this.MinimumDelay = 1;
			this.MaximumDelay = 3;
		}

		// Force the system to read in hashtags to see if there is an update
		public bool UpdateHashtags(string path)
		{
			string npath = String.Format(@"{0}\hashtags\CampaignConfig.json", path);
			return true;
		}
	}
}
