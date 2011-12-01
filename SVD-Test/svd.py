import numpy
import math
import scipy.sparse

class svd:

    # k i s number of features avg is either 'movie' or 'user'
    def __init__(self, matrix, k, avg='movie'):
        matrix = matrix.astype(int)
        row_matrix = matrix.tolil()
        col_matrix = matrix.tocsc()
        if(k < 1 or (avg != 'movie' and avg != 'user')):
            raise ValueError()
        self._calc_user_average_(row_matrix)
        self._calc_movie_average_(col_matrix)
        normalize_matrix(matrix)

    def _calc_user_average_(self, matrix):
        # calc averages of rows
        rows_sum = matrix.sum(1)
        rows_sum.shape = (1, matrix.shape[0])
        rows_sum = rows_sum.tolist()[0]
        self.user_averages = [rows_sum[i] / float(matrix.getrowview(i).nnz) for i in range(0, matrix.shape[0])]

    def _calc_movie_average_(self, matrix):
        col_sum = matrix.sum(0)
        col_sum = col_sum.tolist()[0]
        self.movie_averages = [col_sum[i] / float(matrix.getcol(i).nnz) for i in range(0, matrix.shape[1])]
