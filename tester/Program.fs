﻿open System
open Expecto

/// Expects function `f1` is faster than `f2`. Measurer used to measure only a
/// subset of the functions. Statistical test to 99.99% confidence level.
let isFasterThanSub (f1:Performance.Measurer<_,_>->'a) (f2:Performance.Measurer<_,_>->'a) format =
  let toString (s:SampleStatistics) =
    sprintf "%.4f \u00B1 %.4f ms" s.mean s.meanStandardError

  match Performance.timeCompare f1 f2 with
  | Performance.ResultNotTheSame (r1, r2)->
    printfn "%s. Expected function results to be the same (%A vs %A)." format r1 r2
  | Performance.MetricTooShort (s,p) ->
    printfn "%s. Expected metric (%s) to be much longer than the machine resolution (%s)." format (toString s) (toString p)
  | Performance.MetricEqual (s1,s2) ->
    printfn "%s. Expected f1 (%s) to be faster than f2 (%s) but are equal." format (toString s1) (toString s2)
  | Performance.MetricMoreThan (s1,s2) ->
    printfn "%s. Expected f1 (%s) to be faster than f2 (%s) but is ~%.0f%% slower." format (toString s1) (toString s2) ((s1.mean/s2.mean-1.0)*100.0)
  | Performance.MetricLessThan (s1,s2) ->
    printfn "%s. f1 (%s) is %s faster than f2 (%s)." format (toString s1) (sprintf "~%.1f%%" ((1.0-s1.mean/s2.mean)*100.0)) (toString s2)

/// Expects function `f1` is faster than `f2`. Statistical test to 99.99%
/// confidence level.
let isFasterThan (f1:unit->'a) (f2:unit->'a) message =
  isFasterThanSub (fun measurer -> measurer f1 ())
                  (fun measurer -> measurer f2 ())
                  message


[<EntryPoint>]
let main argv =
    //Improved.MandelBrot.Main([|"16000"|])
    //isFasterThan (fun () -> Improved.MandelBrot.Test [|"16000"|]) (fun () -> MandelBrot.Test [|"16000"|]) "Improved C# Mandelbrot faster than original"
    
    //NBody.Main([|"50000000"|])
    //Improved.NBody.Main([|"50000000"|])
    isFasterThan (fun () -> Improved.NBody.Test [|"50000000"|]) (fun () -> NBody.Test [|"50000000"|]) "Improved C# NBody faster than original"

    


    // let start = System.Diagnostics.Stopwatch.GetTimestamp();
    // FannkuchRedux.Test([|"12"|]) |> printfn "%A"
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp();
    // Improved.FannkuchRedux.Test([|"12"|]) |> printfn "%A"
    // let end2 = System.Diagnostics.Stopwatch.GetTimestamp();
    // Console.WriteLine(end1-start);
    // Console.WriteLine(end2-end1);

    // let start = System.Diagnostics.Stopwatch.GetTimestamp();
    // FannkuchRedux.Test([|"12"|]) |> printfn "%A"
    // let end1 = System.Diagnostics.Stopwatch.GetTimestamp();
    // Improved.FannkuchRedux.Test([|"12"|]) |> printfn "%A"
    // let end2 = System.Diagnostics.Stopwatch.GetTimestamp();
    // Console.WriteLine(end1-start);
    // Console.WriteLine(end2-end1);

    //isFasterThan (fun () -> Improved.FannkuchRedux.Test [|"12"|]) (fun () -> FannkuchRedux.Test [|"12"|]) "Improved C# FannkuchRedux faster than original"
    //printfn "%A" (Improved.FannkuchRedux.Test([|"12"|]));
    
    //isFasterThan (fun () -> FSharpImprovedNBody.test 5000000) (fun () -> FSharpOriginalNBody.test 5000000) "NBody F# Improved faster then F# Original"
    //isFasterThan (fun () -> CSharpParallel.NBody.Test 5000000) (fun () -> CSharpOriginal.NBody.Test 5000000) "NBody C# Parallel faster then C# Original"
    
    0 // return an integer exit code

