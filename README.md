# MemeCannon
Create and post all images in a folder to Twitter with a predefined set of hashtags automatically, using the Twitter API.

To Install:
1) Add your Twitter API Consumer Key and API Consumer Secret to the app.config.
2) Edit the default hashtags, HashTagCount and ImageSourceFolder in CannonConfig.json. 

The 'Default Hashtags' are hashtags that you can include with every meme tweet. Each 'campaign' has it's own set of hashtags that it will randomly select. Make sure to include a number equal or greater than the 'HashTagCount' in CannonConfig.json. It will include this number of hashtags from the campaign, plus the default hashtags, if enabled when running. 

Set the ImageSourceFolder to a folder that includes many folders of memes ready for  the MemeCannon. Assuming your folder structure looks  something like this:
D:\\MemeCannon\Ammo\Comey\hashtags
D:\\MemeCannon\Ammo\Clapper\hashtags

Set the ImageSourceFolder value in CannonConfig.json to "D:\\MemeCannon\\Ammo\\"

3) Store Memes in their Campaign folder [Comey]
4) Edit the hashtags for each campaign in 'D:\\MemeCannon\Ammo\Comey\hashtags\hashtags.txt'.
5) Compile and run.

If you have not run the app previously, it will prompt you to authorize this app to tweet using your Twitter account. Copy the PIN after authorizing on the twitter Website and paste it into the MemeCannon prompt. It will save these values into the CannonConfig.json as the AccessToken and the AccessTokenSecret. Currently only supports a single twitter account.

Choose your campaign and if you want to include the default hashtags.

Note: [campaign]\hashtags\filenames.json keeps track of the filenames that the MemeCannon has posted previously. Just empty out the  array '[]' to start over. 


# Q&A
>.NET Core, should run cross platform

It's just a small console app that looks in a specific folder for images, and posts them to twitter with hashtags. I organize memes in different folders X:\MemeCannon\Ammo\Biden, X:\MemeCannon\Ammo\Bernie and it builds a menu of the different folders. You select which folder/campaign to run

>I've come across a fatal flaw in distributing this as a client app, my Twitter Developer keys cannot be secured in it's current config.

I have a couple potential solutions, but not enough time to work it out. It works now if you have a Twitter Dev account.
 
>what about chanigng out your keys?

Sacrificial lamb technique? maybe, I figure @ jack would just kill off the whole acct. It's NP to change out the keys and there's directions at the github. It even comes with a sample comey 3 meme ammopack.

>Get my own Twiiter Dev account? how long does that usually take.

I had to wait a couple weeks, but that included going back and forth on the description of my app with Twitter not meeting their guidelines. YMMV. I don't think it matters what you are describing as your app, because once you are in, you can make more apps (10?) without waiting around.

>size

4MB Total + your memes.
 
>do i need to download just the release and unpack?

Ya unless you have Visual Studio all set up and ready to build. Extract to where you want it. Find and edit the MemeCannon.dll.config so that it contains your Twitter API ConsumerKey/secret. In the source it's app.config, but that changes to MemeCannon.dll.config when you build. Run MemeCannon.exe when you're all configured.
  
# WARNING!
Be aware that once you add your Dev keys and run the MemeCannon it will encrypt the ConsumerKey and ConsumerSecret and update the MemeCannon.dll.config. This would then be a theoretically distributable version that works, using encrypted values from the MemeCannon.dll.config file. These values are not to be considered secure, only encrypted.
 
CannonConfig.json : UserAccessToken and TokenSecret will be put in there by the app once you give it the OK. That's the keys that Twitter uses to auth your Twitter Account to the MemeCannon TwitterDevKeys
