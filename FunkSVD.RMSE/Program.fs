namespace Vosen.Juiz

module RMSE =

    open MathNet.Numerics.LinearAlgebra.Generic
    open MathNet.Numerics.LinearAlgebra.Double
    type pair<'a,'b> = System.Collections.Generic.KeyValuePair<'a,'b>

    let loadMatrices (file : string) train test =
        let reader = MathNet.Numerics.LinearAlgebra.Double.IO.MatlabMatrixReader(file)
        let matrices = reader.ReadMatrices()
        (matrices.[train] :?> SparseMatrix, matrices.[test] :?> SparseMatrix)

    let readRating (str : string) =
        let parts = str.Split(' ')
        FunkSVD.Rating(int(parts.[0]), int(parts.[1]), float(parts.[2]))

    let readPredict (str : string) =
        let parts = str.Split(' ')
        pair(int(parts.[0]), float(parts.[1]))

    let loadTrainData (train : string) =
        (FunkSVD.loadArray readRating (System.IO.File.ReadAllText(train))).[0]

    let loadTestData (train : string) : (int * float * pair<int, float> array) array =
        let testString = System.IO.File.ReadAllText(train)
        let data = FunkSVD.loadArray readPredict testString
        data |> Array.map (fun row -> (row.[0].Key, row.[0].Value, Array.sub row 1 (row.Length - 1)))

    let buildProbe (testMatrix : SparseMatrix) =
        let copyExceptOne (arr : float array) i =
            let j = ref -1
            let newArray = Array.copy arr
            newArray.[i] <- 0.0
            newArray |> Array.choose (fun v -> j := !j+1; if v = 0.0 then None else Some(pair(!j,v)))
        testMatrix.RowEnumerator()
        |> Seq.collect (fun (i,row) ->
            let arr = row.ToArray() 
            arr 
            |> Array.mapi (fun j score -> if score = 0.0 then None else Some(j, row.[j], copyExceptOne arr j))
            |> Array.choose id)
        |> Seq.toArray

    let pickRatings (sparse : SparseMatrix)=
        sparse.IndexedEnumerator()
        |> Seq.choose (fun (i,j,v) -> if v = 0.0 then None else Some(FunkSVD.Rating(j,i,v)))
        |> Seq.toArray

    let measureRMSE (predict : pair<int, float>[] -> int -> float) probeSet =
        let mutable sum = 0.0
        let mutable count = 0
        for target, score, ratings in probeSet do
            let error = (predict ratings target) - score
            sum <- sum + (error*error)
            count <- count + 1
        sqrt (sum / float(count))

    let measureSelfNullRMSE (generator : Vosen.Juiz.FunkSVD.Rating array -> float[][] * float[][]) ratings =
        let movieFeatures, userFeatures = generator ratings
        let mutable sum = 0.0
        let mutable count = 0
        for rating in ratings do
            let predict = (Vosen.Juiz.FunkSVD.clampedDot 1.0 movieFeatures.[rating.Title] userFeatures.[rating.User])
            printfn "expected: %f, got: %f" rating.Score predict 
            let error = rating.Score - predict
            sum <- sum + (error*error)
            count <- count + 1
        sqrt (sum / float(count))


    let exportDataGraph data path =
        let model = OxyPlot.PlotModel("RMSE in relation to features")
        let series = OxyPlot.LineSeries()
        series.Points <- data |> Array.map (fun rating -> OxyPlot.DataPoint(float(fst rating), snd rating) :> OxyPlot.IDataPoint)
        series.MarkerType <- OxyPlot.MarkerType.Cross
        series.StrokeThickness <- 0.0
        series.MarkerStroke <- OxyPlot.OxyColors.Black;
        series.MarkerSize <- 4.0
        model.Series.Add(series)
        OxyPlot.WindowsForms.PngExporter.Export(model, path + ".png", 800, 600, System.Drawing.Brushes.White)

    let measuresToString measured =
        measured |> Array.fold (fun acc (features,error) -> acc + (sprintf "%d, %.6f\n" features error)) ""

    let run start step stop train test output =
        let trainSet = loadTrainData train
        let probeSet = loadTestData test
        let measured =
            [| start .. step .. stop |] 
            |> Array.Parallel.map (fun featuresCount ->
                let avgs = ref Unchecked.defaultof<float array>
                let features, userAvgs = ((FunkSVD.buildAsync (FunkSVD.averagesBaseline >> (fun (av, est) -> avgs := av; est)) trainSet featuresCount) |> fst).Result
                let model = FunkSVD.Model(features, userAvgs)
                let error = measureRMSE (model.PredictSingle (FunkSVD.Model.averagesBaseline !avgs)) probeSet
                (featuresCount, error))
        // dump the data to text file
        System.IO.File.WriteAllText(output + "-raw.txt", measuresToString measured)
        // graph measured data
        exportDataGraph measured output


    [<EntryPoint>]
    let main args =
        if args.Length <> 6 then
            printfn "USAGE: rmse.exe start step stop train_probe test_probe output"
            exit(0)
        run (int args.[0]) (int args.[1]) (int args.[2]) args.[3] args.[4] args.[5]
        0