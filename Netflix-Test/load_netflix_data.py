import sys, os, csv
import psycopg2 as pg

def init_db(db):
    cur = db.cursor()
    cur.execute("""
    DROP TABLE  IF EXISTS "Seen";
    CREATE TABLE "Seen" (
      "Anime_Id" INTEGER NOT NULL,
      "User_Id" INTEGER NOT NULL,
      "Score" SMALLINT NOT NULL
    );""")
    cur.close()
    db.commit()

def load_file(idx, path, db):
    cur = db.cursor()
    reader = csv.reader(open(path), delimiter=',')
    reader.next()
    for row in reader:
        cur.execute("""INSERT INTO "Seen" ("Anime_Id", "User_Id", "Score") VALUES (%s, %s, %s)""", (idx, row[0], row[1]))
    cur.close()
    db.commit()

def loda_data(train_path, db_path):
    db = pg.connect(db_path)
    init_db(db)
    i = 1
    for file in os.listdir(train_path):
        load_file(i, os.path.join(train_path, file), db)
        i += 1
    db.close()


if __name__ == '__main__':
    if(len(sys.argv) != 3):
        print("USAGE: rmse.py train_data_path db_path")
        print('EXAMPLE: rmse.py "C:\dev\netflix\download\training_set" "host=127.0.0.1 dbname=netflix user=vosen"')
        quit()
    loda_data(sys.argv[1], sys.argv[2])