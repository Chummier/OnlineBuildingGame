# OnlineBuildingGame

Hello, this repository holds the entire Visual Studio project for an online game I'm making with ASP.NET Core in C#.
I'm using MVC and SignalR as well.

The Data folder holds two database contexts: one for the users created through Microsoft's Identity class, and another for
all the tables I use to store information about the game, like player locations, the game world, and items that players have.

The Game folder holds the main files for the game, namely GameWorld.cs and GameLogic.cs. GameWorld handles database
interactions, and has functions that let the frontend get information about the game that it needs to display to the player.
GameLogic handles player actions, like using items and altering the world, and transmits information from GameWorld to
JavaScript.

The Controllers folder holds the MVC Controllers. GameController handles the pages that are used for the main game screen,
as well as pages for hosting a world that other players can join, and for joining a world hosting by someone else.
PagesController handles the initial login screen for the game, and redirects users to pages handled by GameController.
