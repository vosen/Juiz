namespace Vosen.Juiz

module FunkSVD =

    open System.Collections.Generic

    [<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FunkSVD.Tests")>]
    do()

    let defaultFeature = 0.1
    let learningRate = 0.001
    let epochs = 100
    let regularization = 0.015
    let minimumImprovement = 0.0001

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

    let userDeviation (movieAverages : float array) (ratings :  IList<int * float>) = 
        (Seq.sumBy (fun (movie, rating) -> movieAverages.[movie] - float(rating)) ratings) / float(ratings.Count)

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
        let userDeviations = 
            users
            |> Seq.map (fun userRatings -> (userDeviation movieAverages userRatings.Value))
            |> Seq.toArray
        let newRatings = Array.map (fun (rating : Rating) -> clamp (movieAverages.[rating.Title] + userDeviations.[rating.User])) data
        (movieAverages, { Predicted = newRatings; MovieCount = movies.Count; UserCount = users.Count })

    let averagesBaseline (data : Rating array) =
        let movies = Dictionary<int, (int * float)>()
        let users = Dictionary<int, List<int * float>>()
        for i in 0..(data.Length-1) do
            movies.AddOrSet (fun _ -> (1, data.[i].Score)) (fun (oldCount, oldSum) -> (oldCount + 1, oldSum + data.[i].Score)) data.[i].Title
            users.AddOrSet (fun _ -> List([| (data.[i].Title, data.[i].Score) |])) (fun ratingList -> ratingList.Add(data.[i].Title, data.[i].Score); ratingList) data.[i].User
        let calculateMovieAverages (dic : Dictionary<_,_>) idx =
            let tempTuple = dic.[idx]
            snd tempTuple / float(fst tempTuple)
        let movieAverages = Array.init movies.Count (calculateMovieAverages movies)
        let newRatings = data |> Array.map (fun rating -> movieAverages.[rating.Title])
        (movieAverages, { Predicted = newRatings; MovieCount = movies.Count; UserCount = users.Count })

    let predictRatingWithTrailing score movieFeature userFeature features feature =
        score + movieFeature * userFeature
        |> clamp
        |> (+) (float(features - feature - 1) * defaultFeature * defaultFeature)
        |> clamp

    let predictRating score movieFeature userFeature =
        clamp (score + movieFeature * userFeature)

    let trainFeature (movieFeatures : float[][]) (userFeatures : float[][]) (ratings : Rating array) estimates features feature =
        let zippedRatings = Array.zip ratings estimates
        let mutable epoch = 0
        let mutable rmse, lastRmse = (0.0, infinity)
        while (epoch < epochs) || (rmse <= lastRmse - minimumImprovement) do
            lastRmse <- rmse
            let mutable squaredError = 0.0
            for rating, estimate in zippedRatings do
                let movieFeature = movieFeatures.[rating.Title].[feature]
                let userFeature = userFeatures.[rating.User].[feature]
                let predicted = predictRatingWithTrailing estimate movieFeature userFeature features feature
                let error = rating.Score - predicted
                squaredError <- squaredError + (error * error)
                movieFeatures.[rating.Title].[feature] <- movieFeature + (learningRate * (error * userFeature - regularization * movieFeature))
                userFeatures.[rating.User].[feature] <- userFeature + (learningRate * (error * movieFeature - regularization * userFeature))
            rmse <- sqrt(squaredError / float(zippedRatings.Length))
            epoch <- epoch + 1
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

        static member simplePredictBaseline (avgs : float array) (ratings : (int * float) array) =
            let deviation = userDeviation avgs ratings
            avgs |> Array.map ((+) deviation)

        static member averagesBaseline (avgs : float array) (ratings : (int * float) array) =
            avgs |> Array.copy

        static member clampedDot start x y =
            Array.fold2 (fun sum a b -> clamp(sum + a*b)) start x y

        member this.Features = data.[0].Length

        member this.PredictSingle (baseline : (int * float) array -> float array) (ratings : (int * float) array) target =
            let userFeatures = Array.init this.Features (fun _ -> defaultFeature)
            let estimates = ref (baseline ratings)
            for feature in 0..(this.Features - 1) do
                for e in 0..(epochs-1) do
                    for (id, score) in ratings do
                        let movieFeature = data.[id].[feature]
                        let userFeature = userFeatures.[feature]
                        let predicted = predictRatingWithTrailing (!estimates).[id] movieFeature userFeature this.Features feature
                        let error = score - predicted
                        userFeatures.[feature] <- userFeature + (learningRate * (error * movieFeature - regularization * userFeature))
                // update estimates
                for (id, _) in ratings do
                    (!estimates).[id] <- predictRating (!estimates).[id] data.[id].[feature] userFeatures.[feature]
            // userFeatures is now features vector for this user
            Model.clampedDot (!estimates).[target] data.[target] userFeatures