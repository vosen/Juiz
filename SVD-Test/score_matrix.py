import numpy, math, itertools
from scipy import sparse
from scipy.sparse import linalg

class score_matrix:

    # k i s number of features avg is either 'movie' or 'user'
    # matrix should be sparse
    def __init__(self, matrix, avg='movie'):
        if(avg != 'movie' and avg != 'user'):
            raise ValueError()
        self.raw_matrix = matrix.tolil().asfptype()
        iter_matrix = self.raw_matrix.tocoo()
        col_matrix = self.raw_matrix.tocsc()
        self._calc_user_average_(self.raw_matrix)
        self._calc_movie_average_(col_matrix)
        # fill empty spots
        self.normalization = avg
        self._normalize_matrix_(iter_matrix, self.raw_matrix)

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

    def _normalize_matrix_(self, iter_matrix, matrix):
        #normalize by movie
        if self.normalization == 'movie':
            for i,j,v in itertools.izip(iter_matrix.row, iter_matrix.col, iter_matrix.data):
                matrix[i,j] = v - self.movie_averages[j]
        #normalize by user
        else:
            for i,j,v in itertools.izip(iter_matrix.row, iter_matrix.col, iter_matrix.data):
                matrix[i,j] = v - self.user_averages[i]