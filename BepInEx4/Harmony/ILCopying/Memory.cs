using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Harmony.ILCopying
{
    public static class Memory
    {
        private static readonly HashSet<PlatformID> WindowsPlatformIDSet = new HashSet<PlatformID>
        {
            PlatformID.Win32NT,
            PlatformID.Win32S,
            PlatformID.Win32Windows,
            PlatformID.WinCE
        };

        public static bool IsWindows => WindowsPlatformIDSet.Contains(Environment.OSVersion.Platform);

        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, Protection flNewProtect,
            out Protection lpflOldProtect);

        public static void UnprotectMemoryPage(long memory)
        {
            Protection protection;
            if (IsWindows && !VirtualProtect(new IntPtr(memory), new UIntPtr(1u), Protection.PAGE_EXECUTE_READWRITE,
                    out protection)) throw new Win32Exception();
        }

        public static long WriteJump(long memory, long destination)
        {
            UnprotectMemoryPage(memory);
            if (IntPtr.Size == 8)
            {
                memory = WriteBytes(memory, new byte[]
                {
                    72,
                    184
                });
                memory = WriteLong(memory, destination);
                memory = WriteBytes(memory, new byte[]
                {
                    byte.MaxValue,
                    224
                });
            }
            else
            {
                memory = WriteByte(memory, 104);
                memory = WriteInt(memory, (int) destination);
                memory = WriteByte(memory, 195);
            }

            return memory;
        }

        private static RuntimeMethodHandle GetRuntimeMethodHandle(MethodBase method)
        {
            if (!(method is DynamicMethod)) return method.MethodHandle;
            var bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
            var method2 = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", bindingAttr);
            if (method2 != null) return (RuntimeMethodHandle) method2.Invoke(method, new object[0]);
            var field = typeof(DynamicMethod).GetField("m_method", bindingAttr);
            if (field != null) return (RuntimeMethodHandle) field.GetValue(method);
            return (RuntimeMethodHandle) typeof(DynamicMethod).GetField("mhandle", bindingAttr).GetValue(method);
        }

        public static long GetMethodStart(MethodBase method)
        {
            var runtimeMethodHandle = GetRuntimeMethodHandle(method);
            RuntimeHelpers.PrepareMethod(runtimeMethodHandle);
            return runtimeMethodHandle.GetFunctionPointer().ToInt64();
        }

        public static unsafe long WriteByte(long memory, byte value)
        {
            byte* ptr = (byte*) memory;
            *ptr = value;
            return memory + 1L;
        }

        public static long WriteBytes(long memory, byte[] values)
        {
            foreach (var value in values) memory = WriteByte(memory, value);
            return memory;
        }

        public static unsafe long WriteInt(long memory, int value)
        {
            int* ptr = (int*) memory;
            *ptr = value;
            return memory + 4L;
        }

        public static unsafe long WriteLong(long memory, long value)
        {
            long* ptr = (long*) memory;
            *ptr = value;
            return memory + 8L;
        }
    }
}