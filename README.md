![shit](OpenAddOnManager.Windows/Icon.png)

Welcome to the GitHub read me for Open Add-on Manager (OAM).

# For WoW Players

## What is OAM?

OAM is a Windows desktop app that will download and install World of Warcraft add-ons for you.
Using it is simple!
Just search and/or browse for add-ons you want to install and click the install add-on button.
OAM will monitor the add-on online and tell you when an update is available to install.
Or, you can just tell OAM to install updated add-ons for you automatically.

## How do I get OAM?

There are setup packages available for download on [the releases page](https://github.com/OpenAddOnManager/OpenAddOnManager/releases), my friend!

## What flavors of WoW does OAM support?

At present, these:

* Retail (currently Battle for Azeroth)
* PTR (currently Rise of Azshara 8.2.0)
* Classic Demo

We will release updates to OAM as more and different flavors of WoW are made available by Blizzard.

## How can I get something in OAM changed?

If you're a C# developer, fork the repo and take a crack at making the change yourself!
First, take a gander at [our contribution guidelines](CONTRIBUTING.md).
Then, submit your PR and we'll take a look.
If it makes sense, sure.
Why not?

If you're no developer but still an OAM fan, then you may visit [the issues board](https://github.com/OpenAddOnManager/OpenAddOnManager/issues).
Take a look to see if someone else has already mentioned what you're thinking of.
If they have, reply in their issue to cast your vote of support.
If no one has, post your own issue and see if others agree with you.

## What if I want to write a macOS version of OAM?

Who are we kidding?
A talented C# developer with serious Xcode and Interface Builder experience that wants to help out the players on Macs?
Pfft.
We don't know why we even bothered separating out the business logic into its own .NET Standard library in the foolish hope that someone like you would come along, for you surely do not exist...

# For WoW Add-On Authors

## How do I get my add-on listed in OAM?

OAM knows what add-ons are available by periodically downloading [addOns.json](https://github.com/OpenAddOnManager/OpenAddOnManager/blob/master/addOns.json) from its own GitHub repo.
If you would like your add-on to be available to players to install via OAM, fork this repo, add your repo to `addOns.json`, and then submit a PR.
Once we've established that it is, in fact, your add-on for which to control distribution, we will happily merge your PR and OAM users will have access to your add-on!

## How does OAM distribute add-ons?

OAM has a built-in Git client and works by cloning a Git repository for your add-on and then making pulls to check for updates.
Since OAM does support specifying the branch to clone, you may specify the repository you're currently using to develop, if you like.
Otherwise, you may choose to create a Git repository specifically for distributing via OAM.

## What are all the properties available in an `addOns.json` entry?

Here's an example that uses every property, with comments detailing what they do and what other options exist:

```json
{
    // ... other add-ons above this listing ...


    // each entry has its own unique identifier
    // OAM uses this to keep separate editions of even the same add-ons
    // this can happen because of different WoW flavors and pre-release versions of add-ons
    // we recommend using https://www.guidgenerator.com/online-guid-generator.aspx
    "5cfb8fd8-143c-4bbe-915e-a1ccd3762935": {

        // the name of your add-on, doesn't have to be unique
        "name": "Some Cool WoW Add-on",

        // a textual description of what your add-on does
        "description": "Does something really cool.\r\nYou'll love it!",

        // the flavor of WoW this entry is for
        // can be 'wow' (retail), 'wowt' (PTR), or 'wow_classic_beta' (for the Classic beta)
        // if not present, OAM assumes 'wow' (retail)
        "flavor": "wow",

        // a URL pointing to a JPEG, GIF, or PNG to server as your add-on's icon
        "iconUrl": "https://raw.githubusercontent.com/SomeAddOnDeveloper/SomeCoolWoWAddOn/master/icon.png",

        // the URL of the Git repository to clone
        "sourceUrl": "https://github.com/SomeAddOnDeveloper/SomeCoolWoWAddOn.git",

        // the branch of the Git repository to clone
        // if not present, OAM uses the remote's base branch
        "sourceBranch": "master",

        // whether or not this entry is for pre-release version of the add-on
        // let adventurous fans get more frequent updates from your test branch!
        // if not present, OAM assumes false
        "isPrereleaseVersion": false,

        // the name of the add-on's author
        "authorName": "Some Add-On Developer",

        // the email of the add-on's author
        // want email from your adoring fans? hmm, maybe not...
        // that's why this property is totally optional
        // if this is present, OAM will place an EMAIL AUTHOR button on your add-on's entry
        "authorEmail": "someaddondeveloper@gmail.com",

        // the homepage of the add-on's author
        // have a page where you list all your pojects and want people to visit?
        // if this is present, OAM will place an AUTHOR HOMEPAGE button on your add-on's entry
        "authorPageUrl": "https://github.com/SomeAddOnDeveloper",

        // the page at which players can make donations to you and your project
        // if this is present, OAM will place a DONATE button on your add-on's listing
        "donationsUrl": "https://www.patreon.com/SomeAddOnDeveloper",

        // the page at which players can get help using your add-on
        // if this is present, OAM will place a SUPPORT button on your add-on's listing
        "supportUrl": "https://github.com/SomeAddOnDeveloper/SomeCoolWoWAddOn/wiki"

    },


    // ... other add-ons below this listing ...
}
```

*Note: Comments aren't actually legal in JSON, so please do not submit PRs including commments inside of addOns.json.*

## What if I want to test distributing with OAM (or just do it privately among friends)?

You can create your own JSON file like `addOns.json`, upload it somewhere, click the menu button in OAM, click **LISTING SOURCES**, and then add the URL to your JSON file there.
After that point, the installation of OAM will also consult that file, and merge its entries in to the ones it makes available from the primary JSON file.
Then, test to your heart's content (and hopefully post an issue here if you have any).
And, you can also let your friends in on that.