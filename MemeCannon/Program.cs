﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Tweetinvi;
using System.Text;
using SysThread = System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;
using System.Text.Json;

namespace MemeCannon
{
	class Program
	{
		private const int ratelimit = 60;
		private static CannonConfig CannonCfg { get; set; }
		private static string twitterConsumerKey { get; set; }
		private static string twitterConsumerSecret { get; set; }
		private static int TweetsPerHour { get; set; }
		private static List<DateTime> TweetTimes { get; set; }
		private static Stopwatch CampaignTimer { get; set; }
		private static TwitterClient client { get; set; }

		static async Task Main(string[] args)
		{
			try
			{
				await Initialize();
				Engage();

				Program.CampaignTimer.Stop();
				TimeSpan ts = Program.CampaignTimer.Elapsed;
				string runTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, (ts.Milliseconds / 10));
				Console.WriteLine("Done {0}", DateTime.Now);
				Console.WriteLine("Runtime: {0}", runTime);
				Console.WriteLine("Anykey to exit");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.ResetColor();
				Console.WriteLine("Anykey to exit");
				Console.ReadLine();
			}
		}

		/// <summary>Util method to iteratively check/prompt the console response for an integer or an X</summary>
		/// <returns></returns>
		private static string GetMenuResponse()
		{
			string response = Console.ReadLine();
			if (!response.ToLower().Equals("x", StringComparison.InvariantCulture))
			{
				// Pick a number
				int key = -1;
				bool wtf = false;
				while ((!int.TryParse(response, out key) && (!response.ToLower().Equals("x", StringComparison.InvariantCulture))))
				{
					wtf = true;
					Console.WriteLine("Only numbers. Try again or x to exit.");
					response = Console.ReadLine();
				}
				if (wtf) Console.WriteLine("Finally");
			}
			return response;
		}

		/// <summary>Util method to read the console response and check for a 'y'</summary>
		/// <returns>response == 'y'</returns>
		private static bool GetAddData()
		{
			string addData = Console.ReadLine();
			bool returnVal = false;
			if (addData.ToLower().Equals("y", StringComparison.InvariantCulture))
				returnVal = true;
			return returnVal;
		}

		/// <summary>Load and fire the MemeCannon</summary>
		private static void Engage()
		{
			Dictionary<int, string> filePaths = DisplayMenu();
			string response = GetMenuResponse();
			if (!response.ToLower().Equals("x", StringComparison.InvariantCulture))
			{
				Console.WriteLine();
				Console.Write("Include default Hashtags? (y/n) : ");
				bool addDefaultHashtags = GetAddData();

				Console.Write("Include User Mentions? (y/n) : ");
				bool addUserPings = GetAddData();

				string memePath = GetMemePath(response, filePaths);
				if (memePath.Length > 0)
				{
					// Greenlight
					Fire(memePath, addDefaultHashtags, addUserPings);
				}
			}
			else
			{
				Console.WriteLine("[Exit]");
				return;
			}
		}

