import numpy, time, sys
import svd
from matplotlib import pyplot
from scipy import sparse
from scipy.io import loadmat, savemat
from scipy.sparse import linalg
from svd import svd

# probe list is a test vector from build_probe(...)
def rmse(func, probe_list):
    sum_squared = 0
    count = 0
    # users are rows
    for (id, score, ratings) in probe_list:
        error = score - func(id, ratings)
        count += 1
        sum_squared += (error **2)
    return numpy.sqrt(sum_squared / count)

def build_probe(matrix):
    # users are rows
    matrix = matrix.tolil()
    # test vector is a list of tuples (id_to_predict, score, [(id, score)])
    test_vectors = []
    for i in range(0, matrix.shape[0]):
        ratings_row = matrix.getrow(i)
        # user ratings is list of tuple (id, score)
        user_ratings = filter(lambda x: x[1] != 0, enumerate(ratings_row))
        if(len(user_ratings) > 1):
            for i, (id, score) in enumerate(user_ratings):
                copy = user_ratings[:]
                (test_id, test_score) = copy.pop(i)
                test_vectors.append(test_id, test_score, copy)
    return test_vectors

if(len(sys.argv) != 4):
    print("USAGE: rmse.py input.mat train_matrix test_matrix")
    quit()

train_matrix = loadmat(sys.argv[1])[sys.argv[2]]
test_matrix = loadmat(sys.argv[1])[sys.argv[3]]
svd_model = svd(train_matrix, 10)
raw_input()