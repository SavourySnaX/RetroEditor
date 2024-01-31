#pragma once

#if __APPLE__
#if __x86_64__
#define BUILD_MACOS_X64
#define BUILD_X64
#elif __arm64__
#define BUILD_MACOS_ARM64
#define BUILD_ARM64
#endif
#elif defined(WIN32) || defined(_WIN32)
#define BUILD_WINDOWS_X64
#define BUILD_X64
#elif __linux__
#define BUILD_LINUX_X64
#define BUILD_X64
#else
#error Unkown Platform/Architecture
#endif

#if defined(BUILD_MACOS_ARM64) || defined(BUILD_MACOS_X64) || defined(BUILD_LINUX_X64)
#define BUILD_USES_DLSYM
#define BUILD_USES_MMAP
#define BUILD_USES_CLANG
#include <dlfcn.h>
#include <sys/mman.h>

#if defined(BUILD_MACOS_ARM64)
#define BUILD_USES_JITPAGES
#include <libkern/OSCacheControl.h>
#include <pthread.h>            // Needed in addition for handling locking/unlocking of jitted memory
#endif

#elif defined(BUILD_WINDOWS_X64)
#define BUILD_USES_WIN32
#define BUILD_USES_CL
#include <windows.h>
#else
#error Unsupported build configuration
#endif
