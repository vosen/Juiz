#I "bin\\Release"
#r "FunkSVD"
#r "FunkSVD.RMSE"
#r "MathNet.Numerics"

open Vosen.Juiz.RMSE

let ratings = Vosen.Juiz.RMSE.loadTrainData @"C:\Users\vosen\Documents\Visual Studio 2010\Projects\Juiz\FunkSVD.ModelGenerator\probe-train"
let probe = Vosen.Juiz.RMSE.loadTestData @"C:\Users\vosen\Documents\Visual Studio 2010\Projects\Juiz\FunkSVD.ModelGenerator\probe-test"
let avgs, ests = Vosen.Juiz.FunkSVD.averagesBaseline ratings
printfn "%f" (Vosen.Juiz.RMSE.measureRMSE (fun ratings id -> avgs.[id]) probe)