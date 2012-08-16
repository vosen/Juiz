namespace Vosen.Juiz

open System
open System.Collections.Generic
open Npgsql
open Dapper
open Vosen.Juiz.FunkSVD

module ModelGenerator =

    let loadData str =
        use conn = new NpgsqlConnection(str)
        conn.Notice.Add(fun evArgs -> evArgs.Notice |> Console.WriteLine)
        conn.Open()
        use dbTrans = conn.BeginTransaction()
        let records = conn.Query<Rating>("SELECT \"Anime_Id\" as \"Title\", \"User_Id\" as \"User\", CAST(\"Score\" AS float8) FROM \"Seen\"")
        dbTrans.Commit()
        records

    let filterFringeStep titleLimit userLimit (ratings : Rating seq) =
        let usersCount = Dictionary<int, int>()
        let titleCount = Dictionary<int, int>()
        for rating in ratings do
            usersCount.AddOrSet (fun _ -> 1) (fun i -> i + 1) rating.User
            titleCount.AddOrSet (fun _ -> 1) (fun i -> i + 1) rating.Title
        let significantUsers = HashSet(usersCount |> Seq.choose (fun kvp -> if kvp.Value < userLimit then None else Some(kvp.Key)))
        let significantTitles = HashSet(titleCount |> Seq.choose (fun kvp -> if kvp.Value < titleLimit then None else Some(kvp.Key)))
        ratings |> Seq.filter (fun rating -> significantUsers.Contains(rating.User) && significantTitles.Contains(rating.Title))

    let filterFringe titleLimit userLimit (ratings : Rating seq) =
        ratings |> (filterFringeStep titleLimit userLimit) |> (filterFringeStep 1 1)

    let buildMapping (ratings : Rating array) =
        let titleMap = Dictionary<int, int>()
        let docMap = Dictionary<int, int>()
        let userMap = Dictionary<int, int>()
        let titleCount = ref -1
        let userCount = ref -1
        let maxTitle = ref -1
        let newRatings = 
            ratings 
            |> Array.map (fun rating ->
                maxTitle := max !maxTitle rating.Title
                let titleId = 
                    if titleMap.ContainsKey(rating.Title) then
                        titleMap.[rating.Title]
                    else 
                        incr titleCount
                        let currentTitleCount = !titleCount
                        titleMap.Add(rating.Title, currentTitleCount)
                        docMap.Add(currentTitleCount, rating.Title)
                        currentTitleCount
                let userId = 
                    if userMap.ContainsKey(rating.User) then
                        userMap.[rating.User]
                    else 
                        incr userCount
                        let currentUserCount = !userCount
                        userMap.Add(rating.User, currentUserCount)
                        currentUserCount
                Rating(titleId, userId, float(rating.Score)))
        let finalTitleMap = Array.init (!maxTitle + 1) (fun title -> if titleMap.ContainsKey(title) then titleMap.[title] else -1)
        let finalDocMap = Array.init titleMap.Count (fun title -> docMap.[title])
        (newRatings, finalTitleMap, finalDocMap)

    let run dbPath titleLimit userLimit featuresCount output =
        printfn "Loading and preprocessing"
        let ratings, titleToDocument, documentToTile = loadData dbPath |> (filterFringe titleLimit userLimit) |> Seq.toArray |> buildMapping
        let avgs = ref (Unchecked.defaultof<float array>)
        printfn "Calculating features: 0.00%%"
        let task, progress = buildAsync (averagesBaseline >> (fun (av, est) -> avgs := av; est)) ratings featuresCount
        progress.Progress.Add (fun (prog,sum) -> printfn "Calculating features: %.2f%%" (100.0 * (float(prog) / float(sum))))
        let features = fst task.Result
        let saveFloat (v : float) = v.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
        let saveInt (v:int) = v.ToString()
        printfn "Saving results"
        System.IO.File.WriteAllText(output + "-features", (saveArray saveFloat features))
        System.IO.File.WriteAllText(output + "-averages", (saveArray saveFloat [| !avgs |]))
        System.IO.File.WriteAllText(output + "-titles", (saveArray saveInt [| titleToDocument |]))
        System.IO.File.WriteAllText(output + "-docs", (saveArray saveInt [| documentToTile |]))

    [<EntryPoint>]
    let main args =
        if args.Length <> 5 then
            printfn "USAGE: ModelGenerator.exe db_string title_low_limit user_low_limit features out_file"
            printfn "ModelGenerator.exe \"Server=127.0.0.1;Port=5432;Database=mal;User Id=vosen;Password=postgres;\" 50 5 70 result"
            1
        else
            run args.[0] (int args.[1]) (int args.[2]) (int args.[3]) args.[4]
            0
