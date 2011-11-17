import sys, sqlite3, random, scipy, numpy
from scipy import sparse
from scipy.sparse import lil_matrix
from scipy.io import loadmat, savemat, mmwrite
from collections import defaultdict

class PickTrainingMatrices(object):

    # returns anime_id, score, user_id tuple
    def pick_scores(self, db, ids):
        scores = defaultdict(list)
        raw_scores = db.execute("SELECT Anime_Id, Score, User_Id FROM Seen WHERE User_Id IN (" + ", ".join(map(lambda x: str(x), ids)) + ")").fetchall()
        for v in raw_scores:
            scores[v[0]].append((v[2], v[1]))
        return scores

    def data_to_matrix(self, db, ids):
        scores = self.pick_scores(db, ids)
        sorted_ids = {}
        for i, id in enumerate(sorted(ids)):
            sorted_ids[id] = i
        matrix = lil_matrix((len(ids), max(scores.keys())+1), dtype=numpy.dtype(numpy.int8))
        for (anime, ratings) in scores.iteritems():
            for (user, score) in ratings:
                matrix[sorted_ids[user], anime] = score
        return matrix

    # remove problematic movies with score only in one of the datasets
    def trim_matrices(self, *matrices):
        new_matrices = self.trim_cols(*matrices)
        if all(map(lambda x: id(x[0]) == id(x[1]), zip(new_matrices, matrices))):
            return matrices
        new_matrices = self.trim_rows(*new_matrices)
        return self.trim_matrices(*new_matrices)

    @staticmethod
    def trim_cols(*matrices):
        # check columns
        nnz_cols = set(reduce(lambda m,n: m.intersection(n), map(lambda m: set(m.nonzero()[1]), matrices)))
        if len(nnz_cols) == matrices[0].shape[1]:
            return matrices
        # get cols
        new_matrices = []
        i = 0
        for mat in matrices:
            columns = []
            for j in range(0, mat.shape[1]):
                if j in nnz_cols:
                    columns.append(mat.getcol(j).transpose().toarray()[0])
            new_matrices.append(sparse.lil_matrix(columns).transpose())
        return new_matrices

    @staticmethod
    def trim_rows(*matrices):
        # check columns
        nnz_rows = set(reduce(lambda m,n: m.intersection(n), map(lambda m: set(m.nonzero()[0]), matrices)))
        if len(nnz_rows) == matrices[0].shape[0]:
            return matrices
        # get cols
        new_matrices = []
        i = 0
        for mat in matrices:
            rows = []
            for j in range(0, mat.shape[0]):
                if j in nnz_rows:
                    rows.append(mat.getrow(j).toarray()[0])
            new_matrices.append(sparse.lil_matrix(rows))
        return new_matrices

    def run(self, ratio, input_db, output_mat):
        db = sqlite3.connect(input_db)
        # assume no empty users
        users = db.execute("""SELECT Users.[Id] FROM Users""").fetchall()
        # pick <ratio> of them for training db, pick <ratio/10> of them for test db
        train_ids = []
        test_ids = []

        test_threshold = ratio/10
        train_threshold = test_threshold + ratio
        for u in users:
            rnd = random.random()
            if (rnd <= test_threshold):
                test_ids.append(u[0])
            elif (rnd <= train_threshold):
                train_ids.append(u[0])

        train_matrix = self.data_to_matrix(db, train_ids).tocsc()
        test_matrix = self.data_to_matrix(db, test_ids).tocsc()

        (train_matrix, test_matrix) = self.trim_matrices(train_matrix, test_matrix)

        savemat(output_mat, {'train' : train_matrix,'test' : test_matrix}, oned_as = 'row')
        mmwrite(output_mat + '.train', train_matrix)
        mmwrite(output_mat + '.test', test_matrix)
        print("Done!")

    def main(self):
        if(len(sys.argv) != 4):
            print("USAGE: pick_test_db.py ratio input.db output.mat")
            quit()
        self.run(float(sys.argv[1]), sys.argv[2], sys.argv[3])

if __name__ == '__main__':
    PickTrainingMatrices().main()