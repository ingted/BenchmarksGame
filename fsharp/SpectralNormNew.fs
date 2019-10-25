// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// Based on C# version by Isaac Gouy, The Anh Tran, Alan McGovern
// Contributed by Don Syme

module SpectralNormNew

open System
open System.Threading
open System.Runtime.Intrinsics
open System.Runtime.Intrinsics.X86

type Barrier(threads:int) =
    [<VolatileField>]
    let mutable current, iteration = threads, 0
    member _.SignalAndWait() =
        let i = iteration
        if Interlocked.Decrement(&current) = 0 then
            current <- threads
            iteration <- i + 1
        else while iteration = i do ()

let approximate u v tmp s e (barrier:Barrier) =

    let inline A i j = 1.0 / float((i + j) * (i + j + 1) / 2 + i + 1)

    let inline multiplyAv (v:_[]) (Av:_[]) =
        let inline A2 i j = Vector128.Create(A i j, A i (j+1))
        for i = s to e do
            let mutable sum1, sum2 = Vector128.Zero, Vector128.Zero
            for j = 0 to v.Length - 1 do
              sum1 <- Ssse3.Add(sum1, Ssse3.Multiply(A2 (i*2) (j*2), v.[j]))
              sum2 <- Ssse3.Add(sum2, Ssse3.Multiply(A2 (i*2+1) (j*2), v.[j]))
            Av.[i] <- Ssse3.HorizontalAdd(sum1, sum2)

    let inline multiplyAtv (v:_[]) (atv:_[]) =
        let inline A2t i j = Vector128.Create(A j i, A (j+1) i)
        for i = s to e do
            let mutable sum1, sum2 = Vector128.Zero, Vector128.Zero
            for j = 0 to v.Length - 1 do
              sum1 <- Ssse3.Add(sum1, Ssse3.Multiply(A2t (i*2) (j*2), v.[j]))
              sum2 <- Ssse3.Add(sum2, Ssse3.Multiply(A2t (i*2+1) (j*2), v.[j]))
            atv.[i] <- Ssse3.HorizontalAdd(sum1, sum2)

    let inline multiplyatAv v tmp atAv =
        multiplyAv v tmp
        barrier.SignalAndWait()
        multiplyAtv tmp atAv
        barrier.SignalAndWait()

    for _ = 0 to 9 do
        multiplyatAv u tmp v
        multiplyatAv v tmp u

    let mutable vBv, vv = Vector128.Zero, Vector128.Zero
    for i = s to e do
        vBv <- Ssse3.Add(vBv, Ssse3.Multiply(u.[i], v.[i]))
        vv <- Ssse3.Add(vv, Ssse3.Multiply(v.[i], v.[i]))
    Ssse3.HorizontalAdd(vBv, vv)

//[<EntryPoint>]
let main (args:string[]) =
    let n = try int args.[0] / 2 with _ -> 1250
    let u = Vector128.Create 1.0 |> Array.create n
    let tmp = Array.zeroCreate n
    let v = Array.zeroCreate n
    let NP = Environment.ProcessorCount
    let barrier = new Barrier(NP)
    let chunk = n / NP
    let aps = Async.Parallel [
                for i = 0 to NP-1 do
                    let s = i * chunk
                    let e = if i = NP-1 then n-1 else s+chunk-1
                    async { return approximate u v tmp s e barrier }
              ] |> Async.RunSynchronously

    let mutable ap = aps.[0]
    for i = 1 to aps.Length-1 do
        ap <- Ssse3.Add(ap, aps.[i])
    sqrt(ap.ToScalar() / ap.GetElement 1).ToString("f9")
    
    //System.Console.WriteLine("{0:f9}", RunGame n);
    //0

// Barrier - without seems better, try int
// float array and pointer?
// inline
// sum0, temp var etc