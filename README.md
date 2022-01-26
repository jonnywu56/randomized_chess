# randomized_chess
Chess implementation built through Unity using C#, utilizing Unity Transport package to support the following game modes:
- Local: Play a friend on a single device
- Host Online (LAN): Host a game that can be joined by a friend on the same network
- Join Online (LAN): Join a game already hosted by a friend on the same network (need host's private IP to join)

Each game includes three randomizers that affect board setup, gameplay, and win conditions:
- 11 randomizers that affect board setup
- 11 randomizers that affect piece movement and gameplay
- 5 randomizers that affect win conditions

Local and multiplayer UI include:
- Takeback Button: Takes back last move (both players must agree in multiplayer games)
- Restart Button: Restarts the game with new modifiers (both players must agree in multiplayer games)
- Quit Button: Exits game

Local and multiplayer display includes:
- Distinct highlight color for selected piece, possible moves, and king in check
- Description of active modifiers (full list in "Randomizers List")
- Description of current game state
- List of past moves denoted with algebraic notation


