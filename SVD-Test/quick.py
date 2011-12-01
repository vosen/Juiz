import numpy, time, sys
from matplotlib import pyplot
from scipy import sparse
from scipy.io import loadmat, savemat
from scipy.sparse import linalg

work_matrix = loadmat('test.mat')['train'].tocsc().asfptype()
start = time.clock()
print('rank %s' % str(sparse.rank(work_matrix)))
linalg.svds(work_matrix, k = 12366712323378123569251381237812378)
taken = time.clock() - start
print(str(taken))