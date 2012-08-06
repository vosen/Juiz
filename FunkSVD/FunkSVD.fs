namespace Vosen.Juiz

module FunkSVD =

    open System.Collections.Generic

    [<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FunkSVD.Tests")>]
    do()

    let defaultFeature = 0.1
    let learningRate = 0.001
    let epochs = 100
    let regularization = 0.015

    type Rating =
        struct
            val Title : int
            val User : int
            val Score : float
            new(title, user, score) = { Title = title; User = user; Score = score }
        end

    type Estimates = { Ratings : Rating array; MovieCount : int; UserCount : int }

    type Dictionary<'TKey, 'TValue> with
        member this.AddOrSet addFunc setFunc (key : 'TKey)=
            let mutable tempValue = Unchecked.defaultof<'TValue>
            if this.TryGetValue(key, &tempValue) then
                this.[key] <- setFunc tempValue
            else
                this.[key] <- addFunc()

    let initializeFeatures titleCount userCount featCount =
        let titleFeatures = Array.init titleCount (fun idx -> Array.init featCount (fun _ -> defaultFeature))
        let userFeatures = Array.init userCount (fun idx -> Array.init featCount (fun _ -> defaultFeature))
        (titleFeatures, userFeatures)

    let clamp x = max 1.0 (min x 10.0)

    let copyBaseline (data : Rating array) =
        let movies = HashSet<int>()
        let users = HashSet<int>()
        for rating in data do
            movies.Add(rating.Title) |> ignore
            users.Add(rating.User) |> ignore
        { Ratings = data; MovieCount = movies.Count; UserCount = users.Count }

    let nullBaseline (data : Rating array) =
        let copied = copyBaseline data
        { Ratings = copied.Ratings |> Array.map (fun rating -> Rating(rating.Title, rating.User, 1.0)) ; MovieCount = copied.MovieCount; UserCount = copied.UserCount }

    let simplePredictBaseline (data : Rating array) =
        let movies = Dictionary<int, (int * float)>()
        let users = Dictionary<int, List<int * float>>()
        for i in 0..(data.Length-1) do
            movies.AddOrSet (fun _ -> (1, data.[i].Score)) (fun (oldCount, oldSum) -> (oldCount + 1, oldSum + data.[i].Score)) data.[i].Title
            users.AddOrSet (fun _ -> List([| (data.[i].Title, data.[i].Score) |])) (fun ratingList -> ratingList.Add(data.[i].Title, data.[i].Score); ratingList) data.[i].User
        // We've got metrics loaded, now calculate movie averages
        let calculateMovieAverages (dic : Dictionary<_,_>) idx =
            let tempTuple = dic.[idx]
            snd tempTuple / float(fst tempTuple)
        let movieAverages = Array.init movies.Count (calculateMovieAverages movies)
        // now calculate user bias
        let deviation (ratings :  List<int * float>) = 
            (Seq.sumBy (fun (movie, rating) -> movieAverages.[movie] - float(rating)) ratings) / float(ratings.Count)
        let userDeviations = 
            users
            |> Seq.map (fun userRatings -> (deviation userRatings.Value))
            |> Seq.toArray
        let newRatings = Array.map (fun (rating : Rating) -> Rating(rating.Title, rating.User, clamp (movieAverages.[rating.Title] + userDeviations.[rating.User]))) data
        { Ratings = newRatings; MovieCount = movies.Count; UserCount = users.Count }

    let predictRating score movieFeature userFeature features feature =
        score + movieFeature * userFeature
        |> clamp
        |> (+) (float(features - feature - 1) * defaultFeature * defaultFeature)
        |> clamp

    let trainFeature (movieFeatures : float[][]) (userFeatures : float[][]) (ratings : Rating array) features feature =
        for i in 0..(epochs-1) do
            let mutable sq = 0.0
            for rating in ratings do
                let movieFeature = movieFeatures.[rating.Title].[feature]
                let userFeature = userFeatures.[rating.User].[feature]
                let predicted = predictRating rating.Score movieFeature userFeature features feature
                let error = rating.Score - predicted
                sq <- sq + (error * error)
                movieFeatures.[rating.Title].[feature] <- movieFeature + (learningRate * (error * userFeature - regularization * movieFeature))
                userFeatures.[rating.User].[feature] <- userFeature + (learningRate * (error * movieFeature - regularization * userFeature))
            printfn "epoch: %d, rmse: %f" i (sqrt(sq / float(ratings.Length)))
        // now update ratings based on trained values
        ratings |> Array.map (fun rating -> Rating(rating.Title, rating.User, clamp (rating.Score + movieFeatures.[rating.Title].[feature] * userFeatures.[rating.User].[feature])))

    let build (baseline : Rating array -> Estimates) (data : Rating array) features =
        let mutable ratings = Array.copy data
        let userEstimates = baseline ratings
        let movieFeatures, userFeatures = initializeFeatures userEstimates.MovieCount userEstimates.UserCount features
        for i in 0..(features-1) do
            ratings <- trainFeature movieFeatures userFeatures ratings features i
        (movieFeatures, userFeatures)

    type Model(data : float[][]) =
        
        member inline private this.Features = data.[0].Length

        static member clampedDot x y =
            Array.fold2 (fun sum a b -> clamp(sum + a*b)) 0.0 x y

        member this.PredictSingle (ratings : (int * float) array) target =
            let userFeatures = Array.init this.Features (fun _ -> defaultFeature)
            for feature in 0..(this.Features-1) do
                for e in 0..(epochs-1) do
                    for rating in ratings do
                        let movieFeature = data.[fst rating].[feature]
                        let userFeature = userFeatures.[feature]
                        let predicted = predictRating (snd rating) movieFeature userFeature this.Features feature
                        let error = (snd rating) - predicted
                        userFeatures.[feature] <- userFeature + (learningRate * (error * movieFeature - regularization * userFeature))
            // userFeatures is now features vector for this user
            Model.clampedDot data.[target] userFeatures