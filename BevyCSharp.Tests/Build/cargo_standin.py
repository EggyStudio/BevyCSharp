#!/usr/bin/env python3
"""Stands for cargo in the tests of build/test.py, so they need no cargo and no bridge.

Asked to compile the tests with --no-run it fails to compile where BCS_CARGO_STANDIN says `build`,
and compiles otherwise. Asked to run them, it prints a run of three tests as cargo does, one of them
failing with an assertion where it says `fail`, and all passing otherwise.
"""

import os
import sys

MODE = os.environ.get("BCS_CARGO_STANDIN", "pass")


def main():
    if "--no-run" in sys.argv:
        if MODE == "build":
            print("   Compiling bevy_csharp v0.1.0 (/src/native/bevy_csharp)")
            print("error[E0425]: cannot find function `unset` in this scope")
            print("error: could not compile `bevy_csharp` (lib) due to 1 previous error")
            return 101
        print("    Finished `test` profile [unoptimized + debuginfo] target(s) in 0.01s")
        return 0

    failing = MODE == "fail"
    print("     Running unittests src/lib.rs (native/target/debug/deps/bevy_csharp-0)")
    print()
    print("running 3 tests")
    print("test log::tests::an_error_is_kept ... ok")
    print(f"test log::tests::a_line_too_long ... {'FAILED' if failing else 'ok'}")
    print("test render::shaders::tests::the_layout ... ok")
    if failing:
        print()
        print("failures:")
        print()
        print("---- log::tests::a_line_too_long stdout ----")
        print()
        print("thread 'log::tests::a_line_too_long' (2055624) panicked at bevy_csharp/src/log.rs:139:9:")
        print("assertion `left == right` failed")
        print("  left: Some(\"bcs_test: it broke asset=ship.glb\")")
        print(" right: Some(\"bcs_test: it broke\")")
        print("note: run with `RUST_BACKTRACE=1` environment variable to display a backtrace")
        print()
        print()
        print("failures:")
        print("    log::tests::a_line_too_long")
        print()
    print(f"test result: {'FAILED' if failing else 'ok'}. {2 if failing else 3} passed; {1 if failing else 0} failed; 0 ignored; 0 measured; 0 filtered out; finished in 0.00s")
    if failing:
        print()
        print("error: test failed, to rerun pass `-p bevy_csharp --lib`")
        return 101
    return 0


if __name__ == "__main__":
    sys.exit(main())
