namespace Vosen.Juiz.Tests

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
        estimates.Ratings.[0] |> should equal (Rating(0, 0, 3.25))
        estimates.Ratings.[1] |> should equal (Rating(1, 0, 6.75))
        estimates.Ratings.[2] |> should equal (Rating(1, 1, 4.25))
        estimates.Ratings.[3] |> should equal (Rating(2, 1, 5.75))

    [<Test>]
    member test.``build works`` () =
        let ratings = [| Rating(0, 1, 5.0) ; Rating(1, 0, 4.0) ; Rating(1, 1, 2.0) ; Rating(2, 1, 4.0) |]
        let results = Vosen.Juiz.FunkSVD.build simplePredictBaseline ratings 20
        ignore()