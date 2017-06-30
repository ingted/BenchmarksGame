/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   Contributed by Peperud
   Modified to reduce memory use by Anthony Lloyd
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

class RevCompSequence { public List<byte[]> Pages; public int StartHeader, EndExclusive; }

public static class revcomp
{
    const int READER_BUFFER_SIZE = 1024 * 128;
    static BlockingCollection<byte[]> readQue = new BlockingCollection<byte[]>();
    static BlockingCollection<RevCompSequence> groupQue = new BlockingCollection<RevCompSequence>();
    static BlockingCollection<RevCompSequence> writeQue = new BlockingCollection<RevCompSequence>();
    static ConcurrentBag<byte[]> bytePool = new ConcurrentBag<byte[]>(); 

    static byte[] borrowBuffer()
    {
        byte[] ret;
        return bytePool.TryTake(out ret) ? ret : new byte[READER_BUFFER_SIZE];
    }

    static void returnBuffer(byte[] bytes)
    {
        if(bytes.Length==READER_BUFFER_SIZE) bytePool.Add(bytes);
    }

    static void Reader()
    {
        using (var stream = File.OpenRead(@"C:\temp\input25000000.txt"))//Console.OpenStandardInput())
        {
            for (;;)
            {
                var buffer = borrowBuffer();
                var bytesRead = stream.Read(buffer, 0, READER_BUFFER_SIZE);
                if (bytesRead == 0) break;
                if(bytesRead != READER_BUFFER_SIZE) Array.Resize(ref buffer, bytesRead);
                readQue.Add(buffer);
            }
            readQue.CompleteAdding();
        }
    }

    static bool tryTake<T>(BlockingCollection<T> q, out T t) where T : class
    {
        t = null;
        while(!q.IsCompleted && !q.TryTake(out t)) Thread.SpinWait(0);
        return t!=null;
    }

    static void Grouper()
    {
        const byte GT = (byte)'>';
        var startHeader = 0;
        var i = 1;
        var data = new List<byte[]>();
        byte[] bytes;
        while (tryTake(readQue, out bytes))
        {
            data.Add(bytes);
            for (;i<bytes.Length; i++)
            {
                var b = bytes[i];
                if (b == GT)
                {
                    groupQue.Add(new RevCompSequence { Pages = data, StartHeader = startHeader, EndExclusive = i });
                    startHeader = i;
                    data = new List<byte[]> { bytes };
                }
            }
            i = 0;
        }
        groupQue.Add(new RevCompSequence { Pages = data, StartHeader = startHeader, EndExclusive = data[data.Count-1].Length });
        groupQue.CompleteAdding();
    }

    static void Reverser()
    {
        const byte LF = 10;

        // Set up complements map
        var map = new byte[256];
        for (byte i=0; i<255; i++) map[i]=i;
        map[(byte)'A'] = (byte)'T';
        map[(byte)'B'] = (byte)'V';
        map[(byte)'C'] = (byte)'G';
        map[(byte)'D'] = (byte)'H';
        map[(byte)'G'] = (byte)'C';
        map[(byte)'H'] = (byte)'D';
        map[(byte)'K'] = (byte)'M';
        map[(byte)'M'] = (byte)'K';
        map[(byte)'R'] = (byte)'Y';
        map[(byte)'T'] = (byte)'A';
        map[(byte)'V'] = (byte)'B';
        map[(byte)'Y'] = (byte)'R';
        map[(byte)'a'] = (byte)'T';
        map[(byte)'b'] = (byte)'V';
        map[(byte)'c'] = (byte)'G';
        map[(byte)'d'] = (byte)'H';
        map[(byte)'g'] = (byte)'C';
        map[(byte)'h'] = (byte)'D';
        map[(byte)'k'] = (byte)'M';
        map[(byte)'m'] = (byte)'K';
        map[(byte)'r'] = (byte)'Y';
        map[(byte)'t'] = (byte)'A';
        map[(byte)'v'] = (byte)'B';
        map[(byte)'y'] = (byte)'R';

        RevCompSequence sequence;
        while (tryTake(groupQue, out sequence))
        {
            var startPageId = 0;
            var startBytes = sequence.Pages[0];
            var startIndex = sequence.StartHeader;

            // Skip header line
            do
            {
                if (++startIndex == startBytes.Length)
                {
                    startBytes = sequence.Pages[++startPageId];
                    startIndex = 0;
                }
            } while (startBytes[startIndex] != LF);

            var endPageId = sequence.Pages.Count - 1;
            var endIndex = sequence.EndExclusive - 1;
            if(endIndex==-1) endIndex = sequence.Pages[--endPageId].Length-1;
            var endBytes = sequence.Pages[endPageId];
            
            // Swap in place across pages
            do
            {
                var startByte = startBytes[startIndex];
                if(startByte==LF)
                {
                    if (++startIndex == startBytes.Length)
                    {
                        startBytes = sequence.Pages[++startPageId];
                        startIndex = 0;
                    }
                    if (startIndex == endIndex && startPageId == endPageId) break;
                    startByte = startBytes[startIndex];
                }
                var endByte = endBytes[endIndex];
                if(endByte==LF)
                {
                    if (--endIndex == -1)
                    {
                        endBytes = sequence.Pages[--endPageId];
                        endIndex = endBytes.Length - 1;
                    }
                    if (startIndex == endIndex && startPageId == endPageId) break;
                    endByte = endBytes[endIndex];
                }

                startBytes[startIndex] = map[endByte];
                endBytes[endIndex] = map[startByte];

                if (++startIndex == startBytes.Length)
                {
                    startBytes = sequence.Pages[++startPageId];
                    startIndex = 0;
                }
                if (--endIndex == -1)
                {
                    endBytes = sequence.Pages[--endPageId];
                    endIndex = endBytes.Length - 1;
                }
            } while (startPageId < endPageId || (startPageId == endPageId && startIndex < endIndex));
            if (startIndex == endIndex) startBytes[startIndex] = map[startBytes[startIndex]];
            writeQue.Add(sequence);
        }
        writeQue.CompleteAdding();
    }

    static void Writer()
    {
        using (var stream = Stream.Null)//Console.OpenStandardOutput())
        {
            RevCompSequence sequence;
            while (tryTake(writeQue, out sequence))
            {
                var startIndex = sequence.StartHeader;
                var pages = sequence.Pages;

                for (int i = 0; i < pages.Count - 1; i++)
                {
                    var bytes = pages[i];
                    stream.Write(bytes, startIndex, bytes.Length - startIndex);
                    if(!readQue.IsCompleted) returnBuffer(bytes);
                    startIndex = 0;
                }
                stream.Write(pages[pages.Count-1], startIndex, sequence.EndExclusive - startIndex);
            }
        }
    }

    public static void Main(string[] args)
    {
        new Thread(Reader).Start();
        new Thread(Grouper).Start();
        new Thread(Reverser).Start();
        Writer();
    }
}