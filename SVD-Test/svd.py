import numpy, math, itertools
from scipy import sparse
from scipy.sparse import linalg

class svd:

    # k i s number of features avg is either 'movie' or 'user'
    # matrix should be sparse
    def __init__(self, matrix, features, avg='movie'):
        if(features < 1 or (avg != 'movie' and avg != 'user')):
            raise ValueError()
        matrix = matrix.tolil().asfptype()
        iter_matrix = matrix.tocoo()
        col_matrix = matrix.tocsc()
        self._calc_user_average_(matrix)
        self._calc_movie_average_(col_matrix)
        # fill empty spots
        self.normalization = avg
        self._normalize_matrix_(iter_matrix, matrix, avg)
        self.U, self.E, self.V = linalg.svds(matrix.tocoo(), k = features)

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

    def _normalize_matrix_(self, iter_matrix, matrix, method):
        #normalize by movie
        if method == 'movie':
            for i,j,v in itertools.izip(iter_matrix.row, iter_matrix.col, iter_matrix.data):
                matrix[i,j] = v - self.movie_averages[j]
        #normalize by user
        else:
            for i,j,v in itertools.izip(iter_matrix.row, iter_matrix.col, iter_matrix.data):
                matrix[i,j] = v - self.user_averages[i]

    # receive user vector with non-normalized scores
    def predict(self, vector):
        #normalize the vector
        if self.normalization == 'movie':
            pass
        else:
            pass