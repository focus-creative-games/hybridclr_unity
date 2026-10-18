using DotNetDetour;
using System;

namespace MonoHook
{
    public unsafe abstract class CodePatcher
    {
        public bool isValid { get; protected set; }

        protected void* _pTarget, _pReplace, _pProxy;
        protected int _jmpCodeSize;
        protected byte[] _targetHeaderBackup;

        public CodePatcher(IntPtr target, IntPtr replace, IntPtr proxy, int jmpCodeSize)
        {
            _pTarget = target.ToPointer();
            _pReplace = replace.ToPointer();
            _pProxy = proxy.ToPointer();
            _jmpCodeSize = jmpCodeSize;
        }

        public void ApplyPatch()
        {
            BackupHeader();
            EnableAddrModifiable();
            PatchTargetMethod();
            PatchProxyMethod();
            FlushICache();
        }

        public void RemovePatch()
        {
            if (_targetHeaderBackup == null)
                return;

            EnableAddrModifiable();
            RestoreHeader();
            FlushICache();
        }

        protected void BackupHeader()
        {
            if (_targetHeaderBackup != null)
                return;

            uint requireSize = LDasm.SizeofMinNumByte(_pTarget, _jmpCodeSize);
            _targetHeaderBackup = new byte[requireSize];

            fixed (void* ptr = _targetHeaderBackup)
                HookUtils.MemCpy(ptr, _pTarget, _targetHeaderBackup.Length);
        }

        protected void RestoreHeader()
        {
            if (_targetHeaderBackup == null)
                return;

            HookUtils.MemCpy_Jit(_pTarget, _targetHeaderBackup);
        }

        protected void PatchTargetMethod()
        {
            byte[] buff = GenJmpCode(_pTarget, _pReplace);
            HookUtils.MemCpy_Jit(_pTarget, buff);
        }
        protected void PatchProxyMethod()
        {
            if (_pProxy == null)
                return;

            // copy target's code to proxy
            HookUtils.MemCpy_Jit(_pProxy, _targetHeaderBackup);

            // jmp to target's new position
            long jmpFrom = (long)_pProxy + _targetHeaderBackup.Length;
            long jmpTo = (long)_pTarget + _targetHeaderBackup.Length;

            byte[] buff = GenJmpCode((void*)jmpFrom, (void*)jmpTo);
            HookUtils.MemCpy_Jit((void*)jmpFrom, buff);
        }

        protected void FlushICache()
        {
            HookUtils.FlushICache(_pTarget, _targetHeaderBackup.Length);
            HookUtils.FlushICache(_pProxy, _targetHeaderBackup.Length * 2);
        }
        protected abstract byte[] GenJmpCode(void* jmpFrom, void* jmpTo);

#if ENABLE_HOOK_DEBUG
        protected string PrintAddrs()
        {
            if (IntPtr.Size == 4)
                return $"target:0x{(uint)_pTarget:x}, replace:0x{(uint)_pReplace:x}, proxy:0x{(uint)_pProxy:x}";
            else
                return $"target:0x{(ulong)_pTarget:x}, replace:0x{(ulong)_pReplace:x}, proxy:0x{(ulong)_pProxy:x}";
        }
#endif

        private void EnableAddrModifiable()
        {
            HookUtils.SetAddrFlagsToRWX(new IntPtr(_pTarget), _targetHeaderBackup.Length);
            HookUtils.SetAddrFlagsToRWX(new IntPtr(_pProxy), _targetHeaderBackup.Length + _jmpCodeSize);
        }
    }

    public unsafe class CodePatcher_x86 : CodePatcher
    {
        protected static readonly byte[] s_jmpCode = new byte[] // 5 bytes
        {
            0xE9, 0x00, 0x00, 0x00, 0x00,                     // jmp $val   ; $val = $dst - $src - 5 
        };

