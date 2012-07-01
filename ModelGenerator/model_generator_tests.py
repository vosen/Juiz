import unittest, numpy
from model_generator import filter_too_small, join_old_id_dicts, build_title_mapping
from scipy import sparse

class ModelGeneratorTests(unittest.TestCase):

    def test_title_mappings(self):
        id_maps = { 2 : 6, 4 : 7 }
        (title_to_doc, doc_to_title) = build_title_mapping(id_maps, 6)
        self.assertListEqual([0,1,6,3,7,5], doc_to_title)
        self.assertListEqual([0,1,0,3,0,5,2,4], title_to_doc)

    def test_join_dicts1(self):
        d1 = { 2 : 6, 4 : 7 }
        d2 = { 2 : 4, 3 : 5 }
        joined = join_old_id_dicts(d1, d2)
        self.assertDictEqual({ 2 : 7, 3 : 5 }, joined)

    def test_join_dicts2(self):
        d1 = { 1 : 6, 4 : 7 }
        d2 = { 1 : 4, 3 : 5 }
        joined = join_old_id_dicts(d1, d2)
        self.assertDictEqual({ 1 : 7, 3 : 5 }, joined)

    def test_trim_rows_smaller(self):
        mat = sparse.csc_matrix(numpy.array([[1,0,0],[0,0,1], [0,2,3]]))
        (mat_reduced, old_ids) = filter_too_small(mat, 2, 0)
        #self.assertIsInstance(mat_reduced, sparse.csc_matrix)
        self.assertEqual((1,3), mat_reduced.shape)
        self.assertEqual(0, mat_reduced[0,0])
        self.assertEqual(2, mat_reduced[0,1])
        self.assertEqual(3, mat_reduced[0,2])
        self.assertDictEqual({ 0 : 2 }, old_ids)

    def test_trim_rows_larger(self):
        mat = sparse.csc_matrix(numpy.array([[1,2,3],[0,0,1], [0,2,3]]))
        (mat_reduced, old_ids) = filter_too_small(mat, 2, 0)
        #self.assertIsInstance(mat_reduced, sparse.csc_matrix)
        self.assertEqual((2,3), mat_reduced.shape)
        self.assertEqual(1, mat_reduced[0,0])
        self.assertEqual(2, mat_reduced[0,1])
        self.assertEqual(3, mat_reduced[0,2])
        self.assertEqual(0, mat_reduced[1,0])
        self.assertEqual(2, mat_reduced[1,1])
        self.assertEqual(3, mat_reduced[1,2])
        self.assertDictEqual({ 1 : 2 }, old_ids)

    def test_trim_cols_larger(self):
        mat = sparse.csc_matrix(numpy.array([[1,2,0],[0,5,0]]))
        (mat_reduced, old_ids) = filter_too_small(mat, 0, 2)
        #self.assertIsInstance(mat_reduced, sparse.csc_matrix)
        self.assertEqual((2,1), mat_reduced.shape)
        self.assertEqual(2, mat_reduced[0,0])
        self.assertEqual(5, mat_reduced[1,0])
        self.assertEqual(0, len(old_ids))

    def test_trim_cols_smaller(self):
        mat = sparse.csc_matrix(numpy.array([[1,0,0],[0,0,1]]))
        (mat_reduced, old_ids) = filter_too_small(mat, 0, 1)
        #self.assertIsInstance(mat_reduced, sparse.csc_matrix)
        self.assertEqual((2,2), mat_reduced.shape)
        self.assertEqual(1, mat_reduced[0,0])
        self.assertEqual(0, mat_reduced[0,1])
        self.assertEqual(0, mat_reduced[1,0])
        self.assertEqual(1, mat_reduced[1,1])
        self.assertEqual(0, len(old_ids))

    def test_trim_combined(self):
        mat = sparse.csc_matrix(numpy.array([[0,1,0],[1,0,0],[0,0,0]]))
        (mat_reduced, old_ids) = filter_too_small(mat, 1, 1)
        self.assertEqual((2,2), mat_reduced.shape)

if __name__ == '__main__':
    unittest.main()