#I "bin\\Release"
#r "FunkSVD.RMSE"

open Vosen.Juiz.RMSE

let data = [|(1, 0.9); (2, 0.85); (3, 0.8); (4, 0.7); (5, 0.6) |]
Vosen.Juiz.RMSE.exportDataGraph data "test"