/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, transliterated from Oleg Mazurov's Java program
   concurrency fix and minor improvements by Peperud
   parallel and small optimisations by Anthony Lloyd
*/

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public static class FannkuchRedux
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void rotate(int[] p, int[] pp, int l, int d)
    {
        Buffer.BlockCopy(p, 0, pp, 0, d);
        Buffer.BlockCopy(p, d, p, 0, l);
        Buffer.BlockCopy(pp, 0, p, l, d);        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void firstPermutation(int n, int[] fact, int[] p, int[] pp, int[] count, int idx)
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void nextPermutation(int[] p, int[] count)
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
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int countFlips(int n, int[] p, int[] pp)
    {
        int first = p[0];
        if (p[first]==0) return 1;
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
            if (pp[tp]==0) return flips;
            pp[first] = first;
            first = tp;
            flips++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Tuple<int,int> run(int n, int[] fact, int taskId, int taskSize)
    {
        int[] p = new int[n], pp = new int[n], count = new int[n];
        int maxflips=0, chksum=0;
        firstPermutation(n, fact, p, pp, count, taskId*taskSize);
        if(p[0] != 0)
        {
            int flips = countFlips(n, p, pp);
            chksum += flips;
            if(flips>maxflips) maxflips=flips;
        }
        while (--taskSize>0)
        {
            nextPermutation(p, count);
            if (p[0] != 0)
            {
                int flips = countFlips(n, p, pp);
                chksum += taskSize%2==0 ? flips : -flips;
                if(flips>maxflips) maxflips=flips;
            }
        }
        return Tuple.Create(chksum, maxflips);
    }

    public static Tuple<int,int> Test(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 7;
        var fact = new int[n+1];
        fact[0] = 1;
        var factn = 1;
        for (int i=1; i<fact.Length; i++) { fact[i] = factn *= i; }

        int nTasks = Environment.ProcessorCount;
        int taskSize = factn / nTasks;
        var tasks = new Task<Tuple<int,int>>[nTasks];
        for(int i=tasks.Length-1; i>=0; --i)
        {
            int j = i;
            tasks[j] = Task.Run(() => run(n, fact, j, taskSize));
        }
        int chksum=tasks[0].Result.Item1, maxFlips=tasks[0].Result.Item2;
        for(int i=1; i<tasks.Length; i++)
        {
            var result = tasks[i].Result;
            chksum += result.Item1;
            if(result.Item2>maxFlips) maxFlips=result.Item2;
        }
        return Tuple.Create(chksum, maxFlips);
    }
}