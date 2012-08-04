namespace Vosen.Juiz.Tests

open Vosen.Juiz.FunkSVD
open NUnit.Framework
open FsUnit

[<TestFixture>] 
type FunkSVD ()=
    let ratings = [| Rating(0, 0, 2.0) ; Rating(1, 0, 3.0) ; Rating(1, 1, 8.0) ; Rating(2, 1, 7.0) |]
    let estimates = Vosen.Juiz.FunkSVD.initializeEstimates ratings

    [<Test>]
    member test.EstimatesCount ()=
        estimates.MovieCount |> should equal 3
        estimates.UserCount |> should equal 2

    [<Test>]
    member test.EstimatesValues ()=
        estimates.Ratings.[0] |> should equal (Rating(0, 0, 3.25))
        estimates.Ratings.[1] |> should equal (Rating(1, 0, 6.75))
        estimates.Ratings.[2] |> should equal (Rating(1, 1, 4.25))
        estimates.Ratings.[3] |> should equal (Rating(2, 1, 5.75))