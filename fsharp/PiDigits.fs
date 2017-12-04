﻿(**
 * The Computer Language Benchmarks Game
 * http://benchmarksgame.alioth.debian.org/
 *
 * Port to F# by Jomo Fisher of the C# port that uses native GMP:
 * 	contributed by Mike Pall
 * 	java port by Stefan Krause
 *  C# port by Miguel de Icaza
*)

open System
open System.Runtime.InteropServices

[<Struct; StructLayout (LayoutKind.Sequential)>]
type MPZ =
   val _mp_alloc:int
   val _mp_size:int
   val ptr:IntPtr

[<DllImport ("gmp", EntryPoint="__gmpz_init",CallingConvention=CallingConvention.Cdecl,ExactSpelling=true,SetLastError=false)>]
extern void mpzInit(MPZ& _value)

[<DllImport ("gmp", EntryPoint="__gmpz_mul_si",CallingConvention=CallingConvention.Cdecl,ExactSpelling=true,SetLastError=false)>]
extern void mpzMul(MPZ& _dest, MPZ&_src, int _value)

[<DllImport ("gmp", EntryPoint="__gmpz_add",CallingConvention=CallingConvention.Cdecl,ExactSpelling=true,SetLastError=false)>]
extern void mpzAdd(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport ("gmp", EntryPoint="__gmpz_tdiv_q",CallingConvention=CallingConvention.Cdecl,ExactSpelling=true,SetLastError=false)>]
extern void mpzTdiv(MPZ& _dest, MPZ& _src, MPZ& _src2)

[<DllImport ("gmp", EntryPoint="__gmpz_set_si",CallingConvention=CallingConvention.Cdecl,ExactSpelling=true,SetLastError=false)>]
extern void mpzSet(MPZ& _src, int _value)

[<DllImport ("gmp", EntryPoint="__gmpz_get_si",CallingConvention=CallingConvention.Cdecl,ExactSpelling=true,SetLastError=false)>] 
extern int mpzGet(MPZ& _src)

[<EntryPoint>]
let main args =

    let init() = 
        let mutable result = MPZ()
        mpzInit(&result)
        result

    let mutable q,r,s,t,u,v,w = init(),init(),init(),init(),init(),init(),init()

    let mutable i = 0
    let mutable c = 0
    let ch = Array.zeroCreate 10
    let n = int args.[0]
    
    let inline composeR(bq, br, bs, bt) = 
        mpzMul(&u, &r, bs)
        mpzMul(&r, &r, bq)
        mpzMul(&v, &t, br)
        mpzAdd(&r, &r, &v)
        mpzMul(&t, &t, bt)
        mpzAdd(&t, &t, &u)
        mpzMul(&s, &s, bt)
        mpzMul(&u, &q, bs)
        mpzAdd(&s, &s, &u)
        mpzMul(&q, &q, bq)

    // Compose matrix with numbers on the left.
    let inline composeL(bq, br, bs, bt) =
        mpzMul(&r, &r, bt)
        mpzMul(&u, &q, br)
        mpzAdd(&r, &r, &u)
        mpzMul(&u, &t, bs)
        mpzMul(&t, &t, bt)
        mpzMul(&v, &s, br)
        mpzAdd(&t, &t, &v)
        mpzMul(&s, &s, bq)
        mpzAdd(&s, &s, &u)
        mpzMul(&q, &q, bq)

    // Extract one digit.
    let inline extract(j) = 
        mpzMul(&u, &q, j)
        mpzAdd(&u, &u, &r)
        mpzMul(&v, &s, j)
        mpzAdd(&v, &v, &t)
        mpzTdiv(&w, &u, &v)
        mpzGet(&w)


    // Print one digit. Returns 1 for the last digit. 
    let inline prdigit(y:int) = 
        ch.[c] <- char(48+y)
        c <- c + 1
        i <- i + 1
        if (i%10=0 || i = n) then
            while c<>ch.Length do
                ch.[c] <- ' '
                c<-c+1
            c <- 0
            Console.Write(ch)
            Console.Write("\t:")
            Console.WriteLine(i)
        i = n

    // Generate successive digits of PI.
    let mutable k = 1
    i <- 0
    mpzSet(&q, 1)
    mpzSet(&r, 0)
    mpzSet(&s, 0)
    mpzSet(&t, 1)
    let mutable more = true
    while more do
        let y = extract 3
        if y = extract 4 then
            if prdigit y then more<-false
            else composeR(10, -10*y, 0, 1)
        else
            composeL(k, 4*k+2, 0, 2*k+1);
            k<-k+1

    0