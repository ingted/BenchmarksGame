/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, transliterated from Oleg Mazurov's Java program
   concurrency fix and minor improvements by Peperud
   parallel and small optimisations by Anthony Lloyd
*/

using System;
using System.Threading;
using System.Runtime.CompilerServices;

public static class FannkuchReduxImproved
{
    static int[] chkSums, maxFlips;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void rotate(int[] p, int[] pp, int l, int d)
    {
        Buffer.BlockCopy(p, 0, pp, 0, d);
        Buffer.BlockCopy(p, d, p, 0, l);
        Buffer.BlockCopy(pp, 0, p, l, d);        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void firstAndCountFlips(int n, int[] fact, int[] p, int[] pp, int[] count, int idx, ref int chksum, ref int maxFlips)
    {
        for (int i=0; i<n; ++i) p[i] = i;
        for (int i=n-1; i>0; --i)
        {
            int d = idx/fact[i];
            count[i] = d;
            if(d>0)
            {
                idx = idx%fact[i];
                rotate(p, pp, (i+1-d) * 4, d * 4);
            }
        }
        var first = p[0];
        if (first==0) return;
        if (p[first]==0)
        {
            chksum++;
            return;
        }
        Buffer.BlockCopy(p, 0, pp, 0, n * 4);
        int flips = 2;
        while(true)
        {
            for (int lo=1, hi=first-1; lo<hi; lo++,hi--)
            {
                int t = pp[lo];
                pp[lo] = pp[hi];
                pp[hi] = t;
            }
            int tp = pp[first];
            if (pp[tp]==0)
            {
                chksum += flips;
                if(flips>maxFlips) maxFlips = flips;
                return;
            }
            pp[first] = first;
            first = tp;
            flips++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void nextAndCountFlipsPlus(int n, int[] p, int[] pp, int[] count, ref int chksum, ref int maxFlips)
    {
        int first = p[1];
        p[1] = p[0];
        p[0] = first;
        int i = 1;
        while (++count[i] > i)
        {
            count[i++] = 0;
            int next = p[1];
            p[0] = next;
            for(int j=1;j<i;) p[j] = p[++j];
            p[i] = first;
            first = next;
        }
        if (first==0) return;
        if (p[first]==0)
        {
            chksum++;
            return;
        }
        Buffer.BlockCopy(p, 0, pp, 0, n * 4);
        int flips = 2;
        while(true)
        {
            for (int lo=1, hi=first-1; lo<hi; lo++,hi--)
            {
                int t = pp[lo];
                pp[lo] = pp[hi];
                pp[hi] = t;
            }
            int tp = pp[first];
            if (pp[tp]==0)
            {
                chksum += flips;
                if(flips>maxFlips) maxFlips = flips;
                return;
            }
            pp[first] = first;
            first = tp;
            flips++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void nextAndCountFlipsMinus(int n, int[] p, int[] pp, int[] count, ref int chksum, ref int maxflips)
    {
        int first = p[1];
        p[1] = p[0];
        p[0] = first;
        int i = 1;
        while (++count[i] > i)
        {
            count[i++] = 0;
            int next = p[1];
            p[0] = next;
            for(int j=1;j<i;) p[j] = p[++j];
            p[i] = first;
            first = next;
        }
        if (first==0) return;
        if (p[first]==0)
        {
            chksum--;
            return;
        }
        Buffer.BlockCopy(p, 0, pp, 0, n * 4);
        int flips = 2;
        while(true)
        {
            for (int lo=1, hi=first-1; lo<hi; lo++,hi--)
            {
                int t = pp[lo];
                pp[lo] = pp[hi];
                pp[hi] = t;
            }
            int tp = pp[first];
            if (pp[tp]==0)
            {
                chksum -= flips;
                if(flips>maxflips) maxflips = flips;
                return;
            }
            pp[first] = first;
            first = tp;
            flips++;
        }
    }

    static void run(int n, int[] fact, int taskId, int taskSize)
    {
        int[] p = new int[n], pp = new int[n], count = new int[n];
        int maxflips=1, chksum=0;
        firstAndCountFlips(n, fact, p, pp, count, taskId*taskSize, ref chksum, ref maxflips);
        nextAndCountFlipsMinus(n, p, pp, count, ref chksum, ref maxflips);
        taskSize>>=1;
        while (--taskSize>0)
        {
            nextAndCountFlipsPlus(n, p, pp, count, ref chksum, ref maxflips);
            nextAndCountFlipsMinus(n, p, pp, count, ref chksum, ref maxflips);
        }
        chkSums[taskId] = chksum;
        maxFlips[taskId] = maxflips;
    }

    public static Tuple<int,int> Test(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 7;
        var fact = new int[n+1];
        fact[0] = 1;
        var factn = 1;
        for (int i=1; i<fact.Length; i++) { fact[i] = factn *= i; }

        int nTasks = Environment.ProcessorCount;
        chkSums = new int[nTasks];
        maxFlips = new int[nTasks];
        int taskSize = factn / nTasks;
        var threads = new Thread[nTasks];
        for(int i=1; i<nTasks; i++)
        {
            int j = i;
            (threads[j] = new Thread(() => run(n, fact, j, taskSize))).Start();
        }
        run(n, fact, 0, taskSize);
        int chksum=chkSums[0], maxflips=maxFlips[0];
        for(int i=1; i<threads.Length; i++)
        {
            threads[i].Join();
            chksum += chkSums[i];
            if(maxFlips[i]>maxflips) maxflips = maxFlips[i];
        }
        return Tuple.Create(chksum, maxflips);
    }
}