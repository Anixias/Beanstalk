cmake_minimum_required(VERSION 3.25)
project(cpp)

set(CMAKE_CXX_STANDARD 23)

add_executable(cpp main.cpp)
target_link_libraries(cpp "C:/Program Files/LLVM/lib/LLVM-C.lib")
