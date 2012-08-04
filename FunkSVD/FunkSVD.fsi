namespace Vosen.Juiz
  module FunkSVD = begin

    (* public *)
    type Rating =
      struct
        new : title:int * user:int * score:float -> Rating
        val Title: int
        val User: int
        val Score: float
      end
    val build : Rating array -> float [] [] * float [] []

    (* internal *)
    type internal Estimates =
      {Ratings: Rating array;
       MovieCount: int;
       UserCount: int;}
    val internal initializeEstimates : Rating array -> Estimates
  end

