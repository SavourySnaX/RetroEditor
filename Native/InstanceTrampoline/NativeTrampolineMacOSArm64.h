#include <cassert>

#include "JITBuffer.h"

/*
    Forms a trampoline that looks approximately like this (stack space and slots based on largest version for example)

    stp     x29,x30,[sp,#-16]
    mov     x29,sp                                      ; AllocateStackSpace

    mov     x6,x5                                       ; CopySlotToNext

    mov     x5,x4                                       ; CopySlotToNext

    mov     x4,x5                                       ; CopySlotToNext

    mov     x3,x4                                       ; CopySlotToNext

    mov     x2,x3                                       ; CopySlotToNext

    mov     x1,x2                                       ; CopySlotToNext

    adr     x0, ptrOffset
    ldr     x0, [x0]                                    ; SetThis

    movz    x8, #imm64AddressOfCWrapper (bits 0-15)
    movk    x8, #imm64AddressOfCWrapper (bits 16-31), LSL 16
    movk    x8, #imm64AddressOfCWrapper (bits 32-47), LSL 32
    movk    x8, #imm64AddressOfCWrapper (bits 48-63), LSL 48
    blr     x8                                          ; CallMemberFunc

    ldp     x29,x30,[sp],#16                            ; FreeStackSpace

    ret                                                 ; Return
*/


class NativeTrampolineMacOSArm64Implementation
{
public:
    static void AllocateStackSpace(int8_t slots, JITBuffer& jitBuffer)
    {
        // Arm uses registers 0-7 as parameters, which means we don't need stack space based on the number of slots
        //however we do need stack space to preserve the frame pointer and link register, as the frame pointer MUST be set for non leaf functions

        const uint8_t AllocStackSpace[] = {0xFD, 0x7B, 0xBF, 0xA9,     // stp x29,x30,[sp,#-16]
                                           0xFD, 0x03, 0x00, 0x91};    // mov x29 ,sp

        jitBuffer.CopyBlock(&AllocStackSpace, sizeof(AllocStackSpace));
    }

    static void FreeStackSpace(int8_t slots, JITBuffer& jitBuffer)
    {
        // Arm uses registers 0-7 as parameters, which means we don't need stack space based on the number of slots
        //however we do need stack space to preserve the frame pointer and link register, as the frame pointer MUST be set for non leaf functions
        
        const uint8_t FreeStackSpace[] = {0xFD, 0x7B, 0xC1, 0xA8};  // ldp x29,x30,[sp], #16

        jitBuffer.CopyBlock(&FreeStackSpace, sizeof(FreeStackSpace));
    }

    static void CopySlotToNextSlot(const int stackSlot, const int numParams, JITBuffer& jitBuffer)
    {
        assert(stackSlot<7 && "Out of Range stack slot!");

        // this will be mov x(stackSlot+1),x(stackSlot). e.g. for stackslot of 0 - mov x1,x0

        const uint8_t CopySlot[] = {uint8_t(0xE0 + stackSlot+1), 0x03, uint8_t(0x00 + stackSlot), 0xAA };    // mov xd,xs (e.g. mov x1,x0)

        jitBuffer.CopyBlock(&CopySlot, sizeof(CopySlot));
    }

    static void SetThis(void* ptr, JITBuffer& jitBuffer)
    {
        // ptr is allocated within the current jitBlock, so we should always be able to reach it
        // so we compute its relative offset and use adr + ldr to get the value it points to into
        // the first register
        const intptr_t p=(intptr_t)ptr;
        const intptr_t c=(intptr_t)jitBuffer.GetCurrentAddress();
        const intptr_t r=p-c;                                       // Get Relative offset to ptr
        const intptr_t immlo = r&0x3;
        const intptr_t immhi = (r>>2)&0x7FFFF;
        const uint8_t SetThisPtr[] = {uint8_t(0x0 + ((immhi&0x7)<<5)), uint8_t(immhi>>3), uint8_t(immhi>>11), uint8_t(0x10 + (immlo<<5)),            // adr x0, immhi:immlo (pc relative)
                                      0x00, 0x00, 0x40, 0xF9 };                                                                                 // ldr x0, [x0]

        assert((r>-1024*1024) && (r<1024*1024) && "Relocation out of range");

        jitBuffer.CopyBlock(&SetThisPtr, sizeof(SetThisPtr));
    }

    static void Return(JITBuffer& jitBuffer)
    {
        const uint8_t Ret[] = {0xC0, 0x03, 0x5F, 0xD6};

        jitBuffer.CopyBlock(&Ret, sizeof(Ret));
    }

