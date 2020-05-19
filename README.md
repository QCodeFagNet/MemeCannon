# MemeCannon
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

If you have not run the app previously, it will prompt you to Authorize this app to tweet using your Twitter Account. Copy the PIN after authorizing on the Twitter Website and paste it into the MemeCannon prompt. It will save these values into the CannonConfig.json as the AccessToken and the AccessTokenSecret. Currently only supports a single Twitter Account.

Choose your campaign and if you want to include the default hashtags.

Note: [campaign]\hashtags\filenames.json keeps track of the filenames that the MemeCannon has posted previously. Just empty out the  array '[]' to start over. 