        public CodePatcher_x86(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length) { }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];
            int val = (int)jmpTo - (int)jmpFrom - 5;

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr = 0xE9;
                int* pOffset = (int*)(ptr + 1);
                *pOffset = val;
            }
            return ret;
        }
    }

    /// <summary>
    /// x64下2G 内的跳转
    /// </summary>
    public unsafe class CodePatcher_x64_near : CodePatcher_x86 // x64_near pathcer code is same to x86
    {
        public CodePatcher_x64_near(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy) { }
    }

    /// <summary>
    /// x64下距离超过2G的跳转
    /// </summary>
    public unsafe class CodePatcher_x64_far : CodePatcher
    {
        protected static readonly byte[] s_jmpCode = new byte[] // 12 bytes
        {
            // 由于 rax 会被函数作为返回值修改，并且不会被做为参数使用，因此修改是安全的
            0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,         // mov rax, <jmpTo>
            0x50,                                                               // push rax
            0xC3                                                                // ret
        };

        //protected static readonly byte[] s_jmpCode2 = new byte[] // 14 bytes
        //{
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,       // <jmpTo>
        //    0xFF, 0x25, 0xF2, 0xFF, 0xFF, 0xFF                    // jmp [rip - 0xe]
        //};

        public CodePatcher_x64_far(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length) { }
        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr++ = 0x48;
                *ptr++ = 0xB8;
                *(long*)ptr = (long)jmpTo;
                ptr += 8;
                *ptr++ = 0x50;
                *ptr++ = 0xC3;
            }
            return ret;
        }
    }

    public unsafe class CodePatcher_arm32_near : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[]    // 4 bytes
        {
            0x00, 0x00, 0x00, 0xEA,                         // B $val   ; $val = (($dst - $src) / 4 - 2) & 0x1FFFFFF
        };

        public CodePatcher_arm32_near(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length)
        {
            if (Math.Abs((long)target - (long)replace) >= ((1 << 25) - 1))
                throw new ArgumentException("address offset of target and replace must less than ((1 << 25) - 1)");

#if ENABLE_HOOK_DEBUG
            Debug.Log($"CodePatcher_arm32_near: {PrintAddrs()}");
#endif
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];
            int val = ((int)jmpTo - (int)jmpFrom) / 4 - 2;

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr++ = (byte)val;
                *ptr++ = (byte)(val >> 8);
                *ptr++ = (byte)(val >> 16);
                *ptr++ = 0xEA;
            }
            return ret;
        }
    }

    public unsafe class CodePatcher_arm32_far : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[]    // 8 bytes
        {
            0x04, 0xF0, 0x1F, 0xE5,                         // LDR PC, [PC, #-4]
            0x00, 0x00, 0x00, 0x00,                         // $val
        };

        public CodePatcher_arm32_far(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length)
        {
            if (Math.Abs((long)target - (long)replace) < ((1 << 25) - 1))
                throw new ArgumentException("address offset of target and replace must larger than ((1 << 25) - 1), please use InstructionModifier_arm32_near instead");

#if ENABLE_HOOK_DEBUG
            Debug.Log($"CodePatcher_arm32_far: {PrintAddrs()}");
#endif
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];

            fixed (void* p = &ret[0])
            {
                uint* ptr = (uint*)p;
                *ptr++ = 0xE51FF004;
                *ptr = (uint)jmpTo;
            }
            return ret;
        }
    }

    /// <summary>
    /// arm64 下 ±128MB 范围内的跳转
    /// </summary>
    public unsafe class CodePatcher_arm64_near : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[]    // 4 bytes
        {
            /*
             * from 0x14 to 0x17 is B opcode
             * offset bits is 26
             * https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/B--Branch-
             */
            0x00, 0x00, 0x00, 0x14,                         //  B $val   ; $val = (($dst - $src)/4) & 7FFFFFF
        };

        /// <summary>
        /// B 指令的 imm26 是有符号数，以 4 字节为单位，可达范围是 -128MB ~ +128MB-4
        /// </summary>
        public const long MaxJmpOffset = (1L << 27) - 4;

        public CodePatcher_arm64_near(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length)
        {
            if (Math.Abs((long)target - (long)replace) > MaxJmpOffset)
                throw new ArgumentException("address offset of target and replace must not exceed ±128MB, please use CodePatcher_arm64_far instead");

            // proxy 末尾要跳回 target，距离同样受 B 指令范围限制
            if (proxy != IntPtr.Zero && Math.Abs((long)target - (long)proxy) > MaxJmpOffset)
                throw new ArgumentException("address offset of target and proxy must not exceed ±128MB, please use CodePatcher_arm64_far instead");

#if ENABLE_HOOK_DEBUG
            Debug.Log($"CodePatcher_arm64: {PrintAddrs()}");
#endif
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];
            int val = (int)((long)jmpTo - (long)jmpFrom) / 4;

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr++ = (byte)val;
                *ptr++ = (byte)(val >> 8);
                *ptr++ = (byte)(val >> 16);

                byte last = (byte)(val >> 24);
                last &= 0b11;
                last |= 0x14;

                *ptr = last;
            }
            return ret;
        }
    }

    /// <summary>
    /// arm64 下距离超过 ±128MB 的跳转
    /// 会覆盖 target 开头 4 条指令，并把它们原样搬到 proxy 执行，所以这 4 条指令里不能有 PC 相对寻址或跳转，否则拒绝 hook
    /// </summary>
    public unsafe class CodePatcher_arm64_far : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[]    // 16 bytes
        {
            /*
             * LDR(literal): https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/LDR--literal---Load-Register--literal--
             * BR: https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/BR--Branch-to-Register-
             * X16/X17(IP0/IP1) 是 AAPCS64 规定的过程间临时寄存器，函数入口处本就允许被 veneer 改写，用来中转是安全的
             */
            0x50, 0x00, 0x00, 0x58,                         // LDR X16, #8
            0x00, 0x02, 0x1F, 0xD6,                         // BR X16
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // $dst
        };

        private readonly int _scratchReg;

        public CodePatcher_arm64_far(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy, s_jmpCode.Length)
        {
            int insCount = s_jmpCode.Length / 4;
            uint* ins = (uint*)_pTarget;

            // 没有 proxy：被覆盖的指令不会再执行，只要求函数不短于 16 字节；函数入口处 X16 可以随意改写
            if (proxy == IntPtr.Zero)
            {
                for (int i = 0; i < insCount - 1; i++)
                {
                    if (IsFunctionEnd(ins[i]))
                        throw new ArgumentException($"can not hook target 0x{(long)target:x}: instruction {i} (0x{ins[i]:X8}) ends the function, it is shorter than {s_jmpCode.Length} bytes");
                }
                _scratchReg = 16;
#if ENABLE_HOOK_DEBUG
                Debug.Log($"CodePatcher_arm64_far: {PrintAddrs()}, scratch: X{_scratchReg}");
#endif
                return;
            }

            for (int i = 0; i < insCount; i++)
            {
                if (IsPcRelativeOrBranch(ins[i]))
                    throw new ArgumentException($"can not hook target 0x{(long)target:x}: instruction {i} (0x{ins[i]:X8}) is pc-relative or a branch, can not be relocated");
            }

            // proxy 执行完搬过去的指令后跳回 target 时会改写中转寄存器，X16/X17 里挑一个此时已经没用的
            if (IsDeadAfter(ins, insCount, 16))
                _scratchReg = 16;
            else if (IsDeadAfter(ins, insCount, 17))
                _scratchReg = 17;
            else
                throw new ArgumentException($"can not hook target 0x{(long)target:x}: X16 and X17 may both be live after the first {insCount} instructions " +
                    $"({ins[0]:X8} {ins[1]:X8} {ins[2]:X8} {ins[3]:X8})");

            // proxy 要写入 32 字节（搬过去的 4 条指令 + 跳回 target），前 7 条里就出现 RET 说明 proxy 太短，会写坏后面的函数
            uint* proxyIns = (uint*)_pProxy;
            for (int i = 0; i < s_jmpCode.Length * 2 / 4 - 1; i++)
            {
                if ((proxyIns[i] & 0xFFFFFC1F) == 0xD65F0000) // RET Xn
                    throw new ArgumentException($"proxy method at 0x{(long)proxy:x} is too short for arm64 far jump (need {s_jmpCode.Length * 2} bytes), please fill some dummy code inside");
            }

#if ENABLE_HOOK_DEBUG
            Debug.Log($"CodePatcher_arm64_far: {PrintAddrs()}, scratch: X{_scratchReg}");
#endif
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];

            fixed (void* p = &ret[0])
            {
                uint* ptr = (uint*)p;
                *ptr++ = 0x58000040u | (uint)_scratchReg;           // LDR Xn, #8
                *ptr++ = 0xD61F0000u | ((uint)_scratchReg << 5);    // BR Xn
                *(long*)ptr = (long)jmpTo;
            }
            return ret;
        }

        private static bool IsPcRelativeOrBranch(uint ins)
        {
            return (ins & 0x1F000000) == 0x10000000     // ADR / ADRP
                || (ins & 0x7C000000) == 0x14000000     // B / BL
                || (ins & 0xFF000000) == 0x54000000     // B.cond
                || (ins & 0x7E000000) == 0x34000000     // CBZ / CBNZ
                || (ins & 0x7E000000) == 0x36000000     // TBZ / TBNZ
                || (ins & 0x3B000000) == 0x18000000     // LDR / LDRSW / PRFM (literal)
                || (ins & 0xFE000000) == 0xD6000000;    // BR / BLR / RET 等寄存器跳转
        }

        private static bool IsFunctionEnd(uint ins)
        {
            return (ins & 0xFFFFFC1F) == 0xD65F0000     // RET
                || (ins & 0xFFFFFC1F) == 0xD61F0000     // BR
                || (ins & 0xFC000000) == 0x14000000;    // B
        }

        /// <summary>
        /// 粗略判断：凡是可能放寄存器号的字段等于 reg 就算用到，宁可多判
        /// </summary>
        private static bool MayUseReg(uint ins, int reg)
        {
            uint r = (uint)reg;
            return (ins & 0x1F) == r || ((ins >> 5) & 0x1F) == r || ((ins >> 10) & 0x1F) == r || ((ins >> 16) & 0x1F) == r;
        }

        /// <summary>
        /// 判断执行完前 count 条指令后 reg 是否已经没用：
        /// 1. 这几条指令根本没碰它：函数入口处 X16/X17 本就没有有效值（AAPCS64 允许 veneer 随意改写）；
        /// 2. 最后一次访问是 add/sub/mov 把它当源操作数读掉：mono 只把 IP0/IP1 当作单条 IR 内的临时寄存器，
        ///    如大栈帧序言 movz x16, #size; sub x17, sp, x16; mov sp, x17。
        /// 其它指令不区分读写，一律当作可能仍在用。
        /// </summary>
        private static bool IsDeadAfter(uint* ins, int count, int reg)
        {
            uint r = (uint)reg;
            for (int i = count - 1; i >= 0; i--)
            {
                uint x = ins[i];
                if (!MayUseReg(x, reg))
                    continue;

                bool addSubImm = (x & 0x1F000000) == 0x11000000;                // ADD/SUB (immediate)，含 mov sp, xN
                bool addSubLogicReg = (x & 0x1F000000) == 0x0B000000             // ADD/SUB (shifted / extended register)
                                   || (x & 0x1F000000) == 0x0A000000;            // AND/ORR/EOR (shifted register)，含 mov xd, xm
                if (!addSubImm && !addSubLogicReg)
                    return false;

                if ((x & 0x1F) == r)                                            // Rd：被改写，值可能留到后面用
                    return false;
                if (((x >> 5) & 0x1F) == r || (addSubLogicReg && ((x >> 16) & 0x1F) == r))
                    return true;                                                // Rn / Rm：作为源操作数读掉
                // 只是立即数字段恰好等于 reg，不算访问，继续往前看
            }
            return true;
        }
    }
}