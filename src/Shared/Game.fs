namespace Shared

module Game =
    type GameStatus =
        | Playing
        | Won
        | Lost

    type MoveOutcome =
        { Board: int list
          ScoreGained: int
          Moved: bool }

    type GameState =
        { Settings: GameSettings
          Board: int list
          Score: int
          Seed: int
          Moves: Direction list
          Status: GameStatus
          UsedUndo: bool }

    let defaultSettings : GameSettings =
        { BoardSize = 4
          TargetTile = 2048 }

    let private clamp minValue maxValue value =
        value |> max minValue |> min maxValue

    let private isPowerOfTwo value =
        value > 0 && (value &&& (value - 1)) = 0

    let normalizeSettings (settings: GameSettings) : GameSettings =
        let target =
            if settings.TargetTile >= 128 && settings.TargetTile <= 8192 && isPowerOfTwo settings.TargetTile then
                settings.TargetTile
            else
                defaultSettings.TargetTile

        { BoardSize = clamp 3 6 settings.BoardSize
          TargetTile = target }

    let emptyBoard (settings: GameSettings) =
        let normalized = normalizeSettings settings
        List.replicate (normalized.BoardSize * normalized.BoardSize) 0

    let maxTile board =
        board |> List.fold max 0

    let hasWon (settings: GameSettings) (board: int list) =
        maxTile board >= (normalizeSettings settings).TargetTile

    let private nextSeed seed =
        let unsigned = (int64 seed * 1103515245L + 12345L) &&& 0x7fffffffL
        int unsigned

    let private nextInt maxExclusive seed =
        let seed' = nextSeed seed
        seed', seed' % maxExclusive

    let spawnTile seed board =
        let emptyCells =
            board
            |> List.indexed
            |> List.choose (fun (index, value) -> if value = 0 then Some index else None)

        match emptyCells with
        | [] -> board, seed, false
        | _ ->
            let seedAfterCell, cellOffset = nextInt emptyCells.Length seed
            let seedAfterValue, valueRoll = nextInt 10 seedAfterCell
            let index = emptyCells.[cellOffset]
            let tile = if valueRoll = 0 then 4 else 2

            let board' =
                board
                |> List.mapi (fun currentIndex value -> if currentIndex = index then tile else value)

            board', seedAfterValue, true

    let mergeLine line =
        let rec merge score merged remaining =
            match remaining with
            | left :: right :: tail when left = right ->
                let value = left + right
                merge (score + value) (value :: merged) tail
            | value :: tail -> merge score (value :: merged) tail
            | [] -> List.rev merged, score

        let compact = line |> List.filter ((<>) 0)
        let merged, score = merge 0 [] compact
        let padded = merged @ List.replicate (line.Length - merged.Length) 0
        padded, score

    let private lines (direction: Direction) (size: int) (board: int list) : int list list =
        match direction with
        | Left ->
            [ for row in 0 .. size - 1 ->
                  [ for col in 0 .. size - 1 -> board.[row * size + col] ] ]
        | Right ->
            [ for row in 0 .. size - 1 ->
                  [ for col in size - 1 .. -1 .. 0 -> board.[row * size + col] ] ]
        | Up ->
            [ for col in 0 .. size - 1 ->
                  [ for row in 0 .. size - 1 -> board.[row * size + col] ] ]
        | Down ->
            [ for col in 0 .. size - 1 ->
                  [ for row in size - 1 .. -1 .. 0 -> board.[row * size + col] ] ]

    let private boardFromLines (direction: Direction) (size: int) (allLines: int list list) =
        let values = Array.create (size * size) 0

        for lineIndex, line in List.indexed allLines do
            for offset, value in List.indexed line do
                let row, col =
                    match direction with
                    | Left -> lineIndex, offset
                    | Right -> lineIndex, size - 1 - offset
                    | Up -> offset, lineIndex
                    | Down -> size - 1 - offset, lineIndex

                values.[row * size + col] <- value

        values |> Array.toList

    let moveBoard (settings: GameSettings) (direction: Direction) (board: int list) =
        let size = (normalizeSettings settings).BoardSize

        let movedLines =
            lines direction size board
            |> List.map mergeLine

        let board' =
            movedLines
            |> List.map fst
            |> boardFromLines direction size

        { Board = board'
          ScoreGained = movedLines |> List.sumBy snd
          Moved = board' <> board }

    let canMove (settings: GameSettings) (board: int list) =
        board |> List.exists ((=) 0)
        || [ Up; Down; Left; Right ]
           |> List.exists (fun direction -> (moveBoard settings direction board).Moved)

    let newGame (settings: GameSettings) seed =
        let normalized = normalizeSettings settings
        let board0 = emptyBoard normalized
        let board1, seed1, _ = spawnTile seed board0
        let board2, seed2, _ = spawnTile seed1 board1

        { Settings = normalized
          Board = board2
          Score = 0
          Seed = seed2
          Moves = []
          Status = Playing
          UsedUndo = false }

    let applyMove (state: GameState) (direction: Direction) =
        match state.Status with
        | Won
        | Lost ->
            { Board = state.Board
              ScoreGained = 0
              Moved = false },
            state
        | Playing ->
            let outcome = moveBoard state.Settings direction state.Board

            if not outcome.Moved then
                outcome, state
            else
                let boardAfterSpawn, seedAfterSpawn, _ = spawnTile state.Seed outcome.Board

                let score = state.Score + outcome.ScoreGained

                let status =
                    if hasWon state.Settings boardAfterSpawn then Won
                    elif canMove state.Settings boardAfterSpawn then Playing
                    else Lost

                let state' =
                    { state with
                        Board = boardAfterSpawn
                        Score = score
                        Seed = seedAfterSpawn
                        Moves = state.Moves @ [ direction ]
                        Status = status }

                outcome, state'

    let replay (settings: GameSettings) seed (moves: Direction list) =
        moves
        |> List.fold (fun state direction -> applyMove state direction |> snd) (newGame settings seed)
