namespace Vosen.Juiz
    module FunkSVD =
        type Rating =
            struct
                val Title : int
                val User : int
                val Score : float
            end
        val build : (Rating array) -> unit