		private static void Fire(string path, bool addDefaultHashtags, bool addPings)
		{
			List<string> hashtags = new List<string>();
			List<string> existingFileNames = new List<string>();
			List<string> postedFileNames = new List<string>();
			List<string> imageFileTypes = new List<string>() { ".jpg", ".jpeg", ".gif", ".png" };
			Random rnd = new Random();
			string jsonpath = String.Format(@"{0}\config\filenames.json", path); //TODO Could monitor for a change?
			postedFileNames = FileHelper.ReadJSONFromFile(jsonpath).ToObject<List<string>>().ToList();

			existingFileNames = Directory.EnumerateFiles(path)
				.Where(n => !postedFileNames.Contains(n)) // Don't post anything we've already posted
				.Where(n => imageFileTypes.Any(ext => ext == Path.GetExtension(n).ToLower())) // filter by image ext
				.OrderBy(n => Guid.NewGuid()).ToList(); // random order

			//Grab a local instance of the new CampaignConfig, or upgrade us.
			CampaignConfig config = GetCampaignConfig(path);

			Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine("\nFiring...");
			Console.ResetColor();

			int counter = 0;
			bool rateLimiterGood = true;
			foreach (string filename in existingFileNames)
			{
				if (!rateLimiterGood)
				{
					//Something is wrong and we've overshot our 60 tweets per hour limit. Bail so nobody get's banned
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("******* You've gone too big and are in danger of overshooting your rate limit.\nWait an hour and try again so you don't get banned. *******");
					Console.ResetColor();
					break;
				}

				StringBuilder sb = new StringBuilder();
				if (addPings) { AddMentions(sb, config); }
				AddHashtags(sb, path, addDefaultHashtags); // reloads every time so we can inject new hashtags

				//DOIT
				bool twatted = TweetWithImage(sb.ToString(), filename).Result;
				if (twatted)
				{
					// Add to the list of what we've already twatted and show the user
					postedFileNames.Add(filename);
					FileHelper.WriteJSONToFile(String.Format(@"{0}\config\filenames.json", path), postedFileNames);
					string fn = Path.GetFileName(filename);
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine("\n{0}", fn);
					Console.ResetColor();

					//Update the TweetPerHour data
					rateLimiterGood = UpdateTweetsPerHour();

					counter++;
					if (counter < existingFileNames.Count)
					{
						//get a random pause time between configured min and max, in millisec
						int min = (config.MinimumDelay * 60000);
						int max = config.MaximumDelay * 60000;
						int sleep = rnd.Next(min, max);
						Console.WriteLine(String.Format("{0}: Memecannon sleeping for [{1}] seconds", DateTime.Now, (sleep / 1000)));
						SysThread.Thread.Sleep(sleep);
					}
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("{0} no tweet", filename);
					Console.ResetColor();
				}
			}
		}

		private static async Task Initialize()
		{
			try
			{
				Program.CampaignTimer = new Stopwatch();
				Program.CampaignTimer.Start();
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("MemeCannon v{0} :: {1}", GetAssemblyVersion(), DateTime.Now);
				Console.ResetColor();
				DisplayFrameworkName();

				EncryptSettings(); // Encrypt if needed

				Program.twitterConsumerKey = ConfigurationManager.AppSettings["twitterConsumerKey"];
				Program.twitterConsumerSecret = ConfigurationManager.AppSettings["twitterConsumerSecret"];

				if ((Program.twitterConsumerKey.Length == 0) || (Program.twitterConsumerSecret.Length == 0))
					throw new Exception("Invalid Twitter API Keys. Check your app config.");

				Program.CannonCfg = FileHelper.ReadJSONObjectFromFile("CannonConfig.json").ToObject<CannonConfig>();

				// Check to see if we have already generated an accessToken and accessTokenSecret
				// If not, generate and save so we don't have to do it every time
				if (Program.CannonCfg.AccessToken.Length == 0)
				{
					await UpdateUserSettings();
				}

				// Save this again to make sure everything is up to date
				string strang = JsonSerializer.Serialize(Program.CannonCfg);
				FileHelper.WriteJSONToFile("./CannonConfig.json", strang);

				Program.TweetTimes = new List<DateTime>();

				// UserCredentials
				Program.client = new TwitterClient(Program.twitterConsumerKey, Program.twitterConsumerSecret, Program.CannonCfg.AccessToken, Program.CannonCfg.AccessTokenSecret);
				client.Config.TweetMode = TweetMode.Extended;
				await Program.client.Auth.InitializeClientBearerTokenAsync();

				Console.WriteLine("\n*****************");
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.ResetColor();
				Console.WriteLine("Bad Initialization data");
			}
		}

		/// <summary>Prompt the user to Authorize the MemeCannon to make tweets</summary>
		private static async Task<bool> UpdateUserSettings()
		{
			Console.WriteLine("Updating user settings for the first run");
			// Create a new set of credentials for the application.
			TwitterClient appClient = new TwitterClient(new Tweetinvi.Models.TwitterCredentials(twitterConsumerKey, twitterConsumerSecret));
			Tweetinvi.Models.IAuthenticationRequest authRequest = await appClient.Auth.RequestAuthenticationUrlAsync();

			ProcessStartInfo psi = new ProcessStartInfo(authRequest.AuthorizationURL)
			{
				UseShellExecute = true,
				Verb = "open"
			};
			// Causes a WebBrowser to open and the user needs to OK app access
			Process.Start(psi);

			Console.WriteLine();
			// Ask the user to enter the pin code given by Twitter
			Console.WriteLine("Enter PIN Code given by Twitter to continue:");
			string pinCode = Console.ReadLine();
			while (string.IsNullOrEmpty(pinCode))
			{
				Console.WriteLine("No PIN, try again");
				Console.WriteLine("Enter PIN Code given by Twitter to continue:");
				pinCode = Console.ReadLine();
			}

			// With this pin code it is now possible to get the credentials back from Twitter
			Tweetinvi.Models.ITwitterCredentials userCredentials = await appClient.Auth.RequestCredentialsFromVerifierCodeAsync(pinCode, authRequest);

			// Save off the accessToken and accessTokenSecret
			Program.CannonCfg.AccessToken = userCredentials.AccessToken;
			Program.CannonCfg.AccessTokenSecret = userCredentials.AccessTokenSecret;

			string strang = JsonSerializer.Serialize(Program.CannonCfg);
			FileHelper.WriteJSONToFile("./CannonConfig.json", strang);
			return true;
		}

