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
            arr |> Array.mapi (fun j score -> (j, row.[j], copyExceptOne arr j)))
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

    let run start step stop input train test output =
        let trainMatrix, testMatrix = loadMatrices input train test
        let probeSet = buildProbe testMatrix
        let trainSet = pickRatings trainMatrix
        [| start .. step .. stop |] 
        |> Array.Parallel.map (fun features -> (features, FunkSVD.Model(FunkSVD.build FunkSVD.simplePredictBaseline trainSet features |> fst)))
        |> Array.Parallel.map (fun (features, model) -> (features, measureRMSE model.PredictSingle probeSet))
        |> Array.map (fun (features, error) -> (printfn "(%d, %f)" features error))


    [<EntryPoint>]
    let main args =
        if args.Length <> 7 then
            printfn "USAGE: rmse.exe start step stop input.mat train_matrix test_matrix output"
            exit(0)
        run (int args.[0]) (int args.[1]) (int args.[2]) args.[3] args.[4] args.[5] args.[6] |> ignore
        0