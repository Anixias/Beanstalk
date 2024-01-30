#ifndef LIBC_SYSCALLS_H
#define LIBC_SYSCALLS_H

#include "file_handles.h"

#ifdef Arch_x86_64
#define SYS_write 1
#define SYS_exit 60

#define SYSCALL1_NORET(nr, arg1) \
    __asm__("syscall\n\t" \
            : \
            : "a" (nr), "D" (arg1) \
            : "rcx", "r11" )

#define SYSCALL3(retval, nr, arg1, arg2, arg3) \
    __asm__("syscall\n\t" \
            : "=a" (retval) \
            : "a" (nr), "D" (arg1), "S" (arg2), "d" (arg3) \
            : "rcx", "r11" )
#endif

static inline void exit(int retval);
static inline int write(int fd, const void* data, int len);

#endif //LIBC_SYSCALLS_H