		private static void DisplayFrameworkName()
		{
			TargetFrameworkAttribute targetFrameworkAttribute = (TargetFrameworkAttribute)Assembly.GetExecutingAssembly()
				.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
				.SingleOrDefault();
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Target Framework: {0}", targetFrameworkAttribute.FrameworkName);
			Console.ResetColor();
		}

		private static Dictionary<int, string> DisplayMenu()
		{
			int key = 0;
			DirectoryInfo di = new DirectoryInfo(Program.CannonCfg.ImageSourceFolder);
			Dictionary<int, string> dirs = di.GetDirectories().Select(p => p.FullName).OrderBy(n => n).ToDictionary(p => key++);
			// Now output the menu
			Console.WriteLine("\nSelect target campaign:");
			foreach (KeyValuePair<int, string> kvp in dirs)
			{
				string strang = kvp.Value.Split('\\').Last(); // Should get us the Subject, not the full path
				Console.WriteLine(String.Format("{0} : {1}", kvp.Key, strang));
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("Enter Choice: ");
			Console.ResetColor();
			return dirs;
		}

		/// <summary>Util method to get the MemePath from the list of MemePaths</summary>
		/// <param name="resp"></param>
		/// <param name="paths"></param>
		/// <returns>string path</returns>
		private static string GetMemePath(string resp, Dictionary<int, string> paths)
		{
			// Try and parse this into a number
			int key = -1;
			if (int.TryParse(resp, out key))
				return paths[key];
			else
				return string.Empty;
		}

		/// <summary>Updated to run with TweetInvi and Twitter V2 API</summary>
		/// <param name="text"></param>
		/// <param name="imgPath"></param>
		/// <returns></returns>
		private static async Task<bool> TweetWithImage(string text, string imgPath)
		{
			if (imgPath.Length > 0)
			{
				if (File.Exists(imgPath))
				{
					byte[] imgdata = System.IO.File.ReadAllBytes(imgPath);
					Tweetinvi.Models.IMedia media = await Program.client.Upload.UploadBinaryAsync(imgdata);//UploadTweetImageAsync // so you can upload animated gifs
					if (media.IsReadyToBeUsed)
					{

						var tweet = new TweetsV2Poster(Program.client);
						var request = new TweetV2PostRequest
						{
							Text = text,
							Media = new TweetV2Media()
							{
								MediaId = new string[] { media.UploadedMediaInfo.MediaIdStr },
							}
						};

						Tweetinvi.Core.Web.ITwitterResult result = await tweet.PostTweet(request);
						if (result.Response.IsSuccessStatusCode == true)
						{
							return true;
						}

						return false;
					}
				}
			}
			return false;
		}

		/// <summary>Method that reads in the 'hashtags' and adds them to the passed in StringBuilder</summary>
		/// <param name="sb"></param>
		/// <param name="memePath"></param>
		/// <param name="addDefaultHashtags"></param>
		private static void AddHashtags(StringBuilder sb, string memePath, bool addDefaultHashtags)
		{
			// Read in a new CampaignConfig each time se we can check for campaign updates while we're running
			CampaignConfig config = GetCampaignConfig(memePath);

			List<string> currentTags = new List<string>();
			int hashtagCount = config.HashTagCount;
			Random rnd = new Random();

			if ((addDefaultHashtags) && (config.DefaultHashtags.Count > 0))
			{
				config.DefaultHashtags.ForEach(h => { sb.Append(String.Format("{0} ", h)); currentTags.Add(h); });
			}

			if (config.Hashtags.Count > 0)
			{
				// get a random set of hashtags
				for (int eye = 0; eye < hashtagCount; eye++)
				{
					bool ok = false;
					int count = 0;
					while (!ok)
					{
						int id = rnd.Next(0, config.Hashtags.Count);
						string hash = config.Hashtags[id];
						//Dont add a duplicate
						if (!sb.ToString().Contains(hash, StringComparison.InvariantCulture))
						{
							sb.Append(String.Format("{0} ", hash));
							currentTags.Add(hash);
							ok = true;
						}

						// Bail if we've tried everything twice
						if (count == (hashtagCount * 2)) { return; }
						count++;
					}
				}
			}

			//Output
			currentTags.ForEach(h => Console.Write("{0} ", h));
		}

		/// <summary>
		/// Method that reads in the 'mentions' and adds them to the passed in StringBuilder
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="memePath"></param>
		private static void AddMentions(StringBuilder sb, CampaignConfig config)
		{
			int mentionsCount = 1; // Try and keep under the spammeradar, only 1 per tweet
			Random rnd = new Random();

			if (config.Mentions.Count > 0)
			{
				// get a random set of mentions
				for (int eye = 0; eye < mentionsCount; eye++)
				{
					bool ok = false;
					int count = 0;
					while (!ok)
					{
						int id = rnd.Next(0, config.Mentions.Count);
						string mention = config.Mentions[id];
						//Dont add a duplicate
						if (!sb.ToString().Contains(mention, StringComparison.InvariantCulture))
						{
							sb.Append(String.Format(".{0} ", mention));
							ok = true;
						}

						// Bail if we've tried everything twice
						if (count == (mentionsCount * 2)) { return; }
						count++;
					}
				}
			}
		}

		/// <summary>Utility method to encrypt the AppSettings</summary>
		private static void EncryptSettings()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			AppSettingsSection section = config.AppSettings;

			if (!section.SectionInformation.IsProtected)
			{
				section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
				config.Save();
			}
		}

