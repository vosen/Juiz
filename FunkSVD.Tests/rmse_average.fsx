#I "bin\\Release"
#r "FunkSVD"
#r "FunkSVD.RMSE"
#r "MathNet.Numerics"

open Vosen.Juiz.RMSE

let trainMatrix, testMatrix = loadMatrices @"C:\Users\vosen\Documents\Visual Studio 2010\Projects\Juiz\FunkSVD.RMSE\bin\Release\mal.mat" "train" "test"
let ratings = pickRatings trainMatrix
let probe = Vosen.Juiz.RMSE.buildProbe testMatrix
let avgs, ests = Vosen.Juiz.FunkSVD.averagesBaseline ratings
printfn "%f" (Vosen.Juiz.RMSE.measureRMSE (fun ratings id -> avgs.[id]) probe)