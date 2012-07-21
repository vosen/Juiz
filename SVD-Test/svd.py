import numpy, math, itertools
from scipy import sparse
from scipy.sparse import linalg

class svd:

    def __init__(self, score_matrix, features):
        self.source = score_matrix
        u, e, vt = linalg.svds(self.source.raw_matrix.transpose().tocoo(), k = features)
        self.normalization = self.source.normalization
        e_inv = numpy.mat(numpy.diagflat(e)).getI()
        e_sqrt = numpy.sqrt(numpy.diagflat(e))
        u_mat = numpy.mat(u)
        self.terms = u_mat * e_sqrt
        self.documents = e_sqrt * e_inv * u_mat.transpose()

    # receive user vector with non-normalized scores
    def predict(self, id, rankings):
        normal_vector = self.normalize_vector(self.vectorize(rankings))
        query = self.documents * normal_vector
        if self.normalization == 'movie':
            return numpy.dot(query, self.terms[id,:])[0,0] + self.source.movie_averages[id]
        else:
            return numpy.dot(query, self.terms[id,:])[0,0] + numpy.mean(map(lambda x: x[1], rankings))

    # we get a list of id and score => [(id, score)]
    # return full list of scores (also filled with zeros)
    def vectorize(self, rankings):
        vector = numpy.zeros(len(self.source.movie_averages))
        for id, score in rankings:
            vector[id] = score
        return vector

    def normalize_vector(self, vector):
        if self.normalization == 'movie':
            for id in vector.nonzero()[0]:
                vector[id] -= self.source.movie_averages[id]
        else:
            nnz_ids = vector.nonzero()[0]
            nnz_values = map(lambda x: vector[x], vector.nonzero()[0])
            mean = numpy.mean(nnz_values)
            for id in nnz_ids:
                vector[id] -= mean
        return numpy.mat(vector).transpose()