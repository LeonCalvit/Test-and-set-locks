using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

/*
Author: Leon Calvit 
Date: 11/3/2019
*/

/*
 * Implement the TAS and TTAS locks and compare them for a different number of threads, up to 100 threads.
 * The critical section is a counter that increments by 1. Each thread repeatedly executes the critical section(by reacquiring the lock). 
 * The program terminates when the counter value reaches 1 million.
*/
namespace _4585_HW_7
{
    class Program
    {
        private volatile int number;
        public ICustom_lock Lock;
        static void Main()
        {
            // Maximum number of threads to test locks on
            int max_threads = 100;
            Console.WriteLine("Starting test");
            var myself = new Program();
            myself.Lock = new TAS_lock();
            var TAS_times = new List<int>();
            Console.WriteLine("Testing TAS lock");
            for (int i = 5; i <= max_threads; i += 5)
            {
                TAS_times.Add(myself.Test_locks(i));
            }
            myself.Lock = new TTAS_lock();
            var TTAS_times = new List<int>();
            Console.WriteLine("Testing TTAS lock");
            for (int i = 5; i <= max_threads; i += 5)
            {
                TTAS_times.Add(myself.Test_locks(i));
            }
            Console.WriteLine("Test completed");
            //Displaying results
            Console.WriteLine("\nTAS times:");
            int count = 0;
            int sum = 0;
            foreach (var time in TAS_times)
            {
                sum += time;
                count++;
                Console.WriteLine($"Time for {count*5} threads: {time}ms");
            }
            Console.WriteLine($"Average time: {sum / count}ms");

            Console.WriteLine("\nTTAS times:");
            count = 0;
            sum = 0;
            foreach (var time in TTAS_times)
            {
                sum += time;
                count++;
                Console.WriteLine($"Time for {count * 5} threads: {time}ms");
            }
            // Average time isn't the best metric, but it's easier to compare than a list of the
            // durations themselves.
            // A graph would be a better display.
            Console.WriteLine($"Average time: {sum / count}ms");

            Console.ReadLine(); // Pause on completion
        }
        

        public int Test_locks(int num_threads)
        {
            var threads = new  List<Thread>();
            number = 0;
            for(int i  = 0; i < num_threads; i++)
            {
                threads.Add(new Thread(Increment));
            }
            // For timing the duration of the tests
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var t in threads)
            {
                t.Start();
            }
            foreach(var t in threads)
            {
                t.Join();
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            return ts.Milliseconds;
        }
        public void Increment()
        {
            while (number < 1000000)
            {
                Lock.Lock();
                try
                {
                    if (number >= 1000000)
                    {
                        Lock.Unlock();
                        break;
                    }
                    number++;
                }
                finally
                {
                    Lock.Unlock();
                }
            }
            return;
        }
    }
    interface ICustom_lock
    {
        void Lock();
        void Unlock();
    }
    // Test and set lock.  Uses a single memory location for N threads, but doesn't perform well under high contention
    class TAS_lock : ICustom_lock
    {
        // Using int because CompareExchange can't accept bools.
        private volatile static Int32 state = 0;
        public void Lock()
        {
            while (Interlocked.CompareExchange(ref state, 1, 0) == 0);
        }

        public void Unlock()
        {
            state = 0;
        }
    }
    // Test and test and set lock.  Spins on a local variable to reduce bus load.  
    // Much better under high contention than TAS, but still not great
    class TTAS_lock :  ICustom_lock
    {
        private volatile static Int32 state = 0;
        public void Lock()
        {
            while (true)
            {
                while (state != 0);
                if(Interlocked.CompareExchange(ref state, 1, 0) == 0)
                {
                    return;
                }
            }
        }
        public void Unlock()
        {
            state = 0;
        }
    }
}
