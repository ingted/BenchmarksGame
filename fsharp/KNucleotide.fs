// The Computer Language Benchmarks Game
// http://benchmarksgame.alioth.debian.org/

open System
open System.Collections.Generic
open System.Threading.Tasks

[<Literal>]
let BLOCK_SIZE = 8388608 // 1024 * 1024 * 8

[<EntryPoint>]
let main _ =
  let threeStart,threeBlocks,threeEnd =
    use input = IO.File.OpenRead(@"C:\Users\Ant\Google Drive\BenchmarkGame\fasta25000000.txt")
    //let input = Console.OpenStandardInput()
    let mutable threeEnd = 0
    let read buffer =
        let rec read offset count =
            let bytesRead = input.Read(buffer, offset, count)
            if bytesRead=count then offset+count
            elif bytesRead=0 then offset
            else read (offset+bytesRead) (count-bytesRead)
        threeEnd <- read 0 BLOCK_SIZE

    let rec findHeader matchIndex buffer =
        let toFind = ">THREE"B
        let find i matchIndex =
            let rec find i matchIndex =
                if matchIndex=0 then
                    let i = Array.IndexOf(buffer, toFind.[0], i)
                    if -1=i then -1,0
                    else find (i+1) 1
                else
                    let fl = toFind.Length
                    let rec tryMatch i matchIndex =
                        if i>=BLOCK_SIZE || matchIndex>=fl then i,matchIndex
                        else
                            if buffer.[i]=toFind.[matchIndex] then
                                tryMatch (i+1) (matchIndex+1)
                            else
                                find i 0
                    let i,matchIndex = tryMatch i matchIndex
                    if matchIndex=fl then i,matchIndex else -1,matchIndex
            find i matchIndex
        read buffer
        let i,matchIndex = find 0 matchIndex
        if -1<>i then i,buffer
        else findHeader matchIndex buffer

    let rec findSequence i buffer =
        let i = Array.IndexOf(buffer, '\n'B, i)
        if i <> -1 then buffer,i+1
        else
            read buffer
            findSequence 0 buffer
    let buffer,threeStart =
        Array.zeroCreate BLOCK_SIZE |> findHeader 0 ||> findSequence

    let threeBlocks =
        if threeEnd<>BLOCK_SIZE then // Needs to be at least 2 blocks
            for i = threeEnd to BLOCK_SIZE-1 do
                buffer.[i] <- 255uy
            threeEnd <- 0
            [[||];buffer]
        else
            let rec findEnd i buffer threeBlocks =
                let i = Array.IndexOf(buffer, '>'B, i)
                if i <> -1 then
                    threeEnd <- i
                    buffer::threeBlocks
                else
                    let threeBlocks = buffer::threeBlocks
                    let buffer = Array.zeroCreate BLOCK_SIZE
                    read buffer
                    if threeEnd<>BLOCK_SIZE then buffer::threeBlocks
                    else findEnd 0 buffer threeBlocks
            let threeBlocks = findEnd threeStart buffer []
            if threeStart+18>BLOCK_SIZE then // Key needs to be in first block
                let block0 = threeBlocks.[0]
                let block1 = threeBlocks.[1]
                Buffer.BlockCopy(block0, threeStart, block0, threeStart-18,
                    BLOCK_SIZE-threeStart)
                Buffer.BlockCopy(block1, 0, block0, BLOCK_SIZE-18, 18)
                for i = 0 to 17 do block1.[i] <- 255uy
            threeBlocks

    threeStart, List.rev threeBlocks, threeEnd

  let toChar = [|'A'; 'C'; 'G'; 'T'|]
  let toNum = Array.zeroCreate<byte> 256
  toNum.[int 'c'B] <- 1uy; toNum.[int 'C'B] <- 1uy
  toNum.[int 'g'B] <- 2uy; toNum.[int 'G'B] <- 2uy
  toNum.[int 't'B] <- 3uy; toNum.[int 'T'B] <- 3uy
  toNum.[int '\n'B] <- 255uy; toNum.[int '>'B] <- 255uy; toNum.[255] <- 255uy

  Parallel.ForEach(threeBlocks, fun bs ->
    for i = 0 to Array.length bs-1 do
        bs.[i] <- toNum.[int bs.[i]]
  ) |> ignore

  let count l mask (summary:_->string) : Task<string> =
    Task.Run (fun () ->
      let mutable rollingKey = 0
      let firstBlock = threeBlocks.[0]
      let rec startKey l start =
          if l>0 then
             rollingKey <- (rollingKey<<<2) ||| int firstBlock.[start]
             startKey (l-1) (start+1)
      startKey l threeStart
      let dict = Dictionary<_,_>()
      let inline check (nb:byte) =
          if nb<>255uy then
              rollingKey <- ((rollingKey &&& mask) <<< 2) ||| int nb
              match dict.TryGetValue rollingKey with
              | true, v -> incr v
              | false, _ -> dict.[rollingKey] <- ref 1

      for i = threeStart+l to firstBlock.Length-1 do
          check firstBlock.[i]
      
      for bl = 1 to List.length threeBlocks-2 do
          Array.iter check threeBlocks.[bl]

      let lastBlock = threeBlocks.[List.length threeBlocks-1]
      for i = 0 to threeEnd-1 do
          check lastBlock.[i]
      summary dict
    )

  let writeFrequencies fragmentLength (freq:Dictionary<_,_>) =
    let percent = 100.0 / (Seq.sumBy (!) freq.Values |> float)
    freq |> Seq.sortByDescending (fun kv -> kv.Value)
    |> Seq.collect (fun kv ->
        let keyChars = Array.zeroCreate fragmentLength
        let mutable key = kv.Key
        for i in keyChars.Length-1..-1..0 do
            keyChars.[i] <- toChar.[int key &&& 0x3]
            key <- key >>> 2
        [String(keyChars);" ";(float !kv.Value * percent).ToString("F3");"\n"]
      )
    |> String.Concat

  let writeCount (fragment:string) (dict:Dictionary<_,_>) =
    let mutable key = 0
    for i = 0 to fragment.Length-1 do
        key <- (key <<< 2) ||| int toNum.[int fragment.[i]]
    let n =
        match dict.TryGetValue key with
        | true, v -> !v
        | false, _ -> 0
    String.Concat(n.ToString(), "\t", fragment)

  let count64 l mask (summary:_->string) : Task<string> =
    Task.Run (fun () ->
      let mutable rollingKey = 0L
      let firstBlock = threeBlocks.[0]
      let rec startKey l start =
          if l>0 then
             rollingKey <- (rollingKey<<<2) ||| int64 firstBlock.[start]
             startKey (l-1) (start+1)
      startKey l threeStart
      let dict = Dictionary<_,_>()
      let inline check (nb:byte) =
          if nb<>255uy then
              rollingKey <- ((rollingKey &&& mask) <<< 2) ||| int64 nb
              match dict.TryGetValue rollingKey with
              | true, v -> incr v
              | false, _ -> dict.[rollingKey] <- ref 1

      for i = threeStart+l to firstBlock.Length-1 do
          check firstBlock.[i]
      
      for bl = 1 to List.length threeBlocks-2 do
          Array.iter check threeBlocks.[bl]

      let lastBlock = threeBlocks.[List.length threeBlocks-1]
      for i = 0 to threeEnd-1 do
          check lastBlock.[i]
      summary dict
    )

  let writeCount64 (fragment:string) (dict:Dictionary<_,_>) =
    let mutable key = 0L
    for i = 0 to fragment.Length-1 do
        key <- (key <<< 2) ||| int64 toNum.[int fragment.[i]]
    let n =
        match dict.TryGetValue key with
        | true, v -> !v
        | false, _ -> 0
    String.Concat(n.ToString(), "\t", fragment)

  let task18 = count64 18 34359738367L (writeCount64 "GGTATTTTAATTTATAGT")
  let task12 = count 12 8388607 (writeCount "GGTATTTTAATT")
  let task6 = count 6 0b1111111111 (writeCount "GGTATT")
  let task1 = count 1 0 (writeFrequencies 1)
  let task2 = count 2 0b11 (writeFrequencies 2)
  let task3 = count 3 0b1111 (writeCount "GGT")
  let task4 = count 4 0b111111 (writeCount "GGTA")
  
  task1.Result |> stdout.WriteLine
  task2.Result |> stdout.WriteLine
  task3.Result |> stdout.WriteLine
  task4.Result |> stdout.WriteLine
  task6.Result |> stdout.WriteLine
  task12.Result |> stdout.WriteLine
  task18.Result |> stdout.WriteLine

  0