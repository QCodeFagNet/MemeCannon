# MemeCannon
>.NET Core console app that tweets folders/campaigns of nonrepeating images + hashtags randomly every 1-3 minutes till you stop it, or it runs out of images.
Functions using the Twitter API. You MUST have a Twitter Developer account. They are free and you can apply here https://developer.twitter.com/en 
Everything is well below their spamlimits, 1-2 tweets per minute. No install, just run. I've been using it with MemeFarmers mega meme archives for a couple months. Turn it on and it just runs thru 500+ files until you stop it or it's done. It's running right now.

X:\MemeCannon\Bernie <- Campaign folder

X:\MemeCannon\Biden\hashtags <- campaign sub folder that contains configuration information

Builds out a dynamic menu and you choose your campaign/folder and default hashtags. 

Everything funnels thru your Twitter Developer acct. <5MB and is open source, except for my app keys.

# Install MemeCannon
To Install:
1) Download and install the .NET Core 3.1 runtime installer for your OS [.NET Core Runtime 3.1.5 - https://dotnet.microsoft.com/download/dotnet-core/3.1]
2) Download and unzip the release to your chosen install location IE: D:\MemeCannon. It even comes with a sample Comey 3 meme ammopack.
3) Add your Twitter App API Consumer Key and API Consumer Secret to the D:\MemeCannon\MemeCannon.dll.config.
4) Edit the default hashtags, HashTagCount and ImageSourceFolder in D:\MemeCannon\CannonConfig.json. 

The 'Default Hashtags' are hashtags that you can include with every meme tweet. Each 'campaign' has it's own set of hashtags that it will randomly select. Make sure to include a number equal or greater than the 'HashTagCount' in CannonConfig.json. It will include this number of hashtags from the campaign, plus the default hashtags, if enabled when running. So if you set 'HashTagCount' to 3 and have 4 hashtags in hashtags.txt, the system will add all the default hashtags if enabled, and randomly choose 3 of the 4 available hashtags. 

Set the ImageSourceFolder to a folder that includes many folders of memes ready for  the MemeCannon. Assuming your folder structure looks  something like this:

D:\MemeCannon\Ammo\Comey\hashtags
D:\MemeCannon\Ammo\Clapper\hashtags

Set the ImageSourceFolder value in D:\MemeCannon\CannonConfig.json to "D:\\MemeCannon\\Ammo\\"

5) Store Memes in their Campaign folder [Comey] below the 'Ammo' folder.
6) Edit the hashtags for each campaign in 'D:\MemeCannon\Ammo\Comey\hashtags\hashtags.txt'.
7) Compile and run.

If you have not run the app previously, it will prompt you to Authorize this app to tweet using your Twitter Account. Copy the PIN after authorizing on the Twitter Website and paste it into the MemeCannon prompt. It will save these values into the CannonConfig.json as the AccessToken and the AccessTokenSecret. Currently only supports a single Twitter Account. A D:\MemeCannon2 install with it's own CannonConfig.json, could operate using the same Ammo folders but a different Twitter account in parallel. Mind the rate limits that are associated with each Twitter App, only 300 tweets per 3 hrs. https://developer.twitter.com/en/docs/basics/rate-limits. Using the default MemeCannon config where it tweets every 1-3 minutes, you should end up with around 45 avg tweets per hour, but it could theoretically go as high as 60 TPH. The MemeCannon has a safety to shut down when you cross the 60 TPH limit to avoid having your app or user banned.

Choose your campaign by number and if you want to include the default hashtags (y/n).
Choose if you want to include mentions (y/n). DANGER! Overuse of this feature may cause your accounts to get banned. The system is hard coded to only include a single mention per tweet. 

Watch it work until it runs out of images or you stop it. 

End app by closing it down, or [ctrl-C]

# Config Files

* MemeCannon.dll.config - Stores your Twitter API Application Consumer Key and Secret. Once this is working you won't touch this file again.
* CannonConfig.json - Stores all the config information that the MemeCannon needs. The Twitter accounts access token and secret, the default hashtags, number of campaign hashtags to include, minimum and maximum delay between tweets, and the ImageSourceFolder that tells the app how to build the menu and where to find the images.
* filenames.json - Stores the filenames of all the images the MemeCannon has posted for this campaign. It won't post images that are in this file, just empty out the  array '[]' to start over. 
* hashtags.txt - Stores the '#' delimited list of hashtags to choose from for this campaign. IE: #ComeyKnew#BrennanKnew
* mentions.txt - Stores the '@' delimited list of twitter users to mention when tweeting.

# Q&A
>What is this thing and can I run it? 

.NET Core, should run cross platform with the right framework. It's just a small console app that looks in a specific folder for images, and posts them to twitter with hashtags. I organize memes in different folders X:\MemeCannon\Ammo\Biden, X:\MemeCannon\Ammo\Bernie and it builds a menu of the different folders. You select which folder/campaign to run. 

>Uhoh There's a fatal flaw in distributing this as a functioning client app, my Twitter Developer keys cannot be secured in it's current config. I have a couple potential solutions, but not enough time to work it out. It works now if you have a Twitter Dev account, or the motivation to get a free one. Part of the beauty of this is that it's using their own API's. They can't shut us all down without shutting down their API. Distributed is good.

>What about changing out your keys?

Sacrificial lamb technique? maybe, I figure @jack would just kill off the whole dev acct rather than kill a single Twitter app. There is really no way to completely secure the keys and distribute it as a client app that I'm aware of. 

>Size?

~5MB Total + your memes.

>Do i need to download just the release and unpack?

Ya unless you have Visual Studio all set up and ready to build. Install the .NET Core Runtime 3.1.5 for console applications. Extract to where you want it. Find and edit the MemeCannon.dll.config so that it contains your Twitter API ConsumerKey/secret. In the source it's app.config, but that changes to [Executable].config when you build. Configure the ImageSourceFolder in CannonConfig.json. Run MemeCannon.exe when you're all configured.

>How long does that usually take to get a Twitter Dev account?

I had to wait a couple weeks, but that included going back and forth on the description of my app with Twitter not meeting their guidelines. YMMV. I don't think it matters what you are describing as your app, because once you are in, you can make more apps (10?) without waiting around.

>How do I set it up to tweet 60 TPH (Tweets Per Hour)?

Set the D:\MemeCannon\CannonConfig.json\MinimumDelay and MaximumDelay to 1. If you wanted 1 TPH, set them both to 60.

>Can I edit hashtags.txt and mentions.txt while the app is running? I want to change my hashtags.

Go for it. Should be OK, but try and time it between tweets if you want to inject new hashtags or mentions.

# WARNING!
Be aware that once you add your Dev keys and run the MemeCannon it will encrypt the ConsumerKey and ConsumerSecret and update the MemeCannon.dll.config. This does NOT secure your Twitter keys, it only encrypts them for local safety. Not recommended to distribute these values.

CannonConfig.json : UserAccessToken and TokenSecret will be put in there by the app once you give it the OK. That's the keys that Twitter uses to auth your Twitter Account to the MemeCannon TwitterDevKeys. They are only valid when used with the original Twitter Dev keys. Clear these values to use a different Twitter account.
