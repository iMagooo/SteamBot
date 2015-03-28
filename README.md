This modified SteamBot project aims to help those who farm TF2 items across many different Steam accounts by providing an easy to use item collection system.

**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 8 contributors have all added to the bot.  The bot is publicly available under the MIT License. Check out [LICENSE] for more details.

There are several things you must do in order to get SteamBot working:

1. Download the source.
2. Compile the source code.
3. Configure the bots (username, password, etc.). You must specify at least 1 Collecting bot! (See wiki for more details)
4. *Optionally*, customize the behaviour by changing the source code.

## Getting the Source

Retrieving the source code should be done by following the [installation guide] on the wiki. The install guide covers the instructions needed to obtain the source code as well as the instructions for compiling the code.

## Configuring the Bot

See the [configuration guide] on the wiki. This guide covers configuring a basic bot as well as creating a custom user handler.

## Changes from original SteamBot

You may notice some changes from the original SteamBot framework. The most notable change being the removal of using separate processes for each running bot. I made this decision to simplify Bot-to-BotManager communication. Due to the lack of modularity in the original SteamBot framework I have had to chop and change the code, rather than building upon it.
If you already use SteamBot and wish to utilise this project be sure to copy across /Bin/Debug/sentryfiles/ OR /Bin/Release/sentryfiles/ to ensure you do not receive a no-trade period on Steam for this system.

## More help?
If it's a bug, open an Issue; if you have a fix, read [CONTRIBUTING.md] and open a Pull Request. Please use the issue tracker only for bugs reports and pull requests. 

## Wanna Contribute?
Please read [CONTRIBUTING.md].


   [installation guide]: https://github.com/iMagooo/SteamBot/wiki/Installation-Guide
   [CONTRIBUTING.md]: https://github.com/iMagooo/SteamBot/blob/master/CONTRIBUTING.md
   [LICENSE]: https://github.com/iMagooo/SteamBot/blob/master/LICENSE
   [configuration guide]: https://github.com/iMagooo/SteamBot/wiki/Configuration-Guide
   [usage guide]: https://github.com/iMagooo/SteamBot/wiki/Usage-Guide
