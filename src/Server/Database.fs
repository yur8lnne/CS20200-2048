namespace Server

open System
open System.Globalization
open System.IO
open Microsoft.Data.Sqlite
open Shared

type StoredSession =
    { SessionId: Guid
      Seed: int
      Settings: GameSettings
      ExpiresAt: DateTimeOffset
      Consumed: bool }

type LeaderboardStore(dbPath: string) =
    let connectionString = sprintf "Data Source=%s" dbPath

    let ensureDirectory () =
        let directory = Path.GetDirectoryName(Path.GetFullPath(dbPath))

        if not (String.IsNullOrWhiteSpace directory) then
            Directory.CreateDirectory(directory) |> ignore

    let openConnection () =
        ensureDirectory ()
        let connection = new SqliteConnection(connectionString)
        connection.Open()
        connection

    let addParameter name value (command: SqliteCommand) =
        command.Parameters.AddWithValue(name, value) |> ignore

    let dateToText (value: DateTimeOffset) =
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)

    let dateFromText (value: string) =
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)

    member _.Initialize() =
        use connection = openConnection ()

        let execute sql =
            use command = connection.CreateCommand()
            command.CommandText <- sql
            command.ExecuteNonQuery() |> ignore

        execute
            """
            CREATE TABLE IF NOT EXISTS sessions (
                session_id TEXT PRIMARY KEY,
                seed INTEGER NOT NULL,
                board_size INTEGER NOT NULL,
                target_tile INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT NOT NULL,
                consumed INTEGER NOT NULL DEFAULT 0
            );
            """

        execute
            """
            CREATE TABLE IF NOT EXISTS leaderboard (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                nickname TEXT NOT NULL,
                score INTEGER NOT NULL,
                max_tile INTEGER NOT NULL,
                move_count INTEGER NOT NULL,
                duration_ms INTEGER NOT NULL,
                board_size INTEGER NOT NULL,
                target_tile INTEGER NOT NULL,
                won INTEGER NOT NULL,
                submitted_at TEXT NOT NULL
            );
            """

        execute
            """
            CREATE INDEX IF NOT EXISTS ix_leaderboard_scope
            ON leaderboard(board_size, target_tile, score DESC, max_tile DESC, move_count ASC, duration_ms ASC);
            """

    member _.CreateSession(session: GameSessionDto) =
        use connection = openConnection ()
        use command = connection.CreateCommand()

        command.CommandText <-
            """
            INSERT INTO sessions(session_id, seed, board_size, target_tile, created_at, expires_at, consumed)
            VALUES($session_id, $seed, $board_size, $target_tile, $created_at, $expires_at, 0);
            """

        command |> addParameter "$session_id" (string session.SessionId)
        command |> addParameter "$seed" session.Seed
        command |> addParameter "$board_size" session.Settings.BoardSize
        command |> addParameter "$target_tile" session.Settings.TargetTile
        command |> addParameter "$created_at" (dateToText DateTimeOffset.UtcNow)
        command |> addParameter "$expires_at" (dateToText session.ExpiresAt)
        command.ExecuteNonQuery() |> ignore

    member _.TryConsumeSession(sessionId: Guid) =
        use connection = openConnection ()
        use transaction = connection.BeginTransaction()
        use selectCommand = connection.CreateCommand()
        selectCommand.Transaction <- transaction

        selectCommand.CommandText <-
            """
            SELECT session_id, seed, board_size, target_tile, expires_at, consumed
            FROM sessions
            WHERE session_id = $session_id;
            """

        selectCommand |> addParameter "$session_id" (string sessionId)

        let stored =
            use reader = selectCommand.ExecuteReader()

            if reader.Read() then
                Some
                    { SessionId = Guid.Parse(reader.GetString(0))
                      Seed = reader.GetInt32(1)
                      Settings =
                        { BoardSize = reader.GetInt32(2)
                          TargetTile = reader.GetInt32(3) }
                      ExpiresAt = dateFromText (reader.GetString(4))
                      Consumed = reader.GetInt32(5) <> 0 }
            else
                None

        match stored with
        | None ->
            transaction.Rollback()
            Error "The ranked game session was not found."
        | Some session when session.Consumed ->
            transaction.Rollback()
            Error "This ranked game session was already submitted."
        | Some session when session.ExpiresAt < DateTimeOffset.UtcNow ->
            transaction.Rollback()
            Error "This ranked game session has expired."
        | Some session ->
            use updateCommand = connection.CreateCommand()
            updateCommand.Transaction <- transaction
            updateCommand.CommandText <- "UPDATE sessions SET consumed = 1 WHERE session_id = $session_id;"
            updateCommand |> addParameter "$session_id" (string sessionId)
            updateCommand.ExecuteNonQuery() |> ignore
            transaction.Commit()
            Ok session

    member _.InsertEntry(entry: LeaderboardEntryDto, settings: GameSettings) =
        use connection = openConnection ()
        use command = connection.CreateCommand()

        command.CommandText <-
            """
            INSERT INTO leaderboard(
                nickname, score, max_tile, move_count, duration_ms,
                board_size, target_tile, won, submitted_at
            )
            VALUES(
                $nickname, $score, $max_tile, $move_count, $duration_ms,
                $board_size, $target_tile, $won, $submitted_at
            );
            """

        command |> addParameter "$nickname" entry.Nickname
        command |> addParameter "$score" entry.Score
        command |> addParameter "$max_tile" entry.MaxTile
        command |> addParameter "$move_count" entry.MoveCount
        command |> addParameter "$duration_ms" entry.DurationMs
        command |> addParameter "$board_size" settings.BoardSize
        command |> addParameter "$target_tile" settings.TargetTile
        command |> addParameter "$won" (if entry.Won then 1 else 0)
        command |> addParameter "$submitted_at" (dateToText entry.SubmittedAt)
        command.ExecuteNonQuery() |> ignore

    member _.GetLeaderboard(query: LeaderboardQuery) =
        use connection = openConnection ()
        use command = connection.CreateCommand()
        let limit = query.Limit |> max 1 |> min 50

        command.CommandText <-
            """
            SELECT nickname, score, max_tile, move_count, duration_ms, won, submitted_at
            FROM leaderboard
            WHERE board_size = $board_size AND target_tile = $target_tile
            ORDER BY score DESC, max_tile DESC, move_count ASC, duration_ms ASC, submitted_at ASC
            LIMIT $limit;
            """

        command |> addParameter "$board_size" query.BoardSize
        command |> addParameter "$target_tile" query.TargetTile
        command |> addParameter "$limit" limit

        use reader = command.ExecuteReader()

        [ while reader.Read() do
              { Nickname = reader.GetString(0)
                Score = reader.GetInt32(1)
                MaxTile = reader.GetInt32(2)
                MoveCount = reader.GetInt32(3)
                DurationMs = reader.GetInt32(4)
                Won = reader.GetInt32(5) <> 0
                SubmittedAt = dateFromText (reader.GetString(6)) } ]
