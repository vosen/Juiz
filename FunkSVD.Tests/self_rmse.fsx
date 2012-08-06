#I "bin\\Release"
#r "FunkSVD"
#r "FunkSVD.RMSE"
#r "MathNet.Numerics"

open Vosen.Juiz.RMSE

let trainMatrix, testMatrix = loadMatrices @"C:\Users\vosen\Documents\Visual Studio 2010\Projects\Juiz\FunkSVD.RMSE\bin\Release\mal.mat" "train" "test"
let ratings = pickRatings trainMatrix
let estimates = (Vosen.Juiz.FunkSVD.simplePredictBaseline ratings).Ratings
printfn "%f" (measureSelfRMSE (fun r -> Vosen.Juiz.FunkSVD.build Vosen.Juiz.FunkSVD.copyBaseline r 10) estimates)