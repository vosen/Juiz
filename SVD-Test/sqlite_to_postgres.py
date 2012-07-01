import sqlite3, sys
import psycopg2 as pg

def parse_args():
    if(len(sys.argv) != 4):
        print("USAGE: sqlite_to_postgres.py sqlite_db postgres_db size")
        quit()
    return (sys.argv[1], sys.argv[2], int(sys.argv[3]))
    
def move_data(lite_path, pg_path, size):
    sqlite_db = sqlite3.connect(lite_path)
    users = sqlite_db.execute('SELECT "Id", "Name", "Result" FROM "Users" LIMIT %s' % size).fetchall()
    user_ids = zip(*users)[0]
    seen = sqlite_db.execute('SELECT "Anime_Id", "User_Id", "Score" FROM "Seen" WHERE "User_Id" in %s' % str(user_ids)).fetchall()
    sqlite_db.close
    pg_db = pg.connect(pg_path)
    pg_curr = pg_db.cursor()
    pg_curr.executemany('INSERT INTO "Users"("Id", "Name", "Result") VALUES (%s, %s, %s::boolean)', users)
    pg_curr.executemany('INSERT INTO "Seen"("Anime_Id", "User_Id", "Score") VALUES (%s, %s, %s)', seen)
    pg_db.commit()
    #pg_curr.execute('INSERT INTO "Seen"("Anime_Id", "User_Id", "Score") VALUES (%s, %s, %s)', data[0])
    
if __name__ == '__main__':
    (lite_db, pg_db, size) = parse_args()
    move_data(lite_db, pg_db, size)