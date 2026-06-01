# CS20200 2048

SAFE-style F# 2048 web service built with Fable, Elmish, Feliz, Vite, Tailwind, Giraffe/Fable Remoting, and SQLite.

The first screen is the playable game. It supports keyboard play, mobile swipe, undo, themes, board/target settings, local stats, share text, ranked sessions, and a SQLite-backed leaderboard.

## How To Play

- Move tiles with the arrow keys or `W`, `A`, `S`, `D`. On mobile, swipe directly on the board.
- Tiles slide in the chosen direction. Matching normal tiles merge and increase your score.
- Three adjacent normal tiles with the same number merge into an `n³` tile, such as `2 2 2` becoming `2³`.
- An `n³` tile can merge with a normal tile of the same number. It may create a Joker tile; otherwise it creates a normal `4n` tile.
- A Joker tile `J` can merge with any normal number tile and doubles that number. Jokers do not merge with other Jokers.
- Use `New Game` to restart and `Undo` to take back a move. Games that use Undo are not ranked on the leaderboard.


## Live demo

### Play the hosted version on Render: [https://cs20200-2048.onrender.com/](https://cs20200-2048.onrender.com/)
<br />

## Requirements

- .NET SDK 10.0.103 or compatible .NET 10 SDK
- Node.js 22+
- npm 10+

## Development

```powershell
npm run restore
npm run dev:server
npm run dev:client
```

Open the Vite client at `http://localhost:5173`. The client proxies `/api` calls to the F# server on `http://localhost:5000`.

## Build and test

```powershell
npm run build
npm test
```

The production client is emitted into `src/Server/wwwroot` and served by the F# server.

## Docker

```powershell
docker compose up --build
```

The app listens on `http://localhost:8080` and stores SQLite data in the `leaderboard-data` volume.


## Changes After Proposal

In the original proposal, I planned to develop a CLI-based 2048 game. However, after reviewing other students’ proposals, I found that there were already several similar projects based on console-based 2048 games. To make the project more distinct and improve the user experience, I decided to change the game from a CLI-based version to a GUI-based version.

This change does not alter the core concept of the project. The main rules and gameplay of 2048 remain the same: the player moves tiles, combines tiles with the same number, and tries to reach a higher score. The major change is only in the user interface. Instead of controlling the game through text commands in the console, the player can interact with the game through a graphical interface.

I believe this change is reasonable because it helps differentiate my project from other similar proposals while keeping the original idea and requirements mostly intact. It also makes the game more intuitive and visually understandable for players.


## Use of Large Language Models

The LLM was mainly used to help convert the original CLI-based 2048 game into a GUI-based game and to assist with the deployment process. It helped me understand how to organize the graphical interface, connect the game logic to the UI, and prepare the project so that it could be run outside the local development environment.

However, I had to manually modify and re-prompt several parts because the LLM did not fully understand my intended gameplay interaction. In the original CLI version, the game was already designed to be controlled using keyboard arrow keys. Therefore, even after converting the project into a GUI-based game, I wanted to preserve the same keyboard-based control method. However, the LLM initially changed the control method into an unintended touch-slide interaction, where the game was played by swiping or sliding instead of pressing the arrow keys.

Because of this, I had to review and modify the input-handling logic in detail. In particular, I needed to adjust the event handling so that the GUI version could respond properly to keyboard arrow key inputs, rather than relying on touch or mouse-based slide interactions.

The main point that the LLM was not able to do correctly was preserving the intended control method while converting the game from CLI to GUI. While it was helpful for building the GUI structure and giving guidance for deployment, it misunderstood how the player should interact with the game. Therefore, I had to manually correct the control logic and verify that the final program matched my intended requirements.
