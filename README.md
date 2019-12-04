# Wargame Tournament Manager

A tool to manage wargame tournaments, as the name says. It allows you to create tournament, register people and do matchmaking. It's similar to other tools, but with a few differences:

- The tool autosaves when you modify anything in the tournament. Furthermore, before starting each round (or any other "dangerous action"), in addition of saving, it creates a backup.
- The UI is slighty more modern and should be easy to use, nb
- It works completely offline.
- It's generic, so you don't depend on the tool having support for each game. There are defaults for a couple of games but they can be ignored.

## Note

This is still in beta, and it's not even translated yet! It's still fully in spanish, there's no english version. 

## Installation

Just download the latest version from the Releases page on the repository. Unzip in whatever folder you like. Requires .NET 4.7.1 to run (installed by default on most Windows 10 versions).

## ToDo

- English translation, with bindings so it doesn't require software restart.
- Export the ranking to additional formats in addition to CSV: tsv, bbcode.
- Add the posibility of reopening a round, removing all the rounds following it.
- Icon, version and similar stuff
- Add the posibility of editing the details of a matchup AFTER a new round has activated, with the appropiate notice that this will mean that existing pairings won't be correct.

## Developer QuickStart

This has been developed with VS2017, but should work with other versions. Just load the solution, check you have .NET version 4.7 and that's it!

The program is very small and is divided in a few files:

- MainWindow, PlayerScreen, CreateTournamentScreen, MatchupsScreen: UI code and interaction with the user
- Model: All the data of the state is here, as most of the internal logic
- Games, Matchmaker: Stateless utility classes to avoid having all the logic in Model

Since it's still very early in development, I'm not considering contributions (but feel free to open any issue!)

## License

The software will be licensed under GPL after the release of 1.0