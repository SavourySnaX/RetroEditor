#include <cassert>

#include "JITBuffer.h"

/*
    Forms a trampoline that looks approximately like this (stack space and slots based on largest version for example)

    sub     rsp, 64                                     ; AllocateStackSpace

    mov     rax, DWORD PTR [rsp+slot5+64]
    mov     DWORD PTR [rsp+slot6], rax                  ; CopySlotToNext

    mov     rax, DWORD PTR [rsp+slot4+64]
    mov     DWORD PTR [rsp+slot5], rax                  ; CopySlotToNext
    
    mov     QWORD PTR [rsp+slot4], r9d                  ; CopySlotToNext

    mov     r9d, r8d                                    ; CopySlotToNext

    mov     r8d, rdx                                    ; CopySlotToNext

    mov     rdx, rcx                                    ; CopySlotToNext

    mov     rcx, #imm64AddressOfThisStorage
    mov     rcx, [rcx]                                  ; SetThis

    mov     rax, #imm64AddressOfCWrapperForMemberCall
    call    rax                                         ; CallMemberFunc

    add     rsp, 64                                     ; FreeStackSpace

    ret                                                 ; Return
*/

class NativeTrampolineWindowsX64Implementation
{
public:
    static void AllocateStackSpace(int8_t slots, JITBuffer& jitBuffer)
    {
        // For windows 64bit, first four params are in registers, rest are on stack, but
        //shadow stack space is allocated for the first four params always
        //in addition, stack alignment must be mainted

        const uint8_t slotsNeeded = slots<4 ? 4 : slots;
        const uint8_t withWindowsABI = (slotsNeeded&1) ? slotsNeeded : slotsNeeded+1;   // Ensure after call stack is aligned
        const uint8_t AllocStackSpace[] = {0x48, 0x83, 0xEC, (uint8_t)(withWindowsABI*8)};

        assert(withWindowsABI<32 && "Unsupported number of stack slots for Stack Allocation");

        jitBuffer.CopyBlock(&AllocStackSpace, sizeof(AllocStackSpace));
    }

    static void FreeStackSpace(int8_t slots, JITBuffer& jitBuffer)
    {
        // For windows 64bit, first four params are in registers, rest are on stack, but
        //shadow stack space is allocated for the first four params always
        //in addition, stack alignment must be mainted

        const uint8_t slotsNeeded = slots<4 ? 4 : slots;
        const uint8_t withWindowsABI = (slotsNeeded&1) ? slotsNeeded : slotsNeeded+1;   // Ensure after call stack is aligned
        const uint8_t FreeStackSpace[] = {0x48, 0x83, 0xC4, uint8_t(withWindowsABI*8)};

        assert(withWindowsABI<32 && "Unsupported number of stack slots for Stack Free");

        jitBuffer.CopyBlock(&FreeStackSpace, sizeof(FreeStackSpace));
    }

    static void CopySlotToNextSlot(const int stackSlot, const int numParams, JITBuffer& jitBuffer)
    {
        // Complicated by the fact not all stack slots are equal - ie first 4 are registers, rest are on stack
        //so we are either performing a reg <- reg, stack <- reg, or a stack<-stack (via a temporary register)
        switch (stackSlot)
        {
            case 0:
                {
                    const uint8_t slot0[] = {0x48, 0x89, 0xCA};  // mov rdx,rcx
                    jitBuffer.CopyBlock(&slot0, sizeof(slot0));
                }
                break;
            case 1:
                {
                    const uint8_t slot1[] = {0x49, 0x89, 0xD0};  // mov r8,rdx
                    jitBuffer.CopyBlock(&slot1, sizeof(slot1));
                }
                break;
            case 2:
                {
                    const uint8_t slot2[] = {0x4D, 0x89, 0xC1};  // mov r9,r8
                    jitBuffer.CopyBlock(&slot2, sizeof(slot2));
                }
                break;
            case 3:
                {
                    const uint8_t toNewSlot[] = {0x4C, 0x89, 0x4C, 0x24, uint8_t((stackSlot+1)*8)};          // mov QWORD PTR 4*8[rsp], r9
                    jitBuffer.CopyBlock(&toNewSlot, sizeof(toNewSlot));
                }
                break;
            case 4:
            case 5:
                // These all need to move to the stack slots
                {
                    const uint8_t newStackSpace=8+numParams*8+((~numParams)&1)*8;
                    const uint8_t fromOrigSlot[] = {0x48, 0x8B, 0x44, 0x24, uint8_t(newStackSpace+stackSlot*8)};      // mov rax, QWORD PTR newStackSpace+stackSlot*8[rsp]
                    const uint8_t toNewSlot[] = {0x48, 0x89, 0x44, 0x24, uint8_t((stackSlot+1)*8)};                   // mov QWORD PTR (stackSlot+1)*8[rsp], rax
                    jitBuffer.CopyBlock(&fromOrigSlot, sizeof(fromOrigSlot));
                    jitBuffer.CopyBlock(&toNewSlot, sizeof(toNewSlot));
                }
                break;
            default:
                assert(false && "Out of Range stack slot!");
                break;
        }
    }

