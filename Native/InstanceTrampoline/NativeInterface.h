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

NATIVE_EXPORT_API void* allocate_trampoline(void* instance, int numInputs, void* method);
NATIVE_EXPORT_API void* allocate_printer(void* method);

#ifdef __cplusplus
}
#endif