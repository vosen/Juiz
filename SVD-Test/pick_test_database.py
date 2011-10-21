import sys, sqlite3, random, scipy, numpy
from scipy.sparse import lil_matrix
from scipy.io import loadmat, savemat
from collections import defaultdict

# returns anime_id, score, user_id tuple
def pick_scores(db, ids):
    scores = defaultdict(list)
    raw_scores = db.execute("SELECT Anime_Id, Score, User_Id FROM Seen WHERE User_Id IN (" + ", ".join(map(lambda x: str(x), ids)) + ")").fetchall()
    for v in raw_scores:
        scores[v[0]].append((v[2], v[1]))
    return scores

def data_to_matrix(db, ids):
    scores = pick_scores(db, ids)
    sorted_ids = {}
    for i, id in enumerate(sorted(ids)):
        sorted_ids[id] = i
    matrix = lil_matrix((len(ids), max(scores.keys())+1), dtype=numpy.dtype(numpy.int8))
    for (anime, ratings) in scores.iteritems():
        for (user, score) in ratings:
            matrix[sorted_ids[user], anime] = score
    return matrix

if(len(sys.argv) != 3):
    print("USAGE: pick_test_database.py input.db output.mat")
    quit()

db = sqlite3.connect(sys.argv[1])
# assume no empty users
users = db.execute("""SELECT Users.[Id] FROM Users""").fetchall()
# pick 5% of them for training db, pick 0.5% of them for test db
train_ids = []
test_ids = []

for u in users:
    rnd = random.random()
    if (rnd <= 0.005):
        test_ids.append(u[0])
    elif (rnd <= 0.055):
        train_ids.append(u[0])

train_matrix = data_to_matrix(db, train_ids)
test_matrix = data_to_matrix(db, test_ids)

savemat(sys.argv[2], {'train' : train_matrix,'test' : test_matrix}, oned_as = 'row')
print("Done!")