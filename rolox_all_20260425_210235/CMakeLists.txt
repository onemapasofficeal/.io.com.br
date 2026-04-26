cmake_minimum_required(VERSION 3.20)
project(RoloxNative)

set(CMAKE_CXX_STANDARD 17)

add_library(RoloxNative SHARED
    RoloxNative.cpp
    RoloxNative.h
)

target_compile_definitions(RoloxNative PRIVATE ROLOXNATIVE_EXPORTS)

# Output direto para o bin do projeto C#
set_target_properties(RoloxNative PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../RoloxApp/bin/Debug/net8.0-windows"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../RoloxApp/bin/Debug/net8.0-windows"
)
