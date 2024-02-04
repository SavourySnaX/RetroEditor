#include "JITBuffer.h"

class Trampoline
{
public:
    Trampoline(JITBuffer* buffer, void* instance)
    {
        jitBuffer=buffer;
        instancePtr = jitBuffer->AllocatePointer(instance);
    }

    void* GenerateTrampoline(void* functionToWrap, int numParams)
    {
        jitBuffer->StartBlock();
        jitBuffer->AllocateStackSpace(numParams+1);
        for (int i = numParams-1; i >= 0; i--)
        {
            jitBuffer->CopySlotToNextSlot(i, numParams + 1);
        }
        jitBuffer->SetThis(instancePtr);
        jitBuffer->CallMemberFunc(functionToWrap);
        jitBuffer->FreeStackSpace(numParams+1);
        jitBuffer->Return();
        return jitBuffer->EndBlock();
    }

    void* GeneratePrinter(void* wrapper,void* printMethod)
    {
        jitBuffer->StartBlock();
        jitBuffer->SetTemp0From(wrapper);
        jitBuffer->MoveParam0ToTemp0Offset(8);
        jitBuffer->MoveTemp0ToParam0();
        jitBuffer->Jmp(printMethod);

        return jitBuffer->EndBlock();
    }


private:
    
    JITBuffer* jitBuffer;
    void* instancePtr;
};
