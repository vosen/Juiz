import sys, gc, numpy, scipy, time, psutil, os
from scipy import sparse
from scipy.sparse import linalg


def measure_mat(str):
    gc.collect()
    mat = scipy.sparse.rand(6000,600, format = str, density = 0.001) * 10
    start = time.clock()
    for i in range(0, mat.shape[0]):
        row = mat.getrow(i)
        sum(row.toarray().flatten())
    time_taken = time.clock() - start 
    print("%s: %.2fs %.2fMB" %(str, time_taken, psutil.Process(os.getpid()).get_memory_info()[0]/float(1048576)))

# not measuring lil, dia and dok because they are retardedly inefficient
measure_mat('bsr')
measure_mat('coo')
measure_mat('csc')
measure_mat('csr')
#measure_mat('dia')
#measure_mat('dok')
measure_mat('lil')