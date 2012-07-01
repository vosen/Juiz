import sys, numpy, scipy, time
from scipy import sparse
from scipy.sparse import linalg

def measure_mat_csc(mat, nnzs):
    start = time.clock()
    csc = sparse.csc_matrix((20000, 200))
    for (i,j) in nnzs:
        csc[i,j] = mat[i,j]
    time_taken = time.clock() - start
    print("csc: %.2fs" %  time_taken)

def measure_mat_lil(mat, nnzs):
    start = time.clock()
    temp = sparse.lil_matrix((20000, 200))
    for (i,j) in nnzs:
        temp[i,j] = mat[i,j]
    temp = temp.tocsc()
    time_taken = time.clock() - start
    print("lil: %.2fs" % time_taken)

test_mat = scipy.sparse.rand(20000,200, format = 'lil', density = 0.005)
nnz = test_mat.nonzero()
nnzs = zip(nnz[0], nnz[1])
measure_mat_lil(test_mat, nnzs)
measure_mat_csc(test_mat, nnzs)