import numpy, math, itertools
from scipy import sparse
from scipy.sparse import linalg

class svd:

    def __init__(self, score_matrix, features):
        self.source = score_matrix
        u, e, vt = linalg.svds(self.source.raw_matrix.transpose().tocoo(), k = features)
        self.normalization = self.source.normalization
        self.Ut = numpy.mat(u).transpose()
        self.E_inv = numpy.mat(numpy.diagflat(e)).getI()
        self.E_sqrt = numpy.sqrt(numpy.diagflat(e))
        self.Vt_rolled = self.E_sqrt * numpy.mat(vt) 

    # receive user vector with non-normalized scores
    def predict(self, id, rankings):
        normal_vector = self.normalize_vector(self.vectorize(rankings))
        query = (self.E_inv * self.Ut * normal_vector).transpose() * self.E_sqrt
        if self.normalization == 'movie':
            return numpy.dot(query, self.Vt_rolled[:,2])[0,0] + self.source.movie_averages[id]
        else:
            return numpy.dot(query, self.Vt_rolled[:,2])[0,0] + numpy.mean(map(lambda x: x[1], rankings))

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
            nnz = vector.nonzero()[0]
            mean = numpy.mean(nnz)
            for id in nnz:
                vector[id] -= mean
        return numpy.mat(vector).transpose()