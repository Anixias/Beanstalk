#include <stdio.h>
#include <math.h>
#include <time.h>
#include <windows.system.h>

// === Console ===
void set_console_output_encoding(unsigned int code_page)
{
    SetConsoleOutputCP(code_page);
}

unsigned int get_console_output_encoding()
{
    return GetConsoleOutputCP();
}

void print(const char* str)
{
    fputs(str, stdout);
}

void print_int(int value)
{
    if (value >= 0) {
        int max_count = (int) ((ceil(log10(value)) + 1) * sizeof(char));
        char str[max_count];
        sprintf(str, "%d", value);
        fputs(str, stdout);
    }
    else {
        int max_count = (int) ((ceil(log10(-value)) + 1) * sizeof(char));
        char str[max_count];
        sprintf(str, "-%d", -value);
        fputs(str, stdout);
    }
}

void print_long_long(long long value)
{
    int max_count = (int)((ceil(log10(value)) + 1) * sizeof(char));
    char str[max_count];
    sprintf(str, "%lld", value);
    fputs(str, stdout);
}

// === Time ===
time_t get_current_time()
{
    return time(0);
}