#I "bin\\Release"

#r "Npgsql"
#r "Dapper"
#r "FunkSVD"
#r "FunkSVD.ModelGenerator"

open System
open System.Collections.Generic
open Npgsql
open Dapper
open Vosen.Juiz.FunkSVD
open Vosen.Juiz.ModelGenerator

let shuffle (ar : array<_>) =
    let rand = System.Random()
    let swap (a: array<_>) x y =
        let tmp = a.[x]
        a.[x] <- a.[y]
        a.[y] <- tmp
    Array.iteri (fun i _ -> swap ar i (rand.Next(i, Array.length ar))) ar

let pickTrainTest trainCount testCount (ratings : Rating array) =
    let rand = Random()
    let trainSet = Collections.Generic.List()
    let pickedRatings = Collections.Generic.HashSet()
    let pickedMovies = Collections.Generic.Dictionary()
    printfn "initiated"
    for i = 0 to (trainCount - 1) do
        let mutable chosen = rand.Next(ratings.Length)
        while pickedRatings.Contains(chosen) do
            chosen <- rand.Next(ratings.Length)
        pickedRatings.Add(chosen) |> ignore
        pickedMovies.AddOrSet (fun _ -> 1) ((+) 1) ratings.[chosen].Title
        trainSet.Add(ratings.[chosen])
    let approvedMovies = Collections.Generic.HashSet(pickedMovies |> Seq.choose (fun kvp -> if kvp.Value >= 3 then Some(kvp.Key) else None))
    let userRatings = Linq.Enumerable.ToDictionary(ratings |> Array.filter (fun rating -> approvedMovies.Contains(rating.Title)) |> Seq.groupBy (fun rating -> rating.User), fst, snd >> Seq.toArray)
    // now user ratings contain list dist [user: watched movies], movies are from correct list
    let userIds = userRatings |> Seq.map (fun kvp -> kvp.Key) |> Seq.toArray
    let testSet = Collections.Generic.List()
    for i = 0 to (testCount - 1) do
        let mutable chosenId = -1
        while not (userRatings.ContainsKey(chosenId) && userRatings.[chosenId].Length >= 2) do
            chosenId <- userIds.[rand.Next(userIds.Length)]
        let ratings = userRatings.[chosenId]
        shuffle ratings
        // ids, ratings
        testSet.Add(ratings)
    (trainSet, testSet)

let remapData (rawTrainSet : List<Rating>) (rawTestSet : List<Rating array>) =
    let users = Dictionary()
    let moviesTrain = Dictionary()
    let moviesTest = Dictionary()
    for i = 0 to rawTrainSet.Count - 1 do
        users.AddOrSet (fun _ -> [i]) (fun l -> i::l) rawTrainSet.[i].User
        moviesTrain.AddOrSet (fun _ -> [i]) (fun l -> i::l) rawTrainSet.[i].Title
    for i = 0 to rawTestSet.Count - 1 do
        for j = 0 to rawTestSet.[i].Length - 1 do
            moviesTest.AddOrSet (fun _ -> [(i,j)]) (fun l -> (i,j)::l) rawTestSet.[i].[j].Title
    let mutable userIdx = 0
    for kvp in users do
        for idx in kvp.Value do
            rawTrainSet.[idx] <- Rating(rawTrainSet.[idx].Title, userIdx, rawTrainSet.[idx].Score)
        userIdx <- userIdx + 1
    let mutable movieIdx = 0
    for kvp in moviesTrain do
        for idx in kvp.Value do
            rawTrainSet.[idx] <- Rating(movieIdx, rawTrainSet.[idx].User, rawTrainSet.[idx].Score)
        if moviesTest.ContainsKey(kvp.Key) then
            for (i,j) in moviesTest.[kvp.Key] do
                rawTestSet.[i].[j] <- Rating(movieIdx,  rawTestSet.[i].[j].User,  rawTestSet.[i].[j].Score)
        movieIdx <- movieIdx + 1
    (rawTrainSet, rawTestSet)

let run dbPath trainCount testCount out =
    let ratings = loadData dbPath |> Seq.toArray
    let rawTrainSet, rawTestSet = pickTrainTest trainCount testCount ratings
    let trainSet, testSet = remapData rawTrainSet rawTestSet
    System.IO.File.WriteAllText(out + "-train", [| trainSet.ToArray() |] |> saveArray (fun (rating : Rating) ->  sprintf "%d %d %d" rating.Title rating.User (int(rating.Score))))
    System.IO.File.WriteAllText(out + "-test", testSet.ToArray() |> saveArray (fun (rating : Rating) ->  sprintf "%d %d" rating.Title (int(rating.Score))))

let main (args : string array) =
    if args.Length <> 4 then
        printfn "USAGE: build_test_model.fsx db_string train_size test_size out_file"
        printfn "build_test_model.fsx \"Server=127.0.0.1;Port=5432;Database=mal;User Id=vosen;Password=postgres;\" 100000 10000 probe"
        1
    else
        run args.[0] (int args.[1]) (int args.[2]) args.[3]
        0

#if INTERACTIVE
fsi.CommandLineArgs |> Array.toList |> List.tail |> List.toArray |> main
#else
[<EntryPoint; STAThread>]
let entryPoint args = main args
#endif