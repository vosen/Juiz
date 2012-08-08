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

    type Estimates = { Predicted : float array; MovieCount : int; UserCount : int }

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
        { Predicted = (data |> Array.map (fun rating -> rating.Score)); MovieCount = movies.Count; UserCount = users.Count }

    let constantBaseline x (data : Rating array) =
        let copied = copyBaseline data
        { Predicted = Array.init data.Length (fun _ -> x) ; MovieCount = copied.MovieCount; UserCount = copied.UserCount }

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
        let newRatings = Array.map (fun (rating : Rating) -> clamp (movieAverages.[rating.Title] + userDeviations.[rating.User])) data
        { Predicted = newRatings; MovieCount = movies.Count; UserCount = users.Count }

    let predictRatingWithTrailing score movieFeature userFeature features feature =
        score + movieFeature * userFeature
        |> clamp
        |> (+) (float(features - feature - 1) * defaultFeature * defaultFeature)
        |> clamp

    let predictRating score movieFeature userFeature =
        clamp (score + movieFeature * userFeature)

    let trainFeature (movieFeatures : float[][]) (userFeatures : float[][]) (ratings : Rating array) estimates features feature =
        let zippedRatings = Array.zip ratings estimates
        for i in 0..(epochs-1) do
            for rating, estimate in zippedRatings do
                let movieFeature = movieFeatures.[rating.Title].[feature]
                let userFeature = userFeatures.[rating.User].[feature]
                let predicted = predictRatingWithTrailing estimate movieFeature userFeature features feature
                let error = rating.Score - predicted
                movieFeatures.[rating.Title].[feature] <- movieFeature + (learningRate * (error * userFeature - regularization * movieFeature))
                userFeatures.[rating.User].[feature] <- userFeature + (learningRate * (error * movieFeature - regularization * userFeature))
        // now update estimates based on trained values
        zippedRatings |> Array.map (fun (rating, estimate) -> predictRating estimate movieFeatures.[rating.Title].[feature] userFeatures.[rating.User].[feature])

    let build (baseline : Rating array -> Estimates) (ratings : Rating array) features =
        let estimates = baseline ratings
        let movieFeatures, userFeatures = initializeFeatures estimates.MovieCount estimates.UserCount features
        let mutable workEstimates = estimates.Predicted
        for i in 0..(features-1) do
            workEstimates <- trainFeature movieFeatures userFeatures ratings workEstimates features i
        (movieFeatures, userFeatures)

    type Model(data : float[][]) =
        
        member inline private this.Features = data.[0].Length

        static member clampedDot start x y =
            Array.fold2 (fun sum a b -> clamp(sum + a*b)) start x y

        member this.PredictSingle (ratings : (int * float) array) target =
            let userFeatures = Array.init this.Features (fun _ -> defaultFeature)
            let mutable estimates = Array.init ratings.Length (fun _ -> 1.0)
            for feature in 0..(this.Features-1) do
                let zippedRatings = Array.zip ratings estimates
                for e in 0..(epochs-1) do
                    for rating, estimate in zippedRatings do
                        let movieFeature = data.[fst rating].[feature]
                        let userFeature = userFeatures.[feature]
                        let predicted = predictRatingWithTrailing estimate movieFeature userFeature this.Features feature
                        let error = (snd rating) - predicted
                        userFeatures.[feature] <- userFeature + (learningRate * (error * movieFeature - regularization * userFeature))
                // update estimates
                estimates <- zippedRatings |> Array.map (fun (rating, estimate) -> predictRating estimate data.[fst rating].[feature] userFeatures.[feature])
            // userFeatures is now features vector for this user
            Model.clampedDot 1.0 data.[target] userFeatures