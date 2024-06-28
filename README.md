# Crowigor's Base Manager

![Crowigor's Base Manager](thumb.png)

## Script Description
The script is a set of useful functions for automating database management.

## Inventory Manager
The function searches for items in all available blocks and transfers them to the specified containers.

## Assembling and Disassembling Items
The function periodically checks the quantity of specified items and, if the quantity of items (including production queues) is less (in the case of assembly) or less (in the case of disassembly) than the specified quantity, adds the item to the queue.

Additionally, in the case of disassembly, the function checks the assembler queue and transfers the necessary items to the inventory.

## Refinery Management
The function periodically transfers all ore from the refinery to containers, then adds only the ore specified in the settings for processing.

## Item Collection
The function extracts the specified items (or all) from the containers and tools of the connected grid and transfers them to the connector, and then, when the connector is full, to the specified containers.

## Stopping Drones
The function checks the fullness of the containers specified in the connector settings and if the percentage of their filling is greater than the specified one (90% by default), it turns off the block specified in the settings.

## Displaying on Displays
The function allows displaying the specified information on displays.

## Actions
The script allows performing various actions depending on the arguments.

## Guide
A full description of the functions, as well as instructions for their use, can be viewed in the manual:
- [English Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3119211195)
- [Russian Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3030970526)

## Languages
Initially, the script was written with RU localization in mind; all item names were taken from the game files. Additionally, the script supports the ability to specify the names of objects in EN.

## Mod Support
- [Paint Gun](https://steamcommunity.com/sharedfiles/filedetails/?id=500818376)
- [Eat. Drink. Sleep. Repeat!](https://steamcommunity.com/sharedfiles/filedetails/?id=2547246713)
- [Plant and Cook](https://steamcommunity.com/sharedfiles/filedetails/?id=2570427696)
- [AiEnabled](https://steamcommunity.com/sharedfiles/filedetails/?id=2596208372)
- [Personal Shield Generators](https://steamcommunity.com/sharedfiles/filedetails/?id=1330335279)

## Developed Using MDK-SE
This script was developed using [MDK-SE](https://github.com/malware-dev/MDK-SE/).