    static void CallMemberFunc(void* staticFuncWithMemberPointer, JITBuffer& jitBuffer)
    {
        // Here we just use movz,movk,movk,movk to load a directly placed immediate64 value

        const intptr_t p=(intptr_t)staticFuncWithMemberPointer;
        const intptr_t imm0 = p&0xFFFF;
        const intptr_t imm1 = (p>>16)&0xFFFF;
        const intptr_t imm2 = (p>>32)&0xFFFF;
        const intptr_t imm3 = (p>>48)&0xFFFF;

        const uint8_t CallMemberFunc[] = { 
            uint8_t(0x08 + ((imm0&0x7)<<5)), uint8_t(imm0>>3), uint8_t(0x80 + ((imm0>>11)&0x1F)), 0xD2,                   // movz x8, #lo16 bits of ptr
            uint8_t(0x08 + ((imm1&0x7)<<5)), uint8_t(imm1>>3), uint8_t(0xA0 + ((imm1>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 16
            uint8_t(0x08 + ((imm2&0x7)<<5)), uint8_t(imm2>>3), uint8_t(0xC0 + ((imm2>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 32
            uint8_t(0x08 + ((imm3&0x7)<<5)), uint8_t(imm3>>3), uint8_t(0xE0 + ((imm3>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 48
            0x00, 0x01, 0x3F, 0xD6};                                                                                      // blr x8

        jitBuffer.CopyBlock(&CallMemberFunc, sizeof(CallMemberFunc));
    }

    static void SetTemp0From(void* wrapper, JITBuffer& jitBuffer)
    {
        const intptr_t p=(intptr_t)wrapper;
        const intptr_t imm0 = p&0xFFFF;
        const intptr_t imm1 = (p>>16)&0xFFFF;
        const intptr_t imm2 = (p>>32)&0xFFFF;
        const intptr_t imm3 = (p>>48)&0xFFFF;

        const uint8_t CallMemberFunc[] = { 
            uint8_t(0x08 + ((imm0&0x7)<<5)), uint8_t(imm0>>3), uint8_t(0x80 + ((imm0>>11)&0x1F)), 0xD2,                   // movz x8, #lo16 bits of ptr
            uint8_t(0x08 + ((imm1&0x7)<<5)), uint8_t(imm1>>3), uint8_t(0xA0 + ((imm1>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 16
            uint8_t(0x08 + ((imm2&0x7)<<5)), uint8_t(imm2>>3), uint8_t(0xC0 + ((imm2>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 32
            uint8_t(0x08 + ((imm3&0x7)<<5)), uint8_t(imm3>>3), uint8_t(0xE0 + ((imm3>>11)&0x1F)), 0xF2                    // movk x8, #lo16 bits of ptr, LSL 48
            };

        jitBuffer.CopyBlock(&CallMemberFunc, sizeof(CallMemberFunc));
    }

    static void MoveParam0ToTemp0Offset(uint8_t offset, JITBuffer& jitBuffer)
    {
        offset>>=3; // align to 8 byte boundary
        offset<<=2; // put in correct position for instruction
        const uint8_t MoveParam0ToTemp0[] = {0x00, 0x10 + offset, 0x00, 0xF9};                                            // str x0, [x8, offset]
        jitBuffer.CopyBlock(&MoveParam0ToTemp0, sizeof(MoveParam0ToTemp0));
    }

    static void MoveTemp0ToParam0(JITBuffer& jitBuffer)
    {
        const uint8_t MoveTemp0ToParam0[] = {0xE0, 0x03, 0x08, 0xAA};                                                    // mov x0, x8
        jitBuffer.CopyBlock(&MoveTemp0ToParam0, sizeof(MoveTemp0ToParam0));
    }

    static void Jmp(void* target, JITBuffer& jitBuffer)
    {
        const intptr_t p=(intptr_t)target;
        const intptr_t imm0 = p&0xFFFF;
        const intptr_t imm1 = (p>>16)&0xFFFF;
        const intptr_t imm2 = (p>>32)&0xFFFF;
        const intptr_t imm3 = (p>>48)&0xFFFF;

        const uint8_t jmp[] = { 
            uint8_t(0x08 + ((imm0&0x7)<<5)), uint8_t(imm0>>3), uint8_t(0x80 + ((imm0>>11)&0x1F)), 0xD2,                   // movz x8, #lo16 bits of ptr
            uint8_t(0x08 + ((imm1&0x7)<<5)), uint8_t(imm1>>3), uint8_t(0xA0 + ((imm1>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 16
            uint8_t(0x08 + ((imm2&0x7)<<5)), uint8_t(imm2>>3), uint8_t(0xC0 + ((imm2>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 32
            uint8_t(0x08 + ((imm3&0x7)<<5)), uint8_t(imm3>>3), uint8_t(0xE0 + ((imm3>>11)&0x1F)), 0xF2,                   // movk x8, #lo16 bits of ptr, LSL 48
            0x00, 0x01, 0x1F, 0xD6};                                                                                      // br x8

        jitBuffer.CopyBlock(&jmp, sizeof(jmp));
    }

};