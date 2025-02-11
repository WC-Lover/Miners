# Miners

Miners - is a Top-down Multiplayer Game made with ```Unity 6``` and ```Netcode for GameObjects```.

Created for friends to have several joyful hours together and incredible memories to recall.

To navigate players from Main Menu to Lobby and establish connection between players in Lobby, Miners import [SceneSDK](https://github.com/WC-Lover/SceneSDK).

## SceneSDK
### Main Menu

Basic UI setup allowing player to change controlls(if needed), and direct to Lobby Menu. 

### Lobby Menu

SceneSDK provides crucial for multiplayer games possibility to:
* Create Lobby
* Quick Join Lobby
* Join By Lobby Code 
* Join via Lobby List

### Lobby

Lobby shows connected players and their status which can be changed by pressing ```Ready``` button.

Host has possibility to kick players.

Lobby proceeds to ```Game Scene``` when all players are marked as ```Ready```.

## Game Description

### Gameplay

Right after Main Menu players are presented with a choice, to select ```Start Bonus``` and ```Permanent Bonus```.

* ```Start Bonus``` will affect ```Player Building``` immediately at the start of the game, by increasing the selected attribute(eg. AttackStrength, Speed, GatherStrength)
* ```Permanent Bonus``` is goint to affect ```Player Building``` during the whole game session. Each ```Building``` level-up, ```Permanent Bonus``` is applied to the sellected attribute. 

Game provides player with control over attributes development and ordering ```Units``` where to go. Providing different strategic opportunities:

* Focus on gatherint resources and level-up main building to get stronger ```Miners```, then conquer the surrounding.
* Prioritize claiming ```NeutralBuildings``` to get more ```Units``` available for spawn.
* Try to gather ```Holy Resource``` while others are buisy fighting each other over ```Neutral Buildings``` or ```Resources```.

As ```Unit``` eventually becomes independant, 'luck' plays noticable role in the gameplay, which can lead to funny/raging scenarios. 

### Building

There are 2 types of buildings ```Neutral/Player```.

Player is represented by ```PlayerBuilding```, game supports up to 4 players.

Only ```NeutralBuilding``` can be occupied during the game.

Occupied building(```PlayerBuilding``` is considered occupied from start to end of the game) spawns ```Units```(Miners).

Building can be updated by ```Units```, by bringing in resources gathered from map.

### Unit(Miner)

Player sets the first destination for ```Unit``` by pressing anywhere on the map.

After ```Unit``` has arrived to destination, it starts to search for nearest interaction.

```Unit``` has following order of interaction pripority:
* ```Unit```. (Only other players' ```Units``` can be attacked, fellow ```Units``` are being ignored)
* ```Building```. (Only ```NeutralBuilding``` can be claimed)
* ```Resource```. (If nothings else left, search for closest resource)

If ```Unit``` has ran out of ```carryingWeight``` or ```stamina``` it returns to base where it was spawned, to restore ```stamina``` and update ```Building``` with resources.

After ```Unit``` has completed the first circle of actions -> Approach(Start destination, set by player)/Search(find nearest interaction)/Interact, player doesn't have any controll over ```Unit```.

```Unit``` starts to repeat the cycle -> Search/Approach/Search/Interact. Secondary Search was added to avoid Interaction with Despawned objects. 

### Resources

Resource has 3 types:
* ```Common```. (XP only)
* ```Rare```. (More XP and also provides a temporary buff for the ```Unit```)
* ```Holy```. (Player who has gathered the most of ```Holy Resource``` by the time it is gathered completely - wins)

### Resource Spawner

Dinamically spawns equal amount/rarity of ```Resources``` for each player, to provide fair gameplay experience. 
