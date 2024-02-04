/*

 Quickly hack together a wrapper so that we can have an instance wrapped method to deal with audio callbacks from raylib 

*/

#include <stdint.h>
#include "NativeInterface.h"
#include "JITBuffer.h"
#include "NativeTrampoline.h"
#include <stdio.h>

void* allocate_trampoline(void* instance, int numInputs, void* method)
{
    auto jitBuffer = new JITBuffer(4096);
    auto trampoline = new Trampoline(jitBuffer, instance);

    return trampoline->GenerateTrampoline(method, numInputs);

    return nullptr;
}

struct PrintWrapper
{
    uint64_t bufferSize;
    uint64_t verbosity;
    void* bufferForPrint;
    void* method;
};

void printf_handler(PrintWrapper *wrapper, const char *format, ...);

void* allocate_printer(void* method)
{
    auto jitBuffer = new JITBuffer(8192);
    auto string = jitBuffer->Allocate(4096);
    auto wrapper = (PrintWrapper*)jitBuffer->Allocate(sizeof(PrintWrapper));
    wrapper->bufferSize = 4096;
    wrapper->verbosity = 0;
    wrapper->bufferForPrint = string;
    wrapper->method = method;
    auto trampoline = new Trampoline(jitBuffer, nullptr);

    return trampoline->GeneratePrinter(wrapper,printf_handler);
}

void printf_handler(PrintWrapper* wrapper, const char* format, ...)
{
    va_list args;
    va_start(args, format);
    vsnprintf((char*)wrapper->bufferForPrint, wrapper->bufferSize, format, args);
    va_end(args);
    ((void (*)(uint64_t, const char *))wrapper->method)(wrapper->verbosity, (const char *)wrapper->bufferForPrint);
}
