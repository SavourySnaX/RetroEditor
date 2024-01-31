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


private:
    
    JITBuffer* jitBuffer;
    void* instancePtr;
};
