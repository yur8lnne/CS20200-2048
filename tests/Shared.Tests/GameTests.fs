namespace Shared.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Shared
open Shared.Game

[<TestClass>]
type GameTests() =
    let assertList expected actual =
        Assert.IsTrue((expected = actual), sprintf "Expected %A but got %A" expected actual)

    [<TestMethod>]
    member _.``mergeLine merges one pair from three equal tiles``() =
        let line, score = mergeLine [ 2; 2; 2; 0 ]
        assertList [ 4; 2; 0; 0 ] line
        Assert.AreEqual<int>(4, score)

    [<TestMethod>]
    member _.``mergeLine merges two pairs``() =
        let line, score = mergeLine [ 2; 2; 2; 2 ]
        assertList [ 4; 4; 0; 0 ] line
        Assert.AreEqual<int>(8, score)

    [<TestMethod>]
    member _.``move without board change does not spawn``() =
        let settings = defaultSettings

        let state =
            { Settings = settings
              Board = [ 2; 0; 0; 0; 0; 0; 0; 0; 0; 0; 0; 0; 0; 0; 0; 0 ]
              Score = 0
              Seed = 42
              Moves = []
              Status = Playing
              UsedUndo = false }

        let outcome, state' = applyMove state Left
        Assert.IsFalse(outcome.Moved)
        assertList state.Board state'.Board
        Assert.AreEqual<int>(42, state'.Seed)

    [<TestMethod>]
    member _.``win and loss detection work``() =
        Assert.IsTrue(hasWon defaultSettings [ 2048; 0; 0; 0 ])

        let fullLockedBoard =
            [ 2; 4; 2; 4
              4; 2; 4; 2
              2; 4; 2; 4
              4; 2; 4; 2 ]

        Assert.IsFalse(canMove defaultSettings fullLockedBoard)

    [<TestMethod>]
    member _.``seeded spawn is deterministic``() =
        let board = emptyBoard defaultSettings
        let boardA, seedA, spawnedA = spawnTile 123 board
        let boardB, seedB, spawnedB = spawnTile 123 board
        assertList boardA boardB
        Assert.AreEqual<int>(seedA, seedB)
        Assert.AreEqual<bool>(spawnedA, spawnedB)

    [<TestMethod>]
    member _.``settings are normalized``() =
        let normalized =
            normalizeSettings
                { BoardSize = 99
                  TargetTile = 123 }

        Assert.AreEqual<int>(6, normalized.BoardSize)
        Assert.AreEqual<int>(2048, normalized.TargetTile)