		/// <summary>Calculates and displays the current TweetsPerHour</summary>
		/// <returns>bool if you are over the limit</returns>
		private static bool UpdateTweetsPerHour()
		{
			bool returnVal = true;
			if (Program.TweetTimes.Count.Equals(0))
			{
				Program.TweetTimes.Add(DateTime.Now);
				Program.TweetsPerHour = 1;
			}
			else
			{
				DateTime now = DateTime.Now;
				DateTime first = Program.TweetTimes.First();
				TimeSpan ts = now - first;

				Program.TweetTimes.Add(now);
				Program.TweetsPerHour = Program.TweetTimes.Count;
				if (ts.TotalMinutes > 60)
				{
					Program.TweetTimes.Clear();
				}
			}
			if (Program.TweetsPerHour > Program.ratelimit)
				returnVal = false;

			Console.WriteLine("{0} TPH", Program.TweetsPerHour);
			return returnVal;
		}

		private static string GetAssemblyVersion()
		{
			return System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
		}

		/// <summary>
		/// Util method to check and see if we need to upgrade the CampaignConfig files. Create a new CampaignConfig.json if needed
		/// </summary>
		/// <param name="memePath"></param>
		private static CampaignConfig GetCampaignConfig(string memePath)
		{
			string path = String.Format(@"{0}\config\CampaignConfig.json", memePath);
			CampaignConfig config = new CampaignConfig();

			// Try and read in the new CampaignConfig.json, create new if we need to
			if (!File.Exists(path))
			{
				//Set the vals of the things we want to be able to override per campaign
				config.Hashtags = new List<string>() { "MAGA", "Trump2024", "KAG" }; // Nothing to read so MAGA
				config.Mentions.Add("@memecannon17");
				config.DefaultHashtags.Add("#DefaultHashtag1");
				config.HashTagCount = 2;
				config.MaximumDelay = 1;
				config.MinimumDelay = 3;
			}
			else
				config = FileHelper.ReadJSONObjectFromFile(path).ToObject<CampaignConfig>();

			// Keep everything up to date
			FileHelper.WriteJSONToFile<CampaignConfig>(path, config);

			return config;
		}
	}
}
