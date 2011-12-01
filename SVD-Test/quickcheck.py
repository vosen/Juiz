import numpy
import scipy
from scipy.sparse import linalg
test_mat = scipy.sparse.csc_matrix([[1, 2, 3,], [4, 5, 6], [7, 8, 9]], dtype=float).tocsc()
u,e,v = linalg.svds(test_mat,2)
print "U:\n" + str(u) + "\n"
print "E:\n" + str(e) + "\n"
print "Vh:\n" +  str(v) + "\n"
print (numpy.mat(u) * numpy.diagflat(e)) * numpy.mat(v)