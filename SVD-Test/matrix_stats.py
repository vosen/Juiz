import sys
from scipy.io import loadmat

if(len(sys.argv) != 3):
    print("USAGE: matrix_stats.py input.mat name")
    quit()

mat = loadmat(sys.argv[1])[sys.argv[2]]

print "Matrix of shape [%s, %s], with %s non-zero elements." % (mat.shape[0], mat.shape[1], mat.nnz)
print "Fill: %.2f%%" % (100 * mat.nnz / float(mat.shape[0] * mat.shape[1])) 