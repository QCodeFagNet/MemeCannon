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
		private static CannonConfig CannonCfg { get; set; }

//		private static string BasePath = @"F:\Memebank\";

		private static string twitterConsumerKey { get; set; }
		private static string twitterConsumerSecret { get; set; }

		static void Main(string[] args)
		{
			Initialize();
			Engage();
			//Test();
		}

		/// <summary>
		/// Do some tests with logged in users
		/// </summary>
		private static void Test()
		{
			//jackass.wtf
			string ck = "Lgo65DM9RnhMMelpJ52s7Bant";
			string cs = "DVkV7VKBR5jmZEJk5BDCfGca52SWIPSpJScAxVi3C0iyg2DRPJ";
			// AccessToken and secret after we've first granted access to the app, otherwise ya gotta do it every time. After the first time, these values remain static. Save locally for ease of use.
			string zAT = "4830873281-wQ85l83bmQJzqtmqfgLczeMV2CvmTxq7qt2320u";
			string zATS = "i0clTXE6jtsaLuEpeWaA3Wfhh4sYTAEq4cE8XDosZpJSv";

			Auth.SetUserCredentials(ck, cs, zAT, zATS);
			IAuthenticatedUser authenticatedUser = User.GetAuthenticatedUser();

			ITwitterCredentials appCreds = Auth.SetApplicationOnlyCredentials(ck, cs);
			// This method execute the required webrequest to set the bearer Token
			bool wtf = Auth.InitializeApplicationOnlyCredentials(appCreds);

			// Create a new set of credentials for the application.
			TwitterCredentials appCredentials = new TwitterCredentials(twitterConsumerKey, twitterConsumerSecret);
			//TwitterCredentials appCredentials2 = new TwitterCredentials(ck, cs);

			IAuthenticationContext authenticationContext = AuthFlow.InitAuthentication(appCredentials);
			ProcessStartInfo psi = new ProcessStartInfo(authenticationContext.AuthorizationURL)
			{
				UseShellExecute = true,
				Verb = "open"
			};

			//Process.Start(authenticationContext.AuthorizationURL);
			Process.Start(psi);

			// Ask the user to enter the pin code given by Twitter
			string pinCode = Console.ReadLine();

			// With this pin code it is now possible to get the credentials back from Twitter
			ITwitterCredentials userCredentials = AuthFlow.CreateCredentialsFromVerifierCode(pinCode, authenticationContext);

			// Save off the accessToken and accessTokenSecret
			string at = userCredentials.AccessToken;
			string ats = userCredentials.AccessTokenSecret;
			string bt = userCredentials.ApplicationOnlyBearerToken;
			bool booltest1 = userCredentials.AreSetupForApplicationAuthentication();
			bool booltest2 = userCredentials.AreSetupForUserAuthentication();


			// Use the user credentials in your application
			Auth.SetCredentials(userCredentials);
			IAuthenticatedUser authenticatedUser2 = User.GetAuthenticatedUser();

		}

		/// <summary>Load and fire the MemeCannon</summary>
		private static void Engage()
		{
			Dictionary<int, string> filePaths = DisplayMenu();
			string response = Console.ReadLine();
			Console.WriteLine("Include default Hashtags? (y/n)");
			string addDefault = Console.ReadLine();
			bool addDefaultHashtags = false;
			if (addDefault.ToLower().Equals("y", StringComparison.InvariantCulture))
				addDefaultHashtags = true;

			if (!response.ToLower().Equals("x", StringComparison.InvariantCulture))
			{
				string memePath = GetMemePath(response, filePaths);
				if (memePath.Length > 0)
				{
					// GTG
					Fire(memePath, addDefaultHashtags);
				}
			}
			else
			{
				Console.WriteLine("[Exit]");
				return;
			}
		}

		private static void Initialize()
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("MemeCannon");
			Console.ResetColor();
			DisplayFrameworkName();

			EncryptSettings(); // Encrypt if needed

			//Check to see if we have already generated an accessToken and accessTokenSecret
			// If not, generate and save so we don't have to do it every time
			twitterConsumerKey = ConfigurationManager.AppSettings["twitterConsumerKey"];
			twitterConsumerSecret = ConfigurationManager.AppSettings["twitterConsumerSecret"];

			Program.CannonCfg = FileHelper.ReadJSONObjectFromFile("CannonConfig.json").ToObject<CannonConfig>();

			if (Program.CannonCfg.AccessToken.Length == 0) 
			{ 
				UpdateUserSettings();
				FileHelper.WriteJSONToFile("CannonConfig.json", Program.CannonCfg.ToJson());
			}

			Auth.SetUserCredentials(twitterConsumerKey, twitterConsumerSecret, Program.CannonCfg.AccessToken, Program.CannonCfg.AccessTokenSecret);
		}

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
			//Remove the 'hashtags' directory since we don't want to process that.
			Dictionary<int, string> dirs = di.GetDirectories().Where(n => !n.Name.Equals("hashtags")).Select(p => p.FullName).OrderBy(n => n).ToDictionary(p => key++);
			// Now output the menu
			Console.WriteLine("Select target:");
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

		private static string GetMemePath(string resp, Dictionary<int, string> paths) 
		{
			// Try and parse this into a number
			int key = -1;
			if (int.TryParse(resp, out key))
				return paths[key];
			else
				return string.Empty;
		}

		private static void Fire(string path, bool addDefaultHashtags)
		{
			List<string> hashtags = new List<string>();
			List<string> existingFileNames = new List<string>();
			List<string> postedFileNames = new List<string>();
			Random rnd = new Random();
			string jsonpath = String.Format(@"{0}\hashtags\filenames.json", path); //TODO Could monitor for a change?
			postedFileNames = FileHelper.ReadJSONFromFile(jsonpath).ToObject<List<string>>().ToList();

			existingFileNames = Directory.EnumerateFiles(path).Where(n => !postedFileNames.Contains(n)).OrderBy(n => Guid.NewGuid()).ToList();

			int counter = 0;
			foreach (string filename in existingFileNames)
			{
				StringBuilder sb = new StringBuilder();
				AddHashtags(sb, path, addDefaultHashtags); // reloads every time so we can inject new hashtags

			   //DOIT
				bool twatted = TweetWithImage(sb.ToString(), filename);
				if (twatted)
				{
					// Add to the list of what we've already twatted
					postedFileNames.Add(filename);
					FileHelper.WriteJSONToFile(String.Format(@"{0}\hashtags\filenames.json", path), postedFileNames);
					string fn = Path.GetFileName(filename);
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine(String.Format("{0}", fn));
					Console.ResetColor();

					counter++;
					if (counter < existingFileNames.Count)
					{
						//get a random pause time between 1-10 (600000) minutes
						int maxmins = 200000; // 3mins
						int mins = 1;
						int millisec = (mins * 60000); //120000
						int sleep = rnd.Next(millisec, maxmins);
						Console.WriteLine(String.Format("Memecannon sleeping [{0}] {1}", (sleep / 60000), DateTime.Now));
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

		private static Tweetinvi.Models.ITweet TweetTest(string txt)
		{
			//Auth.SetUserCredentials(twitterConsumerKey, twitterConsumerSecret, accessToken, accessTokenKey);
			return Tweet.PublishTweet(txt);
		}

		private static Boolean TweetWithImage(string text, string imgPath)
		{
			if (imgPath.Length > 0)
			{
				//string response = UploadImage(imgPath).Result;
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
		
		private static List<string> ReadHashtags(string path)
		{
			string[] sep = new string[] { "#" };
			// get the list of hashtags
			if (File.Exists(path))
				return FileHelper.ReadTextFromFile(path).Split(sep, StringSplitOptions.RemoveEmptyEntries).ToList();
			return new List<string>() { "MAGA", "Trump2020", "KAG" }; // Nothing to read so MAGA
		}
		
		private static void AddHashtags(StringBuilder sb, string memePath, bool addDefaultHashtags)
		{
			int hashtagCount = Program.CannonCfg.HashTagCount;
			Random rnd = new Random();
			string path = String.Format(@"{0}\hashtags\hashtags.txt", memePath);
			
			//if (addDefaultHashtags)
			//	sb.Append("#Trump2020 #QAnon "); // Add in the one tag we always want

			if ((addDefaultHashtags) && (Program.CannonCfg.DefaultHashtags.Count > 0))
			{
				Program.CannonCfg.DefaultHashtags.ForEach(h => sb.Append(h));
			}
			
			// get the list of hashtags
			List<string> hashtags = ReadHashtags(path);
			// get a random set of [3] hashtags
			for (int eye = 0; eye < hashtagCount; eye++)
			{
				bool ok = false;
				while (!ok)
				{
					int id = rnd.Next(0, hashtags.Count);
					string hash = hashtags[id];
					//Dont add a duplicate
					if (!sb.ToString().Contains(hash, StringComparison.InvariantCulture))
					{
						sb.Append(String.Format("#{0} ", hash));
						ok = true;
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
		
	}
}
