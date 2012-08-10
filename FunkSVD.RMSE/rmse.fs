namespace Vosen.Juiz

module RMSE =

    open MathNet.Numerics.LinearAlgebra.Generic
    open MathNet.Numerics.LinearAlgebra.Double

    let loadMatrices (file : string) train test =
        let reader = MathNet.Numerics.LinearAlgebra.Double.IO.MatlabMatrixReader(file)
        let matrices = reader.ReadMatrices()
        (matrices.[train] :?> SparseMatrix, matrices.[test] :?> SparseMatrix)

    let buildProbe (testMatrix : SparseMatrix) =
        let copyExceptOne (arr : float array) i =
            let j = ref -1
            let newArray = Array.copy arr
            newArray.[i] <- 0.0
            newArray |> Array.choose (fun v -> j := !j+1; if v = 0.0 then None else Some((!j,v)))
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

    let measureRMSE (predict : (int * float)[] -> int -> float) probeSet =
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
            let predict = (Vosen.Juiz.FunkSVD.Model.clampedDot 1.0 movieFeatures.[rating.Title] userFeatures.[rating.User])
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

    let run start step stop input train test output =
        let trainMatrix, testMatrix = loadMatrices input train test
        let probeSet = buildProbe testMatrix
        let trainSet = pickRatings trainMatrix
        let measured =
            [| start .. step .. stop |] 
            |> Array.Parallel.map (fun features ->
                let model = FunkSVD.Model(FunkSVD.build (FunkSVD.constantBaseline 1.0) trainSet features |> fst)
                let error = measureRMSE model.PredictSingle probeSet
                (features, error))
        // dump the data to text file
        System.IO.File.WriteAllText(output + "-raw.txt", measuresToString measured)
        // graph measured data
        exportDataGraph measured output


    [<EntryPoint>]
    let main args =
        if args.Length <> 7 then
            printfn "USAGE: rmse.exe start step stop input.mat train_matrix test_matrix output"
            exit(0)
        run (int args.[0]) (int args.[1]) (int args.[2]) args.[3] args.[4] args.[5] args.[6]
        0