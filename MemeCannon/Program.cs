using System;
//
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Newtonsoft.Json.Linq;
using System.Text;
using SysThread = System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;

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

		static void Main(string[] args)
		{
			try
			{
				Initialize();
				Engage();
				//GetTimelineTest();

				Program.CampaignTimer.Stop();
				TimeSpan ts = Program.CampaignTimer.Elapsed;
				string runTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
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

		private static void GetTimelineTest()
		{
			RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;

			RateLimit.QueryAwaitingForRateLimit += (sender, args) =>
			{
				Console.WriteLine($"Query : {args.Query} is awaiting for rate limits to be available!");
			};

			//var userId = 159053980;
			var userId = "realDonaldTrump";
			UserTimelineParameters userTimelineParameters = new UserTimelineParameters() 
			{
				SinceId = 1262787562206367744
			};

			//var lastTweets = Timeline.GetUserTimeline(userId, 200).ToArray();
			var lastTweets = Timeline.GetUserTimeline(userId, userTimelineParameters).ToArray();

			var allTweets = new List<ITweet>(lastTweets);
			var beforeLast = allTweets;

			while (lastTweets.Length > 0 && allTweets.Count <= 3200)
			{
				var idOfOldestTweet = lastTweets.Select(x => x.Id).Min();
				Console.WriteLine($"Oldest Tweet Id = {idOfOldestTweet}");

				var timelineRequestParameters = new UserTimelineParameters
				{
					// We ensure that we only get tweets that have been posted BEFORE the oldest tweet we received
					MaxId = idOfOldestTweet - 1,
					MaximumNumberOfTweetsToRetrieve = allTweets.Count > 3000 ? (3200 - allTweets.Count) : 200
				};

				lastTweets = Timeline.GetUserTimeline(userId, timelineRequestParameters).ToArray();
				allTweets.AddRange(lastTweets);
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
				while ((!int.TryParse(response, out key) && (! response.ToLower().Equals("x", StringComparison.InvariantCulture))))
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

				Console.Write("Include default Hashtags? (y/n) : ");
				bool addDefaultHashtags = GetAddData();

				Console.Write("Include User Pings? (y/n) : ");
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
			string jsonpath = String.Format(@"{0}\hashtags\filenames.json", path); //TODO Could monitor for a change?
			postedFileNames = FileHelper.ReadJSONFromFile(jsonpath).ToObject<List<string>>().ToList();

			existingFileNames = Directory.EnumerateFiles(path)
				.Where(n => !postedFileNames.Contains(n)) // Don't post anything we've already posted
				.Where(n => imageFileTypes.Any(ext => ext == Path.GetExtension(n).ToLower())) // filter by image ext
				.OrderBy(n => Guid.NewGuid()).ToList(); // random order

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
				if (addPings) { AddMentions(sb, path); }
				AddHashtags(sb, path, addDefaultHashtags); // reloads every time so we can inject new hashtags

				//DOIT
				bool twatted = TweetWithImage(sb.ToString(), filename);
				if (twatted)
				{
					// Add to the list of what we've already twatted and show the user
					postedFileNames.Add(filename);
					FileHelper.WriteJSONToFile(String.Format(@"{0}\hashtags\filenames.json", path), postedFileNames);
					string fn = Path.GetFileName(filename);
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(String.Format("{0}", fn));
					Console.ResetColor();

					//Update the TweetPerHour data
					rateLimiterGood = UpdateTweetsPerHour();

					counter++;
					if (counter < existingFileNames.Count)
					{
						//get a random pause time between configured min and max, in millisec
						int min = (Program.CannonCfg.MinimumDelay * 60000);
						int max = Program.CannonCfg.MaximumDelay * 60000;
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

		private static void Initialize()
		{
			Program.CampaignTimer = new Stopwatch();
			Program.CampaignTimer.Start();
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("MemeCannon {0}", DateTime.Now);
			Console.ResetColor();
			DisplayFrameworkName();

			EncryptSettings(); // Encrypt if needed

			Program.twitterConsumerKey = ConfigurationManager.AppSettings["twitterConsumerKey"];
			Program.twitterConsumerSecret = ConfigurationManager.AppSettings["twitterConsumerSecret"];

			if ((Program.twitterConsumerKey.Length == 0) || (Program.twitterConsumerSecret.Length == 0))
				throw new Exception("Invalid Twitter API Keys. Check your app config.");

			Program.CannonCfg = FileHelper.ReadJSONObjectFromFile("CannonConfig.json").ToObject<CannonConfig>();

			//Check to see if we have already generated an accessToken and accessTokenSecret
			// If not, generate and save so we don't have to do it every time
			if (Program.CannonCfg.AccessToken.Length == 0) 
			{ 
				UpdateUserSettings();
			}
			// Save this again to make sure everything is up to date
			FileHelper.WriteJSONToFile("CannonConfig.json", Program.CannonCfg.ToJson());

			Program.TweetTimes = new List<DateTime>();

			Auth.SetUserCredentials(Program.twitterConsumerKey, Program.twitterConsumerSecret, Program.CannonCfg.AccessToken, Program.CannonCfg.AccessTokenSecret);
		}

		/// <summary>Prompt the user to Authorize the MemeCannon to make tweets</summary>
		private static void UpdateUserSettings()
		{
			ITwitterCredentials appCreds = Auth.SetApplicationOnlyCredentials(twitterConsumerKey, twitterConsumerSecret);
			
			// This method execute the required webrequest to set the bearer Token
			Auth.InitializeApplicationOnlyCredentials(appCreds);

			// Create a new set of credentials for the application.
			TwitterCredentials appCredentials = new TwitterCredentials(twitterConsumerKey, twitterConsumerSecret);

			IAuthenticationContext authenticationContext = AuthFlow.InitAuthentication(appCredentials);
			ProcessStartInfo psi = new ProcessStartInfo(authenticationContext.AuthorizationURL)
			{
				UseShellExecute = true,
				Verb = "open"
			};
			// Causes a WebBrowser to open and the user needs to OK app access
			Process.Start(psi);

			// Ask the user to enter the pin code given by Twitter
			Console.WriteLine("Enter PIN Code given by Twitter to continue:");
			string pinCode = Console.ReadLine();

			// With this pin code it is now possible to get the credentials back from Twitter
			ITwitterCredentials userCredentials = AuthFlow.CreateCredentialsFromVerifierCode(pinCode, authenticationContext);

			// Save off the accessToken and accessTokenSecret
			Program.CannonCfg.AccessToken = userCredentials.AccessToken;
			Program.CannonCfg.AccessTokenSecret = userCredentials.AccessTokenSecret;

			FileHelper.WriteJSONToFile("./CannonConfig.json", Program.CannonCfg.ToJson());
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
			Console.WriteLine("\nSelect target:");
			foreach(KeyValuePair<int, string> kvp in dirs)
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

		private static Boolean TweetWithImage(string text, string imgPath)
		{
			if (imgPath.Length > 0)
			{
				if (File.Exists(imgPath))
				{
					byte[] imgdata = System.IO.File.ReadAllBytes(imgPath);
					Tweetinvi.Models.IMedia media = Upload.UploadBinary(imgdata);
					if (media.IsReadyToBeUsed)
					{
						Tweetinvi.Models.ITweet tweet = Tweet.PublishTweet(text, new PublishTweetOptionalParameters
						{
							Medias = { media }
						});
						if (tweet != null) return true;
					}
				}
			}
			return false;
		}

		/// <summary>Method that reads in the 'hashtags.txt' delimited file and adds them to the passed in StringBuilder</summary>
		/// <param name="sb"></param>
		/// <param name="memePath"></param>
		/// <param name="addDefaultHashtags"></param>
		private static void AddHashtags(StringBuilder sb, string memePath, bool addDefaultHashtags)
		{
			int hashtagCount = Program.CannonCfg.HashTagCount;
			Random rnd = new Random();
			string path = String.Format(@"{0}\hashtags\hashtags.txt", memePath);

			if ((addDefaultHashtags) && (Program.CannonCfg.DefaultHashtags.Count > 0))
			{
				Program.CannonCfg.DefaultHashtags.ForEach(h => sb.Append(String.Format("{0} ", h)));
			}

			// get the list of hashtags
			string[] sep = new string[] { "#" };
			List<string> hashtags = ReadListFromFile(path, sep);
			if (hashtags.Count.Equals(0))
				hashtags = new List<string>() { "MAGA", "Trump2020", "KAG" }; // Nothing to read so MAGA

			if (hashtags.Count > 0)
			{
				// get a random set of hashtags
				for (int eye = 0; eye < hashtagCount; eye++)
				{
					bool ok = false;
					int count = 0;
					while (!ok)
					{
						int id = rnd.Next(0, hashtags.Count);
						string hash = hashtags[id];
						//Dont add a duplicate
						if (!sb.ToString().Contains(hash, StringComparison.InvariantCulture))
						{
							// Put the hash back in since the split took it out
							sb.Append(String.Format("#{0} ", hash));
							ok = true;
						}

						// Bail if we've tried everything twice
						if (count == (hashtagCount * 2)) { return; }
						count++;
					}
				}
			}
		}

		/// <summary>
		/// Method that reads in the 'mentions.txt' delimited file and adds them to the passed in StringBuilder
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="memePath"></param>
		private static void AddMentions(StringBuilder sb, string memePath)
		{
			int mentionsCount = 1; // Try and keep under the spammeradar, only 1 per tweet
			Random rnd = new Random();
			string path = String.Format(@"{0}\hashtags\mentions.txt", memePath);

			// get the list of mentions
			string[] sep = new string[] { "@" };
			List<string> mentions = ReadListFromFile(path, sep);

			if (mentions.Count > 0)
			{
				// get a random set of mentions
				for (int eye = 0; eye < mentionsCount; eye++)
				{
					bool ok = false;
					int count = 0;
					while (!ok)
					{
						int id = rnd.Next(0, mentions.Count);
						string mention = mentions[id];
						//Dont add a duplicate
						if (!sb.ToString().Contains(mention, StringComparison.InvariantCulture))
						{
							// Put the @ back in since the split took it out
							sb.Append(String.Format(".@{0} ", mention));
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

		/// <summary>Utility method to read delimited lists from a local txt file</summary>
		/// <param name="path">Path to the file</param>
		/// <param name="sep">the sep to use</param>
		/// <returns>List of string values</returns>
		private static List<string> ReadListFromFile(string path, string[] sep)
		{
			if (File.Exists(path))
				return FileHelper.ReadTextFromFile(path).Split(sep, StringSplitOptions.RemoveEmptyEntries).ToList();
			else
			{
				//Save them a new one
				Console.WriteLine(@"No mentions for this campaign {0} : Use format @username1@userName2", path);
				FileHelper.WriteJSONToFile(path, String.Empty);
				return new List<string>();
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
	}
}
