namespace ThreadPoolPriorityBenchmark
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Helper class around accessing Win32 thread API's.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class ThreadHelper
    {
        [ThreadStatic]
        private static SafeHandle CachedThreadHandle;

        /// <summary>
        /// Returns the current thread handle that can be shared with other API's. 
        /// Return value must NOT be closed, as it is cached for the lifetime of the process.
        /// </summary>
        /// <returns>Thread handle of current running thread, or unitialized if failed.</returns>
        /// <remarks>This function caches the thread handle in a <c>ThreadStatic</c> field for performance reasons.</remarks>
        internal static IntPtr GetShareableHandleForCurrentThread()
        {
            IntPtr ptr = CachedThreadHandle?.DangerousGetHandle() ?? IntPtr.Zero;
            if (ptr == IntPtr.Zero)
            {
                IntPtr hRealHandle;
                IntPtr hPseudoHandle = NativeMethods.GetCurrentThread();  // pseudo handle does not need to be manually closed.  
                IntPtr hCurrentProcessHandle = NativeMethods.GetCurrentProcess();
                // Disabling this warning because this whole class only works on Windows. If we go to Linux, then we'll have to abstract it all.
#pragma warning disable CA1416 // This call site is reachable on all platforms. 'SafeAccessTokenHandle' is only supported on: 'windows'.
                ptr = NativeMethods.DuplicateHandle(hCurrentProcessHandle,
                                       hPseudoHandle,
                                       hCurrentProcessHandle,
                                       out hRealHandle,
                                       0,
                                       true,
                                       NativeMethods.DUPLICATE_SAME_ACCESS) ? hRealHandle : IntPtr.Zero;
                CachedThreadHandle = new SafeAccessTokenHandle(ptr);
#pragma warning restore CA1416 // This call site is reachable on all platforms. 'SafeAccessTokenHandle' is only supported on: 'windows'.
            }
#if DEBUG
            else
            {
                IntPtr hRealHandle;
                IntPtr hPseudoHandle = NativeMethods.GetCurrentThread();
                IntPtr hCurrentProcessHandle = NativeMethods.GetCurrentProcess();
                NativeMethods.DuplicateHandle(hCurrentProcessHandle,
                                       hPseudoHandle,
                                       hCurrentProcessHandle,
                                       out hRealHandle,
                                       0,
                                       true,
                                       NativeMethods.DUPLICATE_SAME_ACCESS);

                if (!NativeMethods.CompareObjectHandles(CachedThreadHandle.DangerousGetHandle(), hRealHandle))
                {
                    // Let's validate that the cached handle is always pointing to the right thread!
                    //LogAssert.Assert(false, "ThreadHelper.GetShareableHandleForCurrentThread returned a thread handle that didn't match the cached thread handle!");
                }
            }
#endif
            return ptr;
        }
    }

    /// <summary>
    /// Static wrapper class around Win32 Thread/Timing calls
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        /// <summary>
        /// Values obtained from http://msdn.microsoft.com/en-us/library/windows/desktop/ms724251(v=vs.85).aspx
        /// </summary>
        internal static uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        internal static uint DUPLICATE_SAME_ACCESS = 0x00000002;

        /// <summary>
        /// Values obtained from http://msdn.microsoft.com/en-us/library/windows/desktop/ms686277(v=vs.85).aspx
        /// </summary>
        internal static int THREAD_MODE_BACKGROUND_BEGIN = 0x00010000;
        internal static int THREAD_MODE_BACKGROUND_END = 0x00020000;
        internal static int THREAD_PRIORITY_IDLE = -15;
        internal static int THREAD_PRIORITY_LOWEST = -2;
        internal static int THREAD_PRIORITY_NORMAL = 0;

        /// <summary>
        /// Values taken from http://msdn.microsoft.com/en-us/library/windows/desktop/ms682489(v=vs.85).aspx
        /// </summary>
        internal const uint TH32CS_INHERIT = 0x80000000;
        internal const uint TH32CS_SNAPHEAPLIST = 0x00000001;
        internal const uint TH32CS_SNAPMODULE = 0x00000008;
        internal const uint TH32CS_SNAPMODULE32 = 0x00000010;
        internal const uint TH32CS_SNAPPROCESS = 0x00000002;
        internal const uint TH32CS_SNAPTHREAD = 0x00000004;
        internal const uint TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST | TH32CS_SNAPMODULE | TH32CS_SNAPPROCESS | TH32CS_SNAPTHREAD;

        internal const int MAX_PATH = 260;

        /// <summary>
        /// wrapper around Win32 GetSystemTimeAdjustment for obtaining system time adjustments
        /// </summary>
        /// <param name="lpTimeAdjustment">A pointer to a variable that the function sets to the number of lpTimeIncrement100-nanosecond units added to the time-of-day clock for 
        /// every period of time which actually passes as counted by the system. This value only has meaning if lpTimeAdjustmentDisabled is FALSE.</param>
        /// <param name="lpTimeIncrement">A pointer to a variable that the function sets to the interval in 100-nanosecond units at which 
        /// the system will add lpTimeAdjustment to the time-of-day clock. This value only has meaning if lpTimeAdjustmentDisabled is FALSE.</param>
        /// <param name="lpTimeAdjustmentDisabled">A pointer to a variable that the function sets to indicate whether periodic time 
        /// adjustment is in effect.</param>
        /// <returns>bool for success</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetSystemTimeAdjustment(out uint lpTimeAdjustment,
                                                            out uint lpTimeIncrement,
                                                            [MarshalAs(UnmanagedType.Bool)] out bool lpTimeAdjustmentDisabled);

        /// <summary>
        /// Wrapper around Win32 GetThreadTimes.  Retrieves timing information for the specified thread.
        /// </summary>
        /// <param name="handle">Handle of thread to measure</param>
        /// <param name="creation">A pointer to a FILETIME structure that receives the creation time of the thread.</param>
        /// <param name="exit">A pointer to a FILETIME structure that receives the exit time of the thread. If the thread has not exited, the content of this structure is undefined.</param>
        /// <param name="kernel">A pointer to a FILETIME structure that receives the amount of time that the thread has executed in kernel mode.</param>
        /// <param name="user">A pointer to a FILETIME structure that receives the amount of time that the thread has executed in user mode.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetThreadTimes(IntPtr handle,
                                                   out System.Runtime.InteropServices.ComTypes.FILETIME creation,
                                                   out System.Runtime.InteropServices.ComTypes.FILETIME exit,
                                                   out System.Runtime.InteropServices.ComTypes.FILETIME kernel,
                                                   out System.Runtime.InteropServices.ComTypes.FILETIME user);


        [DllImport("Kernel32", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        public static extern Int32 GetCurrentWin32ThreadId();

        /// <summary>
        /// Wrapper around Win32 GetCurrentThread.  Retrieves a pseudo handle for the calling thread.
        /// </summary>
        /// <returns>The return value is a pseudo handle for the current thread.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern IntPtr GetCurrentThread();

        /// <summary>
        /// Wrapper around Win32 DuplicateHandle.  This is used to return with GetCurrentThread to return the true handle
        /// of a currently running thread.  This handle must be manually closed when it is no longer needed.  
        /// </summary>
        /// <param name="hSourceProcessHandle">A handle to the process with the handle to be duplicated.</param>
        /// <param name="hSourceHandle">The handle to be duplicated. This is an open object handle that is valid in the context of the 
        /// source process. For a list of objects whose handles can be duplicated, see the following Remarks section.</param>
        /// <param name="hTargetProcessHandle">A handle to the process that is to receive the duplicated handle. The handle must have 
        /// the PROCESS_DUP_HANDLE access right.</param>
        /// <param name="lpTargetHandle">A pointer to a variable that receives the duplicate handle. This handle value is valid in the 
        /// context of the target process.</param>
        /// <param name="dwDesiredAccess">The access requested for the new handle. For the flags that can be specified for each object 
        /// type, see the following Remarks section.</param>
        /// <param name="bInheritHandle">A variable that indicates whether the handle is inheritable. If TRUE, the duplicate handle can 
        /// be inherited by new processes created by the target process. If FALSE, the new handle cannot be inherited.</param>
        /// <param name="dwOptions">Optional actions. DUPLICATE_CLOSE_SOURCE (0x01), DUPLICATE_SAME_ACCESS (0x02)</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
                                                  IntPtr hSourceHandle,
                                                  IntPtr hTargetProcessHandle,
                                                  out IntPtr lpTargetHandle,
                                                  uint dwDesiredAccess,
                                                  [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
                                                  uint dwOptions);
        /// <summary>
        /// Compare object handles
        /// </summary>
        /// <param name="hHandleA">Fist handle</param>
        /// <param name="hHandleB">Second handle</param>
        /// <returns>True if both handles refer to the same object</returns>
        [DllImport("kernelbase.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CompareObjectHandles(IntPtr hHandleA, IntPtr hHandleB);

        /// <summary>
        /// Wrapper around Win32 CloseHandle. Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Wrapper around Win32 GetCurrentProcess.  Returns a Process component associated with the currently active process
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Returns the current running process ID
        /// </summary>
        /// <returns>process identifier</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetCurrentProcessId();

        /// <summary>
        /// Wrapper around Win32 GetProcessTimes to retrieve timing information for the specified process.
        /// </summary>
        /// <param name="handle">Current process handle</param>
        /// <param name="creation">output for creation time</param>
        /// <param name="exit">output for exit time</param>
        /// <param name="kernel">output for kernel time</param>
        /// <param name="user">output for user time</param>
        /// <returns>success/fail</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessTimes(IntPtr handle,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME creation,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME exit,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME kernel,
                                                    out System.Runtime.InteropServices.ComTypes.FILETIME user);


        /// <summary>
        /// Quick FILETIME to Tick time converter
        /// </summary>
        /// <returns>100's nanoseconds</returns>
        internal static UInt64 FileTimeToTicks(System.Runtime.InteropServices.ComTypes.FILETIME time)
        {
            return ((UInt64)time.dwLowDateTime + ((UInt64)(time.dwHighDateTime) << 32));
        }

        /// <summary>
        /// Wrapper around Win32 GetSystemTimes.  Retrieves system timing information. 
        /// On a multiprocessor system, the values returned are the sum of the designated times across all processors.
        /// </summary>
        /// <param name="lpIdleTime">A pointer to a FILETIME structure that receives the amount of time that the system has been idle.</param>
        /// <param name="lpKernelTime">A pointer to a FILETIME structure that receives the amount of time that the system has spent 
        /// executing in Kernel mode (including all threads in all processes, on all processors). This time value also includes the 
        /// amount of time the system has been idle.</param>
        /// <param name="lpUserTime">A pointer to a FILETIME structure that receives the amount of time that the system has spent 
        /// executing in User mode (including all threads in all processes, on all processors).</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
                                                   out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
                                                   out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);



        /// <summary>
        /// Retrieve thread priority for a given thread handle
        /// </summary>
        /// <param name="hThread">Thread handle</param>
        /// <returns>Thread priority</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int GetThreadPriority(IntPtr hThread);

        /// <summary>
        /// Wrapper around Win32 SetThreadPriority.  Sets the priority value for the specified thread. This value, together with the priority 
        /// class of the thread's process, determines the thread's base priority level.
        /// </summary>
        /// <param name="hThread">A handle to the thread whose priority value is to be set.</param>
        /// <param name="priority">The priority value for the thread.  See static constants in this class.</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetThreadPriority(IntPtr hThread, int priority);

        /// <summary>
        /// Wrapper Around Win32 GetProcessMemoryInfo. 
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="counters">Counter structure containing all values</param>
        /// <param name="size">Size of the structure</param>
        /// <returns>True on success else false</returns>
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS_EX counters, uint size);

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_MEMORY_COUNTERS_EX
        {
            public uint cb;
            public uint PageFaultCount;
            public IntPtr PeakWorkingSetSize;
            public IntPtr WorkingSetSize;
            public IntPtr QuotaPeakPagedPoolUsage;
            public IntPtr QuotaPagedPoolUsage;
            public IntPtr QuotaPeakNonPagedPoolUsage;
            public IntPtr QuotaNonPagedPoolUsage;
            public IntPtr PagefileUsage;
            public IntPtr PeakPagefileUsage;
            public IntPtr PrivateUsage;
        }

        /// <summary>
        /// Returns working set size of the process.
        /// </summary>
        /// <returns>Always expect > 0 value except -1 in failure case</returns>
        internal static long GetWorkingSetBytesOfCurrentProcess()
        {
            var pmc = new PROCESS_MEMORY_COUNTERS_EX();
            pmc.cb = (uint)Marshal.SizeOf(pmc);
            if (!GetProcessMemoryInfo(GetCurrentProcess(), out pmc, pmc.cb))
            {
                return -1;
            }
            return pmc.WorkingSetSize.ToInt64();
        }

        /// <summary>
        /// Returns private bytes size of the process.
        /// </summary>
        /// <returns>Always expect > 0 value except -1 in failure case</returns>
        internal static long GetPrivateBytesOfCurrentProcess()
        {
            var pmc = new PROCESS_MEMORY_COUNTERS_EX();
            pmc.cb = (uint)Marshal.SizeOf(pmc);
            if (!GetProcessMemoryInfo(GetCurrentProcess(), out pmc, pmc.cb))
            {
                return -1;
            }
            return pmc.PrivateUsage.ToInt64();
        }


        /// <summary>
        /// Wrapper around Win32 API CreateToolhelp32Snapshot. Takes a snapshot of the specified processes, as well as the heaps, modules, and threads used by these processes.
        /// </summary>
        /// <param name="dwFlags">The portions of the system to be included in the snapshot</param>
        /// <param name="th32ProcessID">The process identifier of the process to be included in the snapshot</param>
        /// <returns>Handle to the specified SnapShot</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        /// <summary>
        /// Signature to represent a PROCESSENTRY32 structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szExeFile;
        };

        /// <summary>
        /// Wrapper around win32 Process32First API call
        /// </summary>
        /// <param name="hSnapshot">snapshot pointer generated from CreateToolhelp32Snapshot</param>
        /// <param name="lppe">reference to PROCESSENTRY32 struct</param>
        /// <returns>success/fail</returns>
        [DllImport("kernel32.dll")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2205", Justification = "We don't want to use managed alternatives due to large memory object allocation")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /// <summary>
        /// Wrapper around win32 Process32Next API call
        /// </summary>
        /// <param name="hSnapshot">snapshot pointer generated from CreateToolhelp32Snapshot</param>
        /// <param name="lppe">reference to PROCESSENTRY32 struct</param>
        /// <returns>success/fail</returns>
        [DllImport("kernel32.dll")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2205", Justification = "We don't want to use managed alternatives due to large memory object allocation")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /// <summary>
        /// Returns the thread count for the current running process
        /// </summary>
        /// <returns>number of threads, or -1 if error occurred</returns>
        internal static long GetProcessThreadCount()
        {
            // first determine our process id.
            var id = GetCurrentProcessId();

            // Get a process list snapshot (0 indicates current process)
            var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPALL, 0);

            var entry = new PROCESSENTRY32();
            entry.dwSize = (uint)Marshal.SizeOf(entry);
            var ret = Process32First(snapshot, ref entry);
            while (ret && entry.th32ProcessID != id)
            {
                ret = Process32Next(snapshot, ref entry);
            }
            CloseHandle(snapshot);
            if (!ret)
            {
                return -1;
            }

            return entry.cntThreads;
        }

        #region MiniDump
        // Source: http://pinvoke.net/default.aspx/dbghelp/MiniDumpWriteDump.html
        // Modified MiniDumpWriteDump declaration to not take exception information, just a null pointer.

        private const int MiniDumpWithFullMemory = 2;

        [DllImport("Dbghelp.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, IntPtr hFile, int DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

        /// <summary>
        /// Writes a full minidump to the minidumps directory of the supplied data directory.
        /// </summary>
        /// <param name="dataDirectory">Machine's AP data directory</param>
        /// <remarks>
        /// A full memory dump of the AH can take a VERY long time depending on the process size.
        ///
        /// There is an alternate dumping method in AH: <see cref="ProcessDumper.AnalyzeRawProcessSnapshot"/>.
        /// It uses a different underlying native API, and it allows you to interact with the dump in-memory.
        /// In the future, we should consider merging these two methods.
        /// </remarks>
        internal static void WriteFullDump(string dataDirectory)
        {

            string directory = Path.Combine(dataDirectory, "minidumps");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filename = Path.Combine(directory, $"ApplicationHost_FullDump_{DateTime.Now:yyyyMMdd_HHmmss}.dmp");

            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                // ReSharper disable once PossibleNullReferenceException
                MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), fileStream.SafeFileHandle.DangerousGetHandle(), MiniDumpWithFullMemory, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
        }

        #endregion

    }
}

