namespace Shared

open System

type Direction =
    | Up
    | Down
    | Left
    | Right

type GameSettings =
    { BoardSize: int
      TargetTile: int }

type GameSessionDto =
    { SessionId: Guid
      Seed: int
      Settings: GameSettings
      ExpiresAt: DateTimeOffset }

type ScoreSubmissionDto =
    { SessionId: Guid
      Nickname: string
      Moves: Direction list
      DurationMs: int
      UsedUndo: bool }

type LeaderboardQuery =
    { BoardSize: int
      TargetTile: int
      Limit: int }

type LeaderboardEntryDto =
    { Nickname: string
      Score: int
      MaxTile: int
      MoveCount: int
      DurationMs: int
      Won: bool
      SubmittedAt: DateTimeOffset }

type IGameApi =
    { startRankedGame: GameSettings -> Async<GameSessionDto>
      submitScore: ScoreSubmissionDto -> Async<Result<LeaderboardEntryDto, string>>
      getLeaderboard: LeaderboardQuery -> Async<LeaderboardEntryDto list>
      health: unit -> Async<string> }

module Route =
    let builder typeName methodName = sprintf "/api/%s/%s" typeName methodName
