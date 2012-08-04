namespace Vosen.Juiz

module FunkSVD =

    open System.Collections.Generic

    [<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FunkSVD.Tests")>]
    do()

    let defaultFeature = 0.1
    let learningRate = 0.001
    let epochs = 100
    let regularization = 0.015
    let features = 20

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
        let titleFeatures = Array.init featCount (fun idx -> Array.init titleCount (fun _ -> defaultFeature))
        let userFeatures = Array.init featCount (fun idx -> Array.init userCount (fun _ -> defaultFeature))
        (titleFeatures, userFeatures)

    let clamp x = max 1.0 (min x 10.0)

    let initializeEstimates (data : Rating array) =
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

    let predictRating (rating : Rating) movieFeature userFeature feature =
        rating.Score + movieFeature * userFeature
        |> clamp
        |> (+) (float(features - feature - 1) * defaultFeature * defaultFeature)
        |> clamp

    let trainFeature (movieFeatures : float[][]) (userFeatures : float[][]) (ratings : Rating array) feature =
        for rating in ratings do
           let movieFeature = movieFeatures.[feature].[rating.Title]
           let userFeature = userFeatures.[feature].[rating.User]
           let predicted = predictRating rating movieFeature userFeature feature
           let error = rating.Score - predicted
           let movieFeature = movieFeatures.[feature].[rating.Title]
           let userFeature = userFeatures.[feature].[rating.User]
           movieFeatures.[feature].[rating.Title] <- movieFeature + (learningRate * (error * userFeature - regularization * movieFeature))
           userFeatures.[feature].[rating.User] <- userFeature + (learningRate * (error * movieFeature - regularization * userFeature))

    let build (data : Rating array) =
        let userEstimates = initializeEstimates data
        let movieFeatures, userFeatures = initializeFeatures userEstimates.MovieCount userEstimates.UserCount features
        for i in 0..(features-1) do
            trainFeature movieFeatures userFeatures data i
        (movieFeatures, userFeatures)