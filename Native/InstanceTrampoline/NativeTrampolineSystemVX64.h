#include <cassert>

#include "JITBuffer.h"

/*
    Forms a trampoline that looks approximately like this (stack space and slots based on largest version for example)

    sub     rsp, 0                                      ; AllocateStackSpace

    mov     DWORD PTR [rsp], r9                         ; CopySlotToNext
    
    mov     r9, r8                                      ; CopySlotToNext

    mov     r8, rcx                                     ; CopySlotToNext

    mov     rcx, rdx                                    ; CopySlotToNext

    mov     rdx, rsi                                    ; CopySlotToNext

    mov     rsi, rdi                                    ; CopySlotToNext

    mov     rdi, #imm64AddressOfThisStorage
    mov     rdi, [rdi]                                  ; SetThis

    mov     rax, #imm64AddressOfCWrapperForMemberCall
    call    rax                                         ; CallMemberFunc

    add     rsp, 0                                      ; FreeStackSpace

    ret                                                 ; Return
*/

class NativeTrampolineSystemVX64Implementation
{
public:
    static void AllocateStackSpace(int8_t slots, JITBuffer& jitBuffer)
    {
        // For System V 64bit, first six params are in registers, rest are on stack
        //stack alignment must be mainted

        const uint8_t slotsNeeded = 1;
        const uint8_t AllocStackSpace[] = {0x48, 0x83, 0xEC, (uint8_t)(slotsNeeded*8)};

        jitBuffer.CopyBlock(&AllocStackSpace, sizeof(AllocStackSpace));
    }

    static void FreeStackSpace(int8_t slots, JITBuffer& jitBuffer)
    {
        // For System V 64bit, first six params are in registers, rest are on stack
        //stack alignment must be mainted

        const uint8_t slotsNeeded = 1;
        const uint8_t FreeStackSpace[] = {0x48, 0x83, 0xC4, uint8_t(slotsNeeded*8)};

        jitBuffer.CopyBlock(&FreeStackSpace, sizeof(FreeStackSpace));
    }

    static void CopySlotToNextSlot(const int stackSlot, const int numParams, JITBuffer& jitBuffer)
    {
        switch (stackSlot)
        {
            case 0:
                {
                    const uint8_t slot0[] = {0x48, 0x89, 0xFE};  // mov rsi, rdi
                    jitBuffer.CopyBlock(&slot0, sizeof(slot0));
                }
                break;
            case 1:
                {
                    const uint8_t slot1[] = {0x48, 0x89, 0xF2};  // mov rdx, rsi
                    jitBuffer.CopyBlock(&slot1, sizeof(slot1));
                }
                break;
            case 2:
                {
                    const uint8_t slot2[] = {0x48, 0x89, 0xD1};  // mov rcx, rdx
                    jitBuffer.CopyBlock(&slot2, sizeof(slot2));
                }
                break;
            case 3:
                {
                    const uint8_t slot2[] = {0x49, 0x89, 0xC8};  // mov r8, rcx
                    jitBuffer.CopyBlock(&slot2, sizeof(slot2));
                }
                break;
            case 4:
                {
                    const uint8_t slot2[] = {0x4D, 0x89, 0xC1};  // mov r9, r8
                    jitBuffer.CopyBlock(&slot2, sizeof(slot2));
                }
                break;
            case 5:
                {
                    const uint8_t toNewSlot[] = {0x4C, 0x89, 0x0C, 0x24};          // mov QWORD PTR [rsp], r9
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
        const uint8_t SetThisPtr[] = {0x48, 0xBF, 
                                         uint8_t(p>>0), uint8_t(p>>8), uint8_t(p>>16), uint8_t(p>>24), uint8_t(p>>32), uint8_t(p>>40), uint8_t(p>>48), uint8_t(p>>56),  // mov rdi,imm64
                                         0x48, 0x8b, 0x3f};                                                                                                             // mov rdi,[rdi]

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
        const uint8_t MoveParam0ToTemp0[] = {0x48, 0x89, 0x78, uint8_t(offset)};                                                                                     // mov QWORD PTR [rax+offset], rdi
        jitBuffer.CopyBlock(&MoveParam0ToTemp0, sizeof(MoveParam0ToTemp0));
    }

    static void MoveTemp0ToParam0(JITBuffer& jitBuffer)
    {
        const uint8_t MoveTemp0ToParam0[] = {0x48, 0x89, 0xC7};                                                                                                     // mov rdi, rax 
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

