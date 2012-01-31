import numpy, time, sys
from matplotlib import pyplot
from scipy import sparse
from scipy.io import loadmat, savemat
from scipy.sparse import linalg
from svd import svd
from score_matrix import score_matrix

class rmse_tester:

    def __init__(self):
        self.start = 16
        self.stop = 17
        self.step = 1
        train_matrix = loadmat(sys.argv[1])[sys.argv[2]]
        test_matrix = loadmat(sys.argv[1])[sys.argv[3]]
        self.test_vectors = self.build_probe(test_matrix)
        print "Probe vectors generated"
        self.generate_models(train_matrix)
        print "Models generated"

        movie_based_features = []
        movie_based_rmse = []
        for i, model in enumerate(self.movie_normalized_models):
            result = self.rmse(model.predict, self.test_vectors)
            movie_based_features.append(i * self.step + self.start)
            movie_based_rmse.append(result)
            print "Measured RMSE for %s-featured, movie-averaged SVD." % (i * self.step + self.start)

        user_based_features = []
        user_based_rmse = []
        for i, model in enumerate(self.user_normalized_models):
            result = self.rmse(model.predict, self.test_vectors)
            user_based_features.append(i * self.step + self.start)
            user_based_rmse.append(result)
            print "Measured RMSE for %s-featured, user-averaged SVD." % (i * self.step + self.start)

        # now plot it
        pyplot.xlabel("features")
        pyplot.ylabel("error")
        pyplot.grid(True)
        pyplot.title("RMSE of movie-averaged predictor.")
        pyplot.plot(movie_based_features, movie_based_rmse, 'ko')
        pyplot.savefig(sys.argv[4] + "-movies")
        pyplot.clf()

        pyplot.xlabel("features")
        pyplot.ylabel("error")
        pyplot.grid(True)
        pyplot.title("RMSE of user-averaged predictor.")
        pyplot.plot(user_based_features, user_based_rmse, 'ko')
        pyplot.savefig(sys.argv[4] + "-users")

    # probe list is a test vector from build_probe(...)
    def rmse(self, func, probe_list):
        sum_squared = 0
        count = 0
        # users are rows
        for (id, score, ratings) in probe_list:
            error = score - func(id, ratings)
            count += 1
            sum_squared += (error **2)
        return numpy.sqrt(sum_squared / count)

    def build_probe(self, matrix):
        # users are rows
        matrix = matrix.tolil()
        # test vector is a list of tuples (id_to_predict, score, [(id, score)])
        test_vectors = []
        for i in range(0, matrix.shape[0]):
            ratings_row = matrix.getrow(i).toarray()[0]
            # user ratings is list of tuple (id, score)
            user_ratings = filter(lambda x: x[1] != 0, enumerate(ratings_row))
            if(len(user_ratings) > 1):
                for i, (id, score) in enumerate(user_ratings):
                    copy = user_ratings[:]
                    (test_id, test_score) = copy.pop(i)
                    test_vectors.append((test_id, test_score, copy))
        return test_vectors

    def generate_models(self, train_matrix):
        scores_by_movie = score_matrix(train_matrix, avg='movie')
        scores_by_user = score_matrix(train_matrix, avg='user')
        self.movie_normalized_models = []
        self.user_normalized_models = []
        for i in range(self.start, self.stop, self.step):
            self.movie_normalized_models.append(svd(scores_by_movie, i))
            self.user_normalized_models.append(svd(scores_by_user, i))

if __name__ == '__main__':
    if(len(sys.argv) != 5):
        print("USAGE: rmse.py input.mat train_matrix test_matrix output")
        quit()
    rmse_tester()