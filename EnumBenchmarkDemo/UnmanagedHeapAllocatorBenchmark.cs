namespace Benchmark
{
    using BenchmarkDotNet.Attributes;
    using System.Runtime.InteropServices;
    using TerraFX.Interop.Mimalloc;

    [MemoryDiagnoser]
    public class UnmanagedHeapAllocatorBenchmark
    {
        [Params(1000, 10000, 100000, 1000000)]
        public nuint ArraySize { get; set; }

        [GlobalSetup()]
        public void Setup()
        {
        }

        [Benchmark]
        unsafe  public void AllocateWithMimalloc()
        {
            void* s = Mimalloc.mi_malloc(ArraySize * sizeof(int));
            int* p = (int*)s;
            for(int i = 0; i < (int)ArraySize; i++)
            {
                *(p + i) = i;
            }

            Mimalloc.mi_free(s);
        }

        [Benchmark]
        unsafe public void AllocateWithAllocHGlobal()
        {
            IntPtr ptr = Marshal.AllocHGlobal((int)ArraySize * sizeof(int));

            // Write some values to the unmanaged memory
            for (int i = 0; i < (int)ArraySize; i++)
            {
                Marshal.WriteInt32(ptr, i);
            }

            Marshal.FreeHGlobal(ptr);
        }

        [Benchmark]
        public void AllocateWithManagedHeap()
        {
            var s = "123"u8;
            var array = new int[ArraySize];

            // Write some values to the managed memory
            for (int i = 0; i < (int)ArraySize; i++)
            {
                array[i] = i;
            }
        }
    }
}
