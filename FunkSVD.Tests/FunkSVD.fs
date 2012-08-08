namespace Vosen.Juiz.Tests

open MathNet.Numerics.LinearAlgebra.Double
open Vosen.Juiz.FunkSVD
open NUnit.Framework
open FsUnit

[<TestFixture>] 
type FunkSVD ()=

    [<Test>]
    member test.``estimate returns properly formed data`` () =
        let ratings = [| Rating(0, 0, 2.0) ; Rating(1, 0, 3.0) ; Rating(1, 1, 8.0) ; Rating(2, 1, 7.0) |]
        let estimates = Vosen.Juiz.FunkSVD.simplePredictBaseline ratings
        estimates.MovieCount |> should equal 3
        estimates.UserCount |> should equal 2

    [<Test>]
    member test.``estimated values are correct`` () =
        let ratings = [| Rating(0, 0, 2.0) ; Rating(1, 0, 3.0) ; Rating(1, 1, 8.0) ; Rating(2, 1, 7.0) |]
        let estimates = Vosen.Juiz.FunkSVD.simplePredictBaseline ratings
        estimates.Predicted.[0] |> should equal 3.25
        estimates.Predicted.[1] |> should equal 6.75
        estimates.Predicted.[2] |> should equal 4.25
        estimates.Predicted.[3] |> should equal 5.75

    [<Test>]
    member test.``build works`` () =
        let ratings = [| Rating(0, 1, 5.0) ; Rating(1, 0, 4.0) ; Rating(1, 1, 2.0) ; Rating(2, 1, 4.0) |]
        let results = Vosen.Juiz.FunkSVD.build simplePredictBaseline ratings 20
        ignore()

[<TestFixture>] 
type RMSE ()=

    [<Test>]
    member test.``RMSE returns correct result`` () =
        let fakePredict ratings target = 1.0
        let probe = [| (0, 0.5, [||]); (0, 2.5, [||]); (0, 10.0, [||]) |]
        Vosen.Juiz.RMSE.measureRMSE fakePredict probe |> should (equalWithin 0.000001) (sqrt (83.5 / 3.0))

    [<Test>]
    member test.``prints correct mesurements`` () =
        let results = [| (1, 20.0); (10, 0.1) |]
        Vosen.Juiz.RMSE.measuresToString results |> should startWith "1, 20.000000\n10, 0.100000\n"

    [<Test>]
    member test.``RMSE builds correct test probe`` () =
        let sparse = SparseMatrix(2,3, [| 0.0; 3.0; 1.0; 4.0; 2.0; 5.0 |])
        let probe = Vosen.Juiz.RMSE.buildProbe sparse
        probe |> should haveLength 5
        // item 1
        let (target1, score1, probe1) = probe.[0]
        target1 |> should equal 1
        score1 |> should equal 1.0
        probe1 |> should equal [| (2, 2.0) |]
        // item 2
        let (target2, score2, probe2) = probe.[1]
        target2 |> should equal 2
        score2 |> should equal 2.0
        probe2 |> should equal [| (1, 1.0) |]
        // item 3
        let (target3, score3, probe3) = probe.[2]
        target3 |> should equal 0
        score3 |> should equal 3.0
        probe3 |> should equal [| (1, 4.0); (2, 5.0) |]
        // item 4
        let (target4, score4, probe4) = probe.[3]
        target4 |> should equal 1
        score4 |> should equal 4.0
        probe4 |> should equal [| (0, 3.0); (2, 5.0) |]
        // item 5
        let (target5, score5, probe5) = probe.[4]
        target5 |> should equal 2
        score5 |> should equal 5.0
        probe5 |> should equal [| (0, 3.0); (1, 4.0) |]

    [<Test>]

    member test.``correct ratings are picked`` () =
        let scoreMatrix = SparseMatrix(2,4, [|2.0; 0.0; 3.0; 4.0; 0.0; 6.0; 0.0; 0.0|])
        let ratings = Vosen.Juiz.RMSE.pickRatings scoreMatrix
        ratings |> should haveLength 4
        ratings |> should contain (Rating(0, 0, 2.0))
        ratings |> should contain (Rating(1, 0, 3.0))
        ratings |> should contain (Rating(1, 1, 4.0))
        ratings |> should contain (Rating(2, 1, 6.0))