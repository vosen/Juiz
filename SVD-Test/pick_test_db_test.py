import unittest
import numpy
from scipy.sparse import lil_matrix
from pick_test_db import PickTrainingMatrices

class TestDBPicker(unittest.TestCase):

    def test_trim_cols(self):
        m1 = lil_matrix(numpy.array([[1,2],[3,4]]))
        m2 = lil_matrix(numpy.array([[1,0],[0,0]]))
        m1_reduced, m2_reduced = PickTrainingMatrices.trim_cols(m1,m2)
        self.assertEqual(m1_reduced.shape, m2_reduced.shape)
        self.assertEqual(m2_reduced.shape, (2,1))
        self.assertEqual(m1_reduced[0,0], 1)
        self.assertEqual(m1_reduced[1,0], 3)
        self.assertEqual(m2_reduced[0,0], 1)
        self.assertEqual(m2_reduced[1,0], 0)

    def test_trim_rows(self):
        m1 = lil_matrix(numpy.array([[1,2],[3,4]]))
        m2 = lil_matrix(numpy.array([[1,0],[0,0]]))
        m1_reduced, m2_reduced = PickTrainingMatrices.trim_rows(m1,m2)
        self.assertEqual(m1_reduced.shape, m2_reduced.shape)
        self.assertEqual(m2_reduced.shape, (1,2))
        self.assertEqual(m1_reduced[0,0], 1)
        self.assertEqual(m1_reduced[0,1], 2)
        self.assertEqual(m2_reduced[0,0], 1)
        self.assertEqual(m2_reduced[0,1], 0)

    def test_trim_full(self):
        m1 = lil_matrix(numpy.array([[1,2],[3,4]]))
        m2 = lil_matrix(numpy.array([[1,0],[0,0]]))
        m1_reduced, m2_reduced = PickTrainingMatrices().trim_matrices(m1,m2)
        self.assertEqual(m1_reduced.shape, m2_reduced.shape)
        self.assertEqual(m2_reduced.shape, (1,1))
        self.assertEqual(m1_reduced[0,0], 1)
        self.assertEqual(m2_reduced[0,0], 1)

if __name__ == '__main__':
    unittest.main()