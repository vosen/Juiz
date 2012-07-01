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
    conn.Execute("DROP INDEX IF EXISTS romaji_fts_index;", transaction=dbTrans) |> ignore
    conn.Execute("DROP INDEX IF EXISTS english_fts_index;", transaction=dbTrans) |> ignore
    conn.Execute("DROP INDEX IF EXISTS synonym_fts_index;", transaction=dbTrans) |> ignore
    conn.Execute("DROP TABLE IF EXISTS title_words;", transaction=dbTrans) |> ignore
    conn.Execute("DROP INDEX IF EXISTS words_trgm_index;", transaction=dbTrans) |> ignore
    conn.Execute("DROP FUNCTION IF EXISTS tokenize_default(text);", transaction=dbTrans) |> ignore
    conn.Execute("DROP FUNCTION IF EXISTS correct_title_token(text);", transaction=dbTrans) |> ignore
    conn.Execute("DROP FUNCTION IF EXISTS plainto_corrected_tsquery(text);", transaction=dbTrans) |> ignore
    conn.Execute("DROP FUNCTION IF EXISTS find_title(text, integer);", transaction=dbTrans) |> ignore
    dbTrans.Commit()

let Main(args : string[])=
    if args.Length <> 2 then
        printfn "USAGE: %s [CONNECTION_STRING]" args.[0]
    else
        AddIndex args.[1]

fsi.CommandLineArgs |> Main