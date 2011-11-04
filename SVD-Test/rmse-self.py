# calc rmse for step, 2*step, 3*step, ... max inclusive

import itertools
import numpy, time, sys
from matplotlib import pyplot
from scipy import sparse
from scipy.io import loadmat, savemat
from scipy.sparse import linalg

def rmse_error1(a,b):
    return numpy.linalg.norm(a - b, 'fro') / ((a.shape[0] * a.shape[1]) ** 2)

def rmse_error2(sparse, deriv):
    count = 0
    sum_squared = 0
    for i,j,v in itertools.izip(sparse.row, sparse.col, sparse.data):
        count += 1
        sum_squared += ((v - deriv[i,j]) ** 2)
    return numpy.sqrt(sum_squared/count)

if(len(sys.argv) != 6):
    print("USAGE: rmse-self.py step max input.mat matrix_name out_image")
    quit()

# import matrix
#mat_dense = loadmat(sys.argv[3])[sys.argv[4]]
mat_sparse = loadmat(sys.argv[3])[sys.argv[4]].tocoo().asfptype()
x = []
y = []
step = int(sys.argv[1])
limit = int(sys.argv[2])
features = range(step,limit+1, step)
for i in features:
    u,e,vt = linalg.svds(mat_sparse, k = i)
    u = numpy.mat(u)
    e = numpy.mat(numpy.diagflat(e))
    vt = numpy.mat(vt)
    reconstructed = (u * e) * vt
    error = rmse_error2(mat_sparse, reconstructed)
    x.append(i)
    y.append(error)

pyplot.xlabel("features")
pyplot.ylabel("error")
pyplot.grid(True)
pyplot.title("RMSE betwen original and decomposed matrices")
pyplot.plot(x, y, 'ko')
pyplot.savefig(sys.argv[5])
