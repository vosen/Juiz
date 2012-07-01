import sys, numpy
import psycopg2 as pg
from collections import defaultdict
from scipy import sparse
from sparsesvd import sparsesvd
from scipy.io import savemat

def parse_args():
    if(len(sys.argv) != 6):
        print("USAGE: model_generator.py db_string title_low_limit user_low_limit features out_file")
        print('model_generator.py "host=127.0.0.1 dbname=mal-utf8 user=vosen" 50 5 14 result')
        quit()
    return (sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), sys.argv[5])

# returns coo matrix with all elements
def load_scores(db):
    scores = defaultdict(dict)
    curr = db.cursor()
    curr.execute('SELECT "Anime_Id", "User_Id", "Score" FROM "Seen"')
    raw_scores = curr.fetchall()
    curr.close()
    rows, cols, vals = zip(*raw_scores)
    return sparse.csc_matrix((vals, (rows,cols)), dtype = numpy.double)

# filter out rows and cols that are too insignificant
# abuse transpose because it's supercheap
# this function is kinda buggy because it doesn't take into account
# users removed in the first phase, but it should be good enough for now
def filter_too_small(csc_mat, row_lower_limit, col_lower_limit):
    (mat_col_filtered, useless_trash) = filter_too_small_cols(csc_mat, col_lower_limit)
    (mat_row_filtered, old_ids) = filter_too_small_cols(mat_col_filtered.transpose().tocsc(), row_lower_limit)
    return (mat_row_filtered.transpose(), old_ids)

def filter_too_small_cols(csc_mat, col_limit):
    col_diag = sparse.spdiags([1] * csc_mat.shape[1], 0, csc_mat.shape[1], csc_mat.shape[1]).tocsc()
    empty_cols = set()
    for i in range(0, len(csc_mat.indptr)-1):
        if (csc_mat.indptr[i+1] - csc_mat.indptr[i]) < col_limit:
            empty_cols.add(i)
    full_cols = set(range(0, csc_mat.shape[1])) - empty_cols
    full_len = len(full_cols)
    empty_cols = sorted(empty_cols)
    full_cols = reversed(sorted(full_cols)[-len(empty_cols):])
    old_ids = {}
    for (empty_idx, full_idx) in zip(empty_cols, full_cols):
        if empty_idx > full_idx:
            continue
        col_diag.indices[full_idx] = empty_idx
        col_diag.indices[empty_idx] = full_idx
        old_ids[empty_idx] = full_idx
    return ((csc_mat * col_diag)[:,:full_len], old_ids)

def join_old_id_dicts(dict1, dict2):
    result = {}
    for (k,v) in dict2.items():
        if k in dict1:
            del dict1[k]
        if v in dict1:
            result[k] = dict1[v]
            del dict1[v]
        else:
            result[k] = v
    result.update(dict1)
    return result

# mat is in csr format
def row_nnz_average(mat, row_idx):
    start = mat.indptr[row_idx]
    stop = mat.indptr[row_idx + 1]
    return numpy.mean(mat.data[start:stop])

def build_title_mapping(ids_dict, title_count):
    doc_to_title = [0] * title_count
    for i in range(0, title_count):
        if i in ids_dict:
            doc_to_title[i] = ids_dict[i]
        else:
            doc_to_title[i] = i
    title_to_doc = [0] * (max(doc_to_title) + 1)
    for idx, val in enumerate(doc_to_title):
        title_to_doc[val] = idx
    return (title_to_doc, doc_to_title)

def generate_model(in_path, title_limit, user_limit, features, out_path):
    # connect to db
    db = pg.connect(in_path)
    # load scores
    scores = load_scores(db)
    db.close()
    print "Loaded scores"
    # filter insignificant titles/users, second filtering to remove empty cols/rows
    (mat, old_ids_1) = filter_too_small(scores, title_limit, user_limit)
    (mat, old_ids_2) = filter_too_small(mat.tocsc(), 1, 1)
    print "Filtered insignificant titles and users"
    # matrix is in csr format, calc row nnz averages and convert to csc
    averages = map(lambda x: row_nnz_average(mat,x), range(0, mat.shape[0]))
    mat = mat.tocsc()
    # build compact titleid translation tables
    old_ids = join_old_id_dicts(old_ids_1, old_ids_2)
    (title_to_document, document_to_tile) = build_title_mapping(old_ids, mat.shape[0])
    # run svd
    print "Built additional data"
    (ut, s, vt) = sparsesvd(mat.tocsc(), features)
    print "Factorization finished"
    s_sqrt = numpy.diag(numpy.sqrt(s))
    s_inv = numpy.diag(numpy.power(s,-1))
    terms = ut.transpose().dot(s_sqrt)
    documents = s_sqrt.dot(s_inv).dot(ut)
    # dump results
    savemat(out_path, {"Terms": terms, "Documents": documents, "Averages": averages, "TitleMapping": title_to_document, "DocumentMapping" : document_to_tile}, oned_as='row')
    print "Saved generated results"

# build dense scores (eg: {10 : {2,3}, 231 : {4,5}} to {1 : {2,3}, 2 : {4,5}} )
# and generate translation tables
def scores_to_dense(sparse_scores):
    # sorted(...) looks only at keys
    doc_title_map = sorted(sparse_scores)
    title_doc_map = [0] * doc_title_map[-1]
    for idx, val in enumerate(doc_title_map):
        title_doc_map[val] = idx
    dense_scores = dict()
    for title_id, title_scores in sparse_scores.items():
        dense_scores[title_doc_map[title_id]] = title_scores
    return (dense_scores, title_doc_map, doc_title_map)

if __name__ == '__main__':
    (in_path, title_limit, user_limit, features, out_path) = parse_args()
    generate_model(in_path, title_limit, user_limit, features, out_path)