    static void SetThis(void* ptr, JITBuffer& jitBuffer)
    {
        const intptr_t p=(intptr_t)ptr;
        const uint8_t SetThisPtr[] = {0x48, 0xB9, 
                                         uint8_t(p>>0), uint8_t(p>>8), uint8_t(p>>16), uint8_t(p>>24), uint8_t(p>>32), uint8_t(p>>40), uint8_t(p>>48), uint8_t(p>>56),  // mov rcx,imm64
                                         0x48, 0x8b, 0x09};                                                                                                             // mov rcx,[rcx]

        jitBuffer.CopyBlock(&SetThisPtr, sizeof(SetThisPtr));
    }

    static void Return(JITBuffer& jitBuffer)
    {
        const uint8_t Ret[] = {0xC3};        // ret

        jitBuffer.CopyBlock(&Ret, sizeof(Ret));
    }

    static void CallMemberFunc(void* staticFuncWithMemberPointer, JITBuffer& jitBuffer)
    {
        const intptr_t p=(intptr_t)staticFuncWithMemberPointer;
        const uint8_t CallMemberFunc[] = {0x48, 0xB8, 
                                             uint8_t(p>>0), uint8_t(p>>8), uint8_t(p>>16), uint8_t(p>>24), uint8_t(p>>32), uint8_t(p>>40), uint8_t(p>>48), uint8_t(p>>56),  // mov rax,imm64
                                             0xFF, 0xD0 };                                                                                                                  // call rax

        jitBuffer.CopyBlock(&CallMemberFunc, sizeof(CallMemberFunc));
    }

    static void SetTemp0From(void* wrapper, JITBuffer& jitBuffer)
    {
        const intptr_t p=(intptr_t)wrapper;
        const uint8_t SetTemp0[] = {0x48, 0xB8, 
                                     uint8_t(p>>0), uint8_t(p>>8), uint8_t(p>>16), uint8_t(p>>24), uint8_t(p>>32), uint8_t(p>>40), uint8_t(p>>48), uint8_t(p>>56)};  // mov rax,imm64
        jitBuffer.CopyBlock(&SetTemp0, sizeof(SetTemp0));
    }

    static void MoveParam0ToTemp0Offset(uint8_t offset, JITBuffer& jitBuffer)
    {
        const uint8_t MoveParam0ToTemp0[] = {0x48, 0x89, 0x48, uint8_t(offset)};
        jitBuffer.CopyBlock(&MoveParam0ToTemp0, sizeof(MoveParam0ToTemp0));
    }

    static void MoveTemp0ToParam0(JITBuffer& jitBuffer)
    {
        const uint8_t MoveTemp0ToParam0[] = {0x48, 0x89, 0xC1};
        jitBuffer.CopyBlock(&MoveTemp0ToParam0, sizeof(MoveTemp0ToParam0));
    }

    static void Jmp(void* target, JITBuffer& jitBuffer)
    {
        const intptr_t p=(intptr_t)target;
        const uint8_t Jmp[] = {0x48, 0xB8, 
                               uint8_t(p>>0), uint8_t(p>>8), uint8_t(p>>16), uint8_t(p>>24), uint8_t(p>>32), uint8_t(p>>40), uint8_t(p>>48), uint8_t(p>>56),  // mov rax,imm64
                               0xFF, 0xE0 };                                                                                                                  // jmp rax

        jitBuffer.CopyBlock(&Jmp, sizeof(Jmp));
    }
};
