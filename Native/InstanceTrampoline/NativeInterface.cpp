/*

 Quickly hack together a wrapper so that we can have an instance wrapped method to deal with audio callbacks from raylib 

*/

#include <stdint.h>
#include "NativeInterface.h"
#include "JITBuffer.h"
#include "NativeTrampoline.h"

IntPtr allocate_trampoline(IntPtr instance, int numInputs, IntPtr method)
{
    auto jitBuffer = new JITBuffer(4096);
    auto trampoline = new Trampoline(jitBuffer, instance);

    return trampoline->GenerateTrampoline(method, numInputs);

    return nullptr;
}