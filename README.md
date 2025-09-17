# clipboard-tweak

This software conceptually provides a "spare" clipboard to enhance Windows clipboard.
It behaves as follows according to the key pressed together with Ctrl+Alt+Shift. 
- C
  - Stores the selected content into the spare clipboard. A blank window is shown as indication during clipboard operation.
- X
  - Like the "C" case, but the selected content is cut (corresponding to Ctrl+X).
- V
  - The spare clipboard is pasted.

# Dependencies
There are some release versions
- `clipboard-tweak-net-framework` is compiled with .NET Framework 4.6.2, whose runtime is pre-installed to Windows 10/11 and regularly updated Windows8/8.1.  This is **recommended**.
  - This software can be compiled with .NET Framework 3.5, whose runtime is pre-installed in Windows 7, but some defects are confirmed (e.g. unable to preserve font information between copy/paste in some cases)
- `clipboard-tweak` is compiled with newer .NET (not .NET Framework), requiring runtime installation.
