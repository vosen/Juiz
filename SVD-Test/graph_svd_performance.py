import numpy, time, sys
from matplotlib import pyplot
from scipy import sparse
from scipy.io import loadmat, savemat
from scipy.sparse import linalg

if(len(sys.argv) != 5):
    print("USAGE: graph_svd_performance.py threshold input.mat matrix_name out_image")
    quit()

# import matrix
work_matrix = loadmat(sys.argv[2])[sys.argv[3]].tocsc().asfptype()
x = []
y = []
# set threshold
threshold = float(sys.argv[1])
i = 1
time_taken = 0
while time_taken < threshold:
    start = time.clock()
    linalg.svds(work_matrix, k = i)
    time_taken = time.clock() - start
    x.append(i)
    y.append(time_taken)
    print("Measured for k = %d" % i)
    i += 1

pyplot.xlabel("number of features")
pyplot.ylabel("time (s)")
pyplot.grid(True)
pyplot.title('svd plot for matrix of size [%s x %s] with %s non-zero elements' % (str(work_matrix.shape[0]), str(work_matrix.shape[1]), str(work_matrix.nnz)))
pyplot.plot(x, y, 'k--o')
pyplot.savefig(sys.argv[4])