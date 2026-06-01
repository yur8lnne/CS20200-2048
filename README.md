# CS20200 2048

SAFE-style F# 2048 web service built with Fable, Elmish, Feliz, Vite, Tailwind, Giraffe/Fable Remoting, and SQLite.

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
