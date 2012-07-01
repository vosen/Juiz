import time
import numpy as np
from scipy import sparse
from scipy.sparse import linalg

mat = sparse.rand(20000, 2000, 0.05, 'csc', np.dtype('d'))

# bench arpack
start = time.clock()
linalg.svds(mat, 15)
time_taken = time.clock() - start
print("arpack: %.2fs" % time_taken)

# bench svdlibc
from sparsesvd import sparsesvd
start = time.clock()
sparsesvd(mat, 15)
time_taken = time.clock() - start
print("svdlibc: %.2fs" % time_taken)