// ugly workaround for vs2010
#I @"bin\Debug\"

#r "Dapper.dll"
#r "Npgsql.dll"
#r "Mono.Security.dll"
#r "System.Transactions.dll"

open Dapper
open Npgsql
open System

let AddIndex(connString) =
    use conn = new NpgsqlConnection(connString)
    conn.Notice.Add(fun evArgs -> evArgs.Notice |> Console.WriteLine)
    conn.Open()
    use dbTrans = conn.BeginTransaction()
    // add required extension
    conn.Execute("CREATE EXTENSION IF NOT EXISTS pg_trgm;", transaction=dbTrans) |> ignore
    // build fts indexes on title tables
    conn.Execute("CREATE INDEX romaji_fts_index ON \"Anime\" USING gin(to_tsvector('simple', \"RomajiName\"));", transaction=dbTrans) |> ignore
    conn.Execute("CREATE INDEX english_fts_index ON \"Anime\" USING gin(to_tsvector('simple', \"EnglishName\"));", transaction=dbTrans) |> ignore
    conn.Execute("CREATE INDEX synonym_fts_index ON \"Anime_Synonyms\" USING gin(to_tsvector('simple', \"Text\"));", transaction=dbTrans) |> ignore
    // build and index trigram table
    conn.Execute(@"CREATE TABLE title_words AS SELECT word FROM ts_stat(
                    'SELECT to_tsvector(''simple'', ""EnglishName"") FROM ""Anime""
                     UNION SELECT to_tsvector(''simple'', ""RomajiName"") FROM ""Anime""
                     UNION SELECT to_tsvector(''simple'', ""Text"") FROM ""Anime_Synonyms"";');", transaction=dbTrans) |> ignore
    conn.Execute("CREATE INDEX words_trgm_index ON title_words USING gin(word gin_trgm_ops)", transaction=dbTrans) |> ignore
    // add required functions
    conn.Execute(@"CREATE FUNCTION tokenize_default(text) RETURNS SETOF text AS $$
                    SELECT (ts_lexize('simple', token))[1] FROM ts_parse('default', $1) WHERE tokid IN (1,2,3,4,6,9,10,11,15,16,17,19,20,21,22);
                    $$ LANGUAGE SQL IMMUTABLE;", transaction=dbTrans) |> ignore
    conn.Execute(@"CREATE FUNCTION correct_title_token(text) RETURNS text AS $$
                    SELECT word FROM title_words 
                    WHERE word % $1
                    ORDER by similarity($1, word) DESC LIMIT 1;
                    $$ LANGUAGE SQL STABLE;", transaction=dbTrans) |> ignore
    conn.Execute(@"CREATE FUNCTION plainto_corrected_tsquery(text) RETURNS tsquery AS $$
                    SELECT (SELECT string_agg(correct_title_token(token), '&') as corrected FROM tokenize_default($1) as token)::tsquery;
                    $$ LANGUAGE SQL STABLE;", transaction=dbTrans) |> ignore
    conn.Execute(@"CREATE FUNCTION find_title(text, integer) RETURNS TABLE(id int, title text) AS $$
                    WITH precalc AS (SELECT plainto_corrected_tsquery($1) as value)
                    SELECT id, COALESCE(""EnglishName"", ""RomajiName"") AS title
                    FROM    ""Anime"",
                            (SELECT id FROM (
                                SELECT ""Anime_Id"" AS id, ts_rank(to_tsvector('simple', ""Text""), (SELECT value FROM precalc)) AS rank 
                                    FROM ""Anime_Synonyms"", ""Anime""
                                    WHERE to_tsvector('simple', ""Text"") @@ (SELECT value FROM precalc) AND ""Anime_Id"" = ""Id""
                                UNION SELECT ""Id"" AS id, ts_rank(to_tsvector('simple', ""RomajiName""), (SELECT value FROM precalc)) AS rank 
                                    FROM ""Anime""
                                    WHERE to_tsvector('simple', ""RomajiName"") @@ (SELECT value FROM precalc)
                                UNION SELECT ""Id"" AS id, ts_rank(to_tsvector('simple', ""EnglishName""), (SELECT value FROM precalc)) AS rank 
                                    FROM ""Anime""
                                    WHERE to_tsvector('simple', ""EnglishName"") @@ (SELECT value FROM precalc)) AS sub
                                GROUP BY id
                                ORDER BY max(rank) DESC, id ASC
                                LIMIT $2) as sub
                    WHERE id = ""Anime"".""Id"";
                    $$ LANGUAGE sql STABLE;", transaction=dbTrans) |> ignore
    // we dsiable seq scan in this function because postgresql planner is kinda broken in this regard

    //conn.Execute("SELECT (ts_lexize(token))[1] FROM ts_parse('default', 'query') WHERE tokid IN (1,2,3,4,6,9,10,11,15,16,17,19,20,21,22);", transaction=dbTrans) |> ignore
    dbTrans.Commit()

let Main(args : string[])=
    if args.Length <> 2 then
        printfn "USAGE: %s [CONNECTION_STRING]" args.[0]
    else
        AddIndex args.[1]

fsi.CommandLineArgs |> Main