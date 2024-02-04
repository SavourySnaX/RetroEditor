#pragma once

#include <stdint.h>
#include "platform.h"

uint8_t* JitAllocateMemory(size_t sizeInBytes);
void JitBeginWriteToMemory();
void JitWriteToMemory(uint8_t* destinationPtr, const void* sourcePtr, size_t length);
void JitEndWriteToMemory(uint8_t* destinationPtr, size_t length);
void JitFreeMemory(uint8_t* memory, size_t sizeInBytes);

class JITBuffer
{
public:
    JITBuffer(size_t blockSize);
    ~JITBuffer();

    void* AllocatePointer(const void* value);
    void StartBlock();
    void CopyBlock(const void* source, size_t length);
    void* EndBlock();

    void WriteBytes(const void* source, size_t length, uint32_t destinationOffset);

    void AllocateStackSpace(int8_t slots);
    void FreeStackSpace(int8_t slots);
    void CopySlotToNextSlot(const int stackSlot, const int numParams);
    void SetThis(void* ptr);
    void Return();
    void CallMemberFunc(void* staticFuncWithMemberPointer);

    void SetTemp0From(void *wrapper);
    void MoveParam0ToTemp0Offset(int offset);
    void MoveTemp0ToParam0();
    void Jmp(void *destination);

    void* GetCurrentAddress() { return jitBuffer+jitPosition; }

    void *JITBuffer::Allocate(size_t size);

private:
    void JITBuffer::Skip(size_t length);
    uint8_t* jitBuffer;
    size_t jitPosition;
    size_t jitAllocation;
    size_t jitBufferStartLoc;
};

