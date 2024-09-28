# Star Trek Online Real-Time Combat Overlay [![GitHub release (latest by date)](https://img.shields.io/github/v/release/zxeltor/zxeltor.StoCombat.Realtime)](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/releases/latest)

An application which tracks Star Trek Online combat data in real-time, and awards the player with Unreal Tournament style achievements.

![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/overlays_scaled.jpg)
**Figure:** What the application overlays look like on top of STO. You can see the DOUBLE KILL achievement overlay high center screen, and the real-time combat data grid in the upper right.

* [Overview](#overview)
* [Download](#download)
* [Details](#details)
* [Building](#building)
* [Screen Shots](#screen-shots)
* [Disclaimer](#disclaimer)

## Overview
The application provides real-time combat details for Star Trek Online.  The application provides a semi transparent overlay which provides a data grid of available combat statistics for all players in combat.

On top of the combat statistics, another fun feature was added. The application tracks player kills, and awards the player with Unreal Tournament style achievements. Achievements for multi-kills and killing sprees are displayed to the user as a flash of text in the middle of the screen, along with audio playback of the Unreal Tournament Announcer.

This is a companion project of [STOCombatAnalyzer](https://github.com/zxeltor/STOCombatAnalyzer). STOCombatAnalyzer provides a more detailed analysis of past combat log data.

## Download
The installer uses Click-Once technology. This means you should only have to download and install this app once. The app checks with the server each time it's launched, and gives the user the option to automatically download and install available updates. This saves the user from constantly having to manually download and install newer versions.

[Download](https://starfleet.engineer/StoCombatRealtime/Installer/setup.exe)

**Notes:** 
- The installer is signed with a test certificate, so Windows will pop up warnings about the installer not be trusted.
- If you don't have the .NET 8 runtime installed, it will install it for you.

## Details 

### The main ui
![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/ui_top.png)                                  
**Figure:** The main application window.

The main UI is mostly a monitoring tool for the background real-time combat log parser. You can display it on another screen, or just run it minimized.

You can do the following in the main UI:
* Start and Stop the background parser.
* Enable the Grid Overlay, and modify its settings.
* Enable the achievement system and overlay, and modify its settings.

### Grid Overlay
![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/grid_overlay.png)
**Figure:** The main grid overlay displayed in the upper right of the screen.

**Note:** In order for the Grid Overlay to work in STO, the game needs to be running in a Window display mode

The grid overlay is what displays the combat statistics grid. Each player in combat is displayed in the grid. 
The main UI has a settings tab so the user can select which statistics to display in the grid, what colors to use, and its location on the screen.

### Achievements System and Overlay
![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/doublekill_overlay.png)                                  
**Figure:** The DOUBLE KILL achievement being flashed to the screen.

**Note:** In order for the Achievments overlay to work in STO, the game needs to be running in a Window display mode

The achievements system maintains a list of units attacked by the player while in combat. Each unit killed by the player is counted as a kill. When the player reaches certain consecutive kill achievements, a message is displayed to the screen, along with audio file playback from the Unreal Tournament Announcer.

Your consecutive kill count is reset after you've been out of combat for a configured amount of time, or when your player dies. This puts you back at the bottom of the achievement list.

#### Kill Scoring 

##### Killing Sprees
Awards for every 5th kill without dying

* Killing Spree - 5 kills
* Rampage - 10 kills
* Dominating - 15 kills
* Unstoppable - 20 kills
* Godlike - 25 kills
* Wicked Sick - 30+ kills

##### Multiple Kills
Awards for building up chains of kills in quick succession (4 seconds apart or less)

* Double Kill - 2 kills
* Tripple Kill - 3 kills
* Multi Kill - 4 kills
* Mega Kill - 5 kills
* Ultra Kill - 6 kills
* Monster Kill - 7 kills
* Ludicrous Kill - 8 kills
* HOLY S**T - 8+ kills

## Building
The source in this repo is wrapped up in a Visual Studio 2022 solution. You should be able to clone this repo localy, then build and run from inside of Visual Studio.

You could also run the dotnet cli build and run commands from inside the zxeltor.StoCombatAnalyser.Interface project folder as well.

## Screen Shots
Figure: What the application overlays look like on top of STO. You can see the DOUBLE KILL achievement overlay high center screen, and the real-time combat data grid in the upper right.
![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/overlays_scaled.jpg)

Figure: The main application UI. Use this to configure achievment processing, and the overlay settings.
![Overlays](https://github.com/zxeltor/zxeltor.StoCombat.Realtime/blob/master/ScreenShots/ui.png)

## Disclaimer
This software and any related documentation is provided “as is” without warranty of any kind, either express or implied, including, without limitation, the implied warranties of merchantability, fitness for a particular purpose, or non-infringement. Licensee accepts any and all risk arising out of use or performance of Software

**Note:** We are not affiliated, associated, authorized, endorsed by, or in any way officially connected with the game Star Trek Online, or any of its subsidiaries or its affiliates. The official Star Trek Online website can be found at [https://www.playstartrekonline.com/](https://www.playstartrekonline.com/en/
