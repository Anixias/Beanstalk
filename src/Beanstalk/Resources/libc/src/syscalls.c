#include "syscalls.h"

void exit(int retval)
{
    SYSCALL1_NORET(SYS_exit, retval);
}

int write(int fd, const void* data, int len)
{
    int retval;

    if (fd == -1 || !data || len < 0)
        return -1;

    SYSCALL3(retval, SYS_write, fd, data, len);

    if (retval < 0)
        return -1;

    return retval;
}