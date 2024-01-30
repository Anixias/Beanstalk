/*#include <stdio.h>
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

const char* int8_to_string(char value)
{
    if (value >= 0) {
        int max_count = (size_t) ((ceil(log10(value)) + 1) * sizeof(char));
        char* str = malloc(max_count);
        sprintf(str, "%d", value);
        return str;
    }
    else {
        int max_count = (size_t) ((ceil(log10(-value)) + 1) * sizeof(char));
        char* str = malloc(max_count);
        sprintf(str, "-%d", -value);
        return str;
    }
}

const char* uint8_to_string(unsigned char value)
{
    int max_count = (size_t) ((ceil(log10(value)) + 1) * sizeof(char));
    char* str = malloc(max_count);
    sprintf(str, "%d", value);
    return str;
}

const char* int16_to_string(short value)
{
    if (value >= 0) {
        int max_count = (size_t) ((ceil(log10(value)) + 1) * sizeof(char));
        char* str = malloc(max_count);
        sprintf(str, "%d", value);
        return str;
    }
    else {
        int max_count = (size_t) ((ceil(log10(-value)) + 1) * sizeof(char));
        char* str = malloc(max_count);
        sprintf(str, "-%d", -value);
        return str;
    }
}

const char* uint16_to_string(unsigned short value)
{
    int max_count = (size_t) ((ceil(log10(value)) + 1) * sizeof(char));
    char* str = malloc(max_count);
    sprintf(str, "%d", value);
    return str;
}

const char* int32_to_string(int value)
{
    if (value >= 0) {
        int max_count = (size_t) ((ceil(log10(value)) + 1) * sizeof(char));
        char* str = malloc(max_count);
        sprintf(str, "%d", value);
        return str;
    }
    else {
        int max_count = (size_t) ((ceil(log10(-value)) + 1) * sizeof(char));
        char* str = malloc(max_count);
        sprintf(str, "-%d", -value);
        return str;
    }
}

const char* uint32_to_string(unsigned int value)
{
    int max_count = (size_t) ((ceil(log10(value)) + 1) * sizeof(char));
    char* str = malloc(max_count);
    sprintf(str, "%d", value);
    return str;
}

// === Time ===
time_t get_current_time()
{
    return time(0);
}*/