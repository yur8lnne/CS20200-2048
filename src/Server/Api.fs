namespace Server

open System
open Shared
open Shared.Game

module Api =
    let private randomSeed () =
        Random.Shared.Next(1, Int32.MaxValue)

    let private validateNickname (nickname: string) =
        let value =
            if isNull nickname then
                ""
            else
                nickname.Trim()

        if value.Length < 2 || value.Length > 16 then
            Error "Nickname must be 2 to 16 characters."
        elif value |> Seq.exists Char.IsControl then
            Error "Nickname contains invalid characters."
        else
            Ok value

    let private validateSubmission (submission: ScoreSubmissionDto) =
        if submission.UsedUndo then
            Error "Undo games are unranked."
        elif submission.Moves.IsEmpty then
            Error "Play at least one move before submitting."
        elif submission.Moves.Length > 10000 then
            Error "The move history is too long."
        elif submission.DurationMs <= 0 then
            Error "Duration must be positive."
        else
            Ok ()

    let createGameApi (store: LeaderboardStore) : IGameApi =
        let startRankedGame (settings: GameSettings) =
            async {
                let normalized = normalizeSettings settings

                let session : GameSessionDto =
                    { SessionId = Guid.NewGuid()
                      Seed = randomSeed ()
                      Settings = normalized
                      ExpiresAt = DateTimeOffset.UtcNow.AddHours(4.0) }

                store.CreateSession session
                return session
            }

        let submitScore (submission: ScoreSubmissionDto) =
            async {
                match validateNickname submission.Nickname, validateSubmission submission with
                | Error message, _ -> return Error message
                | _, Error message -> return Error message
                | Ok nickname, Ok () ->
                    match store.TryConsumeSession submission.SessionId with
                    | Error message -> return Error message
                    | Ok session ->
                        let replayed = replay session.Settings session.Seed submission.Moves

                        let entry : LeaderboardEntryDto =
                            { Nickname = nickname
                              Score = replayed.Score
                              MaxTile = maxTile replayed.Board
                              MoveCount = replayed.Moves.Length
                              DurationMs = min submission.DurationMs (24 * 60 * 60 * 1000)
                              Won = replayed.Status = Won
                              SubmittedAt = DateTimeOffset.UtcNow }

                        store.InsertEntry(entry, session.Settings)
                        return Ok entry
            }

        let getLeaderboard (query: LeaderboardQuery) =
            async {
                let settings =
                    normalizeSettings
                        { BoardSize = query.BoardSize
                          TargetTile = query.TargetTile }

                let query' =
                    { query with
                        BoardSize = settings.BoardSize
                        TargetTile = settings.TargetTile
                        Limit = query.Limit |> max 1 |> min 50 }

                return store.GetLeaderboard query'
            }

        let health () =
            async { return "ok" }

        { startRankedGame = startRankedGame
          submitScore = submitScore
          getLeaderboard = getLeaderboard
          health = health }
