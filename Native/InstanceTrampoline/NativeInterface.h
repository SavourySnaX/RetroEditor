#pragma once
/*

*/

#ifdef WIN32
#define NATIVE_EXPORT_API __declspec(dllexport)
#else
#define NATIVE_EXPORT_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C"
{
#endif

typedef void* IntPtr;

NATIVE_EXPORT_API IntPtr allocate_trampoline(IntPtr instance, int numInputs, IntPtr method);

#ifdef __cplusplus
}
#endif