# lethal-company-prop-hunt
A lethal company mod that adds a prop hunt gamemode.

Assets were created using unity 2022.3.18f1

For developing, you need to install Unity BepinEx Netcode Patcher found here https://github.com/EvaisaDev/UnityNetcodePatcher/releases/tag/v3.3.4 . Simply run
dotnet tool install -g Evaisa.NetcodePatcher.Cli
You need to have .net runtime v8 installed and on your path.

Props win if all hunters die or the clock reaches midnight. Hunters win if all props are eliminated. No mosters spawn, all flashlights/walkie-talkies are infinite energy, and props cannot exit the map.

By default the following keybinds exit:
| **Key** 	| **Description**                                                                                       	| **Prop Only** 	|
|---------	|-------------------------------------------------------------------------------------------------------	|---------------	|
| k       	| Lock rotation when a prop, carries across changing prop.                                              	| Yes           	|
| z       	| Fire off a random taunt, if you are a prop, this resets your auto taunt. This has a 5 second cooldown 	| No            	|
| G       	| As a prop you can use this to return to a crewmate                                                    	| Yes           	|

For key mapping, we use https://github.com/Rune580/LethalCompanyInputUtils which provides key listening and rebinding.

Taunts were taken from https://github.com/MrLuckyGamer/prophuntenhanced/tree/master the enhanced version of the classic gmod gamemode

Implementation of third person was taken from https://github.com/bakerj76/LCThirdPerson

Audio manager Implementation taken from https://github.com/cmooref17/Lethal-Company-TooManyEmotes/blob/main/TooManyEmotes/Audio/AudioManager.cs

Compatibility with More Company mod https://thunderstore.io/c/lethal-company/p/notnotnotswipez/MoreCompany/.

The gamemod has many configration options outlined below:
| **Config**          	| **Default** 	| **Server Side** 	| **Description**                                                                                               	| **Min** 	| **Max** 	|
|---------------------	|-------------	|-----------------	|---------------------------------------------------------------------------------------------------------------	|---------	|---------	|
| Number of Props     	| 1           	| Yes             	| Number of props, if exceeds number of players, 1 is used.                                                     	| 1       	| 3       	|
| Force Taunt         	| true        	| Yes             	| Whether or not to force the player to taunt after a period of time if they have not already done it manually. 	| false   	| true    	|
| Taunting Interval   	| 30          	| Yes             	| Forces the player to taunt after a number of seconds.                                                         	| 1       	| 60      	|
| Map Size Multiplier 	| 1           	| Yes             	| This tells the level generator how big you want the maps to be.                                               	| 1       	| 10      	|
| Scrap Multiplier    	| 20          	| Yes             	| The multiplier on how much scrap is generated for the given level.                                            	| 1       	| 100     	|
| Time Multiplier     	| 2           	| Yes             	| The multiplier on how fast time passes, 1 being a normal round. <1 is slower rounds, >1 is faster rounds.     	| 0.00001 	| 100     	|
| Allow Keys          	| true        	| Yes             	| Whether or not to include the key as a scrap item because its kinda really small and OP.                      	| false   	| true    	|
| DamageScaling       	| 2           	| Yes             	| How damage is scaled based on prop weight, 1 is disabled.                                                     	| 1       	| 100     	|
| Taunt Volume        	| 0.3         	| No              	| The volume you want your taunts to be at, where 1 is 100% volume.                                             	| 0.00001 	| 1       	|
| ShowCursor          	| true        	| No              	| Whether to show a cursor in third person or not                                                               	| false   	| true    	|
| CameraOffset        	| 0, -1, -2   	| No              	| The camera offset for third person                                                                            	| N/A     	| N/A     	|