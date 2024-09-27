# Star Trek Online Combat Real-Time Display

An application which tracks Star Trek Online combat data in real-time, and awards the player with Unreal Tournament style achievements.

![The main tab](Images/sto-realtime-128.png)

* [Overview](#overview)
* [Building](#building)
* [Screen Shots](#screen-shots)
* [Disclaimer](#disclaimer)

---
## Overview
This application tracks player kills, and awards the player with Unreal Tournament style achievements.  Achievements for multi-kills and killing sprees are displayed to the user as a flash of text in the middle of the screen, along with audio playback of the Unreal Tournament Announcer.
---
## Details 
The addon maintains a list of units attacked by the player while in combat. Each unit killed by the player while in combat is counted as a kill. When the player reaches certain consecutive kill achievements, a message is displayed to the screen, along with an audio file playback from the Unreal Tournament Announcer.

Your consecutive kill count is reset when you enter a new zone, or player death occurs. This puts you back at the bottom of the achievement list.

__Note(s):__
* If you attack a unit, then leave combat with the unit before they die, you won't get credit for the kill.
* Group unit kills don't count towards a players kill count, unless the player actually attacked the killed unit.

## Kill Scoring 

### Killing Sprees
Awards for every 5th kill without dying

* Killing Spree - 5 kills
* Rampage - 10 kills
* Dominating - 15 kills
* Unstoppable - 20 kills
* Godlike - 25 kills
* Wicked Sick - 30+ kills

### Multiple Kills
Awards for building up chains of kills in quick succession (4 seconds apart or less)

* Double Kill - 2 kills
* Multi Kill - 3 kills
* Mega Kill - 4 kills
* Ultra Kill - 5 kills
* Monster Kill - 6 kills
* Ludicrous Kill - 7 kills
* HOLY S**T - 8+ kills

---
## Building
The source in this repo is wrapped up in a Visual Studio 2022 solution. You should be able to clone this repo localy, then build and run from inside of Visual Studio.

You could also run the dotnet cli build and run commands from inside the zxeltor.StoCombatAnalyser.Interface project folder as well.

---

## Screen Shots
Figure 1: What the application overlays look like on top of STO. You can see the DOUBLE KILL achievement overlay high center screen, and the real-time combat data grid in the upper right.
![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/overlays_scaled.jpg)

Figure 2: The main application UI. Use this to configure achievment processing, and the overlay settings.
![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/ui.png)

---

## Disclaimer
This software and any related documentation is provided “as is” without warranty of any kind, either express or implied, including, without limitation, the implied warranties of merchantability, fitness for a particular purpose, or non-infringement. Licensee accepts any and all risk arising out of use or performance of Software

**Note:** We are not affiliated, associated, authorized, endorsed by, or in any way officially connected with the game Star Trek Online, or any of its subsidiaries or its affiliates. The official Star Trek Online website can be found at [https://www.playstartrekonline.com/](https://www.playstartrekonline.com/en/
